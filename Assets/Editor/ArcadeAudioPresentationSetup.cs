using System;
using DamageNumbersPro;
using IndieKit;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit authoring only. Does not run on load or enter player builds.</summary>
public static class ArcadeAudioPresentationSetup
{
    public const string AnnouncementPath = "Assets/Prefabs/UI/DN_ArcadeAnnouncement.prefab";
    public const string UiPath = "Assets/Prefabs/Characters/UICamera.prefab";
    public const string BarrelPath = "Assets/Prefabs/Props/Enemies/barrel_01_destructible.prefab";

    public static string Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(AnnouncementPath))
        {
            if (!AssetDatabase.CopyAsset("Assets/DamageNumbersPro/Demo/Prefabs/UI/Comic.prefab", AnnouncementPath))
                throw new InvalidOperationException("Could not copy the DNP Comic GUI prefab.");
            var popupRoot = PrefabUtility.LoadPrefabContents(AnnouncementPath);
            try
            {
                popupRoot.name = "DN_ArcadeAnnouncement";
                var dn = popupRoot.GetComponent<DamageNumber>();
                dn.permanent = false;
                dn.unscaledTime = true;
                dn.lifetime = 0.78f;
                dn.enableNumber = false;
                dn.enableLeftText = true;
                dn.leftText = "POWERSLAM!!!";
                dn.leftTextSettings.bold = true;
                dn.enableRightText = dn.enableTopText = dn.enableBottomText = false;
                dn.enableColorByNumber = dn.enableScaleByNumber = false;
                dn.enable3DGame = false;
                dn.durationFadeIn = 0.12f;
                dn.enableScaleFadeIn = true;
                dn.scaleFadeIn = new Vector2(0.35f, 0.35f);
                dn.enableCrossScaleFadeIn = true;
                dn.crossScaleFadeIn = new Vector2(1.25f, 0.85f);
                dn.enableOffsetFadeIn = true;
                // DNP GUI movement is converted to canvas units (x100), not pixels.
                dn.offsetFadeIn = new Vector2(0, -0.18f);
                dn.enableShakeFadeIn = false;
                dn.durationFadeOut = 0.22f;
                dn.enableOffsetFadeOut = true;
                dn.offsetFadeOut = new Vector2(0, 0.16f);
                dn.enableScaleFadeOut = true;
                dn.scaleFadeOut = new Vector2(1.08f, 1.08f);
                dn.enableCrossScaleFadeOut = dn.enableShakeFadeOut = false;
                dn.enableLerp = true;
                dn.lerpSettings.speed = 4f;
                dn.lerpSettings.minX = dn.lerpSettings.maxX = 0;
                dn.lerpSettings.minY = dn.lerpSettings.maxY = 0.35f;
                dn.lerpSettings.randomFlip = false;
                dn.enableVelocity = dn.enableShaking = dn.enableFollowing = false;
                dn.enableStartRotation = dn.enableRotateOverTime = false;
                dn.enableScaleOverTime = true;
                dn.scaleOverTime = new AnimationCurve(new Keyframe(0, 0.9f), new Keyframe(0.12f, 1.14f),
                    new Keyframe(0.26f, 1f), new Keyframe(1f, 1f));
                dn.enableCombination = dn.enableDestruction = dn.enableCollision = dn.enablePush = false;
                dn.enablePooling = true;
                dn.poolSize = 8;
                dn.disableOnSceneLoad = true;
                dn.updateDelay = 0;
                dn.spamGroup = "SkateRunnerArcadeAnnouncement";
                foreach (var tmp in popupRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    // Keep the DNP Comic font and its authored outline material.
                    tmp.fontSize = 72f;
                    tmp.enableAutoSizing = false;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    tmp.raycastTarget = false;
                    tmp.rectTransform.sizeDelta = new Vector2(1400, 180);
                }
                PrefabUtility.SaveAsPrefabAsset(popupRoot, AnnouncementPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(popupRoot); }
        }

        var ui = PrefabUtility.LoadPrefabContents(UiPath);
        try
        {
            var canvas = ui.transform.Find("Canvas");
            if (!canvas) throw new InvalidOperationException("UICamera/Canvas missing.");
            var controller = ui.GetComponentInChildren<ArcadeAnnouncerPresentation>(true);
            if (!controller)
            {
                var layer = new GameObject("ArcadeAnnouncements", typeof(RectTransform), typeof(CanvasGroup));
                layer.transform.SetParent(canvas, false);
                var rect = (RectTransform)layer.transform;
                rect.anchorMin = new Vector2(0.08f, 0.68f);
                rect.anchorMax = new Vector2(0.92f, 0.68f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0, 180);
                var group = layer.GetComponent<CanvasGroup>();
                group.interactable = group.blocksRaycasts = false;
                controller = layer.AddComponent<ArcadeAnnouncerPresentation>();
                var so = new SerializedObject(controller);
                so.FindProperty("announcementPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(AnnouncementPath).GetComponent<DamageNumber>();
                so.FindProperty("announcementAnchor").objectReferenceValue = rect;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(ui, UiPath);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(ui); }

        var barrel = PrefabUtility.LoadPrefabContents(BarrelPath);
        try
        {
            var so = new SerializedObject(barrel.GetComponent<SkateRunnerDestructibleObject>());
            if (so.FindProperty("countsAsEnemyKill").boolValue)
                throw new InvalidOperationException("Barrel unexpectedly counts as an enemy; review before proceeding.");
            so.FindProperty("audioKind").enumValueIndex = (int)DestructibleAudioKind.Barrel;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(barrel, BarrelPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(barrel); }

        const string audioPath = "Assets/Resources/SkateRunnerAudio.prefab";
        var audioRoot = PrefabUtility.LoadPrefabContents(audioPath);
        try
        {
            var so = new SerializedObject(audioRoot.GetComponent<SkateRunnerAudioManager>());
            var clip = so.FindProperty("barrelDestruction.clip");
            if (!clip.objectReferenceValue) clip.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Barrel.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(audioRoot, audioPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(audioRoot); }
        return "Configured DNP announcement prefab, UICamera presentation, barrel audio kind and optional barrel cue. Existing audio fields untouched.";
    }
}
