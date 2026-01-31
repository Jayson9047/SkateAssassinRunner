using IndieKit;

namespace Elroi.Missions
{
    public class KillEnemiesMission : IMission
    {
        public MissionType Type => MissionType.KillEnemies;
        public MissionCategory Category => MissionCategory.Combat;

        public string Title => "KILL ENEMIES";
        public string Description => $"Kill {Target} enemies";

        public int Target { get; private set; }
        public int Progress { get; private set; }
        public bool IsComplete => Progress >= Target;

        public KillEnemiesMission(int target)
        {
            Target = target;
            Progress = 0;
        }

        public void StartMission()
        {
            SkateRunnerDestructibleObject.OnEnemyKilled += HandleEnemyKilled;
        }

        public void StopMission()
        {
            SkateRunnerDestructibleObject.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(SkateRunnerDestructibleObject obj)
        {
            if (IsComplete) return;

            Progress++;

            // Notify MissionSystem so GUI can update live
            MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
        }
    }
}
