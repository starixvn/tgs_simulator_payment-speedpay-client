namespace Payment.SpeedPay.Client;
public record SpeedPayDepositResponse(
    decimal Amount,
    string ThirdPartyOrderNo,
    Uri RedirectUrl);
