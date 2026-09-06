using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class Phase2SpeedlinesUIBuilder
{
    private const string ScenePath = "Assets/Scenes/SkateRunner.unity";
    private const string SourceClipPath = "Assets/Art/Speedlines/speedlines.anim";
    private const string UiClipPath = "Assets/Art/Speedlines/speedlines_ui.anim";
    private const string UiControllerPath = "Assets/Art/Speedlines/SpeedlinesUI.controller";
    private const string SubjectLayerName = "Phase2SubjectVisuals";

    [MenuItem("Tools/Skate Runner/Phase 2/Build Fullscreen Speedlines UI")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException("Open Assets/Scenes/SkateRunner.unity before building Phase 2 speedlines.");

        int subjectLayer = LayerMask.NameToLayer(SubjectLayerName);
        if (subjectLayer < 0)
            throw new InvalidOperationException("The " + SubjectLayerName + " layer is missing from TagManager.");

        AnimationClip uiClip = BuildUiClip();
        AnimatorController uiController = BuildController(uiClip);
        Phase2SpeedlinesController controller = UnityEngine.Object.FindFirstObjectByType<Phase2SpeedlinesController>(FindObjectsInactive.Include);
        if (controller == null)
            throw new InvalidOperationException("Phase2SpeedlinesController is missing from the gameplay scene.");

        GameObject host = controller.gameObject;
        host.name = "Phase2SpeedlineCanvas";
        Canvas canvas = GetOrAdd<Canvas>(host);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(host);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAdd<GraphicRaycaster>(host).enabled = false;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            throw new InvalidOperationException("The gameplay Main Camera is missing.");
        canvas.worldCamera = mainCamera;
        canvas.planeDistance = Mathf.Max(mainCamera.nearClipPlane + 0.1f, 0.5f);

        RectTransform viewport = EnsureRect(host.transform, "SpeedlineViewport");
        Stretch(viewport);
        RectMask2D viewportMask = GetOrAdd<RectMask2D>(viewport.gameObject);
        viewportMask.padding = Vector4.zero;
        RectTransform rotator = EnsureRect(viewport, "SpeedlineRotator");
        rotator.anchorMin = rotator.anchorMax = rotator.pivot = new Vector2(0.5f, 0.5f);
        RectTransform imageRect = EnsureRect(rotator, "SpeedlineImage");
        Stretch(imageRect);
        Image image = GetOrAdd<Image>(imageRect.gameObject);
        image.raycastTarget = false;
        image.preserveAspect = false;
        var frames = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Speedlines/frame_001.png").OfType<Sprite>().ToArray();
        if (frames.Length > 0) image.sprite = frames[0];
        Animator animator = GetOrAdd<Animator>(imageRect.gameObject);
        animator.runtimeAnimatorController = uiController;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        Transform old = host.transform.Find("Phase2SpeedlinesBackground");
        if (old != null)
        {
            old.name = "Phase2SpeedlinesBackground_RETIRED";
            old.gameObject.SetActive(false);
        }

        GameObject cameraObject = GameObject.Find("Phase2SubjectOverlayCamera");
        if (cameraObject == null)
            cameraObject = new GameObject("Phase2SubjectOverlayCamera");
        Camera subjectCamera = GetOrAdd<Camera>(cameraObject);
        subjectCamera.enabled = false;
        subjectCamera.clearFlags = CameraClearFlags.Depth;
        subjectCamera.cullingMask = 1 << subjectLayer;
        subjectCamera.depth = 0f;
        subjectCamera.allowHDR = mainCamera.allowHDR;
        subjectCamera.allowMSAA = mainCamera.allowMSAA;
        UniversalAdditionalCameraData subjectData = subjectCamera.GetUniversalAdditionalCameraData();
        subjectData.renderType = CameraRenderType.Overlay;
        Phase2SubjectCameraSync sync = GetOrAdd<Phase2SubjectCameraSync>(cameraObject);
        sync.Configure(mainCamera, subjectCamera);
        UniversalAdditionalCameraData mainData = mainCamera.GetUniversalAdditionalCameraData();
        if (!mainData.cameraStack.Contains(subjectCamera))
            mainData.cameraStack.Add(subjectCamera);
        mainCamera.cullingMask &= ~(1 << subjectLayer);

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "speedlinesRoot", viewport.gameObject);
        SetObject(serialized, "speedlinesAnimator", animator);
        SetString(serialized, "speedlinesStateName", "speedlines_ui");
        SetObject(serialized, "speedlineViewport", viewport);
        SetObject(serialized, "speedlineRotator", rotator);
        SetObject(serialized, "subjectOverlayCamera", subjectCamera);
        SetInt(serialized, "subjectVisualLayer", subjectLayer);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        viewport.gameObject.SetActive(false);
        EditorUtility.SetDirty(host);
        EditorUtility.SetDirty(mainCamera);
        EditorUtility.SetDirty(subjectCamera);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Phase 2 fullscreen UI speedlines built. The retired world-space SpriteRenderer remains inactive for reference.");
    }

    private static AnimationClip BuildUiClip()
    {
        AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceClipPath);
        if (source == null) throw new InvalidOperationException("Missing source clip: " + SourceClipPath);
        var sourceBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
        if (sourceBindings.Length == 0) throw new InvalidOperationException("The source speedline clip has no sprite curve.");
        var keys = AnimationUtility.GetObjectReferenceCurve(source, sourceBindings[0]);
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UiClipPath);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, UiClipPath); }
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        clip.ClearCurves();
        clip.frameRate = source.frameRate;
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite"), keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController BuildController(AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(UiControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(UiControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState state = machine.states.Select(x => x.state).FirstOrDefault(x => x.name == "speedlines_ui");
        if (state == null) state = machine.AddState("speedlines_ui");
        state.motion = clip;
        machine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing as RectTransform;
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T value = go.GetComponent<T>();
        return value != null ? value : go.AddComponent<T>();
    }

    private static void SetObject(SerializedObject so, string name, UnityEngine.Object value) { so.FindProperty(name).objectReferenceValue = value; }
    private static void SetString(SerializedObject so, string name, string value) { so.FindProperty(name).stringValue = value; }
    private static void SetInt(SerializedObject so, string name, int value) { so.FindProperty(name).intValue = value; }
}
