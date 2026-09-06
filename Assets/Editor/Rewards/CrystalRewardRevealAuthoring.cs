#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CrystalRewardRevealAuthoring
{
    const string RootPath = "StartScreenCanvas/Background/CrystalRewardRevealPopup";
    const string CrystalPrefabPath = "Assets/Modern 2D Animated Chests Pack/Chests/Crystal/Chest _ crystal _ 01/Chest _ crystal _ 01 _ Prefab.prefab";
    const string CrystalControllerPath = "Assets/Modern 2D Animated Chests Pack/Chests/Crystal/Animation/AC_Chest_Crystal.controller";
    const string CrystalOpenClipPath = "Assets/Modern 2D Animated Chests Pack/Chests/Crystal/Animation/ANIM_Chest_Crystal_Open.anim";
    const string UiAnimationFolder = "Assets/Animations/UI";
    const string UiOpenClipPath = UiAnimationFolder + "/ANIM_Chest_Crystal_Open_UI.anim";
    const string UiOverrideControllerPath = UiAnimationFolder + "/AC_Chest_Crystal_UI.overrideController";
    const string GlowSourcePath = "StartScreenCanvas/Background/FullscreenPopupRoot/ShopPage/Shop/ScrollRect_Rollerblades/Content/NeonVelocity_Rollerblades/NeonVelocityRollerbladesmage/Fx_Rotate_Light03";
    const string UiRoot = "Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/";
    const string BlurPath = UiRoot + "Demo/Demo_Background/Background_ScreenDimed_Black.png";
    const string ButtonPath = UiRoot + "Components/Button/Button01_175_Orange.png";
    const string GemPath = UiRoot + "Components/UI_Etc/ResourceBar_Icon_Gem_Blue.png";
    const string CashPath = "Assets/Prefabs/UI/Cash 1.png";
    const string AudioPrefabPath = "Assets/Resources/SkateRunnerAudio.prefab";
    const string RevealMusicPath = "Assets/Feel/FeelDemos/Wheel/Sounds/FeelWheelMusic.wav";
    const string CrystalBreakPath = "Assets/Vefects/Stylized AoE URP/Audio/WAV/Sergi/SFX_Vefects_Stylized_AoE_Crystal_Burst_01.wav";
    const string RevealSfxPath = "Assets/ThirdParty/InGame/InfiniteRunnerEngine/ThirdParty/MoreMountains/MMInterface/Common/Sounds/Success3.wav";

    [MenuItem("Tools/Skate Runner/Rewards/Build Crystal Reward Reveal")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Exit Play Mode before authoring the Crystal reward reveal.");
            return;
        }

        GameObject canvasObject = FindSceneObject("StartScreenCanvas");
        GameObject background = FindScenePath("StartScreenCanvas/Background");
        if (!canvasObject || !background)
        {
            Debug.LogError("Open SkateRunnerStartScreen before building the Crystal reward reveal.");
            return;
        }

        RemoveOldRewardObjects(background.transform);
        TMP_FontAsset font = ResolveFont();

        GameObject root = UiObject("CrystalRewardRevealPopup", background.transform);
        Stretch((RectTransform)root.transform);
        root.transform.SetAsLastSibling();
        Image blur = root.AddComponent<Image>();
        blur.sprite = Load<Sprite>(BlurPath);
        blur.type = Image.Type.Sliced;
        blur.color = new Color(0.067f, 0.106f, 0.796f, 1f);
        blur.raycastTarget = true;
        CanvasGroup rootGroup = root.AddComponent<CanvasGroup>();

        RectTransform presentation = RectObject("PresentationRoot", root.transform);
        SetCentered(presentation, new Vector2(900f, 900f), Vector2.zero);

        TMP_Text title = TextObject("Title", presentation, "REWARD UNLOCKED!", font, 58f);
        SetCentered((RectTransform)title.transform, new Vector2(820f, 100f), new Vector2(0f, 390f));

        // Reward is earlier in sibling order than the chest display, so it is
        // literally behind the chest until the reveal callback fades/scales it in.
        RectTransform rewardGroup = RectObject("RewardGroup", presentation);
        SetCentered(rewardGroup, new Vector2(700f, 520f), new Vector2(0f, 25f));
        CanvasGroup rewardGroupCanvas = rewardGroup.gameObject.AddComponent<CanvasGroup>();

        GameObject glowSource = FindScenePath(GlowSourcePath);
        if (!glowSource) throw new MissingReferenceException("The Shop Fx_Rotate_Light03 glow is missing.");
        GameObject glow = Object.Instantiate(glowSource, rewardGroup, false);
        glow.name = "Fx_Rotate_Light03";
        glow.transform.SetAsFirstSibling();
        RectTransform glowRect = glow.transform as RectTransform;
        if (!glowRect) throw new MissingComponentException("Fx_Rotate_Light03 requires its RectTransform.");
        glowRect.anchoredPosition = new Vector2(0f, 35f);
        CanvasGroup glowCanvas = glow.GetComponent<CanvasGroup>();
        if (!glowCanvas) glowCanvas = glow.AddComponent<CanvasGroup>();

        Image primary = ImageObject("PrimaryRewardIcon", rewardGroup, null, Color.white);
        RectTransform primaryRect = (RectTransform)primary.transform;
        SetCentered(primaryRect, new Vector2(240f, 240f), new Vector2(0f, 32f));
        primary.preserveAspect = true;
        UIPulse primaryPulse = AddConfiguredPulse(primary.gameObject);
        WeaponPowerPreviewPlayer primaryAbilityPreview = AddAbilityPreviewPlayer(primary);

        Image secondary = ImageObject("SecondaryRewardIcon", rewardGroup, null, Color.white);
        RectTransform secondaryRect = (RectTransform)secondary.transform;
        SetCentered(secondaryRect, new Vector2(220f, 220f), new Vector2(120f, 32f));
        secondary.preserveAspect = true;
        UIPulse secondaryPulse = AddConfiguredPulse(secondary.gameObject);

        TMP_Text rewardText = TextObject("RewardText", rewardGroup, "+500 CASH", font, 46f);
        SetCentered((RectTransform)rewardText.transform, new Vector2(650f, 135f), new Vector2(0f, -175f));

        RawImage chestDisplay = RawImageObject("CrystalChestDisplay", presentation, null);
        SetCentered((RectTransform)chestDisplay.transform, new Vector2(650f, 650f), new Vector2(0f, 35f));

        Image okImage = ImageObject("Button_OK", presentation, Load<Sprite>(ButtonPath), Color.white);
        SetCentered((RectTransform)okImage.transform, new Vector2(280f, 112f), new Vector2(0f, -385f));
        okImage.type = Image.Type.Sliced;
        okImage.raycastTarget = true;
        Button okButton = okImage.gameObject.AddComponent<Button>();
        okButton.targetGraphic = okImage;
        CanvasGroup okCanvas = okImage.gameObject.AddComponent<CanvasGroup>();
        TMP_Text okLabel = TextObject("Label", okImage.transform, "OK", font, 42f);
        Stretch((RectTransform)okLabel.transform);

        AnimationClip uiOpenClip;
        RuntimeAnimatorController uiChestController = BuildUiAnimationAssets(out uiOpenClip);
        GameObject worldRoot = new GameObject("CrystalRewardRevealWorld");
        worldRoot.transform.position = new Vector3(20000f, 20000f, 0f);

        GameObject cameraObject = new GameObject("CrystalRewardRevealCamera");
        cameraObject.transform.SetParent(worldRoot.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
        Camera chestCamera = cameraObject.AddComponent<Camera>();
        chestCamera.orthographic = true;
        chestCamera.orthographicSize = 5f;
        chestCamera.clearFlags = CameraClearFlags.SolidColor;
        chestCamera.backgroundColor = Color.clear;
        chestCamera.nearClipPlane = 0.1f;
        chestCamera.farClipPlane = 50f;
        chestCamera.allowHDR = false;
        chestCamera.allowMSAA = false;

        GameObject sourcePrefab = Load<GameObject>(CrystalPrefabPath);
        if (!sourcePrefab) throw new MissingReferenceException("The Crystal chest prefab is missing.");
        GameObject chestInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, worldRoot.transform);
        chestInstance.name = "Chest _ crystal _ 01";
        chestInstance.transform.localPosition = Vector3.zero;
        chestInstance.transform.localRotation = Quaternion.identity;

        Animator animator = chestInstance.GetComponentInChildren<Animator>(true);
        if (!animator) throw new MissingComponentException("The Crystal chest prefab no longer contains its authored Animator.");
        animator.runtimeAnimatorController = uiChestController;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        CrystalRewardRevealPopup controller = canvasObject.GetComponent<CrystalRewardRevealPopup>();
        if (!controller) controller = canvasObject.AddComponent<CrystalRewardRevealPopup>();
        SerializedObject serialized = new SerializedObject(controller);
        Set(serialized, "popupRoot", root);
        Set(serialized, "rootCanvasGroup", rootGroup);
        Set(serialized, "presentationRoot", presentation);
        Set(serialized, "titleText", title);
        Set(serialized, "chestWorldRoot", worldRoot);
        Set(serialized, "chestCamera", chestCamera);
        Set(serialized, "chestDisplay", chestDisplay);
        Set(serialized, "chestAnimator", animator);
        Set(serialized, "chestOpenClip", uiOpenClip);
        Set(serialized, "rewardGroup", rewardGroup.gameObject);
        Set(serialized, "rewardCanvasGroup", rewardGroupCanvas);
        Set(serialized, "glowRoot", glow);
        Set(serialized, "glowCanvasGroup", glowCanvas);
        Set(serialized, "glowRect", glowRect);
        Set(serialized, "primaryIcon", primary);
        Set(serialized, "primaryIconRect", primaryRect);
        Set(serialized, "primaryIconPulse", primaryPulse);
        Set(serialized, "primaryAbilityPreview", primaryAbilityPreview);
        Set(serialized, "secondaryIcon", secondary);
        Set(serialized, "secondaryIconRect", secondaryRect);
        Set(serialized, "secondaryIconPulse", secondaryPulse);
        Set(serialized, "rewardText", rewardText);
        Set(serialized, "okButton", okButton);
        Set(serialized, "okButtonCanvasGroup", okCanvas);
        Set(serialized, "cashIcon", Load<Sprite>(CashPath));
        Set(serialized, "gemIcon", Load<Sprite>(GemPath));
        serialized.FindProperty("rewardRevealLeadTime").floatValue = 0.18f;
        serialized.FindProperty("rewardStartScale").floatValue = 0.35f;
        serialized.FindProperty("rewardStagedAlpha").floatValue = 0.35f;
        serialized.FindProperty("currencyIconSize").vector2Value = new Vector2(240f, 240f);
        serialized.FindProperty("shopItemFallbackSize").vector2Value = new Vector2(404.21f, 412.8f);
        serialized.FindProperty("glowIntensity").floatValue = 1f;
        serialized.FindProperty("glowScale").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        rewardGroup.gameObject.SetActive(false);
        okImage.gameObject.SetActive(false);
        root.SetActive(false);
        worldRoot.SetActive(false);
        ConfigureCentralAudio();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        EditorSceneManager.SaveScene(canvasObject.scene);
        Selection.activeGameObject = root;
        Debug.Log("Crystal Reward Reveal authored without a popup frame at " + RootPath + ". The full Crystal chest prefab animation is rendered through its dedicated UI camera.");
    }

    static UIPulse AddConfiguredPulse(GameObject target)
    {
        UIPulse pulse = target.AddComponent<UIPulse>();
        pulse.minScale = 0.95f;
        pulse.maxScale = 1.05f;
        pulse.speed = 2.5f;
        pulse.useUnscaledTime = true;
        pulse.enabled = false;
        return pulse;
    }

    static WeaponPowerPreviewPlayer AddAbilityPreviewPlayer(Image primary)
    {
        WeaponPowerInventoryController inventory = Object.FindFirstObjectByType<WeaponPowerInventoryController>(
            FindObjectsInactive.Include);
        if (!inventory) throw new MissingReferenceException("The Ability Inventory controller is missing.");

        SerializedObject inventorySerialized = new SerializedObject(inventory);
        WeaponPowerPreviewPlayer source = inventorySerialized.FindProperty("previewPlayer").objectReferenceValue
            as WeaponPowerPreviewPlayer;
        if (!source) throw new MissingReferenceException("The existing animated Ability preview player is missing.");

        SerializedObject sourceSerialized = new SerializedObject(source);
        Animator animator = primary.gameObject.AddComponent<Animator>();
        WeaponPowerPreviewPlayer preview = primary.gameObject.AddComponent<WeaponPowerPreviewPlayer>();
        SerializedObject previewSerialized = new SerializedObject(preview);
        Set(previewSerialized, "previewImage", primary);
        Set(previewSerialized, "previewAnimator", animator);
        Set(previewSerialized, "turntableController",
            sourceSerialized.FindProperty("turntableController").objectReferenceValue);
        Set(previewSerialized, "turntableTemplate",
            sourceSerialized.FindProperty("turntableTemplate").objectReferenceValue);
        previewSerialized.FindProperty("turntableStateName").stringValue =
            sourceSerialized.FindProperty("turntableStateName").stringValue;
        previewSerialized.ApplyModifiedPropertiesWithoutUndo();
        return preview;
    }

    static RuntimeAnimatorController BuildUiAnimationAssets(out AnimationClip uiOpenClip)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations")) AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(UiAnimationFolder)) AssetDatabase.CreateFolder("Assets/Animations", "UI");

        AnimationClip source = Load<AnimationClip>(CrystalOpenClipPath);
        if (!source) throw new MissingReferenceException("The authored Crystal chest open clip is missing.");
        uiOpenClip = Load<AnimationClip>(UiOpenClipPath);
        if (!uiOpenClip)
        {
            uiOpenClip = Object.Instantiate(source);
            uiOpenClip.name = "ANIM_Chest_Crystal_Open_UI";
            AssetDatabase.CreateAsset(uiOpenClip, UiOpenClipPath);
        }
        else
        {
            EditorUtility.CopySerialized(source, uiOpenClip);
            uiOpenClip.name = "ANIM_Chest_Crystal_Open_UI";
        }
        // The vendor clip ships with three blank events which only log errors.
        // Every authored transform/activation curve is otherwise preserved exactly.
        AnimationUtility.SetAnimationEvents(uiOpenClip, new AnimationEvent[0]);
        EditorUtility.SetDirty(uiOpenClip);

        RuntimeAnimatorController baseController = Load<RuntimeAnimatorController>(CrystalControllerPath);
        AnimatorOverrideController overrideController = Load<AnimatorOverrideController>(UiOverrideControllerPath);
        if (!overrideController)
        {
            overrideController = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(overrideController, UiOverrideControllerPath);
        }
        else overrideController.runtimeAnimatorController = baseController;
        overrideController["ANIM_Chest_Crystal_Open"] = uiOpenClip;
        EditorUtility.SetDirty(overrideController);
        AssetDatabase.SaveAssets();
        return overrideController;
    }

    static void ConfigureCentralAudio()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(AudioPrefabPath);
        try
        {
            SkateRunnerAudioManager manager = prefabRoot.GetComponent<SkateRunnerAudioManager>();
            if (!manager) throw new MissingComponentException("SkateRunnerAudio.prefab has no SkateRunnerAudioManager.");
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("crystalRewardRevealMusic").objectReferenceValue = Load<AudioClip>(RevealMusicPath);
            serialized.FindProperty("crystalRewardRevealMusicVolume").floatValue = 0.42f;
            SetCueClip(serialized, "crystalChestBreak", Load<AudioClip>(CrystalBreakPath), 0.9f, 1f);
            SetCueClip(serialized, "rewardReveal", Load<AudioClip>(RevealSfxPath), 0.85f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, AudioPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
    }

    static void SetCueClip(SerializedObject serialized, string cueName, AudioClip clip, float volume, float pitch)
    {
        SerializedProperty cue = serialized.FindProperty(cueName);
        cue.FindPropertyRelative("clip").objectReferenceValue = clip;
        cue.FindPropertyRelative("volume").floatValue = volume;
        cue.FindPropertyRelative("pitch").floatValue = pitch;
    }

    static void RemoveOldRewardObjects(Transform background)
    {
        string[] names = { "RewardGrantedPopup", "DailyRewardClaimPopup", "SpinRewardPopup", "CrystalRewardRevealPopup", "CrystalRewardRevealWorld" };
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            Transform candidate = transforms[i];
            if (!candidate || !candidate.gameObject.scene.IsValid()) continue;
            for (int n = 0; n < names.Length; n++)
            {
                if (candidate.name != names[n]) continue;
                Object.DestroyImmediate(candidate.gameObject);
                break;
            }
        }
    }

    static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = Load<TMP_FontAsset>(UiRoot + "Fonts/LilitaOne-Regular Outline_Extended ASCII_54 SDF.asset");
        if (font) return font;
        TMP_Text sample = Object.FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        return sample ? sample.font : null;
    }

    static GameObject UiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform RectObject(string name, Transform parent) => (RectTransform)UiObject(name, parent).transform;

    static Image ImageObject(string name, Transform parent, Sprite sprite, Color color)
    {
        Image image = UiObject(name, parent).AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static RawImage RawImageObject(string name, Transform parent, Texture texture)
    {
        RawImage image = UiObject(name, parent).AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text TextObject(string name, Transform parent, string text, TMP_FontAsset font, float size)
    {
        TextMeshProUGUI label = UiObject(name, parent).AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.fontSizeMin = 18f;
        label.fontSizeMax = size;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static GameObject FindSceneObject(string name)
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < objects.Length; i++) if (objects[i].name == name && objects[i].scene.IsValid()) return objects[i];
        return null;
    }

    static GameObject FindScenePath(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = FindSceneObject(parts[0]);
        Transform current = root ? root.transform : null;
        for (int i = 1; i < parts.Length && current; i++) current = current.Find(parts[i]);
        return current ? current.gameObject : null;
    }

    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    static void Set(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
        property.objectReferenceValue = value;
    }
}
#endif
