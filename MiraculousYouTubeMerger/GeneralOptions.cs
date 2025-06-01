using System.Globalization;
using System.Text.Json.Serialization;
using MiraculousYouTubeMerger.Models;

namespace MiraculousYouTubeMerger;

public class GeneralOptions
{
  public ShowTask[] Tasks { get; set; }
  public string Language { get; set; } = "eng"; // Default language
  public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromHours(12);

  [JsonIgnore]
  public CultureInfo LanguageInfo => new(Language);
}