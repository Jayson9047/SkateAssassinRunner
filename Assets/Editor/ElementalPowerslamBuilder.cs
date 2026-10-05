using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Owned, reproducible mobile Powerslam variants. Imported vendor assets remain untouched.</summary>
public static class ElementalPowerslamBuilder
{
    public const string Folder = "Assets/Prefabs/VFX/ElementalPowerslam";
    public const string EarthSource = "Assets/PixPlays/ElementalAOE/EarthAOE/Version_BuiltIn/EarthSlamSpikesAoeVFX.prefab";
    public const string EarthPrefab = Folder + "/EarthAOE_Mobile.prefab";

    [MenuItem("ELROI/VFX/Rebuild Elemental Powerslams")]
    public static void Build()
    {
        EnsureFolder(Folder + "/Materials");
        EnsureFolder(Folder + "/Earth/Textures");
        PowerslamConeTrialBuilder.Build();
        BuildCone(WeaponPowerId.Fire, new Color(1f, 0.12f, 0.008f), new Color(1f, 0.84f, 0.48f));
        BuildCone(WeaponPowerId.Ice, new Color(0.11f, 0.55f, 1f), new Color(0.8f, 0.96f, 1f));
        BuildCone(WeaponPowerId.Magic, new Color(0.64f, 0.08f, 1f), new Color(0.93f, 0.72f, 1f));
        BuildCone(WeaponPowerId.Poison, new Color(0.12f, 0.9f, 0.035f), new Color(0.79f, 1f, 0.48f));
        BuildCone(WeaponPowerId.Electricity, new Color(0.07f, 0.24f, 1f), new Color(0.48f, 0.87f, 1f));
        BuildEarth();
        BindCharacter();
        ValidateDependencies();
        Debug.Log("[Elemental Powerslam] Built five 45-particle cones and an independent mobile EarthAOE.");
    }

    public static string ConePath(WeaponPowerId id) => Folder + "/Explotion_Cone_" + id + ".prefab";

    private static void BuildCone(WeaponPowerId ability, Color middle, Color hot)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PowerslamConeTrialBuilder.TrialPrefab);
        try
        {
            root.name = "Explotion_Cone_" + ability;
            var copies = new Dictionary<Material, Material>();
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                var source = renderer.sharedMaterial;
                if (ps.name.StartsWith("Fire_cone_Decal_dark", StringComparison.Ordinal))
                {
                    renderer.sortingOrder = -10;
                    continue;
                }
                // The imported soot layer sorted over the flame mesh and dimmed its plume.
                renderer.sortingOrder = ps.name.StartsWith("Fire_cone_Decal_", StringComparison.Ordinal) ? 0 : 2;
                if (!copies.TryGetValue(source, out Material material))
                {
                    string path = Folder + "/Materials/" + ability + "_" + source.name + ".mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
                    else EditorUtility.CopySerialized(source, material);
                    material.name = ability + "_" + source.name;
                    bool ground = ps.name.StartsWith("Fire_cone_Decal_", StringComparison.Ordinal);
                    Texture mainTexture = source.GetTexture("_MainTex");
                    Texture noiseTexture = source.GetTexture("_DetailNoise");
                    material.shader = Shader.Find("ELROI/VFX/Powerslam Element Mobile");
                    if (material.shader == null) throw new InvalidOperationException("Mobile element shader has not imported.");
                    material.shaderKeywords = Array.Empty<string>();
                    material.SetTexture("_MainTex", mainTexture);
                    material.SetTexture("_NoiseTex", noiseTexture);
                    material.SetColor("_ElementColor", middle);
                    material.SetColor("_HotColor", hot);
                    material.SetFloat("_Intensity", ground ? 2.4f : 3.2f);
                    material.SetFloat("_NoiseStrength", ground ? 0.15f : 0.65f);
                    material.SetVector("_Pan", source.GetVector("_MainTexturePanning"));
                    material.SetFloat("_Cull", 0f);
                    material.renderQueue = 3000;
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssetIfDirty(material);
                    copies.Add(source, material);
                }
                renderer.sharedMaterial = material;
                // Remove the source's yellow particle tint; the ramp supplies the element.
                var main = ps.main;
                main.startColor = Color.white;
                var col = ps.colorOverLifetime;
                if (col.enabled)
                {
                    Gradient original = col.color.gradient;
                    var white = new Gradient();
                    white.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, original.alphaKeys);
                    col.color = new ParticleSystem.MinMaxGradient(white);
                }
                if (ps.name == "Fire_Flames_v2_add_soft")
                {
                    main.startSize = renderer.renderMode == ParticleSystemRenderMode.Billboard
                        ? new ParticleSystem.MinMaxCurve(1.5f, 2f) : new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, ConePath(ability));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildEarth()
    {
        // Filled in from audited source geometry below; no PixPlays scripts or Timeline are shipped.
        BuildEarthGeometry();
    }

    private static void BuildEarthGeometry()
    {
        GameObject source = PrefabUtility.LoadPrefabContents(EarthSource);
        GameObject root = new GameObject("EarthAOE_Mobile");
        try
        {
            Transform shatter = source.GetComponentsInChildren<Transform>(true).First(t => t.name == "GroundShatter (1)");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/PixPlays/ElementalAOE/EarthAOE/Animations/Animation Clip_GroundShatter_040.anim");
            // Bake the vendor's broken-ground pose once at authoring time.
            clip.SampleAnimation(shatter.gameObject, 0.4f);
            var pieces = shatter.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
            var combine = pieces.Select(f => new CombineInstance
            {
                mesh = f.sharedMesh,
                transform = source.transform.worldToLocalMatrix * f.transform.localToWorldMatrix
            }).ToArray();
            var mesh = new Mesh { name = "EarthAOE_CombinedGround", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(combine, true, true);
            mesh.RecalculateBounds();
            float radius = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z);
            float normalize = 5.5f / Mathf.Max(radius, 0.1f);
            Vector3[] vertices = mesh.vertices;
            Vector3 center = new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z);
            for (int i = 0; i < vertices.Length; i++) vertices[i] = (vertices[i] - center) * normalize;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            // Fixed faceted shading is baked into vertex colors, with no real-time lights or shadows.
            Vector3[] normals = mesh.normals;
            Color[] colors = new Color[vertices.Length];
            Vector3 lightDirection = new Vector3(-0.35f, 0.85f, -0.4f).normalized;
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color(0.24f, 0.205f, 0.18f, 1f) * (0.55f + 0.8f * Mathf.Max(0f, Vector3.Dot(normals[i], lightDirection)));
            for (int i = 0; i < colors.Length; i++) colors[i].a = 1f;
            mesh.colors = colors;
            string meshPath = Folder + "/Earth/EarthAOE_CombinedGround.asset";
            Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) { saved = mesh; AssetDatabase.CreateAsset(saved, meshPath); }
            else
            {
                // CopySerialized does not reliably persist a generated mesh's native buffers.
                // Assign buffers explicitly while retaining the saved asset's stable GUID.
                saved.Clear();
                saved.indexFormat = mesh.indexFormat;
                saved.vertices = mesh.vertices;
                saved.normals = mesh.normals;
                saved.uv = mesh.uv;
                saved.colors = mesh.colors;
                saved.triangles = mesh.triangles;
                saved.bounds = mesh.bounds;
                EditorUtility.SetDirty(saved);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            AssetDatabase.SaveAssetIfDirty(saved);
            var ground = new GameObject("Combined fractured ground");
            ground.transform.SetParent(root.transform, false);
            ground.AddComponent<MeshFilter>().sharedMesh = saved;
            var groundRenderer = ground.AddComponent<MeshRenderer>();
            groundRenderer.sharedMaterial = EarthMaterial("EarthGround", null, Color.white, false);
            CheapRenderer(groundRenderer);

            // Concise source bursts; no vendor controller, Timeline, or per-piece renderers.
            foreach (var particle in source.GetComponentsInChildren<ParticleSystem>(true))
            {
                int count = particle.name == "StoneBurst (1)" || particle.name == "Stones" ? 12
                    : particle.name == "GroundFlashLines" ? 1 : particle.name == "Flash" ? 1 : 0;
                if (count == 0) continue;
                var child = UnityEngine.Object.Instantiate(particle.gameObject, root.transform);
                child.name = particle.name;
                foreach (var script in child.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                foreach (var animator in child.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                var ps = child.GetComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = false; main.playOnAwake = false; main.duration = 0.05f;
                main.startDelay = 0f; main.startLifetime = count == 12 ? 0.65f : 0.3f;
                main.maxParticles = count;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.stopAction = ParticleSystemStopAction.None;
                main.cullingMode = ParticleSystemCullingMode.Automatic;
                var emission = ps.emission;
                emission.enabled = true; emission.rateOverTime = 0f; emission.rateOverDistance = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
                var noise = ps.noise; noise.enabled = false;
                var collision = ps.collision; collision.enabled = false;
                var lights = ps.lights; lights.enabled = false;
                var trails = ps.trails; trails.enabled = false;
                var subs = ps.subEmitters; subs.enabled = false;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                var material = renderer.sharedMaterial;
                Texture texture = material.mainTexture;
                if (texture != null && !AssetDatabase.GetAssetPath(texture).StartsWith("Assets/", StringComparison.Ordinal)) texture = null;
                if (texture == null && material.HasProperty("_BaseMap")) texture = material.GetTexture("_BaseMap");
                if (texture == null && material.HasProperty("_Mask_1")) texture = material.GetTexture("_Mask_1");
                if (texture == null && material.HasProperty("_Mask_2")) texture = material.GetTexture("_Mask_2");
                Texture copied = CopyEarthTexture(texture);
                bool rock = count == 12;
                renderer.sharedMaterials = new[] { EarthMaterial(child.name, copied,
                    rock ? new Color(0.42f, 0.34f, 0.25f) : new Color(1f, 0.72f, 0.38f), !rock) };
                if (renderer.renderMode == ParticleSystemRenderMode.Mesh && renderer.mesh != null)
                    renderer.SetMeshes(new[] { CopyEarthMesh(renderer.mesh) });
                renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>
                    { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
                CheapRenderer(renderer);
            }
            var burst = root.AddComponent<PowerslamEarthBurst>();
            var settings = new SerializedObject(burst);
            settings.FindProperty("shatteredGround").objectReferenceValue = ground.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, EarthPrefab);
            Debug.Log("[EarthAOE Mobile] Combined " + pieces.Length + " source pieces into " + saved.triangles.Length / 3
                + " triangles; 26 particle capacity; no vendor runtime dependencies.");
        }
        finally { PrefabUtility.UnloadPrefabContents(source); UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void CheapRenderer(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Material EarthMaterial(string name, Texture texture, Color color, bool transparent)
    {
        string path = Folder + "/Earth/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("ELROI/VFX/Powerslam Earth Mobile");
        if (shader == null) throw new InvalidOperationException("Mobile Earth shader has not imported.");
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
        material.SetFloat("_DstBlend", transparent ? (float)BlendMode.One : (float)BlendMode.Zero);
        material.SetFloat("_ZWrite", transparent ? 0f : 1f);
        material.SetFloat("_Cull", transparent ? 0f : 2f);
        material.renderQueue = transparent ? 3000 : 2000;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    private static Mesh CopyEarthMesh(Mesh source)
    {
        string path = Folder + "/Earth/" + source.name + ".asset";
        Mesh owned = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (owned == null) { owned = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(owned, path); }
        return owned;
    }

    private static Texture CopyEarthTexture(Texture source)
    {
        if (source == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(source))) return null;
        string path = Folder + "/Earth/Textures/" + Path.GetFileName(AssetDatabase.GetAssetPath(source));
        if (AssetDatabase.LoadAssetAtPath<Texture>(path) == null)
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path)) throw new InvalidOperationException("Texture copy failed");
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.isReadable = false; importer.maxTextureSize = 512;
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true; android.name = "Android"; android.maxTextureSize = 512;
        android.format = importer.DoesSourceTextureHaveAlpha() ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4;
        android.crunchedCompression = false;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture>(path);
    }

    private static void BindCharacter()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PowerslamConeTrialBuilder.CharacterPrefab);
        try
        {
            var detector = root.GetComponentInChildren<SwipeDownDetector>(true);
            var controller = detector.GetComponent<PowerslamConeFx>();
            if (controller == null) controller = detector.gameObject.AddComponent<PowerslamConeFx>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("enablePowerslamEffects").boolValue = true;
            serialized.FindProperty("defaultImpactPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(EarthPrefab);
            serialized.FindProperty("weaponPowerEquipper").objectReferenceValue = root.GetComponentInChildren<WeaponPowerEquipper>(true);
            var mappings = serialized.FindProperty("abilityImpacts");
            WeaponPowerId[] ids = { WeaponPowerId.Fire, WeaponPowerId.Ice, WeaponPowerId.Magic, WeaponPowerId.Poison, WeaponPowerId.Electricity };
            mappings.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var entry = mappings.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("ability").enumValueIndex = (int)ids[i];
                entry.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ConePath(ids[i]));
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var hook = new SerializedObject(detector);
            hook.FindProperty("powerSlamConeFx").objectReferenceValue = controller;
            hook.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PowerslamConeTrialBuilder.CharacterPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void ValidateDependencies()
    {
        string[] paths = { EarthPrefab, ConePath(WeaponPowerId.Fire), ConePath(WeaponPowerId.Ice),
            ConePath(WeaponPowerId.Magic), ConePath(WeaponPowerId.Poison), ConePath(WeaponPowerId.Electricity) };
        foreach (string path in paths)
            foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                if (dependency.StartsWith("Assets/PixPlays/", StringComparison.Ordinal))
                    throw new InvalidOperationException("Uncopied PixPlays dependency: " + dependency);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
