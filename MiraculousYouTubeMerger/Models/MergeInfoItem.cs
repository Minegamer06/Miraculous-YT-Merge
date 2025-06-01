using FFMpegCore;

namespace MiraculousYouTubeMerger.Models;

public class MergeInfoItem
{
  public required Episode Episode { get; set; }
  public required IMediaAnalysis Analysis { get; set; }
  public int Index { get; set; }
  public double FrameRemove { get; set; } // Frames to remove from the beginning

  public TimeSpan DurationRemove =>
    TimeSpan.FromSeconds(FrameRemove / (Analysis.VideoStreams.FirstOrDefault()?.FrameRate ?? 1)); // Avoid division by zero

  // Effective frame count after removal
  public double FrameCount
  {
    get
    {
      var videoStream = Analysis.VideoStreams.FirstOrDefault();
      if (videoStream == null) return -1;
      // Ensure original frame count is calculated correctly based on reported duration and framerate
      double originalTotalFrames = videoStream.FrameRate * videoStream.Duration.TotalSeconds;
      return originalTotalFrames - FrameRemove;
    }
  }
        
  // Effective duration after removal
  public TimeSpan Duration => Analysis.Duration - DurationRemove;
}