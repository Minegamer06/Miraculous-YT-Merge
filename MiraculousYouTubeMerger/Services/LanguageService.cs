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

  public CultureInfo? GetLanguageFromText(string name, bool secure = true)
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

    var languageStrings = ExtractLanguageStrings(name, secure);
    foreach (var word in languageStrings)
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

  private List<string> ExtractLanguageStrings(string name, bool secure = true)
  {
    List<string> results = [];
    string pattern = @"(?<=\()[^)]*(?=\))|(?<=\[)[^\]]*(?=\])";
    var matches = System.Text.RegularExpressions.Regex.Matches(name, pattern);
    foreach (var match in matches.Where(match => match.Success))
    {
      if (!results.Contains(match.Value.Trim()))
        results.Add(match.Value.Trim());
    }

    var ext = name.Split('.');
    for (int i = ext.Length - 1; i >= 0; i--)
    {
      var extName = ext[i].Trim();
      if (extName.Length == 2 || extName.Length == 3)
      {
        if (!results.Contains(extName))
          results.Add(extName);
      }
      else if (extName.Length > 3)
        break;
    }

    foreach (var word in name.Split([' ', '.', '-', '_'],
               StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      if (secure && word.Length < 4) continue; // Skip short words if secure
      if (results.Contains(word)) continue;
      results.Add(word);
    }

    return results;
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