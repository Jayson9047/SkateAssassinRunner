namespace Elroi.Missions
{
    public class EarnPhase2CashMission : IMission
    {
        public MissionType Type => MissionType.EarnPhase2Cash;

        public int Target { get; private set; }
        public int Progress { get; private set; } // total cash earned during Phase 2
        public bool IsComplete => Progress >= Target;

        public MissionCategory Category => MissionCategory.Phase2;

        public string Title => "PHASE 2 Cash";
        public string Description => $"Earn {Target} cash in Phase 2";

        public EarnPhase2CashMission(int target)
        {
            Target = target;
            Progress = 0;
        }

        public void StartMission()
        {
            TapOnlyMainActionZone.OnPhase2CashEarned += HandleCashEarned;
        }

        public void StopMission()
        {
            TapOnlyMainActionZone.OnPhase2CashEarned -= HandleCashEarned;
        }

        private void HandleCashEarned(int cashDelta)
        {
            if (IsComplete) return;
            if (cashDelta <= 0) return;

            Progress += cashDelta;
            MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
        }
    }
}
