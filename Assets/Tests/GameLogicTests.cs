using System;
using System.IO;
using NUnit.Framework;
using TwoWorlds.Logic;

namespace TwoWorlds.Tests
{
    public class GameLogicTests
    {
        private const string Level1Solution = "UUURRDRRRURUUURDDD";
        private const string Level2Solution = "UUUURRRRRDDDRRRRRRRURRRUURRRDLLLDDDRRRRUURRURRDDRRRRUURRUURRRR";

        // EditMode tests run with the project root as working directory.
        private static GameLogic Load(string fileName) =>
            new GameLogic(LevelParser.Parse(File.ReadAllText(Path.Combine("Assets", "Levels", fileName))));

        private static Direction ToDirection(char c) => c switch
        {
            'U' => Direction.Up,
            'D' => Direction.Down,
            'L' => Direction.Left,
            'R' => Direction.Right,
            _ => throw new ArgumentException($"Unknown move '{c}'")
        };

        private static void Play(GameLogic game, string moves)
        {
            for (int i = 0; i < moves.Length; i++)
            {
                var result = game.Step(ToDirection(moves[i]));
                Assert.IsFalse(result.Died, $"Died on move {i + 1} ('{moves[i]}')");
            }
        }

        [TestCase("Level1.txt", Level1Solution)]
        [TestCase("Level2.txt", Level2Solution)]
        public void ReferenceSolution_WinsOnLastMove(string file, string solution)
        {
            var game = Load(file);
            Play(game, solution.Substring(0, solution.Length - 1));
            Assert.IsFalse(game.IsWon, "Won before the last move");

            var last = game.Step(ToDirection(solution[solution.Length - 1]));
            Assert.IsTrue(last.Won);
            Assert.IsTrue(game.IsWon);
        }

        [Test]
        public void Level1_UpAtStart_BlocksAButMovesB()
        {
            var game = Load("Level1.txt");
            var startA = game.State.Players[0];
            var startB = game.State.Players[1];

            var result = game.Step(Direction.Up);

            Assert.AreEqual(MoveOutcome.Blocked, result.Players[0].Outcome);
            Assert.AreEqual(startA, game.State.Players[0]);
            Assert.AreEqual(MoveOutcome.Moved, result.Players[1].Outcome);
            Assert.AreEqual(startB.Step(Direction.Up), game.State.Players[1]);
        }

        [Test]
        public void Spikes_ResetTheLevel()
        {
            var game = Load("Level1.txt");
            Play(game, "UUURR"); // B opens door A, then A walks under the spikes

            var result = game.Step(Direction.Up); // A steps on spikes

            Assert.IsTrue(result.Died);
            Assert.AreEqual(DeathCause.Spikes, result.DeathCause);
            Assert.AreEqual(MoveOutcome.Died, result.Players[0].Outcome);
            AssertResetToStart(game);
        }

        [Test]
        public void Undo_RestoresPreviousState()
        {
            var game = Load("Level1.txt");
            game.Step(Direction.Up);
            game.Step(Direction.Right);

            Assert.IsTrue(game.Undo());
            Assert.IsTrue(game.Undo());

            Assert.AreEqual(game.Level.Worlds[0].Start, game.State.Players[0]);
            Assert.AreEqual(game.Level.Worlds[1].Start, game.State.Players[1]);
            Assert.IsFalse(game.Undo(), "Nothing left to undo");
        }

        [Test]
        public void Button_OpensDoorInOtherWorld()
        {
            var game = Load("Level1.txt");
            Assert.IsFalse(game.IsDoorOpen('A'));

            game.Step(Direction.Up);
            game.Step(Direction.Up);
            var result = game.Step(Direction.Up); // B reaches the 'a' button in the top-left corner

            Assert.Contains('A', result.OpenedDoors);
            Assert.IsTrue(game.IsDoorOpen('A'));
        }

        [Test]
        public void Parser_RejectsMissingSecondWorld()
        {
            Assert.Throws<FormatException>(() => LevelParser.Parse("S.G"));
        }

        // ---------- Turrets and projectiles ----------

        // Turret in each world, trigger in world A at (2,0). After "RR", A is on the trigger and both turrets fired.
        private static readonly string[] TwoTurrets =
        {
            ">...G",
            "S.!..",
            "",
            "....<",
            "S...G",
        };

        private static GameLogic FromLines(params string[] lines) =>
            new GameLogic(LevelParser.Parse(string.Join("\n", lines)));

        private static void AssertResetToStart(GameLogic game)
        {
            Assert.AreEqual(game.Level.Worlds[0].Start, game.State.Players[0]);
            Assert.AreEqual(game.Level.Worlds[1].Start, game.State.Players[1]);
            Assert.IsEmpty(game.State.OpenDoors);
            Assert.IsEmpty(game.State.Projectiles);
            Assert.IsFalse(game.CanUndo);
        }

        [Test]
        public void Parser_ReadsTurretsAndTriggers()
        {
            var level = LevelParser.Parse("v.G\nS.!\n\nS.G");
            var turret = level.Worlds[0][new Pos(0, 1)];

            Assert.AreEqual(TileKind.Turret, turret.Kind, "'v' must be a turret, not a button");
            Assert.AreEqual(Direction.Down, turret.TurretDirection);
            Assert.AreEqual(TileKind.Trigger, level.Worlds[0][new Pos(2, 0)].Kind);
            Assert.AreEqual(1, level.Turrets.Count);
        }

        [Test]
        public void EnteringTrigger_FiresEveryTurretInBothWorlds()
        {
            var game = FromLines(TwoTurrets);
            game.Step(Direction.Right);
            var result = game.Step(Direction.Right);

            Assert.IsTrue(result.Fired);
            Assert.AreEqual(2, game.State.Projectiles.Count);
            Assert.AreEqual(new Pos(0, 1), game.State.Projectiles[0].Pos);
            Assert.AreEqual(Direction.Right, game.State.Projectiles[0].Direction);
            Assert.AreEqual(1, game.State.Projectiles[1].World);
            Assert.AreEqual(new Pos(4, 1), game.State.Projectiles[1].Pos);
        }

        [Test]
        public void StayingOnTrigger_DoesNotFireAgain()
        {
            var game = FromLines(TwoTurrets);
            Play(game, "RR");

            var result = game.Step(Direction.Down); // both characters are blocked by the bottom edge

            Assert.IsFalse(result.Fired);
            Assert.AreEqual(2, game.State.Projectiles.Count);
        }

        [Test]
        public void Tick_MovesEachProjectileOneCell()
        {
            var game = FromLines(TwoTurrets);
            Play(game, "RR");

            game.Tick();

            Assert.AreEqual(new Pos(1, 1), game.State.Projectiles[0].Pos);
            Assert.AreEqual(new Pos(3, 1), game.State.Projectiles[1].Pos);
        }

        [Test]
        public void Projectile_VanishesAtTheEdge()
        {
            var game = FromLines(TwoTurrets);
            Play(game, "RR");

            for (int i = 0; i < 4; i++) game.Tick();
            Assert.AreEqual(2, game.State.Projectiles.Count, "Still inside the grid after 4 ticks");

            game.Tick();
            Assert.IsEmpty(game.State.Projectiles);
        }

        [TestCase('#')]
        [TestCase('D')] // closed door
        public void Projectile_VanishesAgainstWallsAndClosedDoors(char obstacle)
        {
            var game = FromLines($">.{obstacle}G", "S!..", "", "...G", "S...");
            game.Step(Direction.Right); // A enters the trigger

            game.Tick(); // leaves the turret
            Assert.AreEqual(1, game.State.Projectiles.Count);

            game.Tick(); // next cell is the obstacle
            Assert.IsEmpty(game.State.Projectiles);
        }

        [Test]
        public void Projectile_HitsAStandingCharacter_AndResetsTheLevel()
        {
            var game = FromLines(".v.G", "S!..", "", "...G", "S...");
            game.Step(Direction.Right); // A enters the trigger, right below the turret

            var result = game.Tick();

            Assert.IsTrue(result.Died);
            Assert.AreEqual(new Pos(1, 0), result.Hits[0]);
            Assert.IsNull(result.Hits[1]);
            AssertResetToStart(game);
        }

        [Test]
        public void WalkingIntoAProjectile_ResetsTheLevel()
        {
            var game = FromLines(TwoTurrets);
            Play(game, "RR");
            game.Tick();
            game.Tick(); // A's projectile is now right above A

            var result = game.Step(Direction.Up);

            Assert.IsTrue(result.Died);
            Assert.AreEqual(DeathCause.Projectile, result.DeathCause);
            Assert.AreEqual(MoveOutcome.Died, result.Players[0].Outcome);
            AssertResetToStart(game);
        }

        [Test]
        public void FrozenCharacter_IsNotHit()
        {
            // A reaches its goal on the turret's line, then B enters the trigger.
            var game = FromLines(">.G", "S..", "", "....G", "S...!");
            Play(game, "RRUDRR");
            Assert.IsTrue(game.IsFrozen(0));

            for (int i = 0; i < 3; i++)
                Assert.IsFalse(game.Tick().Died, $"Tick {i + 1}");
            Assert.IsTrue(game.IsFrozen(0));
        }

        [Test]
        public void Undo_IsDisabledInLevelsWithTurrets()
        {
            var game = FromLines(TwoTurrets);
            game.Step(Direction.Right);

            Assert.IsFalse(game.CanUndo);
            Assert.IsFalse(game.Undo());
        }
    }
}
