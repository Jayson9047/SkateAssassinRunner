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
            for (int i = 0; i < levelBands.Count; i++)
            {
                if (levelBands[i] != null && levelBands[i].Matches(level))
                    return levelBands[i].RollX();
            }

            // Fallback if not configured (won't crash your run)
            Debug.LogWarning($"[Missions] No level band matched for {type} at level {level}. Defaulting to 5.");
            return 5;
        }

        public string BuildDescription(int x) => string.Format(descriptionFormat, x);
    }
}
