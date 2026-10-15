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
        public void Death_CancelsTheMove()
        {
            var game = Load("Level1.txt");
            Play(game, "RR"); // A walks under the spikes, B is blocked by a wall
            var a = game.State.Players[0];
            var b = game.State.Players[1];

            var result = game.Step(Direction.Up); // A steps on spikes, B could move up

            Assert.IsTrue(result.Died);
            Assert.AreEqual(MoveOutcome.Died, result.Players[0].Outcome);
            Assert.IsFalse(result.Changed);
            Assert.AreEqual(a, game.State.Players[0]);
            Assert.AreEqual(b, game.State.Players[1], "B must not move when A dies");
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
    }
}
