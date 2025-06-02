namespace MiraculousYouTubeMerger.Models;

public class ShowTask
{
  public string Title { get; set; }
  public string Description { get; set; }
  public int TmdbId { get; set; }
  public string SourcePath { get; set; }
  public string TargetPath { get; set; }
  public EpisodeDefinition[] ManuelMapping { get; set; } = [];
  public string[] RegexMapping { get; set; } = [];
}

public class EpisodeDefinition
{
  public required string NewTitle { get; set; }
  public string? Title { get; set; } // Used for Equals matching
  public string? Path { get; set; } // Used for Contains matching
  public int? Season { get; set; }
  public int? EpisodeNumber { get; set; }
  public string? Language { get; set; } 
  public double RemoveFrames { get; set; } = 0.0; // 720 for Intro
  public bool PatchOutro { get; set; } = false; // Default to false, needs the same FrameCount as the file to merge with
  public double SpeedMultiplier { get; set; } = 1.0; // Default to 1.0, used for speed adjustments
}