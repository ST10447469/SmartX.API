namespace SmartX.API.Controllers;
using global::SmartX.API.Models;
using global::SmartX.API.Services;
using Microsoft.AspNetCore.Mvc;




[ApiController]
[Route("api/sensors")]
public class SensorsController : ControllerBase
{
    
    private static readonly List<SensorRegistration> _sensors = new();

    private readonly DeploymentValidator _validator;

    public SensorsController(DeploymentValidator validator)
    {
        _validator = validator;
    }

    [HttpPost]
    public IActionResult Register([FromBody] SensorRegistration sensor)
    {
        _sensors.Add(sensor);
        return CreatedAtAction(nameof(GetAll), sensor);
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_sensors);

    // Media/Log Attachment — multipart file upload linked to a sensor profile.
    [HttpPost("{deviceId}/attachments")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadAttachment(string deviceId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Uploaded file is empty.");

        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads", deviceId);
        Directory.CreateDirectory(uploadsDir);
        var savePath = Path.Combine(uploadsDir, Path.GetFileName(file.FileName));

        await using (var stream = System.IO.File.Create(savePath))
        {
            await file.CopyToAsync(stream);
        }

        var sensor = _sensors.FirstOrDefault(s => s.DeviceId == deviceId);
        sensor?.AttachmentPaths.Add(savePath);

        return Ok(new { savedTo = savePath });
    }

    // Recursive deployment-tree path validation.
    [HttpPost("validate-path")]
    public IActionResult ValidatePath([FromBody] DeploymentValidationRequest request)
    {
        var valid = _validator.ValidatePath(request.Root, request.PathSegments);
        return Ok(new { valid });
    }
}