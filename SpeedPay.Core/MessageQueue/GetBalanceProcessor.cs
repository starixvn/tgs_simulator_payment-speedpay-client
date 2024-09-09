using System.Text.Json;
using Tgs.Payment.Protos;
using Payment.SpeedPay.Client;
using Valhalla.MessageQueue;

namespace Payment.SpeedPay.MessageQueue;

internal class GetBalanceProcessor(
    SpeedPayClient speedPayClient)
    : IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
    public async ValueTask<PaymentBalanceResponse> HandleAsync(
        string subject,
        PaymentBalanceRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
            data.BrandPaymentSettings,
            SpeedPayUtility.SerializerOptions)!;
        var datetime = TimeProducer.GetTimeByTimeZone(settings.Timezone);
        var dict = new Dictionary<string, string>
        {
            {"queryTime",  datetime.ToString("yyyyMMddHHmmss")}
        };

        var result = await speedPayClient.QueryBalanceAsync(
            dict,
            settings,
            cancellationToken).ConfigureAwait(false);

        return new PaymentBalanceResponse
        {
            Balance = result
        };
    }
}
