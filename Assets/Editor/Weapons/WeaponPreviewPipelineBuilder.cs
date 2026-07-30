using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rebuilds only the Sword Inventory preview assets. Existing scene mappings
/// continue referencing the same clips, so no Shop or gameplay wiring changes.
/// </summary>
public static class WeaponPreviewPipelineBuilder
{
    private const string PreviewFolder = "Assets/Prefabs/PreviewSprites/WeaponSprites";
    private const string TemplatePath = PreviewFolder + "/WeaponTurntable.anim";
    private const string ControllerPath =
        PreviewFolder + "/WeaponSpriteAnimationController.controller";
    private const int PreviewColumns = 6;
    private const int PreviewRows = 4;
    private const int PreviewFrameCount = PreviewColumns * PreviewRows;
    private const int PreviewMaxTextureSize = 4096;

    private sealed class PreviewSpec
    {
        public SwordId Id;
        public string SpriteSheetPath;
        public string ClipPath;
    }

    private static readonly PreviewSpec[] PreviewSpecs =
    {
        Preview(
            SwordId.Katana,
            "KatanaSprite.png",
            "KatanaTurntable.anim"),
        Preview(
            SwordId.Bloodreaver,
            "BloodreaverSprite.png",
            "BloodreaverTurntable.anim"),
        Preview(
            SwordId.Emberguard,
            "EmberguardSprite.png",
            "EmberguardTurntable.anim"),
        Preview(
            SwordId.GlacierCipher,
            "GlacierCipherSprite.png",
            "GlacierCipherTurntable.anim"),
        Preview(
            SwordId.Gravebreaker,
            "GravebreakerSprite.png",
            "GravebreakerTurntable.anim"),
        Preview(
            SwordId.HellForge,
            "HellForgeSprite.png",
            "HellForgeTurntable.anim"),
        Preview(
            SwordId.Sunspire,
            "SunspireSprite.png",
            "SunspireTurntable.anim"),
        Preview(
            SwordId.Wyrmshade,
            "WyrmshadeSprite.png",
            "WyrmshadeTurntable.anim")
    };

    [MenuItem("Tools/Skate Runner/Rebuild Weapon Inventory Previews")]
    public static void RebuildInventoryPreviews()
    {
        for (int i = 0; i < PreviewSpecs.Length; i++)
            ConfigurePreviewSpriteSheet(PreviewSpecs[i].SpriteSheetPath);

        AnimationClip template = BuildPreviewClip(
            TemplatePath,
            PreviewSpecs[0].SpriteSheetPath,
            true);
        ConfigurePreviewController(template);

        for (int i = 0; i < PreviewSpecs.Length; i++)
        {
            PreviewSpec spec = PreviewSpecs[i];
            BuildPreviewClip(spec.ClipPath, spec.SpriteSheetPath, false);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "Rebuilt eight Weapon Inventory previews from uniform 24-frame sprite sheets at 12 FPS.");
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

        if (sprites.Count != PreviewFrameCount)
        {
            throw new InvalidOperationException(
                spriteSheetPath + " must contain exactly " + PreviewFrameCount +
                " sliced preview sprites.");
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            if (existingRequired)
                throw new InvalidOperationException("Missing Weapon animation template: " + clipPath);

            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        foreach (EditorCurveBinding oldBinding in
                 AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, oldBinding, null);
        }

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

    private static void ConfigurePreviewSpriteSheet(string spriteSheetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spriteSheetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Missing Weapon preview sprite sheet: " + spriteSheetPath);

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
            throw new InvalidOperationException("Could not load Weapon preview texture: " + spriteSheetPath);

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

                spriteRects[index] = new SpriteRect
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

    private static void ConfigurePreviewController(AnimationClip template)
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;
        AnimatorState state = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate =>
                candidate != null && candidate.name == "WeaponTurntable");

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
    }

    private static int ExtractTrailingNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            return int.MaxValue;

        int index = value.Length - 1;
        while (index >= 0 && char.IsDigit(value[index]))
            index--;

        int number;
        return int.TryParse(
            value.Substring(index + 1),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out number)
            ? number
            : int.MaxValue;
    }

    private static PreviewSpec Preview(SwordId id, string spriteSheet, string clip)
    {
        return new PreviewSpec
        {
            Id = id,
            SpriteSheetPath = PreviewFolder + "/" + spriteSheet,
            ClipPath = PreviewFolder + "/" + clip
        };
    }
}
