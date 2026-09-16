using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.InfiniteRunnerEngine
{
    [Serializable]
    public sealed class LevelSpeedWindow
    {
        public string Name;
        [Min(1)] public int StartLevel = 1;
        [Min(1)] public int EndLevel = 5;
        [Tooltip("Upper limit for InitialSpeed in this range. The first range uses its limits immediately; later ranges grow from the previous range's limits.")]
        [Min(0f)] public float InitialSpeedLimit = 10f;
        [Tooltip("Increase per level, including the first level of this range. Stops at Initial Speed Limit.")]
        [Min(0f)] public float InitialSpeedIncreasePerLevel;
        [Tooltip("Hard limit for the resolved MaximumSpeed in this range. No per-level increase can exceed it.")]
        [Min(0f)] public float MaximumSpeedLimit = 15f;
        [Tooltip("Increase per level, including the first level of this range. Stops at Maximum Speed Limit.")]
        [Min(0f)] public float MaximumSpeedIncreasePerLevel;
    }

    /// <summary>Startup-only campaign evaluation. Does not own runtime Speed or acceleration.</summary>
    public static class LevelSpeedProgression
    {
        public static List<LevelSpeedWindow> CreateDefaults() => new List<LevelSpeedWindow>
        {
            Window("Tutorial", 1, 5, 10, 0, 15, 0),
            Window("Early Game", 6, 20, 15, 0.5f, 20, 0.5f),
            Window("Early-Mid Game", 21, 80, 15, 0, 25, 0.25f),
            Window("Mid Game", 81, 300, 15, 0, 30, 0.25f),
            Window("Late Game", 301, 500, 15, 0, 35, 0.25f),
            Window("End Game", 501, 1000, 15, 0, 40, 0.25f)
        };

        private static LevelSpeedWindow Window(string name, int start, int end, float initial, float initialRate, float maximum, float maximumRate)
            => new LevelSpeedWindow { Name = name, StartLevel = start, EndLevel = end,
                InitialSpeedLimit = initial, InitialSpeedIncreasePerLevel = initialRate,
                MaximumSpeedLimit = maximum, MaximumSpeedIncreasePerLevel = maximumRate };

        public static bool TryResolve(IReadOnlyList<LevelSpeedWindow> windows, int level,
            out float initial, out float maximum, out LevelSpeedWindow selected, out int evaluatedLevel,
            Action<string> warn = null)
        {
            initial = maximum = 0;
            selected = null;
            evaluatedLevel = level;
            if (windows == null || windows.Count == 0)
            {
                warn?.Invoke("Speed progression configuration is empty; keeping existing LevelManager InitialSpeed and MaximumSpeed.");
                return false;
            }

            var valid = new List<LevelSpeedWindow>(windows.Count);
            var issues = new List<string>();
            for (int i = 0; i < windows.Count; i++)
            {
                var w = windows[i];
                if (!IsValid(w))
                {
                    issues.Add($"Window {i + 1} ('{w?.Name}') ignored: require levels >= 1, Start <= End, and finite nonnegative limits/rates");
                    continue;
                }
                valid.Add(w);
            }
            if (valid.Count == 0)
            {
                issues.Add("No valid speed windows; keeping existing LevelManager InitialSpeed and MaximumSpeed");
                warn?.Invoke(string.Join("; ", issues));
                return false;
            }

            // Sort a private view only, with data-based ties independent of Inspector order.
            valid.Sort(Compare);
            int coveredEnd = valid[0].EndLevel;
            for (int i = 1; i < valid.Count; i++)
            {
                var w = valid[i];
                if (w.StartLevel <= coveredEnd)
                    issues.Add($"Overlap/duplicate at {w.StartLevel}-{w.EndLevel}: latest containing Start Level wins; ties use earliest End, then ascending speed/rate values and Name");
                else if ((long)w.StartLevel > (long)coveredEnd + 1)
                    issues.Add($"Configuration gap {coveredEnd + 1}-{w.StartLevel - 1}: hold the previous window's end speeds");
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
                // Gaps and levels above all ranges hold the nearest completed end.
                foreach (var w in valid)
                    if (w.EndLevel < level && (selected == null || w.EndLevel > selected.EndLevel ||
                        (w.EndLevel == selected.EndLevel && w.StartLevel > selected.StartLevel))) selected = w;
                if (selected == null) selected = valid[0]; // Below every window.
            }

            evaluatedLevel = Mathf.Clamp(level, selected.StartLevel, selected.EndLevel);
            // Derive the baseline from the previous distinct range's targets, not
            // configurable starting values. Equal-start duplicates cannot become
            // one another's baseline. Compare already supplies deterministic ties.
            LevelSpeedWindow previous = null;
            foreach (var w in valid)
                if (w.StartLevel < selected.StartLevel &&
                    (previous == null || w.StartLevel > previous.StartLevel)) previous = w;

            if (previous == null)
            {
                initial = selected.InitialSpeedLimit;
                maximum = selected.MaximumSpeedLimit;
            }
            else
            {
                // First level of a new range is already one increment above the
                // previous limits (level 6 = 10.5 / 15.5 with the default data).
                long steps = (long)evaluatedLevel - selected.StartLevel + 1;
                float previousInitial = Math.Min(previous.InitialSpeedLimit, previous.MaximumSpeedLimit);
                initial = CappedIncrease(previousInitial, selected.InitialSpeedIncreasePerLevel, steps, selected.InitialSpeedLimit);
                maximum = CappedIncrease(previous.MaximumSpeedLimit, selected.MaximumSpeedIncreasePerLevel, steps, selected.MaximumSpeedLimit);
            }
            if (maximum < initial)
            {
                // Never raise MaximumSpeed above its cap to repair inconsistent
                // designer data. Reduce InitialSpeed instead.
                initial = maximum;
                issues.Add($"Window {selected.StartLevel}-{selected.EndLevel}: InitialSpeed exceeded resolved MaximumSpeed; InitialSpeed reduced to {initial:0.00} to preserve the maximum limit");
            }
            if (issues.Count > 0) warn?.Invoke(string.Join("; ", issues));
            return true;
        }

        private static bool IsValid(LevelSpeedWindow w)
        {
            if (w == null || w.StartLevel < 1 || w.EndLevel < w.StartLevel ||
                !FiniteNonnegative(w.InitialSpeedLimit) || !FiniteNonnegative(w.MaximumSpeedLimit) ||
                !FiniteNonnegative(w.InitialSpeedIncreasePerLevel) || !FiniteNonnegative(w.MaximumSpeedIncreasePerLevel)) return false;
            return true;
        }

        // Double intermediate avoids float overflow with huge but finite rates;
        // clamp before converting back to float, including lowered range limits.
        private static float CappedIncrease(float baseline, float rate, long steps, float limit)
            => (float)Math.Min(limit, baseline + (double)rate * steps);

        private static bool FiniteNonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        private static int Compare(LevelSpeedWindow a, LevelSpeedWindow b)
        {
            int c = a.StartLevel.CompareTo(b.StartLevel); if (c != 0) return c;
            c = a.EndLevel.CompareTo(b.EndLevel); if (c != 0) return c;
            c = a.InitialSpeedLimit.CompareTo(b.InitialSpeedLimit); if (c != 0) return c;
            c = a.MaximumSpeedLimit.CompareTo(b.MaximumSpeedLimit); if (c != 0) return c;
            c = a.InitialSpeedIncreasePerLevel.CompareTo(b.InitialSpeedIncreasePerLevel); if (c != 0) return c;
            c = a.MaximumSpeedIncreasePerLevel.CompareTo(b.MaximumSpeedIncreasePerLevel); if (c != 0) return c;
            return string.CompareOrdinal(a.Name, b.Name);
        }
    }
}
