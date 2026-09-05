using Lofelt.NiceVibrations;
using UnityEngine;

/// <summary>
/// Single project-owned gate for Nice Vibrations gameplay feedback.
/// </summary>
public static class SkateRunnerHaptics
{
    public static bool CanPlay => GameSettingsSave.IsVibrationEnabled() && SystemInfo.supportsVibration;

    public static void PlayPreset(HapticPatterns.PresetType preset)
    {
        if (!CanPlay)
        {
            return;
        }

        HapticPatterns.PlayPreset(preset);
    }
}
