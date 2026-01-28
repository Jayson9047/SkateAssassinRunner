using MoreMountains.InfiniteRunnerEngine;

namespace Elroi.Missions
{
    public class UsePowerSlamMission : IMission
    {
        public MissionType Type => MissionType.UsePowerSlam;

        public int Target { get; private set; }
        public int Progress { get; private set; }
        public bool IsComplete => Progress >= Target;

        public MissionCategory Category => MissionCategory.Skill;

        public string Title => "Powerslam KILLS";
        public string Description => $"Perform Powerslam {Target} Times In Phase 1";

        public UsePowerSlamMission(int target)
        {
            Target = target;
            Progress = 0;
        }

        public void StartMission()
        {
            SkateRunnerGUIManager.OnPowerSlamUsed += HandlePowerSlamUsed;
        }

        public void StopMission()
        {
            SkateRunnerGUIManager.OnPowerSlamUsed -= HandlePowerSlamUsed;
        }

        private void HandlePowerSlamUsed()
        {
            if (IsComplete) return;

            Progress++;
            MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
        }
    }
}
