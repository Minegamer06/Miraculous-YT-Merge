using System.Globalization;
using FuzzySharp;
using Microsoft.Extensions.Options;
using MiraculousYouTubeMerger.Models;
using TMDbLib.Client;
using TMDbLib.Objects.Search;

namespace MiraculousYouTubeMerger.Services;

public class YouTubeTMDbMapperService
{
  private readonly ILogger<YouTubeTMDbMapperService> _logger;
  private readonly GeneralOptions _generalOptions;
  private readonly TMDbClient _tmdbClient;
  
  public YouTubeTMDbMapperService(ILogger<YouTubeTMDbMapperService> logger, IOptions<GeneralOptions> generalOptions)
  {
    _logger = logger;
    _generalOptions = generalOptions.Value;
    _tmdbClient = new TMDbClient(_generalOptions.TMDbApiKey);
  }

  public async Task Map(Dictionary<EpisodeInfo, List<Episode>> episodes, int tmdbId)
  {
    List<CultureInfo> languages = [_generalOptions.LanguageInfo];
    foreach (var item in episodes.Values.SelectMany(list => list))
    {
      if (languages.Any(x => x.TwoLetterISOLanguageName == item?.Language?.TwoLetterISOLanguageName)) continue;
      if (item?.Language is not null)
        languages.Add(item.Language);
    }
    
    List<TmdbEpisode> existingEpisodes = []; 
    var show = await _tmdbClient.GetTvShowAsync(tmdbId);
    if (show is null)
      throw new InvalidOperationException($"Show with TMDb ID {tmdbId} not found.");
    _logger.LogInformation("Mapping episodes for show: {ShowName} (TMDb ID: {TmdbId})", show.Name, tmdbId);

    foreach (var season in show.Seasons)
    {
      foreach (var language in languages)
      {
        var episodesForSeason = await _tmdbClient.GetTvSeasonAsync(tmdbId, season.SeasonNumber, language: language.TwoLetterISOLanguageName);
        if (episodesForSeason is null)
          continue;
        foreach (var episode in episodesForSeason.Episodes)
        {
          var epi = existingEpisodes.FirstOrDefault(e => e.Id == episode.Id);
          if (epi is null)
          {
            existingEpisodes.Add(new TmdbEpisode
            {
              Id = episode.Id,
              SeasonNumber = episode.SeasonNumber,
              EpisodeNumber = episode.EpisodeNumber,
              Name = episode.Name,
              Names = [episode.Name]
            });
          }
          else
          {
            epi.Names.Add(episode.Name);
          }
        }
      }
    }

    foreach (var episode in episodes)
    {
      var names = episode.Value.Select(x => x.Title);
      var match = FindClosestMatch(names, existingEpisodes);
      episode.Key.Season = match.SeasonNumber;
      episode.Key.EpisodeNumber = match.EpisodeNumber;
      episode.Key.Title = match.Name;
      foreach (var ep in episode.Value)
      {
        ep.Season = match.SeasonNumber;
        ep.EpisodeNumber = match.EpisodeNumber;
      }
    }
  }

  private TmdbEpisode FindClosestMatch(IEnumerable<string> names, List<TmdbEpisode> existingEpisodes)
  {
    var target = names.Select(n => n.ToLowerInvariant().Trim()).Distinct().ToArray();
    var best = existingEpisodes
      .MaxBy(x => GetRatioForEpisode(x, target)
        );
    return best!;
  }
  
  private int GetRatioForEpisode(TmdbEpisode episode, string[] names)
  {
    var ratio = 0;
    foreach (var name in names)
    {
      var currentRatio = episode.Names.Select(n => Fuzz.Ratio(n.ToLowerInvariant(), name)).Max();
      if (currentRatio > ratio)
      {
        ratio = currentRatio;
      }
    }

    return ratio;
  }

}

internal class TmdbEpisode
{
  public int Id { get; set; }
  public int SeasonNumber { get; set; }
  public int EpisodeNumber { get; set; }
  public string Name { get; set; } = string.Empty;
  public HashSet<string> Names { get; set; } = [];
}