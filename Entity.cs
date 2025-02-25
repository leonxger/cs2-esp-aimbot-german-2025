using System.Numerics;

namespace CS2_Multihack
{
    /// <summary>
    /// Repräsentiert ein Entity im Spiel (z. B. Spieler oder Gegner).
    /// Hier werden alle relevanten Eigenschaften gespeichert, die für das Zeichnen und die Berechnungen benötigt werden.
    /// </summary>
    internal class Entity
    {
        /// <summary>
        /// Speicheradresse des Entities im Spielspeicher.
        /// </summary>
        public IntPtr Address { get; set; }

        /// <summary>
        /// Gesundheit (Wert zwischen 0 und 100).
        /// </summary>
        public int Health { get; set; }

        /// <summary>
        /// Team-ID, um zwischen eigenem Team und Gegnern zu unterscheiden.
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// Flag, das z. B. anzeigt, ob das Entity springt.
        /// </summary>
        public int JumpFlag { get; set; }

        /// <summary>
        /// 3D-Weltposition (Basisposition) des Entities.
        /// </summary>
        public Vector3 Origin { get; set; }

        /// <summary>
        /// Absolute 3D-Position, z. B. zur Darstellung der Kopfposition.
        /// </summary>
        public Vector3 AbsolutePosition { get; set; }

        /// <summary>
        /// Offset, der zur Berechnung der absoluten Position hinzugefügt wird.
        /// </summary>
        public Vector3 ViewOffset { get; set; }

        /// <summary>
        /// Bildschirmkoordinate, die aus der Basisposition (Origin) berechnet wird.
        /// </summary>
        public Vector2 OriginScreenPos { get; set; }

        /// <summary>
        /// Bildschirmkoordinate, die aus der absoluten Position berechnet wird.
        /// </summary>
        public Vector2 AbsoluteScreenPos { get; set; }

        /// <summary>
        /// 3D-Distanz zwischen diesem Entity und dem lokalen Spieler.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// Distanz in Pixeln zwischen der Bildschirmmitte und der absoluten Position,
        /// verwendet zur Auswahl des Ziels im Aimbot.
        /// </summary>
        public float ScreenDistance { get; set; }
    }
}
