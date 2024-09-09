using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Payment.SpeedPay.ViewModels;

public class NotifyViewModel
{
    [FromForm(Name = "signMsg")]
    public string SignMsg { get; set; } = default!;
    [FromForm(Name = "version")]
    public string Version { get; set; } = default!;
    [FromForm(Name = "notifyTime")]
    public string NotifyTime {  get; set; } = default!;
    [FromForm(Name = "content")]
    public string Content { get; set; } = default!;
}
