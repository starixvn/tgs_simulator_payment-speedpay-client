using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Payment.SpeedPay;

public class SpeedPayClient(
    HttpClient httpClient,
    SpeedPayHasher hasher,
    IOptions<SpeedPayOptions> optionsAccessor,
    ILogger<SpeedPayClient> logger)
{
    private readonly SpeedPayOptions _options = optionsAccessor.Value;

    public async ValueTask<JsonNode> DepositAsync(Dictionary<string, object> content, CancellationToken cancellationToken = default)
    {
        var keysIgnore = new HashSet<string> { "merchantNo", "version", "key" };

        var param = content.Where(x=> !keysIgnore.Contains(x.Key)).ToDictionary();
        var requestValue = hasher.EncodeBase64(JsonSerializer.Serialize(param));
        content["request"] = requestValue;

        var keysHash = new string[] { "merchantNo", "request", "version", "key" };
        var contentToHash = string.Join("&", keysHash.Select(x => $"{x}:{content[x]}"));
        var sign = hasher.ComputeMd5(contentToHash);

        var formContent = new Dictionary<string, string>
        {
            ["version"] = (string)content["version"],
            ["merchantNo"] = (string)content["merchantNo"],
            ["signMsg"] = sign,
            ["request"] = requestValue

        };
        var request = new HttpRequestMessage(HttpMethod.Post, "qrh5")
        {
            Content = new FormUrlEncodedContent
            (
                formContent
            )
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var rawResponseContent = hasher.DecodeBase64(body);

        var nodeBody = JsonNode.Parse(body) ?? throw new ApiFailException($"Deposit response {body} can't read.");

        logger.LogInformation("SpeedPay Deposit Body:{body}", nodeBody);

        if (nodeBody["resCode"]!.GetValue<string>() != "000")
            throw new ApiFailException($"Error in Payment Provider with status {nodeBody["resMsg"]!.GetValue<string>()}");
        var responseContent = hasher.DecodeBase64(nodeBody["response"]!.GetValue<string>());
        var nodeResponse = JsonNode.Parse(responseContent) ?? throw new ApiFailException($"Deposit response message {responseContent} can't read.");

        logger.LogInformation("SpeedPay Deposit Response:{body}", nodeResponse);

        if (nodeResponse["code"]!.GetValue<string>() != "0000")
            throw new ApiFailException($"Error in Payment Provider with status {nodeBody["msg"]!.GetValue<string>()}");

        return nodeResponse;
    }

    public async ValueTask<JsonNode> WithdrawAsync(Dictionary<string,string> content, CancellationToken cancellationToken = default)
    {   
        var request = new HttpRequestMessage(HttpMethod.Post, "api/MerchantTransfer/withdraw")
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        request.Headers.Add("Accept", MediaTypeNames.Application.Json);

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new ApiFailException(body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (node["status_code"]!.GetValue<string>() != "001")
            throw new ApiFailException($"Error in Payment Provider with status {node["statusMessage"]!.GetValue<string>()}");

        logger.LogInformation("GameWalletClient Withdraw Body:{body}", body);

        return node;
    }


    public async ValueTask<decimal> QueryBalanceAsync(Dictionary<string, string> content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/merchant/AvailableBalance")
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("GameWallet Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        if (node["error_code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["error_message"]!.GetValue<string>());

        var balance = node["availableBalance"]!.GetValue<decimal>();

        return balance;
    }

}
