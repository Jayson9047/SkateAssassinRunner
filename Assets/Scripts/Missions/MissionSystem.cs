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

        [Header("Mission Definitions (configure in Inspector)")]
        public List<MissionDefinition> missionDefinitions = new List<MissionDefinition>();

        private readonly List<IMission> _activeMissions = new();
        public IReadOnlyList<IMission> ActiveMissions => _activeMissions;

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

            // slot 0: Kill Enemies (existing)
            var killDef = GetDefinition(MissionType.KillEnemies);
            if (killDef != null)
            {
                int target = killDef.GetTargetForLevel(levelNum);
                IMission mission = new KillEnemiesMission(target);
                _activeMissions.Add(mission);
                mission.StartMission();
                OnMissionAssigned?.Invoke(0, BuildProgressText(killDef, mission));
            }
            else
            {
                UnityEngine.Debug.LogError("[Missions] KillEnemies definition missing in inspector.");
            }

            // slot 1: Earn Cash (new)
            var cashDef = GetDefinition(MissionType.EarnCash);
            if (cashDef != null)
            {
                int target = cashDef.GetTargetForLevel(levelNum);
                IMission mission = new EarnCashMission(target);
                _activeMissions.Add(mission);
                mission.StartMission();
                OnMissionAssigned?.Invoke(1, BuildProgressText(cashDef, mission));
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Missions] EarnCash definition missing in inspector (slot 1 will be empty).");
            }
        }

        public void OnLevelEnd(bool success)
        {
            for (int i = 0; i < _activeMissions.Count; i++)
            {
                var m = _activeMissions[i];
                Debug.Log($"[Missions] End Level {_currentLevelNum} | Success={success} | Progress={m.Progress}/{m.Target} | Complete={m.IsComplete}");
            }
        }

        internal void NotifyMissionProgressChanged(IMission mission)
        {
            int slot = _activeMissions.IndexOf(mission);
            if (slot < 0) return;

            var def = GetDefinition(mission.Type);
            string txt = BuildProgressText(def, mission);

            OnMissionProgressText?.Invoke(slot, txt);

            if (mission.IsComplete)
                OnMissionCompleted?.Invoke(slot);
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

        private string BuildProgressText(MissionDefinition def, IMission m)
        {
            // If def missing, fall back to mission.Description
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
