using System.Net;
using System.Net.Mime;
using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payment.SpeedPay;
using Payment.SpeedPay.DependencyInjection;
using Payment.SpeedPay.MessageQueue;
using Tgs.MessageQueue.Nats.Serializers;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
            fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
                "MachineName",
                () => Environment.MachineName));
        });
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});
builder.Services
    .AddControllers();
builder.Services
    .AddTgsLog()
    .AddSingleton<NotifyHandler>()
    .AddSingleton<TgsLogHandler>()
    .AddSpeedPay(
        options => builder.Configuration.GetSection("SpeedPay").Bind(options),
        clientBuilder => clientBuilder
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<SpeedPayOptions>>().Value;

                http.BaseAddress = options.PaymentApiUrl;
            })
            .AddLoggingHandler<SpeedPayOptions>())
    .AddPaymentClient()
    .AddPaymentServiceClient(httpBuilder => httpBuilder
        .ConfigureHttpClient((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<SpeedPayOptions>>().Value;

            http.BaseAddress = options.PaymentServiceBaseUrl;
        }));

builder.Services
    .AddKeyedSingleton(
        "NatQueue",
        (_, _) => new NatsConnection(NatsOpts.Default with
        {
            Url = builder.Configuration.GetConnectionString("NatQueue")!,
            SerializerRegistry = TgsNatsSerializerRegistration.Default
        }))
        .AddMessageQueue()
        .AddNatsMessageQueue(config => config
        .ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("NatQueue"))
        .AddProcessor(
            ProcessorTypes.DepositProcessorType,
            "tgs.payment.speedpay.deposit",
            "tgs.payment.speedpay.deposit")
        .AddProcessor(
            ProcessorTypes.WithdrawProcessorType,
            "tgs.payment.speedpay.withdraw",
            "tgs.payment.speedpay.withdraw")
        .AddHandler(
            ProcessorTypes.DepositTimeoutCheckHandlerType,
            $"tgs.payment.speedpay.deposit.schedule.reject",
            $"tgs.payment.speedpay.deposit.schedule.reject")
        .AddProcessor(
            ProcessorTypes.GetBalanceProcessorType,
            "tgs.payment.speedpay.balance",
            "tgs.payment.speedpay.balance"))
    .AddNatsGlobPatternExchange("*");

builder.Services
    .AddProfileViewer()
    .AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
        logging.MediaTypeOptions.AddText(MediaTypeNames.Application.FormUrlEncoded);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Application.Json);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Text.Html);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Multipart.FormData);
    })
    .AddHealthChecks()
    .Services
    .AddOpenTelemetry()
    .WithTracing(traceBuilder => traceBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddSource("Valhalla.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .WithMetrics(meterBuiler => meterBuiler
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .Services
    .Configure<AspNetCoreTraceInstrumentationOptions>(options => options.Filter =
        httpContext => {
            var path = httpContext.Request.Path;
            if (!path.HasValue)
            {
                return true;
            } 
  
            var excludedPaths = new List<string> { "/swagger", "/healthz", "/metrics", "/profiler/" };
            if (excludedPaths.Exists(segment => path.StartsWithSegments(segment)))
            {
                return false;
            }

            var excludedExtensions = new List<string> { "html", "ico", "png", "jpg" };
            return !excludedExtensions.Exists(ext => path.Value.EndsWith($".{ext}"));
        });

var app = builder.Build();
app.UseCors("AllowAll");
app.UsePathBase($"{builder.Configuration.GetValue<string>("PathBase")}");
app.UseWhen(context => !(context.Request.Path.StartsWithSegments("/healthz") && context.Response.StatusCode == ((int)HttpStatusCode.OK)), app => app.UseHttpLogging());
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Use(async (context, next) =>
{
    await next().ConfigureAwait(false);

    var logger = context.RequestServices.GetService<ILoggerFactory>()
        ?.CreateLogger("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");

    if (logger?.IsEnabled(LogLevel.Information) == true)
    {
        using var stream = new StreamReader(context.Request.Body);

        _ = await stream.ReadToEndAsync().ConfigureAwait(false);
    }
});

SentrySdk.CaptureMessage($"Started result SpeedPay-Payment at {DateTimeOffset.Now} at {Environment.MachineName}");
await app.RunAsync();
