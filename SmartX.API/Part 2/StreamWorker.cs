namespace SmartX.API.Part_2;
using Microsoft.Extensions.Hosting;



/// <summary>Background consumer: drains the queues (critical first) and sweeps for silent nodes.</summary>
public class StreamWorker(IngestionPipeline pipeline, DeviceRegistry registry, PredictionEngine prediction) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var lastSweep = DateTime.UtcNow;
        while (!ct.IsCancellationRequested)
        {
            pipeline.ProcessPending();
            if (DateTime.UtcNow - lastSweep > TimeSpan.FromSeconds(5))
            {
                foreach (var mac in registry.SweepOffline()) prediction.RecordEvent($"ALERT:{mac}");
                lastSweep = DateTime.UtcNow;
            }
            try { await Task.Delay(200, ct); } catch (OperationCanceledException) { }
        }
    }
}