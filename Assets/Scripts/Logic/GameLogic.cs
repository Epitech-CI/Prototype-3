using System.Collections.Generic;

namespace TwoWorlds.Logic
{
    public enum MoveOutcome
    {
        Moved,   // entered the target cell
        Blocked, // wall, closed door or edge of the grid
        Frozen,  // already on its goal, did not try to move
        Died     // stepped on spikes (the whole move was cancelled)
    }

    public struct PlayerMove
    {
        public MoveOutcome Outcome;
        public Pos From;
        /// <summary>Cell the character tried to enter (equals From when Frozen).</summary>
        public Pos To;
    }

    public sealed class StepResult
    {
        public PlayerMove[] Players = new PlayerMove[Level.PlayerCount];
        public bool Died;
        /// <summary>True if the state changed and the move can be undone.</summary>
        public bool Changed;
        /// <summary>Door letters opened by this move.</summary>
        public List<char> OpenedDoors = new List<char>();
        public bool Won;
    }

    public sealed class GameLogic
    {
        public Level Level { get; }
        public GameState State { get; private set; }

        private readonly Stack<GameState> history = new Stack<GameState>();

        public GameLogic(Level level)
        {
            Level = level;
            Restart();
        }

        public bool CanUndo => history.Count > 0;
        public bool IsWon => IsFrozen(0) && IsFrozen(1);
        public bool IsFrozen(int player) => TileUnder(player).Kind == TileKind.Goal;
        public bool IsDoorOpen(char letter) => State.OpenDoors.Contains(letter);

        public Tile TileUnder(int player) => Level.Worlds[player][State.Players[player]];

        public StepResult Step(Direction dir)
        {
            var result = new StepResult();
            if (IsWon) { result.Won = true; return result; }

            var before = State.Clone();

            // 1. Each character moves independently, using the door state from before the move.
            for (int i = 0; i < Level.PlayerCount; i++)
            {
                var from = State.Players[i];
                if (IsFrozen(i))
                {
                    result.Players[i] = new PlayerMove { Outcome = MoveOutcome.Frozen, From = from, To = from };
                    continue;
                }
                var to = from.Step(dir);
                bool canEnter = CanEnter(Level.Worlds[i], to);
                result.Players[i] = new PlayerMove { Outcome = canEnter ? MoveOutcome.Moved : MoveOutcome.Blocked, From = from, To = to };
            }

            // 2. Spikes: if either character would land on them, the whole move is cancelled.
            for (int i = 0; i < Level.PlayerCount; i++)
            {
                var move = result.Players[i];
                if (move.Outcome == MoveOutcome.Moved && Level.Worlds[i][move.To].Kind == TileKind.Spikes)
                {
                    result.Players[i].Outcome = MoveOutcome.Died;
                    result.Died = true;
                }
            }
            if (result.Died) return result;

            // 3. Apply the move, then press buttons (doors open for good, in both worlds).
            for (int i = 0; i < Level.PlayerCount; i++)
            {
                if (result.Players[i].Outcome != MoveOutcome.Moved) continue;
                State.Players[i] = result.Players[i].To;
                result.Changed = true;

                var tile = TileUnder(i);
                if (tile.Kind == TileKind.Button && State.OpenDoors.Add(tile.Letter))
                    result.OpenedDoors.Add(tile.Letter);
            }

            if (result.Changed) history.Push(before);
            result.Won = IsWon;
            return result;
        }

        public bool Undo()
        {
            if (history.Count == 0) return false;
            State = history.Pop();
            return true;
        }

        public void Restart()
        {
            State = new GameState(Level.Worlds[0].Start, Level.Worlds[1].Start);
            history.Clear();
        }

        private bool CanEnter(World world, Pos p)
        {
            if (!world.InBounds(p)) return false;
            var tile = world[p];
            if (tile.Kind == TileKind.Wall) return false;
            if (tile.Kind == TileKind.Door && !IsDoorOpen(tile.Letter)) return false;
            return true;
        }
    }
}
