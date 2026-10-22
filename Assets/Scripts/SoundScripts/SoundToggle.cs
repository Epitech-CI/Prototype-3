using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SoundToggle : MonoBehaviour
{
    [SerializeField] private TMP_Text sfxButtonText;
    [SerializeField] private TMP_Text musicButtonText;


    // Toggle SFX on or off depending on the current state
    public void ToggleSoundEffects()
    {

        // Get the saved sfx state and reverse it
        bool sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        sfxEnabled = !sfxEnabled;

        PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);

        // apply if soundManager is initialized
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXEnabled(sfxEnabled);
        }

        sfxButtonText.text = sfxEnabled
            ? "Sound Effects: ON"
            : "Sound Effects: OFF";
    }

    // Toggle Music on or off depending on the current state
    public void ToggleMusic()
    {
        if (MusicManager.Instance == null) return;
        AudioSource musicSource = MusicManager.Instance.GetComponent<AudioSource>();
        musicSource.mute = !musicSource.mute;

        musicButtonText.text = musicSource.mute
            ? "Music: OFF"
            : "Music: ON";
    }

    private void Start()
    {

        // SFX button
        bool sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        sfxButtonText.text = sfxEnabled
            ? "Sound Effects: ON"
            : "Sound Effects: OFF";

        // Music Button
        // MusicManager lives in the Menu scene: it is missing when the Game scene is played directly.
        if (MusicManager.Instance == null)
        {
            musicButtonText.text = "Music: N/A";
            var musicButton = musicButtonText.GetComponentInParent<Button>();
            if (musicButton != null) musicButton.interactable = false;
            return;
        }

        musicButtonText.text = MusicManager.Instance.IsMusicMuted
            ? "Music: OFF"
            : "Music: ON";
    }
}
