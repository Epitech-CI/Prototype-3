using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMenu : MonoBehaviour
{
    public void PlayLevel(int levelNumber)
    {
        LevelSelect.SelectedLevel = levelNumber - 1;
        SceneManager.LoadScene("Game");
    }
}