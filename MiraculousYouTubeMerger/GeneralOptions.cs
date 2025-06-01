using System.Globalization;
using System.Text.Json.Serialization;

namespace MiraculousYouTubeMerger;

public class GeneralOptions
{
  public string BasePathSource { get; set; } = "./Source";
  public string BasePathTarget { get; set; } = "./Destination";
  public string Language { get; set; } = "eng"; // Default language
  public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromHours(12);

  [JsonIgnore]
  public CultureInfo LanguageInfo => new(Language);
}