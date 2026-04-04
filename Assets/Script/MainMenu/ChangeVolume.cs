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
    private void OnDisable() => SaveGameSystem.OnSaveLoaded -= LoadVolume;

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

    private void RequestSave()
    {
        _pendingSave = true;
        _saveTimer = SAVE_DELAY;
    }

    private void LoadVolume()
    {
        slider.value = playerDatas.generalVolume;
        sliderMusic.value = playerDatas.musicVolume;
        sliderBG.value = playerDatas.SFXVolume;

        ApplyMixer(masterGroup, "Master_Volume", playerDatas.generalVolume);
        ApplyMixer(musicGroup, "Music_Volume", playerDatas.musicVolume);
        ApplyMixer(backgroundGroup, "BG_Volume", playerDatas.SFXVolume);
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