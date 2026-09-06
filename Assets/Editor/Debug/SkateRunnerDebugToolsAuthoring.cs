using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SkateRunnerDebugToolsAuthoring
{
    private const string ScenePath = "Assets/Scenes/SkateRunnerStartScreen.unity";
    private static readonly Color Backdrop = new Color(0.015f, 0.02f, 0.035f, 0.9f);
    private static readonly Color Panel = new Color(0.055f, 0.07f, 0.105f, 0.99f);
    private static readonly Color Input = new Color(0.015f, 0.02f, 0.035f, 1f);
    private static readonly Color Accent = new Color(1f, 0.43f, 0.08f, 1f);
    private static readonly Color Danger = new Color(0.75f, 0.12f, 0.12f, 1f);

    [MenuItem("Tools/Skate Runner/Debug/Build Debug Tools UI")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException("Open Assets/Scenes/SkateRunnerStartScreen.unity before building Debug Tools.");

        GameObject canvas = GameObject.Find("StartScreenCanvas");
        Transform home = GameObject.Find("StartScreenCanvas/Background/HomepageRoot")?.transform;
        Transform popupParent = GameObject.Find("StartScreenCanvas/Background")?.transform;
        if (canvas == null || home == null || popupParent == null)
            throw new InvalidOperationException("The Start Screen authored UI hierarchy is incomplete.");

        RemoveExisting(home, "Button_DebugTools");
        RemoveExisting(popupParent, "DebugToolsPopup");
        Transform oldPopupParent = popupParent.Find("FullscreenPopupRoot");
        if (oldPopupParent != null) RemoveExisting(oldPopupParent, "DebugToolsPopup");

        Button openButton = CreateButton(home, "Button_DebugTools", "DEBUG", Accent, 30);
        RectTransform openRect = (RectTransform)openButton.transform;
        openRect.anchorMin = openRect.anchorMax = new Vector2(0f, 1f);
        openRect.pivot = new Vector2(0f, 1f);
        openRect.anchoredPosition = new Vector2(65f, -120f);
        openRect.sizeDelta = new Vector2(170f, 54f);

        RectTransform popup = CreateRect(popupParent, "DebugToolsPopup");
        Stretch(popup);
        Image backdrop = popup.gameObject.AddComponent<Image>();
        backdrop.color = Backdrop;

        RectTransform panel = CreateRect(popup, "Panel");
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(900f, 800f);
        panel.gameObject.AddComponent<Image>().color = Panel;

        TMP_Text title = CreateLabel(panel, "Title", "DEBUG TOOLS", 48, FontStyles.Bold);
        SetRect(title.rectTransform, 0f, 342f, 820f, 64f);
        TMP_Text currencyHeader = CreateLabel(panel, "CurrencyHeader", "CURRENCY", 28, FontStyles.Bold);
        currencyHeader.color = Accent;
        SetRect(currencyHeader.rectTransform, -300f, 262f, 220f, 44f);

        TMP_Text cashLabel = CreateLabel(panel, "CashLabel", "Cash", 28, FontStyles.Normal);
        cashLabel.alignment = TextAlignmentOptions.MidlineLeft;
        SetRect(cashLabel.rectTransform, -340f, 190f, 120f, 54f);
        TMP_InputField cashInput = CreateInput(panel, "CashInput", "0");
        SetRect((RectTransform)cashInput.transform, -80f, 190f, 380f, 58f);
        Button setCash = CreateButton(panel, "Button_SetCash", "SET CASH", Accent, 24);
        SetRect((RectTransform)setCash.transform, 270f, 190f, 220f, 58f);

        TMP_Text gemsLabel = CreateLabel(panel, "GemsLabel", "Gems", 28, FontStyles.Normal);
        gemsLabel.alignment = TextAlignmentOptions.MidlineLeft;
        SetRect(gemsLabel.rectTransform, -340f, 112f, 120f, 54f);
        TMP_InputField gemsInput = CreateInput(panel, "GemsInput", "0");
        SetRect((RectTransform)gemsInput.transform, -80f, 112f, 380f, 58f);
        Button setGems = CreateButton(panel, "Button_SetGems", "SET GEMS", Accent, 24);
        SetRect((RectTransform)setGems.transform, 270f, 112f, 220f, 58f);

        TMP_Text progressionHeader = CreateLabel(panel, "ProgressionHeader", "PROGRESSION", 28, FontStyles.Bold);
        progressionHeader.color = Accent;
        SetRect(progressionHeader.rectTransform, -270f, 30f, 280f, 44f);
        Button resetInventory = CreateButton(panel, "Button_ResetInventory", "RESET INVENTORY", new Color(0.18f, 0.24f, 0.34f, 1f), 26);
        SetRect((RectTransform)resetInventory.transform, 0f, -50f, 620f, 64f);
        Button resetProgress = CreateButton(panel, "Button_ResetGameProgress", "RESET GAME PROGRESS", Danger, 26);
        SetRect((RectTransform)resetProgress.transform, 0f, -132f, 620f, 64f);

        TMP_Text status = CreateLabel(panel, "Status", string.Empty, 23, FontStyles.Normal);
        status.color = new Color(1f, 0.82f, 0.35f, 1f);
        SetRect(status.rectTransform, 0f, -222f, 760f, 56f);
        Button close = CreateButton(panel, "Button_Close", "CLOSE", new Color(0.25f, 0.28f, 0.34f, 1f), 25);
        SetRect((RectTransform)close.transform, 0f, -306f, 300f, 58f);

        RectTransform confirmation = CreateRect(popup, "ConfirmationPanel");
        Stretch(confirmation);
        confirmation.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform confirmationBox = CreateRect(confirmation, "ConfirmationBox");
        confirmationBox.anchorMin = confirmationBox.anchorMax = confirmationBox.pivot = new Vector2(0.5f, 0.5f);
        confirmationBox.sizeDelta = new Vector2(760f, 340f);
        confirmationBox.gameObject.AddComponent<Image>().color = Panel;
        TMP_Text confirmationTitle = CreateLabel(confirmationBox, "Title", "CONFIRM", 36, FontStyles.Bold);
        confirmationTitle.color = new Color(1f, 0.45f, 0.35f, 1f);
        SetRect(confirmationTitle.rectTransform, 0f, 108f, 680f, 58f);
        TMP_Text confirmationMessage = CreateLabel(confirmationBox, "Message", string.Empty, 24, FontStyles.Normal);
        SetRect(confirmationMessage.rectTransform, 0f, 25f, 660f, 90f);
        Button yes = CreateButton(confirmationBox, "Button_Yes", "YES", Danger, 25);
        SetRect((RectTransform)yes.transform, -150f, -105f, 240f, 60f);
        Button no = CreateButton(confirmationBox, "Button_No", "NO", new Color(0.25f, 0.28f, 0.34f, 1f), 25);
        SetRect((RectTransform)no.transform, 150f, -105f, 240f, 60f);

        SetLayerRecursively(openButton.gameObject, home.gameObject.layer);
        SetLayerRecursively(popup.gameObject, popupParent.gameObject.layer);
        popup.SetAsLastSibling();
        popup.gameObject.SetActive(false);
        confirmation.gameObject.SetActive(false);

        SkateRunnerDebugToolsController controller = GetOrAdd<SkateRunnerDebugToolsController>(canvas);
        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "debugButtonRoot", openButton.gameObject);
        SetObject(serialized, "openButton", openButton);
        SetObject(serialized, "popupRoot", popup.gameObject);
        SetObject(serialized, "cashInput", cashInput);
        SetObject(serialized, "gemsInput", gemsInput);
        SetObject(serialized, "setCashButton", setCash);
        SetObject(serialized, "setGemsButton", setGems);
        SetObject(serialized, "resetInventoryButton", resetInventory);
        SetObject(serialized, "resetProgressButton", resetProgress);
        SetObject(serialized, "closeButton", close);
        SetObject(serialized, "statusText", status);
        SetObject(serialized, "confirmationRoot", confirmation.gameObject);
        SetObject(serialized, "confirmationTitle", confirmationTitle);
        SetObject(serialized, "confirmationMessage", confirmationMessage);
        SetObject(serialized, "confirmationYesButton", yes);
        SetObject(serialized, "confirmationNoButton", no);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Development-only Debug Tools UI authored and wired on the Start Screen.");
    }

    private static TMP_InputField CreateInput(Transform parent, string name, string placeholderValue)
    {
        RectTransform root = CreateRect(parent, name);
        Image image = root.gameObject.AddComponent<Image>();
        image.color = Input;
        TMP_InputField field = root.gameObject.AddComponent<TMP_InputField>();
        field.contentType = TMP_InputField.ContentType.DecimalNumber;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.characterLimit = 15;

        RectTransform viewport = CreateRect(root, "Text Area");
        Stretch(viewport);
        viewport.offsetMin = new Vector2(18f, 6f);
        viewport.offsetMax = new Vector2(-18f, -6f);
        viewport.gameObject.AddComponent<RectMask2D>();
        TMP_Text placeholder = CreateLabel(viewport, "Placeholder", placeholderValue, 26, FontStyles.Italic);
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(placeholder.rectTransform);
        TMP_Text text = CreateLabel(viewport, "Text", string.Empty, 28, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(text.rectTransform);
        field.textViewport = viewport;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.targetGraphic = image;
        return field;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, float fontSize)
    {
        RectTransform root = CreateRect(parent, name);
        Image image = root.gameObject.AddComponent<Image>();
        image.color = color;
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        TMP_Text text = CreateLabel(root, "Label", label, fontSize, FontStyles.Bold);
        Stretch(text.rectTransform);
        return button;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string value, float size, FontStyles style)
    {
        RectTransform root = CreateRect(parent, name);
        TextMeshProUGUI text = root.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void RemoveExisting(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException("Missing serialized property: " + propertyName);
        property.objectReferenceValue = value;
    }
}
