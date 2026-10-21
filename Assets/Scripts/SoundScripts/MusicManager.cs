using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    public bool IsMusicMuted => GetComponent<AudioSource>().mute;
    
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

            // Reset audio settings when the game launches
            PlayerPrefs.DeleteKey("SFXEnabled");
            GetComponent<AudioSource>().mute = false;
        }
    }
}