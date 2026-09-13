using System;
using UnityEngine;

[Serializable]
public sealed class HomepageMusicEntry
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loopEnabled;
    [Min(0), Tooltip("Additional repeats after the first play.")] public int loopCount;
    public int TotalPlays => 1 + (loopEnabled ? Mathf.Clamp(loopCount, 0, int.MaxValue - 1) : 0);
}
