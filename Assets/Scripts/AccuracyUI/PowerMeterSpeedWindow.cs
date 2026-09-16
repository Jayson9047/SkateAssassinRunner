using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PowerMeterSpeedWindow
{
    public string Name;
    [Min(1)] public int StartLevel = 1;
    [Min(1)] public int EndLevel = 5;
    [Tooltip("Cycles per second at Start Level (before any per-level increments).")]
    [Min(0f)] public float StartingTickerSpeed = 0.2f;
    [Tooltip("Cycles per second added per PLAYER LEVEL, not per second of gameplay.")]
    [Min(0f)] public float TickerSpeedIncreasePerLevel;
    [Tooltip("Hard ceiling within this range. Further levels hold this speed once reached.")]
    [Min(0f)] public float MaximumTickerSpeed = 0.2f;
}

/// <summary>Pure, start-of-meter evaluation; independent of Phase 1 speed windows.</summary>
public static class PowerMeterSpeedProgression
{
    public static List<PowerMeterSpeedWindow> CreateDefaults() => new List<PowerMeterSpeedWindow>
    {
        Window("Tutorial", 1, 5, 0.20f, 0f, 0.20f),
        Window("Early Game", 6, 20, 0.20f, 0.005f, 0.25f),
        Window("Developing", 21, 100, 0.25f, 0.0025f, 0.35f),
        Window("Mid Game", 101, 300, 0.35f, 0.001f, 0.45f),
        Window("Late Game", 301, 500, 0.45f, 0.0005f, 0.50f),
        Window("End Game", 501, 1000, 0.50f, 0.0002f, 0.55f)
    };

    private static PowerMeterSpeedWindow Window(string name, int start, int end, float speed, float rate, float cap)
        => new PowerMeterSpeedWindow { Name = name, StartLevel = start, EndLevel = end,
            StartingTickerSpeed = speed, TickerSpeedIncreasePerLevel = rate, MaximumTickerSpeed = cap };

    public static bool TryResolve(IReadOnlyList<PowerMeterSpeedWindow> windows, int level,
        out float speed, out PowerMeterSpeedWindow selected, out int evaluatedLevel, Action<string> warn = null)
    {
        speed = 0f;
        selected = null;
        evaluatedLevel = level;
        if (windows == null || windows.Count == 0)
        {
            warn?.Invoke("Ticker speed windows are empty; using config.speed.");
            return false;
        }

        var valid = new List<PowerMeterSpeedWindow>(windows.Count);
        var issues = new List<string>();
        for (int i = 0; i < windows.Count; i++)
        {
            var w = windows[i];
            if (w == null || w.StartLevel < 1 || w.EndLevel < w.StartLevel ||
                !FiniteNonnegative(w.StartingTickerSpeed) || !FiniteNonnegative(w.TickerSpeedIncreasePerLevel) ||
                !FiniteNonnegative(w.MaximumTickerSpeed))
                issues.Add($"Window {i + 1} ('{w?.Name}') ignored: require positive ordered levels and finite nonnegative speeds/rates");
            else
                valid.Add(w);
        }
        if (valid.Count == 0)
        {
            issues.Add("No valid ticker speed windows; using config.speed");
            warn?.Invoke(string.Join("; ", issues));
            return false;
        }

        // A private sorted view leaves the serialized asset and Inspector order untouched.
        valid.Sort(Compare);
        int coveredEnd = valid[0].EndLevel;
        for (int i = 1; i < valid.Count; i++)
        {
            var w = valid[i];
            if (w.StartLevel <= coveredEnd)
                issues.Add($"Overlap/duplicate at {w.StartLevel}-{w.EndLevel}: latest containing Start wins; ties use earliest End, then ascending starting speed, rate, cap and Name");
            else if ((long)w.StartLevel > (long)coveredEnd + 1)
                issues.Add($"Gap {coveredEnd + 1}-{w.StartLevel - 1}: hold previous range's capped endpoint");
            coveredEnd = Math.Max(coveredEnd, w.EndLevel);
        }
        if (level < 1)
        {
            issues.Add($"Invalid player level {level}; evaluating level 1");
            level = 1;
        }
        foreach (var w in valid)
            if (level >= w.StartLevel && level <= w.EndLevel &&
                (selected == null || w.StartLevel > selected.StartLevel)) selected = w;

        if (selected == null)
        {
            foreach (var w in valid)
                if (w.EndLevel < level && (selected == null || w.EndLevel > selected.EndLevel ||
                    (w.EndLevel == selected.EndLevel && w.StartLevel > selected.StartLevel))) selected = w;
            if (selected == null) selected = valid[0];
        }

        evaluatedLevel = Mathf.Clamp(level, selected.StartLevel, selected.EndLevel);
        // Clamp before conversion back to float: huge finite rates cannot overflow the result.
        speed = (float)Math.Min(selected.MaximumTickerSpeed, selected.StartingTickerSpeed +
            (double)selected.TickerSpeedIncreasePerLevel * (evaluatedLevel - selected.StartLevel));
        if (selected.MaximumTickerSpeed < selected.StartingTickerSpeed)
            issues.Add($"Window {selected.StartLevel}-{selected.EndLevel}: starting ticker speed exceeds maximum; clamped to the configured maximum");
        if (issues.Count > 0) warn?.Invoke(string.Join("; ", issues));
        return true;
    }

    private static bool FiniteNonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

    private static int Compare(PowerMeterSpeedWindow a, PowerMeterSpeedWindow b)
    {
        int c = a.StartLevel.CompareTo(b.StartLevel); if (c != 0) return c;
        c = a.EndLevel.CompareTo(b.EndLevel); if (c != 0) return c;
        c = a.StartingTickerSpeed.CompareTo(b.StartingTickerSpeed); if (c != 0) return c;
        c = a.TickerSpeedIncreasePerLevel.CompareTo(b.TickerSpeedIncreasePerLevel); if (c != 0) return c;
        c = a.MaximumTickerSpeed.CompareTo(b.MaximumTickerSpeed); if (c != 0) return c;
        return string.CompareOrdinal(a.Name, b.Name);
    }
}
