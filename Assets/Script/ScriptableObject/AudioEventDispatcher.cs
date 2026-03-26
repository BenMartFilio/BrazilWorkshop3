using System;
using UnityEngine;

public enum AudioType
{
    None,
    Gard,
    PaperGet,
    PaperSet,
    Pneu,
    Correct,
    Wrong,
    Crash,
    GameOver,
    TicTac,
    Pickup,
    CarWindow,
    StartEngine,
    UIClick
}
//Deux audio source (à faire) une qui fait la musique, et l'autre les sons (les sons sont joué un par un)
[System.Serializable]
public struct AudioInfos
{
    public AudioType audioType;
    public AudioClip audioClip;
}


[CreateAssetMenu(fileName = "AudioEventDispatcher", menuName = "Scriptable Objects/AudioEventDispatcher")]
public class AudioEventDispatcher : ScriptableObject
{
    [SerializeField] private AudioInfos[] audioClips;

    public event Action<AudioClip> OnAudioEvent;

    public void PlayAudio(AudioType audioType)
    {
        for (int i = 0; i < audioClips.Length; i++)
        {
            if (audioClips[i].audioType == audioType)
            {
                OnAudioEvent?.Invoke(audioClips[i].audioClip);
                return;
            }
        }
        // OnAudioEvent?.Invoke(audioType);
    }
}
