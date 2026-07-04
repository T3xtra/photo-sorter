# PhotoSorter – Entwicklungsphasen

Quelle: `PhotoSorter Entwicklungsplan.pdf`. Jede Phase gilt erst als
abgeschlossen, wenn sie kompiliert, keine Compilerwarnungen erzeugt, getestet
und dokumentiert ist, bestehende Funktionen nicht beeinträchtigt und sauber in
MVVM integriert ist (Definition of Done).

- ✅ Phase 1 — Projekt aufsetzen (Avalonia, MVVM, DI, Logging, Konfiguration) → leeres Fenster
- ✅ Phase 2 — Grundlayout (Toolbar, Thumbnail-Leiste, Bildbereich, Statusleiste), noch keine Funktionen
- ✅ Phase 3 — Ordner laden (Ordner auswählen, rekursiv Bilder finden, Bildliste erzeugen)
- ✅ Phase 4 — Bildanzeige (erstes/nächstes/vorheriges Bild, keine Animationen)
- ✅ Phase 5 — Thumbnail-System (Vorschaubilder, Thumbnail-Leiste, aktuelle Auswahl markieren)
- ✅ Phase 6 — Zoom (Mausrad, Pan, Bild einpassen, 100 %)
- ✅ Phase 7 — Hotkeys (links, rechts, Undo, Vollbild, konfigurierbar)
- ✅ Phase 8 — Sortiersystem (Entscheidungen speichern, Bilder verschwinden aus Liste, Zähler aktualisieren, noch keine Dateioperationen)
- ✅ Phase 9 — Undo (beliebig viele Schritte zurück)
- ✅ Phase 10 — Projektdatei (Speichern, Laden, automatische Wiederherstellung)
- ✅ Phase 11 — Dateioperationen (Verschieben nach Bestätigung, Papierkorb, Fehlerbehandlung)
- ✅ Phase 12 — Animationen (Swipe, Thumbnail-Animation, sanfte Übergänge)
- ✅ Phase 13 — Performance (asynchrones Laden, Thumbnail-Cache, Bild-Cache, Speicheroptimierung)
- ✅ Phase 14 — RAW-Unterstützung (Preview laden, große RAW-Dateien testen)
- ✅ Phase 15 — Einstellungen (Hotkeys, Dark Mode, Animationen, Ordner merken)
- ✅ Phase 16 — Fehlerbehandlung (beschädigte Bilder, fehlende Dateien, Zugriffsfehler, Projektwiederherstellung)
- ✅ Phase 17 — Feinschliff (Icons, Tooltips, Tastenkürzel, Fenstergrößen, Animationen optimieren)
- ✅ Phase 18 — Version 1.0 (Code bereinigen, Kommentare, Dokumentation, Tests, Release)

## Phase 1 — Details

- Solution mit drei Projekten: `PhotoSorter.Core` (Models/Services/ViewModels),
  `PhotoSorter.App` (Avalonia Views + Composition Root), `PhotoSorter.Tests` (xUnit).
- Dependency Injection via `Microsoft.Extensions.DependencyInjection`,
  Registrierung zentral in `PhotoSorter.Core.DependencyInjection.ServiceCollectionExtensions`.
- Logging via `Microsoft.Extensions.Logging` + Serilog (Konsole + rollierende
  Datei unter `%AppData%/PhotoSorter/logs`).
- Konfiguration/Einstellungen: `ISettingsService` + `JsonSettingsService`,
  JSON-Datei unter `%AppData%/PhotoSorter/settings.json`; aktuell nur
  Fenstergröße, wird in späteren Phasen erweitert.
- Dark Theme fest eingestellt (siehe `docs/architecture-decisions.md`, Punkt 5).
- Ergebnis manuell verifiziert: `dotnet build` (0 Fehler, 0 Warnungen),
  `dotnet run` startet ohne Exception, Settings-Datei und Logs werden
  korrekt unter `%AppData%/PhotoSorter` angelegt.
- Tests: `JsonSettingsServiceTests` (Defaults, Round-Trip, korrupte Datei) —
  3/3 grün.

## Phase 2 — Details

- Vier neue Views + ViewModels gemäß SDD-Architekturkapitel: `ToolbarView`,
  `ThumbnailBarView`, `ImageViewerView`, `StatusBarView`, jeweils mit
  zugehörigem ViewModel in `PhotoSorter.Core.ViewModels`. `MainWindowViewModel`
  komponiert alle vier; `MainWindow.axaml` ordnet sie in einem Grid
  (Toolbar/Thumbnail-Leiste/Bildbereich/Statusleiste) an.
- Farbpalette gemäß UI-Design.md ("Dark Mode") als App-Ressourcen in
  `Styles/Colors.axaml` (Hintergrund, Toolbar, Akzent, Markierung, Text).
- Noch keine Funktionen: Toolbar-Buttons ohne `Command`-Bindung, Thumbnail-
  und Bildbereich sind leere Container, Statusleiste zeigt den
  "nichts geladen"-Zustand (0/0/0, "Kein Bild geladen").
- **Bugfix während Verifikation gefunden:** Sync-over-Async-Deadlock in
  `JsonSettingsService`, ausgelöst durch `.GetAwaiter().GetResult()` in
  `App.axaml.cs` beim Start/Beenden. Behoben mit `ConfigureAwait(false)`
  durchgängig in Core-Services (siehe `docs/architecture-decisions.md`,
  Punkt 8). Mit zwei Regressionstests abgesichert.
- Ergebnis manuell verifiziert: `dotnet build` (0 Fehler, 0 Warnungen),
  App startet sowohl ohne als auch mit vorhandener `settings.json` fehlerfrei
  (genau das Szenario, das den Deadlock ausgelöst hatte).
- Tests: 8/8 grün (3 aus Phase 1 + 3 neue StatusBarViewModel-Tests + 2
  Deadlock-Regressionstests).

## Phase 3 — Details

- Neue Modelle: `ImageFile` (Dateiname/Pfad/Größe), `Project` (Quellordner +
  Bildliste; wächst in Phase 8/10 um Entscheidungen/Persistenz).
- Neue Core-Services: `IFolderScannerService`/`FolderScannerService`
  (rekursive, asynchrone Ordnersuche über `SupportedImageFormats`,
  `IgnoreInaccessible` gegen Zugriffsfehler in Unterordnern),
  `IProjectService`/`ProjectService` (hält das aktuelle `Project`, feuert
  `ProjectChanged`).
- Zwei neue Abstraktionen für sauberes MVVM: `IFolderPickerService` (nativer
  Ordnerdialog, Implementierung in `PhotoSorter.App`) und `IUiDispatcher`
  (UI-Thread-Marshaling für ViewModels, die auf Core-Events reagieren, die von
  einem Hintergrund-Thread ausgelöst werden können) — siehe
  `docs/architecture-decisions.md`, Punkt 9.
- `ToolbarViewModel.SelectSourceFolderCommand` öffnet den Ordnerdialog (Mehrfachauswahl
  möglich, siehe SDD/UI-Design) und lädt darüber ein neues Projekt;
  `StatusBarViewModel` reagiert auf `ProjectChanged` und zeigt Bild-Position
  sowie "Offen"-Zähler an.
- `SupportedImageFormats`: zentrale Liste aller erkannten Dateiendungen
  (Pflichtformate + optional GIF), Basis für spätere RAW/HEIC-Erweiterung
  (Phase 14) ohne Änderungen an der Scan-Logik.
- Ergebnis manuell verifiziert: `dotnet build` (0 Fehler, 0 Warnungen), App
  startet fehlerfrei (komplette DI-Auflösung inkl. neuer Services erfolgreich).
- Tests: 31/31 grün (`SupportedImageFormatsTests`, `FolderScannerServiceTests`,
  `ProjectServiceTests`, `ToolbarViewModelTests`, erweiterte
  `StatusBarViewModelTests`).

## Phase 4 — Details

- `IImageDecoder`/`ImageSharpImageDecoder` (Core): dekodiert jede Pflichtform
  (inkl. TIFF) einheitlich zu PNG-Bytes via `SixLabors.ImageSharp` **3.1.x**
  (nicht 4.x — Lizenzwechsel, siehe `docs/architecture-decisions.md`, Punkt 10).
- Avalonias `Bitmap` kann nachweislich nicht ohne laufende Avalonia-App
  konstruiert werden (Spike-Test). Deshalb liefert Core nur Bytes;
  `PngBytesToBitmapConverter` (App) wandelt sie beim Binding in ein `Bitmap`.
- `IProjectService` um `CurrentIndex`, `MoveNext()`, `MovePrevious()` und
  `CurrentIndexChanged` erweitert — zentrale, geteilte Positions-Verwaltung
  statt Duplikation in einzelnen ViewModels (Punkt 11).
- `ImageViewerViewModel` lädt automatisch das erste Bild nach
  `ProjectChanged` und bietet `NextImageCommand`/`PreviousImageCommand` mit
  korrektem `CanExecute` an den Bildgrenzen.
- Interimistische Pfeiltasten-Navigation (nur Auf/Ab) direkt in
  `MainWindow.axaml.cs`, klar als Übergangslösung vor Phase 7 markiert
  (Punkt 12) — Links/Rechts bleiben für die Sortier-Aktionen (Phase 8)
  unangetastet.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei. Die
  visuelle End-to-End-Prüfung ("Quelle" klicken → Bild erscheint) konnte in
  dieser Umgebung mangels GUI-Interaktion nicht durchgeführt werden und
  sollte manuell nachgeholt werden.
- Tests: 45/45 grün, davon neu: `ImageViewerViewModelTests`,
  `ImageSharpImageDecoderTests` (echter Decode-Roundtrip für
  PNG/JPEG/BMP/TIFF, keine Fakes), erweiterte `ProjectServiceTests`
  (Navigation).

## Phase 5 — Details

- `IThumbnailGenerator`/`ImageSharpThumbnailGenerator`: Resize auf max.
  100×100 (UI-Design.md), PNG-Ausgabe wie `IImageDecoder`.
- `ThumbnailItemViewModel` pro Bild (Thumbnail-Bytes, `IsCurrent`,
  `SelectCommand` zum Springen zur jeweiligen Position); Index wird beim
  Klick dynamisch über `IProjectService.Current.Images` aufgelöst statt
  gecacht, damit spätere Entfernungen aus der Liste (Phase 8) keine
  veralteten Indizes erzeugen können.
- `ThumbnailBarViewModel` generiert Thumbnails parallel mit begrenzter
  Nebenläufigkeit (`Parallel.ForEachAsync`, `ProcessorCount / 2`) im
  Hintergrund, damit tausende Bilder das UI nicht blockieren; jede
  UI-Aktualisierung läuft über `IUiDispatcher`.
- `IProjectService` um `MoveTo(int)` erweitert (Basis für `MoveNext`/
  `MovePrevious`, jetzt auch für Klick-Navigation).
- View: `ItemsControl` mit horizontalem `StackPanel`, Orange-Highlight
  (`CurrentThumbnailBorderBrushConverter`, siehe UI-Design.md "Markierung:
  Orange") um das aktuelle Thumbnail, automatisches Scrollen zur Auswahl
  über `ContainerFromItem(...).BringIntoView()` im Code-Behind (View-Verhalten,
  keine Businesslogik).
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 50/50 grün, davon neu: `ThumbnailBarViewModelTests` (Population,
  Highlight, parallele Generierung, Klick-Navigation, Index-Sync bei
  `CurrentIndexChanged`).

## Phase 6 — Details

- `ImageViewerViewModel` um `ZoomMode` (Fit/Manual), `ZoomFactor`,
  `PanOffsetX/Y` erweitert; jedes neue Bild setzt Zoom/Pan zurück (Standard:
  vollständig sichtbar, UI-Design.md).
- Mausrad → `ApplyZoomDelta` (Zoom-Schritt 1.1, Grenzen 10 %–800 %); mittlere
  Maustaste ziehen → `SetPan`; Doppelklick → `ToggleZoomModeCommand`
  (Einpassen ↔ 100 %) — Maussteuerung exakt nach Architekturentscheidung 1.
- `ZoomModeToStretchConverter` schaltet zwischen `Stretch.Uniform`
  (Einpassen) und `Stretch.None` (100 % Basis für weiteren Zoom) um, siehe
  `docs/architecture-decisions.md`, Punkt 13.
- `MainWindowViewModel` spiegelt `ImageViewerViewModel.ZoomFactor` in
  `StatusBarViewModel.ZoomPercentage` — bewusst über den gemeinsamen
  Koordinator statt direkter ViewModel-zu-ViewModel-Kopplung. Statusleiste
  zeigt jetzt auch Zoom-Prozent und Dateiname (UI-Design.md-Beispiel).
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 61/61 grün, davon neu: `ImageViewerZoomTests` (Zoom-Grenzen,
  Pan-Guard, Toggle-Verhalten), `MainWindowViewModelTests`
  (Zoom-Prozent-Synchronisierung).

## Phase 7 — Details

- `HotkeyAction` (Enum), `HotkeyChord` (Record, Key als String statt
  `Avalonia.Input.Key` — siehe `docs/architecture-decisions.md`, Punkt 14),
  `IHotkeyService`/`HotkeyService` mit Standardbelegung exakt nach
  SoftwareDesign.md/UI-Design.md.
- `MainWindow.axaml.cs` löst jeden Tastendruck über `IHotkeyService.Resolve`
  auf und dispatcht an die passenden ViewModel-Commands; ersetzt die
  Phase-4-Interimslösung vollständig. Vollbild-Toggle (F) neu implementiert.
- `SortLeft`/`SortRight`/`Skip`/`Undo` lösen bereits korrekt auf, haben aber
  noch keine Wirkung (Phase 8/9 liefern die Befehle dahinter).
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei (inkl.
  `IHotkeyService`-Auflösung in `MainWindow`).
- Tests: 73/73 grün, davon neu: `HotkeyServiceTests` (Standardbelegung,
  Modifier-Tasten, Rebinding, Reset, Record-Formatierung).

## Phase 8 — Details

- `SortAction` (Enum), `SortDecision` (Record). `Project` verwaltet
  Entscheidungen intern (`AddDecision`/`RemoveLastDecision`/`IsDecided`),
  bleibt aber in `Images`/`SourceFolders` unveränderlich — siehe
  `docs/architecture-decisions.md`, Punkt 15.
- `IProjectService.RecordDecision(SortAction)`: speichert die Entscheidung
  für das aktuelle Bild und springt automatisch zum nächsten offenen Bild;
  `MoveNext`/`MovePrevious` überspringen bereits entschiedene Bilder;
  `HasNextImage`/`HasPreviousImage` für korrekte `CanExecute`-Zustände.
  Neues Event `DecisionsChanged` (separat von `CurrentIndexChanged`, damit
  Thumbnail-Leiste nur bei echten Entscheidungen neu filtert, nicht bei
  jeder einfachen Navigation).
- `ImageViewerViewModel`: `SortLeftCommand`/`SortRightCommand`/`SkipCommand`,
  jetzt vollständig über Hotkeys (Links/Rechts/Leertaste) erreichbar.
- `ThumbnailBarViewModel`: entfernt sortierte Bilder aus der Anzeige
  (`RemoveDecidedThumbnails`); Highlight-Logik auf Bildreferenz statt
  Listenposition umgestellt (Bugfix: nach dem Entfernen entsprach
  `Thumbnails[i]` nicht mehr `Project.Images[i]`).
- `StatusBarViewModel` zeigt jetzt echte Links-/Rechts-/Offen-Zähler statt
  Platzhalterwerten.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 89/89 grün, davon neu: `ProjectTests`, erweiterte
  `ProjectServiceTests` (Entscheidungen, Navigation überspringt Entschiedene),
  `ImageViewerSortingTests`, erweiterte `ThumbnailBarViewModelTests` und
  `StatusBarViewModelTests`.

## Phase 9 — Details

- `IProjectService.Undo()` nutzt das in Phase 8 vorbereitete
  `Project.RemoveLastDecision()`; "beliebig viele Schritte zurück" ergibt
  sich automatisch aus der Decisions-Liste (siehe
  `docs/architecture-decisions.md`, Punkt 16).
- `ImageViewerViewModel.UndoCommand` (`CanUndo` = mindestens eine
  Entscheidung vorhanden), über Backspace (konfigurierbar) und zusätzlich
  fest über Strg+Z erreichbar (SDD verlangt beide).
- Undo springt automatisch zum wiederhergestellten Bild zurück, auch wenn
  bereits alle Bilder sortiert waren (`CurrentIndex` war -1).
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 96/96 grün, davon neu: Undo-Tests in `ProjectServiceTests`
  (Einzelschritt, mehrfach in umgekehrter Reihenfolge, nach vollständigem
  Sortieren, No-op ohne Entscheidungen) und `ImageViewerSortingTests`.

## Phase 10 — Details

- Toolbar "Links"/"Rechts" jetzt funktional: `TargetFolder`-Modell
  (Papierkorb oder echter Pfad), `IProjectService.SetLeftTarget`/
  `SetRightTarget`, Flyout-Menü am "Links"-Button (Papierkorb/Ordner wählen),
  Anzeige des gewählten Ziels als Tooltip.
- `ProjectFileDto`/`TargetFolderDto`/`DecisionDto` (Core/Models/Persistence)
  als vom Domänenmodell entkoppeltes Dateiformat;
  `IProjectFileSerializer`/`JsonProjectFileSerializer` für die eigentliche
  JSON-(De-)Serialisierung.
- `IProjectService.SaveAsync`/`OpenAsync`: persistiert/lädt Quellordner,
  Reihenfolge (Bildpfade), Zielordner, Entscheidungen, aktuelle Position.
  Fehlende Bilddateien werden beim Laden übersprungen (mit Log-Warnung statt
  Absturz).
- Automatisches Speichern nach jeder Entscheidung/jedem Laden
  (`ProjectChanged`/`DecisionsChanged`) in eine feste Autosave-Datei;
  automatisches Laden beim Start, falls vorhanden — bewusst ohne
  Unterscheidung Absturz/normales Beenden (siehe
  `docs/architecture-decisions.md`, Punkt 17).
- Bugfix während der Umsetzung: `ImageViewerViewModel`/`ThumbnailBarViewModel`
  initialisierten sich nicht proaktiv aus bereits vorhandenem
  Projekt-Zustand; behoben, damit Wiederherstellung unabhängig von der
  ViewModel-Konstruktionsreihenfolge korrekt funktioniert.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei sowohl
  ohne Autosave-Datei als auch mit einer absichtlich beschädigten
  Autosave-Datei (sauberer Fallback auf leeren Zustand statt Absturz).
- Tests: 109/109 grün, davon neu: `JsonProjectFileSerializerTests`,
  Zielordner- und Save/Open-Tests in `ProjectServiceTests` (inkl. echtem
  Datei-Roundtrip und fehlender Bilddatei), erweiterte `ToolbarViewModelTests`.

## Phase 11 — Details

- `IFileMoveService`/`FileMoveService`: führt alle Verschiebeoperationen
  gesammelt aus, überspringt bei Fehlern nur die betroffene Datei
  ("Restliche Dateien weiter bearbeiten"), verhindert Überschreiben durch
  automatische Umbenennung (`name (1).ext`) bei Namenskollision am Ziel.
- `ITrashService`/`PlatformTrashService`: echter Papierkorb unter Windows,
  Finder-`osascript`-Trick unter macOS, endgültiges Löschen als Fallback
  sonst — alles in einer Klasse, siehe `docs/architecture-decisions.md`,
  Punkt 18.
- Neuer Toolbar-Button "Anwenden" öffnet den Abschluss-Dialog
  (`IApplyConfirmationDialogService`, zeigt Links/Rechts/Übersprochen-Zähler)
  und führt nach Bestätigung `IFileMoveService.ApplyAsync` aus;
  `IProjectService.RemoveAppliedImages` entfernt erfolgreich verschobene
  Bilder endgültig aus dem Projekt (siehe Punkt 19), fehlgeschlagene bleiben
  als offene Entscheidungen erhalten.
- Ergebnis-/Fehleranzeige über einen einfachen `IMessageDialogService` —
  bewusst minimal, ausführlichere Fehlerbehandlung folgt in Phase 16.
- "Projekt speichern" als dritter Button im Abschlussdialog bewusst
  ausgelassen (automatische Speicherung deckt das bereits ab, siehe Punkt 19).
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 124/124 grün, davon neu: `FileMoveServiceTests` (echte Dateien in
  Temp-Verzeichnissen: Verschieben, Papierkorb-Aufruf, fehlender Zielordner,
  fehlende Quelldatei, Namenskollision), `Project.RemoveImages`-Tests,
  `ProjectService.RemoveAppliedImages`-Tests, erweiterte
  `ToolbarViewModelTests` (Anwenden-Flow inkl. Abbrechen/Fehlerfall).

## Phase 12 — Details

- `ImageViewerViewModel`: `SwipeOffsetX`/`SwipeRotationAngle`/`SwipeOpacity`
  + `AnimationsEnabled` (aus `AppSettings`, Standard an). `SortLeft`/
  `SortRight` sind jetzt asynchron: Transform auf Endzustand setzen → ~150 ms
  warten → Entscheidung speichern → Transform zurücksetzen.
- `ImageViewerView.axaml`: äußerer Swipe-Container mit `Transitions` auf
  `TranslateTransform.X`, `RotateTransform.Angle`, `Opacity` — Avalonia
  übernimmt die eigentliche Interpolation deklarativ (siehe
  `docs/architecture-decisions.md`, Punkt 20).
- Thumbnail-Fade-in beim ersten Erscheinen (`NullToOpacityConverter` +
  `Opacity`-Transition); Entfernen sortierter Thumbnails bleibt bewusst
  instantan (Vereinfachung, siehe Punkt 20).
- Wichtiger Bugfix während der Umsetzung: bestehende Tests, die
  `SortLeftCommand.Execute(null)` synchron aufriefen, mussten auf
  `await ...ExecuteAsync(null)` umgestellt werden, da die Entscheidung jetzt
  erst nach der Animation gespeichert wird.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 129/129 grün, davon neu: Mid-Flight- und Endzustand-Tests für die
  Swipe-Animation, Settings-Initialisierungstest für `AnimationsEnabled`.

## Phase 13 — Details

- `IImageCache`/`ImageCache` (Core): hält dekodierte PNG-Bytes für ein
  Fenster `aktueller Index ± 2` im Speicher, verdrängt alles außerhalb bei
  jeder Positionsänderung, lädt fehlende Fensterbilder im Hintergrund vor —
  siehe `docs/architecture-decisions.md`, Punkt 21.
- `ImageViewerViewModel` nutzt `IImageCache` statt `IImageDecoder` direkt;
  `UpdateCacheWindow()` wird bei jeder Positionsänderung aufgerufen.
- Thumbnail-Generierung (Phase 5) und Ordner-Scan (Phase 3) waren bereits
  vollständig asynchron mit begrenzter Parallelität — hier keine Änderung
  nötig, nur der Bild-Cache war der fehlende Baustein.
- Nicht empirisch mit tausenden echten Bildern durchgemessen (siehe Punkt 21)
  — die Architektur ist darauf ausgelegt, ein Praxistest mit einer großen
  Fotosammlung wird empfohlen.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 135/135 grün, davon neu: `ImageCacheTests` (Cache-Hit, Fenster-
  Verdrängung, Hintergrund-Vorladen, kein erneutes Dekodieren bereits
  gecachter Bilder), erweiterte `ImageViewerViewModelTests`
  (Fenster-Verschiebung bei Navigation).

## Phase 14 — Details

- `SupportedImageFormats.Raw`/`IsRaw`: CR2, CR3, NEF, ARW, RAF, DNG, ORF, RW2.
- `RawPreviewReader` (intern, geteilt) extrahiert die eingebettete
  Vorschau-JPEG via `MetadataExtractor`; `RawPreviewImageDecoder` und
  `RawThumbnailGenerator` re-encodieren sie zu PNG bzw. Thumbnail-PNG.
- `CompositeImageDecoder`/`CompositeThumbnailGenerator` wählen anhand der
  Dateiendung zwischen RAW- und Standard-Pfad — als einzige neu registrierte
  `IImageDecoder`/`IThumbnailGenerator`-Implementierung, ohne
  `ImageViewerViewModel`/`ThumbnailBarViewModel`/`ImageCache` anzufassen
  (genau die in Phase 1 anvisierte Erweiterbarkeit).
- Vor der Umsetzung empirisch mit einem synthetischen Test-TIFF verifiziert,
  dass die Offset-Interpretation für TIFF-basierte RAW-Formate korrekt ist
  (siehe `docs/architecture-decisions.md`, Punkt 22) — keine Blindannahme.
- Bekannte, dokumentierte Lücke: CR3 und RW2 werden erkannt/aufgelistet,
  aber die Preview-Extraktion schlägt für sie mit der verwendeten Bibliothek
  strukturell fehl (kontrolliert abgefangen, keine Abstürze).
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei.
- Tests: 150/150 grün, davon neu: `RawPreviewTests` (echter Extraktions-
  Roundtrip gegen synthetisches TIFF, Fehlerfall), `CompositeImageDecoderTests`,
  erweiterte `SupportedImageFormatsTests`.

## Phase 15 — Details

- `AppSettings` erweitert um `HotkeyBindings`, `LastSourceFolders`,
  `LastLeftTargetIsTrash`, `LastLeftTargetPath`, `LastRightTargetPath`.
- `ISettingsService.SettingsChanged`-Event (neu): erlaubt bereits
  konstruierten Singletons (z. B. `ImageViewerViewModel.AnimationsEnabled`),
  live auf Einstellungsänderungen zu reagieren, ohne Neustart.
- `HotkeyService` lädt/speichert Bindings jetzt über `ISettingsService`
  (Standardwerte bleiben der Fallback für alles, was nicht überschrieben
  wurde) statt nur im Arbeitsspeicher zu leben.
- `HotkeyBindingViewModel` (neu, Core): eine Zeile im Hotkey-Editor —
  Aufnahme starten/abbrechen, Konfliktprüfung gegen `IHotkeyService.Resolve`,
  Anwenden der neuen Zuordnung. Siehe `docs/architecture-decisions.md`,
  Punkt 24, für die Konflikterkennung und die Aufnahme-Mechanik.
- `SettingsViewModel` (neu, Core): Animationen-Toggle (persistiert sofort
  über `ISettingsService.SaveAsync`), fester Dark-Mode-Hinweis, Liste aller
  `HotkeyBindingViewModel`, Zurücksetzen-auf-Standard-Befehl.
- `ISettingsDialogService`/`AvaloniaSettingsDialogService` (neu): öffnet
  `SettingsWindow` modal über der Hauptfensters, erzeugt pro Aufruf ein
  frisches `SettingsViewModel` (siehe Punkt 24) — analog zum bereits
  bestehenden `IApplyConfirmationDialogService`-Muster.
- `SettingsWindow.axaml`/`.axaml.cs` (neu, App): Abschnitte "Darstellung"
  (Dark Mode fest, Animationen-Umschalter), "Ordner merken" (Infotext) und
  "Hotkeys" (Liste mit "Ändern"-Button je Aktion, Tastaturaufnahme im
  Code-behind, Konfliktmeldung inline, "Standardwerte"-Button).
  "Einstellungen"-Button in der Toolbar ist jetzt verdrahtet
  (`ToolbarViewModel.OpenSettingsCommand`).
- "Ordner merken": `IFolderPickerService.PickFoldersAsync`/`PickFolderAsync`
  haben einen neuen optionalen `suggestedStartLocation`-Parameter;
  `AvaloniaFolderPickerService` löst ihn über
  `IStorageProvider.TryGetFolderFromPathAsync` auf. Quellordner werden nur
  als Dialog-Startort vorgeschlagen, Links-/Rechts-Zielordner dagegen beim
  Start automatisch als aktive `Project`-Ziele wiederhergestellt (sofern der
  Ordner noch existiert) — Begründung in `docs/architecture-decisions.md`,
  Punkt 23.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei
  (Hintergrund-Start, Log geprüft, sauber beendet).
- Tests: 173/173 grün, davon neu: `HotkeyBindingViewModelTests`,
  `SettingsViewModelTests`, erweiterte `HotkeyServiceTests` (Persistenz,
  Laden aus Settings) und erweiterte `ToolbarViewModelTests` ("Ordner
  merken"-Persistenz und -Wiederherstellung, Settings-Dialog-Aufruf).

## Phase 16 — Details

- **Systematische Lücke gefunden und geschlossen:** `UnauthorizedAccessException`
  erbt nicht von `IOException` und wurde deshalb an fast jeder bestehenden
  Fehlerbehandlungsstelle im Projekt übersehen (`JsonSettingsService`,
  `JsonProjectFileSerializer`, `ProjectService.AutoSaveAsync`,
  `ToolbarViewModel.SelectSourceFolderAsync`, Bild-Decodierung). Überall
  ergänzt. Details und Begründung in `docs/architecture-decisions.md`,
  Punkt 25.
- **Kritischer Bugfix:** `JsonSettingsService.LoadAsync` und
  `JsonProjectFileSerializer.LoadAsync` öffneten die Datei VOR dem
  schützenden `try`-Block - eine unlesbare `settings.json` hätte die
  gesamte App schon beim Start zum Absturz gebracht (App.axaml.cs ruft
  `LoadAsync` synchron auf, bevor ein Fenster existiert). Behoben; mit
  `File.SetUnixFileMode(..., UnixFileMode.None)` nachgestellte
  Regressionstests.
- `RecoverableImageErrors` (neu, Core, `internal`): zentrale Klassifikation
  "ist dieser Fehler ein Grund, dieses eine Bild zu überspringen, statt
  abzustürzen" (`IOException`, `UnauthorizedAccessException`,
  `NotSupportedException`, `SixLabors.ImageSharp.ImageFormatException`),
  jetzt von `ImageCache`, `ImageViewerViewModel` und `ThumbnailBarViewModel`
  gemeinsam genutzt statt dreimal unabhängig gepflegt. Schließt insbesondere
  die Lücke, dass `InvalidImageContentException` (echte Bildkorruption)
  bisher nicht abgefangen wurde, nur `UnknownImageFormatException`
  (unbekanntes Format).
- `ImageViewerViewModel.ImageLoadErrorMessage`/`HasImageLoadError` (neu):
  sichtbare, deutschsprachige Fehlermeldung ("Datei nicht gefunden" / "Kein
  Zugriff auf die Datei" / "beschädigt oder nicht unterstütztes Format")
  zentral über der Bildfläche in `ImageViewerView.axaml`, statt einer
  stillen Leerfläche.
- `ThumbnailItemViewModel.HasError`/`ToolTipText` (neu): defekte Thumbnails
  zeigen ein ⚠-Overlay und einen angepassten Tooltip in der Thumbnail-Leiste.
- `FolderScannerService`: Scan pro Quellordner jetzt einzeln mit
  `try`/`catch` abgesichert, damit ein zwischenzeitlich unzugänglicher
  Ordner nicht die in anderen, bereits erfolgreich gescannten Ordnern
  gefundenen Bilder verwirft.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei
  (Hintergrund-Start, Log geprüft, sauber beendet).
- Tests: 190/190 grün, davon neu: `RecoverableImageErrorsTests`, erweiterte
  `ImageViewerViewModelTests`/`ThumbnailBarViewModelTests` (Ladefehler,
  Zugriffsfehler, Fehler-Reset bei erfolgreicher Folgenavigation), sowie
  echte dateisystembasierte Regressionstests mit `UnixFileMode.None` in
  `JsonSettingsServiceTests`, `JsonProjectFileSerializerTests` und
  `FolderScannerServiceTests` (unter Windows automatisch übersprungen).

## Phase 17 — Details

- **Icons:** Unicode-Glyphen als Button-Präfix in `ToolbarView.axaml`
  (📁 Quelle, ⬅ Links, ➡ Rechts, ✔ Anwenden, ⚙ Einstellungen, ❓ Hilfe) statt
  einer neuen Icon-Asset-Pipeline. Begründung in
  `docs/architecture-decisions.md`, Punkt 26.
- **Tooltips:** alle Toolbar-Buttons haben jetzt einen erklärenden
  `ToolTip.Tip`; Links/Rechts zeigen zusätzlich das aktuell gewählte Ziel.
- **Tastenkürzel ("Hilfe"):** Der bisher unverdrahtete "Hilfe"-Button öffnet
  jetzt eine Übersicht aller aktuellen Tastenkürzel
  (`ToolbarViewModel.ShowHelpCommand`, über den bestehenden
  `IMessageDialogService`). Da Hotkeys seit Phase 15 umbelegbar sind, liest
  die Übersicht live aus `IHotkeyService.Bindings`, statt eine feste Liste
  zu zeigen. Die deutschen Anzeigenamen wurden dafür aus `SettingsViewModel`
  in die neue, gemeinsam genutzte `HotkeyActionDisplayNames` (Core, Models)
  extrahiert.
- **Fenstergrößen:** `MainWindow` bekommt `MinWidth="800"`/`MinHeight="500"`,
  damit sich das Layout (Toolbar, Thumbnail-Leiste, Bildbereich,
  Statusleiste) nicht in eine unbrauchbare Größe verkleinern lässt. Die
  bestehende Persistierung der Fenstergröße (Phase 1) bleibt unverändert.
- **Animationen optimieren:** Swipe- und Thumbnail-Fade-Transitions
  (`ImageViewerView.axaml`, `ThumbnailBarView.axaml`) haben jetzt explizites
  Easing (`CubicEaseOut` für den Versatz/die Rotation, `QuadraticEaseOut`
  für Opacity) statt linearer Standardinterpolation - gleiche Dauer, aber
  spürbar weicherer Bewegungsablauf.
- Manuell verifiziert: `dotnet build` (0/0), App startet fehlerfrei
  (Hintergrund-Start, Log geprüft, sauber beendet).
- Tests: 191/191 grün, davon neu: `ToolbarViewModelTests.ShowHelpCommand_ShowsCurrentHotkeyBindings`
  (prüft, dass eine über `SetBinding` geänderte Taste tatsächlich in der
  Hilfe-Übersicht erscheint, nicht nur der Standardwert).

## Phase 18 — Details

- **Code bereinigen:** gezielt nach `TODO`/`FIXME`/`HACK`-Markern,
  `Console.Write*`-Debugausgaben, auskommentiertem Code und leeren
  `catch`-Blöcken durchsucht - keine Funde. `dotnet clean` + vollständiger
  Neubau bestätigt weiterhin 0 Warnungen/Fehler.
- **Dokumentation:** neue `README.md` im Projektwurzelverzeichnis (Was ist
  PhotoSorter, Voraussetzungen, Bauen/Testen/Starten, Projektstruktur,
  bekannte Einschränkungen), verlinkt auf `TODO.md` und
  `docs/architecture-decisions.md` statt deren Inhalt zu duplizieren.
- **Tests:** vollständige Suite zusätzlich in der Release-Konfiguration
  ausgeführt (`dotnet test -c Release`), nicht nur Debug.
- **Release:** `dotnet build -c Release` (0/0) und die daraus resultierende
  `PhotoSorter.App`-Binärdatei direkt (ohne `dotnet run`) gestartet und
  sauber beendet - deckt Unterschiede zwischen Debug- und
  Release-Konfiguration ab, die ein reiner Debug-Testlauf verbergen würde.
  Kein Git-Tag/Commit erstellt, da das Repository noch keine Commits hat
  und das Anlegen des ersten Commits dem Nutzer vorbehalten bleibt.
  Begründung in `docs/architecture-decisions.md`, Punkt 27.
- Tests: 191/191 grün in Debug UND Release.

## Fazit

Alle 18 Phasen aus `PhotoSorter Entwicklungsplan.pdf` sind umgesetzt,
getestet (191 Tests, Debug und Release grün) und in
`docs/architecture-decisions.md` (27 dokumentierte Entscheidungen) sowie
dieser Datei nachvollziehbar dokumentiert. Offene, bewusst dokumentierte
Einschränkungen: CR3/RW2-RAW-Vorschau (Punkt 22), Papierkorb auf Plattformen
ohne systemeigenen Trash-Mechanismus (Punkt 18), sowie die noch fehlende
Git-Historie des Projekts (Punkt 27).
