namespace Payment.SpeedPay.Client;
public record SpeedPayWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal Fee,
    decimal ActualAmount);
