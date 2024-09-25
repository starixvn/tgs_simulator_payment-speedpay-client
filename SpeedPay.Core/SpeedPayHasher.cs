using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Payment.SpeedPay;

public class SpeedPayHasher(ILogger<SpeedPayHasher> logger)
{
    private readonly ILogger<SpeedPayHasher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string ProduceSign(IDictionary<string, string> content, IEnumerable<string> keysHash)
    {
        var contentToHash = string.Join("&", keysHash.Select(x => $"{x}:{content[x]}"));

        return ComputeMd5(contentToHash);
    }

    public string ComputeMd5(string content)
    {

        _logger.LogInformation("Compute MD5: {Content}", content);

        var bytes = Encoding.UTF8.GetBytes(content);
        var md5 = MD5.HashData(bytes);

        return BitConverter.ToString(md5).Replace("-", "").ToUpperInvariant();

    }

    public string EncodeBase64(string content)
    {
        _logger.LogInformation("EncodeBase64: {PlainText}", content);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
    }

    public string DecodeBase64(string content)
    {
        _logger.LogInformation("EncodeBase64: {PlainText}", content);

        return Encoding.UTF8.GetString(Convert.FromBase64String(content));
    }
}
