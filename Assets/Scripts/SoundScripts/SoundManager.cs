using UnityEngine;

//This file controls the sound manager, which plays the sounds objects call for
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public bool IsSFXMuted => sfxSource.mute;

    [SerializeField]
    private SoundLibrary sfxLibrary;
    [SerializeField]
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            bool sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
            SetSFXEnabled(sfxEnabled);
        }
    }
    
    public void PlaySound(string soundName)
    {
        sfxSource.PlayOneShot(sfxLibrary.GetClipFromName(soundName));
    }

    public void SetSFXEnabled(bool enabled)
    {
        sfxSource.mute = !enabled;
    }
}
