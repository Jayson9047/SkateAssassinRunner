using System;
using System.Collections;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

/// <summary>
/// Captures gameplay prefab clones during an isolated, automated Play Mode session.
/// Rebuilds only animated Katana/aura previews. Existing card images, card UI,
/// ownership, equip state, source prefabs and gameplay definitions are untouched.
/// </summary>
[InitializeOnLoad]
public static class WeaponPowerPreviewPipelineBuilder
{
    public const string OutputFolder = "Assets/Prefabs/PreviewSprites/AbilitySprites";
    public const string PreviewFolder = OutputFolder + "/KatanaPreviews";
    public const string PreviewState = "AbilityKatanaPreview";
    public const string ScenePath = "Assets/Scenes/SkateRunnerStartScreen.unity";
    const string WeaponKey = "Katana_Default";
    const string DefinitionFolder = "Assets/Prefabs/VFX/WeaponPowerVFX";
    const string ControllerPath = PreviewFolder + "/AbilityKatanaPreview.controller";
    const string CaptureScenePath = "Assets/Editor/Weapons/AbilityPreviewCapture_Temporary.unity";
    const string SessionKey = "SkateRunner.AbilityCapture.";
    const int FrameSize = 512;
    const int Frames = 24;
    const int Columns = 6;
    const int Rows = 4;
    const float Fps = 12f;
    const int SimulationStepsPerFrame = 5; // 60 Hz simulation, 12 FPS sprites.
    const float SimulationStep = 1f / 60f;
    const float YawDegrees = 18f; // Never show the blade edge-on.
    static readonly WeaponPowerId[] Ids = { WeaponPowerId.None, WeaponPowerId.Fire,
        WeaponPowerId.Ice, WeaponPowerId.Electricity, WeaponPowerId.Poison, WeaponPowerId.Magic };

    static IEnumerator job;
    static CaptureStage stage;
    public static bool IsBuilding => job != null || SessionState.GetBool(SessionKey + "active", false);
    public static string Status { get; private set; } = "Idle";
    public static string LastError { get; private set; }

    static WeaponPowerPreviewPipelineBuilder()
    {
        AssemblyReloadEvents.beforeAssemblyReload += DisposeCapture;
        EditorApplication.quitting += Cancel;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        // A script reload during capture must not strand an isolated Play session.
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SessionKey + "active", false) && EditorApplication.isPlaying && job == null)
                Cancel();
        };
    }

    [MenuItem("Tools/Skate Runner/Abilities/Rebuild Animated Previews and Bind Inventory")]
    public static void Rebuild()
    {
        if (IsBuilding) throw new InvalidOperationException("An Ability capture is already running.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Rebuild Ability visuals in Edit Mode.");
        ValidateInputs();
        EnsureFolder(PreviewFolder);
        SessionState.SetString(SessionKey + "cards", CardFingerprint());
        LastError = null;
        SessionState.EraseString(SessionKey + "error");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CaptureScenePath))
            throw new InvalidOperationException("A previous temporary capture scene still exists at " + CaptureScenePath + ". Remove that leftover capture artifact before rebuilding.");
        SessionState.SetString(SessionKey + "previousStart", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetInt(SessionKey + "quality", QualitySettings.GetQualityLevel());
        var activeScene = SceneManager.GetActiveScene();
        var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var sessionCamera = new GameObject("Capture session camera", typeof(Camera), typeof(AudioListener));
        SceneManager.MoveGameObjectToScene(sessionCamera, empty);
        sessionCamera.GetComponent<Camera>().enabled = false;
        var light = new GameObject("Capture session light", typeof(Light));
        SceneManager.MoveGameObjectToScene(light, empty);
        light.GetComponent<Light>().type = LightType.Directional;
        light.GetComponent<Light>().enabled = false;
        EditorSceneManager.SaveScene(empty, CaptureScenePath);
        SceneManager.SetActiveScene(activeScene);
        EditorSceneManager.CloseScene(empty, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CaptureScenePath);
        SessionState.SetBool(SessionKey + "active", true);
        SessionState.SetBool(SessionKey + "bind", false);
        Status = "Entering isolated capture session";
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tools/Skate Runner/Abilities/Bind Existing Animated Previews")]
    public static void BindExisting()
    {
        if (IsBuilding || EditorApplication.isPlayingOrWillChangePlaymode) return;
        BindInventory();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Skate Runner/Abilities/Cancel Capture")]
    public static void Cancel()
    {
        DisposeCapture();
        SessionState.SetBool(SessionKey + "bind", false);
        if (SessionState.GetBool(SessionKey + "active", false))
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
            else FinishSession();
        }
        Status = "Cancelled";
    }

    static void DisposeCapture()
    {
        EditorApplication.update -= Tick;
        (job as IDisposable)?.Dispose();
        job = null;
        stage?.Dispose();
        stage = null;
        EditorUtility.ClearProgressBar();
    }

    static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey + "active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            job = Build();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode) DisposeCapture();
        else if (state == PlayModeStateChange.EnteredEditMode) FinishSession();
    }

    static void FinishSession()
    {
        bool bind = SessionState.GetBool(SessionKey + "bind", false);
        string previous = SessionState.GetString(SessionKey + "previousStart", "");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        QualitySettings.SetQualityLevel(SessionState.GetInt(SessionKey + "quality", QualitySettings.GetQualityLevel()), true);
        SessionState.SetBool(SessionKey + "active", false);
        SessionState.SetBool(SessionKey + "bind", false);
        AssetDatabase.DeleteAsset(CaptureScenePath);
        LastError = SessionState.GetString(SessionKey + "error", "");
        SessionState.EraseString(SessionKey + "error");
        if (!bind) { Status = string.IsNullOrEmpty(LastError) ? "Cancelled" : "Failed: " + LastError; return; }
        try
        {
            if (SessionState.GetString(SessionKey + "cards", "") != CardFingerprint())
                throw new InvalidOperationException("Card assets changed during capture; preview binding aborted.");
            BindInventory();
            AssetDatabase.SaveAssets();
            Status = "Complete: six animated Katana/aura previews bound; existing cards preserved.";
            Debug.Log("[Ability Previews] " + Status);
        }
        catch (Exception e) { LastError = e.ToString(); Status = "Binding failed: " + e.Message; Debug.LogException(e); }
    }

    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) { Cancel(); return; }
            if (job != null && !job.MoveNext())
            {
                DisposeCapture();
                SessionState.SetBool(SessionKey + "bind", true);
                Status = "Returning to Edit Mode to bind Inventory";
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception e)
        {
            SessionState.SetString(SessionKey + "error", e.ToString());
            Cancel();
            LastError = e.ToString();
            Status = "Failed: " + e.Message;
            Debug.LogException(e);
        }
    }

    static string Label(WeaponPowerId id) => id == WeaponPowerId.None ? "Default" : id.ToString();
    static string SheetPath(WeaponPowerId id) => PreviewFolder + "/" + Label(id) + "PowerPreview.png";
    static string ClipPath(WeaponPowerId id) => PreviewFolder + "/" + Label(id) + "PowerPreview.anim";
    static WeaponPowerDefinition Definition(WeaponPowerId id) =>
        AssetDatabase.LoadAssetAtPath<WeaponPowerDefinition>(DefinitionFolder + "/WP_" + id + ".asset");

    static void ValidateInputs()
    {
        foreach (var id in Ids)
        {
            var def = Definition(id);
            if (!def || (id != WeaponPowerId.None && !def.weaponAuraPrefab))
                throw new InvalidOperationException("Incomplete real gameplay content for " + id + ". Existing previews were left intact.");
            if (!def.TryGetOverride(WeaponKey, out var tuning) || !tuning.weaponPrefab)
                throw new InvalidOperationException("Missing Katana_Default tuning/prefab for " + id);
        }
    }

    static string CardFingerprint()
    {
        using (var hash = SHA256.Create())
            return string.Join("|", Ids.SelectMany(id => new[] {
                OutputFolder + "/" + Label(id) + "PowerCard.png",
                OutputFolder + "/" + Label(id) + "PowerCard.png.meta" })
                .Select(path => File.Exists(path) ? BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))) : "missing:" + path));
    }

    static IEnumerator Build()
    {
        foreach (var id in Ids)
        {
            Status = "Warming " + Label(id) + " Katana aura";
            stage = new CaptureStage(Definition(id));
            // Simulate actual GPU VFX over player-loop frames, not a synchronous
            // loop. The capture session is isolated from the user's game scene.
            for (int i = 0; i < 90; i++)
            {
                stage.Step(SimulationStep);
                stage.RenderWarmup();
                yield return null;
            }
            stage.VerifySimulation();
            var sheet = new Texture2D(FrameSize * Columns, FrameSize * Rows, TextureFormat.RGBA32, false);
            try
            {
                for (int i = 0; i < Frames; i++)
                {
                    Status = "Capturing " + Label(id) + " Katana preview " + (i + 1) + "/" + Frames;
                    for (int sub = 0; sub < SimulationStepsPerFrame; sub++)
                    {
                        stage.SetPhase((i + (sub + 1f) / SimulationStepsPerFrame) / Frames);
                        stage.Step(SimulationStep);
                        stage.RenderWarmup();
                        yield return null;
                    }
                    using (var pixels = stage.Capture(FrameSize))
                    {
                        VerifyPixels(pixels.Texture, Label(id) + " preview frame " + i);
                        sheet.SetPixels((i % Columns) * FrameSize, (Rows - 1 - i / Columns) * FrameSize,
                            FrameSize, FrameSize, pixels.Texture.GetPixels());
                    }
                }
                sheet.Apply();
                File.WriteAllBytes(SheetPath(id), sheet.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(sheet); }
            stage.Dispose(); stage = null;
            ImportTexture(SheetPath(id));
            BuildClip(id);
            yield return null;
        }
        ConfigureController();
        AssetDatabase.SaveAssets();
        Status = "Verifying selection and shared preview playback";
        var checks = WeaponPowerPreviewPipelineChecks.CheckPlayback();
        try { while (checks.MoveNext()) yield return checks.Current; }
        finally { (checks as IDisposable)?.Dispose(); }
    }

    static void VerifyPixels(Texture2D texture, string label)
    {
        int visible = texture.GetPixels32().Count(p => p.a > 8);
        if (visible < 32) throw new InvalidOperationException(label + " rendered empty; existing UI has not been rebound.");
    }

    sealed class CapturedPixels : IDisposable
    {
        public Texture2D Texture;
        public void Dispose() { if (Texture) Object.DestroyImmediate(Texture); }
    }

    sealed class CaptureStage : IDisposable
    {
        readonly Scene scene;
        readonly Camera camera;
        readonly GameObject content;
        readonly ParticleSystem[] particles;
        readonly ParticleSystem[] particleRoots;
        readonly VisualEffect[] graphs;
        RenderTexture target;

        public CaptureStage(WeaponPowerDefinition def)
        {
            scene = SceneManager.CreateScene("Ability capture stage");
            try
            {
                camera = NewObject("AbilityCaptureCamera").AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.cullingMask = 1 << 31;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                data.volumeLayerMask = 0;
                data.requiresColorTexture = true;
                data.requiresDepthTexture = true;
                Light key = NewObject("Capture Key").AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = 2f;
                key.cullingMask = 1 << 31;
                key.transform.rotation = Quaternion.Euler(20f, 55f, 0f);
                Light fill = NewObject("Capture Fill").AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = 0.8f;
                fill.cullingMask = 1 << 31;
                fill.transform.rotation = Quaternion.Euler(-15f, -120f, 0f);

                content = NewObject("Capture Content");
                def.TryGetOverride(WeaponKey, out var tuning);
                var katana = Clone(tuning.weaponPrefab, content.transform);
                katana.transform.localPosition = Vector3.zero;
                katana.transform.localRotation = Quaternion.identity;
                katana.transform.localScale = Vector3.one;
                // Suppress the baked-in Ice effect on the clone only.
                foreach (var fx in katana.GetComponentsInChildren<VisualEffect>(true)) fx.gameObject.SetActive(false);
                foreach (var ps in katana.GetComponentsInChildren<ParticleSystem>(true)) ps.gameObject.SetActive(false);
                if (def.weaponPowerId != WeaponPowerId.None)
                {
                    var identity = katana.GetComponentInChildren<WeaponIdentity>(true);
                    if (!identity || !identity.AuraAnchor) throw new InvalidOperationException("Katana aura anchor is missing.");
                    var aura = Clone(def.weaponAuraPrefab, identity.AuraAnchor);
                    aura.transform.localPosition = def.GetAuraPos(WeaponKey);
                    aura.transform.localRotation = Quaternion.Euler(def.GetAuraRot(WeaponKey));
                    aura.transform.localScale = def.GetAuraScale(WeaponKey);
                }
                // A shared fixed camera avoids zoom jitter between powers/frames.
                Frame(new Bounds(new Vector3(0f, 0.38f, 0f), new Vector3(1.2f, 1.32f, 1.2f)), 1.08f);
                particles = content.GetComponentsInChildren<ParticleSystem>(false);
                particleRoots = particles.Where(p => !p.transform.parent.GetComponentInParent<ParticleSystem>()).ToArray();
                graphs = content.GetComponentsInChildren<VisualEffect>(false);
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = particles[i].main;
                    main.stopAction = ParticleSystemStopAction.None;
                    particles[i].useAutoRandomSeed = false;
                    particles[i].randomSeed = (uint)(107 + i);
                }
                for (int i = 0; i < graphs.Length; i++)
                {
                    graphs[i].resetSeedOnPlay = false;
                    graphs[i].startSeed = (uint)(307 + i);
                }
                Restart();
            }
            catch { Dispose(); throw; }
        }

        GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.layer = 31;
            return go;
        }

        GameObject Clone(GameObject prefab, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.SetParent(parent, false);
            go.SetActive(true);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            // Do not allow script-driven destruction or gameplay side effects in
            // the capture scene; ParticleSystem and VisualEffect are not scripts.
            foreach (var script in go.GetComponentsInChildren<MonoBehaviour>(true)) if (script) script.enabled = false;
            return go;
        }

        public void Restart()
        {
            foreach (var p in particleRoots)
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                p.Simulate(0f, true, true, false);
            }
            foreach (var v in graphs) { v.Reinit(); v.Play(); v.pause = true; }
        }

        public void Step(float dt)
        {
            foreach (var p in particleRoots) p.Simulate(dt, true, false, false);
            foreach (var v in graphs) v.Simulate(dt, 1);
            EditorApplication.QueuePlayerLoopUpdate();
        }

        public void VerifySimulation()
        {
            if (graphs.Length > 0 && !graphs.Any(v => v.aliveParticleCount > 0))
                throw new InvalidOperationException("The real aura did not simulate. No static/fake aura preview will be accepted.");
        }

        public void SetPhase(float phase)
        {
            content.transform.localRotation = Quaternion.Euler(0f, YawDegrees * Mathf.Sin(phase * Mathf.PI * 2f), 0f);
        }

        public void Frame(Bounds bounds, float padding)
        {
            Vector3 direction = new Vector3(-1f, 0f, -0.25f).normalized;
            camera.transform.position = bounds.center + direction * 10f;
            camera.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            Vector3 e = bounds.extents;
            var right = camera.transform.right; var up = camera.transform.up;
            float width = Mathf.Abs(right.x) * e.x + Mathf.Abs(right.y) * e.y + Mathf.Abs(right.z) * e.z;
            float height = Mathf.Abs(up.x) * e.x + Mathf.Abs(up.y) * e.y + Mathf.Abs(up.z) * e.z;
            camera.orthographicSize = Mathf.Max(width, height) * padding;
            camera.aspect = 1f;
        }

        void EnsureTarget(int size)
        {
            if (target && target.width == size) return;
            if (target) { camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); }
            target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGBHalf);
            target.Create(); camera.targetTexture = target;
        }

        public void RenderWarmup()
        {
            EnsureTarget(64);
            camera.backgroundColor = Color.clear;
            camera.Render();
        }

        Color[] Render(Color background, int size)
        {
            EnsureTarget(size);
            camera.backgroundColor = background;
            camera.Render();
            var previous = RenderTexture.active;
            var readback = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                readback.Apply();
                return readback.GetPixels();
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(readback); }
        }

        public CapturedPixels Capture(int size)
        {
            // Dual-background reconstruction preserves opaque metal, translucent
            // smoke and additive glow in ordinary straight-alpha UI sprites.
            // Some gameplay shaders output zero alpha for emissive particles.
            // Their black-background radiance supplies a minimum coverage alpha.
            var black = Render(Color.black, size);
            var white = Render(Color.white, size);
            for (int i = 0; i < black.Length; i++)
            {
                Color b = black[i]; Color w = white[i];
                float opacity = 1f - Mathf.Max(w.r - b.r, Mathf.Max(w.g - b.g, w.b - b.b));
                // Keep HDR flame/lightning detail instead of clipping emissive
                // colors to solid blocks. Tone mapping is capture-only.
                b.r = ToneMap(b.r); b.g = ToneMap(b.g); b.b = ToneMap(b.b);
                float a = Mathf.Clamp01(Mathf.Max(opacity, Mathf.Max(b.r, Mathf.Max(b.g, b.b))));
                black[i] = a > 1f / 255f ? new Color(b.r / a, b.g / a, b.b / a, a) : Color.clear;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(black); texture.Apply();
            return new CapturedPixels { Texture = texture };
        }

        static float ToneMap(float linear)
        {
            linear = Mathf.Max(0f, linear);
            return Mathf.LinearToGammaSpace(linear / (1f + linear));
        }

        public void Dispose()
        {
            if (target) { if (camera) camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); }
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    static void ImportTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects().ToDictionary(x => x.name, x => x.spriteID);
        var rects = new SpriteRect[Frames];
        for (int i = 0; i < Frames; i++)
        {
            string name = Path.GetFileNameWithoutExtension(path) + "_" + i.ToString("00");
            rects[i] = new SpriteRect { name = name,
                rect = new Rect(i % Columns * FrameSize, (Rows - 1 - i / Columns) * FrameSize, FrameSize, FrameSize),
                alignment = SpriteAlignment.Center, pivot = Vector2.one * 0.5f,
                spriteID = existing.TryGetValue(name, out var guid) ? guid : GUID.Generate() };
        }
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(
            rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
    }

    static AnimationClip BuildClip(WeaponPowerId id)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath(id)).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (sprites.Length != Frames) throw new InvalidOperationException("Invalid sheet for " + id);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(id));
        if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ClipPath(id)); }
        clip.ClearCurves(); clip.frameRate = Fps;
        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / Fps, value = s }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true; settings.loopBlend = false; settings.startTime = 0; settings.stopTime = Frames / Fps;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimatorController ConfigureController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == PreviewState);
        if (!state) state = machine.AddState(PreviewState);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(WeaponPowerId.None));
        machine.defaultState = state;
        EditorUtility.SetDirty(state); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller);
        return controller;
    }

    static void BindInventory()
    {
        foreach (var id in Ids)
            if (!AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(id)))
                throw new InvalidOperationException("Rebuild visuals before binding: missing " + id);
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            // Preserve a recoverable copy of any pre-existing unsaved edits.
            if (scene.isDirty)
            {
                Directory.CreateDirectory("Library/AbilityPreviewBackups");
                EditorSceneManager.SaveScene(scene, "Library/AbilityPreviewBackups/StartScreen_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
            }
            var inventory = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WeaponPowerInventoryController>(true)).Single();
            var serialized = new SerializedObject(inventory);
            var preview = serialized.FindProperty("previewPlayer").objectReferenceValue as WeaponPowerPreviewPlayer;
            if (!preview) throw new InvalidOperationException("The existing shared Ability preview reference is missing.");
            // Only replace each existing entry's clip: preserve ordering, IDs,
            // powerSlots, card Image components, hierarchy and equip references.
            var entries = serialized.FindProperty("previewAnimations");
            var seen = new System.Collections.Generic.HashSet<WeaponPowerId>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = (WeaponPowerId)entry.FindPropertyRelative("powerId").intValue;
                if (!Ids.Contains(id) || !seen.Add(id))
                    throw new InvalidOperationException("Unexpected/duplicate authored Ability preview mapping: " + id);
            }
            if (seen.Count != Ids.Length) throw new InvalidOperationException("An authored Ability preview mapping is missing.");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = (WeaponPowerId)entry.FindPropertyRelative("powerId").intValue;
                entry.FindPropertyRelative("animationClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(id));
            }
            serialized.ApplyModifiedProperties();
            var player = new SerializedObject(preview);
            var controller = ConfigureController();
            player.FindProperty("turntableController").objectReferenceValue = controller;
            player.FindProperty("turntableTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(WeaponPowerId.None));
            player.FindProperty("turntableStateName").stringValue = PreviewState;
            player.ApplyModifiedProperties();
            // Sword / Rollerblade controllers retain their category overrides.
            var animator = preview.GetComponent<Animator>();
            Undo.RecordObject(animator, "Bind default Ability playback"); animator.runtimeAnimatorController = controller;
            var previewImage = preview.GetComponent<Image>();
            Undo.RecordObject(previewImage, "Bind default Ability sprite");
            previewImage.sprite = AssetDatabase.LoadAllAssetsAtPath(SheetPath(WeaponPowerId.None)).OfType<Sprite>().OrderBy(s => s.name).First();
            RecordVisual(previewImage); RecordVisual(animator);
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }

    static void RecordVisual(Object value)
    {
        EditorUtility.SetDirty(value);
        if (PrefabUtility.GetCorrespondingObjectFromSource(value))
            PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
