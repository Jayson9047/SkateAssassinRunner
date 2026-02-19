namespace IndieKit
{
    /// <summary>
    /// Global context used to tag the "cause" + "attack instance" of a kill at the moment damage is applied.
    /// </summary>
    public static class KillContext
    {
        public static KillCause Current { get; private set; } = KillCause.Unknown;
        public static int CurrentAttackId { get; private set; } = 0;

        public static void Set(KillCause cause, int attackId = 0)
        {
            Current = cause;
            CurrentAttackId = attackId;
        }

        public static void Clear()
        {
            Current = KillCause.Unknown;
            CurrentAttackId = 0;
        }
    }
}
