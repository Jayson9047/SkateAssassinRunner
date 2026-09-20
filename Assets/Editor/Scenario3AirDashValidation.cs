using System;
using System.Linq;
using MoreMountains.InfiniteRunnerEngine;
using UnityEditor;
using UnityEngine;

/// <summary>Disposable Play-mode reproduction using Scenario3 and the production tap/dash actions.</summary>
[InitializeOnLoad]
public static class Scenario3AirDashValidation
{
    static Jumper player;
    static GameObject scenario;
    static double started;
    static int step;
    static bool setup;
    static BoxCollider blade, body;
    static Vector3 oldLocal;
    static bool history;
    static SweptBoxIntersection.BoxPose oldBlade, oldBody;
    static int ghostFrames;
    static float peak;
    public static string Result = "Not run";

    public static string CheckSweepRegression()
    {
        var hazard = new SweptBoxIntersection.BoxPose { Rotation = Quaternion.identity, HalfSize = new Vector3(.05f, 2f, .05f) };
        var playerPose = new SweptBoxIntersection.BoxPose { Position = new Vector3(3, 0, 0), Rotation = Quaternion.identity, HalfSize = new Vector3(.3f, .7f, .3f) };
        int checks = 0;
        for (int angle = 0; angle <= 180; angle += 5)
        {
            var rotated = hazard; rotated.Rotation = Quaternion.Euler(0, angle, 0);
            if (SweptBoxIntersection.IntersectsMovingBoxes(hazard, rotated, playerPose, playerPose))
                throw new Exception("False contact during axial sword rotation at " + angle);
            checks++;
        }
        var crossing = playerPose; crossing.Position.x = -3f;
        if (!SweptBoxIntersection.IntersectsMovingBoxes(hazard, hazard, playerPose, crossing)) throw new Exception("Thin obstacle tunneling regression");
        checks++;
        playerPose.Position.y = crossing.Position.y = 4f;
        if (SweptBoxIntersection.IntersectsMovingBoxes(hazard, hazard, playerPose, crossing)) throw new Exception("Overhead clearance regression");
        checks++;
        playerPose.Position = Vector3.zero;
        if (!SweptBoxIntersection.IntersectsMovingBoxes(hazard, hazard, playerPose, playerPose)) throw new Exception("Real weapon contact missed");
        checks++;
        var swung = hazard; swung.Rotation = Quaternion.Euler(0, 0, 90f);
        playerPose.Position = new Vector3(1.5f, 0, 0);
        if (!SweptBoxIntersection.IntersectsMovingBoxes(hazard, swung, playerPose, playerPose)) throw new Exception("Rotating blade contact missed");
        checks++;
        for (int i = 0; i < 100; i++) SweptBoxIntersection.IntersectsMovingBoxes(hazard, swung, playerPose, playerPose);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) SweptBoxIntersection.IntersectsMovingBoxes(hazard, swung, playerPose, playerPose);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0) throw new Exception("Sweep allocated " + allocated + " bytes");
        return "PASS: " + checks + " regression cases; zero managed bytes in 1000 warmed rotating sweeps.";
    }

    static Scenario3AirDashValidation()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Scenario3AirDashCheck", false))
            {
                SessionState.SetBool("Scenario3AirDashCheck", false);
                setup = history = false; step = ghostFrames = 0; peak = 0;
                started = EditorApplication.timeSinceStartup;
                EditorApplication.update += Tick;
            }
            if (s == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= Tick;
            if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("Scenario3RestoreScene", false))
            {
                UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("Scenario3PreviousScene", ""));
                SessionState.SetBool("Scenario3RestoreScene", false);
            }
        };
    }

    public static void Start()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Start in Edit mode.");
        SessionState.SetBool("Scenario3AirDashCheck", true);
        SessionState.SetBool("Scenario3RestoreScene", true);
        SessionState.SetString("Scenario3PreviousScene", AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SkateRunner.unity");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        try
        {
            if (!setup)
            {
                player = UnityEngine.Object.FindFirstObjectByType<Jumper>();
                if (!player) return;
                foreach (var s in UnityEngine.Object.FindObjectsByType<DistanceSpawner>(FindObjectsSortMode.None)) s.enabled = false;
                foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    if (c.gameObject.layer != LayerMask.NameToLayer("Ground") && !c.transform.IsChildOf(player.transform)) c.enabled = false;
                scenario = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MicroScenarios/Scenario3.prefab"));
                scenario.name = "Scenario3_AirDashValidation";
                // The real runner spawner supplies -X; raw prefab instantiation otherwise retains its -Z default.
                scenario.GetComponent<MovingObject>().SetDirection(Vector3.left);
                scenario.GetComponent<MovingObject>().enabled = false;
                blade = scenario.GetComponentsInChildren<BoxCollider>().First(c => c.GetComponent<KillsPlayerOnTouch_IgnoreDuringDownAttack>());
                body = player.GetComponent<BoxCollider>();
                // Keep the fixture ahead until the intro invincibility expires.
                Transform initialCart = scenario.GetComponentsInChildren<Transform>().First(c => c.name == "Hot_Dog");
                scenario.transform.position += new Vector3(player.transform.position.x + 100f - initialCart.position.x,
                    -initialCart.position.y, player.transform.position.z - initialCart.position.z);
                started = EditorApplication.timeSinceStartup; setup = true;
            }
            double t = EditorApplication.timeSinceStartup - started;
            if (step == 0 && t > 4.0)
            {
                foreach (var s in UnityEngine.Object.FindObjectsByType<DistanceSpawner>(FindObjectsSortMode.None)) s.enabled = false;
                foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    if (c.gameObject.layer != LayerMask.NameToLayer("Ground") && !c.transform.IsChildOf(player.transform) && !c.transform.IsChildOf(scenario.transform)) c.enabled = false;
                Transform cart = scenario.GetComponentsInChildren<Transform>().First(c => c.name == "Hot_Dog");
                scenario.transform.position += Vector3.right * (player.transform.position.x + 4f - cart.position.x);
                LevelManager.Instance.SetSpeed(15f); LevelManager.Instance.SpeedAcceleration = 0f;
                scenario.GetComponent<MovingObject>().enabled = true;
                GameManager.Instance.SetStatus(GameManager.GameStatus.GameInProgress);
                UnityEngine.Object.FindFirstObjectByType<TapOnlyMainActionZone>().TriggerTutorialMainAction();
                started = EditorApplication.timeSinceStartup; step = 1;
                Result = "First jump";
            }
            else if (step == 1 && t > 0.16)
            {
                UnityEngine.Object.FindFirstObjectByType<TapOnlyMainActionZone>().TriggerTutorialMainAction(); step = 2;
            }
            else if (step == 2 && t > 0.48)
            {
                player.GetComponent<SwipeRightAttackDetector>().TriggerTutorialDashAttack(); step = 3;
            }
            if (step > 0 && player)
            {
                peak = Mathf.Max(peak, player.transform.position.y);
                if (blade && blade.enabled)
                {
                    var rel = blade.transform.worldToLocalMatrix * body.transform.localToWorldMatrix;
                    Vector3 local = rel.MultiplyPoint3x4(body.center) - blade.center;
                    var h = SweptBoxIntersection.BoxPose.Capture(blade); var p = SweptBoxIntersection.BoxPose.Capture(body);
                    if (history && SweptBoxIntersection.Intersects(oldLocal, local, blade.size * .5f,
                        rel.MultiplyVector(Vector3.right * body.size.x * .5f), rel.MultiplyVector(Vector3.up * body.size.y * .5f), rel.MultiplyVector(Vector3.forward * body.size.z * .5f))
                        && !SweptBoxIntersection.IntersectsMovingBoxes(oldBlade, h, oldBody, p)) ghostFrames++;
                    oldLocal = local; oldBlade = h; oldBody = p; history = true;
                }
                else history = false;
            }
            if (step > 0 && (!player || !LevelManager.Instance.CurrentPlayableCharacters.Contains(player)))
            {
                Finish("Player died. peak=" + peak + "; old-only sword hits=" + ghostFrames); return;
            }
            if (step == 3 && t > 1.5)
                Finish("Completed double jump -> air dash -> descent alive, invincible=" + player.Invincible + "; y=" + player.transform.position.y + "; peak=" + peak + "; old-only sword hits=" + ghostFrames);
        }
        catch (Exception e) { Finish(e.ToString()); Debug.LogException(e); }
    }

    static void Finish(string message)
    {
        Result = message;
        EditorApplication.update -= Tick;
        Time.timeScale = 0f;
    }
}
