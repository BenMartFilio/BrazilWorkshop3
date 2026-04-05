// SoundManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup backgroundGroup;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioEventDispatcher _AudioEventDispatcher;

    private AudioSource _musicSource;
    private AudioSource _backgroundSource;

    private const string MusicLowPassParam = "Music_LowPass";
    private const string MusicVolumeParam = "Music_Volume";
    private const string MasterVolumeParam = "Master_Volume";
    private const string BGVolumeParam = "BG_Volume";

    private const float MaxCutoff = 22000f;
    private const float MinCutoff = 400f;
    private const float TransitionDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheAudioSources(); // une seule fois ici
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => _AudioEventDispatcher.OnAudioEvent += PlaySound;
    private void OnDisable() => _AudioEventDispatcher.OnAudioEvent -= PlaySound;

    // Résout le double-parcours Awake+Start en une seule passe
    private void CacheAudioSources()
    {
        foreach (AudioSource source in GetComponents<AudioSource>())
        {
            if (source.outputAudioMixerGroup == musicGroup)
                _musicSource = source;
            else if (source.outputAudioMixerGroup == backgroundGroup)
                _backgroundSource = source;
        }
    }

    private void PlaySound(AudioClip son) => _backgroundSource.PlayOneShot(son);

    public void PlayMusic(AudioClip music)
    {
        _musicSource.Stop();
        _musicSource.clip = music;
        _musicSource.Play();
    }

    public void PlayMusicWithLowPass(AudioClip music)
    {
        if (_musicSource.clip == music) return;
        StartCoroutine(LowPassTransition(music));
    }

    private float _currentMusicVolumeDb = 0f; // volume cible mémorisé en dB

    public void ApplyAllVolumes(float master, float music, float sfx)
    {
        _currentMusicVolumeDb = LinearToDecibels(music); // ← mémorisé ici
        audioMixer.SetFloat("Master_Volume", LinearToDecibels(master));
        audioMixer.SetFloat("Music_Volume", _currentMusicVolumeDb);
        audioMixer.SetFloat("BG_Volume", LinearToDecibels(sfx));
    }

    private IEnumerator LowPassTransition(AudioClip nextMusic)
    {
        // Phase 1 : fade out depuis le volume actuel du mixer
        audioMixer.GetFloat(MusicVolumeParam, out float initialMusicDb);

        float elapsed = 0f;
        while (elapsed < TransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / TransitionDuration;
            audioMixer.SetFloat(MusicLowPassParam, Mathf.Lerp(MaxCutoff, MinCutoff, t));
            audioMixer.SetFloat(MusicVolumeParam, Mathf.Lerp(initialMusicDb, -80f, t));
            yield return null;
        }

        _musicSource.Stop();
        _musicSource.clip = nextMusic;
        if (nextMusic != null) _musicSource.Play();

        // Phase 2 : fade in vers _currentMusicVolumeDb
        // Si OnSaveLoaded a appelé ApplyAllVolumes pendant le fade-out,
        // _currentMusicVolumeDb contient déjà la valeur sauvegardée correcte.
        elapsed = 0f;
        while (elapsed < TransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / TransitionDuration;
            audioMixer.SetFloat(MusicLowPassParam, Mathf.Lerp(MinCutoff, MaxCutoff, t));
            audioMixer.SetFloat(MusicVolumeParam, Mathf.Lerp(-80f, _currentMusicVolumeDb, t));
            yield return null;
        }

        audioMixer.SetFloat(MusicLowPassParam, MaxCutoff);
        audioMixer.SetFloat(MusicVolumeParam, _currentMusicVolumeDb);
    }


    private static float LinearToDecibels(float value)
        => Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
}