using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance = null;
    private AudioSource soundEffectAudio;

    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup backgroundGroup;
    [SerializeField] private AudioMixer audioMixer;

    private AudioSource musicSource;
    private AudioSource backgroundSource;

    [SerializeField] private AudioEventDispatcher _AudioEventDispatcher;

    private string MusicLowPassParam = "Music_LowPass";
    private string MusicVolumeParam = "Music_Volume";
    private float MaxCutoff = 22000f;
    private float MinCutoff = 400f;
    private float TransitionDuration = 2.5f;

    private void OnEnable()
    {
        _AudioEventDispatcher.OnAudioEvent += PlaySound;
    }

    private void OnDisable()
    {
        _AudioEventDispatcher.OnAudioEvent -= PlaySound;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // soundEffectAudio : source sans clip assigné (utilisée pour les effets ponctuels)
        AudioSource[] sources = GetComponents<AudioSource>();
        foreach (AudioSource source in sources)
        {
            if (source.clip == null)
                soundEffectAudio = source;
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

    
    public void PlayMusicWithLowPass(AudioClip music)
    {
        if (musicSource.clip == music) return;
        StartCoroutine(LowPassTransition(music));
    }

    private IEnumerator LowPassTransition(AudioClip nextMusic)
    {
        float elapsed = 0f;

        
        while (elapsed < TransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / TransitionDuration;
            audioMixer.SetFloat(MusicLowPassParam, Mathf.Lerp(MaxCutoff, MinCutoff, t));
            
            audioMixer.SetFloat(MusicVolumeParam, Mathf.Lerp(0f, -80f, t));
            yield return null;
        }

        
        musicSource.Stop();
        musicSource.clip = nextMusic;
        musicSource.Play();

        elapsed = 0f;

        
        while (elapsed < TransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / TransitionDuration;
            audioMixer.SetFloat(MusicLowPassParam, Mathf.Lerp(MinCutoff, MaxCutoff, t));
            audioMixer.SetFloat(MusicVolumeParam, Mathf.Lerp(-80f, 0f, t));
            yield return null;
        }

        audioMixer.SetFloat(MusicLowPassParam, MaxCutoff);
        audioMixer.SetFloat(MusicVolumeParam, 0f);
    }

}