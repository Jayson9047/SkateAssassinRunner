using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

namespace Elroi.Missions
{
    public class SurviveSecondsMission : IMission
    {
        public MissionType Type => MissionType.SurviveSeconds;
        public MissionCategory Category => MissionCategory.Survival;

        public string Title => "SURVIVE";
        public string Description => $"Survive {Target} seconds";

        public int Target { get; private set; }
        public int Progress { get; private set; }
        public bool IsComplete => Progress >= Target;

        private SkateAssassinRunnerLevelManager _lm;
        private bool _running;

        public SurviveSecondsMission(int target, SkateAssassinRunnerLevelManager levelManager)
        {
            Target = target;
            _lm = levelManager;
            Progress = 0;
        }

        public void StartMission()
        {
            _running = true;

            // Force an initial UI update
            UpdateFromLevelTimer(force: true);
        }

        public void StopMission()
        {
            _running = false;
        }

        // Called by MissionSystem.Update() once per frame (single call, no per-mission ticking list)
        public void UpdateFromLevelTimer(bool force = false)
        {
            if (!_running || IsComplete || _lm == null) return;

            int seconds = Mathf.FloorToInt(_lm.Phase1ElapsedSeconds);
            if (force || seconds != Progress)
            {
                Progress = seconds;
                MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
            }
        }
    }
}
