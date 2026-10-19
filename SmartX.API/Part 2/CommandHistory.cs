namespace SmartX.API.Part_2;


public class CommandHistory(DeviceRegistry registry, PredictionEngine prediction)
{
    readonly Stack<DeviceCommand> _stack = new();
    readonly object _lock = new();

    public DeviceCommand? Issue(string mac, string action)
    {
        mac = mac.Trim().ToUpperInvariant();
        action = action.Trim().ToUpperInvariant();
        if (!registry.SetState(mac, action, out var previous)) return null;

        var cmd = new DeviceCommand(Guid.NewGuid(), mac, registry.NameOf(mac), action, previous, DateTime.UtcNow);
        lock (_lock) _stack.Push(cmd);
        prediction.RecordEvent($"CMD:{mac}:{action}");
        return cmd;
    }

    public DeviceCommand? Undo()
    {
        DeviceCommand? cmd;
        lock (_lock) { if (!_stack.TryPop(out cmd)) return null; }
        registry.SetState(cmd.Mac, cmd.PreviousState, out _);
        return cmd;
    }

   
    public DeviceCommand[] Snapshot() { lock (_lock) return _stack.ToArray(); }
}