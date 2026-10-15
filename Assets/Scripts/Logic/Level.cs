namespace TwoWorlds.Logic
{
    public enum TileKind { Floor, Wall, Spikes, Goal, Button, Door }

    public readonly struct Tile
    {
        public readonly TileKind Kind;
        /// <summary>Uppercase door letter for Button and Door tiles ('A' for both 'a' and 'A'), '\0' otherwise.</summary>
        public readonly char Letter;

        public Tile(TileKind kind, char letter = '\0') { Kind = kind; Letter = letter; }
    }

    /// <summary>One character's grid. Immutable once parsed: doors being open lives in GameState.</summary>
    public sealed class World
    {
        public int Width { get; }
        public int Height { get; }
        public Pos Start { get; }
        private readonly Tile[,] tiles;

        public World(Tile[,] tiles, Pos start)
        {
            this.tiles = tiles;
            Width = tiles.GetLength(0);
            Height = tiles.GetLength(1);
            Start = start;
        }

        public bool InBounds(Pos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        public Tile this[Pos p] => tiles[p.X, p.Y];
    }

    public sealed class Level
    {
        public const int PlayerCount = 2;

        /// <summary>Worlds[0] = world A (shown on top), Worlds[1] = world B.</summary>
        public World[] Worlds { get; }

        public Level(World a, World b) { Worlds = new[] { a, b }; }
    }
}
