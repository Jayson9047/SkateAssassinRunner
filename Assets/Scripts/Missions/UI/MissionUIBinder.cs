using System.Collections;
using UnityEngine;

namespace Elroi.Missions.UI
{
    public class MissionUIBinder : MonoBehaviour
    {
        [Header("Assign Mission UI Slots")]
        public MissionUIItem mission1;
        public MissionUIItem mission2;

        private void OnEnable()
        {
            StartCoroutine(SubscribeNextFrame());
        }

        private IEnumerator SubscribeNextFrame()
        {
            // Wait one frame so MissionSystem.Awake runs and sets accessor.
            yield return null;

            var ms = Elroi.Missions.MissionSystem.MissionSystemAccessor;
            if (ms == null)
            {
                Debug.LogError("[MissionsUI] MissionSystem accessor is NULL. Did you add MissionSystem to the scene?");
                yield break;
            }

            ms.OnMissionAssigned += HandleAssigned;
            ms.OnMissionProgressText += HandleProgressText;
            ms.OnMissionCompleted += HandleCompleted;

            // If missions already exist (ex: binder enabled late), force-refresh slot text
            var missions = ms.ActiveMissions;
            if (missions != null && missions.Count > 0)
            {
                HandleAssigned(0, $"{missions[0].Description} ({missions[0].Progress}/{missions[0].Target})");
                if (missions[0].IsComplete) HandleCompleted(0);
            }
            if (missions != null && missions.Count > 1)
            {
                HandleAssigned(1, $"{missions[1].Description} ({missions[1].Progress}/{missions[1].Target})");
                if (missions[1].IsComplete) HandleCompleted(1);
            }
        }

        private void OnDisable()
        {
            var ms = Elroi.Missions.MissionSystem.MissionSystemAccessor;
            if (ms == null) return;

            ms.OnMissionAssigned -= HandleAssigned;
            ms.OnMissionProgressText -= HandleProgressText;
            ms.OnMissionCompleted -= HandleCompleted;
        }

        private void HandleAssigned(int slot, string text) => GetSlot(slot)?.SetInitial(text);
        private void HandleProgressText(int slot, string text) => GetSlot(slot)?.SetProgress(text);
        private void HandleCompleted(int slot) => GetSlot(slot)?.SetComplete();

        private MissionUIItem GetSlot(int slot)
        {
            return slot switch
            {
                0 => mission1,
                1 => mission2,
                _ => null
            };
        }
    }
}
