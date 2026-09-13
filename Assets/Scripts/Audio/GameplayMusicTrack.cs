using System;
using UnityEngine;

[Serializable]
public sealed class GameplayMusicTrack
{
    public AudioClip intro;
    public AudioClip body;
    public AudioClip outro;
    [Range(0f, 1f)] public float volume = 1f;
    public bool IsValid => intro || body || outro;
}
