using System;

namespace Elroi.Missions
{
    public abstract class MissionTemplate
    {
        public abstract MissionType Type { get; }
        public abstract MissionCategory Category { get; }

        public abstract IMission CreateInstance(int levelNum, Random rng);
    }
}
