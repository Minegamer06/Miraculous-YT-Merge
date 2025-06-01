namespace MiraculousYouTubeMerger;

public class ProcessingOptions
{
  public string[] AllowedExtensions { get; set; } = [];
  public EpisodeDefinition[] ManuelMapping { get; set; } = [];
  public string[] RegexMapping { get; set; } = [];
}

public class EpisodeDefinition
{
  public string? Title { get; set; } // Used for Equals matching
  public string? Path { get; set; } // Used for Contains matching
  public int Season { get; set; }
  public int EpisodeNumber { get; set; }
  public string? Language { get; set; } 
}