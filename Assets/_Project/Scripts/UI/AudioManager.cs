using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private bool isAudioOn = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ToggleAudio(bool isOn)
    {
        isAudioOn = isOn;
        AudioListener.volume = isAudioOn ? 1.0f : 0.0f;
    }

    public bool IsAudioOn()
    {
        return isAudioOn;
    }
}
