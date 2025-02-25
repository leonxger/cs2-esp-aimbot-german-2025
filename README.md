# CS2 Multihack – Game Hacking Overlay in C#

[![.NET 6](https://img.shields.io/badge/.NET-6-blue)](https://dotnet.microsoft.com/download/dotnet/6.0)

CS2 Multihack ist ein Beispielprojekt, das zeigt, wie man ein transparentes Overlay für ein Spiel erstellt, einschließlich ESP (Anzeige von Spielerinformationen) und einem Aimbot, der automatisch auf Gegner zielt. Der Code ist ausführlich kommentiert, sodass auch Anfänger im Bereich Game Hacking und Programmierung ohne tiefe mathematische Vorkenntnisse nachvollziehen können, wie die einzelnen Berechnungen ablaufen.

Der Code basiert auf der wundervollen Videoreihe des Youtubers "swedz c#"
https://www.youtube.com/watch?v=0Fjh7Y4aYxo&list=PLTFAYsPdUBH3AfVHujOe94rdcAI2KCQsd

---

## Inhaltsverzeichnis

- [Features](#features)
- [Voraussetzungen](#voraussetzungen)
- [Installation](#installation)
- [Projektkonfiguration](#projektkonfiguration)
- [Anwendung](#anwendung)
- [Code-Erklärung](#code-erklärung)

---

## Features

- **ESP (Extra Sensory Perception):**
  - Anzeige von grafischen Elementen wie Boxen, Linien, Punkten und Gesundheitsbalken um Spieler.
- **Aimbot:**
  - Automatisches Zielen auf den nächsten Gegner, ausgelöst durch die X2-Maustaste (Tastencode 0x06).
- **Detaillierte Kommentare:**
  - Jede mathematische Operation wird erklärt – ideal als Lehrmaterial für Einsteiger im Game Hacking.
- **Modulares Design:**
  - Implementiert in .NET 6.0 mit Nutzung von ImGui.NET, ClickableTransparentOverlay und weiteren nützlichen NuGet-Paketen.

---

## Voraussetzungen

- **.NET 6.0 SDK**
  - [Download .NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)

- **NuGet-Pakete**
  - Dieses Projekt benötigt die folgenden NuGet-Pakete:
    - `ClickableTransparentOverlay` v6.2.1
    - `ImGui.NET` v1.89.7.1
    - `SixLabors.ImageSharp` v3.1.6
    - `swed64` v1.0.5
    - `Veldrid.ImGui` v5.72.0
    - `Vortice.Mathematics` v1.6.2

  **NuGet Installationsbefehle:**

  ```powershell
  Install-Package ClickableTransparentOverlay -Version 6.2.1
  Install-Package ImGui.NET -Version 1.89.7.1
  Install-Package SixLabors.ImageSharp -Version 3.1.6
  Install-Package swed64 -Version 1.0.5
  Install-Package Veldrid.ImGui -Version 5.72.0
  Install-Package Vortice.Mathematics -Version 1.6.2
  ```

## Installation

Repository klonen:

```bash
git clone https://github.com/dein-github-benutzername/CS2MULTI.git
cd CS2MULTI
```

Projekt bauen:

```bash
dotnet build
```

Anwendung starten:

```bash
dotnet run
```

## Projektkonfiguration

Stelle sicher, dass in den Projekteinstellungen das PlatformTarget auf x64 (64 Bit) gesetzt ist. Hier ist die vollständige .csproj-Datei als Referenz:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>CS2_Multihack_German</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ClickableTransparentOverlay" Version="6.2.1" />
    <PackageReference Include="ImGui.NET" Version="1.89.7.1" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.6" />
    <PackageReference Include="swed64" Version="1.0.5" />
    <PackageReference Include="Veldrid.ImGui" Version="5.72.0" />
    <PackageReference Include="Vortice.Mathematics" Version="1.6.2" />
  </ItemGroup>

</Project>
```

## Anwendung

Overlay starten:

- Beim Ausführen des Programms wird ein transparentes Overlay über dem Spiel-Fenster angezeigt.

ESP aktivieren:

- Über das eingeblendete Menü kannst du ESP-Elemente wie Boxen, Linien, Punkte und Gesundheitsbalken ein- oder ausschalten.

Aimbot nutzen:

- Drücke die X2-Maustaste (Tastencode 0x06), um den Aimbot zu aktivieren. Der Aimbot berechnet automatisch die Zielwinkel (Yaw und Pitch) zum nächstgelegenen Gegner und passt die Kameraausrichtung im Spiel an.

Konfiguration:

- Im Menü kannst du Einstellungen wie Farben, Zielkriterien (z. B. nächst zum Fadenkreuz) und Sichtbarkeitsoptionen individuell anpassen.

## Code-Erklärung

Der Quellcode ist ausführlich kommentiert und erklärt alle mathematischen Operationen im Detail. Beispiele aus dem Code:

World-to-Screen Konvertierung:

- Die ViewMatrix wandelt 3D-Koordinaten in 2D-Koordinaten um. Beispiel:
  ```csharp
  float screenW = (matrix.m41 * worldPos.X) + (matrix.m42 * worldPos.Y) + (matrix.m43 * worldPos.Z) + matrix.m44;
  ```
  Hier wird z. B. bei m41 = 0.2 und worldPos.X = 100 folgendermaßen gerechnet:
  0.2 * 100 = 20.

Zielwinkelberechnung (Yaw & Pitch):

- Der Code verwendet Math.Atan2 zur Berechnung von Winkelwerten. Beispiel:
  - Wenn ΔX = 50 und ΔY = 50, liefert Atan2(50, 50) ca. 0,785 Radiant, was ca. 45° entspricht.
  - Der Pitch wird anhand der Differenz in Z und der horizontalen Distanz berechnet.

Diese detaillierten Erklärungen im Code helfen dir, die Funktionsweise des Overlays und des Aimbots genau zu verstehen und dienen als ideale Lernressource.
