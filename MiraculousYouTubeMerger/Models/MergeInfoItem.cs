using FFMpegCore;

namespace MiraculousYouTubeMerger.Models;

public class MergeInfoItem
{
  public int Index { get; set; }
  public required Episode Episode { get; set; }
  public required IMediaAnalysis Analysis { get; set; }
  // Helper properties for accessing Some Data Easily
  public VideoStream Video => Analysis.VideoStreams.First();
  public double FrameRate => Video.FrameRate;
  public TimeSpan SourceDuration => Video.Duration; // Adjust duration by speed multiplier
  public double SourceFrameCount => FrameRate * SourceDuration.TotalSeconds;
  
  
  // Start cut properties
  public double StartFrameCutCount { get; set; }
  public TimeSpan StartCutDuration =>
    TimeSpan.FromSeconds(StartFrameCutCount / FrameRate);
  
  // End cut properties
  public double EndFrameCutCount { get; set; } // Frames to patch at the end, used for outro patching
  public TimeSpan EndCutDuration => TimeSpan.FromSeconds(EndFrameCutCount / FrameRate);
  public TimeSpan? DurationToEnd => EndFrameCutCount > 0 ? SourceDuration - EndCutDuration : null;

  // Total cut properties
  public double TotalFrameCutCount => StartFrameCutCount + EndFrameCutCount; // Total frames to remove
  public TimeSpan TotalCutDuration => TimeSpan.FromSeconds(TotalFrameCutCount / FrameRate);
  public double OutputFrameCount => SourceFrameCount - TotalFrameCutCount;
  public TimeSpan OutputDuration => SourceDuration - TotalCutDuration;
  private double? _speedMultiplier;
  public void SetFrameIgnore(MergeInfoItem mainVideo, double startFrameCutCount = 0, double speedMultiplier = 1.0)
  { 
    _speedMultiplier = speedMultiplier;
    StartFrameCutCount = startFrameCutCount * speedMultiplier;
    EndFrameCutCount = 0;
    var diff = (SourceFrameCount - StartFrameCutCount) - (mainVideo.OutputFrameCount * speedMultiplier);
    EndFrameCutCount = diff;
  }

  public double GetSpeedMultiplier(MergeInfoItem mainVideo)
  {
    if (_speedMultiplier.HasValue)
      return _speedMultiplier.Value;

    return OutputDuration.TotalSeconds / mainVideo.OutputDuration.TotalSeconds;
  }

  public TimeSpan GetDurationDiff(MergeInfoItem mainVideo)
  {
    // if (_speedMultiplier.HasValue)
    // {
    //   var mainDur = mainVideo.OutputDuration * _speedMultiplier.Value;
    //   return mainDur > OutputDuration
    //     ? mainDur - OutputDuration
    //     : OutputDuration - mainDur;
    // }
    //
    return mainVideo.OutputDuration > OutputDuration
      ? mainVideo.OutputDuration - OutputDuration
      : OutputDuration - mainVideo.OutputDuration;
  }
}