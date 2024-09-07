namespace Payment.SpeedPay.Client;
public record SpeedPayDepositSettings(
    string PaymentCode,
    string MemberCode,
    string CurrencyCode,
    string BankCode,
    string Customer,
    Guid OrderNo,
    string ReturnUrl,
    string Language,
    string ClientIP,
    string Method);
