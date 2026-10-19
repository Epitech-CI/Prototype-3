using System.Collections.Generic;

namespace TwoWorlds.Logic
{
    public enum TileKind
    {
        Floor,
        Wall,
        Spikes,
        Goal,
        Button,
        Door,
        Turret,
        Trigger
    };

    public readonly struct Tile
    {
        public readonly TileKind Kind;

        /// <summary>Uppercase door letter for Button and Door tiles ('A' for both 'a' and 'A'), '\0' otherwise.</summary>
        public readonly char Letter;

        /// <summary>Firing direction, only meaningful for Turret tiles.</summary>
        public readonly Direction TurretDirection;

        public Tile(TileKind kind, char letter = '\0', Direction turretDirection = Direction.Up)
        {
            Kind = kind;
            Letter = letter;
            TurretDirection = turretDirection;
        }
    }

    /// <summary>A turret's place in the level, so firing does not have to scan the grids.</summary>
    public readonly struct Turret
    {
        public readonly int World;
        public readonly Pos Pos;
        public readonly Direction Direction;

        public Turret(int world, Pos pos, Direction direction)
        {
            World = world;
            Pos = pos;
            Direction = direction;
        }
    }

    /// <summary>One character's grid. Immutable once parsed: doors being open lives in GameState.</summary>
    public sealed class World
    {
        public int Width { get; }
        public int Height { get; }
        public Pos Start { get; }

        //Check if mirror level
        public bool IsMirror { get; }

        private readonly Tile[,] tiles;

        public World(Tile[,] tiles, Pos start, bool isMirror = false)
        {
            this.tiles = tiles;
            Width = tiles.GetLength(0);
            Height = tiles.GetLength(1);
            Start = start;
            IsMirror = isMirror;
        }

        public bool InBounds(Pos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        public Tile this[Pos p] => tiles[p.X, p.Y];
    }

    public sealed class Level
    {
        public const int PlayerCount = 2;

        /// <summary>Worlds[0] = world A (shown on top), Worlds[1] = world B.</summary>
        public World[] Worlds { get; }

        /// <summary>Every turret of both worlds. A trigger fires all of them.</summary>
        public IReadOnlyList<Turret> Turrets { get; }

        public bool HasTurrets => Turrets.Count > 0;

        public Level(World a, World b)
        {
            Worlds = new[] { a, b };

            var turrets = new List<Turret>();
            for (int w = 0; w < Worlds.Length; w++)
            for (int x = 0; x < Worlds[w].Width; x++)
            for (int y = 0; y < Worlds[w].Height; y++)
            {
                var tile = Worlds[w][new Pos(x, y)];
                if (tile.Kind == TileKind.Turret) turrets.Add(new Turret(w, new Pos(x, y), tile.TurretDirection));
            }
            Turrets = turrets;
        }
    }
}
