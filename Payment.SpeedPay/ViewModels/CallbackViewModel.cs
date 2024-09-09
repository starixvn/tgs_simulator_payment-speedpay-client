using System.Text.Json.Serialization;

namespace Payment.SpeedPay.ViewModels;

public class CallbackViewModel
{
    [JsonPropertyName("service")]
    public string Service { get; set; } = default!;

    [JsonPropertyName("merchantNo")]
    public string MerchantNo { get; set; } = default!;

    [JsonPropertyName("paymentCode")]
    public string? PaymentCode { get; set; } = default!;

    [JsonPropertyName("bankCode")]
    public string? BankCode { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = default!;

    [JsonPropertyName("orderNo")]
    public string OrderNo { get; set; } = default!;

    [JsonPropertyName("gbpOrderNo")]
    public string GbpOrderNo { get; set; } = default!;

    [JsonPropertyName("createTime")]
    public string CreateTime { get; set; } = default!;

    [JsonPropertyName("iplocation")]
    public string IpLocation { get; set; } = default!;

    [JsonPropertyName("ipaddress")]
    public string IpAddress { get; set; } = default!;
}
