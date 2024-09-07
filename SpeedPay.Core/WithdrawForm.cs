using Tgs.Payment.Clients;
using Payment.SpeedPay.Client;

namespace Payment.SpeedPay;
public record WithdrawForm(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    SpeedPayWithdrawApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
