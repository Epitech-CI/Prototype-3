using TwoWorlds.Logic;
using TwoWorlds.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TwoWorlds
{
    /// <summary>
    /// Reads the keyboard, calls the game logic and refreshes the view. Also draws the (minimal) HUD.
    /// </summary>
    [RequireComponent(typeof(GridView))]
    public class GameController : MonoBehaviour
    {
        [SerializeField] private TextAsset[] levels;

        private GridView view;
        private GameLogic game;
        private int levelIndex;
        private GUIStyle textStyle;

        private bool IsLastLevel => levelIndex == levels.Length - 1;

        private void Awake()
        {
            view = GetComponent<GridView>();
        } 

        private void Start() => LoadLevel(0);

        private void LoadLevel(int index)
        {
            levelIndex = index;
            game = new GameLogic(LevelParser.Parse(levels[index].text));
            view.Build(game.Level);
            view.Refresh(game);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (game.IsWon)
            {
                bool next = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                if (next && !IsLastLevel) LoadLevel(levelIndex + 1);
                return;
            }

            if (kb.rKey.wasPressedThisFrame)
            {
                game.Restart();
                view.Refresh(game);
            }
            else if (kb.backspaceKey.wasPressedThisFrame)
            {
                if (game.Undo()) view.Refresh(game);
            }
            else if (TryReadDirection(kb, out var dir))
            {
                game.Step(dir);
                view.Refresh(game);
            }
        }

        // Letter keys are physical positions: wKey is Z on an AZERTY keyboard, aKey is Q, so ZQSD works too.
        private static bool TryReadDirection(Keyboard kb, out Direction dir)
        {
            dir = default;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            {
                dir = Direction.Up;
                SoundManager.Instance.PlaySound("StepVert");
            }
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            {
                dir = Direction.Down;
                SoundManager.Instance.PlaySound("StepVert");
            }
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            {
                dir = Direction.Left;
                SoundManager.Instance.PlaySound("StepHor");
            }
            else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            {
                dir = Direction.Right;
                SoundManager.Instance.PlaySound("StepHor");
            }
            else return false;
            return true;
        }

        private void OnGUI()
        {
            if (game == null) return;
            textStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = false };
            int size = Mathf.Max(14, Screen.height / 32);
            textStyle.fontSize = size;
            textStyle.alignment = TextAnchor.UpperLeft;

            GUI.Label(new Rect(16, 8, Screen.width, size * 2), $"Level {levelIndex + 1} / {levels.Length}", textStyle);
            GUI.Label(new Rect(16, Screen.height - size * 2, Screen.width, size * 2),
                "Move: Arrows / WASD / ZQSD     Undo: Backspace     Restart: R", textStyle);

            if (!game.IsWon) return;
            var box = new Rect(Screen.width * 0.25f, Screen.height * 0.35f, Screen.width * 0.5f, Screen.height * 0.3f);
            GUI.Box(box, GUIContent.none);
            GUI.Box(box, GUIContent.none); // drawn twice for a darker background
            textStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(box, IsLastLevel
                ? "All levels complete!\nThanks for playing."
                : "Level complete!\nPress Enter to continue.", textStyle);
        }
    }
}
