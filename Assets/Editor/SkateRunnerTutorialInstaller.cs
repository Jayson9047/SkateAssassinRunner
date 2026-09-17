using Elroi.Tutorials;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SkateRunnerTutorialInstaller
{
    private const string ScenePath = "Assets/Scenes/SkateRunner.unity";
    private const string PresentationPath = "Assets/ELROI/TutorialFramework/Prefabs/ELROI_TutorialCanvas.prefab";
    private const string ThemePath = "Assets/ELROI/TutorialFramework/Materials/ELROI_MangaTheme.asset";

    [MenuItem("ELROI/Tutorial Framework/Install Skate Runner Adapters")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError($"[ELROI Tutorials] Open {ScenePath} before installing the Skate Runner integration.");
            return;
        }

        TutorialManager manager = Object.FindFirstObjectByType<TutorialManager>();
        GameObject root;
        if (manager == null)
        {
            root = new GameObject("ELROI Tutorial Manager");
            Undo.RegisterCreatedObjectUndo(root, "Install ELROI Tutorial Framework");
            manager = root.AddComponent<TutorialManager>();
        }
        else
        {
            root = manager.gameObject;
        }

        SkateRunnerTutorialGameplayAdapter gameplay = GetOrAdd<SkateRunnerTutorialGameplayAdapter>(root);
        SkateRunnerTutorialVariableProvider variables = GetOrAdd<SkateRunnerTutorialVariableProvider>(root);
        SkateRunnerTutorialPersistence persistence = GetOrAdd<SkateRunnerTutorialPersistence>(root);
        TutorialPresentation presentation = AssetDatabase.LoadAssetAtPath<TutorialPresentation>(PresentationPath);
        TutorialTheme theme = AssetDatabase.LoadAssetAtPath<TutorialTheme>(ThemePath);
        Camera camera = Camera.main;
        manager.ConfigureProvidersForAuthoring(presentation, theme, camera, gameplay, variables, persistence);

        if (manager.MutableTutorialsForAuthoring.Count == 0)
            ConfigureTutorialContent(manager);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ELROI Tutorials] Installed one scene-local manager and Skate Runner adapters. Level sequences are intentionally disabled until target markers are assigned.", manager);
    }

    private static void ConfigureTutorialContent(TutorialManager manager)
    {
        TutorialDefinition jump = Make("Jump Tutorial", "Tutorial.JumpTarget", TutorialGesture.Tap, "Jump",
            "A hazard is approaching.", "Tap to Jump");
        TutorialDefinition doubleJump = Make("Double Jump Tutorial", "Tutorial.DoubleJumpObstacle", TutorialGesture.DoubleTap, "DoubleJump",
            "This obstacle is too large for a normal jump.", "Double Tap to Double Jump");
        TutorialDefinition groundDash = Make("Ground Dash Tutorial", "Tutorial.GroundDashEnemy", TutorialGesture.SwipeRight, "DashAttack",
            "Dash through this enemy from the ground.", "Swipe Right to Dash Attack");
        TutorialDefinition airDash = Make("Air Dash Tutorial", "Tutorial.AirDashTarget", TutorialGesture.SwipeRight, "AirDashAttack",
            "You are aligned for an airborne dash.", "Swipe Right to Air Dash");
        TutorialDefinition downAttack = Make("Down Attack Tutorial", "Tutorial.DownAttackTarget", TutorialGesture.SwipeDown, "DownAttack",
            "Strike the target beneath you.", "Swipe Down to Slam");

        manager.MutableTutorialsForAuthoring.Add(jump);
        manager.MutableTutorialsForAuthoring.Add(doubleJump);
        manager.MutableTutorialsForAuthoring.Add(groundDash);
        manager.MutableTutorialsForAuthoring.Add(airDash);
        manager.MutableTutorialsForAuthoring.Add(downAttack);

        TutorialSequencerDefinition level1 = MakeSequence("Level 1 Basic Controls", 1);
        level1.Entries.Add(TimeEntry(jump, 2f));
        level1.Entries.Add(DistanceEntry(doubleJump, "Tutorial.Player", "Tutorial.DoubleJumpObstacle", 4f, true, -0.5f, 0.5f));
        level1.Entries.Add(VisibilityEntry(groundDash, "Tutorial.GroundDashEnemy", 0.4f));
        manager.MutableSequencersForAuthoring.Add(level1);

        TutorialSequencerDefinition level2 = MakeSequence("Level 2 Advanced Controls", 2);
        level2.Entries.Add(DistanceEntry(airDash, "Tutorial.Player", "Tutorial.AirDashTarget", 4f, true, -0.5f, 0.5f));
        level2.Entries.Add(DistanceEntry(downAttack, "Tutorial.Player", "Tutorial.DownAttackTarget", 3.5f, true, -3f, -0.25f));
        manager.MutableSequencersForAuthoring.Add(level2);
    }

    private static TutorialDefinition Make(string name, string targetId, TutorialGesture gesture, string actionId, string intro, string instruction)
    {
        TutorialDefinition tutorial = new TutorialDefinition
        {
            Enabled = true,
            TutorialName = name,
            TargetType = TutorialTargetType.ThreeDimensional,
            TargetSource = TutorialTargetSource.RuntimeTargetId,
            RuntimeTargetId = targetId,
            CompletionType = TutorialCompletionType.Gesture,
            RequiredGesture = gesture,
            GameplayActionId = actionId,
            IntroductionText = intro,
            BottomInstructionText = instruction,
            FreezeWorld = true,
            SpotlightShape = TutorialSpotlightShape.RoundedRectangle
        };
        tutorial.EnsureStableId();
        return tutorial;
    }

    private static TutorialSequencerDefinition MakeSequence(string name, int level)
    {
        TutorialSequencerDefinition sequence = new TutorialSequencerDefinition
        {
            Enabled = false,
            SequenceName = name,
            RunPolicy = TutorialRunPolicy.OnceEver,
            LockGameplayBetweenTutorials = true
        };
        sequence.EnsureStableId();
        sequence.StartTrigger.TriggerType = TutorialTriggerType.VariableCondition;
        sequence.StartTrigger.VariableCondition.VariableId = "CurrentLevel";
        sequence.StartTrigger.VariableCondition.Comparison = TutorialComparisonOperator.Equal;
        sequence.StartTrigger.VariableCondition.ExpectedValue = TutorialValue.From(level);
        return sequence;
    }

    private static TutorialSequenceEntry TimeEntry(TutorialDefinition tutorial, float seconds)
    {
        TutorialSequenceEntry entry = Entry(tutorial, TutorialTriggerType.Time);
        entry.Trigger.DelaySeconds = seconds;
        entry.Trigger.TimeOrigin = TutorialTimeOrigin.ManagerStart;
        return entry;
    }

    private static TutorialSequenceEntry DistanceEntry(TutorialDefinition tutorial, string referenceId, string targetId, float x,
        bool useY, float yMin, float yMax)
    {
        TutorialSequenceEntry entry = Entry(tutorial, TutorialTriggerType.Distance);
        entry.Trigger.ReferenceTargetId = referenceId;
        entry.Trigger.TargetId = targetId;
        entry.Trigger.MaximumAbsoluteXDistance = x;
        entry.Trigger.UseYRange = useY;
        entry.Trigger.RelativeYMinimum = yMin;
        entry.Trigger.RelativeYMaximum = yMax;
        return entry;
    }

    private static TutorialSequenceEntry VisibilityEntry(TutorialDefinition tutorial, string targetId, float delay)
    {
        TutorialSequenceEntry entry = Entry(tutorial, TutorialTriggerType.VisibleOnScreen);
        entry.Trigger.TargetId = targetId;
        entry.Trigger.VisibilityDelay = delay;
        return entry;
    }

    private static TutorialSequenceEntry Entry(TutorialDefinition tutorial, TutorialTriggerType triggerType)
    {
        TutorialSequenceEntry entry = new TutorialSequenceEntry { Enabled = true, TutorialStableId = tutorial.StableId };
        entry.Trigger.TriggerType = triggerType;
        return entry;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T value = target.GetComponent<T>();
        return value != null ? value : target.AddComponent<T>();
    }
}
