namespace Elroi.Missions
{
    public class ReachPhase2ComboMission : IMission
    {
        public MissionType Type => MissionType.ReachPhase2Combo;

        public int Target { get; private set; }
        public int Progress { get; private set; } // stores max combo reached
        public bool IsComplete => Progress >= Target;
        public MissionCategory Category => MissionCategory.Phase2;

        public string Title => "PHASE 2 COMBO";
        public string Description => $"Reach {Target} combo hits in Phase 2";
        public ReachPhase2ComboMission(int target)
        {
            Target = target;
            Progress = 0;
        }

        public void StartMission()
        {
            TapOnlyMainActionZone.OnPhase2ComboUpdated += HandleComboUpdated;
        }

        public void StopMission()
        {
            TapOnlyMainActionZone.OnPhase2ComboUpdated -= HandleComboUpdated;
        }

        private void HandleComboUpdated(int combo)
        {
            if (IsComplete) return;

            // Track max combo reached this Phase 2
            if (combo > Progress)
            {
                Progress = combo;
                MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
            }
        }
    }
}
