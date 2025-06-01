using Microsoft.Extensions.Options;

namespace MiraculousYouTubeMerger.Services;

public class VideoProcessingBackgroundService : BackgroundService
{
  private readonly VideoProcessingService _videoService;
  private readonly ILogger<VideoProcessingBackgroundService> _logger;
  private readonly PeriodicTimer _timer;

  public VideoProcessingBackgroundService(VideoProcessingService videoService, ILogger<VideoProcessingBackgroundService> logger, IOptions<GeneralOptions> options)
  {
    _videoService = videoService;
    _logger = logger;
    _timer = new PeriodicTimer(options.Value.ProcessingInterval);
    _logger.LogTrace("VideoProcessingBackgroundService initialized");
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("VideoProcessingBackgroundService is starting with interval: {Interval}", _timer.Period);
    await Process();
    while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
    {
      _logger.LogDebug("VideoProcessingBackgroundService is running at: {time}", DateTimeOffset.Now);
      await Process(); // Start the processing method
    }
  }

  private async Task Process()
  {
    if (_videoService.Status == ProcessingStatus.Processing)
    {
      _logger.LogInformation(
        "Processing start request denied: A process is already running."); // Verwendet den injizierten Logger
      return;
    }
    await _videoService.StartProcessingAsync(); // Startet die Verarbeitung asynchron
  }
}