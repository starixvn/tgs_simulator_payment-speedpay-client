using System.Buffers.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
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

        _logger.LogInformation("Compute MD5: {content}", content);

        var bytes = Encoding.UTF8.GetBytes(content);
        var md5 = MD5.HashData(bytes);

        return BitConverter.ToString(md5).Replace("-", "").ToUpper();

    }

    public string EncodeBase64(string content)
    {
        _logger.LogInformation("EncodeBase64: {plainText}", content);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
    }

    public string DecodeBase64(string content)
    {
        _logger.LogInformation("EncodeBase64: {plainText}", content);

        return Encoding.UTF8.GetString(Convert.FromBase64String(content));
    }

    public string EncodeParam(string content)
    {
        _logger.LogTrace("FullText: {plainText}", content);

        var based64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        if (string.IsNullOrEmpty(based64))
            return string.Empty;

        var length = based64.Length;
        if (length < 100)
            throw new InvalidDataException("Invalid text to encode");
        var key = new Random().Next(1, 10);
        var param = based64.Insert(length - 10, $"{key}").Insert(key, Guid.NewGuid().ToString("n")[..1]);
        return HttpUtility.UrlEncode(param);
    }
}
