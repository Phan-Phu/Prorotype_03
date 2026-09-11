namespace Prototype.Domain
{
    /// <summary>Integer grid coordinate. Plain C# (no UnityEngine) so it is usable in EditMode tests.</summary>
    public readonly struct GridCoord : System.IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;
        public GridCoord(int x, int y) { X = x; Y = y; }

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object o) => o is GridCoord c && Equals(c);
        public override int GetHashCode() => (X * 73856093) ^ Y;
        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);
        public override string ToString() => $"({X},{Y})";
    }
}
