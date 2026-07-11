# Architekturentscheidungen

Dieses Dokument hält Entscheidungen fest, die von den Spezifikationsdokumenten
(`PhotoSorter – Software Design Document.pdf`, `PhotoSorter – UI & UX Design.pdf`,
`PhotoSorter Entwicklungsplan.pdf`) abweichen oder diese konkretisieren, wo die
Dokumente unterspezifiziert oder widersprüchlich waren.

## 1. Maus-Pan-Taste: mittlere statt linke Maustaste

**Widerspruch:** Das Software Design Document sagt "Linke Maustaste ziehen →
Bild verschieben" (Pan). Das UI & UX Design Dokument sagt "Mittlere Maustaste
→ Pan". Beide Dokumente sehen zusätzlich ein optionales Feature vor, bei dem
das Bild mit der Maus nach links/rechts gezogen werden kann, um es zu
sortieren (Drag-to-Sort) — das würde mit "linke Maustaste = Pan" kollidieren.

**Entscheidung:** Mittlere Maustaste = Pan (UI-Design.md folgend). Die linke
Maustaste bleibt frei für das optionale Drag-to-Sort-Feature.

**Status:** Vom Nutzer bestätigt ("mach es wie du es für am besten hältst").
Endgültig. Bei Bedarf leicht revidierbar (betrifft nur die
Maus-Event-Verdrahtung in `ImageViewerViewModel` bzw. der zugehörigen View).

## 2. Projektstruktur: Core/App/Tests statt Einzelprojekt

Das Software Design Document beschreibt Models/Services/ViewModels/Views als
logische Schichten, macht aber keine Vorgabe zur physischen Projektaufteilung.

**Entscheidung:** Drei Projekte:

- `PhotoSorter.Core` — Models, Service-Interfaces und -Implementierungen,
  ViewModels. Kennt Avalonia nicht (bis auf reine Datentypen, falls später
  nötig), ist dadurch vollständig unit-testbar ohne UI-Runtime.
- `PhotoSorter.App` — Avalonia Views, Composition Root (`Program.cs`,
  `App.axaml.cs`), Dark-Theme/Styling, plattformspezifische
  Service-Implementierungen (z. B. später der Papierkorb-Service).
- `PhotoSorter.Tests` — xUnit-Tests, referenziert nur `Core`.

**Begründung:** Erfüllt die Vorgabe "keine Businesslogik in Views" strukturell
(ViewModels können UI-Typen aus Avalonia gar nicht referenzieren, ohne dass
Core eine Abhängigkeit auf Avalonia.Desktop bekommt) und macht ViewModels ohne
UI-Testhost testbar (Vorgabe: "Jede neue Funktion durch Unit-Tests absichern").

## 3. Bilddekodierung über eigene `IImageDecoder`-Abstraktion

Wird in Phase 3/4 eingeführt, hier vorab dokumentiert, da es eine
grundlegende Erweiterbarkeits-Entscheidung ist: Anstatt direkt auf Avalonias
plattformabhängige Bild-Codecs zu setzen, decodiert eine Backend-Implementierung
(zunächst `SixLabors.ImageSharp`) alle Pflichtformate (jpg/png/bmp/tif/webp)
einheitlich auf allen Plattformen. RAW- und HEIC-Unterstützung (optional laut
SDD) werden später als zusätzliche `IImageDecoder`-Implementierungen ergänzt,
ausgewählt anhand der Dateiendung — ohne bestehenden Code zu ändern. Das ist
die gleiche Erweiterungsstelle, die auch für das im Entwicklungsplan erwähnte
Plugin-System (Version 2.x) genutzt werden kann.

## 4. Einstellungen: eigener `ISettingsService` statt `IConfiguration`/`IOptions`

Das .NET-übliche `Microsoft.Extensions.Configuration`/`IOptions`-Muster ist auf
unveränderliche, beim Start gelesene Konfiguration ausgelegt. PhotoSorters
Einstellungen (Fenstergröße, Ordner, Hotkeys, Zoomverhalten, Animationen) sind
zur Laufzeit veränderlich und müssen beim Beenden zurückgeschrieben werden.
Dafür gibt es einen einfachen `ISettingsService` mit `LoadAsync`/`SaveAsync`
und JSON-Persistenz — das entspricht der Vorgabe "Einstellungen: Speichern in
JSON" direkter und ohne unnötige zusätzliche Abhängigkeit.

## 5. Dark Mode ist fest, kein Light-Mode-Umschalter

UI-Design.md: "Dark Mode: Standard." und "dunkles Design" als
Designphilosophie-Punkt. Es wird kein Light-Theme-Toggle in den Einstellungen
vorgesehen, solange die Spezifikation keinen expliziten Umschalter fordert.
`RequestedThemeVariant` ist fest auf `Dark` gesetzt.

## 6. .NET-Version und Kernpakete

- **.NET 8 (LTS)** statt der neuesten .NET-Version, da PhotoSorter ein
  langlebiges Desktop-Tool ist und LTS-Support Priorität vor neuesten
  Sprachfeatures hat.
- **Avalonia UI 11.x/12.x**, **CommunityToolkit.Mvvm** (Source-Generator-MVVM,
  weniger Boilerplate als manuelles `INotifyPropertyChanged`).
- **Microsoft.Extensions.DependencyInjection** für DI,
  **Microsoft.Extensions.Logging** + **Serilog** (Konsole + rollierende
  Datei unter `%AppData%/PhotoSorter/logs`) für Logging.
- **xUnit** für Tests.

## 7. Kein dauerhaftes `dotnet`-PATH-Setup in der Shell des Nutzers

Für die Entwicklung wurde das .NET SDK ohne Root-Rechte nach `~/.dotnet`
installiert. Es wurde bewusst **keine** Shell-Konfigurationsdatei
(`.zshrc`/`.zprofile`) dauerhaft verändert, da dies eine persistente
Systemänderung ohne explizite Freigabe wäre. Wer `dotnet` auch interaktiv im
Terminal nutzen möchte, muss `export PATH="$HOME/.dotnet:$PATH"` selbst
dauerhaft einrichten.

## 8. `ConfigureAwait(false)` verpflichtend in `PhotoSorter.Core`

**Gefundener Bug (Phase 2):** `App.axaml.cs` lädt/speichert Einstellungen
synchron über `settingsService.LoadAsync().GetAwaiter().GetResult()` (nötig,
da beim Start noch kein Fenster existiert, auf das man normal `await`en
könnte, und Avalonias `OnFrameworkInitializationCompleted` keine async
Signatur hat). Ohne `ConfigureAwait(false)` in `JsonSettingsService` versucht
die Fortsetzung nach dem inneren `await`, auf den vom Aufrufer erfassten
`SynchronizationContext` (Avalonias UI-Thread) zurückzukehren — genau der
Thread, der gerade blockierend auf das Ergebnis wartet. Das ist ein klassischer
Sync-over-Async-Deadlock. Er trat nicht bei jedem Start auf, weil der
synchrone Schnellpfad ("Datei existiert nicht") nie deadlockt — erst sobald
eine `settings.json` bereits vorhanden war (z. B. nach einem vorherigen Lauf),
hing die App beim Start.

**Fix:** Alle `await`s in `JsonSettingsService.LoadAsync`/`SaveAsync` nutzen
jetzt `ConfigureAwait(false)`. Das ist die Standardregel für jeden `async`
Code in `PhotoSorter.Core`: Core kennt keine UI und darf nicht annehmen, dass
ein `SynchronizationContext` vorhanden oder harmlos ist. Diese Regel gilt für
alle künftigen Services in Core (ProjectService, FileMoveService, ImageLoader
usw.), nicht nur für Settings.

**Regressionstests:** `JsonSettingsServiceTests` enthält zwei Tests, die
`LoadAsync`/`SaveAsync` unter einem `SynchronizationContext` synchron
aufrufen, der gepostete Fortsetzungen nie ausführt (`NonPumpingSynchronizationContext`)
— ohne `ConfigureAwait(false)` würden diese Tests zuverlässig hängen
(abgesichert durch ein 5-Sekunden-Timeout, das den Test fehlschlagen statt
den gesamten Testlauf hängen lässt).

## 9. Dialoge und UI-Thread-Marshaling als Core-Abstraktionen (`IFolderPickerService`, `IUiDispatcher`)

**Problem:** Ordnerauswahl braucht einen nativen Dialog, der an ein Fenster
gebunden ist (Avalonias `IStorageProvider`) — das ist unvermeidbar
UI-Framework-Code und darf laut Vorgabe nicht in ein ViewModel. Gleichzeitig
läuft der rekursive Ordner-Scan (potenziell tausende Dateien) bewusst
asynchron im Hintergrund (`ConfigureAwait(false)`, siehe Punkt 8) und meldet
sein Ergebnis über `IProjectService.ProjectChanged` — dieses Event kann also
von einem Threadpool-Thread statt vom UI-Thread ausgelöst werden.

**Entscheidung:** Beide Probleme werden als Interfaces in
`PhotoSorter.Core.Services.Abstractions` deklariert
(`IFolderPickerService`, `IUiDispatcher`), aber **nicht** in Core
implementiert — die konkreten Avalonia-Implementierungen
(`AvaloniaFolderPickerService`, `AvaloniaUiDispatcher`) leben in
`PhotoSorter.App` und werden dort über eine eigene
`AddPhotoSorterApp()`-Registrierung eingebunden. ViewModels, die auf
Core-Events reagieren, marshalen ihre Aktualisierung explizit über
`IUiDispatcher.Post(...)` auf den UI-Thread.

**Begründung:** So bleibt `PhotoSorter.Core` vollständig frei von
Avalonia-Referenzen (auch nicht das Basispaket) und ViewModel-Reaktionslogik
bleibt mit einem synchronen Test-Double (`ImmediateUiDispatcher`) ohne echten
UI-Thread testbar — ein `Dispatcher.UIThread.Post(...)`-Aufruf direkt in
einem xUnit-Test würde in den meisten Fällen nie ausgeführt, da außerhalb
einer laufenden Avalonia-Anwendung niemand die Dispatcher-Queue abarbeitet.

## 10. `SixLabors.ImageSharp` fixiert auf Version 3.1.x (letzte Apache-2.0-Version)

**Problem:** Avalonias `Bitmap`-Klasse kann laut Spike-Test (Konsolen-Testprojekt,
`new Bitmap(stream)` ohne `AppBuilder`-Initialisierung) nicht ohne laufende
Avalonia-Anwendung konstruiert werden (`InvalidOperationException: Unable to
locate 'Avalonia.Platform.IPlatformRenderInterface'`). Das bestätigt Punkt 3:
Core braucht einen eigenen Decoder, der reine Bytes statt eines
Avalonia-Typs liefert (`IImageDecoder.DecodeToPngAsync` → PNG-Bytes; die
App-Schicht wandelt sie über `PngBytesToBitmapConverter` in ein `Bitmap` um).

**Lizenzproblem:** `SixLabors.ImageSharp` Version 4.0.0 (aktuell auf NuGet)
verlangt beim Build einen kommerziellen Lizenzschlüssel
("No Six Labors license found... obtain a license from
sixlabors.com/pricing"). Das Projekt bleibt daher auf **Version 3.1.x**
(zuletzt getestet: 3.1.12), der letzten Hauptversion unter der
Apache-2.0-Lizenz vor dem Lizenzwechsel. 3.1.5 hatte noch bekannte
Sicherheitslücken (NU1902/NU1903); 3.1.12 behebt diese.

**Konsequenz für spätere Phasen:** Ein NuGet-Update auf `SixLabors.ImageSharp`
5.x/6.x etc. darf nicht ungeprüft erfolgen — zuerst die Lizenzbedingungen der
jeweiligen Version prüfen. Die Abhängigkeit ist vollständig hinter
`IImageDecoder` gekapselt, ein Wechsel auf eine andere Bibliothek (z. B.
SkiaSharp direkt oder Magick.NET) würde nur `ImageSharpImageDecoder`
betreffen.

## 11. Aktuelle Position (`CurrentIndex`) lebt in `IProjectService`, nicht im `ImageViewerViewModel`

Das SDD listet "aktuelle Position" explizit als Teil des `Project`-Modells
(persistiert in der `.photosort`-Projektdatei, Phase 10). Da sowohl
`StatusBarViewModel` (Anzeige "Bild x / y") als auch `ImageViewerViewModel`
(welches Bild wird angezeigt) und später `ThumbnailBarViewModel` (Phase 5,
welches Thumbnail wird hervorgehoben) dieselbe Position kennen müssen, hält
`IProjectService` sie zentral (`CurrentIndex`, `MoveNext()`,
`MovePrevious()`, Event `CurrentIndexChanged`) statt sie in einem einzelnen
ViewModel zu duplizieren. Das vermeidet Index-Sync-Bugs zwischen ViewModels
und passt zur SDD-eigenen Datenmodellierung — in Phase 10 wird `CurrentIndex`
einfach mit in die Projektdatei aufgenommen, ohne dass diese Services
umgebaut werden müssen.

## 12. Interimistische Pfeiltasten-Navigation (Auf/Ab) vor Phase 7

Phase 4 ("Bildanzeige: nächstes/vorheriges Bild") liegt vor Phase 7
("Hotkeys ... konfigurierbar"). Damit die Navigation aber schon jetzt in der
laufenden Anwendung nutzbar und manuell verifizierbar ist, reagiert
`MainWindow.axaml.cs` bereits jetzt hart codiert auf Pfeil-Auf/-Ab (ruft
`ImageViewerViewModel.PreviousImageCommand`/`NextImageCommand` auf). Das ist
bewusst **nur ein Zwischenschritt**:

- Es werden nur Pfeil-Auf/-Ab behandelt (vorheriges/nächstes Bild) — Pfeil
  Links/Rechts sind laut SDD für die Sortier-Aktionen reserviert (Phase 8)
  und bleiben bis dahin unbehandelt, um kein Verhalten einzuführen, das
  später wieder entfernt werden müsste.
- Phase 7 ersetzt diesen Handler durch den vollständigen, konfigurierbaren
  `HotkeyService`. Die eigentliche Navigationslogik (die Commands im
  ViewModel) bleibt dabei unverändert — nur die Tastenzuordnung wird von
  hart codiert auf konfigurierbar umgestellt.

## 13. "100 %" ist `Stretch.None` + `ZoomFactor`, "Einpassen" ist `Stretch.Uniform`

Für echtes pixelgenaues "100 %" (SDD: "100 %-Ansicht") reicht ein einzelner
`ZoomFactor` auf einem immer `Stretch="Uniform"` gerenderten Bild nicht:
Uniform skaliert bereits automatisch auf die verfügbare Fläche, wodurch
"ZoomFactor = 1.0" dort nur "Einpassen" bedeuten würde, nicht "1 Bildpixel =
1 Bildschirmpixel". Deshalb schaltet `ZoomModeToStretchConverter` den
`Stretch` des `Image`-Controls je nach `ZoomMode` um: `Uniform` für
"Einpassen" (kein zusätzlicher Transform nötig), `None` für "Manual" (zeigt
native Pixelgröße; `ZoomFactor` skaliert von dieser 100-%-Basis aus weiter
per Mausrad). Der Übergang von "Einpassen" zu "Manual" per Mausrad springt
dadurch bewusst direkt auf eine 100-%-Basis (nicht nahtlos von der
Einpassen-Skalierung aus) — das ViewModel kennt die tatsächliche
Einpassen-Skalierung nicht (die berechnet Avalonias Layout intern) und soll
sie aus Testbarkeits-/Architekturgründen auch nicht kennen müssen.

## 14. `HotkeyChord.Key` als String statt `Avalonia.Input.Key`

`IHotkeyService` (Core) bildet Tasten auf Strings ab (z. B. `"Left"`,
`"OemPlus"`), die bewusst exakt den Namen der Enum-Werte von
`Avalonia.Input.Key` entsprechen, statt den Avalonia-Typ selbst zu
verwenden. Anders als bei `Bitmap` (Punkt 10, braucht eine laufende App) wäre
die Verwendung des reinen `Key`-Enums technisch unproblematisch — die
Entscheidung ist stattdessen konsistente Abgrenzung: Core bleibt frei von
jeglicher Avalonia-Abhängigkeit, und die Konvertierung
(`e.Key.ToString()` → `HotkeyChord`) bleibt ein trivialer Einzeiler in
`MainWindow.axaml.cs`. Persistenz der Bindings in der JSON-Konfiguration und
der Hotkey-Editor mit Konfliktererkennung (UI-Design.md) folgen in Phase 15;
Phase 7 liefert die konfigurierbare Infrastruktur samt Standardbelegung.
`SortLeft`/`SortRight`/`Skip`/`Undo` lösen bereits korrekt auf, haben aber
noch keinen Befehl dahinter (Phase 8/9) — bewusst kein Platzhalter-Handler,
sondern schlicht noch keine Zuordnung im Dispatch von `MainWindow.axaml.cs`.

## 15. `Project.Images` bleibt stabil; "offene" Bilder sind eine Ableitung, keine separate Liste

Für "Bilder verschwinden aus der Liste" (Roadmap Phase 8) und "bereits
sortierte Bilder verschwinden" (UI-Design.md, Thumbnail-Leiste) gab es zwei
Optionen: (a) entschiedene Bilder aus `Project.Images` tatsächlich entfernen,
oder (b) `Project.Images` als stabile Originalreihenfolge behalten und
"offen" nur als Ableitung (`!IsDecided(image)`) zu berechnen.

**Entscheidung:** (b). `Project.Images` wird nach dem Laden nie mehr
verändert. `IProjectService.CurrentIndex` navigiert nur noch über offene
Bilder (`MoveNext`/`MovePrevious`/`RecordDecision` überspringen bereits
entschiedene automatisch), und `ThumbnailBarViewModel` entfernt entschiedene
Einträge nur aus seiner eigenen UI-Projektion (`Thumbnails`), nicht aus dem
Modell.

**Begründung:** Phase 10 muss die ursprüngliche "Reihenfolge" persistieren
und Phase 9 (Undo) muss eine rückgängig gemachte Entscheidung exakt an ihrer
ursprünglichen Position wiederherstellen können — beides ist mit einer
stabilen Originalliste trivial, mit einer mutierten Liste (Index-Verschiebung
bei jedem Entfernen) fehleranfällig. `Project.IsDecided(ImageFile)` nutzt
Referenzgleichheit (kein `Equals`-Override auf `ImageFile` nötig), da
dieselben Instanzen aus `Images` auch in `SortDecision.Image` landen.

## 16. Undo: "beliebig viele Schritte" ergibt sich aus der Decisions-Liste; Strg+Z ist ein fester Zusatz-Alias

`IProjectService.Undo()` entfernt einfach das letzte Element aus
`Project.Decisions` (`RemoveLastDecision`, in Phase 8 bereits vorbereitet)
und springt zur wiederhergestellten Position zurück. Da `Decisions` eine
normale Liste ist, ergibt sich "beliebig viele Schritte zurück" (Roadmap
Phase 9) automatisch, ohne einen separaten History-Stack — jeder erneute
Aufruf entfernt einfach die nächste Entscheidung.

SoftwareDesign.md fordert **zwei** Tasten für Undo: "Backspace oder Strg+Z".
`IHotkeyService` erlaubt aber bewusst nur eine konfigurierbare Taste pro
Aktion (siehe Punkt 14) — ein zweites Bindungssystem nur für diesen einen
Fall wäre unverhältnismäßig. Backspace bleibt die konfigurierbare
`HotkeyAction.Undo`-Bindung; Strg+Z ist zusätzlich fest in
`MainWindow.axaml.cs` verdrahtet und immer aktiv, unabhängig von der
konfigurierten Undo-Taste.

## 17. Projektdatei: "Einstellungen" bewusst nicht mit-persistiert; automatische Wiederherstellung ohne Abstürzungserkennung

**"Einstellungen" fehlt bewusst:** Das SDD listet unter Projektdatei-Inhalt
auch "Einstellungen". Aktuell enthält `AppSettings` nur Fenstergröße — ein
Snapshot davon in jeder `.photosort`-Datei zu speichern hätte keinen
erkennbaren Nutzen (Fenstergröße ist global, nicht projektspezifisch) und
würde nur eine unnötige Kopplung zwischen Projektdatei-Format und
`AppSettings`-Struktur schaffen. Sobald projektrelevante Einstellungen
existieren (z. B. individuelle Hotkeys pro Projekt, falls das je gewünscht
wird), kann `ProjectFileDto` einfach um ein Feld erweitert werden.

**Automatische Wiederherstellung immer, nicht nur nach Absturz:** Es wird
nicht zwischen sauberem Beenden und Absturz unterschieden (das würde einen
"clean shutdown"-Marker erfordern, z. B. Löschen der Autosave-Datei bei
`ShutdownRequested`). Stattdessen wird die Autosave-Datei
(`%AppData%/PhotoSorter/autosave.photosort`) bei **jedem** Start geladen,
falls vorhanden. Für ein Sortier-Werkzeug ist "mach dort weiter, wo du
aufgehört hast" in beiden Fällen (Absturz oder normales Beenden) das
erwartbare Verhalten — die Unterscheidung hätte keinen praktischen Nutzen,
aber zusätzliche Komplexität und eine weitere Fehlerquelle bedeutet.
Geprüft: Eine beschädigte Autosave-Datei führt zu einem Log-Warning und
einem leeren Start-Zustand, nicht zu einem Absturz.

**Auto-Save-Trigger:** Nur `ProjectChanged` und `DecisionsChanged` lösen ein
Auto-Save aus, nicht `CurrentIndexChanged` — reine Navigation (Pfeiltasten)
würde sonst bei jedem Tastendruck eine Datei schreiben.

**ViewModel-Initialisierung nachgebessert:** Bei der Umsetzung fiel auf, dass
`ImageViewerViewModel` und `ThumbnailBarViewModel` sich (anders als
`StatusBarViewModel` seit Phase 3) nur über Events aktualisierten, nicht
proaktiv aus dem bereits vorhandenen `IProjectService`-Zustand im
Konstruktor. Das hätte bei einer Wiederherstellung vor ihrer Erzeugung zu
einem leeren Bildbereich/einer leeren Thumbnail-Leiste geführt, obwohl
Daten vorhanden sind. Beide rufen jetzt ihre Aktualisierungsmethode auch
einmal direkt im Konstruktor auf — unabhängig von der Konstruktions­reihenfolge
korrekt.

## 18. Papierkorb plattformabhängig in einer Klasse; Linux/Fallback löscht endgültig

`PlatformTrashService` (Core) prüft zur Laufzeit per `RuntimeInformation.IsOSPlatform`,
statt getrennter Codepfade/Projekte pro Plattform zu pflegen — genau das von
Phase 1 anvisierte Muster für "macOS optional, ohne Codeänderungen". Windows
nutzt den echten Papierkorb über `Microsoft.VisualBasic.FileIO.FileSystem`
(im .NET-SDK ohne zusätzliche Paketreferenz verfügbar, funktioniert aber nur
unter Windows). macOS hat keine eingebaute .NET-API dafür; hier wird der
Finder per `osascript` angewiesen, die Datei zu löschen (bewegt sie in den
Papierkorb). Für alle anderen Plattformen (Linux ohne Trash-Helper) wird
**endgültig gelöscht**, statt die Datei stillschweigend liegen zu lassen —
explizit dokumentiert, damit dieses Verhalten nicht überrascht.

## 19. "Anwenden" entfernt erfolgreich verschobene Bilder wirklich aus `Project.Images`

Punkt 15 hält fest, dass `Project.Images` während des Sortierens stabil
bleibt (nötig für Undo/Reihenfolge-Persistenz). Nach einem erfolgreichen
`IFileMoveService.ApplyAsync`-Aufruf gilt das nicht mehr: Die Quelldatei
existiert an ihrem ursprünglichen Pfad nicht mehr, ein Undo dorthin wäre
sinnlos. Deshalb entfernt `IProjectService.RemoveAppliedImages(...)` erfolgreich
verschobene Bilder endgültig aus `Project.Images`/`Decisions`
(`Project.RemoveImages`, real mutierend). Fehlgeschlagene Verschiebungen
bleiben unverändert als offene Entscheidungen bestehen, damit sie erneut
versucht werden können. Da dabei Indizes verschieben können, ermittelt
`ProjectService.RemoveAppliedImages` die neue Position des aktuellen Bildes
über Referenzgleichheit neu, statt den rohen Index beizubehalten.

**Bewusste Vereinfachung:** Der Abschlussdialog aus UI-Design.md hat einen
dritten Button "Projekt speichern" (zusätzlich zu Anwenden/Abbrechen). Da das
Projekt durch die automatische Speicherung (Phase 10) ohnehin kontinuierlich
persistiert wird, wurde dieser Button für die aktuelle Umsetzung ausgelassen
— ein expliziter "Speichern unter"-Dialog mit selbst gewähltem Dateinamen
bleibt wie in Punkt 10 vermerkt zurückgestellt. Die eigentliche
Fehleranzeige bei fehlgeschlagenen Verschiebeoperationen ist bewusst
minimal (ein einfacher Meldungsdialog mit Dateiname + Fehlertext) — eine
ausführlichere, wiederverwendbare Fehlerbehandlungs-UI ist Gegenstand von
Phase 16 ("Fehlerbehandlung"), die explizit als eigene, spätere Phase
vorgesehen ist.

## 20. Swipe-Animation: Zeitsteuerung im ViewModel, Interpolation deklarativ in XAML

`ImageViewerViewModel` setzt beim Sortieren `SwipeOffsetX`/`SwipeRotationAngle`/
`SwipeOpacity` auf die "Endzustand"-Werte (weit außerhalb des sichtbaren
Bereichs, leichte Rotation, unsichtbar) und wartet dann `~150 ms`
(`Task.Delay`), bevor die eigentliche Entscheidung über `RecordDecision`
gespeichert und der Transform zurückgesetzt wird. Die eigentliche visuelle
Interpolation (das sichtbare Gleiten) übernimmt Avalonia selbst über
`Transitions` auf `TranslateTransform.X`, `RotateTransform.Angle` und
`Opacity` in `ImageViewerView.axaml` — das ViewModel kennt nur "wann" sich
diese Werte ändern, nicht "wie" der Übergang aussieht. Diese Trennung hält
Core frei von Rendering-Details und macht die Sequenzierung (Decision erst
NACH der Animation speichern) unit-testbar (`ImageViewerSortingTests`
prüft sowohl den Mid-Flight-Zustand als auch den Endzustand).

**Wichtiger Bugfix während der Umsetzung:** Da `SortLeftCommand`/
`SortRightCommand` jetzt asynchron sind (und bei aktivierten Animationen
~150 ms brauchen), mussten bestehende Tests von `.Execute(null)` auf
`await ...ExecuteAsync(null)` umgestellt werden — sonst wird die
Entscheidung zum Prüfzeitpunkt noch nicht gespeichert sein.

**"Sanfte Übergänge" für Thumbnails:** Statt eine (in Avalonia ohne
Zusatzaufwand nicht trivial umsetzbare) Fade-out-Animation beim Entfernen
sortierter Thumbnails zu bauen, wurde bewusst nur ein Fade-**in** beim
erstmaligen Erscheinen umgesetzt (`NullToOpacityConverter` + `Opacity`-
Transition) — die Thumbnails poppen dadurch nicht mehr abrupt auf, sobald
sie im Hintergrund generiert wurden. Das entfernen sortierter Thumbnails
bleibt instantan; das ist eine bewusste Vereinfachung, kein Versehen.

**Animationen ein/aus:** `AppSettings.AnimationsEnabled` (Standard: an)
wird beim Erzeugen von `ImageViewerViewModel` einmalig gelesen. Die
eigentliche Einstellungs-UI zum Umschalten folgt in Phase 15; die
zugrundeliegende Funktionalität (Animation überspringen, wenn deaktiviert)
ist bereits vollständig funktionsfähig und getestet.

## 21. Bild-Cache: Fenster nach Bildanzahl begrenzt, nicht nach Speichergröße

`IImageCache`/`ImageCache` hält PNG-dekodierte Bytes für ein Fenster von
`aktueller Index ± 2` (5 Bilder) im Speicher — mehr als die von
SoftwareDesign.md geforderten "Vorheriges/Aktuelles/Nächstes", aber
begrenzt genug, dass der Speicherverbrauch bei "mehreren tausend Bildern"
nicht unbegrenzt wächst. Bei jeder Positionsänderung ruft
`ImageViewerViewModel.UpdateCacheWindow` `IImageCache.UpdateWindow(...)` auf,
was alles außerhalb des neuen Fensters entfernt und noch fehlende Bilder im
Hintergrund nachlädt (fire-and-forget, Fehler werden geloggt statt die
Navigation zu blockieren).

**Bewusste Vereinfachung:** Begrenzung nach Bildanzahl (5 Bilder), nicht nach
tatsächlicher Speichergröße in Byte. Eine größenbasierte Begrenzung wäre
präziser (ein 45-MP-TIFF wiegt deutlich mehr als ein kleines JPEG), aber
deutlich komplexer umzusetzen und zu testen. Die feste Fenstergröße ist eine
angemessene Näherung für "Speicherverbrauch und Geschwindigkeit
ausbalancieren" und kann bei Bedarf später zu einer byte-genauen
Begrenzung ausgebaut werden, ohne die `IImageCache`-Schnittstelle nach außen
zu ändern.

**Nicht empirisch mit tausenden Bildern getestet:** Die Architektur
(asynchrones Scannen, begrenzte parallele Thumbnail-Generierung aus Phase 5,
begrenztes Bild-Cache-Fenster) ist auf große Bildmengen ausgelegt, konnte in
dieser Umgebung aber mangels echter Testbilder und GUI-Interaktion nicht mit
tatsächlich tausenden Bildern durchgemessen werden. Ein Praxistest mit einer
großen, echten Fotosammlung wird empfohlen.

## 22. RAW-Preview über `MetadataExtractor`; CR3 und RW2 mit bekannter Einschränkung

**Bibliothekswahl:** `MetadataExtractor` (Apache 2.0, reines .NET, keine
nativen Abhängigkeiten) liest die eingebettete Vorschau-JPEG aus RAW-Dateien
über `ExifThumbnailDirectory` (Tags `ThumbnailOffset`/`ThumbnailLength`).
Das entspricht exakt "RAW-Dateien sollen mindestens über das eingebettete
Preview angezeigt werden" (SoftwareDesign.md) — es wird bewusst NICHT
demosaicing/voll dekodiert.

**Verifiziert, nicht nur angenommen:** Vor der Umsetzung wurde per
Quellcode-Analyse von MetadataExtractor 2.9.3 und einem synthetischen
Test-TIFF (siehe `tests/.../MinimalTiffBuilder.cs`) geprüft, ob
`TagThumbnailOffset` bei TIFF-basierten Dateien (wie es CR2/NEF/ARW/DNG/ORF
sind) tatsächlich ein absoluter Datei-Offset ist. Ergebnis: Für alle über
`TiffMetadataReader` verarbeiteten Formate ist `ExifStartOffset = 0`, der
Tag-Wert also direkt der absolute Offset — **bestätigt**, nicht nur vermutet.
Dieser Mechanismus ist jetzt durch echte Regressionstests abgesichert
(`RawPreviewTests`), nicht nur durch eine einmalige Stichprobe.

**Bekannte Lücke — CR3 und RW2 funktionieren nicht:**

- **CR3** (Canon, ISO-BMFF/MP4-artiger Container): Die eingebettete Vorschau
  liegt in `PRVW`/`THMB`-Boxen, die `MetadataExtractor` nicht auswertet. Es
  gibt keinen Weg, über die öffentliche API dieser Bibliothek an die
  Preview-Bytes zu kommen.
- **RW2** (Panasonic): Die Root-IFD wird als `PanasonicRawIfd0Directory`
  statt `ExifIfd0Directory` geparst, wodurch nie ein `ExifThumbnailDirectory`
  erzeugt wird. Die eigentliche Vorschau liegt im proprietären Tag `0x002e`,
  auf das die Bibliothek keinen öffentlichen Zugriff bietet.
- **CR2, NEF, ARW, DNG, ORF, RAF** funktionieren nachweislich (RAF wird
  intern auf den JPEG/Exif-Pfad umgeleitet, sobald die eingebettete
  Vorschau-JPEG-Kennung in den ersten 512 Bytes gefunden wird).

**Entscheidung:** CR3 und RW2 bleiben trotzdem in
`SupportedImageFormats.Raw` (Dateien werden also erkannt/aufgelistet, nicht
stillschweigend ignoriert), aber `RawPreviewReader` wirft für sie eine
`NotSupportedException`, die von der bereits vorhandenen Fehlerbehandlung in
`ImageViewerViewModel`/`ThumbnailBarViewModel` abgefangen wird (Bild bleibt
leer, Warnung im Log, keine Abstürze, restliche Bilder funktionieren
weiter — bereits in Phase 4/5 so gebaut). Das ist ehrlicher als CR3/RW2
entweder gar nicht zu erkennen (Dateien wären für den Nutzer unsichtbar)
oder fälschlich zu behaupten, sie würden vollständig unterstützt. Eine
echte CR3/RW2-Unterstützung würde eine zusätzliche, formatspezifische
Bibliothek oder manuelles Container-Parsing erfordern.

## 23. "Ordner merken": Quellordner nur als Vorschlag, Zielordner als echte Wiederherstellung

`AppSettings` bekommt `LastSourceFolders`, `LastLeftTargetIsTrash`,
`LastLeftTargetPath` und `LastRightTargetPath`. Diese vier Werte werden
aber bewusst unterschiedlich verwendet, weil sie unterschiedliche
Nutzungsmuster abbilden:

- **Quellordner** wechseln typischerweise mit jeder Sitzung (der Nutzer
  sortiert heute den Camera-Import von letzter Woche, morgen einen anderen
  Ordner) — `LastSourceFolders` wird daher nur als
  `SuggestedStartLocation` an `IFolderPickerService.PickFoldersAsync`
  übergeben, öffnet den Dialog also am zuletzt verwendeten Ort, erzwingt
  aber keine Auswahl.
- **Links-/Rechts-Zielordner** sind dagegen meist feste "Sortiere-hinein"-
  Ordner (z. B. immer "Behalten" und immer der Papierkorb), die über viele
  Sitzungen hinweg gleich bleiben. Diese werden deshalb in
  `ToolbarViewModel`'s Konstruktor (`RestoreRememberedTargets`) direkt als
  aktive `Project`-Ziele wiederhergestellt, nicht nur als Dialog-Vorschlag —
  sonst müsste der Nutzer sie bei jedem Start erneut auswählen, obwohl sie
  sich nie ändern.
- Ein gemerkter Zielordner wird nur wiederhergestellt, wenn
  `Directory.Exists(...)` zum Startzeitpunkt noch zutrifft (z. B. externe
  Festplatte nicht angeschlossen) — sonst bliebe das Ziel unsichtbar auf
  einen nicht mehr existierenden Pfad gesetzt, und "Anwenden" würde erst
  beim Verschieben mit einer verwirrenden Fehlermeldung fehlschlagen.
  Der Papierkorb (`LastLeftTargetIsTrash`) hat keinen Pfad und wird ohne
  Existenzprüfung wiederhergestellt.
- `IFolderPickerService.PickFoldersAsync`/`PickFolderAsync` haben dafür
  einen neuen optionalen `suggestedStartLocation`-Parameter bekommen;
  `AvaloniaFolderPickerService` löst ihn über
  `IStorageProvider.TryGetFolderFromPathAsync` auf und ignoriert ihn
  (`null`), wenn der Ordner nicht mehr existiert.

## 24. Hotkey-Editor: Konflikterkennung statt stillem Überschreiben; `ISettingsDialogService` erzeugt pro Aufruf ein frisches ViewModel

**Konflikterkennung:** `HotkeyBindingViewModel.ApplyCapturedChord` prüft
über `IHotkeyService.Resolve` vor jeder Neuzuordnung, ob die aufgenommene
Tastenkombination bereits einer anderen Aktion zugeordnet ist. Falls ja,
bleibt die Aufnahme aktiv (`IsCapturing = true`) und eine
`ConflictMessage` erscheint ("Bereits belegt durch ..."), statt die andere
Aktion stillschweigend zu entwerten. Das ist zwar mehr Aufwand als
einfaches Überschreiben, verhindert aber ein subtiles Bedienproblem: ohne
Warnung würde ein Nutzer versehentlich z. B. "Rückgängig" auf eine bereits
für "Vollbild" genutzte Taste legen und beide Aktionen wären am Ende nur
noch über Umwege erreichbar.

**Aufnahme-Mechanik:** Da die Umwandlung eines Tastendrucks in einen
`HotkeyChord` Avalonias `Key`-Enum benötigt, passiert das (wie schon bei
`MainWindow.OnKeyDown`, Punkt 14) im View-Code-behind
(`SettingsWindow.OnKeyDown`), nicht im ViewModel. Reine Modifikator-
Tastendrücke (Strg/Umschalt/Alt allein) werden ignoriert, damit der Nutzer
z. B. Strg gedrückt halten kann, bevor er die eigentliche Taste drückt;
Escape bricht die Aufnahme ab.

**`ISettingsDialogService` konstruiert `SettingsViewModel` pro Aufruf neu**
(wie schon `IApplyConfirmationDialogService`/`ApplyConfirmationViewModel`,
Punkt 9), statt es als DI-Singleton zu registrieren. Dadurch spiegelt das
Fenster bei jedem Öffnen garantiert den aktuellen `IHotkeyService`-/
`ISettingsService`-Zustand wider, und ein mitten in der Tastenaufnahme
geschlossenes Fenster hinterlässt keinen hängenden `IsCapturing`-Zustand,
der beim nächsten Öffnen sichtbar wäre.

**Live-Reaktivität für "Animationen":** `ISettingsService.SettingsChanged`
(neu in dieser Phase) erlaubt es bereits konstruierten Singletons wie
`ImageViewerViewModel`, auf eine Änderung von `AnimationsEnabled` sofort zu
reagieren, ohne Neustart der Anwendung — das Settings-Fenster ruft beim
Umschalten `ISettingsService.SaveAsync()` auf, was das Event auslöst.

## 25. Fehlerbehandlung: `UnauthorizedAccessException` systematisch nachgezogen; zentrale `RecoverableImageErrors`-Klassifikation

Bei der Durchsicht aller bestehenden `catch`-Klauseln für Phase 16 (beschädigte
Bilder, fehlende Dateien, Zugriffsfehler, Projektwiederherstellung) fielen
zwei systematische Lücken auf, die keine hypothetischen Randfälle sind,
sondern bei ganz normaler Nutzung auftreten können:

**Lücke 1 — `UnauthorizedAccessException` erbt nicht von `IOException`:**
Fast jede bestehende Fehlerbehandlung im Projekt fing bisher nur
`IOException` ab (z. B. `JsonSettingsService`, `JsonProjectFileSerializer`,
`ProjectService.AutoSaveAsync`, `ToolbarViewModel.SelectSourceFolderAsync`,
sowie die Bild-Decodierung). `UnauthorizedAccessException` leitet sich aber
direkt von `SystemException` ab, nicht von `IOException` - eine
schreibgeschützte oder durch ein anderes Programm gesperrte Datei hätte diese
Fehlerbehandlung also einfach durchschlagen. Alle betroffenen Stellen wurden
auf `ex is IOException or UnauthorizedAccessException` (oder die entsprechend
erweiterte Variante) umgestellt.

**Lücke 2 — `File.OpenRead`/`File.Create` lag teilweise außerhalb des
`try`-Blocks:** In `JsonSettingsService.LoadAsync` und
`JsonProjectFileSerializer.LoadAsync` stand der Aufruf, der die Datei
tatsächlich öffnet, VOR dem `try`, der nur die anschließende Deserialisierung
schützte. Da `App.axaml.cs` `settingsService.LoadAsync()` synchron per
`GetAwaiter().GetResult()` aufruft, bevor überhaupt ein Fenster existiert,
hätte eine unlesbare `settings.json` (z. B. Rechteproblem) die gesamte
Anwendung schon vor dem ersten Fensteraufbau abstürzen lassen - der
gravierendste denkbare "Zugriffsfehler". Beide Methoden wurden so
umstrukturiert, dass das Öffnen der Datei selbst im `try` liegt. Mit
`File.SetUnixFileMode(..., UnixFileMode.None)` nachgestellte Regressionstests
(`JsonSettingsServiceTests`, `JsonProjectFileSerializerTests`) verifizieren
das - nicht nur eine Behauptung, sondern ein echter, plattformabhängig
übersprungener (Windows: kein verlässliches POSIX-Rechte-Modell) Testfall.

**`RecoverableImageErrors` (neu, `internal`, Core):** Die drei Stellen, die
Bilder in großer Zahl decodieren (`ImageCache.PreloadAsync`,
`ImageViewerViewModel.LoadCurrentImageAsync`,
`ThumbnailBarViewModel.GenerateThumbnailsAsync`), hatten bisher identische,
aber unabhängig gepflegte `catch`-Filter
(`IOException or NotSupportedException or UnknownImageFormatException`).
Dabei fehlte `SixLabors.ImageSharp.InvalidImageContentException` (echte
Bildkorruption bei erkanntem Format - der Kernfall von "beschädigte Bilder")
komplett, da nur die Unterklasse `UnknownImageFormatException` (unbekanntes
Format) abgedeckt war. Beide Klassen leiten von der gemeinsamen Basisklasse
`ImageFormatException` ab; der neue, zentrale Helfer
`RecoverableImageErrors.IsRecoverable` prüft gegen diese Basisklasse plus
`IOException`/`UnauthorizedAccessException`/`NotSupportedException` und wird
von allen drei Stellen verwendet - eine Erweiterung der Klassifikation (z. B.
um ein weiteres RAW-Bibliotheks-Fehlerbild) muss jetzt nur noch an einer
Stelle gepflegt werden.

**Sichtbares Feedback statt stiller Leerfläche:** Vorher wurde bei einem
Ladefehler nur `CurrentImageBytes = null` gesetzt und eine Warnung geloggt -
für den Nutzer sah das aus wie eine leere/abgestürzte Anzeige. Jetzt setzt
`ImageViewerViewModel` zusätzlich `ImageLoadErrorMessage` (Deutsch, je nach
Fehlerart: "Datei nicht gefunden", "Kein Zugriff auf die Datei", oder
allgemein "beschädigt oder nicht unterstütztes Format"), die `ImageViewerView`
als zentral platzierten Text über der (leeren) Bildfläche anzeigt - passend
zu "Bildanzeige ist immer zentral" (SoftwareDesign.md). Analog bekommt
`ThumbnailItemViewModel.HasError` ein sichtbares ⚠-Overlay plus angepassten
Tooltip in der Thumbnail-Leiste, statt dass ein defektes Bild einfach
dauerhaft "noch am Laden" aussieht.

**`FolderScannerService`: ein nicht erreichbarer Ordner darf die anderen
nicht mitreißen:** Der Scan mehrerer ausgewählter Quellordner lief bisher in
einer gemeinsamen Schleife ohne Absicherung pro Ordner - ein einzelner
zwischenzeitlich unzugänglicher Ordner (Rechteänderung, externe Festplatte
während des Scans getrennt) hätte den gesamten Scan abgebrochen und auch die
in bereits erfolgreich gescannten Ordnern gefundenen Bilder verworfen. Der
Scan pro Ordner ist jetzt einzeln mit `try`/`catch` abgesichert; ein
fehlschlagender Ordner wird geloggt und übersprungen, der Rest läuft normal
weiter (`FolderScannerServiceTests.ScanAsync_WhenOneFolderIsInaccessible_...`).

## 26. Feinschliff: Unicode-Glyphen statt Icon-Asset-Pipeline; "Hilfe" zeigt echte, aktuelle Tastenkürzel

**Icons als Unicode-Glyphen statt Bild-Assets:** Für "Icons" (Roadmap Phase 17)
wurde bewusst keine Icon-Bibliothek/Asset-Pipeline eingeführt (z. B. SVG-Icons,
ein Icon-Font-Paket), sondern vorhandene Unicode-Symbole direkt als
Button-Präfix verwendet (📁 Quelle, ⬅ Links, ➡ Rechts, ✔ Anwenden,
⚙ Einstellungen, ❓ Hilfe) - genau das Muster, das das defekte-Thumbnail-
Warnsymbol (⚠, Phase 16) bereits etabliert hat. Das vermeidet zusätzliche
Abhängigkeiten und Asset-Verwaltung für einen rein kosmetischen Effekt, ist
aber auf jedem unterstützten Betriebssystem ohne Weiteres darstellbar (die
Fluent-Schriftart deckt diese Symbole ab). Eine spätere Umstellung auf
gezeichnete Icons würde nur `ToolbarView.axaml` betreffen, keine ViewModels.

**"Hilfe" zeigt die tatsächlich aktuellen Tastenkürzel, nicht eine feste
Liste:** Da Hotkeys seit Phase 15 vom Nutzer umbelegt werden können, wäre eine
im Text fest hinterlegte Tastenkürzel-Übersicht potenziell falsch, sobald
jemand eine Taste ändert. `ToolbarViewModel.ShowHelpCommand` liest deshalb bei
jedem Aufruf `IHotkeyService.Bindings` aus und baut die Übersicht live -
über den bereits vorhandenen `IMessageDialogService` (kein neues Fenster
nötig, siehe Punkt 9). Die deutschen Anzeigenamen pro `HotkeyAction` waren
bisher nur privat in `SettingsViewModel` hinterlegt; sie wurden nach
`HotkeyActionDisplayNames` (Core, `Models`) extrahiert, damit die
Hotkey-Einstellungen und die Hilfe-Übersicht nicht unabhängig voneinander
gepflegt werden und dadurch auseinanderlaufen können.

**`MainWindow` bekommt `MinWidth`/`MinHeight` (800×500):** Verhindert, dass
sich das Fenster auf eine Größe verkleinern lässt, bei der Toolbar,
Thumbnail-Leiste und Statusleiste nicht mehr sinnvoll nebeneinander Platz
haben. Die bereits bestehende Persistierung von `WindowWidth`/`WindowHeight`
(Phase 1) ist davon nicht betroffen - Avalonia begrenzt eine zu klein
gespeicherte Größe beim Start automatisch auf das Minimum, ohne dass
`App.axaml.cs` das selbst prüfen müsste.

**Animationen optimieren - Easing statt linearer Interpolation:** Die
Swipe- (`ImageViewerView.axaml`) und Thumbnail-Fade-in-Transitions
(`ThumbnailBarView.axaml`) hatten bisher keine explizite `Easing`-Angabe
(Avalonias Standard ist linear). Swipe-Versatz und -Rotation bekommen
`CubicEaseOut`, beide Opacity-Übergänge `QuadraticEaseOut` - das Bild
"schnappt" dadurch spürbar weniger mechanisch aus dem Fenster und
Thumbnails blenden sich weicher ein, ohne die Gesamtdauer (150 ms/200 ms)
zu ändern. Reine XAML-Änderung, keine ViewModel-Anpassung nötig, da die
Zeitsteuerung (Punkt 20) unverändert im ViewModel bleibt.

## 27. Version 1.0: Wurzelverzeichnis-`README.md` ergänzt; "Release" bedeutet hier ein verifizierter Release-Build, kein Git-Tag

**Code bereinigen / Kommentare:** Für den Abschluss wurde gezielt nach
typischen Aufräum-Kandidaten gesucht - übrig gebliebene `TODO`/`FIXME`-
Marker, `Console.Write*`-Debugausgaben, auskommentierter toter Code, leere
`catch`-Blöcke. Keiner davon wurde gefunden; `Nullable`+`ImplicitUsings`
sind in beiden `.csproj`-Dateien seit Phase 1 aktiv und liefern durchgehend
0 Warnungen, was ungenutzte Usings und Null-Sicherheitsprobleme bereits
laufend statt erst am Ende erzwungen hat.

**Dokumentation:** `docs/README.md` (seit Phase 1) indiziert nur die
Architekturentscheidungen; es fehlte ein Einstiegspunkt im
Projektwurzelverzeichnis für "was ist das, wie baue/teste/starte ich es".
Die neue `README.md` dort verlinkt auf `TODO.md` (Phasenstand) und
`docs/architecture-decisions.md` (Begründungen), statt deren Inhalt zu
duplizieren.

**"Release" ohne Git-Historie:** Das Projektverzeichnis enthält ein
initialisiertes, aber committer-loses Git-Repository (`git log` zeigt "No
commits yet") - es gibt also keine bestehende Historie, in die sich ein
"Version 1.0"-Tag im herkömmlichen Sinn einordnen ließe, und das Anlegen
des allerersten Commits ist eine Entscheidung, die dem Nutzer vorbehalten
bleibt (Commits werden hier nur auf explizite Anfrage erstellt). "Release"
wurde deshalb als Qualitäts-Checkpoint interpretiert statt als
Versionskontroll-Aktion: `dotnet build`/`dotnet test` wurden zusätzlich zur
gewohnten Debug-Konfiguration auch mit `-c Release` verifiziert (0
Warnungen/Fehler, 191/191 Tests grün) und die daraus gebaute
`PhotoSorter.App`-Binärdatei direkt gestartet und wieder sauber beendet -
Debug-only-Verifikation hätte Release-spezifische Probleme (z. B.
abweichendes Optimierungsverhalten) sonst bis zu einem tatsächlichen
Release-Vorgang verborgen.

## 28. Paketierung: self-contained statt Framework-dependent; rollender `latest`-Release; Windows separat von `scripts/package.sh`

**Self-contained statt Framework-dependent:** `scripts/package.sh` published
mit `--self-contained true`, ohne `PublishSingleFile` und ohne Trimming.
Self-contained bedeutet größere Pakete (~45–75 MB je Plattform statt
wenigen MB), aber niemand, der die App nur "einmal ausprobieren" möchte,
soll vorher eine passende .NET-Runtime installieren müssen.
`PublishSingleFile` wurde bewusst weggelassen, weil es bei Avalonia-Apps mit
nativen Abhängigkeiten (SkiaSharp) zu Entpack-Problemen zur Laufzeit führen
kann; Trimming wurde weggelassen, weil `JsonSettingsService`/
`JsonProjectFileSerializer` reflection-basierte `System.Text.Json`-
Serialisierung nutzen, die der Trimmer ohne zusätzliche Annotationen
kaputt-optimieren könnte. Ein einfacher, kopierbarer Ordner (bzw. ein
minimales `.app`-Bundle auf macOS) ist robuster als beides.

**Minimales, unsigniertes macOS-`.app`-Bundle:** `scripts/package.sh` baut
für `osx-*` kein bloßes Verzeichnis, sondern
`PhotoSorter.app/Contents/{MacOS,Resources}` plus ein statisches
`packaging/macos/Info.plist` (`CFBundleExecutable=PhotoSorter.App`, passend
zum tatsächlichen, unveränderten Binärnamen). Das ermöglicht "Rechtsklick →
Öffnen" statt eine nackte Unix-Binärdatei im Terminal starten zu müssen.
Ohne Code-Signatur/Notarisierung (dafür fehlt ein Apple-Developer-Zertifikat)
warnt Gatekeeper beim ersten Start – dokumentiert in `README.md`, nicht
softwareseitig umgangen.

**Windows-Paketierung läuft nicht über `scripts/package.sh`:** Auf
GitHub-`windows-latest`-Runnern ist weder `zip` noch ein verlässliches `zip`
in der mitgelieferten Git-Bash vorhanden (bestätigt: GitHub hat 7-Zip aus
dem Standard-Image entfernt, `zip` gab es dort nie). Der Windows-Zweig der
GitHub Action nutzt deshalb direkt PowerShells eingebautes
`Compress-Archive` statt eine plattformübergreifende Bash-Lösung zu
erzwingen. Lokal (`make package`/`make package-all`, ausgeführt auf einem
Mac) behandelt das Skript `win-x64` dagegen einfach wie `linux-x64` (dort
ist `zip` vorhanden) – die beiden Codepfade unterscheiden sich also bewusst
zwischen lokalem Build und CI, nicht aus Inkonsistenz, sondern weil `make`
ohnehin ein Unix-Werkzeug ist und die CI-Windows-Umgebung eine andere
Toolchain hat.

**Cross-RID-Publish von einer einzigen Maschine aus funktioniert:**
`dotnet publish -r <rid> --self-contained true` lädt das passende
Runtime-Pack für die Ziel-RID über NuGet, unabhängig von Host-Betriebssystem
und -Architektur (kein AOT, kein ReadyToRun, kein ARM-Cross-Toolchain
nötig). Dadurch kann `make package-all` alle vier Plattformen von einem
einzigen Mac aus bauen; die GitHub Action nutzt trotzdem eine
Runner-Matrix (je eine native Umgebung pro Ziel-Betriebssystem), weil der
Windows-Zweig ohnehin PowerShell statt Bash braucht.

**Rollender `latest`-Release statt Tag pro Push:** Auf Nutzerwunsch erzeugt
die GitHub Action keinen neuen Tag pro Push, sondern aktualisiert einen
einzigen Release/Tag `latest`. Wichtige Einschränkung von
`softprops/action-gh-release`: die Action verschiebt einen bereits
existierenden Tag NICHT automatisch auf den neuen Commit (`target_commitish`
wirkt nur bei der Ersterstellung). Der Workflow löscht deshalb vor jedem
Neu-Erstellen explizit den alten `latest`-Release samt Tag
(`gh release delete latest --cleanup-tag --yes || true` – das `|| true`
macht den allerersten Lauf ohne bestehenden Release zum No-op). Ein
`concurrency`-Guard auf Workflow-Ebene verhindert, dass zwei schnell
aufeinanderfolgende Pushes gleichzeitig um denselben Tag konkurrieren.

## 29. Windows-Paket bekommt doch `PublishSingleFile` – abweichend von Punkt 28, aber nur dort

Punkt 28 begründet den Verzicht auf `PublishSingleFile` für alle Plattformen
mit möglichen Native-Library-Entpack-Problemen (SkiaSharp). In der Praxis
zeigte sich aber: Ohne `.app`-Bundle-Wrapper wie auf macOS bleibt bei einem
reinen self-contained Windows-Publish ein Ordner mit über 100 losen DLLs
neben der `.exe` übrig – nach dem Entpacken ist für einen Nutzer nicht
erkennbar, welche Datei überhaupt gestartet werden soll. Deshalb bekommt
**nur der Windows-Zweig** (in `scripts/package.sh` für `win-*` und im
identischen Windows-Job in `.github/workflows/release.yml`) zusätzlich
`-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
-p:DebugType=none`: Das bündelt alle verwalteten Assemblies sowie die
nativen SkiaSharp/HarfBuzzSharp-Bibliotheken in eine einzige
`PhotoSorter.App.exe`, die sich beim ersten Start selbst in einen
Temp-Ordner entpackt – nach dem Entpacken des Release-Zips liegt dort
buchstäblich nur noch die eine `.exe`.

**Warum nicht auch für macOS/Linux:** macOS hat mit dem `.app`-Bundle
(Punkt 27/28) bereits eine native, getestete Lösung für "eine Sache zum
Öffnen"; Linux-Nutzer, die eine gepackte `.zip` entpacken und `chmod +x`
ausführen, sind mit dieser Arbeitsweise vertraut. Beide wurden vom Nutzer
nicht als Problem gemeldet – eine Änderung dort hätte nur unnötiges Risiko
auf bereits funktionierenden Plattformen bedeutet (siehe Punkt 28 zur
Skepsis gegenüber `PublishSingleFile` bei Avalonia/Skia).

**Übrig gebliebene `.pdb`-Dateien:** `-p:DebugType=none` unterdrückt nur die
projekteigene `PhotoSorter.App.pdb`; die beiden nativen
`libSkiaSharp.pdb`/`libHarfBuzzSharp.pdb` (zusammen größer als die `.exe`
selbst, ca. 100 MB) kommen als Content-Item aus den jeweiligen NuGet-Paketen
und werden deshalb explizit per `rm`/`Remove-Item` aus dem Publish-Ordner
entfernt, bevor gezippt wird – reine Debug-Symbole, zur Laufzeit nie
benötigt.

**Nicht selbst auf echtem Windows verifiziert:** Der Build wurde lokal
(cross-publish von einem Mac aus) erfolgreich erzeugt und das
Ordner-Ergebnis geprüft (genau eine `.exe`), aber mangels Windows-Maschine
in dieser Umgebung nicht tatsächlich gestartet. `PublishSingleFile` +
`IncludeNativeLibrariesForSelfExtract` für self-contained Avalonia-Apps auf
Windows ist ein in der Avalonia-Community verbreitetes, dokumentiertes
Muster, aber der erste echte Praxistest erfolgt beim nächsten `latest`-Build
durch den Nutzer selbst.
