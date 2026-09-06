using System;
using System.Collections.Generic;

/// <summary>Persistent, explicit unseen-item state. Existing ownership is never inferred as new.</summary>
public static class InventoryNewItemNotifications
{
    public const string SaveKey = "Inventory.NewItemsV1";
    public static event Action Changed;

    [Serializable]
    private sealed class State
    {
        public List<int> swords = new List<int>();
        public List<int> abilities = new List<int>();
        public List<int> rollerblades = new List<int>();
    }

    private static State state;
    private static bool loaded;

    public static void RegisterSword(SwordId id) { if (id != SwordId.Katana && SwordOwnershipSave.IsValidSwordId(id)) Add(Get().swords, (int)id); }
    public static void RegisterAbility(WeaponPowerId id) { if (id != WeaponPowerId.None && WeaponPowerOwnershipSave.IsValidPowerId(id)) Add(Get().abilities, (int)id); }
    public static void RegisterRollerblade(RollerbladeId id) { if (id != RollerbladeId.Default && RollerbladeOwnershipSave.IsValidRollerbladeId(id)) Add(Get().rollerblades, (int)id); }

    public static void MarkSwordSeen(SwordId id) { Remove(Get().swords, (int)id); }
    public static void MarkAbilitySeen(WeaponPowerId id) { Remove(Get().abilities, (int)id); }
    public static void MarkRollerbladeSeen(RollerbladeId id) { Remove(Get().rollerblades, (int)id); }

    public static bool IsSwordUnseen(SwordId id) { return Get().swords.Contains((int)id); }
    public static bool IsAbilityUnseen(WeaponPowerId id) { return Get().abilities.Contains((int)id); }
    public static bool IsRollerbladeUnseen(RollerbladeId id) { return Get().rollerblades.Contains((int)id); }
    public static int SwordCount => CountOwnedSwords();
    public static int AbilityCount => CountOwnedAbilities();
    public static int RollerbladeCount => CountOwnedRollerblades();
    public static int TotalCount => SwordCount + AbilityCount + RollerbladeCount;

    /// <summary>Clears explicit unseen state without inferring anything from ownership.</summary>
    public static void ClearAll()
    {
        state = new State();
        loaded = true;
        if (ES3.KeyExists(SaveKey)) ES3.DeleteKey(SaveKey);
        Changed?.Invoke();
    }

    private static State Get()
    {
        if (loaded) return state;
        state = ES3.Load(SaveKey, new State()) ?? new State();
        loaded = true;
        if (PruneInvalid()) Save(false);
        return state;
    }

    private static bool PruneInvalid()
    {
        bool changed = false;
        changed |= state.swords.RemoveAll(x => x == (int)SwordId.Katana || !Enum.IsDefined(typeof(SwordId), x)) > 0;
        changed |= state.abilities.RemoveAll(x => x == (int)WeaponPowerId.None || !Enum.IsDefined(typeof(WeaponPowerId), x)) > 0;
        changed |= state.rollerblades.RemoveAll(x => x == (int)RollerbladeId.Default || !Enum.IsDefined(typeof(RollerbladeId), x)) > 0;
        changed |= Deduplicate(state.swords); changed |= Deduplicate(state.abilities); changed |= Deduplicate(state.rollerblades);
        return changed;
    }

    private static bool Deduplicate(List<int> values)
    {
        var seen = new HashSet<int>();
        return values.RemoveAll(x => !seen.Add(x)) > 0;
    }

    private static int CountOwnedSwords() { int count = 0; List<int> values = Get().swords; for (int i = 0; i < values.Count; i++) if (SwordOwnershipSave.IsOwned((SwordId)values[i])) count++; return count; }
    private static int CountOwnedAbilities() { int count = 0; List<int> values = Get().abilities; for (int i = 0; i < values.Count; i++) if (WeaponPowerOwnershipSave.IsOwned((WeaponPowerId)values[i])) count++; return count; }
    private static int CountOwnedRollerblades() { int count = 0; List<int> values = Get().rollerblades; for (int i = 0; i < values.Count; i++) if (RollerbladeOwnershipSave.IsOwned((RollerbladeId)values[i])) count++; return count; }

    private static void Add(List<int> values, int id)
    {
        if (values.Contains(id)) return;
        values.Add(id); Save(true);
    }

    private static void Remove(List<int> values, int id)
    {
        if (!values.Remove(id)) return;
        Save(true);
    }

    private static void Save(bool notify)
    {
        ES3.Save(SaveKey, state);
        if (notify) Changed?.Invoke();
    }
}
