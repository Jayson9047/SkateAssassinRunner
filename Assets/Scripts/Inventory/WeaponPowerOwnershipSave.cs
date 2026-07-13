using System;
using System.Collections.Generic;

/// <summary>
/// Persistent ownership for Weapon Powers. This is intentionally separate from
/// WeaponPowerSave, which stores only the currently equipped power.
/// </summary>
public static class WeaponPowerOwnershipSave
{
    public const string OwnedWeaponPowerIdsKey = "OwnedWeaponPowerIds";

    public static bool IsOwned(WeaponPowerId id)
    {
        if (id == WeaponPowerId.None)
            return true;

        if (!IsValidPowerId(id))
            return false;

        List<int> ownedIds = ES3.Load(OwnedWeaponPowerIdsKey, new List<int>());
        return ownedIds.Contains((int)id);
    }

    /// <summary>
    /// Grants a valid power once. Returns false only when the id is invalid.
    /// </summary>
    public static bool Grant(WeaponPowerId id)
    {
        if (!IsValidPowerId(id))
            return false;

        if (id == WeaponPowerId.None)
            return true;

        List<int> ownedIds = ES3.Load(OwnedWeaponPowerIdsKey, new List<int>());
        int serializedId = (int)id;

        if (!ownedIds.Contains(serializedId))
        {
            ownedIds.Add(serializedId);
            ES3.Save(OwnedWeaponPowerIdsKey, ownedIds);
        }

        return true;
    }

    public static bool IsValidPowerId(WeaponPowerId id)
    {
        return Enum.IsDefined(typeof(WeaponPowerId), id);
    }
}
