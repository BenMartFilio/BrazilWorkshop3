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

    public void ChangeMasterVolume()
    {
        float newvalue = GetComponent<Slider>().value;
        masterGroup.audioMixer.SetFloat("Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
    }

    public void ChangeMusicVolume()
    {
        float newvalue = GetComponent<Slider>().value;
        musicGroup.audioMixer.SetFloat("Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
    }

    public void ChangeBackGroundVolume()
    {
        float newvalue = GetComponent<Slider>().value;
        backgroundGroup.audioMixer.SetFloat("Volume", Mathf.Log10(Mathf.Clamp(newvalue, 0.0001f, 1f)) * 20);
    }
}
