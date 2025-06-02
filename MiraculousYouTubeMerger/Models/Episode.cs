using System.Globalization;

namespace MiraculousYouTubeMerger.Models;

public class Episode
{
  public required string Title { get; set; }
  public required string Path { get; set; }
  public int Season { get; set; }
  public int EpisodeNumber { get; set; }
  public CultureInfo? Language { get; set; }
  public EpisodeDefinition? ManualMapping { get; set; }
}