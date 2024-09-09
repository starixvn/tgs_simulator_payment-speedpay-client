using System.Text.Json;
using Payment.SpeedPay.ViewModels;

namespace Payment.SpeedPay;

public class NotifyHandler(SpeedPayHasher hasher, ILogger<NotifyHandler> logger)
{
    public bool IsValidSign(NotifyViewModel source, string key)
    {
        var dict = new Dictionary<string, string>
        {
            ["content"] = source.Content,
            ["notifyTime"] = source.NotifyTime,
            ["version"] = source.Version,
            ["key"] = key
        };

        var sign = hasher.ProduceSign(dict, ["content","notifyTime", "version",  "key"]);
        var check = sign! == source.SignMsg;

        if (check)
            logger.LogWarning("Sign value invalid. return: {sign}, compute: {hash}", source.SignMsg, sign);

        return sign! == source.SignMsg;
    }

    public CallbackViewModel ProduceCallbackViewModel(NotifyViewModel source)
    {
        var rawContent = hasher.DecodeBase64(source.Content);
        logger.LogInformation("Params in: {content}", rawContent);

        return JsonSerializer.Deserialize<CallbackViewModel>(rawContent)!;
    }
}
