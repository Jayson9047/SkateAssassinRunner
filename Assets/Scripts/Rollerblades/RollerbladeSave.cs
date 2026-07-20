using UnityEngine;

/// <summary>Stores only the currently equipped Rollerblade pair.</summary>
public static class RollerbladeSave
{
    public const string EquippedRollerbladeIdKey = "EquippedRollerbladeId";

    public static void Save(RollerbladeId id)
    {
        PlayerPrefs.SetInt(EquippedRollerbladeIdKey, (int)id);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out RollerbladeId id)
    {
        if (!PlayerPrefs.HasKey(EquippedRollerbladeIdKey))
        {
            id = RollerbladeId.Default;
            return false;
        }

        id = (RollerbladeId)PlayerPrefs.GetInt(
            EquippedRollerbladeIdKey,
            (int)RollerbladeId.Default);
        return true;
    }
}
