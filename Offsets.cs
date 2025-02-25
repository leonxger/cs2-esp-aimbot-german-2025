namespace CS2_Multihack
{
    /// <summary>
    /// Enthält alle Speicheroffsets, die zum Auslesen und Schreiben von Spielvariablen benötigt werden.
    /// </summary>
    internal class Offsets
    {
        // Basisadressen
        public int LocalPlayerOffset = 0x1889F20;   // Offset zur lokalen Spieleradresse
        public int EntityListOffset = 0x1897230;    // Offset zur Liste aller Entities

        // Offsets zur Transformation von 3D- in 2D-Koordinaten
        public int ViewMatrixOffset = 0x1AA17B0;     // Offset der 4x4 ViewMatrix
        public int ViewAnglesOffset = 0x1AABA40;     // Offset, an dem die Blickwinkel (Yaw, Pitch) gespeichert werden

        // Offsets für weitere Spielvariablen
        public int HealthOffset = 0x344;             // Offset der Gesundheit
        public int PositionOffset = 0xDB8;           // Offset der 3D-Position (Origin)

        public int IsJumpingOffset = 0x3ec;          // Offset, der den Sprungstatus anzeigt
        public int TeamId = 0x3e3;                   // Offset der Team-ID
    }
}
