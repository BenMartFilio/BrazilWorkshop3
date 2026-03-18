using Unity.VisualScripting;
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

    [SerializeField] private AudioEventDispatcher _AudioEventDispatcher;
    [SerializeField] private AudioClip _AudioClip;
    [SerializeField] private AudioType _AudioClip2;
    [SerializeField] private AudioType _AudioClip3;

    private void OnEnable()
    {
        _AudioEventDispatcher.OnAudioEvent += PlaySound;
    }

    private void OnDisable()
    {
        
    }

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

        _AudioEventDispatcher.PlayAudio(_AudioClip2);
        _AudioEventDispatcher.PlayAudio(_AudioClip3);

        PlayMusic(_AudioClip);
        // A UTILISER POUR CALL     _AudioEventDispatcher.PlayAudio(_DeathAudioType);
    }

    private void PlaySound(AudioClip son)
    {
        backgroundSource.PlayOneShot(son);
    }


    public void PlayMusic(AudioClip music)
    {
        musicSource.Stop();
        musicSource.clip = music;
        musicSource.Play();
    }
}


