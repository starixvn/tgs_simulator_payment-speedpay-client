namespace Payment.SpeedPay.Client;
public record SpeedPayWithdrawApplyInfo(
    SpeedPayWithdrawSettings Settings,
    SpeedPayWithdrawResponse Response);