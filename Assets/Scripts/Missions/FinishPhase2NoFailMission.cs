using MoreMountains.InfiniteRunnerEngine;

namespace Elroi.Missions
{
    public class FinishPhase2NoFailMission : IMission
    {
        public MissionType Type => MissionType.FinishPhase2NoFail;
        public MissionCategory Category => MissionCategory.Phase2;

        public string Title => "DON'T DIE ON PHASE 2";
        public string Description => $"Finish Phase 2 without losing a single life";
        public int Target { get; private set; } = 1;   // binary mission
        public int Progress { get; private set; } = 0; // 0 = not completed, 1 = completed
        public bool IsComplete => Progress >= 1;

        private bool _phase2Started;
        private bool _failedInPhase2;

        public FinishPhase2NoFailMission(int targetIgnored)
        {
            // Ignore target from definition; this mission is inherently binary.
            Target = 1;
            Progress = 0;
        }

        public void StartMission()
        {
            SkateAssassinRunnerLevelManager.OnPhase2Started += HandlePhase2Started;
            SkateAssassinRunnerLevelManager.OnPhase2LifeLost += HandlePhase2LifeLost;

            // If you add the LevelWon event below, subscribe here:
            SkateAssassinRunnerLevelManager.OnLevelWon += HandleLevelWon;
        }

        public void StopMission()
        {
            SkateAssassinRunnerLevelManager.OnPhase2Started -= HandlePhase2Started;
            SkateAssassinRunnerLevelManager.OnPhase2LifeLost -= HandlePhase2LifeLost;

            SkateAssassinRunnerLevelManager.OnLevelWon -= HandleLevelWon;
        }

        private void HandlePhase2Started()
        {
            _phase2Started = true;
        }

        private void HandlePhase2LifeLost()
        {
            _failedInPhase2 = true;
        }

        private void HandleLevelWon()
        {
            if (IsComplete) return;

            // Only count if Phase2 actually happened this run
            if (!_phase2Started) return;

            // Must not have lost a life during Phase2
            if (_failedInPhase2) return;

            Progress = 1;
            MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
        }
    }
}
