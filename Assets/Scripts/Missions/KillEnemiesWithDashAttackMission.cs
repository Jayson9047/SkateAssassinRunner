using IndieKit;

namespace Elroi.Missions
{
    public class KillEnemiesWithDashAttackMission : IMission
    {
        public MissionType Type => MissionType.KillEnemiesWithDashAttack;

        public int Target { get; private set; }
        public int Progress { get; private set; }
        public bool IsComplete => Progress >= Target;

        public MissionCategory Category => MissionCategory.Combat;

        public string Title => "DASH-ATTACK KILLS";
        public string Description => $"Kill {Target} enemies using Dash-Attack";
        public KillEnemiesWithDashAttackMission(int target)
        {
            Target = target;
            Progress = 0;
        }

        public void StartMission()
        {
            SkateRunnerDestructibleObject.OnEnemyKilledWithCause += OnEnemyKilledWithCause;
        }

        public void StopMission()
        {
            SkateRunnerDestructibleObject.OnEnemyKilledWithCause -= OnEnemyKilledWithCause;
        }

        private void OnEnemyKilledWithCause(SkateRunnerDestructibleObject enemy, KillCause cause)
        {
            if (IsComplete) return;
            if (cause != KillCause.DashAttack) return;

            Progress++;
            MissionSystem.MissionSystemAccessor?.NotifyMissionProgressChanged(this);
        }
    }
}
