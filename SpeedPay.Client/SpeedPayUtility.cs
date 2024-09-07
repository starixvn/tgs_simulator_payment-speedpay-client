using System.Text.Json;
using System.Text.Json.Serialization;

namespace Payment.SpeedPay.Client;

public class SpeedPayUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
        }
    };
}
