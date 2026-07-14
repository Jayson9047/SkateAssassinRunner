using System;
using System.Collections.Generic;

/// <summary>
/// Persistent Sword ownership. Equipped Sword state is stored separately by SwordSave.
/// </summary>
public static class SwordOwnershipSave
{
    public const string OwnedSwordIdsKey = "OwnedSwordIds";

    public static bool IsOwned(SwordId id)
    {
        if (id == SwordId.Katana)
            return true;

        if (!IsValidSwordId(id))
            return false;

        List<int> ownedIds = ES3.Load(OwnedSwordIdsKey, new List<int>());
        return ownedIds.Contains((int)id);
    }

    /// <summary>
    /// Grants a valid Sword once. Katana is implicitly owned and is never written.
    /// Returns false only for an invalid id.
    /// </summary>
    public static bool Grant(SwordId id)
    {
        if (!IsValidSwordId(id))
            return false;

        if (id == SwordId.Katana)
            return true;

        List<int> ownedIds = ES3.Load(OwnedSwordIdsKey, new List<int>());
        int serializedId = (int)id;

        if (!ownedIds.Contains(serializedId))
        {
            ownedIds.Add(serializedId);
            ES3.Save(OwnedSwordIdsKey, ownedIds);
        }

        return true;
    }

    public static bool IsValidSwordId(SwordId id)
    {
        return Enum.IsDefined(typeof(SwordId), id);
    }
}
