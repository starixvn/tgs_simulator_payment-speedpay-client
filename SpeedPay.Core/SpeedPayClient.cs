using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Payment.SpeedPay.Client;
using Tgs.Logging;

namespace Payment.SpeedPay;

public class SpeedPayClient(
    HttpClient httpClient,
    SpeedPayHasher hasher,
    ITgsLog tgsLog,
    ILogger<SpeedPayClient> logger)
{

    public async ValueTask<JsonNode> DepositAsync(Dictionary<string, string> content, SpeedPayDepositSettings settings, CancellationToken cancellationToken = default)
    {
        var generalRequest = new SpeedPayGeneralRequest
        {
            MerchantNo = settings.MerchantNo,
            Version = settings.Version,
        };

        var formContent = MakeFormRequest(generalRequest, content, settings.Key);
        var request = new HttpRequestMessage(HttpMethod.Post, "qrh5")
        {
            Content = new FormUrlEncodedContent
            (
                formContent
            )
        };

        var (timeExecute, response) = await CalculateTimeExcuteAsync(async () =>
        {
            return await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        });

        var (nodeResponse, rawBody) = await HandleResponseAsync(response, settings.Key, cancellationToken);
        LogApi(tgsLog, formContent, request, timeExecute, response, rawBody, httpClient.BaseAddress!.ToString());

        logger.LogInformation("SpeedPay Deposit Response:{body}", nodeResponse);

        return nodeResponse["code"]!.GetValue<string>() != "0000"
            ? throw new ApiFailException($"Error in Payment Provider with status {nodeResponse["msg"]!.GetValue<string>()}")
            : nodeResponse;
    }

    private static void LogApi(ITgsLog tgsLog, Dictionary<string, string> formContent, HttpRequestMessage request, int timeExecute, HttpResponseMessage response, string rawBody, string prefix)
    {
        var apiCall = new ApiLogEvent
        {
            ExecutionTimeMs = timeExecute,
            Ip = GetIPAddress(),
            Method = request.Method.Method,
            Path = request.RequestUri?.ToString() ?? string.Empty,
            Prefix = prefix,
            RecordedOn = DateTimeOffset.Now,
            RequestContent = JsonSerializer.Serialize(formContent),
            RequestHeaders = JsonSerializer.Serialize(request.Headers),
            ResponseCode = (short)response.StatusCode,
            ResponseContent = rawBody,
            ResponseHeaders = JsonSerializer.Serialize(response.Headers),
            ServerName = Environment.MachineName,
            TrackingId = Guid.Parse(formContent["orderNo"]),
        };

        tgsLog.LogApi(apiCall);
    }

    private Dictionary<string, string> MakeFormRequest(SpeedPayGeneralRequest generalRequest, IDictionary<string, string> paramDetails, string key)
    {
        var requestValue = hasher.EncodeBase64(JsonSerializer.Serialize(paramDetails));
        var hashContent = new Dictionary<string, string>
        {
            { "merchantNo", generalRequest.MerchantNo },
            { "version", generalRequest.Version},
            { "request", requestValue},
            { "key", key}
        };
        var sign = hasher.ProduceSign(hashContent, ["merchantNo", "request", "version", "key"]);

        return new Dictionary<string, string>
        {
            {"merchantNo", generalRequest.MerchantNo },
            { "version", generalRequest.Version},
            { "request", requestValue },
            { "signMsg", sign }
        };
    }

    public async Task<(JsonNode node, string rawBody)> HandleResponseAsync(HttpResponseMessage response, string key, CancellationToken cancellationToken = default)
    {
        _ = response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var paramResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(body)!;
        paramResponse["key"] = key;

        logger.LogInformation("SpeedPay Body:{body}", paramResponse);

        var sign = hasher.ProduceSign(paramResponse, ["merchantNo", "resCode", "resMsg", "response", "version", "key"]);

        if (sign != paramResponse["signMsg"])
            throw new ApiFailException($"Invalid signMsg real: {paramResponse["resMsg"]}, compute: {sign}");

        if (paramResponse!["resCode"] != "000")
            throw new ApiFailException($"Error in response from SpeedPay with status {paramResponse["resMsg"]}");
        var responseContent = hasher.DecodeBase64(paramResponse["response"]);
        var nodeResponse = JsonNode.Parse(responseContent) ?? throw new ApiFailException($"Deposit response message {responseContent} can't read.");

        return (nodeResponse, body);
    }

    public async ValueTask<JsonNode> WithdrawAsync(Dictionary<string, string> content, SpeedPayWithdrawSettings settings, CancellationToken cancellationToken = default)
    {
        var generalRequest = new SpeedPayGeneralRequest
        {
            MerchantNo = settings.MerchantNo,
            Version = settings.Version,
        };

        var formContent = MakeFormRequest(generalRequest, content, settings.Key);
        var request = new HttpRequestMessage(HttpMethod.Post, "payout")
        {
            Content = new FormUrlEncodedContent
            (
                formContent
            )
        };

        var (timeExecute, response) = await CalculateTimeExcuteAsync(async () =>
        {
            return await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        });

        var (nodeResponse, rawBody) = await HandleResponseAsync(response, settings.Key, cancellationToken);

        LogApi(tgsLog, formContent, request, timeExecute, response, rawBody, httpClient.BaseAddress!.ToString());

        logger.LogInformation("SpeedPay Withdraw Response:{body}", nodeResponse);

        return nodeResponse["code"]!.GetValue<string>() switch
        {
            "0000" => nodeResponse,
            _ => throw new ApiFailException($"Error in Payment Provider with status {nodeResponse["msg"]!.GetValue<string>()}"),
        };
    }


    public async ValueTask<decimal> QueryBalanceAsync(Dictionary<string, string> content, BrandPaymentMerchantInfo settings, CancellationToken cancellationToken = default)

    {
        var generalRequest = new SpeedPayGeneralRequest
        {
            MerchantNo = settings.MerchantNo,
            Version = settings.Version,
        };

        var formContent = MakeFormRequest(generalRequest, content, settings.Key);
        var request = new HttpRequestMessage(HttpMethod.Post, "setting")
        {
            Content = new FormUrlEncodedContent
            (
                formContent
            )
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var (nodeResponse, _) = await HandleResponseAsync(response, settings.Key, cancellationToken);

        logger.LogInformation("SpeedPay Withdraw Response:{body}", nodeResponse);

        return decimal.Parse(nodeResponse["availableBalance"]!.GetValue<string>());
    }

    private static async Task<(int timeExecute, T result)> CalculateTimeExcuteAsync<T>(Func<Task<T>> function)
    {
        var timeStart = DateTimeOffset.Now;
        var result = await function().ConfigureAwait(false);
        var timeEnd = DateTimeOffset.Now;

        return (((int)timeEnd.Subtract(timeStart).TotalMilliseconds), result);
    }

    public static string GetIPAddress()
    {
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress? ipAddress = ipHostInfo.AddressList.FirstOrDefault();

        return ipAddress?.ToString() ?? string.Empty;
    }

}
