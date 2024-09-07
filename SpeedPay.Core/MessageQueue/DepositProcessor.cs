using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.SpeedPay.Client;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Payment.SpeedPay.MessageQueue;

internal class DepositProcessor(
    IPaymentClient paymentClient,
    SpeedPayClient speedPayClient,
    ILogger<DepositProcessor> logger,
    IOptions<SpeedPayOptions> optionsAccessor)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly SpeedPayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var applyInfoFromData = JsonSerializer.Deserialize<Dictionary<string, string>>(
            data.ApplyInfo,
            SpeedPayUtility.SerializerOptions)!;
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        var datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone!);

        applyInfoFromData["orderTime"] = datetime.ToString("yyyyMMddHHmmss");
        applyInfoFromData["notifyUrl"] = new Uri(_options.PaymentApiUrl, "qrh5").AbsoluteUri;
        applyInfoFromData["amount"] = decimal.Parse(data.Amount).ToString("0.00");

        logger.LogInformation("Request send: {applyInfo}", applyInfoFromData);

        var response = await speedPayClient.DepositAsync(applyInfoFromData, cancellationToken).ConfigureAwait(false);

        var settings = JsonSerializer.Deserialize<SpeedPayDepositSettings>(
            data.ApplyInfo,
            SpeedPayUtility.SerializerOptions)!;

        var applyInfo = new SpeedPayDepositApplyInfo(
            settings,
            new SpeedPayDepositResponse(
                FormId: settings.OrderNo,
                Amount: decimal.Parse(data.Amount),
                RedirectUrl: new Uri(response["resultUrl"]!.GetValue<string>())
             ));

        await paymentClient.ScheduleAsync(
            "tgs.payment.speedpay.deposit.schedule.reject",
            settings.OrderNo,
            DateTime.UtcNow.AddMinutes(60D),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, SpeedPayUtility.SerializerOptions)
        };
    }
}
