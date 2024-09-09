namespace Payment.SpeedPay.Client;
public record SpeedPayWithdrawSettings(
    string BankCode,
    string BankBranch,
    string BankAccountNo,
    string BankAccountName,
    Guid OrderNo,
    string MemberCode,
    string MerchantNo,
    string Version,
    string Key,
    string Timezone);
