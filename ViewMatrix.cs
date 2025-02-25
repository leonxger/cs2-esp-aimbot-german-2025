namespace CS2_Multihack
{
    /// <summary>
    /// Repräsentiert eine 4x4 View-Matrix, die in 3D-Grafikanwendungen verwendet wird.
    /// Eine ViewMatrix transformiert 3D-Weltkoordinaten in Kamerakoordinaten.
    /// 
    /// Beispiel:
    /// Angenommen, du hast einen Punkt (X, Y, Z) in der Welt.
    /// Durch Multiplikation mit der ViewMatrix erhältst du neue Koordinaten, die angeben,
    /// wo sich dieser Punkt relativ zur Kamera befindet, was später zur Berechnung der
    /// 2D-Bildschirmkoordinaten dient.
    /// </summary>
    public class ViewMatrix
    {
        public float m11, m12, m13, m14;
        public float m21, m22, m23, m24;
        public float m31, m32, m33, m34;
        public float m41, m42, m43, m44;
    }
}
