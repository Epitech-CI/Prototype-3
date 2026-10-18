using UnityEngine;

[System.Serializable]
public struct SoundEffect
{
    public string groupID;
    public AudioClip[] clips;
}

//This file helps objects call the method rquired to play a sound
public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] sfx;

    public AudioClip GetClipFromName(string name)
    {
        foreach (var soundEffect in sfx)
        {
            if(soundEffect.groupID == name)
            {
                return soundEffect.clips[Random.Range(0, soundEffect.clips.Length)];
            }
        }
        return null;
    }
}
