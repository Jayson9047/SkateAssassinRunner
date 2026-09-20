using System;
using System.IO;
using System.Linq;
using IndieKit;
using MoreMountains.InfiniteRunnerEngine;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit Play Mode checks and a real-dash capture. Never included in a player build.</summary>
[InitializeOnLoad]
public static class Enemy1SliceValidation
{
    static double _started;
    static bool _setup, _dash, _recording;
    static int _frame, _kills;
    static float _nextCapture;
    static RenderTexture _target;
    static Texture2D _pixels;
    static SwipeRightAttackDetector _player;
    static GameObject[] _enemies;
    public static string Result = "Not run";
    const string CaptureFolder = "Documentation/Enemy1SliceValidation";

    static Enemy1SliceValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Enemy1Capture", false))
            {
                SessionState.SetBool("Enemy1Capture", false);
                _started = EditorApplication.timeSinceStartup;
                _setup = _dash = false;
                EditorApplication.update += CaptureUpdate;
            }
            if (state == PlayModeStateChange.ExitingPlayMode) StopCapture();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("Enemy1RestoreStartScene", false))
            {
                UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("Enemy1PreviousStartScene", ""));
                SessionState.SetBool("Enemy1RestoreStartScene", false);
            }
        };
    }

    public static void StartGameplayCapture(bool cosmetics = true, float speed = 20f)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Start from Edit mode.");
        SessionState.SetBool("Enemy1Capture", true);
        SessionState.SetBool("Enemy1Cosmetics", cosmetics);
        SessionState.SetFloat("Enemy1Speed", speed);
        SessionState.SetString("Enemy1PreviousStartScene", AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
        SessionState.SetBool("Enemy1RestoreStartScene", true);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SkateRunner.unity");
        EditorApplication.isPlaying = true;
    }

    public static string CheckPools()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in Play mode.");
        var pool = Enemy1SlicePool.Instance;
        if (!pool) throw new InvalidOperationException("Pool was not prewarmed by Enemy 1 Awake.");
        bool blood = pool.directionalBlood, contact = pool.contactStreak, ground = pool.groundBlood;
        var pos = LevelManager.Instance.StartingPosition.transform.position + Vector3.right * 3f;
        pos.y = 0.15f;
        var rot = Quaternion.Euler(0, 270, 0);
        pool.Clear();
        var parts = pool.GetComponentsInChildren<Enemy1SlicePresentation>(true);
        int objectCount = pool.GetComponentsInChildren<Transform>(true).Length;
        Assert(parts.Length == 8, "8 bodies prewarmed");
        Assert(pool.GetComponentsInChildren<Rigidbody>(true).Length == 0, "no debris rigidbodies");
        Assert(pool.GetComponentsInChildren<Collider>(true).Length == 0, "no debris colliders");
        Assert(!pool.Play(pool.legacyDebrisPrefab, pos, rot, Vector3.one, KillCause.Phase2), "Phase 2 bypass");
        Assert(!pool.Play(null, pos, rot, Vector3.one, KillCause.DashAttack), "unrelated debris bypass");
        pool.directionalBlood = pool.contactStreak = pool.groundBlood = true;
        pool.Play(pool.legacyDebrisPrefab, pos, rot, Vector3.one, KillCause.DashAttack);
        var body = parts.First(p => p.gameObject.activeSelf);
        float initialDelta = body.UpperCenter.x - body.LowerCenter.x;
        for (int i = 0; i < 12; i++) pool.Tick(1f / 60f, 40f);
        float scatter = body.UpperCenter.x - body.LowerCenter.x - initialDelta;
        Assert(scatter > 0.8f, "upper/lower scatter exceeds 0.8 units after 0.2s at level speed 40");
        for (int i = 0; i < 30; i++) pool.Play(pool.legacyDebrisPrefab, pos, rot, Vector3.one, KillCause.DashAttack);
        pool.Tick(0.001f, 40f);
        Assert(pool.ActiveBodies == 8 && pool.ActiveContacts == 8 && pool.ActiveGrounds == 4, "burst caps 8/8/4");
        pool.Clear();
        pool.directionalBlood = pool.contactStreak = pool.groundBlood = false;
        pool.Play(pool.legacyDebrisPrefab, pos, rot, Vector3.one, KillCause.DashAttack);
        pool.Tick(0.2f, 40f);
        Assert(pool.ActiveBodies == 1 && pool.ActiveContacts == 0 && pool.ActiveGrounds == 0, "cosmetics off keeps bodies");
        Assert(pool.GetComponentsInChildren<ParticleSystem>(true).All(p => p.particleCount == 0), "no stale particles after cosmetic fallback");
        pool.directionalBlood = pool.contactStreak = pool.groundBlood = true;
        for (int i = 0; i < 100; i++)
        {
            pool.Play(pool.legacyDebrisPrefab, pos, rot, Vector3.one, KillCause.DashAttack);
            pool.Tick(0.016f, 40f);
        }
        // Isolate our synchronous hot path from editor, audio, HUD, and existing attack allocations.
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            pool.Play(pool.legacyDebrisPrefab, pos, rot, Vector3.one, (i % 2 == 0) ? KillCause.DashAttack : KillCause.DownAttack);
            pool.Tick(0.016f, 40f);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        double milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Assert(allocated == 0, "zero managed bytes in 1000 warmed presentation calls + ticks: " + allocated);
        pool.Tick(2f, 40f);
        Assert(pool.ActiveBodies == 0 && pool.ActiveContacts == 0 && pool.ActiveGrounds == 0, "all cleanup complete");
        Assert(pool.GetComponentsInChildren<ParticleSystem>(true).All(p => p.particleCount == 0), "all particles cleared");
        Assert(pool.GetComponentsInChildren<Transform>(true).Length == objectCount, "fixed object count after reuse");
        pool.directionalBlood = blood; pool.contactStreak = contact; pool.groundBlood = ground;
        return "PASS: single, 30 simultaneous, 1000 reused, speed40, cosmetics off, 8/8/4 caps, cleanup, no physics, Phase2/unrelated bypass. Scatter=" + scatter.ToString("F3") + "; warmed managed bytes=" + allocated + "; fixed transforms=" + objectCount + "; Editor CPU ms for 1000 calls/ticks=" + milliseconds.ToString("F2");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Enemy1 validation: " + message);
    }

    static void CaptureUpdate()
    {
        if (!Application.isPlaying) return;
        try
        {
            if (!_setup)
            {
                _player = UnityEngine.Object.FindFirstObjectByType<SwipeRightAttackDetector>();
                if (!_player) { if (EditorApplication.timeSinceStartup - _started > 15) throw new Exception("Player did not spawn."); return; }
                foreach (var spawner in UnityEngine.Object.FindObjectsByType<DistanceSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
                // Isolate the test lane in this disposable Play session; keep the real road/camera/player.
                foreach (var col in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    if (col.gameObject.layer != LayerMask.NameToLayer("Ground") && !col.transform.IsChildOf(_player.transform)) col.enabled = false;
                var manager = SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor;
                manager.SetSpeed(SessionState.GetFloat("Enemy1Speed", 20f));
                manager.SpeedAcceleration = 0f;
                bool cosmetics = SessionState.GetBool("Enemy1Cosmetics", true);
                _enemies = new GameObject[3];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Human Enemies/Enemy1_Female.prefab");
                for (int i = 0; i < _enemies.Length; i++)
                {
                    _enemies[i] = UnityEngine.Object.Instantiate(prefab);
                    _enemies[i].name = "Enemy1_Validation_" + i;
                    _enemies[i].GetComponent<EnemyType1>().enabled = false;
                    foreach (var move in _enemies[i].GetComponentsInChildren<MovingObject>()) move.enabled = false;
                    foreach (var c in _enemies[i].GetComponentsInChildren<Collider>()) if (c.isTrigger) c.enabled = false;
                    _enemies[i].transform.SetPositionAndRotation(new Vector3(_player.transform.position.x + 2.5f + i * 1.8f, 0.10f, _player.transform.position.z), Quaternion.Euler(0, 270, 0));
                }
                var pool = Enemy1SlicePool.Instance;
                pool.directionalBlood = pool.contactStreak = pool.groundBlood = cosmetics;
                _target = new RenderTexture(640, 640, 24);
                _pixels = new Texture2D(640, 640, TextureFormat.RGB24, false);
                Directory.CreateDirectory(CaptureFolder);
                _setup = true;
                _started = EditorApplication.timeSinceStartup;
                SkateRunnerDestructibleObject.OnDestroyed += Killed;
                Result = "Preparing real dash capture";
            }
            if (!_dash && EditorApplication.timeSinceStartup - _started > 4)
            {
                foreach (var spawner in UnityEngine.Object.FindObjectsByType<DistanceSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<SkateRunnerDestructibleObject>(FindObjectsSortMode.None))
                    if (!enemy.name.StartsWith("Enemy1_Validation_")) enemy.gameObject.SetActive(false);
                GameManager.Instance.SetStatus(GameManager.GameStatus.GameInProgress);
                Time.timeScale = 1f;
                _player.TriggerTutorialDashAttack();
                _dash = _recording = true;
                _frame = _kills = 0;
                _nextCapture = Time.unscaledTime;
                Result = "Recording actual dash at normal time scale";
            }
            if (_recording && Time.unscaledTime >= _nextCapture)
            {
                _nextCapture = Time.unscaledTime + 0.05f;
                Camera camera = Camera.main;
                var previous = camera.targetTexture; var active = RenderTexture.active;
                camera.targetTexture = _target; camera.Render(); RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); _pixels.Apply();
                string tier = SessionState.GetBool("Enemy1Cosmetics", true) ? "full" : "core";
                File.WriteAllBytes(CaptureFolder + "/" + tier + "-" + _frame.ToString("D2") + ".png", _pixels.EncodeToPNG());
                camera.targetTexture = previous; RenderTexture.active = active;
                _frame++;
                if (_frame >= 30)
                {
                    Result = "Real dash capture complete: kills=" + _kills + ", frames=" + _frame + ", speed=" + SessionState.GetFloat("Enemy1Speed", 20f);
                    StopCapture();
                    Time.timeScale = 0f;
                }
            }
        }
        catch (Exception ex) { Result = ex.ToString(); StopCapture(); Time.timeScale = 0f; Debug.LogException(ex); }
    }

    static void Killed(SkateRunnerDestructibleObject enemy) { if (enemy.name.StartsWith("Enemy1_Validation_")) _kills++; }
    static void StopCapture()
    {
        EditorApplication.update -= CaptureUpdate;
        SkateRunnerDestructibleObject.OnDestroyed -= Killed;
        _recording = false;
        if (_target) UnityEngine.Object.DestroyImmediate(_target);
        if (_pixels) UnityEngine.Object.DestroyImmediate(_pixels);
    }
}
