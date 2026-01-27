namespace Elroi.Missions
{
    public interface IMission
    {
        MissionType Type { get; }
        MissionCategory Category { get; }

        string Title { get; }
        string Description { get; }

        int Target { get; }
        int Progress { get; }
        bool IsComplete { get; }

        void StartMission();
        void StopMission();
    }
}
