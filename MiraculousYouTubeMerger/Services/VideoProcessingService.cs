using FFMpegCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.RegularExpressions;
using MiraculousYouTubeMerger.Models; // Your models namespace

namespace MiraculousYouTubeMerger.Services
{
  public enum ProcessingStatus
  {
    Idle,
    Processing,
    Completed,
    Error
  }

  public class VideoProcessingService
  {
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly GeneralOptions _generalOptions;
    private readonly ProcessingOptions _processingOptions;
    private readonly LanguageService _languageService;
    private static readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public ProcessingStatus Status { get; private set; } = ProcessingStatus.Idle;
    public string? LastMessage { get; private set; } = "Service has not run yet.";
    public DateTime? LastRunTime { get; private set; } = null;


    public VideoProcessingService(
      ILogger<VideoProcessingService> logger,
      IOptions<GeneralOptions> generalOptions,
      IOptions<ProcessingOptions> processingOptions, LanguageService languageService)
    {
      _logger = logger;
      _languageService = languageService;
      _generalOptions = generalOptions.Value;
      _processingOptions = processingOptions.Value;
    }

    public async Task<bool> StartProcessingAsync()
    {
      if (!await _processingSemaphore.WaitAsync(0)) // Try to acquire semaphore without waiting
      {
        _logger.LogWarning("Processing is already in progress.");
        LastMessage = "Processing is already in progress.";
        return false; // Already processing
      }

      try
      {
        Status = ProcessingStatus.Processing;
        LastMessage = "Processing started...";
        _logger.LogInformation("Starting video processing task...");
        foreach (var task in _generalOptions.Tasks)
        {
          // Wenn die Quelle nicht existiert können wir die Verarbeitung überspringen
          if (!Directory.Exists(task.SourcePath))
          {
            _logger.LogWarning($"Task {task.SourcePath} does not exist. Skipping processing for this task.");
            LastMessage = $"Task {task.SourcePath} does not exist. Skipping processing for this task.";
            continue;
          }
          // Wenn das Zielverzeichnis nicht existiert, erstellen wir es
          if (!Directory.Exists(task.TargetPath))
          {
            _logger.LogInformation($"Target directory {task.TargetPath} does not exist. Creating it.");
            Directory.CreateDirectory(task.TargetPath);
          }
          await ProcessVideosInternal(task);
        }

        Status = ProcessingStatus.Completed;
        LastMessage = "Processing completed successfully.";
        _logger.LogInformation("Video processing task finished.");
        LastRunTime = DateTime.UtcNow;
        return true;
      }
      catch (Exception ex)
      {
        Status = ProcessingStatus.Error;
        LastMessage = $"Error during processing: {ex.Message}";
        _logger.LogError(ex, "Error during video processing task.");
        LastRunTime = DateTime.UtcNow;
        return false;
      }
      finally
      {
        _processingSemaphore.Release();
      }
    }

    private async Task ProcessVideosInternal(ShowTask task)
    {
      var episodes = GetEpisodes(task);
      _logger.LogInformation("Found {Count} unique episode(s) to process.", episodes.Count);

      if (episodes.Count == 0)
      {
        _logger.LogInformation("No episodes found to process based on current configuration and source files.");
        LastMessage = "No episodes found to process.";
        return;
      }

      var orderedEpisodes = episodes
        .OrderBy(x => x.Key.Season)
        .ThenBy(x => x.Key.EpisodeNumber)
        .ToDictionary(x => x.Key, x => x.Value);

      int progress = 0;
      foreach (var episodeEntry in orderedEpisodes)
      {
        progress++;
        var episodeKey = episodeEntry.Key;
        _logger.LogInformation("[{progress}/{count}] Processing {Title} (Season {Season}, Episode {EpisodeNumber})...",
          progress, orderedEpisodes.Count, episodeKey.Title, episodeKey.Season, episodeKey.EpisodeNumber);

        var targetDir = Path.Combine(task.TargetPath, $"Staffel {episodeKey.Season}");
        var targetFile = Path.Combine(targetDir,
          $"S{episodeKey.Season:D2}E{episodeKey.EpisodeNumber:D2} - {episodeKey.Title}.mkv");

        if (!Directory.Exists(targetDir))
        {
          Directory.CreateDirectory(targetDir);
          _logger.LogDebug("Target directory created: {targetDir}", targetDir);
        }

        if (episodeEntry.Value.Count < 2)
        {
          _logger.LogWarning(
            "Not enough video files for {Title} (Season {Season}, Episode {EpisodeNumber}). Minimum 2 files required. Found {FileCount}.",
            episodeKey.Title, episodeKey.Season, episodeKey.EpisodeNumber, episodeEntry.Value.Count);
          continue;
        }

        if (File.Exists(targetFile))
        {
          _logger.LogInformation("File already exists, skipping: {targetFile}", targetFile);
          continue;
        }

        try
        {
          await ProcessSingleEpisodeAsync(episodeEntry, targetFile);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error processing episode: {Title} (S{Season}E{EpisodeNumber})",
            episodeKey.Title, episodeKey.Season, episodeKey.EpisodeNumber);
          // Continue with the next episode
        }
      }

      _logger.LogInformation("Finished processing all episodes.");
    }
    
    private Episode? GetEpisodeInfoByName(string name)
    {
      // Manual Mapping
      foreach (var map in _processingOptions.ManuelMapping)
      {
        bool titleMatch = !string.IsNullOrEmpty(map.Title) &&
                          name.Equals(map.Title, StringComparison.InvariantCultureIgnoreCase);
        // In ManuelMapping, 'Path' is used as a distinctive part of the filename to match, not the full path.
        bool pathKeywordMatch = !string.IsNullOrEmpty(map.Path) &&
                                name.Contains(map.Path, StringComparison.InvariantCultureIgnoreCase);

        if (titleMatch || pathKeywordMatch)
        {
          CultureInfo? lang = null;
          if (!string.IsNullOrEmpty(map.Language))
          {
            try
            {
              lang = new CultureInfo(map.Language);
            }
            catch (CultureNotFoundException)
            {
              _logger.LogWarning("Invalid language code in manual mapping: {Language}", map.Language);
            }
          }

          return new Episode
          {
            Title = !string.IsNullOrEmpty(map.Title)
              ? map.Title
              : "Manually Mapped Episode", // Provide a default if title is null
            Season = map.Season,
            EpisodeNumber = map.EpisodeNumber,
            Language = lang, // Path will be set when file is found
            Path = string.Empty // Placeholder, will be set later
          };
        }
      }

      // Regex Mapping
      foreach (var pattern in _processingOptions.RegexMapping)
      {
        var match = Regex.Match(name, pattern);
        if (match.Success)
        {
          return new Episode
          {
            Title = match.Groups["name"].Value.Trim(),
            Season = int.Parse(match.Groups["season"].Value),
            EpisodeNumber = int.Parse(match.Groups["episode"].Value),
            Path = string.Empty // Placeholder
          };
        }
      }

      return null;
    }

    private Dictionary<EpisodeInfo, List<Episode>> GetEpisodes(ShowTask task)
    {
      DirectoryInfo directory = new(task.SourcePath);
      var episodes = new Dictionary<EpisodeInfo, List<Episode>>();

      if (!directory.Exists)
      {
        _logger.LogWarning("Source directory does not exist: {SourcePath}", task.SourcePath);
        return episodes; // Return empty if source doesn't exist
      }

      foreach (var folder in directory.EnumerateDirectories())
      {
        CultureInfo folderLang = _languageService.GetLanguageFromText(folder.Name) ?? _generalOptions.LanguageInfo;

        foreach (var file in folder.EnumerateFiles())
        {
          if (!_processingOptions.AllowedExtensions.Contains(Path.GetExtension(file.Name).TrimStart('.'),
                StringComparer.OrdinalIgnoreCase))
            continue;

          var episodeDetails = GetEpisodeInfoByName(file.Name);
          if (episodeDetails == null)
          {
            _logger.LogDebug("No episode pattern matched for file {file}", file.Name); // Changed to Debug
            continue;
          }

          // Determine language: file > folder > general default
          CultureInfo fileLang = _languageService.GetLanguageFromText(Path.GetFileNameWithoutExtension(file.Name)) ?? folderLang;
          episodeDetails.Language ??= fileLang;
          episodeDetails.Path = file.FullName;

          var episodeInfoKey = new EpisodeInfo()
          {
            Title = episodeDetails.Title,
            Season = episodeDetails.Season,
            EpisodeNumber = episodeDetails.EpisodeNumber
          };

          if (!episodes.TryGetValue(episodeInfoKey, out var episodeList))
          {
            episodeList = [];
            episodes.Add(episodeInfoKey, episodeList);
          }

          episodeList.Add(episodeDetails);

          _logger.LogDebug(
            "Found: {Title} (Season {Season}, Episode {EpisodeNumber}) in {Path} with language {Language}",
            episodeInfoKey.Title, episodeInfoKey.Season, episodeInfoKey.EpisodeNumber, file.FullName,
            episodeDetails.Language?.DisplayName ?? "Unknown");
        }
      }

      return episodes;
    }
    private async Task ProcessSingleEpisodeAsync(KeyValuePair<EpisodeInfo, List<Episode>> episodeData,
      string targetPath)
    {
      var episodeKey = episodeData.Key;
      var episodeFiles = episodeData.Value;
      List<MergeInfoItem> videos = [];
      foreach (var e in episodeFiles)
      {
        var videoInfo = await FFProbe.AnalyseAsync(e.Path);
        videos.Add(
          new MergeInfoItem()
          {
            Analysis = videoInfo, 
            Episode = e
          });
      }

      videos = videos.OrderBy(x => x.FrameCount).ThenBy(x => x.Duration).ToList();
      for (var i = 0; i < videos.Count; i++)
        videos[i].Index = i;

      var mainVideo = videos.First();
      foreach (var video in videos.Skip(1))
      {
        double frameDelta = Math.Abs(mainVideo.FrameCount - video.FrameCount);
        double durationDelta = Math.Abs((video.Duration - mainVideo.Duration).TotalSeconds);
        if (durationDelta < 0.5)
        {
          _logger.LogDebug("Videos scheinen synchron zu sein: {MainVideo} vs {Video}, auch ohne FPS-Anpassung.",
            mainVideo.Episode.Title, video.Episode.Title);
        }
        else if (Math.Abs(frameDelta - 720) < 2)
        {
          _logger.LogWarning(
            "Frame count mismatch for {MainVideo} vs {Video}, aber nur {FrameDelta} Frames Unterschied.",
            mainVideo.Episode.Title, video.Episode.Title, frameDelta);
          _logger.LogWarning("Warscheinlich hat Video '{Video}' das Intro enthalten",
            Path.GetFileName(video.FrameCount > mainVideo.FrameCount
              ? video.Episode.Path
              : mainVideo.Episode.Path));
          _logger.LogWarning("Video '{Video}' muss angepasst werden.",
            Path.GetFileName(video.FrameCount > mainVideo.FrameCount
              ? "Aktuelles Video"
              : "Hauptvideo"));
          if (video.FrameCount > mainVideo.FrameCount)
          {
            video.FrameRemove = frameDelta;
            _logger.LogDebug(
              "Gute Nachricht, das Intro kann Problemlos entfernt werden, da das Hauptvideo kürzer ist.");
          }
          else if (episodeFiles.Count < 3)
          {
            mainVideo.FrameRemove = frameDelta;
            // Extensions.WriteSuccessLine("Gute Nachricht, das Intro kann Problemlos entfernt werden, da es nur 2 Videos gibt.");
            _logger.LogError(
              "Leider kann das Hauptvideo nicht angepasst werden, da es momentan noch nicht unterstützt wird.");
            return;
          }
          else // Wenn es mehr als 2 Videos gibt, dürfen wir das Hauptvideo nicht anpassen.
          {
            _logger.LogError(
              "Bitte entfernen Sie das Intro manuell aus dem Hauptvideo, und gff. möglichen weiteren: {MainVideo}",
              mainVideo.Episode.Path);
            return;
          }
        }
        else if (video.FrameCount > mainVideo.FrameCount + 10 || video.FrameCount < mainVideo.FrameCount - 10)
        {
          _logger.LogError(
            "Die Videos '{mainVideo}' und '{currentVideo}' (S {season}, E {episode}) scheinen nicht synchron zu sein, und können nicht zusammengeführt werden." +
            "\n Die Differenz beträgt {frameDelta} Frames, das sind ca. {durationDelta} Sekunden.",
            mainVideo.Episode.Title, video.Episode.Title, video.Episode.Season, video.Episode.EpisodeNumber, frameDelta,
            (frameDelta / mainVideo.Analysis.VideoStreams[0].FrameRate));
          return;
        }
      }


      var args = FFMpegArguments
        .FromFileInput(mainVideo.Episode.Path);
      foreach (var e in videos.Skip(1))
      {
        args.AddFileInput(e.Episode.Path);
      }

      await args.OutputToFile(targetPath, true, opts =>
      {
        var filterSpecs = new List<string>();
        for (var i = 0; i < videos.Count; i++)
        {
          var e = videos[i];
          var diff = Math.Abs((e.Duration - mainVideo.Duration).TotalSeconds);
          var aTempo = (e.Duration.TotalSeconds / mainVideo.Duration.TotalSeconds);
          var secondsToSkip = e.FrameRemove / e.Analysis.VideoStreams.First().FrameRate;
          string filterStart = $"[{i}:a]";
          string filterEnd = $"[a{i}]";
          List<string> filterCurrent = [];
          if (secondsToSkip > 0)
          {
            filterCurrent.Add($"atrim=start={secondsToSkip.ToString(CultureInfo.InvariantCulture)}");
            filterCurrent.Add("asetpts=PTS-STARTPTS");
          }

          if (diff > 0.5)
          {
            // Label für jede gefilterte Spur erzeugen
            filterCurrent.Add($"atempo={aTempo.ToString(CultureInfo.InvariantCulture)}");
          }

          if (filterCurrent.Count > 0)
            filterSpecs.Add($"{filterStart}{string.Join(",", filterCurrent)}{filterEnd}");
        }

        var filterComplex = string.Join(";", filterSpecs);
        if (!string.IsNullOrEmpty(filterComplex))
          opts.WithCustomArgument($"-filter_complex \"{filterComplex}\"");

        opts.WithCustomArgument("-map 0:v:0");
        foreach (var e in videos)
        {
          var diff = Math.Abs((e.Duration - mainVideo.Duration).TotalSeconds);
          if (diff > 0.5)
            opts.WithCustomArgument($"-map [a{e.Index}]");
          else
            opts.WithCustomArgument($"-map {e.Index}:a:0");
        }

        opts.WithVideoCodec("copy"); // Video 1:1 kopieren
        foreach (var e in videos)
        {
          var difference = Math.Abs((e.Duration - mainVideo.Duration).TotalSeconds);
          opts.WithCustomArgument(difference <= 0.5
            ? $"-c:a:{e.Index} copy"
            : $"-c:a:{e.Index} aac");
        }

        foreach (var e in videos)
        {
          opts.WithCustomArgument($"-disposition:a:{e.Index} -default");
        }

        foreach (var e in videos)
        {
          string displayName = _languageService.GetDisplayName(e.Episode.Language!);
          _logger.LogTrace(
            "Setze Metadaten für Audio-Spur: {Title} (S {Season}, E {EpisodeNumber}) für Sprache {Language}",
            e.Episode.Title, e.Episode.Season, e.Episode.EpisodeNumber, displayName);
          opts.WithCustomArgument($"-metadata:s:a:{e.Index} title={displayName}")
            .WithCustomArgument($"-metadata:s:a:{e.Index} language={e.Episode.Language!.TwoLetterISOLanguageName}");
        }
      }).ProcessAsynchronously();

      _logger.LogInformation("✅ Datei erstellt: {targetPath}", targetPath);
    }
  }
}