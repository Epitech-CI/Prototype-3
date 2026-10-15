using System;
using System.Collections.Generic;

namespace TwoWorlds.Logic
{
    /// <summary>
    /// Parses a level file: world A, a blank line, world B. First line = top row.
    /// . floor  # wall  X spikes  S start  G goal  a-z button  A-Z door (except S, G, X)
    /// </summary>
    public static class LevelParser
    {
        public static Level Parse(string text)
        {
            var blocks = SplitBlocks(text);
            if (blocks.Count != 2)
                throw new FormatException($"A level needs exactly 2 worlds separated by a blank line, found {blocks.Count}.");
            return new Level(ParseWorld(blocks[0], "A"), ParseWorld(blocks[1], "B"));
        }

        private static List<List<string>> SplitBlocks(string text)
        {
            var blocks = new List<List<string>>();
            List<string> current = null;
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var line = raw.TrimEnd();
                if (line.Length == 0) { current = null; continue; }
                if (current == null) { current = new List<string>(); blocks.Add(current); }
                current.Add(line);
            }
            return blocks;
        }

        private static World ParseWorld(List<string> rows, string name)
        {
            int width = rows[0].Length;
            int height = rows.Count;
            var tiles = new Tile[width, height];
            Pos? start = null;
            bool hasGoal = false;

            for (int row = 0; row < height; row++)
            {
                if (rows[row].Length != width)
                    throw new FormatException($"World {name}: row {row + 1} has {rows[row].Length} cells, expected {width}.");

                int y = height - 1 - row; // file's first line is the top row
                for (int x = 0; x < width; x++)
                {
                    char c = rows[row][x];
                    tiles[x, y] = ParseTile(c, name);
                    if (c == 'S')
                    {
                        if (start != null) throw new FormatException($"World {name}: more than one start 'S'.");
                        start = new Pos(x, y);
                    }
                    if (c == 'G') hasGoal = true;
                }
            }

            if (start == null) throw new FormatException($"World {name}: no start 'S'.");
            if (!hasGoal) throw new FormatException($"World {name}: no goal 'G'.");
            return new World(tiles, start.Value);
        }

        private static Tile ParseTile(char c, string worldName)
        {
            switch (c)
            {
                case '.': case 'S': return new Tile(TileKind.Floor);
                case '#': return new Tile(TileKind.Wall);
                case 'X': return new Tile(TileKind.Spikes);
                case 'G': return new Tile(TileKind.Goal);
            }
            if (c >= 'a' && c <= 'z') return new Tile(TileKind.Button, char.ToUpperInvariant(c));
            if (c >= 'A' && c <= 'Z') return new Tile(TileKind.Door, c);
            throw new FormatException($"World {worldName}: unknown character '{c}'.");
        }
    }
}
