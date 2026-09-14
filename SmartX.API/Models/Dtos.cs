namespace SmartX.API.Models;



// Request wrappers. ASP.NET Core model binding needs one concrete type per
// [FromBody] parameter, so multi-value requests are wrapped in a record.
public record DeploymentValidationRequest(DeploymentNode Root, string[] PathSegments);
public record MeterAggregateRequest(SmartMeterReading A, SmartMeterReading B);