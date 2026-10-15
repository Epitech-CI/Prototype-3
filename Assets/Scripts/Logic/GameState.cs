using System.Collections.Generic;

namespace TwoWorlds.Logic
{
    /// <summary>
    /// Everything that changes during play. Small on purpose so it can be copied for undo.
    /// A character is frozen exactly when it stands on a goal, so that is not stored separately.
    /// </summary>
    public sealed class GameState
    {
        public Pos[] Players { get; }
        /// <summary>Uppercase letters of doors opened so far (shared by both worlds).</summary>
        public HashSet<char> OpenDoors { get; }

        public GameState(Pos a, Pos b) : this(new[] { a, b }, new HashSet<char>()) { }

        private GameState(Pos[] players, HashSet<char> openDoors)
        {
            Players = players;
            OpenDoors = openDoors;
        }

        public GameState Clone() => new GameState((Pos[])Players.Clone(), new HashSet<char>(OpenDoors));
    }
}
