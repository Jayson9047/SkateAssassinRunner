using UnityEngine;

/// <summary>Stores only the currently equipped Sword.</summary>
public static class SwordSave
{
    public const string EquippedSwordIdKey = "EquippedSwordId";

    public static void Save(SwordId id)
    {
        PlayerPrefs.SetInt(EquippedSwordIdKey, (int)id);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out SwordId id)
    {
        if (!PlayerPrefs.HasKey(EquippedSwordIdKey))
        {
            id = SwordId.Katana;
            return false;
        }

        id = (SwordId)PlayerPrefs.GetInt(EquippedSwordIdKey, (int)SwordId.Katana);
        return true;
    }
}
