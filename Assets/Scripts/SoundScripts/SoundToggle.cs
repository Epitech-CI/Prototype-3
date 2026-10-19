using UnityEngine;
using TMPro;

public class SoundToggle : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;

    public void ToggleSound()
    {
        AudioListener.volume = AudioListener.volume > 0 ? 0 : 1;

        buttonText.text = AudioListener.volume > 0
            ? "Sound: ON"
            : "Sound: OFF";
    }

    private void Start()
    {
        buttonText.text = AudioListener.volume > 0
            ? "Sound: ON"
            : "Sound: OFF";
    }
}