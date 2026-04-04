// ChangeVolume.cs — debounce de la sauvegarde + imports nettoyés
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class ChangeVolume : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup backgroundGroup;
    [SerializeField] private AudioMixerGroup masterGroup;

    [SerializeField] private Slider slider;
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private Slider sliderBG;

    [SerializeField] private SaveGameSystem save;
    [SerializeField] private SO_PlayerDatas playerDatas;

    // Debounce : on ne sauvegarde qu'après que le joueur a arrêté de glisser
    private float _saveTimer = 0f;
    private bool _pendingSave = false;
    private const float SAVE_DELAY = 1f; // secondes d'inactivité avant sauvegarde

    private void OnEnable() => SaveGameSystem.OnSaveLoaded += LoadVolume;
    private void OnDisable()
    {
        SaveGameSystem.OnSaveLoaded -= LoadVolume;

        if (_pendingSave)
        {
            _pendingSave = false;
            playerDatas.SaveDatas();
        }
    }

    private void Update()
    {
        if (!_pendingSave) return;
        _saveTimer -= Time.unscaledDeltaTime;
        if (_saveTimer <= 0f)
        {
            _pendingSave = false;
            playerDatas.SaveDatas();
        }
    }

    private void Start()
    {
        LoadVolume();
    }

    private void RequestSave()
    {
        _pendingSave = true;
        _saveTimer = SAVE_DELAY;
    }

    private void LoadVolume()
    {
        // Applique les valeurs aux sliders sans déclencher leurs callbacks (évite double-appel)
        slider.SetValueWithoutNotify(playerDatas.generalVolume);
        sliderMusic.SetValueWithoutNotify(playerDatas.musicVolume);
        sliderBG.SetValueWithoutNotify(playerDatas.SFXVolume);

        // Applique au mixer via SoundManager (qui persiste entre les scènes)
        if (SoundManager.Instance != null)
            SoundManager.Instance.ApplyAllVolumes(
                playerDatas.generalVolume,
                playerDatas.musicVolume,
                playerDatas.SFXVolume);
        else
        {
            // Fallback direct si le SoundManager n'est pas encore prêt
            ApplyMixer(masterGroup, "Master_Volume", playerDatas.generalVolume);
            ApplyMixer(musicGroup, "Music_Volume", playerDatas.musicVolume);
            ApplyMixer(backgroundGroup, "BG_Volume", playerDatas.SFXVolume);
        }
    }

    public void ChangeMasterVolume()
    {
        playerDatas.generalVolume = slider.value;
        ApplyMixer(masterGroup, "Master_Volume", slider.value);
        RequestSave();
    }

    public void ChangeMusicVolume()
    {
        playerDatas.musicVolume = sliderMusic.value;
        ApplyMixer(musicGroup, "Music_Volume", sliderMusic.value);
        RequestSave();
    }

    public void ChangeBackGroundVolume()
    {
        playerDatas.SFXVolume = sliderBG.value;
        ApplyMixer(backgroundGroup, "BG_Volume", sliderBG.value);
        RequestSave();
    }

    private static void ApplyMixer(AudioMixerGroup group, string param, float value)
    {
        group.audioMixer.SetFloat(param, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
    }
}