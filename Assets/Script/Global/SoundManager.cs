using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance = null;
    private AudioSource soundEffectAudio;

    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup backgroundGroup;
    private AudioSource musicSource;
    private AudioSource backgroundSource;


    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            Init();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        foreach (AudioSource source in sources)
        {
            if (source.clip == null)
            {
                soundEffectAudio = source;
            }
        }
    }

    private void Init()
    {
        foreach (var source in GetComponents<AudioSource>())
        {
            if (source.outputAudioMixerGroup == musicGroup)
                musicSource = source;

            else if (source.outputAudioMixerGroup == backgroundGroup)
                backgroundSource = source;
        }
    }
}


