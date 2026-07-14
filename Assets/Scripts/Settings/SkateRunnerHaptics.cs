using Lofelt.NiceVibrations;
using UnityEngine;

/// <summary>
/// Single project-owned gate for Nice Vibrations gameplay feedback.
/// </summary>
public static class SkateRunnerHaptics
{
    public static void PlayPreset(HapticPatterns.PresetType preset)
    {
        if (!GameSettingsSave.IsVibrationEnabled() || !SystemInfo.supportsVibration)
        {
            return;
        }

        HapticPatterns.PlayPreset(preset);
    }
}
