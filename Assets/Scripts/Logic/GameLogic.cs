using System.Collections.Generic;

namespace TwoWorlds.Logic
{
    public enum MoveOutcome
    {
        Moved, // entered the target cell
        Blocked, // wall, turret, closed door or edge of the grid
        Frozen, // already on its goal, did not try to move
        Died // stepped on spikes or into a projectile (the level was reset)
    }

    public enum DeathCause
    {
        Spikes,
        Projectile
    }

    public struct PlayerMove
    {
        public MoveOutcome Outcome;
        public Pos From;

        /// <summary>Cell the character tried to enter (equals From when Frozen), or the exit of the teleporter it entered.</summary>
        public Pos To;

        /// <summary>Teleporter the character stepped on before being moved to To, null if it did not teleport.</summary>
        public Pos? TeleportedFrom;
    }

    public sealed class StepResult
    {
        public PlayerMove[] Players = new PlayerMove[Level.PlayerCount];
        public bool Died;

        /// <summary>Why the character died. Only meaningful when Died is true.</summary>
        public DeathCause DeathCause;

        /// <summary>True if the state changed and the move can be undone.</summary>
        public bool Changed;

        /// <summary>Door letters opened by this move.</summary>
        public List<char> OpenedDoors = new List<char>();

        /// <summary>True if a character entered a trigger and every turret fired.</summary>
        public bool Fired;

        public bool Won;
    }

    /// <summary>What happened during one clock tick. A tick can only kill with a projectile.</summary>
    public sealed class TickResult
    {
        /// <summary>True if a projectile hit a character (the level was reset).</summary>
        public bool Died;

        /// <summary>Cell where each character was hit, null if it was not hit.</summary>
        public Pos?[] Hits = new Pos?[Level.PlayerCount];
    }

    public sealed class GameLogic
    {
        public Level Level { get; }
        public GameState State { get; private set; }

        /// <summary>Deaths since the level was loaded. A manual restart does not count.</summary>
        public int Deaths { get; private set; }

        private readonly Stack<GameState> history = new Stack<GameState>();

        // Never reset, so a projectile fired after a restart never reuses the id of an old sprite.
        private int nextProjectileId;

        public GameLogic(Level level)
        {
            Level = level;
            Restart();
        }

        /// <summary>Undo is disabled in levels with turrets: rewinding time would make dodging trivial.</summary>
        public bool CanUndo => !Level.HasTurrets && history.Count > 0;

        public bool IsWon => IsFrozen(0) && IsFrozen(1);
        public bool IsFrozen(int player) => TileUnder(player).Kind == TileKind.Goal;
        public bool IsDoorOpen(char letter) => State.OpenDoors.Contains(letter);

        public Tile TileUnder(int player) => Level.Worlds[player][State.Players[player]];

        public StepResult Step(Direction dir)
        {
            var result = new StepResult();
            if (IsWon)
            {
                result.Won = true;
                return result;
            }

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

                //Also checks if mirror movement
                var world = Level.Worlds[i];
                var to = world.IsMirror ? from.MirrorStep(dir) : from.Step(dir);
                bool canEnter = CanEnter(Level.Worlds[i], to);

                // If the player is on ice and moves toward another ice tile, keep sliding until it reaches the last consecutive ice tile.
                if (canEnter &&
                    world[from].Kind == TileKind.Ice &&
                    world[to].Kind == TileKind.Ice)
                {
                    while (true)
                    {
                        // Stop if there is a projectile on this tile.
                        if (HasProjectileAt(i, to))
                            break;

                        var next = world.IsMirror
                            ? to.MirrorStep(dir)
                            : to.Step(dir);


                        // Stop if the next tile is blocked or isn't ice or spikes
                        if (!CanEnter(world, next) ||
                            (world[next].Kind != TileKind.Ice &&
                             world[next].Kind != TileKind.Spikes))
                            break;

                        to = next;

                        // Allow player to slide into spikes
                        if (world[to].Kind == TileKind.Spikes)
                            break;
                    }
                }

                // Stepping on a teleporter moves the character to the other teleporter with the same digit.
                // Arriving on the exit does not teleport again: only entering a teleporter does.
                Pos? teleportedFrom = null;
                if (canEnter && world[to].Kind == TileKind.Teleporter && world.TryGetTeleporterExit(to, out var exit))
                {
                    teleportedFrom = to;
                    to = exit;
                }

                result.Players[i] = new PlayerMove
                {
                    Outcome = canEnter ? MoveOutcome.Moved : MoveOutcome.Blocked, From = from, To = to,
                    TeleportedFrom = teleportedFrom
                };
            }

            // 2. Deaths: landing on spikes or walking into a projectile resets the whole level.
            for (int i = 0; i < Level.PlayerCount; i++)
            {
                var move = result.Players[i];
                if (move.Outcome != MoveOutcome.Moved) continue;

                DeathCause? cause = null;
                if (Level.Worlds[i][move.To].Kind == TileKind.Spikes) cause = DeathCause.Spikes;
                else if (HasProjectileAt(i, move.To)) cause = DeathCause.Projectile;
                // A projectile on the entry teleporter also kills: the character walked into it.
                else if (move.TeleportedFrom is Pos entry && HasProjectileAt(i, entry))
                {
                    cause = DeathCause.Projectile;
                    result.Players[i].To = entry;
                }
                if (cause == null) continue;

                result.Players[i].Outcome = MoveOutcome.Died;
                result.Died = true;
                result.DeathCause = cause.Value;
            }

            if (result.Died)
            {
                Deaths++;
                Restart();
                return result;
            }

            // 3. Apply the move, then press buttons (doors open for good, in both worlds) and triggers.
            bool fire = false;
            for (int i = 0; i < Level.PlayerCount; i++)
            {
                if (result.Players[i].Outcome != MoveOutcome.Moved) continue;
                State.Players[i] = result.Players[i].To;
                result.Changed = true;

                var tile = TileUnder(i);
                if (tile.Kind == TileKind.Button && State.OpenDoors.Add(tile.Letter))
                    result.OpenedDoors.Add(tile.Letter);
                if (tile.Kind == TileKind.Trigger) fire = true;
            }

            // Both characters entering a trigger on the same move still fire only once.
            if (fire)
            {
                FireTurrets();
                result.Fired = true;
            }

            if (result.Changed) history.Push(before);
            result.Won = IsWon;
            return result;
        }

        /// <summary>
        /// Advances every projectile by one cell. Called by the clock, not by the player.
        /// </summary>
        public TickResult Tick()
        {
            var result = new TickResult();
            var projectiles = State.Projectiles;
            if (IsWon || projectiles.Count == 0) return result;

            // 1. A projectile vanishes when its next cell is a wall, a turret, a closed door or off the grid.
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var p = projectiles[i];
                var to = p.Pos.Step(p.Direction);
                if (CanEnter(Level.Worlds[p.World], to)) projectiles[i] = p.MovedTo(to);
                else projectiles.RemoveAt(i);
            }

            // 2. A projectile on a character's cell kills it, unless the character is frozen on its goal.
            for (int i = 0; i < Level.PlayerCount; i++)
            {
                if (IsFrozen(i) || !HasProjectileAt(i, State.Players[i])) continue;
                result.Hits[i] = State.Players[i];
                result.Died = true;
            }

            if (result.Died)
            {
                Deaths++;
                Restart();
            }
            return result;
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            State = history.Pop();
            return true;
        }

        public void Restart()
        {
            State = new GameState(Level.Worlds[0].Start, Level.Worlds[1].Start);
            history.Clear();
        }

        // Every turret of both worlds fires; the projectile starts on the turret and leaves it on the next tick.
        private void FireTurrets()
        {
            foreach (var turret in Level.Turrets)
                State.Projectiles.Add(new Projectile(nextProjectileId++, turret.World, turret.Pos, turret.Direction));
        }

        private bool HasProjectileAt(int world, Pos p)
        {
            foreach (var projectile in State.Projectiles)
                if (projectile.World == world && projectile.Pos == p) return true;
            return false;
        }

        // Shared by characters and projectiles: both are stopped by the same things.
        private bool CanEnter(World world, Pos p)
        {
            if (!world.InBounds(p)) return false;
            var tile = world[p];
            if (tile.Kind == TileKind.Wall) return false;
            if (tile.Kind == TileKind.Turret) return false;
            if (tile.Kind == TileKind.Door && !IsDoorOpen(tile.Letter)) return false;
            return true;
        }
    }
}
