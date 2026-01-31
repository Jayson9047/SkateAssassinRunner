namespace IndieKit
{
    /// <summary>
    /// Simple global context used to tag the "cause" of a kill at the moment damage is applied.
    /// Not thread-safe by design (Unity gameplay runs on main thread).
    /// </summary>
    public static class KillContext
    {
        public static KillCause Current { get; private set; } = KillCause.Unknown;

        public static void Set(KillCause cause) => Current = cause;

        public static void Clear() => Current = KillCause.Unknown;
    }
}
