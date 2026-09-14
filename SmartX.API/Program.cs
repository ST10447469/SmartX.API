
using SmartX.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registered as singletons: TelemetryStore holds the in-memory readings,
// DeploymentValidator wraps the recursive path-validation algorithm.
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddSingleton<DeploymentValidator>();

var app = builder.Build();

// Seed mock telemetry 
app.Services.GetRequiredService<TelemetryStore>().SeedMockData();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// MVC views (landing page + dashboard)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Attribute-routed API controllers 
app.MapControllers();

app.Run();

