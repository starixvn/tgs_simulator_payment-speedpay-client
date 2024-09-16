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
        var settings = JsonSerializer.Deserialize<SpeedPayDepositSettings>(
            data.ApplyInfo,
            SpeedPayUtility.SerializerOptions)!;
        var amount = decimal.Parse(data.Amount);
        var datetime = TimeProducer.GetTimeByTimeZone(settings.Timezone);
        var paymentDetails = new Dictionary<string, string>
        {
            { "bankCode", settings.BankCode },
            { "amount", amount.ToString("0.00") },
            { "paymentCode", settings.PaymentCode },
            { "orderNo", settings.OrderNo.ToString() },
            { "orderTime", datetime.ToString("yyyyMMddHHmmss") },
            { "returnUrl", settings.ReturnUrl },
            { "notifyUrl", new Uri(_options.CallbackBaseUrl, "deposit/callback").AbsoluteUri },
            { "memberCode", settings.MemberCode },
        };

        AddNoRequireValue(ref paymentDetails, [(settings.RealName, "realName"), (settings.AccountName, "accountName")]);

        logger.LogInformation("Request send: {applyInfo}", paymentDetails);

        var response = await speedPayClient.DepositAsync(paymentDetails, settings, cancellationToken).ConfigureAwait(false);

        var applyInfo = new SpeedPayDepositApplyInfo(
            settings,
            new SpeedPayDepositResponse(
                FormId: settings.OrderNo,
                Amount: response["RealOrderAmount"]!.GetValue<decimal>(),
                RedirectUrl: new Uri(response["resultUrl"]!.GetValue<string>())
             ));

        await paymentClient.ScheduleAsync(
            "tgs.payment.speedpay.deposit.schedule.reject",
            settings.OrderNo,
            DateTime.UtcNow.AddMinutes(settings.ExpirationTime),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, SpeedPayUtility.SerializerOptions)
        };
    }

    private static void AddNoRequireValue(ref Dictionary<string, string> origin, IEnumerable<(string? value, string key)> items)
    {
        foreach (var (value, key) in items)
        {
            if (value != null)
            {
                origin[value] = key;
            }
        }
    }
}
