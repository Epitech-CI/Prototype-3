using System;

namespace TwoWorlds.Logic
{
    public enum Direction { Up, Down, Left, Right }

    /// <summary>
    /// Grid coordinate. X = column (0 = left), Y = row (0 = bottom), so Up means Y + 1.
    /// </summary>
    public readonly struct Pos : IEquatable<Pos>
    {
        public readonly int X;
        public readonly int Y;

        public Pos(int x, int y) { X = x; Y = y; }

        public Pos Step(Direction dir) => dir switch
        {
            Direction.Up => new Pos(X, Y + 1),
            Direction.Down => new Pos(X, Y - 1),
            Direction.Left => new Pos(X - 1, Y),
            Direction.Right => new Pos(X + 1, Y),
            _ => this
        };

        public Pos MirrorStep(Direction dir) => dir switch
        {
            Direction.Up => new Pos(X, Y - 1),
            Direction.Down => new Pos(X, Y + 1),
            Direction.Left => new Pos(X + 1, Y),
            Direction.Right => new Pos(X - 1, Y),
            _ => this
        };

        public bool Equals(Pos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Pos other && Equals(other);
        public override int GetHashCode() => X * 397 ^ Y;
        public static bool operator ==(Pos a, Pos b) => a.Equals(b);
        public static bool operator !=(Pos a, Pos b) => !a.Equals(b);
        public override string ToString() => $"({X}, {Y})";
    }
}
