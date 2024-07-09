# BP 19/20 - Entwicklung einer Multiplayer-HoloLens-AR-Anwendung

## Struktur
### Framework Ordner
|   |   |
|---|---|
| Code:                                 | ```Framework/Runtime/NetLib/```           |
| 3rd-party libraries:                  | ```Framework/Runtime/Plugins/<name>/```   |
| Tests:                                | ```Framework/Tests/Runtime/Standard/```   |
| Tests, die Holographic-Mode brauchen: | ```FrameWork/Tests/Runtime/Holographic/```|
   
### Client/Server Unity Projekte
Keine Exporte/Importe mehr nötig. Das Framework wird über die ```WorldSaver[Client|Server]/Packages/manifest.json``` automatisch eingebunden.

Im Unity-Editor wird das Framework im Hierarchiefenster unter 
```
-Assets/
   ...
-Packages/
   ...
   NetLib/      <-- hier
   ...
``` 
angezeigt.

Die von Unity generierte Visual Studio Solution beinhaltet separate Projekte für das Framework: ```NetLib``` und ```NetLib.Tests``` (```NetLib.Editor``` ist irrevelant, das wird nur für die .sln und .csproj Generierung verwendet). 

Alle Änderungen in diesen VS-Projekten oder im Unity-Projekt(Client/Server) unter ```Packages/NetLib``` sind direkt im ```Framework/``` Ordner sichtbar, und damit auch im jeweils anderen Unity-Projekt(Client/Server).

## Einrichtung
### Resharper
Visual Studio Plugin von JetBrains (statische Codeanalyse, Codestilformatierung, ...)

#### Installation
- JetBrains Student Account registrieren: https://www.jetbrains.com/shop/eform/students
- Download Installer (aktuelle Version 2019.3.1): https://www.jetbrains.com/de-de/resharper/download/
- Im Installer: ReSharper 2019.3.1 installieren (die anderen Sachen brauchen wir nicht)
- In Visual Studio: Im Lizenzfenster mit JetBrains Account anmelden (geht automatisch auf, ansonsten Extensions->ReSharper->Help->Licence Information)
- In Visual Studio: Extensions->ReSharper->Extension Manager-> 'Unity Support' plugin installieren

#### Codestil Einstellungen
Globale Codestil Einstellungen werden automatisch aus der ```*.sln.DotSettings``` Datei geladen.

#### Workaround für Unity
Unity generiert für das WorldSaverClient Projekt neben den GameAssembly und NetLib VS-Projekten auch noch ein GameAssembly.Player Projekt (hat irgendwas mit dem Build Target zu tun, ist aber egal). Dieses Projekt benutzt ```C# 6``` während die normalen Projekte ```C# 7``` benutzen. Wenn wir im GameAssembly Projekt ```C# 7``` Features benutzen, dann erkennt ReSharper das im GameAssembly.Player Projekt als Fehler (das Player Projekt hat alle Dateien aus GameAssembly). 

Um diese Fehler zu umgehen, muss man in Visual Studio das GameAssembly.Player Projekt ausblenden: Rechtsklick auf GameAssembly.Player->Unload Project

### Unity
- Verwendete Version: **2019.2.11f**
- **Hololens Simulator** (Client): ```Window->XR->Holographic Emulation``` und dann ```Emulation Mode->Simulate in Editor```
- **Test Runner** (Client oder Server): ```Window->General->Test Runner```, dann ```Play Mode```, Tests aus dem Framework sollten automatisch erkannt werden 

### Githook
Mit dem Githook-Skript ```.githooks/pre-push``` werden alle Unit Tests aus der Kategorie "Standard" automatisch vor jedem Push ausgeführt. Wenn dabei ein Test fehlschlägt, dann wird der Push abgebrochen und man bekommt stattdessen eine Fehlermeldung ausgegeben.

#### Unity Workaround
Da man die Unity Tests nur in einem Projekt ausführen kann, wird für diesen Githook das WorldSaverClient-Projekt über die Konsole ausgeführt. Das heißt leider auch, dass das Projekt nicht offen sein kann, wenn man einen Push ausführen will, weil immmer nur eine Instanz eines Projekts geöffnet sein kann.

#### Einrichtung
Es gibt 2 Möglichkeiten, das Skript in Git einzubinden:

- Die Datei ```.githooks/pre-push``` in den Ordner ```.git/hooks/``` kopieren
- Den git-command ```git config core.hooksPath .githooks``` im Top-Level Ordner ausführen

Falls ihr Unity nicht im Standardpfad installiert habt, dann müsst ihr Variante 1 wählen und in Zeile 3 der kopierten Datei den Installationspfad anpassen.
