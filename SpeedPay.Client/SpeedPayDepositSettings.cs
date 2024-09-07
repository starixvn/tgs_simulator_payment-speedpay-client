using System.Text.Json;
using System.Text.Json.Serialization;

namespace Payment.SpeedPay.Client;

public class SpeedPayDepositSettings
{
    [JsonPropertyName("merchantNo")]
    public string MerchantNo { get; set; } = default!;

    [JsonPropertyName("key")]
    public string Key { get; set; } = default!;

    [JsonPropertyName("version")]
    public string Version { get; set; } = default!;

    [JsonPropertyName("bankCode")]
    public string BankCode { get; set; } = default!;

    [JsonPropertyName("paymentCode")]
    public string PaymentCode { get; set; } = default!;

    [JsonPropertyName("orderNo")]
    public Guid OrderNo { get; set; } = default!;

    [JsonPropertyName("returnUrl")]
    public string ReturnUrl { get; set; } = default!;

    [JsonPropertyName("notifyUrl")]
    public string NotifyUrl { get; set; } = default!;

    [JsonPropertyName("memberCode")]
    public string MemberCode { get; set; } = default!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("realName")]
    public string? RealName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("accountName")]
    public string? AccountName { get; set; }

}
