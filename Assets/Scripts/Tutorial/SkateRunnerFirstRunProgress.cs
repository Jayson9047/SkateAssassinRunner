/// <summary>First-run routing is independent of the current level and tutorial replay settings.</summary>
public static class SkateRunnerFirstRunProgress
{
    public const string LevelPlayedKey = "SkateRunner.Onboarding.Level1Played.v1";
    public const string HomeStepKey = "SkateRunner.Onboarding.HomeStep.v1";
    public const string HomeCompleteKey = "SkateRunner.Onboarding.HomeComplete.v1";
    public const string IntroSeenKey = "SkateRunner.Onboarding.IntroSeen.v1";

    public static bool HasPlayedFirstLevel
    {
        get
        {
            if (ES3.KeyExists(LevelPlayedKey)) return ES3.Load(LevelPlayedKey, false);
            // Older builds save LevelNum when banking a result, never merely on game startup.
            bool legacyPlayer = ES3.KeyExists("LevelNum");
            if (legacyPlayer) ES3.Save(LevelPlayedKey, true);
            return legacyPlayer;
        }
    }

    public static bool HomeComplete => ES3.Load(HomeCompleteKey, false);
    public static int HomeStep => ES3.Load(HomeStepKey, 0);
    public static bool NeedsHomeTour => HasPlayedFirstLevel && !HomeComplete;
    public static string ResolveDestination(string homeScene, string gameScene) => HasPlayedFirstLevel ? homeScene : gameScene;
    public static void MarkLevelPlayed(int level) { if (level == 1) ES3.Save(LevelPlayedKey, true); }
    public static void SaveHomeStep(int step) => ES3.Save(HomeStepKey, step);
    public static void CompleteHomeTour() => ES3.Save(HomeCompleteKey, true);

    public static void Reset()
    {
        // Explicit false prevents migration from a subsequently recreated LevelNum key.
        ES3.Save(LevelPlayedKey, false);
        ES3.DeleteKey(HomeStepKey);
        ES3.DeleteKey(HomeCompleteKey);
        ES3.DeleteKey(IntroSeenKey);
    }
}
