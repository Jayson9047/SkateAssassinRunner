using System;
using DamageNumbersPro;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit, project-owned preset authoring. Never runs automatically.</summary>
public static class ArcadeKillPopupSetup
{
    public const string RankPath = "Assets/Prefabs/UI/DN_ArcadeRank.prefab";
    public const string BloodPath = "Assets/Prefabs/UI/DN_EnemyBloodPopup.prefab";
    public const string BloodFontPath = "Assets/DamageNumbersPro/Materials/Bloody/Blood-Thick.asset";

    public static string Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        // Lifetime includes fade-in in DNP 4.51. Reserve two fully visible seconds.
        Edit(ArcadeAudioPresentationSetup.AnnouncementPath, root =>
        {
            var dn = root.GetComponent<DamageNumber>();
            float previousTotal = dn.lifetime + dn.durationFadeOut;
            dn.lifetime = dn.durationFadeIn + 2f;
            // DNP normalizes this curve over total lifetime: preserve the original
            // entrance's real-time key positions while extending only the hold.
            if (dn.enableScaleOverTime && previousTotal > 0)
            {
                var keys = dn.scaleOverTime.keys;
                float ratio = previousTotal / (dn.lifetime + dn.durationFadeOut);
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i].time > 0 && keys[i].time < 1)
                    {
                        keys[i].time *= ratio;
                        keys[i].inTangent /= ratio;
                        keys[i].outTangent /= ratio;
                    }
                }
                dn.scaleOverTime.keys = keys;
            }
        });

        if (!AssetDatabase.LoadAssetAtPath<GameObject>(RankPath))
        {
            Copy(ArcadeAudioPresentationSetup.AnnouncementPath, RankPath);
            Edit(RankPath, root =>
            {
                root.name = "DN_ArcadeRank";
                var dn = root.GetComponent<DamageNumber>();
                dn.leftText = "KILLER ASSASSIN";
                dn.durationFadeIn = 0.3f;
                dn.lifetime = 2.3f;
                dn.durationFadeOut = 0.6f;
                dn.unscaledTime = true;
                dn.enableScaleFadeIn = dn.enableCrossScaleFadeIn = dn.enableOffsetFadeIn = dn.enableShakeFadeIn = false;
                dn.enableScaleOverTime = dn.enableLerp = false;
                dn.enableScaleFadeOut = dn.enableCrossScaleFadeOut = dn.enableShakeFadeOut = false;
                dn.spamGroup = "SkateRunnerArcadeRank";
            });
        }

        if (!AssetDatabase.LoadAssetAtPath<GameObject>(BloodPath))
        {
            Copy("Assets/DamageNumbersPro/Demo/Prefabs/3D/Blood Text.prefab", BloodPath);
            Edit(BloodPath, root =>
            {
                root.name = "DN_EnemyBloodPopup";
                var dn = root.GetComponent<DamageNumber>();
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BloodFontPath);
                if (!font) throw new InvalidOperationException("DNP Blood-Thick font missing.");
                dn.SetFontMaterial(font);
                foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
                    tmp.fontSharedMaterial = font.material;
                dn.leftText = "Execution";
                dn.unscaledTime = true;
                dn.enablePooling = true;
                dn.poolSize = 12;
                dn.disableOnSceneLoad = true;
                dn.updateDelay = 0;
                dn.spamGroup = "SkateRunnerEnemyBlood";
                // Use the same world/camera behavior as the existing cash popup.
                dn.enable3DGame = true;
                dn.faceCameraView = true;
                dn.renderThroughWalls = false;
                dn.consistentScreenSize = false;
                dn.enableCombination = dn.enableDestruction = dn.enableCollision = dn.enablePush = false;
            });
        }

        Edit(ArcadeAudioPresentationSetup.UiPath, root =>
        {
            var controller = root.GetComponentInChildren<ArcadeAnnouncerPresentation>(true);
            if (!controller) throw new InvalidOperationException("Existing ArcadeAnnouncements owner missing.");
            var so = new SerializedObject(controller);
            so.FindProperty("rankAnnouncementPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RankPath).GetComponent<DamageNumber>();
            so.FindProperty("enemyBloodPopupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BloodPath).GetComponent<DamageNumber>();
            so.ApplyModifiedPropertiesWithoutUndo();
        });
        return "Configured separate rank fade/hold preset, two-second Powerslam hold, and pooled Blood-Thick world popup. Vendor assets and scene untouched.";
    }

    private static void Copy(string source, string destination)
    {
        if (!AssetDatabase.CopyAsset(source, destination)) throw new InvalidOperationException("Could not copy " + source);
    }

    private static void Edit(string path, Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
