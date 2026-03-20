using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

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

    void OnEnable()
    {
        SaveGameSystem.OnSaveLoaded += Init;
    }

    void OnDisable()
    {
        SaveGameSystem.OnSaveLoaded -= Init;
    }

    void Init()
    {
        LoadVolume();
    }

    private void LoadVolume()
    {
        slider.value = playerDatas.generalVolume;
        sliderMusic.value = playerDatas.musicVolume;
        sliderBG.value = playerDatas.SFXVolume;
        masterGroup.audioMixer.SetFloat("Master_Volume", Mathf.Log10(Mathf.Clamp(playerDatas.generalVolume, 0.0001f, 1f)) * 20);
        musicGroup.audioMixer.SetFloat("Music_Volume", Mathf.Log10(Mathf.Clamp(playerDatas.musicVolume, 0.0001f, 1f)) * 20);
        backgroundGroup.audioMixer.SetFloat("BG_Volume", Mathf.Log10(Mathf.Clamp(playerDatas.SFXVolume, 0.0001f, 1f)) * 20);
    }


    public void ChangeMasterVolume()
    {
        float newvalue = slider.value;
        masterGroup.audioMixer.SetFloat("Master_Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
        playerDatas.generalVolume = newvalue;
        save.SaveGame();
    }

    public void ChangeMusicVolume()
    {
        float newvalue = sliderMusic.value;
        musicGroup.audioMixer.SetFloat("Music_Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
        playerDatas.musicVolume = newvalue;
        save.SaveGame();
    }

    public void ChangeBackGroundVolume()
    {
        float newvalue = sliderBG.value;
        backgroundGroup.audioMixer.SetFloat("BG_Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
        playerDatas.SFXVolume = newvalue;
        save.SaveGame();
    }
}
