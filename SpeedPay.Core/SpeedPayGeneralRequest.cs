using System.Text.Json.Serialization;

namespace Payment.SpeedPay;

public class SpeedPayGeneralRequest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = default!;

    [JsonPropertyName("merchantNo")]
    public string MerchantNo { get; set; } = default!;

}
