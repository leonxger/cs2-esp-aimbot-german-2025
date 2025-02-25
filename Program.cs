using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using ImGuiNET;
using ClickableTransparentOverlay;
using CS2_Multihack;
using Swed64;

namespace CS2MULTI
{
    // Diese Klasse enthält Windows-Funktionen, die uns helfen, Fensterinformationen
    // zu erhalten und Tastendrücke zu prüfen. Diese Informationen sind wichtig, um
    // das Overlay korrekt zu platzieren und Funktionen wie den Aimbot zu aktivieren.
    internal static class NativeMethods
    {
        /// <summary>
        /// Ruft die Position und Größe eines Fensters ab.
        /// Beispiel: Startet das Fenster bei (100,50) mit einer Breite von 800 und einer Höhe von 600,
        /// so wird ein Rechteck zurückgegeben: Left=100, Top=50, Right=900, Bottom=650.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        /// <summary>
        /// Prüft, ob eine bestimmte Taste gedrückt wird.
        /// Hier wird 0x06 verwendet, was die X2-Maustaste repräsentiert.
        /// Liefert einen negativen Wert, wenn die Taste (X2-Maustaste) aktiv ist.
        /// Beispiel: Wenn du die X2-Maustaste drückst, wird der Rückgabewert kleiner als 0.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }

    // Diese Struktur definiert ein Rechteck, das z. B. die Fensterkoordinaten beschreibt.
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;   // X-Koordinate der linken Seite
        public int Top;    // Y-Koordinate der oberen Seite
        public int Right;  // X-Koordinate der rechten Seite
        public int Bottom; // Y-Koordinate der unteren Seite
    }

    // Diese Klasse erweitert das Overlay und implementiert alle Funktionen:
    // Das Auslesen von Spielinformationen, die Umrechnung von 3D- in 2D-Koordinaten
    // (wichtig für ESP) und den Aimbot, der automatisch auf Gegner zielt.
    class Program : Overlay
    {
        // Tastencode für die X2-Maustaste (0x06 repräsentiert X2-Maustaste)
        private const int AIMBOT_KEY = 0x06;

        // Objekt, das den Spielspeicher liest und schreibt.
        private Swed memoryReader = new Swed("cs2");
        // Enthält Offsets (Speicheradressen) für wichtige Spielvariablen.
        private Offsets memoryOffsets = new Offsets();
        // ImGui-Zeichenliste zum Zeichnen der Grafiken.
        private ImDrawListPtr imDrawList;

        // Listen für alle Spieler im Spiel: eigene, verbündete und Gegner.
        private List<Entity> entities = new List<Entity>();
        private List<Entity> allyEntities = new List<Entity>();
        private List<Entity> enemyEntities = new List<Entity>();

        // Repräsentiert den lokalen Spieler (deine Spielfigur).
        private Entity localPlayerEntity = new Entity();

        // Basisadresse des Client-Moduls (z. B. "client.dll").
        private IntPtr clientModuleBase;

        // Vektor, der den "headOffset" definiert (20 Einheiten in Z-Richtung).
        // Beispiel: Liegt ein Gegner bei (100,200,50), so wird durch Subtraktion 20 Einheiten in Z-Richtung (50-20) der "Kopf" getroffen.
        private readonly Vector3 headOffset = new Vector3(0, 0, 20);

        // Farben in RGBA: Teamfarbe (blau), Gegnerfarbe (rot) und Farbe für Health-Text (schwarz).
        private Vector4 teamColor = new Vector4(0, 0, 1, 1);
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1);
        private Vector4 healthTextColor = new Vector4(0, 0, 0, 1);

        // Fensterposition und -größe, die später aus dem aktiven Fenster ausgelesen werden.
        private Vector2 windowPosition = new Vector2(0, 0);
        private Vector2 windowDimensions = new Vector2(1920, 1080);

        // Ausgangspunkt für Linien: z. B. die Mitte unten im Fenster.
        private Vector2 lineStartPoint = new Vector2(1920 / 2, 1080);
        // Bildschirmmitte, wichtig für Zielberechnungen.
        private Vector2 windowCenterPoint = new Vector2(1920 / 2, 1080 / 2);

        // Schalter, die über das Menü ein- oder ausgeschaltet werden können.
        private bool enableEsp = true;
        private bool enableAimbot = true;
        private bool enableAimbotCrosshair = true;
        private bool showAllyLine = true;
        private bool showAllyBox = true;
        private bool showAllyDot = true;
        private bool showAllyHealthBar = true;
        private bool showAllyDistance = true;
        private bool showEnemyLine = true;
        private bool showEnemyBox = true;
        private bool showEnemyDot = true;
        private bool showEnemyHealthBar = true;
        private bool showEnemyDistance = true;

        #region Rendering und Einfache Mathematische Transformationen

        /// <summary>
        /// Diese Methode wird jeden Frame aufgerufen, um alle grafischen Elemente zu zeichnen:
        /// Menü, Overlay und ESP (z. B. Boxen, Linien).
        /// </summary>
        protected override void Render()
        {
            DrawMenu();    // Zeichnet das Menü, in dem du Funktionen aktivieren kannst.
            DrawOverlay(); // Positioniert das Overlay exakt über dem Spiel.
            DrawEsp();     // Zeichnet die grafischen ESP-Elemente für Spieler.
            ImGui.End();   // Beendet den ImGui-Zeichenblock.
        }

        /// <summary>
        /// Liest die ViewMatrix aus dem Spielspeicher, die angibt, wie 3D-Koordinaten in 2D-Koordinaten umgerechnet werden.
        /// Beispiel: Die ViewMatrix besteht aus 16 Zahlen, die in einer 4x4-Anordnung vorliegen.
        /// </summary>
        private ViewMatrix ReadViewMatrix(IntPtr matrixAddress)
        {
            // Lies 16 Float-Werte, die einer 4x4-Matrix entsprechen.
            float[] matrixValues = memoryReader.ReadMatrix(matrixAddress);
            var viewMatrix = new ViewMatrix
            {
                m11 = matrixValues[0],  // Beispiel: Falls matrixValues[0] = 0.9, dann m11 = 0.9
                m12 = matrixValues[1],
                m13 = matrixValues[2],
                m14 = matrixValues[3],
                m21 = matrixValues[4],
                m22 = matrixValues[5],
                m23 = matrixValues[6],
                m24 = matrixValues[7],
                m31 = matrixValues[8],
                m32 = matrixValues[9],
                m33 = matrixValues[10],
                m34 = matrixValues[11],
                m41 = matrixValues[12],
                m42 = matrixValues[13],
                m43 = matrixValues[14],
                m44 = matrixValues[15]
            };
            return viewMatrix;
        }

        /// <summary>
        /// Wandelt eine 3D-Position in eine 2D-Position um, sodass sie auf dem Bildschirm angezeigt werden kann.
        /// Hier kommen mehrere mathematische Operationen zum Einsatz, die wir mit Beispielen erklären:
        /// </summary>
        private Vector2 ConvertWorldToScreen(ViewMatrix matrix, Vector3 worldPos, int width, int height)
        {
            // Berechne screenW als Summe der Produkte von Matrixwerten und Weltkoordinaten:
            // screenW = (m41 * X) + (m42 * Y) + (m43 * Z) + m44.
            // Beispiel: Wenn m41 = 0.2, worldPos.X = 100, dann trägt (0.2*100)=20 bei.
            float screenW = (matrix.m41 * worldPos.X)    // Beispiel: 0.2 * 100 = 20
                            + (matrix.m42 * worldPos.Y)    // Beispiel: 0.3 * 200 = 60
                            + (matrix.m43 * worldPos.Z)    // Beispiel: 0.1 * 50  = 5
                            + matrix.m44;                  // Beispiel: m44 = 1 → 1
            // Falls screenW > 0.001, wird der Punkt vor der Kamera angenommen.
            if (screenW > 0.001f)
            {
                // Berechne screenX: (m11 * X) + (m12 * Y) + (m13 * Z) + m14.
                // Beispiel: m11=1, worldPos.X=100 ergibt 1*100=100, etc.
                float screenX = (matrix.m11 * worldPos.X)    // Beispiel: 1 * 100 = 100
                              + (matrix.m12 * worldPos.Y)    // Beispiel: 0 * 200 = 0
                              + (matrix.m13 * worldPos.Z)    // Beispiel: 0 * 50  = 0
                              + matrix.m14;                  // Beispiel: m14 = 0 → 0
                // Berechne screenY: (m21 * X) + (m22 * Y) + (m23 * Z) + m24.
                float screenY = (matrix.m21 * worldPos.X)    // Beispiel: 0 * 100 = 0
                              + (matrix.m22 * worldPos.Y)    // Beispiel: 1 * 200 = 200
                              + (matrix.m23 * worldPos.Z)    // Beispiel: 0 * 50  = 0
                              + matrix.m24;                  // Beispiel: m24 = 0 → 0

                // Bestimme die Mitte des Bildschirms: Teilen der Breite und Höhe durch 2.
                float camX = width / 2f;  // Beispiel: 1920/2 = 960
                float camY = height / 2f; // Beispiel: 1080/2 = 540

                // Berechne finalX:
                // finalX = camX + (camX * screenX / screenW)
                // Beispiel: camX = 960, screenX = 100, screenW = 2 → (960 * 100 / 2) = 48000; finalX = 960 + 48000
                // (In der Praxis ergeben die Matrixwerte kleinere Zahlen, sodass das Ergebnis realistisch bleibt.)
                float finalX = camX + (camX * screenX / screenW);
                // Berechne finalY:
                // finalY = camY - (camY * screenY / screenW)
                // Beispiel: camY = 540, screenY = 50, screenW = 2 → (540 * 50 / 2) = 13500; finalY = 540 - 13500
                // Hinweis: Bei Y wird subtrahiert, da die Y-Achse im Bildschirmkoordinatensystem oft umgekehrt verläuft.
                float finalY = camY - (camY * screenY / screenW);
                return new Vector2(finalX, finalY);
            }
            else
            {
                // Falls der Punkt hinter der Kamera liegt, wird ein ungültiger Wert zurückgegeben.
                return new Vector2(-99, -99);
            }
        }

        /// <summary>
        /// Berechnet die Entfernung zwischen zwei 2D-Punkten auf dem Bildschirm.
        /// Hier wird die klassische Distanzformel genutzt: √((ΔX)²+(ΔY)²).
        /// Beispiel: Zwischen (0,0) und (3,4) beträgt die Entfernung √(9+16)=√25=5.
        /// </summary>
        private float CalculateScreenDistance(Vector2 point1, Vector2 point2)
        {
            return Vector2.Distance(point1, point2);
        }

        /// <summary>
        /// Berechnet die 3D-Entfernung zwischen zwei Punkten in der Spielwelt.
        /// Die Formel lautet: √((ΔX)²+(ΔY)²+(ΔZ)²).
        /// Beispiel: Zwischen (1,2,3) und (4,6,3) beträgt ΔX=3, ΔY=4, ΔZ=0, also √(9+16+0)=√25=5.
        /// </summary>
        private float Calculate3dDistance(Vector3 pos1, Vector3 pos2)
        {
            return Vector3.Distance(pos1, pos2);
        }

        /// <summary>
        /// Berechnet die Zielwinkel (Yaw und Pitch) von einem Startpunkt zu einem Zielpunkt.
        /// Dabei:
        /// - Yaw: Bestimmt den horizontalen Drehwinkel.
        ///   Beispiel: Liegt das Ziel direkt rechts, beträgt der Winkel ca. 90°.
        /// - Pitch: Bestimmt den vertikalen Winkel.
        ///   Beispiel: Liegt das Ziel höher, muss der Winkel nach oben (negativ) korrigiert werden.
        /// </summary>
        private Vector3 CalculateAimAngles(Vector3 from, Vector3 to)
        {
            // Differenz in X: ΔX = Ziel.X - Start.X.
            float deltaX = to.X - from.X; // Beispiel: 150 - 100 = 50

            // Differenz in Y: ΔY = Ziel.Y - Start.Y.
            float deltaY = to.Y - from.Y; // Beispiel: 250 - 200 = 50

            // Differenz in Z: ΔZ = Ziel.Z - Start.Z.
            float deltaZ = to.Z - from.Z; // Beispiel: 80 - 60 = 20

            // Berechne den horizontalen Winkel (Yaw) in Radiant.
            // Atan2(ΔY, ΔX) gibt den Winkel im Bogenmaß zurück.
            float yawRadians = (float)Math.Atan2(deltaY, deltaX);
            // Beispiel: Atan2(50, 50) ≈ 0.785 Rad, was ca. 45° entspricht.

            // Konvertiere den Winkel von Radiant zu Grad: (180/π) Multiplikator.
            float yawDegrees = yawRadians * (180f / (float)Math.PI);
            // Beispiel: 0.785 * (180/π) ≈ 45°

            // Berechne die horizontale Distanz als Basis für den Pitch.
            double horizontalDistance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            // Beispiel: √(50²+50²)=√(2500+2500)=√5000 ≈ 70.71

            // Berechne den vertikalen Winkel (Pitch) in Radiant.
            float pitchRadians = (float)Math.Atan2(deltaZ, horizontalDistance);
            // Beispiel: Atan2(20,70.71) ≈ 0.278 Rad

            // Konvertiere Pitch zu Grad und kehre das Vorzeichen um.
            float pitchDegrees = -pitchRadians * (180f / (float)Math.PI);
            // Beispiel: -0.278 * (180/π) ≈ -15.93°

            // Roll wird hier nicht genutzt (0).
            return new Vector3(yawDegrees, pitchDegrees, 0);
        }

        /// <summary>
        /// Überprüft, ob ein 2D-Punkt innerhalb des sichtbaren Bereichs des Fensters liegt.
        /// </summary>
        private bool IsPointOnScreen(Vector2 point)
        {
            return point.X > windowPosition.X && point.X < windowPosition.X + windowDimensions.X &&
                   point.Y > windowPosition.Y && point.Y < windowPosition.Y + windowDimensions.Y;
        }

        #endregion

        #region Overlay-Menü und Zeichenfunktionen

        /// <summary>
        /// Zeichnet ein Menü, in dem Funktionen wie ESP und Aimbot aktiviert werden können.
        /// </summary>
        private void DrawMenu()
        {
            ImGui.Begin("CS2 Multihack");
            if (ImGui.BeginTabBar("Tabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Checkbox("ESP", ref enableEsp);
                    ImGui.Checkbox("Aimbot", ref enableAimbot);
                    if (enableAimbot)
                    {
                        ImGui.SameLine();
                        ImGui.Checkbox("Closest to crosshair", ref enableAimbotCrosshair);
                    }
                    else
                    {
                        // Wenn der Aimbot deaktiviert ist, wird auch die Crosshair-Zielwahl ausgeschaltet.
                        enableAimbotCrosshair = false;
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Colors"))
                {
                    ImGui.ColorPicker4("Team Color", ref teamColor);
                    ImGui.Checkbox("Ally Line", ref showAllyLine);
                    ImGui.Checkbox("Ally Box", ref showAllyBox);
                    ImGui.Checkbox("Ally Dot", ref showAllyDot);
                    ImGui.Checkbox("Ally Healthbar", ref showAllyHealthBar);
                    ImGui.Checkbox("Ally Distance", ref showAllyDistance);
                    ImGui.ColorPicker4("Enemy Color", ref enemyColor);
                    ImGui.Checkbox("Enemy Line", ref showEnemyLine);
                    ImGui.Checkbox("Enemy Box", ref showEnemyBox);
                    ImGui.Checkbox("Enemy Dot", ref showEnemyDot);
                    ImGui.Checkbox("Enemy Healthbar", ref showEnemyHealthBar);
                    ImGui.Checkbox("Enemy Distance", ref showEnemyDistance);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }

        /// <summary>
        /// Konfiguriert das Overlay-Fenster, sodass es exakt über dem Spiel angezeigt wird.
        /// </summary>
        private void DrawOverlay()
        {
            ImGui.SetNextWindowSize(windowDimensions);
            ImGui.SetNextWindowPos(windowPosition);
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration |
                                     ImGuiWindowFlags.NoBackground |
                                     ImGuiWindowFlags.NoBringToFrontOnFocus |
                                     ImGuiWindowFlags.NoMove |
                                     ImGuiWindowFlags.NoInputs |
                                     ImGuiWindowFlags.NoCollapse |
                                     ImGuiWindowFlags.NoScrollbar |
                                     ImGuiWindowFlags.NoScrollWithMouse);
        }

        /// <summary>
        /// Zeichnet grafische Elemente (ESP) für ein einzelnes Entity (Spieler).
        /// Hier werden die mathematischen Berechnungen genutzt, um Boxen, Linien und Healthbars an den korrekten Positionen darzustellen.
        /// </summary>
        private void DrawEntityOverlay(Entity entity, Vector4 color, bool drawLine, bool drawBox, bool drawDot, bool drawHealthBar, bool drawDistance)
        {
            if (IsPointOnScreen(entity.OriginScreenPos))
            {
                // Konvertiere die Farbe in das Format, das ImGui benötigt.
                uint uintColor = ImGui.ColorConvertFloat4ToU32(color);

                // Berechne das Verhältnis der Gesundheit: (z.B. 75/100 = 0.75).
                float healthRatio = entity.Health / 100f;

                // Erstelle eine Farbe für die Healthbar, die von Rot (niedrige Gesundheit) zu Grün (hohe Gesundheit) wechselt.
                uint uintHealthBarColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1 - healthRatio, healthRatio, 0, 1));
                // Beispiel: Bei 75% Gesundheit: 1 - 0.75 = 0.25 → relativ wenig Rot, viel Grün.

                // Berechne die Höhe der Box, die das Entity umrahmt.
                // Differenz der Y-Koordinaten zwischen der Basis (Füße) und dem Kopf.
                float boxHeight = entity.OriginScreenPos.Y - entity.AbsoluteScreenPos.Y;
                // Beispiel: Wenn OriginScreenPos.Y = 500 und AbsoluteScreenPos.Y = 400, dann boxHeight = 100.

                // Berechne die Boxbreite als die Hälfte der Höhe.
                float boxWidth = boxHeight / 2f;
                // Beispiel: 100 / 2 = 50.

                // Bestimme die linke obere Ecke der Box:
                // Subtrahiere die Breite von der X-Koordinate der Kopfposition.
                Vector2 boxTopLeft = new Vector2(entity.AbsoluteScreenPos.X - boxWidth, entity.AbsoluteScreenPos.Y);
                // Beispiel: Wenn AbsoluteScreenPos.X = 600 und boxWidth = 50, dann topLeft.X = 550.

                // Bestimme die rechte untere Ecke der Box:
                // Addiere die Breite zur X-Koordinate der Basisposition.
                Vector2 boxBottomRight = new Vector2(entity.OriginScreenPos.X + boxWidth, entity.OriginScreenPos.Y);
                // Beispiel: Wenn OriginScreenPos.X = 600 und boxWidth = 50, dann bottomRight.X = 650.

                // Berechne die Höhe der Healthbar als Produkt von boxHeight und dem Gesundheitsverhältnis.
                float healthBarHeight = boxHeight * healthRatio;
                // Beispiel: 100 * 0.75 = 75.

                // Positioniere die Healthbar links neben der Box.
                Vector2 healthBarTopLeft = new Vector2(boxTopLeft.X - 4, entity.OriginScreenPos.Y - healthBarHeight);
                // Beispiel: Wenn boxTopLeft.X = 550, dann healthBarTopLeft.X = 546.
                Vector2 healthBarBottomRight = new Vector2(boxTopLeft.X - 2, entity.OriginScreenPos.Y);
                // Beispiel: Wenn boxTopLeft.X = 550, dann healthBarBottomRight.X = 548.

                // Zeichne je nach Einstellung die grafischen Elemente:
                if (drawLine)
                    imDrawList.AddLine(lineStartPoint, entity.OriginScreenPos, uintColor, 3);
                if (drawBox)
                    imDrawList.AddRect(boxTopLeft, boxBottomRight, uintColor, 3);
                if (drawDot)
                    imDrawList.AddCircleFilled(entity.OriginScreenPos, 5, uintColor);
                if (drawHealthBar)
                {
                    imDrawList.AddText(entity.OriginScreenPos, uintHealthBarColor, $"HP: {entity.Health}");
                    imDrawList.AddRectFilled(healthBarTopLeft, healthBarBottomRight, uintHealthBarColor);
                }
            }
        }

        /// <summary>
        /// Zeichnet alle ESP-Elemente für alle Entities.
        /// Dabei wird anhand der Team-ID unterschieden, ob ein Entity als Verbündeter oder Gegner dargestellt wird.
        /// </summary>
        private void DrawEsp()
        {
            imDrawList = ImGui.GetWindowDrawList();
            if (enableEsp)
            {
                try
                {
                    foreach (var entity in entities)
                    {
                        if (entity == null)
                            continue;
                        if (entity.TeamId == localPlayerEntity.TeamId)
                        {
                            DrawEntityOverlay(entity, teamColor, showAllyLine, showAllyBox, showAllyDot, showAllyHealthBar, showAllyDistance);
                        }
                        else
                        {
                            DrawEntityOverlay(entity, enemyColor, showEnemyLine, showEnemyBox, showEnemyDot, showEnemyHealthBar, showEnemyDistance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error drawing ESP: " + ex.Message);
                }
            }
        }

        #endregion

        #region Hauptlogik und Aktualisierung der Entity-Daten

        /// <summary>
        /// Führt den Aimbot aus: Wird die X2-Maustaste (0x06) gedrückt und der Aimbot ist aktiviert,
        /// sucht diese Methode das nächste Ziel und berechnet die Zielwinkel.
        /// </summary>
        private void ExecuteAimbot()
        {
            // Prüfe, ob die X2-Maustaste gedrückt ist (Rückgabewert < 0) und ob der Aimbot aktiviert ist.
            if (NativeMethods.GetAsyncKeyState(AIMBOT_KEY) < 0 && enableAimbot)
            {
                if (enemyEntities.Count > 0)
                {
                    // Berechne das Ziel, indem vom Gegner die Kopfverschiebung (headOffset) abgezogen wird.
                    // Beispiel: Gegnerposition = (200,300,80); Kopf = (200,300,80-20) = (200,300,60)
                    Vector3 targetPosition = enemyEntities[0].Origin - headOffset;
                    // Subtraktion: Für jeden Komponenten (X, Y, Z) wird 0, 0, 20 abgezogen.

                    // Berechne die nötigen Zielwinkel (Yaw, Pitch) vom lokalen Spieler zur Zielposition.
                    Vector3 aimAngles = CalculateAimAngles(localPlayerEntity.Origin, targetPosition);
                    // Beispiel: Wenn der lokale Spieler bei (100,200,60) steht und Ziel bei (200,300,60),
                    // könnte der berechnete Yaw ca. 45° und Pitch ca. 0° betragen.

                    // Schreibe die berechneten Winkel in den Spielspeicher, um die Kamera auszurichten.
                    AimAtTarget(aimAngles);
                }
            }
        }

        /// <summary>
        /// Schreibt die berechneten Zielwinkel (Pitch und Yaw) in den Spielspeicher.
        /// Dadurch wird dem Spiel signalisiert, die Kamera in die entsprechende Richtung zu drehen.
        /// </summary>
        private void AimAtTarget(Vector3 angles)
        {
            // Schreibe den Pitch-Wert (angles.Y) an die entsprechende Speicheradresse.
            memoryReader.WriteFloat(clientModuleBase, memoryOffsets.ViewAnglesOffset, angles.Y);
            // Schreibe den Yaw-Wert (angles.X) an die Adresse, die 4 Byte weiterliegt.
            memoryReader.WriteFloat(clientModuleBase, memoryOffsets.ViewAnglesOffset + 0x4, angles.X);
        }

        /// <summary>
        /// Aktualisiert die Daten aller Entities im Spiel.
        /// </summary>
        private void ReloadEntitiesData(ViewMatrix currentViewMatrix)
        {
            entities.Clear();
            allyEntities.Clear();
            enemyEntities.Clear();

            // Lese die Adresse des lokalen Spielers.
            localPlayerEntity.Address = memoryReader.ReadPointer(clientModuleBase, memoryOffsets.LocalPlayerOffset);
            // Aktualisiere die Daten des lokalen Spielers.
            UpdateEntityData(localPlayerEntity, currentViewMatrix);
            // Aktualisiere alle anderen Entities.
            UpdateAllEntities(currentViewMatrix);

            // Sortiere die Gegner entweder anhand der Entfernung zum Fadenkreuz oder der 3D-Entfernung.
            enemyEntities = enableAimbotCrosshair
                ? enemyEntities.OrderBy(e => e.ScreenDistance).ToList()
                : enemyEntities.OrderBy(e => e.Distance).ToList();

            Console.WriteLine($"LocalPlayer: Health: {localPlayerEntity.Health} Origin: {localPlayerEntity.Origin}");
        }

        /// <summary>
        /// Aktualisiert die Daten eines einzelnen Entities.
        /// </summary>
        private void UpdateEntityData(Entity entity, ViewMatrix currentViewMatrix)
        {
            // Lese die Basisposition (Füße) aus dem Spielspeicher.
            entity.Origin = memoryReader.ReadVec(entity.Address, memoryOffsets.PositionOffset);
            // Setze einen festen Versatz (65 Einheiten) für die Kopfposition.
            entity.ViewOffset = new Vector3(0, 0, 65);
            // Berechne die absolute Position (Kopfposition) als Summe von Basis und Versatz.
            entity.AbsolutePosition = entity.Origin + entity.ViewOffset; // Beispiel: (100,200,60) + (0,0,65) = (100,200,125)

            // Berechne die 3D-Entfernung zwischen dem lokalen Spieler und diesem Entity.
            entity.Distance = Calculate3dDistance(localPlayerEntity.Origin, entity.Origin);

            // Wandle die 3D-Positionen in 2D-Koordinaten um und addiere die Fensterposition als Offset.
            entity.OriginScreenPos = ConvertWorldToScreen(currentViewMatrix, entity.Origin, (int)windowDimensions.X, (int)windowDimensions.Y)
                                    + windowPosition;
            entity.AbsoluteScreenPos = ConvertWorldToScreen(currentViewMatrix, entity.AbsolutePosition, (int)windowDimensions.X, (int)windowDimensions.Y)
                                      + windowPosition;

            // Lese Gesundheits- und Teamdaten aus dem Speicher.
            entity.Health = memoryReader.ReadInt(entity.Address, memoryOffsets.HealthOffset);
            entity.TeamId = memoryReader.ReadInt(entity.Address, memoryOffsets.TeamId);

            // Berechne die Entfernung vom Bildschirmmittelpunkt.
            entity.ScreenDistance = CalculateScreenDistance(windowCenterPoint, entity.AbsoluteScreenPos);
        }

        /// <summary>
        /// Geht alle Entities (bis zu 64) durch und aktualisiert deren Daten.
        /// </summary>
        private void UpdateAllEntities(ViewMatrix currentViewMatrix)
        {
            const int maxEntities = 64;
            for (int i = 0; i < maxEntities; i++)
            {
                // Berechne die Adresse des i-ten Entities: Basisoffset + (i * 0x8)
                IntPtr entityAddr = memoryReader.ReadPointer(clientModuleBase, memoryOffsets.EntityListOffset + i * 0x8);
                if (entityAddr == IntPtr.Zero)
                    continue;
                Entity entity = new Entity { Address = entityAddr };
                UpdateEntityData(entity, currentViewMatrix);

                // Überspringe Entities mit ungültigen Gesundheitswerten.
                if (entity.Health < 1 || entity.Health > 100)
                    continue;

                // Füge nur neue Entities hinzu, wenn noch kein fast identisches (gleiche X-Position) existiert.
                if (!entities.Any(e => Math.Abs(e.Origin.X - entity.Origin.X) < 0.01f))
                {
                    entities.Add(entity);
                    if (entity.TeamId == localPlayerEntity.TeamId)
                        allyEntities.Add(entity);
                    else
                        enemyEntities.Add(entity);
                }
            }
        }

        /// <summary>
        /// Die Hauptschleife, die kontinuierlich Spielinformationen aktualisiert und Funktionen (z. B. Aimbot) ausführt.
        /// </summary>
        private void RunMainLoop()
        {
            // Bestimme die Basisadresse des Client-Moduls (z. B. "client.dll").
            clientModuleBase = memoryReader.GetModuleBase("client.dll");

            // Ermittle Fensterposition und -größe.
            if (NativeMethods.GetWindowRect(memoryReader.GetProcess().MainWindowHandle, out RECT rect))
            {
                // Erstelle einen Vektor aus rect.Left und rect.Top.
                windowPosition = new Vector2(rect.Left, rect.Top);
                // Berechne die Fensterbreite und -höhe als Differenz zwischen rechter und linker bzw. unterer und oberer Kante.
                windowDimensions = new Vector2(rect.Right - rect.Left, rect.Bottom - rect.Top);
                // Berechne den Ausgangspunkt für Linien: Fensterposition.X + halbe Fensterbreite.
                lineStartPoint = new Vector2(windowPosition.X + windowDimensions.X / 2, rect.Bottom);
                // Berechne den Bildschirmmittelpunkt: X-Wert wie lineStartPoint.X, Y-Wert: rect.Bottom minus halbe Fensterhöhe.
                windowCenterPoint = new Vector2(lineStartPoint.X, rect.Bottom - windowDimensions.Y / 2);
            }
            else
            {
                Console.WriteLine("Error: Could not retrieve window rectangle.");
            }

            while (true)
            {
                // Lese die ViewMatrix: Berechnung erfolgt als clientModuleBase + memoryOffsets.ViewMatrixOffset.
                ViewMatrix currentViewMatrix = ReadViewMatrix(clientModuleBase + memoryOffsets.ViewMatrixOffset);
                // Aktualisiere alle Spieler-Daten.
                ReloadEntitiesData(currentViewMatrix);
                // Führe den Aimbot aus, falls aktiviert.
                if (enableAimbot)
                    ExecuteAimbot();
                // Kurze Pause (3 Millisekunden) zur Schonung der CPU.
                Thread.Sleep(3);
            }
        }

        #endregion

        #region Programmstart

        /// <summary>
        /// Der Einstiegspunkt des Programms: Startet das Overlay und den Hauptlogik-Thread.
        /// </summary>
        static void Main(string[] args)
        {
            Program app = new Program();
            app.Start().Wait(); // Starte das Overlay und warte auf die Initialisierung.
            Thread mainLoopThread = new Thread(app.RunMainLoop)
            {
                IsBackground = true // Der Hauptlogik-Thread läuft im Hintergrund.
            };
            mainLoopThread.Start();
        }

        #endregion
    }
}
