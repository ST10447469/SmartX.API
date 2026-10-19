namespace SmartX.API.Part_2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
public static class Part2Extensions
{

    public static IServiceCollection AddSmartXStream(this IServiceCollection services)
    {
        services.AddSingleton<DeviceRegistry>();
        services.AddSingleton<PredictionEngine>();
        services.AddSingleton<IngestionPipeline>();
        services.AddSingleton<CommandHistory>();
        services.AddHostedService<StreamWorker>();
        return services;
    }
}