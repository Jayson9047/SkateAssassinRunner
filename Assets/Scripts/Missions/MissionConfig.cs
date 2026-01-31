using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Missions
{
    [Serializable]
    public class MissionLevelBand
    {
        [Tooltip("Inclusive. Example: 1")]
        public int minLevel = 1;

        [Tooltip("Inclusive. Use -1 for infinity.")]
        public int maxLevel = -1;

        [Tooltip("Inclusive min random target (X). Example: 6")]
        public int minX = 6;

        [Tooltip("Inclusive max random target (X). Example: 10")]
        public int maxX = 10;

        public bool Matches(int level)
        {
            if (level < minLevel) return false;
            if (maxLevel != -1 && level > maxLevel) return false;
            return true;
        }

        public int RollX()
        {
            int lo = Mathf.Min(minX, maxX);
            int hi = Mathf.Max(minX, maxX);
            return UnityEngine.Random.Range(lo, hi + 1); // inclusive
        }
    }

    [Serializable]
    public class MissionDefinition
    {
        public MissionType type;
        public MissionCategory category = MissionCategory.Combat;

        [Tooltip("Optional: shown later if you want headers like 'KILL ENEMIES'")]
        public string title = "MISSION";

        [Tooltip("Use {0} where X goes. Example: Kill {0} enemies")]
        public string descriptionFormat = "Kill {0} enemies";

        [Tooltip("Level-based randomization ranges. First match wins (top to bottom).")]
        public List<MissionLevelBand> levelBands = new List<MissionLevelBand>();

        public int GetTargetForLevel(int level)
        {
            MissionLevelBand bestMatch = null;
            MissionLevelBand latestBand = null;

            for (int i = 0; i < levelBands.Count; i++)
            {
                var band = levelBands[i];
                if (band == null) continue;

                // Track the "latest" band by highest minLevel
                // (if tie, prefer the one with higher maxLevel, treating -1 as infinity)
                if (latestBand == null ||
                    band.minLevel > latestBand.minLevel ||
                    (band.minLevel == latestBand.minLevel && NormalizeMax(band.maxLevel) > NormalizeMax(latestBand.maxLevel)))
                {
                    latestBand = band;
                }

                // First-match wins for actual in-range matches (top to bottom)
                if (bestMatch == null && band.Matches(level))
                {
                    bestMatch = band;
                }
            }

            if (bestMatch != null)
                return bestMatch.RollX();

            // Fallback: if level is beyond configured ranges, keep using the latest band
            if (latestBand != null)
                return latestBand.RollX();

            Debug.LogWarning($"[Missions] No level bands configured for {type}. Defaulting to 5.");
            return 5;
        }

        private int NormalizeMax(int maxLevel)
        {
            return maxLevel == -1 ? int.MaxValue : maxLevel;
        }


        public string BuildDescription(int x) => string.Format(descriptionFormat, x);
    }
}
