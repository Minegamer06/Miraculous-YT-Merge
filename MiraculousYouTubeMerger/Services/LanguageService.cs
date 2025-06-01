using System.Globalization;
using Microsoft.Extensions.Options;

namespace MiraculousYouTubeMerger.Services;

public class LanguageService
{
  private readonly ILogger<LanguageService> _logger;
  private readonly GeneralOptions _generalOptions;
  private CultureInfo[]? _cachedCultures;

  public LanguageService(ILogger<LanguageService> logger, IOptions<GeneralOptions> generalOptions)
  {
    _logger = logger;
    _generalOptions = generalOptions.Value;
  }

  public CultureInfo? GetLanguageFromText(string name)
  {
    _logger.LogTrace("Searching for language in name: {Name}", name); // Changed to Trace
    if (_cachedCultures is null || _cachedCultures.Length == 0)
    {
      _cachedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
      _logger.LogDebug("Cached cultures initialized with {Count} cultures", _cachedCultures.Length);
    }
    
    var lang = _cachedCultures.FirstOrDefault(culture =>
      culture.ThreeLetterISOLanguageName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
      culture.TwoLetterISOLanguageName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
      culture.EnglishName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
      culture.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
      culture.NativeName.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (lang is not null) return lang;

    foreach (var word in name.Split([' ', '.', '-', '_'],
               StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      lang = _cachedCultures.FirstOrDefault(culture =>
        culture.ThreeLetterISOLanguageName.Equals(word, StringComparison.OrdinalIgnoreCase) ||
        culture.TwoLetterISOLanguageName.Equals(word, StringComparison.OrdinalIgnoreCase) ||
        culture.EnglishName.Equals(word, StringComparison.OrdinalIgnoreCase) ||
        culture.Name.Equals(word, StringComparison.OrdinalIgnoreCase) ||
        culture.NativeName.Equals(word, StringComparison.OrdinalIgnoreCase));
      if (lang is not null) return lang;
    }

    _logger.LogDebug("Language not found for: {Name}", name); // Changed to Debug
    return null;
  }

  public string GetDisplayName(CultureInfo culture)
  {
    var currentCulture = CultureInfo.CurrentUICulture;
    try
    {
      CultureInfo.CurrentUICulture = _generalOptions.LanguageInfo;
      return culture.DisplayName;
    }
    finally
    {
      CultureInfo.CurrentUICulture = currentCulture;
      _logger.LogTrace("Restored CurrentUICulture to: {Culture}", currentCulture.Name); 
    }
  }
}