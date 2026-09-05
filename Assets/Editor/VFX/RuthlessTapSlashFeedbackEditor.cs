using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RuthlessTapSlashFeedback))]
public sealed class RuthlessTapSlashFeedbackEditor : Editor
{
    private bool showAdvanced;
    private int remaining;
    private double nextTrigger;
    private double interval;
    private RuthlessTapSlashFeedback testTarget;

    private static readonly string[] AdvancedFields =
    {
        "splitDist", "distortPower", "slashFade", "coreWidth", "glowSpread",
        "glowColor", "smokeColor1", "smokeColor2", "backgroundColor",
        "lightSmokeWhiteMix", "darkSmokeBlackMix",
        "smokeFade", "smokeExpand", "smokeSize1", "smokeSize2",
        "brightness", "contrast", "gamma", "hue", "saturation"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        Draw("slashVolume", "gameplayCamera", "weaponPowerEquipper", "powerPalette", "feedbackEnabled",
            "slashDuration", "peakIntensity",
            "fadeStartNormalized", "startProgress", "minimumAngle", "maximumAngle",
            "minimumConsecutiveAngleDifference", "visualImpactScale");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Final Strike", EditorStyles.boldLabel);
        Draw("finalStrikeDuration", "finalPeakIntensity", "finalVisualImpactScale",
            "finalFadeStartNormalized", "finalStartProgress", "finalCoreWidthMultiplier",
            "finalGlowMultiplier", "finalSmokeMultiplier", "finalSlashAngleOffset",
            "finalSlashTestStart", "finalSlashTestEnd");
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Visual Settings", true);
        if (showAdvanced) Draw(AdvancedFields);
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Newest tap replaces the only slash. Narrow angle ranges limit separation to half their span. Smoke alpha controls visibility, not shader workload.", MessageType.Info);
        var feedback = (RuthlessTapSlashFeedback)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !feedback.isActiveAndEnabled || !feedback.IsReady))
        {
            if (GUILayout.Button("TEST TAP — DEFAULT")) TestTap(feedback, WeaponPowerId.None);
            if (GUILayout.Button("TEST TAP — FIRE")) TestTap(feedback, WeaponPowerId.Fire);
            if (GUILayout.Button("TEST TAP — ICE")) TestTap(feedback, WeaponPowerId.Ice);
            if (GUILayout.Button("TEST TAP — ELECTRICITY")) TestTap(feedback, WeaponPowerId.Electricity);
            if (GUILayout.Button("TEST TAP — POISON")) TestTap(feedback, WeaponPowerId.Poison);
            if (GUILayout.Button("TEST TAP — MAGIC")) TestTap(feedback, WeaponPowerId.Magic);
            if (GUILayout.Button("TEST FINAL SLASH"))
            {
                CancelBurst();
                if (!feedback.TriggerFinalStrikeForEditor())
                    Debug.LogWarning("[Ruthless Tap Slash] Assign Final Slash Test Start and End before testing.", feedback);
            }
            if (GUILayout.Button("TEST RAPID 10 TAP BURST")) BeginBurst(feedback, 10, 0.08);
            if (GUILayout.Button("TEST RAPID 50 TAP BURST")) BeginBurst(feedback, 50, 0.04);
            if (GUILayout.Button("STOP IMMEDIATELY")) { CancelBurst(); feedback.StopImmediate(); }
        }
        if (remaining > 0) EditorGUILayout.LabelField("Test triggers remaining", remaining.ToString());
        if (!Application.isPlaying) EditorGUILayout.HelpBox("Tests require Play Mode, but not Phase 2. They drive visuals only: no Cash, combo or gameplay taps are awarded.", MessageType.None);
    }

    private void TestTap(RuthlessTapSlashFeedback feedback, WeaponPowerId power)
    {
        CancelBurst();
        feedback.TriggerTapSlashForEditor(power);
    }

    private void Draw(params string[] names)
    {
        foreach (string name in names) EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
    }

    private void BeginBurst(RuthlessTapSlashFeedback feedback, int count, double spacing)
    {
        CancelBurst();
        testTarget = feedback;
        remaining = count;
        interval = spacing;
        nextTrigger = EditorApplication.timeSinceStartup;
        EditorApplication.update += TickBurst;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void TickBurst()
    {
        if (!Application.isPlaying || testTarget == null || !testTarget.isActiveAndEnabled) { CancelBurst(); return; }
        if (EditorApplication.isPaused || EditorApplication.timeSinceStartup < nextTrigger) return;
        // Do not catch up queued triggers after a stall or pause.
        nextTrigger = EditorApplication.timeSinceStartup + interval;
        testTarget.TriggerSlash();
        if (--remaining == 0) CancelBurst();
        Repaint();
    }

    private void CancelBurst()
    {
        EditorApplication.update -= TickBurst;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        remaining = 0;
        testTarget = null;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) CancelBurst();
    }

    private void OnDisable() => CancelBurst();
}
