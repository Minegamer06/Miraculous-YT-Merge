using Microsoft.Extensions.Options;
using MiraculousYouTubeMerger;
using MiraculousYouTubeMerger.Services;

var builder = WebApplication.CreateBuilder(args);
const string configPath = "/app/appsettings.json";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration
  .AddJsonFile(configPath, true, true)
  .AddEnvironmentVariables();
builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection("Processing"));
builder.Services.Configure<GeneralOptions>(builder.Configuration.GetSection("General"));
// Register your video processing service as a Singleton
builder.Services.AddSingleton<LanguageService>();
builder.Services.AddSingleton<VideoProcessingService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true) // For now, always enable Swagger
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

// Endpunkt-spezifischer Logger wird direkt injiziert
app.MapPost("/api/processing/start", (VideoProcessingService service, ILogger<Program> logger) =>
  {
    if (service.Status == ProcessingStatus.Processing)
    {
      logger.LogWarning(
        "Processing start request denied: A process is already running."); // Verwendet den injizierten Logger
      return Results.Conflict(new { message = "Processing is already in progress." });
    }

    _ = service.StartProcessingAsync(); // Startet die Verarbeitung asynchron

    return Results.Accepted("/api/processing/status", new { messeage = "Processing started." });
  })
  .WithName("StartProcessing")
  .WithTags("Processing")
  .Produces(StatusCodes.Status202Accepted)
  .Produces(StatusCodes.Status409Conflict);

// Endpunkt-spezifischer Logger wird direkt injiziert
app.MapGet("/api/processing/status", (VideoProcessingService service, ILogger<Program> logger) =>
  {
    logger.LogInformation("GET /api/processing/status endpoint called."); // Verwendet den injizierten Logger
    return Results.Ok(new
    {
      Status = service.Status.ToString(),
      service.LastMessage,
      LastRunTime = service.LastRunTime?.ToString("o")
    });
  })
  .WithName("GetProcessingStatus")
  .WithTags("Processing")
  .Produces<object>(StatusCodes.Status200OK);

app.MapGet("api/config/general", (ILogger<Program> logger, IOptions<GeneralOptions> options) =>
  {
    logger.LogInformation("GET /api/config/general endpoint called.");
    return Results.Ok(options.Value);
  })
  .WithName("GetGeneralConfig")
  .WithTags("Configuration")
  .Produces<GeneralOptions>(StatusCodes.Status200OK);

app.MapGet("api/config/processing", (ILogger<Program> logger, IOptions<ProcessingOptions> options) =>
  {
    logger.LogInformation("GET /api/config/processing endpoint called.");
    return Results.Ok(options.Value);
  })
  .WithName("GetProcessingConfig")
  .WithTags("Configuration")
  .Produces<ProcessingOptions>(StatusCodes.Status200OK);

app.Run();