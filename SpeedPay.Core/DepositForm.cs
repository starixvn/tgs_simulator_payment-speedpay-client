using Tgs.Payment.Clients;
using Payment.SpeedPay.Client;

namespace Payment.SpeedPay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    SpeedPayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
