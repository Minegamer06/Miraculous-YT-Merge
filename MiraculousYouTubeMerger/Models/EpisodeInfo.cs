namespace MiraculousYouTubeMerger.Models;

public class EpisodeInfo
{
  public required string Title { get; set; }
  public int Season { get; set; }
  public int EpisodeNumber { get; set; }

  public override bool Equals(object? obj)
  {
    if (obj is not EpisodeInfo episodeInfo) return false;
    return Season.Equals(episodeInfo.Season) &&
           EpisodeNumber.Equals(episodeInfo.EpisodeNumber);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(Season, EpisodeNumber); 
  }
}