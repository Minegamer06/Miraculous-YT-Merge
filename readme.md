# Projektbeschreibung

Dieses Projekt kann mehere Versisonen (Es gibt bei Miraculous für viele Sprachen uploads auf YT) von Serien wie 
"Miraculous 
Ladybug" in eine einzige Datei 
zusammenführen. Das mergen Versucht darauf zu achten das die Videos teilweise Intros enthalten, aber in andern 
Uploads für andere Sprachen nicht. Das die erkennung der Sprache funktioniert mussen die Videos entweder in Ordner 
mit dem Sprachenamen abgelegt werden (der Ordnder muss denn Namen nur enthalten, werden Sachen wie de, (Deutsch), 
Französisch, eng, English erkannt).

## Docker Compose Beispiel

Verwende das folgende `docker-compose`‑Setup zum Starten des Projekts:

```yaml
services:
  miraculous-yt-merger:
    image: miraculous-yt-merger
    build:
      context: .
      dockerfile: MiraculousYouTubeMerger/Dockerfile
    ports:
      - "8080:8080" # Port für die API (HTTP)
      - "8081:8081" # Port für die API (HTTPS)
    environment:
      GENERAL__LANGUAGE: "deu" # Sprache die für die Dateinamen und Spuranzeigename verwendet werden soll
      GENERAL__TMDBAPIKEY: "DEIN_API_KEY" # Der API-KEY für das Mapping mit TMDb
    volumes:
      - "./config:/mnt/config"
      - "./Source:/mnt/Source/Miraculous"
      - "./Target:/mnt/Target/Miraculous"
```

## Environment Variablen

| Variable              | Beschreibung                                               | Beispielwert         |
|-----------------------|------------------------------------------------------------|----------------------|
| GENERAL\_\_LANGUAGE   | Sprache für Metadaten und Dateinamen                       | eng                  |
| GENERAL\_\_TMDBAPIKEY | API Key für den Zugriff auf TMDb                           | DEIN\_TMDB\_API\_KEY |
| GENERAL\_\_ProcessingInterval | Intervall in dem das Processing auomatisch gestartetd wird | 12:00:00 // 12h      |

## Appsettings\.json Beispiel

Die Anwendung kann über eine externe Konfigurationsdatei gesteuert werden. Nutze zum Beispiel folgende Datei. Alles 
aus General ist Optinal, da es bereits vordefiniert ist. Die Configuartions datei muss unter 
`/mnt/config/appsettings.json` abgelegt werden::

```json
{
  // Das setzen der Loggin Konfiguartion ist optional.
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  // AllowedHosts ist optional, da es bereits vordefiniert ist.
  "AllowedHosts": "*", 
  "General": {
    // Unterstützte Dateiendungen für die Videoverarbeitung, Optional, da es bereits vordefiniert ist.
    "AllowedExtensions": [ "mkv", "mp4", "webm" ],
    // Optional, kann auch über Umgebungsvariablen gesetzt werden. Standardmäßig ist "eng" gesetzt.
    "Language": "deu",
    // Muss hier oder über Umgebungsvariablen gesetzt werden.
    "TMDbApiKey": "DEIN_API_KEY",
    // Hier können die Tasks konfiguriert werden, die verarbeitet werden sollen. Für eine Task muss SourcePath und 
    TargetPath gesetzt sein. Auserdem muss RegexMapping und/oder ManuelMapping gesetzt sein. so dass die Verschieden 
    versionen einer Folge gemappt werden können.
    "Tasks": [
      {
        "Title": "Miraculous Serie",
        "Description": "Task für die Verarbeitung von Miraculous Videos",
        "TmdbId": 65334,
        "SourcePath": "/mnt/Source/Miraculous",
        "TargetPath": "/mnt/Target/Miraculous",
        // Manuelle Zuordunungen für Episoden, die nicht automatisch erkannt werden können. 
        // Oder für Episode welche ein Intro enthalt welches nur in einer Version der Episode vorhanden ist. 
        // Und nicht automatisch erkannt werden kann.
        "ManuelMapping": [
          {
            "NewTitle": "The Mime",
            "Path": "THE MIME",
            "Season": 1,
            "EpisodeNumber": 19,
            "RemoveFrames": 720,
            "PatchOutro": true,
            "SpeedMultiplier": 1.041666666666667
          },
          {
            "NewTitle": "Princess Fragrance",
            "Path": "PRINCESS FRAGRANCE",
            "Season": 1,
            "EpisodeNumber": 4,
            "RemoveFrames": 720,
            "PatchOutro": true,
            "SpeedMultiplier": 1.041666666666667
          }
        ],
        "RegexMapping": [
          ".+\\🐞\\ (?<name>.+?)\\ 🐾.*?(Staffel|Season)\\ (?<season>\\d+).*?(Episode|Folge)\\ (?<episode>\\d+)",
          ".+\\🐞\\ (?<name>.+?)\\ 🐾.*?(?<season>\\d+).*?(?<episode>\\d+)"
        ]
      }
    ]
  }
}
```

## Nutzung

Die Anwendung wird über Docker gestartet. Konfiguriere die Umgebungsvariablen entweder im `docker-compose`‑File oder in der Datei `./config/appsettings.json`, die in den Container gemountet wird. Zur Steuerung stehen folgende Endpunkte zur Verfügung:

- **POST /api/processing/start**: Startet die Videoverarbeitung. // Die Verarbeitung wird auch alle 12 Stunden 
  automatisch gestartet.
- **GET /api/processing/status**: Liefert den aktuellen Verarbeitungsstatus.
- **GET /api/config/general**: Gibt die allgemeine Konfiguration zurück.

Folge diesen Schritten:
1. Passe die Umgebungsvariablen im `docker-compose`‑File an.
2. Erstelle bzw. passe die Datei `./config/appsettings.json` an.
3. Starte die Anwendung mit dem Befehl: `docker-compose up --build`.