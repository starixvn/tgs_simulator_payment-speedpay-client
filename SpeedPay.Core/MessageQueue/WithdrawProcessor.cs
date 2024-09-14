using System.Text.Json;
using Microsoft.Extensions.Options;
using Payment.SpeedPay.Client;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Payment.SpeedPay.MessageQueue;

internal class WithdrawProcessor(
    SpeedPayClient speedPayClient,
    IPaymentClient paymentClient,
    IOptions<SpeedPayOptions> optionsAccessor) : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly SpeedPayOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<SpeedPayWithdrawSettings>(data.ApplyInfo, SpeedPayUtility.SerializerOptions)!;
        var datetime = TimeProducer.GetTimeByTimeZone(settings.Timezone);
        var amount = decimal.Parse(data.Amount);
        var dict = new Dictionary<string, string>
        {
            { "amount",amount.ToString("0.00")},
            { "bankCode", settings.BankCode },
            { "bankBranch", settings.BankBranch },
            { "bankAccountNo", settings.BankAccountNo },
            { "bankAccountName", settings.BankAccountName },
            { "orderNo", settings.OrderNo.ToString() },
            { "orderTime", datetime.ToString("yyyyMMddHHmmss") },
            { "notifyUrl", new Uri(_options.CallbackBaseUrl, "withdraw/callback").AbsoluteUri },
            { "memberCode", settings.MemberCode }
        };

        var withdrawResponse = await speedPayClient.WithdrawAsync(
            dict,
            settings,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new SpeedPayWithdrawApplyInfo(
            settings,
            new SpeedPayWithdrawResponse(Guid.Parse(withdrawResponse["orderId"]!.GetValue<string>()),
                                        withdrawResponse["gbpOrderNo"]!.GetValue<string>(),
                                        decimal.Parse(withdrawResponse["fee"]!.GetValue<string>()),
                                        decimal.Parse(withdrawResponse["amount"]!.GetValue<string>())));

        await paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            applyInfo.Response.FormId,
            applyInfo.Response.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, SpeedPayUtility.SerializerOptions)
        };
    }
}
