namespace Payment.SpeedPay;
public record SpeedPayClientDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string Token,
    string Currency,
    string Transaction);
