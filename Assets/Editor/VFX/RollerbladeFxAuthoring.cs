using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Builds the single Neon Velocity trial from existing lightweight assets.</summary>
public static class RollerbladeFxAuthoring
{
    const string Folder = "Assets/Prefabs/VFX/RollerbladeTrial";
    [MenuItem("Tools/Skate Runner/VFX/Rebuild Neon Velocity Rollerblade Trial")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(Folder + "/Textures");
        AssetDatabase.Refresh();
        var ribbon = CopyMask("cfxr stretch rectangle ray blur.png", "RibbonMask.png");
        var spark = CopyMask("cfxr stretch trait.png", "SparkMask.png");
        var glow = CopyMask("cfxr ember blur.png", "WheelGlowMask.png");
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new System.InvalidOperationException("Missing existing mobile unlit shader.");
        var coreMaterial = Material("RibbonCore", shader, ribbon, new Color(2.8f, 2.8f, 2.8f, 1f), true);
        var haloMaterial = Material("RibbonHalo", shader, ribbon, new Color(1f, 1f, 1f, 0.8f), true);
        var sparkMaterial = Material("FrictionSparks", shader, spark, new Color(1.8f, 1.8f, 1.8f, 1f), false);
        var glowMaterial = Material("WheelGlow", shader, glow, new Color(0.04f, 0.8f, 1f, 0.8f), false);
        var root = new GameObject("NeonVelocity_FootFx");
        var fx = root.AddComponent<RollerbladeFootFx>();
        var core = Trail(root, "WhiteHotRibbon", coreMaterial, 0.1f);
        var halo = Trail(root, "CyanRibbonHalo", haloMaterial, 0.4f);
        var neon = halo.colorGradient;
        neon.colorKeys = new[] {new GradientColorKey(new Color(0.03f, 0.7f, 1f), 0f),
            new GradientColorKey(new Color(0.2f, 0.3f, 1f), 0.55f),
            new GradientColorKey(new Color(0.7f, 0.08f, 1f), 1f)};
        halo.colorGradient = neon;
        var sparks = Particles(root, "WheelContactFrictionSparks", sparkMaterial, 24);
        var sm = sparks.main; sm.simulationSpace = ParticleSystemSimulationSpace.World;
        sm.gravityModifier = 0.3f; sm.startSpeed = 0f;
        var sr = sparks.GetComponent<ParticleSystemRenderer>(); sr.renderMode = ParticleSystemRenderMode.Stretch;
        sr.lengthScale = 1.8f; sr.velocityScale = 0.002f;
        var wheel = Particles(root, "SoftWheelGlow", glowMaterial, 1);
        var wm = wheel.main; wm.startSize = 1.0f; wm.startLifetime = 0.15f;
        wm.simulationSpace = ParticleSystemSimulationSpace.Local;
        var we = wheel.emission; we.enabled = true; we.rateOverTime = 12f;
        Set(fx, "ribbonCore", core); Set(fx, "ribbonHalo", halo);
        Set(fx, "frictionSparks", sparks); Set(fx, "wheelGlow", wheel);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/NeonVelocity_FootFx.prefab");
        Object.DestroyImmediate(root);
        var definition = AssetDatabase.LoadAssetAtPath<RollerbladeDefinition>("Assets/Prefabs/Rollerblades/Definitions/Rollerblade_NeonVelocity.asset");
        definition.movementFxPrefab = prefab; EditorUtility.SetDirty(definition);
        const string playerPath = "Assets/Prefabs/Characters/S_01_Male.prefab";
        var player = PrefabUtility.LoadPrefabContents(playerPath);
        if (player.GetComponent<RollerbladeMovementFx>() == null) player.AddComponent<RollerbladeMovementFx>();
        PrefabUtility.SaveAsPrefabAsset(player, playerPath); PrefabUtility.UnloadPrefabContents(player);
        AssetDatabase.SaveAssets();
    }
    static Texture2D CopyMask(string originalName, string ownedName)
    {
        string target = Folder + "/Textures/" + ownedName;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(target) == null)
            AssetDatabase.CopyAsset("Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/" + originalName, target);
        var importer = (TextureImporter)AssetImporter.GetAtPath(target);
        // Keep normal RGB and derive opacity from the grayscale mask for the
        // built-in unlit particle shader; do not inherit an alpha-only GPU format.
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importer.sRGBTexture = false; importer.mipmapEnabled = false; importer.isReadable = false;
        importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 256;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {name="Android", overridden=true, maxTextureSize=256, format=TextureImporterFormat.ASTC_6x6});
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(target);
    }
    static Material Material(string name, Shader shader, Texture texture, Color color, bool ribbon)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.shaderKeywords = new string[0];
        material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", color);
        material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 2f);
        material.SetFloat("_ColorMode", 0f); material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_SoftParticlesEnabled", 0f); material.SetFloat("_CameraFadingEnabled", 0f);
        material.SetFloat("_DistortionEnabled", 0f); material.SetFloat("_FlipbookBlending", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f); material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetTextureScale("_BaseMap", ribbon ? new Vector2(0f, 1f) : Vector2.one);
        material.SetTextureOffset("_BaseMap", ribbon ? new Vector2(0.5f, 0f) : Vector2.zero);
        material.renderQueue = 3000; material.SetOverrideTag("RenderType", "Transparent");
        EditorUtility.SetDirty(material); return material;
    }
    static LineRenderer Trail(GameObject root, string name, Material material, float width)
    {
        var go = new GameObject(name); go.transform.SetParent(root.transform, false);
        var trail = go.AddComponent<LineRenderer>();
        trail.sharedMaterial = material; trail.useWorldSpace = true; trail.positionCount = 0;
        trail.widthMultiplier = width;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0f));
        var gradient = new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(Color.white,1f)},
            new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(0.65f,0.55f),new GradientAlphaKey(0f,1f)});
        trail.colorGradient = gradient; trail.alignment = LineAlignment.View;
        trail.numCornerVertices = 2; trail.numCapVertices = 2; trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
        trail.lightProbeUsage = LightProbeUsage.Off; trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return trail;
    }
    static ParticleSystem Particles(GameObject root, string name, Material material, int maximum)
    {
        var go = new GameObject(name); go.transform.SetParent(root.transform, false);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = maximum;
        main.startLifetime = 0.25f; main.startSize = 0.045f; main.startSpeed = 0f; main.useUnscaledTime = false;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.enabled = false;
        var colors = ps.colorOverLifetime; colors.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(Color.white,1f)},
            new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(0.75f,0.4f),new GradientAlphaKey(0f,1f)});
        colors.color = gradient;
        var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return ps;
    }
    static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
