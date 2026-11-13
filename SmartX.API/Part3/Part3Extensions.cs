using Microsoft.Extensions.DependencyInjection;

namespace SmartX.API.Part3;

public static class Part3Extensions
{
    /// <summary>Call once in Program.cs: builder.Services.AddSmartXMesh();</summary>
    public static IServiceCollection AddSmartXMesh(this IServiceCollection services)
    {
        services.AddSingleton<TelemetryHistory>();
        services.AddSingleton<MeshGraph>();
        return services;
    }
}