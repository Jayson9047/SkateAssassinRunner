using System;
using IndieKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Rebuilds only project-owned Enemy 1 presentation assets; never edits purchased prefabs.</summary>
public static class Enemy1SliceAuthoring
{
    const string Legacy = "Assets/Prefabs/Characters/Human Enemies/Enemy1_Sliced.prefab";
    const string Body = "Assets/Prefabs/Characters/Human Enemies/Enemy1_Sliced_Stylized.prefab";
    const string Fx = "Assets/Prefabs/ParticleEffects/";
    const string Graphics = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/";

    [MenuItem("Tools/Skate Runner/Enemy 1/Rebuild Stylized Slice Assets")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Build in Edit mode.");
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        GameObject body = null, contact = null, ground = null, pool = null;
        try
        {
            body = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Legacy), preview);
            PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            body.name = "Enemy1_Sliced_Stylized";
            body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            // Only this private copy loses physics, scrolling, and automatic destruction.
            foreach (var component in body.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in body.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in body.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(component);
            var presentation = body.AddComponent<Enemy1SlicePresentation>();
            presentation.upper = body.transform.Find("UpperBody");
            presentation.lower = body.transform.Find("LowerBody");
            presentation.blood = body.GetComponentInChildren<ParticleSystem>(true);
            presentation.blood.name = "Directional Cut Blood";
            presentation.blood.transform.localPosition = presentation.seam;
            var systems = body.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                BaseParticle(ps, (short)(i == 0 ? 6 : 4), 0.23f);
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.24f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(i == 0 ? 2.8f : 4f, i == 0 ? 4.2f : 5.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(i == 0 ? 0.09f : 0.055f, i == 0 ? 0.13f : 0.08f);
                main.startColor = new Color(0.82f, 0.018f, 0.025f, 1f);
                main.gravityModifier = 0.8f;
                var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 18f; shape.radius = 0.045f; shape.radiusThickness = 1f;
                var velocity = ps.velocityOverLifetime; velocity.enabled = false;
                var noise = ps.noise; noise.enabled = false;
                var size = ps.sizeOverLifetime; size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.35f));
                var color = ps.colorOverLifetime; color.enabled = true; color.color = Fade(0.7f);
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = MobileMaterial(Graphics + "cfxr proc glow crisp ab.mat", Fx + "Enemy1_CutBlood.mat");
                renderer.maxParticleSize = 0.035f;
                if (i > 0) { ps.transform.localRotation = Quaternion.identity; renderer.lengthScale = 1.5f; renderer.velocityScale = 0.08f; }
            }
            PrefabUtility.SaveAsPrefabAsset(body, Body);

            // Reuse a single CFXR streak's texture/material, not the 11-system text/sparks prefab.
            contact = new GameObject("Enemy1_CutContact", typeof(ParticleSystem));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(contact, preview);
            var cp = contact.GetComponent<ParticleSystem>(); BaseParticle(cp, 1, 0.075f);
            var cm = cp.main; cm.startSpeed = 0f; cm.startSize3D = true;
            cm.startSizeX = 0.68f; cm.startSizeY = 0.065f; cm.startSizeZ = 1f;
            cm.startRotation = 0.30f; cm.startColor = new Color(1f, 0.88f, 0.74f, 0.8f);
            var cr = cp.GetComponent<ParticleSystemRenderer>();
            cr.sharedMaterial = MobileMaterial(Graphics + "cfxr stretch triangle to diamond add.mat", Fx + "Enemy1_CutContact.mat");
            cr.renderMode = ParticleSystemRenderMode.Billboard; cr.maxParticleSize = 0.045f;
            var cc = cp.colorOverLifetime; cc.enabled = true; cc.color = Fade(0.15f);
            PrefabUtility.SaveAsPrefabAsset(contact, Fx + "Enemy1_CutContact.prefab");

            // One unlit alpha sample, without dissolve, soft particles, lighting, or projected decals.
            Material bloodMark = AssetDatabase.LoadAssetAtPath<Material>(Fx + "Enemy1_GroundBlood.mat");
            if (!bloodMark)
            {
                bloodMark = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(bloodMark, Fx + "Enemy1_GroundBlood.mat");
            }
            bloodMark.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Graphics + "cfxr puddle.png"));
            bloodMark.SetColor("_BaseColor", Color.white);
            bloodMark.SetFloat("_Surface", 1f); bloodMark.SetFloat("_Blend", 0f);
            bloodMark.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            bloodMark.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            bloodMark.SetFloat("_ZWrite", 0f); bloodMark.SetFloat("_Cull", 0f);
            bloodMark.SetFloat("_SoftParticlesEnabled", 0f); bloodMark.SetFloat("_CameraFadingEnabled", 0f);
            bloodMark.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); bloodMark.renderQueue = 3000;
            bloodMark.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(bloodMark);
            ground = new GameObject("Enemy1_GroundBlood", typeof(ParticleSystem));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ground, preview);
            var gp = ground.GetComponent<ParticleSystem>(); BaseParticle(gp, 1, 1f);
            var gm = gp.main; gm.startSize = 0.48f; gm.startSpeed = 0f;
            gm.startColor = new Color(0.55f, 0.008f, 0.015f, 0.85f);
            var gr = gp.GetComponent<ParticleSystemRenderer>(); gr.sharedMaterial = bloodMark;
            gr.renderMode = ParticleSystemRenderMode.Billboard; gr.alignment = ParticleSystemRenderSpace.Local;
            gr.maxParticleSize = 0.06f;
            var gc = gp.colorOverLifetime; gc.enabled = true; gc.color = Fade(0.65f);
            PrefabUtility.SaveAsPrefabAsset(ground, Fx + "Enemy1_GroundBlood.prefab");

            pool = new GameObject("Enemy1SlicePool");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pool, preview);
            var config = pool.AddComponent<Enemy1SlicePool>();
            config.legacyDebrisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Legacy);
            config.bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Body).GetComponent<Enemy1SlicePresentation>();
            config.contactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fx + "Enemy1_CutContact.prefab").GetComponent<ParticleSystem>();
            config.groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fx + "Enemy1_GroundBlood.prefab").GetComponent<ParticleSystem>();
            config.groundMask = LayerMask.GetMask("Ground");
            PrefabUtility.SaveAsPrefabAsset(pool, "Assets/Resources/Enemy1SlicePool.prefab");
            AssetDatabase.SaveAssets();
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
    }

    static Material MobileMaterial(string source, string destination)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(destination);
        if (!material)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>(source));
            AssetDatabase.CreateAsset(material, destination);
        }
        // Private copies avoid a mobile depth-texture dependency and leave vendor settings intact.
        material.DisableKeyword("_FADING_ON");
        material.DisableKeyword("_CFXR_DITHERED_SHADOWS_ON");
        if (material.HasProperty("_UseSP")) material.SetFloat("_UseSP", 0f);
        material.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void BaseParticle(ParticleSystem ps, short count, float lifetime)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false; main.playOnAwake = false; main.prewarm = false;
        main.duration = lifetime; main.startLifetime = lifetime; main.startDelay = 0f;
        main.maxParticles = count; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.useUnscaledTime = false; main.stopAction = ParticleSystemStopAction.None;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = ps.emission; emission.enabled = true;
        emission.rateOverTime = 0f; emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        var shape = ps.shape; shape.enabled = false;
        var collision = ps.collision; collision.enabled = false;
        var trails = ps.trails; trails.enabled = false;
        var lights = ps.lights; lights.enabled = false;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    static Gradient Fade(float hold)
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, hold), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }
}
