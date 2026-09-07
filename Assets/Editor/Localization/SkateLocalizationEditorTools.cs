#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

/// <summary>Explicit, idempotent production localization maintenance tools.</summary>
public static class SkateLocalizationEditorTools
{
    private const string Root = "Assets/Localization";
    private const string SettingsPath = Root + "/Settings/SkateRunnerLocalizationSettings.asset";
    private const string LocaleFolder = Root + "/Locales";
    private const string TableFolder = Root + "/Tables";
    private const string ExportFolder = Root + "/Exports";
    private const string AuditPath = Root + "/ProductionLocalizationAudit.md";
    private const string PseudoCode = "qps-ploc";
    private const string LayerLabsFontFolder = "Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts";

    private static readonly string[] ProductionScenes =
    {
        "Assets/Scenes/ElroiBootSplash.unity",
        "Assets/Scenes/SkateRunnerLoadingScreen.unity",
        "Assets/Scenes/SkateRunnerStartScreen.unity",
        "Assets/Scenes/SkateRunner.unity"
    };

    private static readonly string[] ProductionPrefabs =
    {
        "Assets/Prefabs/Characters/UICamera.prefab"
    };

    [MenuItem("Tools/Skate Runner/Localization/Build or Update Production Localization", priority = 0)]
    public static void BuildOrUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Localization] Exit Play Mode before running the production builder.");
            return;
        }

        EnsureFolders();
        LocalizationSettings settings = EnsureSettingsAndLocales();
        EnsureTables();
        EnsureLilitaLatinExtendedFallbacks();
        ConfigureLanguagePage();
        BindProductionStaticText();
        ExportCsvInternal();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AuditProductionLocalization();
        Debug.Log("[Localization] Production localization build/update finished.");
    }

    [MenuItem("Tools/Skate Runner/Localization/Audit Production Localization", priority = 20)]
    public static void AuditProductionLocalization()
    {
        EnsureFolders();
        AuditResult result = InspectProductionAssets();
        int missingTranslations = CountMissingTranslations(out List<string> missingRows);
        int brokenReferences = CountBrokenReferences(out List<string> brokenRows);
        int hardcoded = ScanHardcodedStrings(out List<string> codeWarnings);
        int missingLatinGlyphs = ValidateFontGlyphsInternal(out List<string> glyphWarnings);
        int fitErrors = ValidateFitInternal(out List<string> fitWarnings);

        var report = new StringBuilder();
        report.AppendLine("# Production Localization Audit").AppendLine();
        report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").AppendLine();
        report.AppendLine("## Scope").AppendLine();
        foreach (string path in ProductionScenes.Concat(ProductionPrefabs)) report.AppendLine("- `" + path + "`");
        report.AppendLine().AppendLine("Debug Tools, third-party demos, internal IDs, save keys, scene names, and branding are excluded by policy.").AppendLine();
        report.AppendLine("## Summary").AppendLine();
        report.AppendLine($"- Production TMP components inspected: {result.TotalTmp}");
        report.AppendLine($"- Static TMP with LocalizeStringEvent: {result.LocalizedTmp}");
        report.AppendLine($"- Intentional/dynamic/unmapped TMP: {result.UnboundTmp}");
        report.AppendLine($"- Missing/empty translations: {missingTranslations}");
        report.AppendLine($"- Broken localized references: {brokenReferences}");
        report.AppendLine($"- Latin-table glyph warnings: {missingLatinGlyphs}");
        report.AppendLine("- Production Latin font chain: authored Lilita One/LayerLabs assets only; Extended ASCII Lilita atlases supply required accents.");
        report.AppendLine("- Lilita One source is included under OFL in Fonts; plain SDF uses a matching Lilita SDF fallback. Bitmap accents use matching styled LayerLabs atlases.");
        report.AppendLine("- Static checks do not certify appearance or fit. Live six-locale Game-view captures and drift observations are required; see ProductionPolishReport.md.");
        report.AppendLine($"- Static typography policy warnings: {fitErrors}");
        report.AppendLine($"- Likely hardcoded user-string warnings: {hardcoded}");
        report.AppendLine("- Known unresolved baked-text art: see `BakedTextAudit.md`.").AppendLine();
        AppendSection(report, "Unbound TMP review list", result.UnboundRows);
        AppendSection(report, "Missing translations", missingRows);
        AppendSection(report, "Broken references", brokenRows);
        AppendSection(report, "Font warnings", glyphWarnings);
        AppendSection(report, "Fit warnings", fitWarnings);
        AppendSection(report, "Likely hardcoded strings", codeWarnings);
        File.WriteAllText(AuditPath, report.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(AuditPath);
        Debug.Log($"[Localization] Audit written to {AuditPath}. Missing={missingTranslations}, Broken={brokenReferences}, LatinGlyph={missingLatinGlyphs}, Fit={fitErrors}, HardcodedWarnings={hardcoded}.");
    }

    [MenuItem("Tools/Skate Runner/Localization/Validate Tables", priority = 21)]
    public static void ValidateTables() => ValidateMissingEntries();

    [MenuItem("Tools/Skate Runner/Localization/Validate Missing Entries", priority = 22)]
    public static void ValidateMissingEntries()
    {
        int count = CountMissingTranslations(out List<string> rows);
        LogValidation("missing translations", count, rows);
    }

    [MenuItem("Tools/Skate Runner/Localization/Validate Font Glyphs", priority = 23)]
    public static void ValidateFontGlyphs()
    {
        int count = ValidateFontGlyphsInternal(out List<string> rows);
        LogValidation("font glyph warnings", count, rows);
    }

    [MenuItem("Tools/Skate Runner/Localization/Validate TMP Fit", priority = 24)]
    public static void ValidateTmpFit()
    {
        int count = ValidateFitInternal(out List<string> rows);
        LogValidation("TMP fit warnings", count, rows);
    }

    public static void ConfigureCjkFontFallback()
    {
        if (!TryConfigureCjkFontFallback(true))
            EditorUtility.DisplayDialog("CJK font required", "Import an approved Noto Sans SC or Source Han Sans SC .ttf/.otf into Assets, then run this command again.", "OK");
    }

    [MenuItem("Tools/Skate Runner/Localization/Report Baked Text", priority = 25)]
    public static void ReportBakedText()
    {
        AssetDatabase.ImportAsset(Root + "/BakedTextAudit.md");
        Object report = AssetDatabase.LoadAssetAtPath<Object>(Root + "/BakedTextAudit.md");
        Selection.activeObject = report;
        Debug.Log("[Localization] Opened the explicit production baked-text audit.");
    }

    [MenuItem("Tools/Skate Runner/Localization/Report Hardcoded User Strings", priority = 26)]
    public static void ReportHardcodedStrings()
    {
        int count = ScanHardcodedStrings(out List<string> rows);
        LogValidation("likely hardcoded user strings", count, rows);
    }

    [MenuItem("Tools/Skate Runner/Localization/Export CSV", priority = 40)]
    public static void ExportCsv()
    {
        EnsureFolders();
        ExportCsvInternal();
        AssetDatabase.Refresh();
        Debug.Log("[Localization] CSV files exported to " + ExportFolder + ".");
    }

    [MenuItem("Tools/Skate Runner/Localization/Test Locale/English")]
    private static void TestEnglish() => SelectEditorLocale("en");
    [MenuItem("Tools/Skate Runner/Localization/Test Locale/Spanish")]
    private static void TestSpanish() => SelectEditorLocale("es");
    [MenuItem("Tools/Skate Runner/Localization/Test Locale/Dutch")]
    private static void TestDutch() => SelectEditorLocale("nl");
    [MenuItem("Tools/Skate Runner/Localization/Test Locale/French")]
    private static void TestFrench() => SelectEditorLocale("fr");
    [MenuItem("Tools/Skate Runner/Localization/Test Locale/Portuguese (Brazil)")]
    private static void TestPortuguese() => SelectEditorLocale("pt-BR");
    [MenuItem("Tools/Skate Runner/Localization/Test Locale/German")]
    private static void TestGerman() => SelectEditorLocale("de");
    [MenuItem("Tools/Skate Runner/Localization/Test Locale/Pseudo (+35%)")]
    private static void TestPseudo() => SelectEditorLocale(PseudoCode);

    private static LocalizationSettings EnsureSettingsAndLocales()
    {
        LocalizationSettings settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Skate Runner Localization Settings";
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        settings.GetStringDatabase().UseFallback = true;
        settings.GetAssetDatabase().UseFallback = true;

        // Preserve the generated Chinese assets for future use, but keep zh-Hans
        // outside the launch build's supported locale provider.
        Locale dormantChinese = LocalizationEditorSettings.GetLocale(new LocaleIdentifier("zh-Hans"));
        if (dormantChinese != null)
        {
            LocalizationEditorSettings.RemoveLocale(dormantChinese, false);
        }

        var locales = new Dictionary<string, Locale>();
        foreach (string code in SkateLocalizationCatalog.ProductionLocaleCodes)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
            if (locale == null)
            {
                locale = Locale.CreateLocale(code);
                locale.name = NativeLocaleName(code) + " (" + code + ")";
                AssetDatabase.CreateAsset(locale, LocaleFolder + "/" + SafeFileName(locale.name) + ".asset");
                LocalizationEditorSettings.AddLocale(locale);
            }
            locales[code] = locale;
        }

        Locale english = locales["en"];
        foreach (Locale locale in locales.Values.Where(value => value != english))
        {
            FallbackLocale fallback = locale.Metadata.GetMetadata<FallbackLocale>();
            if (fallback == null) locale.Metadata.AddMetadata(new FallbackLocale(english));
            else fallback.Locale = english;
            EditorUtility.SetDirty(locale);
        }

        PseudoLocale pseudo = LocalizationEditorSettings.GetPseudoLocales().FirstOrDefault(value => value.Identifier.Code == PseudoCode);
        if (pseudo == null)
        {
            pseudo = PseudoLocale.CreatePseudoLocale();
            pseudo.Identifier = new LocaleIdentifier(PseudoCode);
            pseudo.name = "Pseudo (+35%) (qps-ploc)";
            Expander expander = pseudo.Methods.OfType<Expander>().FirstOrDefault();
            expander?.SetConstantExpansion(0.35f);
            AssetDatabase.CreateAsset(pseudo, LocaleFolder + "/Pseudo_35pct_qps-ploc.asset");
            LocalizationEditorSettings.AddLocale(pseudo);
        }
        FallbackLocale pseudoFallback = pseudo.Metadata.GetMetadata<FallbackLocale>();
        if (pseudoFallback == null) pseudo.Metadata.AddMetadata(new FallbackLocale(english));
        else pseudoFallback.Locale = english;
        // The production display fonts intentionally have a narrow decorative glyph set.
        // Keep pseudo-localization focused on layout expansion instead of introducing
        // accented test glyphs that render as tofu and obscure the actual fit result.
        pseudo.Methods.RemoveAll(method => method is Accenter);
        Expander pseudoExpander = pseudo.Methods.OfType<Expander>().FirstOrDefault();
        if (pseudoExpander != null)
        {
            pseudoExpander.SetConstantExpansion(0.35f);
            pseudoExpander.PaddingCharacters.Clear();
            pseudoExpander.PaddingCharacters.Add('W');
        }
        EditorUtility.SetDirty(pseudo);

        LocalizationSettings.ProjectLocale = english;
        string savedCode = PlayerPrefs.GetString(SkateLocalization.SelectedLocalePlayerPrefsKey, "en");
        if (!SkateLocalization.IsProductionLocaleCode(SkateLocalization.NormalizeLocaleCode(savedCode)))
        {
            PlayerPrefs.SetString(SkateLocalization.SelectedLocalePlayerPrefsKey, "en");
            PlayerPrefs.Save();
        }
        List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
        selectors.Clear();
        selectors.Add(new PlayerPrefLocaleSelector { PlayerPreferenceKey = SkateLocalization.SelectedLocalePlayerPrefsKey });
        selectors.Add(new LegacyLanguageLocaleSelector());
        selectors.Add(new SystemLocaleSelector());
        selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier("en") });
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static void EnsureTables()
    {
        List<Locale> locales = SkateLocalizationCatalog.ProductionLocaleCodes
            .Select(code => LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code)))
            .Where(locale => locale != null).ToList();

        foreach (string collectionName in SkateLocalizationCatalog.CollectionNames)
        {
            string collectionFolder = TableFolder + "/" + collectionName;
            Directory.CreateDirectory(collectionFolder);
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(collectionName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(collectionName, collectionFolder, locales);

            foreach (Locale locale in locales)
            {
                if (!collection.ContainsTable(locale.Identifier))
                {
                    string tablePath = collectionFolder + "/" + collectionName + "_" + locale.Identifier.Code + ".asset";
                    collection.AddNewTable(locale.Identifier, tablePath);
                }
            }

            foreach (SkateLocalizationCatalog.Entry source in SkateLocalizationCatalog.Entries.Where(value => value.Table == collectionName))
            {
                SharedTableData.SharedTableEntry shared = collection.SharedData.GetEntry(source.Key) ?? collection.SharedData.AddKey(source.Key);
                foreach (Locale locale in locales)
                {
                    StringTable table = (StringTable)collection.GetTable(locale.Identifier);
                    StringTableEntry entry = table.GetEntry(shared.Id) ?? table.AddEntry(shared.Id, source.ValueFor(locale.Identifier.Code));
                    entry.Value = source.ValueFor(locale.Identifier.Code);
                    entry.IsSmart = source.Smart;
                    EditorUtility.SetDirty(table);
                }
            }
            EditorUtility.SetDirty(collection.SharedData);
        }
    }

    private static void ConfigureLanguagePage()
    {
        const string scenePath = "Assets/Scenes/SkateRunnerStartScreen.unity";
        string restorePath = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        SettingsPopupController controller = Object.FindObjectsByType<SettingsPopupController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (controller == null)
        {
            Debug.LogWarning("[Localization] SettingsPopupController was not found; Language page was not expanded.");
            return;
        }

        Transform languagePage = FindInLoadedScene("StartScreenCanvas/Background/Popup/LanguagePage");
        Transform panel = languagePage != null ? languagePage.Find("LanguagePanel") : null;
        Transform english = panel != null ? panel.Find("Button_English") : null;
        if (languagePage == null || panel == null || english == null)
        {
            Debug.LogWarning("[Localization] Existing Settings LanguagePage hierarchy did not match the authored production UI.");
            return;
        }

        Transform dormantChineseRow = panel.Find("Button_SimplifiedChinese");
        if (dormantChineseRow != null)
        {
            Object.DestroyImmediate(dormantChineseRow.gameObject);
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.10f);
        panelRect.anchorMax = new Vector2(0.88f, 0.84f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        string[] codes = SkateLocalizationCatalog.ProductionLocaleCodes;
        var options = new LocalizationLanguageMenu.Option[codes.Length];
        for (int i = 0; i < codes.Length; i++)
        {
            string code = codes[i];
            string objectName = "Button_" + LocaleObjectSuffix(code);
            Transform row = i == 0 ? english : panel.Find(objectName);
            if (row == null)
            {
                row = Object.Instantiate(english.gameObject, panel).transform;
                row.name = objectName;
            }
            if (i == 0) row.name = objectName;

            RectTransform rect = row.GetComponent<RectTransform>();
            float top = 0.96f - i * 0.157f;
            rect.anchorMin = new Vector2(0.06f, top - 0.125f);
            rect.anchorMax = new Vector2(0.94f, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            TMP_Text label = row.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
            if (label != null)
            {
                label.text = NativeLocaleName(code);
                LocalizeStringEvent accidental = label.GetComponent<LocalizeStringEvent>();
                if (accidental != null) Object.DestroyImmediate(accidental);
                LocalizedTMPFitPolicy fit = label.GetComponent<LocalizedTMPFitPolicy>() ?? label.gameObject.AddComponent<LocalizedTMPFitPolicy>();
                fit.Configure(AuthoredFontSize(label), 20f, true, true, label.text.Length);
            }
            GameObject selected = row.Find("Icon_Selected")?.gameObject;
            options[i] = new LocalizationLanguageMenu.Option
            {
                localeCode = code,
                button = row.GetComponent<Button>(),
                label = label,
                selectedVisual = selected
            };
        }

        Transform comingLater = panel.Find("Text_MoreLanguagesComingLater");
        if (comingLater != null) Object.DestroyImmediate(comingLater.gameObject);
        TMP_Text currentLabel = controller.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(value => value.name == "Text_CurrentLanguage");
        LocalizationLanguageMenu menu = languagePage.GetComponent<LocalizationLanguageMenu>()
            ?? languagePage.gameObject.AddComponent<LocalizationLanguageMenu>();
        menu.Configure(currentLabel, options);
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!string.IsNullOrEmpty(restorePath) && restorePath != scenePath) EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
    }

    private static void BindProductionStaticText()
    {
        string restorePath = SceneManager.GetActiveScene().path;
        foreach (string scenePath in ProductionScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int changed = BindRootObjects(scene.GetRootGameObjects());
            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[Localization] {scenePath}: bound/updated {changed} static TMP labels.");
        }

        foreach (string prefabPath in ProductionPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int changed = BindRootObjects(new[] { root });
                if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Localization] {prefabPath}: bound/updated {changed} static TMP labels.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (!string.IsNullOrEmpty(restorePath)) EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
    }

    private static int BindRootObjects(IEnumerable<GameObject> roots)
    {
        int changed = 0;
        foreach (TMP_Text text in roots.SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
        {
            string path = HierarchyPath(text.transform);
            if (ShouldAddDynamicFit(text, path))
            {
                LocalizedTMPFitPolicy dynamicFit = text.GetComponent<LocalizedTMPFitPolicy>() ?? Undo.AddComponent<LocalizedTMPFitPolicy>(text.gameObject);
                ConfigureFit(dynamicFit, text, path);
                EditorUtility.SetDirty(dynamicFit);
                changed++;
            }

            if (!ShouldBind(text, path)) continue;
            if (!SkateLocalizationCatalog.TryFindStatic(text.text, out SkateLocalizationCatalog.Entry catalog)) continue;

            LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>() ?? Undo.AddComponent<LocalizeStringEvent>(text.gameObject);
            localizer.StringReference.SetReference(catalog.Table, catalog.Key);
            if (localizer.OnUpdateString.GetPersistentEventCount() == 0)
            {
                var action = Delegate.CreateDelegate(typeof(UnityAction<string>), text, text.GetType().GetProperty("text").GetSetMethod()) as UnityAction<string>;
                UnityEventTools.AddPersistentListener(localizer.OnUpdateString, action);
                localizer.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            }

            LocalizedTMPFitPolicy fit = text.GetComponent<LocalizedTMPFitPolicy>() ?? Undo.AddComponent<LocalizedTMPFitPolicy>(text.gameObject);
            ConfigureFit(fit, text, path);
            EditorUtility.SetDirty(localizer);
            EditorUtility.SetDirty(fit);
            changed++;
        }
        return changed;
    }

    private static bool ShouldAddDynamicFit(TMP_Text text, string path)
    {
        if (path.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0) return false;

        string objectName = text.name;
        if (objectName.Equals("CurrentLevel", StringComparison.OrdinalIgnoreCase)
            || objectName.Equals("NextLevel", StringComparison.OrdinalIgnoreCase))
            return true;

        string[] dynamicTokens =
        {
            "Cost", "Price", "Balance", "Amount", "Countdown", "Timer", "Progress", "SpinsLeft",
            "Status", "Result", "RewardText", "Text_Reward", "CurrencyValue", "LevelValue", "Count"
        };
        return dynamicTokens.Any(token => objectName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool ShouldBind(TMP_Text text, string path)
    {
        if (text.name == "PhaseTimerText") return false; // Numeric runtime countdown; intentionally blank outside Phase 1.
        if (string.IsNullOrWhiteSpace(text.text)) return false;
        string normalizedPath = path.Replace(" ", string.Empty);
        if (normalizedPath.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (normalizedPath.IndexOf("LanguagePanel/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (normalizedPath.IndexOf("Text_CurrentLanguage", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        string objectName = text.name;
        string[] runtimeTokens =
        {
            "Cost", "Price", "Balance", "Amount", "Countdown", "TimerValue", "Progress", "SpinsLeft",
            "Version", "Status", "Result", "RewardValue", "CurrencyValue", "LevelValue", "Count"
        };
        return !runtimeTokens.Any(token => objectName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void ConfigureFit(LocalizedTMPFitPolicy fit, TMP_Text text, string path)
    {
        float maximum = AuthoredFontSize(text);
        bool body = path.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("Message", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0;
        bool compactHomeButton = path.IndexOf("HomepageRoot", StringComparison.OrdinalIgnoreCase) >= 0
            && (path.IndexOf("FreeCash", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("NoAds", StringComparison.OrdinalIgnoreCase) >= 0);
        bool dynamicLevel = text.name.Equals("CurrentLevel", StringComparison.OrdinalIgnoreCase)
            || text.name.Equals("NextLevel", StringComparison.OrdinalIgnoreCase);
        float minimum = maximum >= 50f ? 26f : maximum >= 34f ? 21f : Mathf.Min(maximum, 18f);
        if (compactHomeButton) minimum = Mathf.Min(maximum, 14f);
        else if (dynamicLevel) minimum = Mathf.Min(maximum, 16f);
        fit.Configure(maximum, minimum, true, !body, SkateLocalizationCatalog.Normalize(text.text).Length);
    }

    private static float AuthoredFontSize(TMP_Text text)
    {
        LocalizedTMPFitPolicy existing = text.GetComponent<LocalizedTMPFitPolicy>();
        if (existing != null && !string.IsNullOrEmpty(existing.AuthoredText)) return existing.maxFontSize;
        return Mathf.Max(1f, text.enableAutoSizing ? text.fontSizeMax : text.fontSize);
    }

    private static AuditResult InspectProductionAssets()
    {
        var result = new AuditResult();
        string restorePath = SceneManager.GetActiveScene().path;
        foreach (string scenePath in ProductionScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            InspectRoots(scenePath, scene.GetRootGameObjects(), result);
        }
        foreach (string prefabPath in ProductionPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try { InspectRoots(prefabPath, new[] { root }, result); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        if (!string.IsNullOrEmpty(restorePath)) EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
        return result;
    }

    private static void InspectRoots(string assetPath, IEnumerable<GameObject> roots, AuditResult result)
    {
        foreach (TMP_Text text in roots.SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
        {
            result.TotalTmp++;
            if (text.GetComponent<LocalizeStringEvent>() != null) result.LocalizedTmp++;
            else if (ShouldReportUnbound(text, HierarchyPath(text.transform)))
            {
                result.UnboundTmp++;
                if (!string.IsNullOrWhiteSpace(text.text))
                    result.UnboundRows.Add($"{assetPath} :: {HierarchyPath(text.transform)} :: `{SkateLocalizationCatalog.Normalize(text.text)}`");
            }
        }
    }

    private static bool ShouldReportUnbound(TMP_Text text, string path)
    {
        string value = SkateLocalizationCatalog.Normalize(text.text);
        if (string.IsNullOrEmpty(value)) return false;
        if (path.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (path.IndexOf("LanguagePanel/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (path.IndexOf("Text_CurrentLanguage", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (path.IndexOf("CurrentLevel", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("NextLevel", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("Text_Count", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (Regex.IsMatch(value, @"^[\d\s.,/+%:$]+$")) return false;
        if (Regex.IsMatch(value, @"^(?:USD\s*)?\$\d") || Regex.IsMatch(value, @"^\$\d.*USD$")) return false;
        string[] runtimeOrPlaceholder =
        {
            "TITLE", "DESCRIPTION", "REWARD", "Remember me", "0 Left", "0s", "23h 59m",
            "ENDS IN 00:00:00", "RESETS IN 00:00:00", "Version", "+500 CASH"
        };
        if (runtimeOrPlaceholder.Contains(value)) return false;
        return true;
    }

    private static int CountMissingTranslations(out List<string> rows)
    {
        rows = new List<string>();
        foreach (SkateLocalizationCatalog.Entry source in SkateLocalizationCatalog.Entries)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(source.Table);
            if (collection == null)
            {
                rows.Add(source.Table + " :: collection missing");
                continue;
            }
            foreach (string code in SkateLocalizationCatalog.ProductionLocaleCodes)
            {
                StringTable table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                StringTableEntry entry = table?.GetEntry(source.Key);
                if (entry == null || string.IsNullOrWhiteSpace(entry.Value)) rows.Add($"{source.Table}/{source.Key} :: {code}");
            }
        }
        return rows.Count;
    }

    private static int CountBrokenReferences(out List<string> rows)
    {
        rows = new List<string>();
        string restorePath = SceneManager.GetActiveScene().path;
        foreach (string scenePath in ProductionScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateReferenceRoots(scenePath, scene.GetRootGameObjects(), rows);
        }
        foreach (string prefabPath in ProductionPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try { ValidateReferenceRoots(prefabPath, new[] { root }, rows); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        if (!string.IsNullOrEmpty(restorePath)) EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
        return rows.Count;
    }

    private static void ValidateReferenceRoots(string assetPath, IEnumerable<GameObject> roots, List<string> rows)
    {
        foreach (LocalizeStringEvent localizer in roots.SelectMany(root => root.GetComponentsInChildren<LocalizeStringEvent>(true)))
        {
            string tableName = localizer.StringReference.TableReference;
            string key = localizer.StringReference.TableEntryReference;
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null || collection.SharedData.GetEntry(key) == null)
                rows.Add($"{assetPath} :: {HierarchyPath(localizer.transform)} :: {tableName}/{key}");
        }
    }

    private static int ValidateFontGlyphsInternal(out List<string> rows)
    {
        rows = new List<string>();
        var fontPaths = new HashSet<string>();
        string restorePath = SceneManager.GetActiveScene().path;
        foreach (string scenePath in ProductionScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (TMP_Text text in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
            {
                if (text.font == null) continue;
                string fontPath = AssetDatabase.GetAssetPath(text.font);
                if (!string.IsNullOrEmpty(fontPath)) fontPaths.Add(fontPath);
            }
        }
        if (!string.IsNullOrEmpty(restorePath)) EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);

        foreach (string code in SkateLocalizationCatalog.ProductionLocaleCodes)
        {
            string characters = string.Concat(SkateLocalizationCatalog.Entries.Select(entry => entry.ValueFor(code))).Distinct()
                .Where(character => !char.IsWhiteSpace(character)).Aggregate(string.Empty, (current, character) => current + character);
            foreach (string fontPath in fontPaths)
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                if (font == null) continue;
                if (!font.HasCharacters(characters, out uint[] missing, true, false) && missing != null && missing.Length > 0)
                {
                    string sample = new string(missing.Take(24).Select(value => (char)value).ToArray());
                    rows.Add($"{code} :: {font.name} :: {missing.Length} missing :: `{sample}`");
                }
            }
        }
        return rows.Count;
    }

    private static void EnsureLilitaLatinExtendedFallbacks()
    {
        RepairLilitaBitmapAtlasReferences();
        Dictionary<string, TMP_FontAsset> extendedByName = AssetDatabase
            .FindAssets("t:TMP_FontAsset", new[] { LayerLabsFontFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(font => font != null && font.name.Contains("LilitaOne-Regular") && font.name.Contains("Extended ASCII"))
            .ToDictionary(font => font.name, StringComparer.Ordinal);

        foreach (string fontPath in CollectProductionFontPaths())
        {
            TMP_FontAsset primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (primary == null || !primary.name.StartsWith("LilitaOne-Regular", StringComparison.Ordinal)
                || primary.name.Contains("Extended ASCII"))
            {
                continue;
            }

            if (primary.name == "LilitaOne-Regular SDF")
            {
                TMP_FontAsset sdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/SkateRunner_LilitaLatinSDF.asset");
                if (sdf != null)
                {
                    primary.fallbackFontAssetTable = new List<TMP_FontAsset> { sdf };
                    EditorUtility.SetDirty(primary);
                }
                continue;
            }

            Match size = Regex.Match(primary.name, @"Outline (\d+) SDF$");
            string preferredName = size.Success
                ? "LilitaOne-Regular Outline_Extended ASCII_" + size.Groups[1].Value + " SDF"
                : "LilitaOne-Regular Outline_Extended ASCII_72 SDF";
            if (!extendedByName.TryGetValue(preferredName, out TMP_FontAsset extended))
            {
                Debug.LogWarning("[Localization] Matching Lilita One Extended ASCII atlas not found for " + primary.name + ".");
                continue;
            }

            if (primary.fallbackFontAssetTable == null)
            {
                primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }
            primary.fallbackFontAssetTable.RemoveAll(font => font == null || font.name.IndexOf("Liberation", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!primary.fallbackFontAssetTable.Contains(extended))
            {
                primary.fallbackFontAssetTable.Insert(0, extended);
            }
            EditorUtility.SetDirty(primary);
        }
    }

    [MenuItem("Tools/Skate Runner/Localization/Repair LayerLabs Bitmap Atlas References")]
    public static void RepairLilitaBitmapAtlasReferences()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { LayerLabsFontFolder }))
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (font == null || !font.name.StartsWith("LilitaOne-Regular") || font.material == null
                || font.material.shader.name != "TextMeshPro/Bitmap Custom Atlas") continue;
            Texture2D styledAtlas = font.material.mainTexture as Texture2D;
            if (styledAtlas == null || !AssetDatabase.GetAssetPath(styledAtlas).EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            // LayerLabs' appearance is baked into this RGBA PNG. The embedded
            // alpha-only generation atlas must never be used for fallback meshes.
            SerializedObject serialized = new SerializedObject(font);
            SerializedProperty atlases = serialized.FindProperty("m_AtlasTextures");
            if (atlases.arraySize == 1 && atlases.GetArrayElementAtIndex(0).objectReferenceValue != styledAtlas)
            {
                atlases.GetArrayElementAtIndex(0).objectReferenceValue = styledAtlas;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(font);
            }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Skate Runner/Localization/Build Matching Lilita SDF Fallback")]
    public static void BuildMatchingLilitaSdfFallback()
    {
        const string path = Root + "/Fonts/SkateRunner_LilitaLatinSDF.asset";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            if (source == null) throw new InvalidOperationException("The licensed Lilita One source font is missing.");
            font = TMP_FontAsset.CreateFontAsset(source, 70, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            font.name = "SkateRunner_LilitaLatinSDF";
            string characters = string.Concat(SkateLocalizationCatalog.Entries.SelectMany(entry =>
                SkateLocalizationCatalog.ProductionLocaleCodes.Select(code => entry.ValueFor(code))))
                + "äöüÄÖÜßéèêëàâçîïôùûÉÀÇãõáâéêíóôúçáéíóúüñÑ";
            characters = new string(characters.Where(c => !char.IsControl(c)).Distinct().ToArray());
            if (!font.TryAddCharacters(characters, out string missing))
                throw new InvalidOperationException("Lilita One is missing: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
        }
        TMP_FontAsset primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LayerLabsFontFolder + "/LilitaOne-Regular SDF.asset");
        primary.fallbackFontAssetTable = new List<TMP_FontAsset> { font };
        EditorUtility.SetDirty(primary);
        AssetDatabase.SaveAssets();
    }

    // Static typography checks cannot certify live layout or fallback glyph appearance.
    // Use LocalizationVisualQA in Play Mode for visible labels and repeated locale cycles.
    private static int ValidateFitInternal(out List<string> rows)
    {
        rows = new List<string>();
        if (EditorApplication.isPlaying)
        {
            rows.Add("Static audit requires Edit Mode. Run LocalizationVisualQA for live screens.");
            return rows.Count;
        }
        string restorePath = SceneManager.GetActiveScene().path;
        try
        {
            foreach (string scenePath in ProductionScenes)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (LocalizedTMPFitPolicy fit in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LocalizedTMPFitPolicy>(true)))
                {
                    TMP_Text text = fit.GetComponent<TMP_Text>();
                    string location = scenePath + " :: " + HierarchyPath(fit.transform);
                    if (text == null || text.font == null || text.fontSharedMaterial == null)
                    {
                        rows.Add(location + " :: missing TMP/font/material; cannot validate");
                        continue;
                    }
                    if (!new SerializedObject(fit).FindProperty("baselineCaptured").boolValue)
                        rows.Add(location + " :: authored baseline has not been captured");
                    if (fit.minFontSize < fit.maxFontSize * 0.799f || text.fontSize < fit.maxFontSize * 0.799f)
                        rows.Add($"{location} :: suspicious shrink: effective {text.fontSize:0.##}, floor {fit.minFontSize:0.##}, authored {fit.maxFontSize:0.##}");
                    if (text.fontSize < 16f && !string.IsNullOrWhiteSpace(text.text))
                        rows.Add(location + " :: effective size below 16; inspect visual scale");
                    if (!text.font.name.Contains("Lilita"))
                        rows.Add(location + " :: unexpected font " + text.font.name);
                    if (text.GetComponents<LocalizeStringEvent>().Length > 1)
                        rows.Add(location + " :: duplicate localization writers");
                    if (text.font.material.shader.name.Contains("Bitmap") && text.font.atlasTextures.Length > 0
                        && text.font.material.mainTexture != text.font.atlasTextures[0])
                        rows.Add(location + " :: bitmap atlas/material mismatch");
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(restorePath)) EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
        }
        return rows.Count;
    }

    private static int ScanHardcodedStrings(out List<string> rows)
    {
        rows = new List<string>();
        Regex assignment = new Regex("(?:\\.text\\s*=|SetText\\s*\\(|ShowPopup\\s*\\()\\s*(?:\\$?\\\"[^\\\"]*[A-Za-z][^\\\"]*\\\")", RegexOptions.Compiled);
        foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.IndexOf("/Debug", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("/Editor", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("Localization/", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.EndsWith("/ShadowDebug.cs", StringComparison.OrdinalIgnoreCase)) continue;
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool numericOnlyUi = line.Contains("PhaseTimerText.text")
                    || line.Contains("dailySpinCountText.text")
                    || line.Contains("percentageText.text");
                if (!numericOnlyUi && assignment.IsMatch(line)) rows.Add($"{normalized}:{i + 1} :: `{line.Trim()}`");
            }
        }
        return rows.Count;
    }

    private static void ExportCsvInternal()
    {
        foreach (StringTableCollection collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            string path = ExportFolder + "/" + collection.TableCollectionName + ".csv";
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true))) Csv.Export(writer, collection);
        }
    }

    private static bool TryConfigureCjkFontFallback(bool logMissing)
    {
        Directory.CreateDirectory(Root + "/Fonts");
        Font source = AssetDatabase.FindAssets("t:Font", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault(font => font != null &&
                (font.name.IndexOf("NotoSansSC", StringComparison.OrdinalIgnoreCase) >= 0
                 || font.name.IndexOf("Noto Sans SC", StringComparison.OrdinalIgnoreCase) >= 0
                 || font.name.IndexOf("SourceHanSans", StringComparison.OrdinalIgnoreCase) >= 0
                 || font.name.IndexOf("Source Han Sans", StringComparison.OrdinalIgnoreCase) >= 0));
        if (source == null)
        {
            if (logMissing) Debug.LogWarning("[Localization] CJK fallback not configured: import an approved Noto Sans SC or Source Han Sans SC .ttf/.otf into Assets.");
            return false;
        }

        const string cjkAssetPath = Root + "/Fonts/SkateRunner_CJK_Dynamic.asset";
        TMP_FontAsset cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(cjkAssetPath);
        if (cjk == null)
        {
            cjk = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            cjk.name = "SkateRunner CJK Dynamic";
            AssetDatabase.CreateAsset(cjk, cjkAssetPath);
        }

        foreach (string fontPath in CollectProductionFontPaths())
        {
            TMP_FontAsset primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (primary == null || primary == cjk) continue;
            if (primary.fallbackFontAssetTable == null) primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!primary.fallbackFontAssetTable.Contains(cjk))
            {
                primary.fallbackFontAssetTable.Add(cjk);
                EditorUtility.SetDirty(primary);
            }
        }
        EditorUtility.SetDirty(cjk);
        AssetDatabase.SaveAssets();
        Debug.Log("[Localization] Configured Dynamic SDF CJK fallback: " + cjkAssetPath);
        return true;
    }

    private static HashSet<string> CollectProductionFontPaths()
    {
        var paths = new HashSet<string>();
        string restorePath = SceneManager.GetActiveScene().path;
        foreach (string scenePath in ProductionScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (TMP_Text text in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
            {
                string path = text.font != null ? AssetDatabase.GetAssetPath(text.font) : string.Empty;
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }
        }
        foreach (string prefabPath in ProductionPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    string path = text.font != null ? AssetDatabase.GetAssetPath(text.font) : string.Empty;
                    if (!string.IsNullOrEmpty(path)) paths.Add(path);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        if (!string.IsNullOrEmpty(restorePath)) EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
        return paths;
    }

    private static void SelectEditorLocale(string code)
    {
        Locale locale = code == PseudoCode
            ? LocalizationEditorSettings.GetPseudoLocales().FirstOrDefault(value => value.Identifier.Code == code)
            : LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
        if (locale == null)
        {
            Debug.LogError("[Localization] Locale not built yet. Run Build or Update Production Localization first.");
            return;
        }
        LocalizationSettings.SelectedLocale = locale;
        SceneView.RepaintAll();
        Debug.Log("[Localization] Editor test locale: " + locale.Identifier.Code);
    }

    private static void EnsureFolders()
    {
        foreach (string path in new[] { Root, Root + "/Settings", LocaleFolder, TableFolder, ExportFolder }) Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
    }

    private static Transform FindInLoadedScene(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(value => value.name == parts[0]);
        if (root == null) return null;
        return parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts.Skip(1)));
    }

    private static string NativeLocaleName(string code)
    {
        switch (code)
        {
            case "es": return "Español";
            case "nl": return "Nederlands";
            case "fr": return "Français";
            case "pt-BR": return "Português (Brasil)";
            case "de": return "Deutsch";
            default: return "English";
        }
    }

    private static string LocaleObjectSuffix(string code)
    {
        switch (code)
        {
            case "es": return "Spanish";
            case "nl": return "Dutch";
            case "fr": return "French";
            case "pt-BR": return "PortugueseBrazil";
            case "de": return "German";
            default: return "English";
        }
    }

    private static string SafeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
        return value;
    }

    private static string HierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
        return path;
    }

    private static void AppendSection(StringBuilder report, string title, List<string> rows)
    {
        report.AppendLine("## " + title).AppendLine();
        if (rows.Count == 0) report.AppendLine("None.");
        else foreach (string row in rows) report.AppendLine("- " + row);
        report.AppendLine();
    }

    private static void LogValidation(string label, int count, List<string> rows)
    {
        if (count == 0) Debug.Log("[Localization] Validation passed: 0 " + label + ".");
        else Debug.LogWarning("[Localization] " + count + " " + label + ":\n" + string.Join("\n", rows.Take(100)));
    }

    private sealed class AuditResult
    {
        internal int TotalTmp;
        internal int LocalizedTmp;
        internal int UnboundTmp;
        internal readonly List<string> UnboundRows = new List<string>();
    }
}
#endif
