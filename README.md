# PhotoSorter

Ein Avalonia/.NET-Desktop-Werkzeug, um große Fotosammlungen per Tastatur
zügig in zwei Zielordner (rechts/links, wahlweise mit Papierkorb) zu
sortieren. Bildanzeige, Zoom, Undo, Projektdateien, RAW-Vorschau und
konfigurierbare Hotkeys sind vollständig implementiert (siehe [TODO.md](TODO.md)
für den Stand aller 18 Entwicklungsphasen).

Grundlage sind die drei Spezifikationsdokumente im Projektwurzelverzeichnis
(`PhotoSorter – Software Design Document (Version 1.0).pdf`,
`PhotoSorter – UI & UX Design.pdf`, `PhotoSorter Entwicklungsplan.pdf`).
Wo die Umsetzung bewusst davon abweicht oder sie konkretisiert, ist das in
[docs/architecture-decisions.md](docs/architecture-decisions.md) dokumentiert und begründet.

## Voraussetzungen

- .NET 8 SDK

## Bauen, testen, starten

```bash
dotnet build                              # Debug-Build aller drei Projekte
dotnet test                               # 191 Tests (xUnit)
dotnet run --project src/PhotoSorter.App  # Anwendung starten
```

Für eine Release-Konfiguration `-c Release` an `build`/`test`/`run` anhängen.

## Paketieren / Ausprobieren

Fertige, direkt startbare Pakete für Windows, macOS (Intel & Apple Silicon)
und Linux gibt es ohne eigenen Build im
[„latest"-Release](https://github.com/T3xtra/photo-sorter/releases/tag/latest)
dieses Repositories – er wird bei jedem Push auf `main` automatisch neu
gebaut (`.github/workflows/release.yml`).

Für ein lokales Paket der eigenen Plattform genügt ein Befehl:

```bash
make package          # baut ein self-contained Paket für die aktuelle Plattform nach dist/
make package-all       # baut Pakete für alle vier Plattformen (win-x64, osx-x64, osx-arm64, linux-x64)
```

`make package` führt vorher automatisch die Tests aus. Die Pakete sind
self-contained (keine .NET-Installation auf dem Zielrechner nötig) und
enthalten keine Code-Signatur, daher beim ersten Start:

- **Windows**: SmartScreen warnt bei der `.exe` – „Weitere Informationen" →
  „Trotzdem ausführen".
- **macOS**: Gatekeeper warnt beim `.app` – Rechtsklick → „Öffnen", oder
  vorab `xattr -cr PhotoSorter.app` im Terminal.
- **Linux**: `chmod +x PhotoSorter.App` falls die Ausführungsrechte beim
  Entpacken verloren gehen.

## Projektstruktur

| Projekt | Inhalt |
|---|---|
| `src/PhotoSorter.Core` | Models, Services, ViewModels – frei von Avalonia-Abhängigkeiten, daher ohne UI testbar |
| `src/PhotoSorter.App` | Avalonia-Views, Composition Root, plattformspezifische Service-Implementierungen (Dateidialoge, Papierkorb, UI-Thread-Dispatch) |
| `tests/PhotoSorter.Tests` | xUnit-Tests für Core (ViewModels, Services, Modelle) mit Fake-Testdoubles |

Architektur in Kürze: strikt MVVM (CommunityToolkit.Mvvm), Dependency
Injection über `Microsoft.Extensions.DependencyInjection`, jede Service-
Abhängigkeit hinter einem Interface. Details und die Begründung jeder
nicht-trivialen Entscheidung stehen in
[docs/architecture-decisions.md](docs/architecture-decisions.md).

## Konfiguration & Daten

Einstellungen, die automatische Sitzungswiederherstellung und die Logs
liegen plattformabhängig unter dem Anwendungsdatenverzeichnis (z. B. macOS:
`~/Library/Application Support/PhotoSorter/`, Windows: `%AppData%/PhotoSorter/`).

## Bekannte Einschränkungen

- RAW-Vorschau basiert auf dem eingebetteten JPEG-Preview (kein volles
  Demosaicing). Für die Formate CR3 und RW2 kann diese Vorschau mit der
  verwendeten Bibliothek nicht extrahiert werden (Dateien werden erkannt,
  aber ohne Bildvorschau angezeigt) – siehe
  [docs/architecture-decisions.md](docs/architecture-decisions.md), Punkt 22.
- Auf Plattformen ohne systemeigenen Papierkorb-Mechanismus (z. B. Linux
  ohne Desktop-Trash-Helper) löscht "Papierkorb" endgültig statt in einen
  Papierkorb zu verschieben – siehe Punkt 18.
