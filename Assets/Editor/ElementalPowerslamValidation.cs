using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ElementalPowerslamValidation
{
    [MenuItem("ELROI/VFX/Validate Elemental Powerslam Assets")]
    public static void Validate()
    {
        ElementalPowerslamBuilder.ValidateDependencies();
        foreach (WeaponPowerId id in new[] { WeaponPowerId.Fire, WeaponPowerId.Ice, WeaponPowerId.Magic, WeaponPowerId.Poison, WeaponPowerId.Electricity })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElementalPowerslamBuilder.ConePath(id));
            if (prefab == null) throw new InvalidOperationException("Missing cone: " + id);
            if (prefab.GetComponentsInChildren<ParticleSystem>(true).Sum(p => p.main.maxParticles) != 45)
                throw new InvalidOperationException("Unexpected particle budget: " + id);
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterial == null || ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))
                    throw new InvalidOperationException("Broken shader: " + id + "/" + renderer.name);
        }
        var earth = AssetDatabase.LoadAssetAtPath<GameObject>(ElementalPowerslamBuilder.EarthPrefab);
        if (earth.GetComponentsInChildren<Renderer>(true).Length != 5
            || earth.GetComponentsInChildren<Animator>(true).Length != 0
            || earth.GetComponentsInChildren<ParticleSystem>(true).Sum(p => p.main.maxParticles) != 26
            || earth.GetComponentInChildren<MeshFilter>(true).sharedMesh.vertexCount == 0
            || earth.GetComponentInChildren<MeshFilter>(true).sharedMesh.triangles.Length == 0)
            throw new InvalidOperationException("Earth budget or animation regression.");
        Debug.Log("[Elemental Powerslam] All six assets validated; no PixPlays runtime references.");
    }

    [MenuItem("ELROI/VFX/Render Elemental Powerslam Previews")]
    public static void RenderAll()
    {
        foreach (WeaponPowerId id in new[] { WeaponPowerId.Fire, WeaponPowerId.Ice, WeaponPowerId.Magic, WeaponPowerId.Poison, WeaponPowerId.Electricity })
            Render(ElementalPowerslamBuilder.ConePath(id), id.ToString(), 0.18f, false);
        Render(ElementalPowerslamBuilder.EarthPrefab, "Earth", 0.18f, true);
    }

    public static void Render(string path, string label, float time, bool earth)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture previous = RenderTexture.active;
        RenderTexture rt = null;
        Texture2D tex = null;
        Camera camera = null;
        try
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.localScale = Vector3.one * (earth ? 1f : 0.6f);
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                ps.Simulate(time, false, true, true);
            if (earth)
            {
                root.transform.GetChild(0).gameObject.SetActive(true);
                root.transform.GetChild(0).localPosition = Vector3.zero;
                root.transform.GetChild(0).localScale = Vector3.one;
            }
            var go = new GameObject("Powerslam preview camera");
            SceneManager.MoveGameObjectToScene(go, scene);
            camera = go.AddComponent<Camera>();
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.42f, 0.45f);
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(11f, 12f, -6f);
            camera.transform.LookAt(new Vector3(0f, 0f, earth ? 0f : 4.5f));
            camera.allowHDR = true;
            rt = new RenderTexture(960, 640, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            tex = new Texture2D(960, 640, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 640), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Captures/ElementalPowerslam");
            File.WriteAllBytes("Captures/ElementalPowerslam/" + label + ".png", tex.EncodeToPNG());
        }
        finally
        {
            if (camera != null) camera.targetTexture = null;
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
