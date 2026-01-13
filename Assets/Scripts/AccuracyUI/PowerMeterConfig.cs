using System;
using UnityEngine;

[CreateAssetMenu(menuName = "SkateRunner/Power Meter Config", fileName = "PowerMeterConfig")]
public class PowerMeterConfig : ScriptableObject
{
    [Header("Ticker Motion")]
    [Tooltip("Cycles per second. Higher = faster.")]
    public float speed = 1.2f;

    [Tooltip("If true, ticker starts at a random position each time you StartMeter().")]
    public bool randomizeStartPosition = true;

    [Tooltip("If true, the ticker uses smooth easing at ends (sin wave). If false, linear ping-pong.")]
    public bool smoothMotion = true;

    [Header("Zones (Normalized 0..1)")]
    [Tooltip("Each zone is an inclusive range [min,max] in normalized meter space (0 bottom, 1 top).")]
    public ZoneRange red = new ZoneRange(0.00f, 0.35f);
    public ZoneRange yellow = new ZoneRange(0.35f, 0.55f);
    public ZoneRange green = new ZoneRange(0.55f, 0.80f);
    public ZoneRange cyan = new ZoneRange(0.80f, 1.00f);

    [Serializable]
    public struct ZoneRange
    {
        [Range(0, 1)] public float min;
        [Range(0, 1)] public float max;

        public ZoneRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(float v) => v >= min && v <= max;
    }
}
