using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns Sword selection, shared preview playback, and the shared Equip button while Swords is active.</summary>
[DisallowMultipleComponent]
public sealed class SwordInventoryController : MonoBehaviour
{
    [Serializable]
    private sealed class PreviewEntry
    {
        public SwordId swordId;
        public AnimationClip animationClip;
    }

    [Header("Sword Cards")]
    [SerializeField] private SwordInventorySlot[] swordSlots;

    [Header("Shared Preview")]
    [SerializeField] private WeaponPowerPreviewPlayer previewPlayer;
    [SerializeField] private RuntimeAnimatorController previewController;
    [SerializeField] private AnimationClip previewTemplate;
    [SerializeField] private string previewStateName = "WeaponTurntable";
    [SerializeField] private PreviewEntry[] previewAnimations;

    [Header("Shared Equip Button")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonLabel;
    [SerializeField] private Graphic[] equipButtonBackgrounds;
    [SerializeField] private Color equipColor = Color.white;
    [SerializeField] private Color equippedColor = new Color(0.48f, 0.48f, 0.48f, 1f);

    private readonly Dictionary<SwordId, SwordInventorySlot> slotsById =
        new Dictionary<SwordId, SwordInventorySlot>();

    private readonly Dictionary<SwordId, AnimationClip> previewsById =
        new Dictionary<SwordId, AnimationClip>();

    private SwordId selectedSwordId = SwordId.Katana;
    private SwordId equippedSwordId = SwordId.Katana;
    private bool mapsBuilt;

    public SwordId SelectedSwordId => selectedSwordId;
    public SwordId EquippedSwordId => equippedSwordId;

    private void OnEnable()
    {
        BuildMaps();
        RefreshAvailability();

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(EquipSelectedSword);
            equipButton.onClick.AddListener(EquipSelectedSword);
        }

        if (previewPlayer != null)
            previewPlayer.ConfigurePlayback(previewController, previewTemplate, previewStateName);

        SwordId savedId;
        bool hasSavedSword = SwordSave.TryLoad(out savedId);
        bool savedSwordIsUsable = hasSavedSword && IsSwordUsable(savedId);

        equippedSwordId = savedSwordIsUsable ? savedId : SwordId.Katana;

        if (!hasSavedSword || !savedSwordIsUsable)
            SwordSave.Save(SwordId.Katana);

        SelectSwordInternal(equippedSwordId, true);
    }

    private void OnDisable()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(EquipSelectedSword);
    }

    public void SelectSword(SwordId id)
    {
        SelectSwordInternal(id, false);
    }

    public void RefreshAvailability()
    {
        BuildMaps();

        // Future Shop/ownership integration:
        // Query the ownership backend by SwordId here, then call slot.SetAvailable(isOwned).
        foreach (KeyValuePair<SwordId, SwordInventorySlot> pair in slotsById)
            pair.Value.SetAvailable(SwordOwnershipSave.IsOwned(pair.Key));
    }

    public void SetSwordAvailable(SwordId id, bool available)
    {
        BuildMaps();

        SwordInventorySlot slot;
        if (!slotsById.TryGetValue(id, out slot) || slot == null)
            return;

        slot.SetAvailable(id == SwordId.Katana ||
                          (available && SwordOwnershipSave.IsOwned(id)));

        if (!slot.IsAvailable && selectedSwordId == id)
            SelectSwordInternal(IsSwordUsable(equippedSwordId) ? equippedSwordId : SwordId.Katana, true);
    }

    private void SelectSwordInternal(SwordId id, bool allowFallback)
    {
        BuildMaps();

        if (!IsSwordUsable(id))
        {
            if (!allowFallback || id == SwordId.Katana)
                return;

            id = SwordId.Katana;
            if (!IsSwordUsable(id))
                return;
        }

        selectedSwordId = id;

        AnimationClip clip;
        if (previewPlayer != null && previewsById.TryGetValue(id, out clip))
            previewPlayer.Play(clip);

        UpdateEquipButtonState();
    }

    private void EquipSelectedSword()
    {
        if (!IsSwordUsable(selectedSwordId))
            return;

        SwordSave.Save(selectedSwordId);
        equippedSwordId = selectedSwordId;
        UpdateEquipButtonState();
    }

    private void UpdateEquipButtonState()
    {
        bool isEquipped = selectedSwordId == equippedSwordId;

        if (equipButtonLabel != null)
            equipButtonLabel.text = isEquipped ? "EQUIPPED" : "EQUIP";

        Color targetColor = isEquipped ? equippedColor : equipColor;
        if (equipButtonBackgrounds != null)
        {
            for (int i = 0; i < equipButtonBackgrounds.Length; i++)
            {
                if (equipButtonBackgrounds[i] != null)
                    equipButtonBackgrounds[i].color = targetColor;
            }
        }

        if (equipButton != null)
            equipButton.interactable = !isEquipped;
    }

    private bool IsSwordUsable(SwordId id)
    {
        if (!SwordOwnershipSave.IsValidSwordId(id) || !SwordOwnershipSave.IsOwned(id))
            return false;

        SwordInventorySlot slot;
        return slotsById.TryGetValue(id, out slot) &&
               slot != null &&
               slot.IsAvailable &&
               previewsById.ContainsKey(id);
    }

    private void BuildMaps()
    {
        if (mapsBuilt)
            return;

        mapsBuilt = true;
        slotsById.Clear();
        previewsById.Clear();

        if (swordSlots != null)
        {
            for (int i = 0; i < swordSlots.Length; i++)
            {
                SwordInventorySlot slot = swordSlots[i];
                if (slot == null)
                    continue;

                if (slotsById.ContainsKey(slot.SwordId))
                    Debug.LogWarning("Duplicate Sword Inventory mapping for " + slot.SwordId + ".", slot);

                slotsById[slot.SwordId] = slot;
            }
        }

        if (previewAnimations != null)
        {
            for (int i = 0; i < previewAnimations.Length; i++)
            {
                PreviewEntry entry = previewAnimations[i];
                if (entry == null || entry.animationClip == null)
                    continue;

                if (previewsById.ContainsKey(entry.swordId))
                    Debug.LogWarning("Duplicate Sword preview mapping for " + entry.swordId + ".", this);

                previewsById[entry.swordId] = entry.animationClip;
            }
        }

        if (previewPlayer == null || previewController == null || previewTemplate == null ||
            equipButton == null || equipButtonLabel == null)
        {
            Debug.LogWarning("Sword Inventory UI has one or more missing serialized references.", this);
        }

        if (!previewsById.ContainsKey(SwordId.Katana))
            Debug.LogWarning("Sword Inventory requires a configured Katana preview.", this);
    }
}
