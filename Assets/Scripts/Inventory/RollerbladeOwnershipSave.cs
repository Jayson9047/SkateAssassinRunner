using System;
using System.Collections.Generic;

/// <summary>
/// Persistent Rollerblade ownership. Equipped state is stored separately by RollerbladeSave.
/// </summary>
public static class RollerbladeOwnershipSave
{
    public const string OwnedRollerbladeIdsKey = "OwnedRollerbladeIds";

    public static bool IsOwned(RollerbladeId id)
    {
        if (id == RollerbladeId.Default)
            return true;

        if (!IsValidRollerbladeId(id))
            return false;

        List<int> ownedIds = ES3.Load(OwnedRollerbladeIdsKey, new List<int>());
        return ownedIds.Contains((int)id);
    }

    /// <summary>
    /// Grants a valid pair once. Default is implicitly owned and is never written.
    /// Returns false only for an invalid id.
    /// </summary>
    public static bool Grant(RollerbladeId id)
    {
        if (!IsValidRollerbladeId(id))
            return false;

        if (id == RollerbladeId.Default)
            return true;

        List<int> ownedIds = ES3.Load(OwnedRollerbladeIdsKey, new List<int>());
        int serializedId = (int)id;

        if (!ownedIds.Contains(serializedId))
        {
            ownedIds.Add(serializedId);
            ES3.Save(OwnedRollerbladeIdsKey, ownedIds);
        }

        return true;
    }

    public static bool IsValidRollerbladeId(RollerbladeId id)
    {
        return Enum.IsDefined(typeof(RollerbladeId), id);
    }
}
