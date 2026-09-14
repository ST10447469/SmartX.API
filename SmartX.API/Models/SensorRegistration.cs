namespace SmartX.API.Models;


public class SensorRegistration
{
    public string DeviceId { get; set; } = string.Empty;   // MAC address / unique identifier
    public string Location { get; set; } = string.Empty;   // Room, Zone, Node ID
    public SensorCategory Category { get; set; }
    public List<string> AttachmentPaths { get; set; } = new();
}