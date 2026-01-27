using MoreMountains.InfiniteRunnerEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Missions
{
    public class MissionSystem : MonoBehaviour
    {
        public static MissionSystem MissionSystemAccessor { get; private set; }

        // UI-facing events
        public event Action<int, string> OnMissionAssigned;
        public event Action<int, string> OnMissionProgressText;
        public event Action<int> OnMissionCompleted;

        [Header("References")]
        [SerializeField] private SkateAssassinRunnerLevelManager _levelManager;
        public SkateAssassinRunnerLevelManager LevelManager => _levelManager;

        [Header("Mission Definitions (configure in Inspector)")]
        public List<MissionDefinition> missionDefinitions = new List<MissionDefinition>();

        private readonly List<IMission> _activeMissions = new();
        public IReadOnlyList<IMission> ActiveMissions => _activeMissions;

        // CRITICAL: mission -> UI slot mapping (slot 0/1)
        private readonly Dictionary<IMission, int> _missionSlotMap = new();

        private int _currentLevelNum;

        private void Awake()
        {
            MissionSystemAccessor = this;
        }

        private void OnDisable()
        {
            StopAllActiveMissions();
        }

        public void BeginLevel(int levelNum)
        {
            _currentLevelNum = levelNum;

            StopAllActiveMissions();
            _activeMissions.Clear();
            _missionSlotMap.Clear();

            if (missionDefinitions == null || missionDefinitions.Count == 0)
            {
                Debug.LogError("[Missions] No missionDefinitions configured in inspector.");
                return;
            }

            // Build eligible list (must be configured + implemented)
            List<MissionDefinition> eligible = new List<MissionDefinition>();
            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                var def = missionDefinitions[i];
                if (def == null) continue;
                if (def.levelBands == null || def.levelBands.Count == 0) continue;
                if (!IsImplemented(def.type)) continue;

                eligible.Add(def);
            }

            if (eligible.Count == 0)
            {
                Debug.LogError("[Missions] No eligible missions. Check inspector and mission implementations.");
                return;
            }

            // Pick up to 2 distinct missions
            int seed = unchecked(levelNum * 92821) ^ (int)(DateTime.UtcNow.Ticks & 0x0000FFFF);
            var rng = new System.Random(seed);

            int pickCount = Mathf.Min(2, eligible.Count);

            for (int slot = 0; slot < pickCount; slot++)
            {
                int idx = rng.Next(0, eligible.Count);
                var def = eligible[idx];
                eligible.RemoveAt(idx);

                int target = def.GetTargetForLevel(levelNum);

                // SurviveSeconds must not exceed phase 1 duration
                if (def.type == MissionType.SurviveSeconds && _levelManager != null)
                {
                    int maxPossible = Mathf.FloorToInt(_levelManager.Phase1Duration);
                    target = Mathf.Clamp(target, 1, maxPossible);
                }

                IMission mission = CreateMissionInstance(def.type, target);
                if (mission == null) continue;

                _activeMissions.Add(mission);
                _missionSlotMap[mission] = slot;

                mission.StartMission();

                // Initial UI text
                OnMissionAssigned?.Invoke(slot, BuildDisplayText(def, mission));

                Debug.Log($"[Missions] Level {levelNum} slot {slot}: {def.type} | Target={target}");
            }
        }

        public void OnLevelEnd(bool success)
        {
            for (int i = 0; i < _activeMissions.Count; i++)
            {
                var m = _activeMissions[i];
                Debug.Log($"[Missions] End Level {_currentLevelNum} | Success={success} | Type={m.Type} | Progress={m.Progress}/{m.Target} | Complete={m.IsComplete}");
            }
        }

        public int GetStarsEarned(bool levelSuccess)
        {
            if (!levelSuccess) return 0;

            int stars = 1; // level completed = 1 star

            // +1 per completed mission (up to 2)
            for (int i = 0; i < _activeMissions.Count && i < 2; i++)
            {
                if (_activeMissions[i] != null && _activeMissions[i].IsComplete)
                    stars++;
            }

            return Mathf.Clamp(stars, 0, 3);
        }

        internal void NotifyMissionProgressChanged(IMission mission)
        {
            if (mission == null) return;

            if (!_missionSlotMap.TryGetValue(mission, out int slot))
                return; // mission not tracked / not in UI

            var def = GetDefinition(mission.Type);
            string txt = BuildDisplayText(def, mission);

            OnMissionProgressText?.Invoke(slot, txt);

            if (mission.IsComplete)
                OnMissionCompleted?.Invoke(slot);
        }

        private void Update()
        {
            // Observe Phase1 timer for SurviveSeconds mission(s)
            for (int i = 0; i < _activeMissions.Count; i++)
            {
                if (_activeMissions[i] is SurviveSecondsMission s)
                {
                    s.UpdateFromLevelTimer();
                }
            }
        }

        private MissionDefinition GetDefinition(MissionType type)
        {
            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                if (missionDefinitions[i] != null && missionDefinitions[i].type == type)
                    return missionDefinitions[i];
            }
            return null;
        }

        private bool IsImplemented(MissionType type)
        {
            return type == MissionType.KillEnemies
                || type == MissionType.EarnCash
                || type == MissionType.SurviveSeconds;
        }

        private IMission CreateMissionInstance(MissionType type, int target)
        {
            switch (type)
            {
                case MissionType.KillEnemies:
                    return new KillEnemiesMission(target);

                case MissionType.EarnCash:
                    return new EarnCashMission(target);

                case MissionType.SurviveSeconds:
                    return new SurviveSecondsMission(target, _levelManager);

                default:
                    Debug.LogError($"[Missions] No mission class implemented for type: {type}");
                    return null;
            }
        }

        private string BuildDisplayText(MissionDefinition def, IMission m)
        {
            // SurviveSeconds: countdown (no "(0/X)")
            if (m.Type == MissionType.SurviveSeconds)
            {
                int remaining = Mathf.Max(0, m.Target - m.Progress);

                if (def != null)
                    return string.Format(def.descriptionFormat, remaining);

                return $"Survive {remaining} seconds";
            }

            // Default missions: "Desc (progress/target)"
            string baseDesc = def != null ? def.BuildDescription(m.Target) : m.Description;
            return $"{baseDesc} ({m.Progress}/{m.Target})";
        }

        private void StopAllActiveMissions()
        {
            for (int i = 0; i < _activeMissions.Count; i++)
                _activeMissions[i]?.StopMission();
        }
    }
}
