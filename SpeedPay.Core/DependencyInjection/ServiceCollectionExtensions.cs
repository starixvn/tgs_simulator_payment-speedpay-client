using System.Reflection;
using Payment.SpeedPay;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpeedPay(
       this IServiceCollection services,
       Action<SpeedPayOptions> configureOptions,
       Action<IHttpClientBuilder> ConfigureAiClient)
    {
        ConfigureAiClient?.Invoke(services.AddHttpClient<SpeedPayClient>());

        return services
            .AddOptions<SpeedPayOptions>()
            .Configure(configureOptions)
            .Services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<SpeedPayHasher>();
    }
}
