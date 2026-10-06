using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Builds bounded CFXR comparison effects and matching palettes for all upgraded rollerblades.</summary>
public static class RollerbladeFxAuthoring
{
    const string Folder = "Assets/Prefabs/VFX/RollerbladeTrial";
    [MenuItem("Tools/Skate Runner/VFX/Rebuild Rollerblade FX Palettes")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(Folder + "/Textures");
        AssetDatabase.Refresh();
        var ribbon = CopyMask("cfxr stretch rectangle ray blur.png", "RibbonMask.png");
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new System.InvalidOperationException("Missing existing mobile unlit shader.");
        var coreMaterial = Material("RibbonCore", shader, ribbon, new Color(2.8f, 2.8f, 2.8f, 1f), true);
        var haloMaterial = Material("RibbonHalo", shader, ribbon, new Color(1f, 1f, 1f, 0.8f), true);
        var sparksPrefab = BuildContactPrefab(false);
        var thrusterPrefab = BuildContactPrefab(true);
        foreach (RollerbladeId id in System.Enum.GetValues(typeof(RollerbladeId)))
        {
            if (id == RollerbladeId.Default) continue;
            BuildFoot(id, coreMaterial, haloMaterial, sparksPrefab, thrusterPrefab);
        }
        const string playerPath = "Assets/Prefabs/Characters/S_01_Male.prefab";
        var player = PrefabUtility.LoadPrefabContents(playerPath);
        if (player.GetComponent<RollerbladeMovementFx>() == null) player.AddComponent<RollerbladeMovementFx>();
        PrefabUtility.SaveAsPrefabAsset(player, playerPath); PrefabUtility.UnloadPrefabContents(player);
        AssetDatabase.SaveAssets();
    }
    static void BuildFoot(RollerbladeId id, Material coreMaterial, Material haloMaterial,
        GameObject sparksPrefab, GameObject thrusterPrefab)
    {
        var root = new GameObject(id + "_FootFx");
        try
        {
            var fx = root.AddComponent<RollerbladeFootFx>();
            var core = Trail(root, "BrightRibbonCore", coreMaterial, 0.1f);
            var halo = Trail(root, "ColoredRibbonHalo", haloMaterial, 0.4f);
            var sparks = Object.Instantiate(sparksPrefab, root.transform).GetComponent<ParticleSystem>();
            var thruster = Object.Instantiate(thrusterPrefab, root.transform).GetComponent<ParticleSystem>();
            sparks.name = "RearWheel_DirectionalSparks";
            thruster.name = "RearWheel_BlueThruster";
            sparks.gameObject.SetActive(false); thruster.gameObject.SetActive(false);
            Set(fx, "ribbonCore", core); Set(fx, "ribbonHalo", halo);
            Set(fx, "directionalSparks", sparks); Set(fx, "blueThruster", thruster);
            Color coreColor, head, middle, tail;
            GetPalette(id, out coreColor, out head, out middle, out tail);
            var ribbon = halo.colorGradient;
            // The accepted Neon ribbon remains byte-for-byte equivalent in color,
            // width, fade and history; the new blue-violet palette affects sparks only.
            ribbon.colorKeys = id == RollerbladeId.NeonVelocity
                ? new[] {new GradientColorKey(new Color(0.03f, 0.7f, 1f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.3f, 1f), 0.55f),
                    new GradientColorKey(new Color(0.7f, 0.08f, 1f), 1f)}
                : new[] {new GradientColorKey(head, 0f), new GradientColorKey(middle, 0.55f),
                    new GradientColorKey(tail, 1f)};
            halo.colorGradient = ribbon;
            var coreGradient = core.colorGradient;
            coreGradient.colorKeys = new[] {new GradientColorKey(coreColor, 0f), new GradientColorKey(coreColor, 1f)};
            core.colorGradient = coreGradient;
            var sparkGradient = new Gradient();
            sparkGradient.SetKeys(new[] {new GradientColorKey(head, 0f), new GradientColorKey(middle, 0.4f),
                new GradientColorKey(tail, 1f)}, sparks.colorOverLifetime.color.gradient.alphaKeys);
            var serialized = new SerializedObject(fx);
            serialized.FindProperty("matchingSparkColors").gradientValue = sparkGradient;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + id + "_FootFx.prefab");
            var definition = AssetDatabase.LoadAssetAtPath<RollerbladeDefinition>(
                "Assets/Prefabs/Rollerblades/Definitions/Rollerblade_" + id + ".asset");
            if (definition == null) throw new System.InvalidOperationException("Missing rollerblade definition: " + id);
            definition.movementFxPrefab = prefab; EditorUtility.SetDirty(definition);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void GetPalette(RollerbladeId id, out Color core, out Color head, out Color middle, out Color tail)
    {
        switch (id)
        {
            case RollerbladeId.UrbanRush:
                core = new Color(1f, 0.10f, 0.14f);
                head = new Color(1f, 0.16f, 0.20f); middle = new Color(1f, 0.02f, 0.06f); tail = new Color(0.55f, 0.005f, 0.025f);
                break;
            case RollerbladeId.FrostbiteGlide:
                core = new Color(0.30f, 0.72f, 1f);
                head = new Color(0.56f, 0.94f, 1f); middle = new Color(0.13f, 0.66f, 1f); tail = new Color(0.02f, 0.29f, 0.90f);
                break;
            case RollerbladeId.InfernoDrift:
                core = new Color(1f, 0.34f, 0.04f);
                head = new Color(1f, 0.75f, 0.12f); middle = new Color(1f, 0.27f, 0.02f); tail = new Color(0.65f, 0.015f, 0f);
                break;
            case RollerbladeId.CelestialApex:
                core = new Color(1f, 0.65f, 0.07f);
                head = new Color(1f, 0.90f, 0.35f); middle = new Color(1f, 0.65f, 0.04f); tail = new Color(0.85f, 0.32f, 0.01f);
                break;
            case RollerbladeId.NeonVelocity:
                core = Color.white;
                head = new Color(0.30f, 0.75f, 1f); middle = new Color(0.17f, 0.32f, 1f); tail = new Color(0.70f, 0.06f, 1f);
                break;
            default: throw new System.ArgumentOutOfRangeException(nameof(id));
        }
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
        if (material != null && ribbon) return material; // Preserve the accepted ribbon's tuning.
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
        material.SetFloat("_DstBlendAlpha", (float)(ribbon ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
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
    static GameObject BuildContactPrefab(bool thruster)
    {
        const string vendor = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/";
        string source = vendor + (thruster ? "Space/CFXR4 Spaceship Thruster (Blue).prefab"
            : "Electric/CFXR2 Sparks Source Directional.prefab");
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(source);
        if (original == null) throw new System.InvalidOperationException("Missing CFXR source: " + source);
        var root = Object.Instantiate(original);
        try
        {
            root.name = thruster ? "NeonVelocity_CFXR4_BlueThruster" : "NeonVelocity_CFXR2_DirectionalSparks";
            root.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Our cached rig owns lifetime/contact. Remove vendor light animation,
            // camera shake and self-destruction, never alter the imported originals.
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            foreach (var light in root.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light.gameObject);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.LookRotation(thruster ? Vector3.left : new Vector3(-1f, 0.25f, 0f));
            root.transform.localScale = Vector3.one * (thruster ? 0.16f : 1f);
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                bool core = ps.gameObject == root;
                var main = ps.main;
                main.playOnAwake = false; main.loop = true; main.prewarm = false;
                main.useUnscaledTime = false; main.stopAction = ParticleSystemStopAction.None;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.cullingMode = ParticleSystemCullingMode.Pause;
                main.simulationSpace = thruster ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
                main.maxParticles = thruster ? (core ? 5 : 8) : 16;
                // Retain the authored animated gradient but keep overlapping
                // cores cyan-blue instead of clipping to an opaque white flash.
                if (thruster) main.startColor = new Color(0.3f, 0.62f, 1f, 0.9f);
                main.startLifetime = thruster && core ? new ParticleSystem.MinMaxCurve(0.24f)
                    : new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
                if (!thruster)
                {
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.06f);
                    main.gravityModifier = 0.12f;
                }
                else if (!core)
                {
                    main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 28f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.26f);
                }
                var emission = ps.emission; emission.enabled = true;
                emission.rateOverTime = thruster ? (core ? 18f : 24f) : 36f;
                emission.rateOverDistance = 0f;
                emission.SetBursts(thruster && core ? new[] {new ParticleSystem.Burst(0f, 1)} : new ParticleSystem.Burst[0]);
                var collision = ps.collision; collision.enabled = false;
                var trigger = ps.trigger; trigger.enabled = false;
                var lights = ps.lights; lights.enabled = false;
                var noise = ps.noise; noise.enabled = false;
                var trails = ps.trails; trails.enabled = false;
                var sub = ps.subEmitters; sub.enabled = false;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = CopyCfxMaterial(renderer.sharedMaterial,
                    thruster ? (core ? "ThrusterCore" : "ThrusterStreaks") : "DirectionalSparks");
                if (thruster && !core) { renderer.lengthScale = 6f; renderer.pivot = new Vector3(0f, 3f, 0f); }
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            return PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + root.name + ".prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static Material CopyCfxMaterial(Material source, string name)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
        else material.CopyPropertiesFromMaterial(source);
        // Retain CFXR's authored blue gradient, custom streams and core dissolve.
        // No depth-texture sampling, distortion, lighting or shadow pass.
        material.DisableKeyword("_FADING_ON");
        if (material.HasProperty("_UseSP")) material.SetFloat("_UseSP", 0f);
        material.SetShaderPassEnabled("ShadowCaster", false);
        if (material.HasProperty("_HdrMultiply")) material.SetFloat("_HdrMultiply", 2f);
        foreach (string property in material.GetTexturePropertyNames())
        {
            var texture = material.GetTexture(property);
            string original = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(original) || !original.StartsWith("Assets/")) continue;
            string target = Folder + "/Textures/" + System.IO.Path.GetFileName(original);
            if (AssetDatabase.LoadAssetAtPath<Texture>(target) == null && !AssetDatabase.CopyAsset(original, target))
                throw new System.InvalidOperationException("Could not copy " + original);
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false; importer.mipmapEnabled = false; importer.isReadable = false;
            importer.maxTextureSize = 256;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {name="Android", overridden=true, maxTextureSize=256, format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();
            material.SetTexture(property, AssetDatabase.LoadAssetAtPath<Texture>(target));
        }
        EditorUtility.SetDirty(material);
        return material;
    }
    static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
