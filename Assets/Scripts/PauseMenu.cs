using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoWorlds
{
    /// <summary>
    /// Shows and hides the pause panel of the Game scene. The buttons of the panel call the public methods below
    /// (sound and music buttons use SoundToggle, like the main menu). GameController opens it with Escape.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GameController game;

        public bool IsPaused => panel.activeSelf;

        private void Awake() => panel.SetActive(false);

        private void OnDestroy() => Time.timeScale = 1f;

        public void Toggle() => SetPaused(!IsPaused);

        public void Resume() => SetPaused(false);

        public void RestartLevel()
        {
            game.RestartLevel();
            SetPaused(false);
        }

        public void MainMenu()
        {
            SetPaused(false);
            SceneManager.LoadScene("Menu");
        }

        private void SetPaused(bool paused)
        {
            panel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
        }
    }
}
