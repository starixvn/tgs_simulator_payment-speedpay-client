using Microsoft.Extensions.DependencyInjection;

namespace Payment.SpeedPay.DependencyInjection;

public static class SpeedPayServiceCollectionExtensions
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
            .RegisterAllServiceUtilityTypes(typeof(SpeedPayServiceCollectionExtensions).Assembly, ServiceLifetime.Transient)
            .AddSingleton<SpeedPayHasher>();
    }
}
