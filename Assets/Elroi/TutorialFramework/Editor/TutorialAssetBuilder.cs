using System;
using Elroi.Tutorials.Demo;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Elroi.Tutorials.Editor
{
    public static class TutorialAssetBuilder
    {
        private const string Root = "Assets/Elroi/TutorialFramework";
        private const string ThemePath = Root + "/Materials/ELROI_MangaTheme.asset";
        private const string PresentationPath = Root + "/Prefabs/ELROI_TutorialCanvas.prefab";
        private const string ManagerPath = Root + "/Prefabs/ELROI_TutorialManager.prefab";
        private const string GesturePath = Root + "/Demo/Generated/ELROI_DemoGesture.asset";
        private const string DemoScenePath = Root + "/Demo/ELROI_Tutorial_Demo.unity";

        [MenuItem("ELROI/Tutorial Framework/Build Original Assets and Demo")]
        public static void BuildAll()
        {
            EnsureFolders();
            TutorialTheme theme = CreateOrUpdateTheme();
            TutorialGestureAnimation gesture = CreateOrUpdateGestureAnimation();
            TutorialPresentation presentation = CreatePresentationPrefab();
            CreateManagerPrefab(presentation, theme);
            CreateDemoScene(presentation, theme, gesture);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ELROI Tutorials] Built original framework assets and standalone demo at {DemoScenePath}.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ELROI");
            EnsureFolder("Assets/Elroi", "TutorialFramework");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Demo");
            EnsureFolder(Root + "/Demo", "Generated");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static TutorialTheme CreateOrUpdateTheme()
        {
            TutorialTheme theme = AssetDatabase.LoadAssetAtPath<TutorialTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<TutorialTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static TutorialGestureAnimation CreateOrUpdateGestureAnimation()
        {
            TutorialGestureAnimation animation = AssetDatabase.LoadAssetAtPath<TutorialGestureAnimation>(GesturePath);
            if (animation == null)
            {
                animation = ScriptableObject.CreateInstance<TutorialGestureAnimation>();
                AssetDatabase.CreateAsset(animation, GesturePath);
            }

            const string texturePath = Root + "/Demo/Generated/ELROI_DemoGestureFrames.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(128, 64, TextureFormat.RGBA32, false) { name = "ELROI Demo Gesture Frames", filterMode = FilterMode.Bilinear };
                Color clear = new Color(0f, 0f, 0f, 0f);
                Color ink = new Color(1f, 0.8f, 0.12f, 1f);
                Color outline = Color.black;
                Color[] pixels = new Color[128 * 64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
                for (int frame = 0; frame < 2; frame++)
                {
                    Vector2 center = new Vector2(frame * 64 + 22 + frame * 18, 32);
                    for (int y = 0; y < 64; y++)
                    for (int x = frame * 64; x < frame * 64 + 64; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        if (distance <= 12f) pixels[y * 128 + x] = ink;
                        if (distance > 12f && distance <= 15f) pixels[y * 128 + x] = outline;
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                AssetDatabase.CreateAsset(texture, texturePath);
            }

            Sprite[] sprites = new Sprite[2];
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath);
            for (int i = 0; i < subAssets.Length; i++)
                if (subAssets[i] is Sprite sprite && sprite.name.EndsWith("0")) sprites[0] = sprite;
                else if (subAssets[i] is Sprite second && second.name.EndsWith("1")) sprites[1] = second;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null) continue;
                sprites[i] = Sprite.Create(texture, new Rect(i * 64, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
                sprites[i].name = "ELROI_DemoGesture_" + i;
                AssetDatabase.AddObjectToAsset(sprites[i], texture);
            }
            animation.ConfigureForAuthoring(sprites, 4f, true);
            EditorUtility.SetDirty(animation);
            return animation;
        }

        private static TutorialPresentation CreatePresentationPrefab()
        {
            GameObject root = new GameObject("ELROI_TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TutorialPresentation));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject captureObject = CreateUiObject("Input Capture Layer", root.transform);
            Stretch(captureObject.GetComponent<RectTransform>());
            Image captureImage = captureObject.AddComponent<Image>();
            captureImage.color = new Color(0f, 0f, 0f, 0.001f);
            captureImage.raycastTarget = true;
            TutorialGestureInput input = captureObject.AddComponent<TutorialGestureInput>();

            GameObject spotlightObject = CreateUiObject("Spotlight Overlay", root.transform);
            Stretch(spotlightObject.GetComponent<RectTransform>());
            TutorialSpotlightGraphic spotlight = spotlightObject.AddComponent<TutorialSpotlightGraphic>();

            GameObject bubbleObject = CreateUiObject("Dialogue Bubble", root.transform);
            RectTransform bubble = bubbleObject.GetComponent<RectTransform>();
            bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
            bubble.pivot = new Vector2(0.5f, 0.5f);
            bubble.sizeDelta = new Vector2(520f, 180f);
            Image bubbleImage = bubbleObject.AddComponent<Image>();
            Outline outline = bubbleObject.AddComponent<Outline>();

            GameObject tailObject = CreateUiObject("Dialogue Tail", bubbleObject.transform);
            RectTransform tailRect = tailObject.GetComponent<RectTransform>();
            tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 0.5f);
            tailRect.sizeDelta = new Vector2(34f, 34f);
            TutorialTriangleGraphic tail = tailObject.AddComponent<TutorialTriangleGraphic>();
            tail.raycastTarget = false;

            TextMeshProUGUI intro = CreateText("Introduction Text", bubbleObject.transform, 34f, TextAlignmentOptions.Center);
            Stretch(intro.rectTransform, 26f, 26f, 26f, 26f);
            intro.enableWordWrapping = true;
            intro.raycastTarget = false;

            TextMeshProUGUI instruction = CreateText("Bottom Instruction Text", root.transform, 38f, TextAlignmentOptions.Center);
            RectTransform instructionRect = instruction.rectTransform;
            instructionRect.anchorMin = new Vector2(0.1f, 0f);
            instructionRect.anchorMax = new Vector2(0.9f, 0f);
            instructionRect.pivot = new Vector2(0.5f, 0f);
            instructionRect.anchoredPosition = new Vector2(0f, 52f);
            instructionRect.sizeDelta = new Vector2(0f, 90f);
            instruction.fontStyle = FontStyles.Bold;
            instruction.raycastTarget = false;

            GameObject gestureObject = CreateUiObject("Gesture Animation", root.transform);
            RectTransform gestureRect = gestureObject.GetComponent<RectTransform>();
            gestureRect.anchorMin = gestureRect.anchorMax = new Vector2(0.5f, 0f);
            gestureRect.pivot = new Vector2(0.5f, 0f);
            gestureRect.anchoredPosition = new Vector2(0f, 155f);
            gestureRect.sizeDelta = new Vector2(140f, 140f);
            Image gestureImage = gestureObject.AddComponent<Image>();
            gestureImage.preserveAspect = true;
            gestureImage.raycastTarget = false;

            Button ok = CreateButton("OK Button", root.transform, "OK", new Vector2(0f, 150f), new Vector2(220f, 84f));
            RectTransform okRect = ok.GetComponent<RectTransform>();
            okRect.anchorMin = okRect.anchorMax = new Vector2(0.5f, 0f);
            okRect.pivot = new Vector2(0.5f, 0f);
            TextMeshProUGUI okText = ok.GetComponentInChildren<TextMeshProUGUI>();

            TutorialPresentation view = root.GetComponent<TutorialPresentation>();
            view.ConfigureForAuthoring(canvas, spotlight, bubble, bubbleImage, outline, tail, intro, instruction, gestureImage, ok, okText, input);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PresentationPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<TutorialPresentation>();
        }

        private static void CreateManagerPrefab(TutorialPresentation presentation, TutorialTheme theme)
        {
            GameObject root = new GameObject("ELROI_TutorialManager");
            TutorialManager manager = root.AddComponent<TutorialManager>();
            TutorialVariableStore variables = root.AddComponent<TutorialVariableStore>();
            PlayerPrefsTutorialPersistence persistence = root.AddComponent<PlayerPrefsTutorialPersistence>();
            manager.ConfigureProvidersForAuthoring(presentation, theme, null, null, variables, persistence);
            PrefabUtility.SaveAsPrefabAsset(root, ManagerPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateDemoScene(TutorialPresentation presentation, TutorialTheme theme, TutorialGestureAnimation gesture)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "ELROI_Tutorial_Demo";
            SceneManager.SetActiveScene(scene);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 3f, -12f);
            cameraObject.transform.LookAt(new Vector3(0f, 1f, 0f));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.12f, 1f);

            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Demo Floor";
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(20f, 0.5f, 4f);

            GameObject runner = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            runner.name = "Demo Runner";
            runner.transform.position = new Vector3(-8f, 1f, 0f);
            TutorialTargetMarker runnerMarker = runner.AddComponent<TutorialTargetMarker>();
            runnerMarker.TargetId = "DemoRunner";

            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Distance Target";
            obstacle.transform.position = new Vector3(0f, 1f, 0f);
            obstacle.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            TutorialTargetMarker obstacleMarker = obstacle.AddComponent<TutorialTargetMarker>();
            obstacleMarker.TargetId = "DemoDistanceTarget";

            GameObject visibleTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visibleTarget.name = "Visible Target";
            visibleTarget.transform.position = new Vector3(5f, 1.2f, 0f);
            TutorialTargetMarker visibleMarker = visibleTarget.AddComponent<TutorialTargetMarker>();
            visibleMarker.TargetId = "DemoVisibleTarget";

            GameObject runtimeTarget = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runtimeTarget.name = "Runtime Context Target";
            runtimeTarget.transform.position = new Vector3(-4f, 1f, 1.5f);

            GameObject managerObject = new GameObject("ELROI Tutorial Manager");
            TutorialManager manager = managerObject.AddComponent<TutorialManager>();
            TutorialVariableStore variables = managerObject.AddComponent<TutorialVariableStore>();
            variables.Variables.Add(new TutorialVariableEntry { Id = "Coins", Value = TutorialValue.From(0) });
            variables.RebuildIndex();
            PlayerPrefsTutorialPersistence persistence = managerObject.AddComponent<PlayerPrefsTutorialPersistence>();
            TutorialDemoGameplayAdapter adapter = managerObject.AddComponent<TutorialDemoGameplayAdapter>();
            adapter.ConfigureForAuthoring(runner.transform);
            manager.ConfigureProvidersForAuthoring(presentation, theme, camera, adapter, variables, persistence);

            GameObject demoCanvasObject = new GameObject("Demo Controls Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas demoCanvas = demoCanvasObject.GetComponent<Canvas>();
            demoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            demoCanvas.sortingOrder = 10;
            CanvasScaler demoScaler = demoCanvasObject.GetComponent<CanvasScaler>();
            demoScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            demoScaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            GameObject uiTarget = CreateUiObject("2D Edge Target", demoCanvasObject.transform);
            RectTransform uiTargetRect = uiTarget.GetComponent<RectTransform>();
            uiTargetRect.anchorMin = uiTargetRect.anchorMax = new Vector2(0.94f, 0.86f);
            uiTargetRect.sizeDelta = new Vector2(170f, 90f);
            Image uiTargetImage = uiTarget.AddComponent<Image>();
            uiTargetImage.color = new Color(0.15f, 0.85f, 0.95f, 1f);
            TextMeshProUGUI uiTargetText = CreateText("Label", uiTarget.transform, 26f, TextAlignmentOptions.Center);
            Stretch(uiTargetText.rectTransform);
            uiTargetText.text = "2D TARGET";
            uiTargetText.color = Color.black;

            GameObject controlsObject = new GameObject("Demo Controls", typeof(TutorialDemoControls));
            TutorialDemoControls controls = controlsObject.GetComponent<TutorialDemoControls>();
            controls.ConfigureForAuthoring(manager, runtimeTarget);
            CreateControlButton(demoCanvasObject.transform, "Manual Runtime Target", new Vector2(30f, -40f), controls.PlayManualRuntimeTarget);
            CreateControlButton(demoCanvasObject.transform, "2D UI Spotlight", new Vector2(30f, -130f), controls.PlayTwoDimensionalTutorial);
            CreateControlButton(demoCanvasObject.transform, "Tap", new Vector2(30f, -220f), controls.PlayTap);
            CreateControlButton(demoCanvasObject.transform, "Double Tap", new Vector2(30f, -310f), controls.PlayDoubleTap);
            CreateControlButton(demoCanvasObject.transform, "Swipe Left", new Vector2(30f, -400f), controls.PlaySwipeLeft);
            CreateControlButton(demoCanvasObject.transform, "Swipe Right", new Vector2(30f, -490f), controls.PlaySwipeRight);
            CreateControlButton(demoCanvasObject.transform, "Swipe Up", new Vector2(30f, -580f), controls.PlaySwipeUp);
            CreateControlButton(demoCanvasObject.transform, "Swipe Down", new Vector2(30f, -670f), controls.PlaySwipeDown);

            TutorialDemoWorld world = new GameObject("Demo World Driver", typeof(TutorialDemoWorld)).GetComponent<TutorialDemoWorld>();
            world.ConfigureForAuthoring(manager, variables, runner.transform);

            ConfigureDemoTutorials(manager, runner, obstacle, visibleTarget, uiTarget, gesture);
            EditorSceneManager.SaveScene(scene, DemoScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
        }

        private static void ConfigureDemoTutorials(TutorialManager manager, GameObject runner, GameObject obstacle, GameObject visibleTarget,
            GameObject uiTarget, TutorialGestureAnimation gesture)
        {
            TutorialDefinition timeIntro = MakeTutorial("Time Trigger", runner, TutorialCompletionType.OkButton, TutorialGesture.None, string.Empty,
                "TIME TRIGGER", "The automatic sequence began after an explicit scaled delay.", null);
            TutorialDefinition variableTap = MakeTutorial("Variable Trigger", runner, TutorialCompletionType.Gesture, TutorialGesture.Tap, "Tap",
                "VARIABLE TRIGGER", "Tap once. The demo world raised Coins without polling.", gesture);
            TutorialDefinition eventDouble = MakeTutorial("Event Trigger", obstacle, TutorialCompletionType.Gesture, TutorialGesture.DoubleTap, "DoubleTap",
                "EVENT TRIGGER", "Double tap within 0.3 seconds. Distant taps never count.", gesture);
            TutorialDefinition distanceSwipe = MakeTutorial("Distance Trigger", obstacle, TutorialCompletionType.Gesture, TutorialGesture.SwipeRight, "SwipeRight",
                "DISTANCE + Y RANGE", "Swipe right. The runner met an absolute X and relative Y condition.", gesture);
            TutorialDefinition visibleSwipe = MakeTutorial("Visible Trigger", visibleTarget, TutorialCompletionType.Gesture, TutorialGesture.SwipeDown, "SwipeDown",
                "ACTUAL SCREEN VISIBILITY", "Swipe down. This target remained camera-visible through the configured delay.", gesture);

            TutorialDefinition manual = MakeTutorial("Manual Runtime Target", null, TutorialCompletionType.OkButton, TutorialGesture.None, string.Empty,
                "RUNTIME CONTEXT TARGET", "This target was supplied directly to PlayTutorial(name, GameObject).", null);
            manual.TargetSource = TutorialTargetSource.RuntimeContextTarget;
            TutorialDefinition ui = MakeTutorial("2D UI Spotlight", uiTarget, TutorialCompletionType.OkButton, TutorialGesture.None, string.Empty,
                "2D TARGET", "The dialogue clamps inward even when its RectTransform is near a screen edge.", null);
            ui.TargetType = TutorialTargetType.TwoDimensional;
            ui.SpotlightShape = TutorialSpotlightShape.RoundedRectangle;

            manager.MutableTutorialsForAuthoring.Add(timeIntro);
            manager.MutableTutorialsForAuthoring.Add(variableTap);
            manager.MutableTutorialsForAuthoring.Add(eventDouble);
            manager.MutableTutorialsForAuthoring.Add(distanceSwipe);
            manager.MutableTutorialsForAuthoring.Add(visibleSwipe);
            manager.MutableTutorialsForAuthoring.Add(manual);
            manager.MutableTutorialsForAuthoring.Add(ui);
            manager.MutableTutorialsForAuthoring.Add(MakeTutorial("Gesture - Tap", runner, TutorialCompletionType.Gesture, TutorialGesture.Tap, "Tap", "TAP", "Tap anywhere on the capture layer.", gesture));
            manager.MutableTutorialsForAuthoring.Add(MakeTutorial("Gesture - Double Tap", runner, TutorialCompletionType.Gesture, TutorialGesture.DoubleTap, "DoubleTap", "DOUBLE TAP", "Two taps must arrive within 0.3 seconds.", gesture));
            manager.MutableTutorialsForAuthoring.Add(MakeTutorial("Gesture - Swipe Left", runner, TutorialCompletionType.Gesture, TutorialGesture.SwipeLeft, "SwipeLeft", "SWIPE LEFT", "Swipe left.", gesture));
            manager.MutableTutorialsForAuthoring.Add(MakeTutorial("Gesture - Swipe Right", runner, TutorialCompletionType.Gesture, TutorialGesture.SwipeRight, "SwipeRight", "SWIPE RIGHT", "Swipe right.", gesture));
            manager.MutableTutorialsForAuthoring.Add(MakeTutorial("Gesture - Swipe Up", runner, TutorialCompletionType.Gesture, TutorialGesture.SwipeUp, "SwipeUp", "SWIPE UP", "Swipe up.", gesture));
            manager.MutableTutorialsForAuthoring.Add(MakeTutorial("Gesture - Swipe Down", runner, TutorialCompletionType.Gesture, TutorialGesture.SwipeDown, "SwipeDown", "SWIPE DOWN", "Swipe down.", gesture));

            TutorialSequencerDefinition sequence = new TutorialSequencerDefinition { SequenceName = "Automatic Trigger Showcase", Enabled = true, RunPolicy = TutorialRunPolicy.EveryTime, LockGameplayBetweenTutorials = true };
            sequence.EnsureStableId();
            sequence.StartTrigger.TriggerType = TutorialTriggerType.Immediate;

            TutorialSequenceEntry entry0 = Entry(timeIntro, TutorialTriggerType.Time);
            entry0.Trigger.DelaySeconds = 0.5f;
            entry0.Trigger.TimeOrigin = TutorialTimeOrigin.SequencerStart;
            sequence.Entries.Add(entry0);

            TutorialSequenceEntry entry1 = Entry(variableTap, TutorialTriggerType.VariableCondition);
            entry1.Trigger.VariableCondition.VariableId = "Coins";
            entry1.Trigger.VariableCondition.Comparison = TutorialComparisonOperator.GreaterOrEqual;
            entry1.Trigger.VariableCondition.ExpectedValue = TutorialValue.From(1);
            sequence.Entries.Add(entry1);

            TutorialSequenceEntry entry2 = Entry(eventDouble, TutorialTriggerType.Event);
            entry2.Trigger.EventId = "DemoWorldReachedGate";
            sequence.Entries.Add(entry2);

            TutorialSequenceEntry entry3 = Entry(distanceSwipe, TutorialTriggerType.Distance);
            entry3.Trigger.ReferenceObject = runner;
            entry3.Trigger.TargetObject = obstacle;
            entry3.Trigger.MaximumAbsoluteXDistance = 2.5f;
            entry3.Trigger.UseYRange = true;
            entry3.Trigger.RelativeYMinimum = -1f;
            entry3.Trigger.RelativeYMaximum = 1f;
            sequence.Entries.Add(entry3);

            TutorialSequenceEntry entry4 = Entry(visibleSwipe, TutorialTriggerType.VisibleOnScreen);
            entry4.Trigger.TargetObject = visibleTarget;
            entry4.Trigger.VisibilityDelay = 0.4f;
            sequence.Entries.Add(entry4);
            manager.MutableSequencersForAuthoring.Add(sequence);
        }

        private static TutorialDefinition MakeTutorial(string name, GameObject target, TutorialCompletionType completion, TutorialGesture requiredGesture,
            string actionId, string introduction, string instruction, TutorialGestureAnimation gesture)
        {
            TutorialDefinition tutorial = new TutorialDefinition
            {
                TutorialName = name,
                Enabled = true,
                TargetType = TutorialTargetType.ThreeDimensional,
                TargetSource = TutorialTargetSource.DirectObject,
                SpotlightTarget = target,
                CompletionType = completion,
                RequiredGesture = requiredGesture,
                GameplayActionId = actionId,
                IntroductionText = introduction,
                BottomInstructionText = instruction,
                GestureAnimation = gesture,
                FreezeWorld = true,
                SpotlightShape = TutorialSpotlightShape.RoundedRectangle
            };
            tutorial.EnsureStableId();
            return tutorial;
        }

        private static TutorialSequenceEntry Entry(TutorialDefinition tutorial, TutorialTriggerType type)
        {
            TutorialSequenceEntry entry = new TutorialSequenceEntry { Enabled = true, TutorialStableId = tutorial.StableId };
            entry.Trigger.TriggerType = type;
            return entry;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject value = new GameObject(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, float size, TextAlignmentOptions alignment)
        {
            GameObject value = CreateUiObject(name, parent);
            TextMeshProUGUI text = value.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = true;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject value = CreateUiObject(name, parent);
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = value.AddComponent<Image>();
            image.color = new Color(1f, 0.78f, 0.12f, 1f);
            Button button = value.AddComponent<Button>();
            TextMeshProUGUI text = CreateText("Label", value.transform, 30f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 8f, 8f, 8f, 8f);
            text.text = label;
            text.color = Color.black;
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void CreateControlButton(Transform parent, string label, Vector2 topLeftOffset, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(label + " Button", parent, label, Vector2.zero, new Vector2(320f, 72f));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftOffset;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
