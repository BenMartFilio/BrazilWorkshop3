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

    public void ChangeMasterVolume()
    {
        float newvalue = slider.value;
        masterGroup.audioMixer.SetFloat("Master_Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
    }

    public void ChangeMusicVolume()
    {
        float newvalue = sliderMusic.value;
        musicGroup.audioMixer.SetFloat("Music_Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
    }

    public void ChangeBackGroundVolume()
    {
        float newvalue = sliderBG.value;
        backgroundGroup.audioMixer.SetFloat("BG_Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
    }
}
