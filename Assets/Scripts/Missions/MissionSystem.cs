using MoreMountains.InfiniteRunnerEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Missions
{
    public class MissionSystem : MonoBehaviour
    {

        [Header("DEBUG")]
        [SerializeField] private bool debugForceOneMission = false;
        [SerializeField] private MissionType debugForcedMissionType = MissionType.KillEnemies;
        [SerializeField, Range(0, 1)] private int debugForcedSlot = 0; // 0 or 1

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

                if (def == null)
                {
                    Debug.Log($"[Missions][EligibleCheck] Index {i}: NULL def");
                    continue;
                }

                bool needsBands = RequiresLevelBands(def.type);
                if (needsBands && (def.levelBands == null || def.levelBands.Count == 0))
                {
                    Debug.Log($"[Missions][EligibleCheck] {def.type}: SKIP (no levelBands configured)");
                    continue;
                }

                if (!IsImplemented(def.type))
                {
                    Debug.Log($"[Missions][EligibleCheck] {def.type}: SKIP (not implemented)");
                    continue;
                }

                Debug.Log($"[Missions][EligibleCheck] {def.type}: OK");
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

            // DEBUG: force just ONE mission in a chosen slot (the other slot stays random)
            MissionDefinition forcedDef = null;
            if (debugForceOneMission)
            {
                forcedDef = eligible.Find(d => d != null && d.type == debugForcedMissionType);
                if (forcedDef == null)
                {
                    Debug.LogWarning($"[Missions][DEBUG] Forced mission {debugForcedMissionType} not found/eligible. Falling back to random.");
                }
            }

            for (int slot = 0; slot < pickCount; slot++)
            {
                MissionDefinition def;

                // If forcing and this is the forced slot, pick it (as long as it's eligible)
                if (debugForceOneMission && forcedDef != null && slot == debugForcedSlot)
                {
                    def = forcedDef;

                    // Remove so it cannot be selected again in the other slot (keeps "distinct" behavior)
                    eligible.Remove(def);

                    // If we just removed the last item but still need another slot, we can't continue
                    if (slot + 1 < pickCount && eligible.Count == 0)
                    {
                        Debug.LogWarning("[Missions][DEBUG] Only one eligible mission available after forcing; reducing pickCount to 1.");
                        pickCount = 1;
                    }
                }
                else
                {
                    if (eligible.Count == 0)
                    {
                        Debug.LogWarning("[Missions] Eligible list exhausted unexpectedly.");
                        break;
                    }

                    int idx = rng.Next(0, eligible.Count);
                    def = eligible[idx];
                    eligible.RemoveAt(idx);
                }

                int target;
                if (!RequiresLevelBands(def.type))
                {
                    target = 1; // binary completion
                }
                else
                {
                    target = def.GetTargetForLevel(levelNum);
                }

                // SurviveSeconds must not exceed phase 1 duration
                if (def.type == MissionType.SurviveSeconds && _levelManager != null)
                {
                    int maxPossible = Mathf.FloorToInt(_levelManager.Phase1Duration);
                    target = Mathf.Clamp(target, 1, maxPossible);
                }

                IMission mission = CreateMissionInstance(def.type, target);
                if (mission == null)
                {
                    Debug.LogWarning($"[Missions] CreateMissionInstance returned null for {def.type}. Skipping slot {slot}.");
                    continue;
                }

                _activeMissions.Add(mission);
                _missionSlotMap[mission] = slot;

                mission.StartMission();

                // Initial UI text
                OnMissionAssigned?.Invoke(slot, BuildDisplayText(def, mission));

                Debug.Log($"[Missions] Level {levelNum} slot {slot}: {def.type} | Target={target}");
            }
        }
        private bool RequiresLevelBands(MissionType type)
        {
            switch (type)
            {
                // Binary / fixed-target missions: level bands don't apply
                case MissionType.FinishPhase2NoFail:
                    return false;

                default:
                    return true;
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
                || type == MissionType.SurviveSeconds
                || type == MissionType.KillEnemiesWithDownAttack
                || type == MissionType.KillEnemiesWithDashAttack
                || type == MissionType.UsePowerSlam
                || type == MissionType.ReachPhase2Combo
                || type == MissionType.EarnPhase2Cash
                || type == MissionType.FinishPhase2NoFail;
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

                case MissionType.KillEnemiesWithDownAttack:
                    return new KillEnemiesWithDownAttackMission(target);

                case MissionType.KillEnemiesWithDashAttack:
                    return new KillEnemiesWithDashAttackMission(target);

                case MissionType.UsePowerSlam:
                    return new UsePowerSlamMission(target);

                case MissionType.ReachPhase2Combo:
                    return new ReachPhase2ComboMission(target);

                case MissionType.EarnPhase2Cash:
                    return new EarnPhase2CashMission(target);

                case MissionType.FinishPhase2NoFail:
                    return new FinishPhase2NoFailMission(target);

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
