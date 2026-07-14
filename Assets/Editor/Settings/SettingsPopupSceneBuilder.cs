#if UNITY_EDITOR
using System;
using System.Linq;
using MoreMountains.InfiniteRunnerEngine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Idempotent editor-only builder for the scene-authored Settings popup.
/// </summary>
public static class SettingsPopupSceneBuilder
{
    private const string LayerLabs = "Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/";

    private static readonly Color White = Color.white;
    private static readonly Color Muted = new Color(0.70f, 0.82f, 0.94f, 1f);
    private static readonly Color Gold = new Color(1f, 0.69f, 0.22f, 1f);
    private static readonly Color PanelTint = new Color(0.48f, 0.69f, 0.92f, 0.82f);

    private static TMP_Text textTemplate;
    private static Sprite panelSprite;
    private static Sprite rowSprite;
    private static Sprite blueButtonSprite;
    private static Sprite greenButtonSprite;
    private static Sprite grayButtonSprite;
    private static Sprite switchOnSprite;
    private static Sprite switchOffSprite;
    private static Sprite switchKnobSprite;
    private static Sprite backSprite;
    private static Sprite chevronSprite;
    private static Sprite checkSprite;
    private static Sprite playSprite;
    private static Sprite cameraSprite;
    private static Sprite musicNoteSprite;

    [MenuItem("Tools/Skate Runner/Rebuild Settings Popup")]
    public static void Rebuild()
    {
        SettingsPopup popup = Resources.FindObjectsOfTypeAll<SettingsPopup>()
            .FirstOrDefault(candidate => candidate.gameObject.scene.IsValid()
                && GetPath(candidate.transform) == "StartScreenCanvas/Background/Popup");

        if (popup == null)
        {
            throw new InvalidOperationException("The live Settings Popup was not found at StartScreenCanvas/Background/Popup.");
        }

        Transform root = popup.transform;
        textTemplate = root.Find("Text_Title")?.GetComponent<TMP_Text>();
        if (textTemplate == null)
        {
            throw new InvalidOperationException("The existing Text_Title TMP style is missing.");
        }

        LoadLayerLabsAssets();
        RemoveObsoleteContent(root);

        SettingsPopupController existingController = root.GetComponent<SettingsPopupController>();
        if (existingController != null)
        {
            UnityEngine.Object.DestroyImmediate(existingController);
        }

        SettingsPopupController controller = root.gameObject.AddComponent<SettingsPopupController>();

        GameObject mainPage = CreateRect("MainSettingsPage", root, Vector2.zero, Vector2.one, new Vector2(40f, 24f), new Vector2(-40f, -104f));
        GameObject privacyPage = CreateRect("PrivacyLegalPage", root, Vector2.zero, Vector2.one, new Vector2(40f, 24f), new Vector2(-40f, -104f));
        GameObject languagePage = CreateRect("LanguagePage", root, Vector2.zero, Vector2.one, new Vector2(40f, 24f), new Vector2(-40f, -104f));

        BuildMainPage(mainPage.transform, out MainPageReferences mainRefs);
        BuildPrivacyPage(privacyPage.transform, out PrivacyPageReferences privacyRefs);
        BuildLanguagePage(languagePage.transform, out LanguagePageReferences languageRefs);

        mainPage.SetActive(true);
        privacyPage.SetActive(false);
        languagePage.SetActive(false);

        TMP_Text title = root.Find("Text_Title")?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.text = "SETTINGS";
            title.transform.SetAsLastSibling();
        }

        Transform closeButton = root.Find("ButtonScaler");
        if (closeButton != null)
        {
            closeButton.SetAsLastSibling();
        }

        SoundManager sceneSoundManager = Resources.FindObjectsOfTypeAll<SoundManager>()
            .FirstOrDefault(candidate => candidate.gameObject.scene.IsValid());

        WireController(controller, mainPage, privacyPage, languagePage, sceneSoundManager, mainRefs, privacyRefs, languageRefs);

        SerializedObject popupObject = new SerializedObject(popup);
        SerializedProperty settingsControllerProperty = popupObject.FindProperty("settingsController");
        if (settingsControllerProperty != null)
        {
            settingsControllerProperty.objectReferenceValue = controller;
            popupObject.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(popup);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
        Debug.Log("[Settings] Rebuilt and saved the LayerLabs Settings popup.");
    }

    private static void LoadLayerLabsAssets()
    {
        panelSprite = LoadSprite("Frame/PanelFrame02_Round_Single_Navy.png");
        rowSprite = LoadSprite("Button/Button01_225_BlueGray.png");
        blueButtonSprite = LoadSprite("Button/Button01_145_Blue.Png");
        greenButtonSprite = LoadSprite("Button/Button01_145_Green.Png");
        grayButtonSprite = LoadSprite("Button/Button01_145_Gray.Png");
        switchOnSprite = LoadSprite("UI_Etc/Switch_Bg_On.png");
        switchOffSprite = LoadSprite("UI_Etc/Switch_Bg_Off.png");
        switchKnobSprite = LoadSprite("UI_Etc/Switch_Handle_White.png");
        backSprite = LoadSprite("IconMisc/Icon_PictoIcon_Back.png");
        chevronSprite = backSprite;
        checkSprite = LoadSprite("Icon_PictoIcons/128/PictoIcon_Check.Png");
        playSprite = LoadSprite("IconMisc/Icon_PictoIcon_Play.png");
        cameraSprite = LoadSprite("Icon_PictoIcons/128/Pictoicon_Camera.Png");
        musicNoteSprite = LoadSprite("Icon_PictoIcons/128/Pictoicon_Music.Png");

        Sprite[] required =
        {
            panelSprite, rowSprite, blueButtonSprite, greenButtonSprite, grayButtonSprite,
            switchOnSprite, switchOffSprite, switchKnobSprite, backSprite, checkSprite,
            playSprite, cameraSprite, musicNoteSprite
        };

        if (required.Any(sprite => sprite == null))
        {
            throw new InvalidOperationException("One or more required LayerLabs sprites could not be loaded.");
        }
    }

    private static Sprite LoadSprite(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(LayerLabs + relativePath);
    }

    private static void RemoveObsoleteContent(Transform root)
    {
        string[] names =
        {
            "AudioSubtitle", "TitleLine", "ButtonSave",
            "MainSettingsPage", "PrivacyLegalPage", "LanguagePage"
        };

        foreach (string name in names)
        {
            Transform child = root.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void BuildMainPage(Transform page, out MainPageReferences references)
    {
        references = new MainPageReferences();
        CreateScrollView(page, "ScrollView", 1024f, out ScrollRect scrollRect, out RectTransform content);
        references.scrollRect = scrollRect;

        Transform audio = CreateSection(content, "Section_Audio", -8f, 310f);
        CreateText(audio, "Text_AudioHeader", "AUDIO", 34f, Gold, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -60f), new Vector2(-28f, -15f));
        CreateToggleRow(audio, "Row_Music", "Music", -70f,
            out references.musicToggle, out references.musicTrack, out references.musicKnob);
        CreateToggleRow(audio, "Row_SoundEffects", "Sound Effects", -150f,
            out references.sfxToggle, out references.sfxTrack, out references.sfxKnob);
        CreateToggleRow(audio, "Row_Vibration", "Vibration", -230f,
            out references.vibrationToggle, out references.vibrationTrack, out references.vibrationKnob);

        Transform display = CreateSection(content, "Section_Display", -332f, 178f);
        CreateText(display, "Text_DisplayHeader", "DISPLAY", 34f, Gold, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -60f), new Vector2(-28f, -15f));
        GameObject qualityRow = CreateTopStretch("Row_GraphicsQuality", display, -68f, 86f, 18f);
        AddImage(qualityRow, rowSprite, White);
        CreateText(qualityRow.transform, "Text_Label", "Graphics Quality", 31f, White, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.45f, 1f), new Vector2(26f, 5f), new Vector2(-6f, -5f));
        GameObject qualitySelector = CreateRect("QualitySelector", qualityRow.transform, new Vector2(0.46f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);
        CreateQualityButton(qualitySelector.transform, "Button_Auto", "AUTO", 0f, out references.autoButton, out references.autoVisual);
        CreateQualityButton(qualitySelector.transform, "Button_Low", "LOW", 1f / 3f, out references.lowButton, out references.lowVisual);
        CreateQualityButton(qualitySelector.transform, "Button_High", "HIGH", 2f / 3f, out references.highButton, out references.highVisual);

        Transform general = CreateSection(content, "Section_General", -524f, 220f);
        CreateText(general, "Text_GeneralHeader", "GENERAL", 34f, Gold, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -60f), new Vector2(-28f, -15f));
        references.languageButton = CreateNavigationRow(general, "Button_Language", "Language", "English", -70f, out references.currentLanguageLabel);
        references.privacyButton = CreateNavigationRow(general, "Button_PrivacyLegal", "PRIVACY & LEGAL", string.Empty, -145f, out _);

        Transform footer = CreateSection(content, "Footer", -758f, 250f);
        CreateText(footer, "Text_FollowUs", "FOLLOW US", 31f, Gold, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -58f), new Vector2(-25f, -16f));
        GameObject socialButtons = CreateRect("SocialButtons", footer, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -164f), new Vector2(190f, -68f));
        references.youtubeButton = CreateSocialButton(socialButtons.transform, "Button_YouTube", playSprite, -128f);
        references.instagramButton = CreateSocialButton(socialButtons.transform, "Button_Instagram", cameraSprite, 0f);
        references.tiktokButton = CreateSocialButton(socialButtons.transform, "Button_TikTok", musicNoteSprite, 128f);
        references.versionText = CreateText(footer, "Text_Version", "Version", 24f, Muted, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 18f), new Vector2(-20f, 62f));

        scrollRect.verticalNormalizedPosition = 1f;
    }

    private static void BuildPrivacyPage(Transform page, out PrivacyPageReferences references)
    {
        references = new PrivacyPageReferences();
        references.backButton = CreateBackButton(page, "Button_Back");
        CreateText(page, "Text_PageTitle", "PRIVACY & LEGAL", 38f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.2f, 1f), new Vector2(0.8f, 1f), new Vector2(0f, -72f), new Vector2(0f, -10f));

        GameObject scrollViewObject = CreateRect("ScrollView", page, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, -84f));
        ScrollRect scrollRect = scrollViewObject.AddComponent<ScrollRect>();
        references.scrollRect = scrollRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 42f;

        GameObject viewport = CreateRect("Viewport", scrollViewObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.AddComponent<RectMask2D>();
        GameObject contentObject = CreateRect("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(-42f, 610f);
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content;
        scrollRect.verticalNormalizedPosition = 1f;

        references.privacyPolicyButton = CreateLegalButton(content, "Button_PrivacyPolicy", "PRIVACY POLICY", -8f);
        references.termsButton = CreateLegalButton(content, "Button_TermsOfUse", "TERMS OF USE", -96f);
        references.eulaButton = CreateLegalButton(content, "Button_Eula", "END USER LICENCE AGREEMENT", -184f);
        references.dataButton = CreateLegalButton(content, "Button_DataDeletion", "DATA & DELETION REQUEST", -272f);
        references.restoreButton = CreateLegalButton(content, "Button_RestorePurchases", "RESTORE PURCHASES", -360f);
        references.supportButton = CreateLegalButton(content, "Button_Support", "SUPPORT", -448f);
        references.statusText = CreateText(content, "Text_Status", string.Empty, 24f, Muted, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -594f), new Vector2(-18f, -536f));
    }

    private static void BuildLanguagePage(Transform page, out LanguagePageReferences references)
    {
        references = new LanguagePageReferences();
        references.backButton = CreateBackButton(page, "Button_Back");
        CreateText(page, "Text_PageTitle", "LANGUAGE", 38f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.2f, 1f), new Vector2(0.8f, 1f), new Vector2(0f, -72f), new Vector2(0f, -10f));

        GameObject languagePanel = CreateRect("LanguagePanel", page, new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.78f), Vector2.zero, Vector2.zero);
        AddImage(languagePanel, panelSprite, PanelTint);
        references.englishButton = CreateButton("Button_English", languagePanel.transform, rowSprite,
            new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
        CreateText(references.englishButton.transform, "Text_Language", "ENGLISH", 34f, White, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.8f, 1f), new Vector2(34f, 0f), new Vector2(-10f, 0f));
        references.englishSelected = CreateRect("Icon_Selected", references.englishButton.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-92f, -32f), new Vector2(-28f, 32f));
        Image selectedImage = AddImage(references.englishSelected, checkSprite, White);
        selectedImage.preserveAspect = true;
        selectedImage.raycastTarget = false;

        CreateText(languagePanel.transform, "Text_MoreLanguagesComingLater", "More languages coming later", 26f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.40f), Vector2.zero, Vector2.zero);
    }

    private static void WireController(
        SettingsPopupController controller,
        GameObject mainPage,
        GameObject privacyPage,
        GameObject languagePage,
        SoundManager soundManager,
        MainPageReferences main,
        PrivacyPageReferences privacy,
        LanguagePageReferences language)
    {
        SerializedObject serializedController = new SerializedObject(controller);
        SetReference(serializedController, "mainSettingsPage", mainPage);
        SetReference(serializedController, "privacyLegalPage", privacyPage);
        SetReference(serializedController, "languagePage", languagePage);
        SetReference(serializedController, "mainScrollRect", main.scrollRect);
        SetReference(serializedController, "privacyLegalScrollRect", privacy.scrollRect);
        SetReference(serializedController, "soundManager", soundManager);
        SetReference(serializedController, "musicToggle", main.musicToggle);
        SetReference(serializedController, "soundEffectsToggle", main.sfxToggle);
        SetReference(serializedController, "vibrationToggle", main.vibrationToggle);
        SetReference(serializedController, "musicSwitchTrack", main.musicTrack);
        SetReference(serializedController, "soundEffectsSwitchTrack", main.sfxTrack);
        SetReference(serializedController, "vibrationSwitchTrack", main.vibrationTrack);
        SetReference(serializedController, "musicSwitchKnob", main.musicKnob);
        SetReference(serializedController, "soundEffectsSwitchKnob", main.sfxKnob);
        SetReference(serializedController, "vibrationSwitchKnob", main.vibrationKnob);
        SetReference(serializedController, "switchOnSprite", switchOnSprite);
        SetReference(serializedController, "switchOffSprite", switchOffSprite);
        SetReference(serializedController, "autoQualityButton", main.autoButton);
        SetReference(serializedController, "lowQualityButton", main.lowButton);
        SetReference(serializedController, "highQualityButton", main.highButton);
        SetReference(serializedController, "autoQualityVisual", main.autoVisual);
        SetReference(serializedController, "lowQualityVisual", main.lowVisual);
        SetReference(serializedController, "highQualityVisual", main.highVisual);
        SetReference(serializedController, "qualitySelectedSprite", greenButtonSprite);
        SetReference(serializedController, "qualityUnselectedSprite", grayButtonSprite);
        SetReference(serializedController, "languageButton", main.languageButton);
        SetReference(serializedController, "privacyLegalButton", main.privacyButton);
        SetReference(serializedController, "privacyBackButton", privacy.backButton);
        SetReference(serializedController, "languageBackButton", language.backButton);
        SetReference(serializedController, "englishButton", language.englishButton);
        SetReference(serializedController, "currentLanguageLabel", main.currentLanguageLabel);
        SetReference(serializedController, "englishSelectedVisual", language.englishSelected);
        SetReference(serializedController, "privacyPolicyButton", privacy.privacyPolicyButton);
        SetReference(serializedController, "termsOfUseButton", privacy.termsButton);
        SetReference(serializedController, "eulaButton", privacy.eulaButton);
        SetReference(serializedController, "dataDeletionButton", privacy.dataButton);
        SetReference(serializedController, "restorePurchasesButton", privacy.restoreButton);
        SetReference(serializedController, "supportButton", privacy.supportButton);
        SetReference(serializedController, "legalStatusText", privacy.statusText);
        SetReference(serializedController, "youtubeButton", main.youtubeButton);
        SetReference(serializedController, "instagramButton", main.instagramButton);
        SetReference(serializedController, "tiktokButton", main.tiktokButton);
        SetReference(serializedController, "versionText", main.versionText);
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform CreateSection(RectTransform content, string name, float y, float height)
    {
        GameObject section = CreateTopStretch(name, content, y, height, 15f);
        AddImage(section, panelSprite, PanelTint);
        return section.transform;
    }

    private static void CreateToggleRow(
        Transform parent,
        string name,
        string label,
        float y,
        out Toggle toggle,
        out Image track,
        out RectTransform knob)
    {
        GameObject row = CreateTopStretch(name, parent, y, 70f, 18f);
        AddImage(row, rowSprite, White);
        CreateText(row.transform, "Text_Label", label, 31f, White, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.74f, 1f), new Vector2(26f, 3f), new Vector2(-8f, -3f));

        GameObject toggleObject = CreateRect("Toggle_" + name.Substring(4), row.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-152f, -32f), new Vector2(-32f, 32f));
        track = AddImage(toggleObject, switchOnSprite, White);
        toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.graphic = null;
        toggle.isOn = true;
        toggle.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = toggle.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.96f, 1f);
        toggle.colors = colors;

        GameObject knobObject = CreateRect("Knob", toggleObject.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-1f, -27f), new Vector2(53f, 27f));
        Image knobImage = AddImage(knobObject, switchKnobSprite, White);
        knobImage.preserveAspect = true;
        knobImage.raycastTarget = false;
        knob = knobObject.GetComponent<RectTransform>();
        knob.anchoredPosition = new Vector2(25f, 0f);
    }

    private static void CreateQualityButton(Transform parent, string name, string label, float anchorX, out Button button, out Image visual)
    {
        GameObject buttonObject = CreateRect(name, parent,
            new Vector2(anchorX, 0f), new Vector2(anchorX + 1f / 3f, 1f),
            new Vector2(4f, 0f), new Vector2(-4f, 0f));
        visual = AddImage(buttonObject, grayButtonSprite, White);
        button = buttonObject.AddComponent<Button>();
        button.targetGraphic = visual;
        ConfigureButton(button);
        CreateText(buttonObject.transform, "Text", label, 25f, White, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, -2f));
    }

    private static Button CreateNavigationRow(Transform parent, string name, string label, string value, float y, out TMP_Text valueText)
    {
        GameObject row = CreateTopStretch(name, parent, y, 66f, 18f);
        Image rowImage = AddImage(row, rowSprite, White);
        Button button = row.AddComponent<Button>();
        button.targetGraphic = rowImage;
        ConfigureButton(button);

        CreateText(row.transform, "Text_Label", label, 30f, White, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(26f, 2f), new Vector2(-8f, -2f));
        valueText = CreateText(row.transform, "Text_CurrentLanguage", value, 27f, Muted, TextAlignmentOptions.MidlineRight,
            new Vector2(0.62f, 0f), new Vector2(0.91f, 1f), new Vector2(0f, 2f), new Vector2(-10f, -2f));
        GameObject icon = CreateRect("Icon_Chevron", row.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, -23f), new Vector2(-24f, 23f));
        Image iconImage = AddImage(icon, chevronSprite, White);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        icon.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
        return button;
    }

    private static Button CreateSocialButton(Transform parent, string name, Sprite iconSprite, float x)
    {
        Button button = CreateButton(name, parent, blueButtonSprite,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x - 45f, -45f), new Vector2(x + 45f, 45f));
        GameObject icon = CreateRect("Icon", button.transform, Vector2.zero, Vector2.one, new Vector2(22f, 19f), new Vector2(-22f, -19f));
        Image iconImage = AddImage(icon, iconSprite, White);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        return button;
    }

    private static Button CreateBackButton(Transform parent, string name)
    {
        Button button = CreateButton(name, parent, blueButtonSprite,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -72f), new Vector2(150f, -10f));
        GameObject icon = CreateRect("Icon_Back", button.transform, new Vector2(0f, 0f), new Vector2(0.42f, 1f), new Vector2(16f, 12f), new Vector2(-2f, -12f));
        Image iconImage = AddImage(icon, backSprite, White);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        CreateText(button.transform, "Text", "BACK", 24f, White, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.36f, 0f), new Vector2(1f, 1f), new Vector2(2f, 1f), new Vector2(-9f, -1f));
        return button;
    }

    private static Button CreateLegalButton(Transform parent, string name, string label, float y)
    {
        GameObject buttonObject = CreateTopStretch(name, parent, y, 76f, 75f);
        Image image = AddImage(buttonObject, blueButtonSprite, White);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ConfigureButton(button);
        CreateText(buttonObject.transform, "Text", label, 27f, White, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(18f, 4f), new Vector2(-18f, -4f));
        return button;
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject buttonObject = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        Image image = AddImage(buttonObject, sprite, White);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ConfigureButton(button);
        return button;
    }

    private static void ConfigureButton(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.93f);
        colors.pressedColor = new Color(0.78f, 0.84f, 0.92f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.6f, 0.68f, 0.78f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void CreateScrollView(Transform parent, string name, float contentHeight, out ScrollRect scrollRect, out RectTransform content)
    {
        GameObject scrollView = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.14f;
        scrollRect.scrollSensitivity = 42f;

        GameObject viewport = CreateRect("Viewport", scrollView.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-18f, 0f));
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject contentObject = CreateRect("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        content = contentObject.GetComponent<RectTransform>();
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, contentHeight);
        scrollRect.content = content;

        GameObject scrollbarObject = CreateRect("Scrollbar_Vertical", scrollView.transform,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-15f, 18f), new Vector2(0f, -18f));
        Image scrollbarBackground = AddImage(scrollbarObject, switchOffSprite, new Color(1f, 1f, 1f, 0.72f));
        GameObject slidingArea = CreateRect("Sliding Area", scrollbarObject.transform, Vector2.zero, Vector2.one, new Vector2(2f, 6f), new Vector2(-2f, -6f));
        GameObject handleObject = CreateRect("Handle", slidingArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image handleImage = AddImage(handleObject, switchOnSprite, White);
        Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleObject.GetComponent<RectTransform>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        scrollbarBackground.raycastTarget = true;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private static GameObject CreateTopStretch(string name, Transform parent, float y, float height, float horizontalMargin)
    {
        GameObject gameObject = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-horizontalMargin * 2f, height);
        return gameObject;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        return gameObject;
    }

    private static Image AddImage(GameObject gameObject, Sprite sprite, Color color)
    {
        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = true;
        return image;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject gameObject = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = textTemplate.font;
        text.fontSharedMaterial = textTemplate.fontSharedMaterial;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.text = value;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Serialized field '{propertyName}' was not found on {serializedObject.targetObject.name}.");
        }

        property.objectReferenceValue = value;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private sealed class MainPageReferences
    {
        public ScrollRect scrollRect;
        public Toggle musicToggle;
        public Toggle sfxToggle;
        public Toggle vibrationToggle;
        public Image musicTrack;
        public Image sfxTrack;
        public Image vibrationTrack;
        public RectTransform musicKnob;
        public RectTransform sfxKnob;
        public RectTransform vibrationKnob;
        public Button autoButton;
        public Button lowButton;
        public Button highButton;
        public Image autoVisual;
        public Image lowVisual;
        public Image highVisual;
        public Button languageButton;
        public Button privacyButton;
        public TMP_Text currentLanguageLabel;
        public Button youtubeButton;
        public Button instagramButton;
        public Button tiktokButton;
        public TMP_Text versionText;
    }

    private sealed class PrivacyPageReferences
    {
        public ScrollRect scrollRect;
        public Button backButton;
        public Button privacyPolicyButton;
        public Button termsButton;
        public Button eulaButton;
        public Button dataButton;
        public Button restoreButton;
        public Button supportButton;
        public TMP_Text statusText;
    }

    private sealed class LanguagePageReferences
    {
        public Button backButton;
        public Button englishButton;
        public GameObject englishSelected;
    }
}
#endif
