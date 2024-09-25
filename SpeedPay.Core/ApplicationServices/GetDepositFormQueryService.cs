using System.Text.Json;
using Tgs.Payment.Clients;
using Payment.SpeedPay.Client;
using Tgs.ServiceUtility;

namespace Payment.SpeedPay.ApplicationServices;

public class GetDepositFormQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetDepositFormQuery, DepositForm?>
{
    private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

    public async ValueTask<DepositForm?> GetAsync(GetDepositFormQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _paymentServiceClient
                        .GetDepositFormAsync(query.FormId, cancellationToken)
                        .ConfigureAwait(false);

        if (data == null)
        {
            return null;
        }

        var applyInfo = data.ApplyInfo == null
            ? null
            : JsonSerializer.Deserialize<SpeedPayDepositApplyInfo>(data.ApplyInfo, SpeedPayUtility.SerializerOptions);

        return new DepositForm(
            data.Id,
            data.PlayerCode,
            data.BrandId,
            data.BrandPaymentId,
            data.Amount,
            data.Status,
            applyInfo,
            data.PaymentReturn);
    }
}
