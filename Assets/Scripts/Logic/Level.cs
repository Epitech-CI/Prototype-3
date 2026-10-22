using System;
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
        Trigger,
        Ice,
        Teleporter
    };

    public readonly struct Tile
    {
        public readonly TileKind Kind;

        /// <summary>
        /// Uppercase door letter for Button and Door tiles ('A' for both 'a' and 'A'),
        /// pair digit for Teleporter tiles ('1'-'9'), '\0' otherwise.
        /// </summary>
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

        // Each teleporter maps to the other teleporter with the same digit.
        private readonly Dictionary<Pos, Pos> teleporterExits = new Dictionary<Pos, Pos>();

        /// <exception cref="FormatException">A teleporter digit does not appear exactly twice.</exception>
        public World(Tile[,] tiles, Pos start, bool isMirror = false)
        {
            this.tiles = tiles;
            Width = tiles.GetLength(0);
            Height = tiles.GetLength(1);
            Start = start;
            IsMirror = isMirror;

            var pads = new Dictionary<char, List<Pos>>();
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var tile = tiles[x, y];
                if (tile.Kind != TileKind.Teleporter) continue;
                if (!pads.TryGetValue(tile.Letter, out var list)) pads[tile.Letter] = list = new List<Pos>();
                list.Add(new Pos(x, y));
            }

            foreach (var pair in pads)
            {
                if (pair.Value.Count != 2)
                    throw new FormatException($"teleporter '{pair.Key}' appears {pair.Value.Count} time(s), expected exactly 2.");
                teleporterExits[pair.Value[0]] = pair.Value[1];
                teleporterExits[pair.Value[1]] = pair.Value[0];
            }
        }

        public bool InBounds(Pos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        /// <summary>Where a character entering the teleporter at p comes out. False if p has no partner.</summary>
        public bool TryGetTeleporterExit(Pos p, out Pos exit) => teleporterExits.TryGetValue(p, out exit);

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
