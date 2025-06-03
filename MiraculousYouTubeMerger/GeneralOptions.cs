using System.Globalization;
using System.Text.Json.Serialization;
using MiraculousYouTubeMerger.Models;

namespace MiraculousYouTubeMerger;

public class GeneralOptions
{
  public string[] AllowedExtensions { get; set; } = [ "mkv", "mp4", "webm" ];
  public ShowTask[] Tasks { get; set; } = [];
  public string Language { get; set; } = "eng"; // Default language
  public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromHours(12);
  public string TMDbApiKey { get; set; } = string.Empty;

  [JsonIgnore]
  public CultureInfo LanguageInfo => new(Language);
}

