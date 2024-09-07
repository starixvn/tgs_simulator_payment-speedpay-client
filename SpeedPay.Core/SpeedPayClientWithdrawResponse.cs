namespace Payment.SpeedPay;
public record SpeedPayClientWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal MerchantBalance);
