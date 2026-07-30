using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Idempotent editor wiring for the authored Rollerblade assets and existing Start Screen UI.
/// Runtime code contains no AssetDatabase or hierarchy-search dependency.
/// </summary>
public static class RollerbladePipelineBuilder
{
    private const string DefinitionsFolder = "Assets/Prefabs/Rollerblades/Definitions";
    private const string PreviewFolder = "Assets/Prefabs/PreviewSprites/RollerbladeSprites";
    private const string TemplatePath = PreviewFolder + "/RollerbladesTurntable.anim";
    private const string ControllerPath = PreviewFolder + "/RollerbladeSpriteAnimationController.controller";
    private const string PlayerPrefabPath = "Assets/Prefabs/Characters/S_01_Male.prefab";
    private const int PreviewColumns = 6;
    private const int PreviewRows = 4;
    private const int PreviewFrameCount = PreviewColumns * PreviewRows;
    private const int PreviewMaxTextureSize = 4096;

    private const string ShopScrollPath =
        "StartScreenCanvas/Background/FullscreenPopupRoot/ShopPage/Shop/ScrollRect_Rollerblades";
    private const string PopupPath =
        "StartScreenCanvas/Background/FullscreenPopupRoot/ShopPage/AbilityPurchaseConfirmationPopup";
    private const string InventoryScrollPath =
        "StartScreenCanvas/Background/FullscreenPopupRoot/InventoryPage/Left/MidScreenLeft/InventoryListFrame/ScrollRect_RollerBlades";
    private const string PreviewImagePath =
        "StartScreenCanvas/Background/FullscreenPopupRoot/InventoryPage/Right/MidScreenRight/PreviewFrame/PowerPreviewViewport/PowerPreviewImage";
    private const string EquipButtonPath =
        "StartScreenCanvas/Background/FullscreenPopupRoot/InventoryPage/Right/MidScreenRight/EquipButton";

    private sealed class DefinitionSpec
    {
        public RollerbladeId Id;
        public string LeftPrefabPath;
        public string RightPrefabPath;
        public string SpriteSheetPath;
        public string ClipPath;
    }

    private sealed class ShopSpec
    {
        public string CardName;
        public RollerbladeId Id;
        public RollerbladePurchaseType PurchaseType;
        public int GemCost;
        public int CashCost;
        public string CardPrice;
    }

    private static readonly DefinitionSpec[] DefinitionSpecs =
    {
        Definition(
            RollerbladeId.Default,
            "DefaultRollerBlade_Left.prefab",
            "DefaultRollerBlade_Right.prefab",
            "Default_RollerbladeSprite.png",
            "DefaultRollerbladeTurntable.anim"),
        Definition(
            RollerbladeId.UrbanRush,
            "UrbanRush_Left.prefab",
            "UrbanRush_Right.prefab",
            "UrbanRush_RollerbladeSprite.png",
            "UrbanRushRollerbladeTurntable.anim"),
        Definition(
            RollerbladeId.NeonVelocity,
            "NeonVelocity_Left.prefab",
            "NeonVelocity_Right.prefab",
            "NeonVelocity_RollerbladeSprite.png",
            "NeonVelocityRollerbladeTurntable.anim"),
        Definition(
            RollerbladeId.FrostbiteGlide,
            "FrostbiteGlide_Left.prefab",
            "FrostbiteGlide_Right.prefab",
            "FrostbiteGlide_RollerbladeSprite.png",
            "FrostbiteGlideRollerbladeTurntable.anim"),
        Definition(
            RollerbladeId.InfernoDrift,
            "InfernoDrift_Left.prefab",
            "InfernoDrift_Right.prefab",
            "InfernoDrift_RollerbladeSprite.png",
            "InfernoDriftRollerbladeTurntable.anim"),
        Definition(
            RollerbladeId.CelestialApex,
            "CelestialApex_Left.prefab",
            "CelestialApex_Right.prefab",
            "CelestialApex_RollerbladeSprite.png",
            "CelestialApexRollerbladeTurntable.anim")
    };

    private static readonly ShopSpec[] ShopSpecs =
    {
        new ShopSpec
        {
            CardName = "InfernoDrift_Rollerblades",
            Id = RollerbladeId.InfernoDrift,
            PurchaseType = RollerbladePurchaseType.RealMoneyPlaceholder,
            CardPrice = "$1.99"
        },
        new ShopSpec
        {
            CardName = "UrbanRush_Rollerblades",
            Id = RollerbladeId.UrbanRush,
            PurchaseType = RollerbladePurchaseType.Cash,
            CashCost = 25000,
            CardPrice = "25000"
        },
        new ShopSpec
        {
            CardName = "NeonVelocity_Rollerblades",
            Id = RollerbladeId.NeonVelocity,
            PurchaseType = RollerbladePurchaseType.Cash,
            CashCost = 45000,
            CardPrice = "45000"
        },
        new ShopSpec
        {
            CardName = "FrostbiteGlide_Rollerblades",
            Id = RollerbladeId.FrostbiteGlide,
            PurchaseType = RollerbladePurchaseType.Gems,
            GemCost = 2500,
            CardPrice = "2500"
        },
        new ShopSpec
        {
            CardName = "CelestialApex_Rollerblades",
            Id = RollerbladeId.CelestialApex,
            PurchaseType = RollerbladePurchaseType.Gems,
            GemCost = 1500,
            CardPrice = "1500"
        }
    };

    private static readonly KeyValuePair<string, RollerbladeId>[] InventorySlots =
    {
        new KeyValuePair<string, RollerbladeId>("Default_Slot", RollerbladeId.Default),
        new KeyValuePair<string, RollerbladeId>("InfernoDrift_Slot", RollerbladeId.InfernoDrift),
        new KeyValuePair<string, RollerbladeId>("UrbanRush_Slot", RollerbladeId.UrbanRush),
        new KeyValuePair<string, RollerbladeId>("NeonVelocity_Slot", RollerbladeId.NeonVelocity),
        new KeyValuePair<string, RollerbladeId>("FrostbiteGlide_Slot", RollerbladeId.FrostbiteGlide),
        new KeyValuePair<string, RollerbladeId>("CelestialApex_Slot", RollerbladeId.CelestialApex)
    };

    [MenuItem("Tools/Skate Runner/Build Rollerblade Pipeline")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Open the current Start Screen scene before building Rollerblades.");

        EnsureFolder("Assets/Prefabs/Rollerblades", "Definitions");
        ConfigurePreviewSpriteSheets();

        AnimationClip template = BuildPreviewClip(
            TemplatePath,
            DefinitionSpecs[0].SpriteSheetPath,
            true);
        AnimatorController previewController = ConfigurePreviewController(template);

        Dictionary<RollerbladeId, AnimationClip> clips = new Dictionary<RollerbladeId, AnimationClip>();
        for (int i = 0; i < DefinitionSpecs.Length; i++)
        {
            DefinitionSpec spec = DefinitionSpecs[i];
            clips[spec.Id] = BuildPreviewClip(spec.ClipPath, spec.SpriteSheetPath, false);
        }

        RollerbladeSideTransform defaultLeftTransform;
        RollerbladeSideTransform defaultRightTransform;
        RollerbladeDefinition[] definitions = ConfigurePlayerPrefabAndDefinitions(
            out defaultLeftTransform,
            out defaultRightTransform);

        WireShopScene();
        WireInventoryScene(previewController, template, clips);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Rollerblade pipeline built: " + definitions.Length +
            " definitions, " + clips.Count + " previews, Shop/Inventory UI, and gameplay pair sockets.");
    }

    [MenuItem("Tools/Skate Runner/Rebuild Rollerblade Inventory Previews")]
    public static void RebuildInventoryPreviews()
    {
        ConfigurePreviewSpriteSheets();

        AnimationClip template = BuildPreviewClip(
            TemplatePath,
            DefinitionSpecs[0].SpriteSheetPath,
            true);
        ConfigurePreviewController(template);

        for (int i = 0; i < DefinitionSpecs.Length; i++)
        {
            DefinitionSpec spec = DefinitionSpecs[i];
            BuildPreviewClip(spec.ClipPath, spec.SpriteSheetPath, false);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "Rebuilt six Rollerblade Inventory previews from uniform 24-frame sprite sheets at 12 FPS.");
    }

    private static RollerbladeDefinition[] ConfigurePlayerPrefabAndDefinitions(
        out RollerbladeSideTransform defaultLeftTransform,
        out RollerbladeSideTransform defaultRightTransform)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (prefabRoot == null)
            throw new InvalidOperationException("Could not load player prefab: " + PlayerPrefabPath);

        try
        {
            Transform leftFoot = prefabRoot.transform.Find(
                "Body/Armature/Hips/UpLeg.L/Leg.L/Foot.L");
            Transform rightFoot = prefabRoot.transform.Find(
                "Body/Armature/Hips/UpLeg.R/Leg.R/Foot.R");

            if (leftFoot == null || rightFoot == null)
                throw new InvalidOperationException("Could not find both authored Foot transforms in the player prefab.");

            Transform staticLeft = leftFoot.Find("P_Roller_Left");
            Transform staticRight = rightFoot.Find("P_Roller_Right");
            if (staticLeft == null || staticRight == null)
            {
                throw new InvalidOperationException(
                    "Could not find both original static P_Roller objects in the player prefab.");
            }

            defaultLeftTransform = CaptureTransform(staticLeft);
            defaultRightTransform = CaptureTransform(staticRight);

            Transform leftSocket = EnsureSocket(leftFoot, "RollerbladeSocket_Left", staticLeft.gameObject.layer);
            Transform rightSocket = EnsureSocket(rightFoot, "RollerbladeSocket_Right", staticRight.gameObject.layer);

            RollerbladeDefinition[] definitions = BuildDefinitions(
                defaultLeftTransform,
                defaultRightTransform);

            RollerbladeEquipper equipper = prefabRoot.GetComponent<RollerbladeEquipper>();
            if (equipper == null)
                equipper = prefabRoot.AddComponent<RollerbladeEquipper>();

            SerializedObject serializedEquipper = new SerializedObject(equipper);
            SetObject(serializedEquipper, "leftSocket", leftSocket);
            SetObject(serializedEquipper, "rightSocket", rightSocket);
            SetObject(serializedEquipper, "staticLeftRollerblade", staticLeft.gameObject);
            SetObject(serializedEquipper, "staticRightRollerblade", staticRight.gameObject);
            SetObjectArray(serializedEquipper, "rollerbladeDefinitions", definitions);
            serializedEquipper.ApplyModifiedPropertiesWithoutUndo();

            staticLeft.gameObject.SetActive(true);
            staticRight.gameObject.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            return definitions;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static RollerbladeDefinition[] BuildDefinitions(
        RollerbladeSideTransform defaultLeftTransform,
        RollerbladeSideTransform defaultRightTransform)
    {
        RollerbladeDefinition[] results = new RollerbladeDefinition[DefinitionSpecs.Length];

        for (int i = 0; i < DefinitionSpecs.Length; i++)
        {
            DefinitionSpec spec = DefinitionSpecs[i];
            string assetPath = DefinitionsFolder + "/Rollerblade_" + spec.Id + ".asset";
            RollerbladeDefinition definition =
                AssetDatabase.LoadAssetAtPath<RollerbladeDefinition>(assetPath);
            bool createdNewDefinition = definition == null;

            if (createdNewDefinition)
            {
                definition = ScriptableObject.CreateInstance<RollerbladeDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            definition.rollerbladeId = spec.Id;
            definition.leftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.LeftPrefabPath);
            definition.rightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.RightPrefabPath);

            if (definition.leftPrefab == null || definition.rightPrefab == null)
                throw new InvalidOperationException("Missing pair prefab for " + spec.Id + ".");

            if (spec.Id == RollerbladeId.Default)
            {
                definition.leftTransform = defaultLeftTransform;
                definition.rightTransform = defaultRightTransform;
            }
            else if (createdNewDefinition)
            {
                // Premium alignment is intentionally left for later manual configuration.
                // Rerunning this builder must not erase those future per-side adjustments.
                definition.leftTransform = RollerbladeSideTransform.Identity;
                definition.rightTransform = RollerbladeSideTransform.Identity;
            }

            EditorUtility.SetDirty(definition);
            results[i] = definition;
        }

        return results;
    }

    private static void WireShopScene()
    {
        GameObject shopScroll = FindSceneObject(ShopScrollPath);
        GameObject popupObject = FindSceneObject(PopupPath);
        if (shopScroll == null || popupObject == null)
            throw new InvalidOperationException("Could not find the existing Rollerblade Shop or shared popup.");

        SwordShopController wrongController = shopScroll.GetComponent<SwordShopController>();
        if (wrongController != null)
            UnityEngine.Object.DestroyImmediate(wrongController);

        RollerbladeShopController controller = shopScroll.GetComponent<RollerbladeShopController>();
        if (controller == null)
            controller = shopScroll.AddComponent<RollerbladeShopController>();

        RollerbladeShopItem[] items = new RollerbladeShopItem[ShopSpecs.Length];
        for (int i = 0; i < ShopSpecs.Length; i++)
        {
            ShopSpec spec = ShopSpecs[i];
            Transform card = FindDescendant(shopScroll.transform, spec.CardName);
            if (card == null)
                throw new InvalidOperationException("Missing Rollerblade Shop card: " + spec.CardName);

            SwordShopItem wrongItem = card.GetComponent<SwordShopItem>();
            if (wrongItem != null)
                UnityEngine.Object.DestroyImmediate(wrongItem);

            RollerbladeShopItem item = card.GetComponent<RollerbladeShopItem>();
            if (item == null)
                item = card.gameObject.AddComponent<RollerbladeShopItem>();

            Button button = card.GetComponent<Button>();
            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
            TMP_Text costText = card.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(component => component.name == "Text_Cost");
            Transform icon = card.Find("Group_Cost/Gem");

            if (button == null || canvasGroup == null || costText == null)
                throw new InvalidOperationException("Shop card is missing an authored UI reference: " + spec.CardName);

            SerializedObject serializedItem = new SerializedObject(item);
            SetEnum(serializedItem, "rollerbladeId", (int)spec.Id);
            SetEnum(serializedItem, "purchaseType", (int)spec.PurchaseType);
            SetInt(serializedItem, "gemCost", spec.GemCost);
            SetInt(serializedItem, "cashCost", spec.CashCost);
            SetString(serializedItem, "realMoneyConfirmationPrice", "$1.99 USD");
            SetString(serializedItem, "realMoneyCardPrice", "$1.99");
            SetObject(serializedItem, "controller", controller);
            SetObject(serializedItem, "clickButton", button);
            SetObject(serializedItem, "costText", costText);
            SetObject(serializedItem, "currencyIcon", icon != null ? icon.gameObject : null);
            SetObject(serializedItem, "cardCanvasGroup", canvasGroup);
            serializedItem.ApplyModifiedPropertiesWithoutUndo();

            costText.text = spec.CardPrice;
            if (icon != null)
                icon.gameObject.SetActive(spec.PurchaseType != RollerbladePurchaseType.RealMoneyPlaceholder);

            EditorUtility.SetDirty(item);
            EditorUtility.SetDirty(costText);
            items[i] = item;
        }

        WeaponPowerPurchasePopup popup = popupObject.GetComponent<WeaponPowerPurchasePopup>();
        HomeUIBinder homeBinder = FindComponentInActiveScene<HomeUIBinder>();
        if (popup == null || homeBinder == null)
            throw new InvalidOperationException("Could not find the shared popup component or HomeUIBinder.");

        SerializedObject serializedController = new SerializedObject(controller);
        SetObjectArray(serializedController, "items", items);
        SetObject(serializedController, "purchasePopup", popup);
        SetObject(serializedController, "homeUIBinder", homeBinder);
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void WireInventoryScene(
        RuntimeAnimatorController previewController,
        AnimationClip previewTemplate,
        Dictionary<RollerbladeId, AnimationClip> clips)
    {
        GameObject inventoryScroll = FindSceneObject(InventoryScrollPath);
        GameObject previewObject = FindSceneObject(PreviewImagePath);
        GameObject equipObject = FindSceneObject(EquipButtonPath);
        if (inventoryScroll == null || previewObject == null || equipObject == null)
            throw new InvalidOperationException("Could not find the authored Rollerblade Inventory/shared preview UI.");

        RollerbladeInventoryController controller =
            inventoryScroll.GetComponent<RollerbladeInventoryController>();
        if (controller == null)
            controller = inventoryScroll.AddComponent<RollerbladeInventoryController>();

        RollerbladeInventorySlot[] slots = new RollerbladeInventorySlot[InventorySlots.Length];
        for (int i = 0; i < InventorySlots.Length; i++)
        {
            KeyValuePair<string, RollerbladeId> mapping = InventorySlots[i];
            Transform slotTransform = FindDescendant(inventoryScroll.transform, mapping.Key);
            if (slotTransform == null)
                throw new InvalidOperationException("Missing Rollerblade Inventory slot: " + mapping.Key);

            RollerbladeInventorySlot slot = slotTransform.GetComponent<RollerbladeInventorySlot>();
            if (slot == null)
                slot = slotTransform.gameObject.AddComponent<RollerbladeInventorySlot>();

            SerializedObject serializedSlot = new SerializedObject(slot);
            SetEnum(serializedSlot, "rollerbladeId", (int)mapping.Value);
            SetObject(serializedSlot, "controller", controller);
            serializedSlot.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slot);
            slots[i] = slot;
        }

        WeaponPowerPreviewPlayer previewPlayer = previewObject.GetComponent<WeaponPowerPreviewPlayer>();
        Image previewImage = previewObject.GetComponent<Image>();
        Button equipButton = equipObject.GetComponent<Button>();
        Image equipBackground = equipObject.GetComponent<Image>();
        TMP_Text equipLabel = equipObject.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
        if (previewPlayer == null || previewImage == null || equipButton == null ||
            equipBackground == null || equipLabel == null)
        {
            throw new InvalidOperationException("The shared Preview or Equip button is missing an authored component.");
        }

        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;
        EditorUtility.SetDirty(previewImage);

        SerializedObject serializedController = new SerializedObject(controller);
        SetObjectArray(serializedController, "rollerbladeSlots", slots);
        SetObject(serializedController, "previewPlayer", previewPlayer);
        SetObject(serializedController, "previewController", previewController);
        SetObject(serializedController, "previewTemplate", previewTemplate);
        SetString(serializedController, "previewStateName", "WeaponTurntable");
        SetObject(serializedController, "equipButton", equipButton);
        SetObject(serializedController, "equipButtonLabel", equipLabel);
        SetObjectArray(serializedController, "equipButtonBackgrounds", new Graphic[] { equipBackground });
        SetColor(serializedController, "equipColor", Color.white);
        SetColor(serializedController, "equippedColor", new Color(0.48f, 0.48f, 0.48f, 1f));

        SerializedProperty previewEntries = serializedController.FindProperty("previewAnimations");
        previewEntries.arraySize = DefinitionSpecs.Length;
        for (int i = 0; i < DefinitionSpecs.Length; i++)
        {
            DefinitionSpec spec = DefinitionSpecs[i];
            SerializedProperty entry = previewEntries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("rollerbladeId").enumValueIndex = (int)spec.Id;
            entry.FindPropertyRelative("animationClip").objectReferenceValue = clips[spec.Id];
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static AnimationClip BuildPreviewClip(
        string clipPath,
        string spriteSheetPath,
        bool existingRequired)
    {
        List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => ExtractTrailingNumber(sprite.name))
            .ToList();

        if (sprites.Count == 0)
            throw new InvalidOperationException("No sliced sprites found at " + spriteSheetPath + ".");

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            if (existingRequired)
                throw new InvalidOperationException("Missing Rollerblade animation template: " + clipPath);

            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        foreach (EditorCurveBinding oldBinding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            AnimationUtility.SetObjectReferenceCurve(clip, oldBinding, null);
        clip.ClearCurves();
        clip.frameRate = 12f;

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / 12f,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            string.Empty,
            typeof(Image),
            "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = false;
        settings.startTime = 0f;
        settings.stopTime = sprites.Count / 12f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void ConfigurePreviewSpriteSheets()
    {
        for (int i = 0; i < DefinitionSpecs.Length; i++)
            ConfigurePreviewSpriteSheet(DefinitionSpecs[i].SpriteSheetPath);
    }

    private static void ConfigurePreviewSpriteSheet(string spriteSheetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spriteSheetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Missing Rollerblade preview sprite sheet: " + spriteSheetPath);

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Multiple;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.mipmapEnabled = false;
        settings.alphaIsTransparency = true;
        settings.filterMode = FilterMode.Bilinear;
        importer.SetTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.maxTextureSize = PreviewMaxTextureSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spriteSheetPath);
        if (texture == null)
            throw new InvalidOperationException("Could not load Rollerblade preview texture: " + spriteSheetPath);

        if (texture.width % PreviewColumns != 0 || texture.height % PreviewRows != 0)
        {
            throw new InvalidOperationException(
                spriteSheetPath + " must divide evenly into a " +
                PreviewColumns + "x" + PreviewRows + " frame grid, but is " +
                texture.width + "x" + texture.height + ".");
        }

        int cellWidth = texture.width / PreviewColumns;
        int cellHeight = texture.height / PreviewRows;
        string spriteName = System.IO.Path.GetFileNameWithoutExtension(spriteSheetPath);

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider =
            factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            throw new InvalidOperationException(
                "Could not acquire the Sprite Editor data provider for " + spriteSheetPath + ".");
        }

        dataProvider.InitSpriteEditorDataProvider();
        Dictionary<string, UnityEditor.GUID> existingSpriteIds = dataProvider.GetSpriteRects()
            .GroupBy(rect => rect.name)
            .ToDictionary(group => group.Key, group => group.First().spriteID);
        SpriteRect[] spriteRects = new SpriteRect[PreviewFrameCount];

        for (int row = 0; row < PreviewRows; row++)
        {
            for (int column = 0; column < PreviewColumns; column++)
            {
                int index = row * PreviewColumns + column;
                string frameName =
                    spriteName + "_" + index.ToString("00", CultureInfo.InvariantCulture);
                UnityEditor.GUID spriteId;
                if (!existingSpriteIds.TryGetValue(frameName, out spriteId))
                    spriteId = UnityEditor.GUID.Generate();

                SpriteRect spriteRect = new SpriteRect
                {
                    name = frameName,
                    rect = new Rect(
                        column * cellWidth,
                        texture.height - ((row + 1) * cellHeight),
                        cellWidth,
                        cellHeight),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                    spriteID = spriteId
                };
                spriteRects[index] = spriteRect;
            }
        }
        dataProvider.SetSpriteRects(spriteRects);

        ISpriteNameFileIdDataProvider nameFileIdProvider =
            dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdProvider != null)
        {
            SpriteNameFileIdPair[] namePairs = spriteRects
                .Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID))
                .ToArray();
            nameFileIdProvider.SetNameFileIdPairs(namePairs);
        }

        dataProvider.Apply();
        importer.SaveAndReimport();

        List<Sprite> importedSprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
            .OfType<Sprite>()
            .ToList();
        if (importedSprites.Count != PreviewFrameCount ||
            importedSprites.Any(sprite =>
                !Mathf.Approximately(sprite.rect.width, cellWidth) ||
                !Mathf.Approximately(sprite.rect.height, cellHeight)))
        {
            throw new InvalidOperationException(
                spriteSheetPath + " did not import as " + PreviewFrameCount +
                " uniform " + cellWidth + "x" + cellHeight + " frames.");
        }
    }

    private static AnimatorController ConfigurePreviewController(AnimationClip template)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;
        AnimatorState state = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate != null && candidate.name == "WeaponTurntable");

        if (state == null)
        {
            state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null);
        }

        if (state == null)
            state = stateMachine.AddState("WeaponTurntable");

        state.name = "WeaponTurntable";
        state.motion = template;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static DefinitionSpec Definition(
        RollerbladeId id,
        string leftPrefab,
        string rightPrefab,
        string spriteSheet,
        string clip)
    {
        return new DefinitionSpec
        {
            Id = id,
            LeftPrefabPath = "Assets/Prefabs/Rollerblades/" + leftPrefab,
            RightPrefabPath = "Assets/Prefabs/Rollerblades/" + rightPrefab,
            SpriteSheetPath = PreviewFolder + "/" + spriteSheet,
            ClipPath = PreviewFolder + "/" + clip
        };
    }

    private static RollerbladeSideTransform CaptureTransform(Transform source)
    {
        return new RollerbladeSideTransform
        {
            localPosition = source.localPosition,
            localEulerAngles = source.localEulerAngles,
            localScale = source.localScale
        };
    }

    private static Transform EnsureSocket(Transform parent, string name, int layer)
    {
        Transform socket = parent.Find(name);
        if (socket == null)
        {
            GameObject socketObject = new GameObject(name);
            socket = socketObject.transform;
            socket.SetParent(parent, false);
        }

        socket.localPosition = Vector3.zero;
        socket.localRotation = Quaternion.identity;
        socket.localScale = Vector3.one;
        socket.gameObject.layer = layer;
        return socket;
    }

    private static GameObject FindSceneObject(string path)
    {
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        string childPath = slash < 0 ? string.Empty : path.Substring(slash + 1);

        Scene scene = SceneManager.GetActiveScene();
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == rootName);
        if (root == null)
            return null;

        if (string.IsNullOrEmpty(childPath))
            return root;

        Transform child = root.transform.Find(childPath);
        return child != null ? child.gameObject : null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindDescendant(root.GetChild(i), name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static T FindComponentInActiveScene<T>() where T : Component
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static int ExtractTrailingNumber(string value)
    {
        int index = value.Length - 1;
        while (index >= 0 && char.IsDigit(value[index]))
            index--;

        string digits = value.Substring(index + 1);
        int parsed;
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : int.MaxValue;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void SetObject(SerializedObject target, string name, UnityEngine.Object value)
    {
        SerializedProperty property = RequireProperty(target, name);
        property.objectReferenceValue = value;
    }

    private static void SetObjectArray<T>(SerializedObject target, string name, T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = RequireProperty(target, name);
        property.arraySize = values != null ? values.Length : 0;
        for (int i = 0; values != null && i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void SetEnum(SerializedObject target, string name, int value)
    {
        RequireProperty(target, name).enumValueIndex = value;
    }

    private static void SetInt(SerializedObject target, string name, int value)
    {
        RequireProperty(target, name).intValue = value;
    }

    private static void SetString(SerializedObject target, string name, string value)
    {
        RequireProperty(target, name).stringValue = value;
    }

    private static void SetColor(SerializedObject target, string name, Color value)
    {
        RequireProperty(target, name).colorValue = value;
    }

    private static SerializedProperty RequireProperty(SerializedObject target, string name)
    {
        SerializedProperty property = target.FindProperty(name);
        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing serialized property '" + name + "' on " + target.targetObject.GetType().Name + ".");
        }

        return property;
    }
}
