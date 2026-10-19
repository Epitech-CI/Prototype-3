using System.Collections.Generic;

namespace TwoWorlds.Logic
{
    /// <summary>A shot travelling in a straight line, one cell per clock tick.</summary>
    public readonly struct Projectile
    {
        /// <summary>Unique id, so the view can match each projectile with its sprite.</summary>
        public readonly int Id;
        public readonly int World;
        public readonly Pos Pos;
        public readonly Direction Direction;

        public Projectile(int id, int world, Pos pos, Direction direction)
        {
            Id = id;
            World = world;
            Pos = pos;
            Direction = direction;
        }

        public Projectile MovedTo(Pos pos) => new Projectile(Id, World, pos, Direction);
    }

    /// <summary>
    /// Everything that changes during play. Small on purpose so it can be copied for undo.
    /// A character is frozen exactly when it stands on a goal, so that is not stored separately.
    /// </summary>
    public sealed class GameState
    {
        public Pos[] Players { get; }
        /// <summary>Uppercase letters of doors opened so far (shared by both worlds).</summary>
        public HashSet<char> OpenDoors { get; }
        public List<Projectile> Projectiles { get; }

        public GameState(Pos a, Pos b) : this(new[] { a, b }, new HashSet<char>(), new List<Projectile>()) { }

        private GameState(Pos[] players, HashSet<char> openDoors, List<Projectile> projectiles)
        {
            Players = players;
            OpenDoors = openDoors;
            Projectiles = projectiles;
        }

        public GameState Clone() =>
            new GameState((Pos[])Players.Clone(), new HashSet<char>(OpenDoors), new List<Projectile>(Projectiles));
    }
}
