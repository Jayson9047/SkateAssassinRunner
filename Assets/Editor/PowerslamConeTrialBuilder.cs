using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Creates owned mobile copies; never edits the imported Piloto assets.</summary>
public static class PowerslamConeTrialBuilder
{
    public const string SourcePrefab = "Assets/Piloto Studio/Super Realistic FX Bundle/ARPG_Realistic Essentials Fire/Prefabs/AoE/Explotion_Cone.prefab";
    public const string TrialFolder = "Assets/Prefabs/VFX/PowerslamConeTrial";
    public const string TrialPrefab = TrialFolder + "/Explotion_Cone_Mobile.prefab";
    public const string CharacterPrefab = "Assets/Prefabs/Characters/S_01_Male.prefab";

    [MenuItem("ELROI/VFX/Rebuild Powerslam Cone Trial")]
    public static void Build()
    {
        EnsureFolder(TrialFolder + "/Materials");
        EnsureFolder(TrialFolder + "/Textures");
        GameObject root = PrefabUtility.LoadPrefabContents(SourcePrefab);
        try
        {
            root.name = "Explotion_Cone_Mobile";
            var materials = new Dictionary<Material, Material>();
            var textures = new Dictionary<Texture, Texture>();
            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                // Keep the hero mesh, a short flame burst, ground flames, and two ground quads.
                bool keep = ps.name == "Fire_Explosion_cone_add_soft"
                    || ps.name.StartsWith("Fire_cone_Decal_", StringComparison.Ordinal)
                    || ps.name == "Fire_Flames_v2_add_soft";
                if (!keep)
                {
                    if (ps.gameObject == root)
                    {
                        UnityEngine.Object.DestroyImmediate(ps.GetComponent<ParticleSystemRenderer>());
                        UnityEngine.Object.DestroyImmediate(ps);
                    }
                    else UnityEngine.Object.DestroyImmediate(ps.gameObject);
                    continue;
                }

                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
                main.duration = 0.05f;
                main.cullingMode = ParticleSystemCullingMode.Automatic;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.stopAction = ParticleSystemStopAction.None;
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.rateOverDistance = 0;
                var noise = ps.noise;
                noise.enabled = false;
                var collision = ps.collision;
                collision.enabled = false;
                var trails = ps.trails;
                trails.enabled = false;
                var lights = ps.lights;
                lights.enabled = false;

                int count;
                if (ps.name == "Fire_Flames_v2_add_soft"
                    && ps.GetComponent<ParticleSystemRenderer>().renderMode == ParticleSystemRenderMode.Billboard)
                {
                    count = 18;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.65f);
                    main.startSize = new ParticleSystem.MinMaxCurve(1.05f, 1.5f);
                }
                else if (ps.name == "Fire_Flames_v2_add_soft")
                {
                    count = 24;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.1f);
                    main.startDelay = 0.08f;
                }
                else if (ps.name.StartsWith("Fire_cone_Decal_", StringComparison.Ordinal))
                {
                    count = 1;
                    main.startLifetime = 1.45f;
                }
                else
                {
                    count = 1;
                    main.startLifetime = 0.6f;
                }
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
                main.maxParticles = count;

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.sharedMaterial = CopyMaterial(renderer.sharedMaterial, materials, textures);
            }
            PrefabUtility.SaveAsPrefabAsset(root, TrialPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        Debug.Log("[Powerslam Cone Trial] Built 5 emitting layers / 45 particle capacity. Original assets preserved.");
    }

    private static Material CopyMaterial(Material source, Dictionary<Material, Material> materials,
        Dictionary<Texture, Texture> textures)
    {
        if (materials.TryGetValue(source, out Material result)) return result;
        string path = TrialFolder + "/Materials/" + source.name + ".mat";
        result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result == null)
        {
            result = new Material(source);
            AssetDatabase.CreateAsset(result, path);
        }
        else EditorUtility.CopySerialized(source, result);
        result.name = source.name;
        foreach (string property in source.GetTexturePropertyNames())
        {
            Texture texture = source.GetTexture(property);
            if (texture != null)
                result.SetTexture(property, CopyTexture(texture, textures));
        }
        // The vendor shader is already unlit. Remove its depth-sampling soft-particle branch.
        if (result.HasProperty("_USESOFTALPHA")) result.SetFloat("_USESOFTALPHA", 0);
        result.DisableKeyword("_USESOFTALPHA_ON");
        result.DisableKeyword("_USESOFTALPHA");
        EditorUtility.SetDirty(result);
        AssetDatabase.SaveAssetIfDirty(result);
        materials[source] = result;
        return result;
    }

    private static Texture CopyTexture(Texture source, Dictionary<Texture, Texture> textures)
    {
        if (textures.TryGetValue(source, out Texture result)) return result;
        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(sourcePath)) return source;
        string path = TrialFolder + "/Textures/" + Path.GetFileName(sourcePath);
        if (AssetDatabase.LoadAssetAtPath<Texture>(path) == null && !AssetDatabase.CopyAsset(sourcePath, path))
            throw new InvalidOperationException("Could not copy texture: " + sourcePath);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            // Keep pattern definition at 1024; shared noise/masks retain at most 512.
            int maxSize = source.width > 1024 ? 1024 : Mathf.NextPowerOfTwo(Mathf.Max(source.width, source.height));
            string filename = Path.GetFileName(sourcePath);
            if (filename.IndexOf("Noise", StringComparison.OrdinalIgnoreCase) >= 0
                || filename == "2x2_tiler_mask.png") maxSize = Mathf.Min(maxSize, 256);
            importer.maxTextureSize = maxSize;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = maxSize;
            android.format = importer.DoesSourceTextureHaveAlpha()
                ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4;
            android.textureCompression = TextureImporterCompression.Compressed;
            android.compressionQuality = 50;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
        result = AssetDatabase.LoadAssetAtPath<Texture>(path);
        textures[source] = result;
        return result;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
