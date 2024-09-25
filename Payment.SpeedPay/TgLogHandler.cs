using Microsoft.AspNetCore.Http.Extensions;
using Payment.SpeedPay.ViewModels;
using System.Net;
using System.Text.Json;
using Tgs.Logging;

namespace Payment.SpeedPay;

public class TgsLogHandler(ITgsLog tgsLog)
{
    public void LogApi(HttpContext context, CallbackViewModel content, string response)
    {

        var apiCall = new ApiLogEvent
        {
            Ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Method = context.Request.Method,
            Path = context.Request.Path,
            Prefix = context.Request.GetDisplayUrl().Replace(context.Request.Path, string.Empty),
            RecordedOn = DateTimeOffset.Now,
            RequestContent = JsonSerializer.Serialize(content),
            RequestHeaders = JsonSerializer.Serialize(context.Request.Headers),
            ResponseCode = ((short)HttpStatusCode.OK),
            ResponseContent = response,
            ResponseHeaders = JsonSerializer.Serialize(context.Response.Headers),
            ServerName = Environment.MachineName,
            TrackingId = Guid.Parse(content.OrderNo),
        };

        tgsLog.LogApi(apiCall);
    }
}