using MoreMountains.InfiniteRunnerEngine;

namespace Elroi.Missions
{
    public class EarnCashMission : IMission
    {
        public MissionType Type => MissionType.EarnCash;
        public MissionCategory Category => MissionCategory.Economy;

        public string Title => "EARN CASH";
        public string Description => $"Earn {Target} cash";

        public int Target { get; private set; }
        public int Progress { get; private set; }
        public bool IsComplete => Progress >= Target;

        public EarnCashMission(int target)
        {
            Target = target;
            Progress = 0;
        }

        public void StartMission()
        {
            // Initialize with current earned-this-level (should be 0 at level start, but safe)
            var gm = SkateRunnerGameManager.SkateRunnerGameManagerAccessor;
            if (gm != null)
            {
                Progress = (int)gm.GetCashEarnedThisLevel();
            }

            SkateRunnerGameManager.OnCashAdded += HandleCashAdded;
        }

        public void StopMission()
        {
            SkateRunnerGameManager.OnCashAdded -= HandleCashAdded;
        }

        private void HandleCashAdded(float amount)
        {
            if (IsComplete) return;

            var gm = SkateRunnerGameManager.SkateRunnerGameManagerAccessor;
            if (gm == null) return;

            // This ensures we track "this run" (this level) not lifetime cash
            Progress = (int)gm.GetCashEarnedThisLevel();

            MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
        }
    }
}
