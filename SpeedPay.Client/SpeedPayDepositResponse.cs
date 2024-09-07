namespace Payment.SpeedPay.Client;
public record SpeedPayDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl);
