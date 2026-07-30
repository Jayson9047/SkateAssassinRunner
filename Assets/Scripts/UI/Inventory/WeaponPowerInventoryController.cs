using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns only the Start Screen Weapon Power inventory workflow:
/// card selection -> animated preview -> save selected power on Equip.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponPowerInventoryController : MonoBehaviour
{
    [Serializable]
    private sealed class PreviewEntry
    {
        public WeaponPowerId powerId;
        public AnimationClip animationClip;
    }

    [Header("Power Cards")]
    [SerializeField] private WeaponPowerInventorySlot[] powerSlots;

    [Header("Preview")]
    [SerializeField] private WeaponPowerPreviewPlayer previewPlayer;
    [SerializeField] private PreviewEntry[] previewAnimations;

    [Header("Existing Equip Button")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonLabel;
    [SerializeField] private Graphic[] equipButtonBackgrounds;
    [SerializeField] private Color equipColor = Color.white;
    [SerializeField] private Color equippedColor = new Color(0.48f, 0.48f, 0.48f, 1f);

    private readonly Dictionary<WeaponPowerId, WeaponPowerInventorySlot> slotsById =
        new Dictionary<WeaponPowerId, WeaponPowerInventorySlot>();

    private readonly Dictionary<WeaponPowerId, AnimationClip> previewsById =
        new Dictionary<WeaponPowerId, AnimationClip>();

    private WeaponPowerId selectedPowerId = WeaponPowerId.None;
    private WeaponPowerId equippedPowerId = WeaponPowerId.None;
    private bool mapsBuilt;

    public WeaponPowerId SelectedPowerId => selectedPowerId;
    public WeaponPowerId EquippedPowerId => equippedPowerId;

    private void OnEnable()
    {
        BuildMaps();
        RefreshAvailability();

        if (previewPlayer != null)
            previewPlayer.RestoreDefaultPlayback();

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(EquipSelectedPower);
            equipButton.onClick.AddListener(EquipSelectedPower);
        }

        WeaponPowerId savedId;
        bool hasSavedPower = WeaponPowerSave.TryLoad(out savedId);
        bool savedPowerIsUsable = hasSavedPower &&
                                  WeaponPowerOwnershipSave.IsOwned(savedId) &&
                                  HasConfiguredPreview(savedId) &&
                                  IsPowerAvailable(savedId);

        equippedPowerId = savedPowerIsUsable ? savedId : WeaponPowerId.None;

        if (hasSavedPower && !savedPowerIsUsable)
            WeaponPowerSave.Save(WeaponPowerId.None);

        RefreshEquippedHighlights();
        SelectPowerInternal(equippedPowerId, true);
    }

    private void OnDisable()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(EquipSelectedPower);
    }

    private void BuildMaps()
    {
        if (mapsBuilt)
            return;

        mapsBuilt = true;
        slotsById.Clear();
        previewsById.Clear();

        if (powerSlots != null)
        {
            for (int i = 0; i < powerSlots.Length; i++)
            {
                WeaponPowerInventorySlot slot = powerSlots[i];
                if (slot == null)
                    continue;

                if (slotsById.ContainsKey(slot.PowerId))
                    Debug.LogWarning("Duplicate Weapon Power slot mapping for " + slot.PowerId + ".", slot);

                slotsById[slot.PowerId] = slot;
            }
        }

        if (previewAnimations != null)
        {
            for (int i = 0; i < previewAnimations.Length; i++)
            {
                PreviewEntry entry = previewAnimations[i];
                if (entry == null || entry.animationClip == null)
                    continue;

                if (previewsById.ContainsKey(entry.powerId))
                    Debug.LogWarning("Duplicate Weapon Power preview mapping for " + entry.powerId + ".", this);

                previewsById[entry.powerId] = entry.animationClip;
            }
        }

        if (previewPlayer == null || equipButton == null || equipButtonLabel == null)
            Debug.LogWarning("Weapon Power inventory UI has one or more missing serialized references.", this);

        if (!previewsById.ContainsKey(WeaponPowerId.None))
            Debug.LogWarning("Weapon Power inventory UI requires a default None preview.", this);
    }

    public void SelectPower(WeaponPowerId id)
    {
        SelectPowerInternal(id, false);
    }

    private void SelectPowerInternal(WeaponPowerId id, bool allowFallback)
    {
        BuildMaps();

        if (!IsPowerAvailable(id) || !previewsById.ContainsKey(id))
        {
            if (!allowFallback || id == WeaponPowerId.None)
                return;

            id = WeaponPowerId.None;
            if (!IsPowerAvailable(id) || !previewsById.ContainsKey(id))
                return;
        }

        selectedPowerId = id;

        AnimationClip clip;
        if (previewPlayer != null && previewsById.TryGetValue(id, out clip))
            previewPlayer.Play(clip);

        UpdateEquipButtonState();
    }

    private void EquipSelectedPower()
    {
        if (!WeaponPowerOwnershipSave.IsOwned(selectedPowerId) ||
            !IsPowerAvailable(selectedPowerId) ||
            !HasConfiguredPreview(selectedPowerId))
            return;

        WeaponPowerSave.Save(selectedPowerId);
        equippedPowerId = selectedPowerId;
        RefreshEquippedHighlights();
        UpdateEquipButtonState();
    }

    private void RefreshEquippedHighlights()
    {
        foreach (KeyValuePair<WeaponPowerId, WeaponPowerInventorySlot> pair in slotsById)
            pair.Value.SetEquipped(pair.Key == equippedPowerId);
    }

    private void UpdateEquipButtonState()
    {
        bool isEquipped = selectedPowerId == equippedPowerId;

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

    public void RefreshAvailability()
    {
        BuildMaps();

        foreach (KeyValuePair<WeaponPowerId, WeaponPowerInventorySlot> pair in slotsById)
            pair.Value.SetAvailable(WeaponPowerOwnershipSave.IsOwned(pair.Key));
    }

    public void SetPowerAvailable(WeaponPowerId id, bool available)
    {
        BuildMaps();

        WeaponPowerInventorySlot slot;
        if (!slotsById.TryGetValue(id, out slot) || slot == null)
            return;

        slot.SetAvailable(id == WeaponPowerId.None ||
                          (available && WeaponPowerOwnershipSave.IsOwned(id)));

        if (!slot.IsAvailable && selectedPowerId == id)
        {
            WeaponPowerId fallback = IsPowerAvailable(equippedPowerId)
                ? equippedPowerId
                : WeaponPowerId.None;
            SelectPowerInternal(fallback, true);
        }
    }

    private bool IsPowerAvailable(WeaponPowerId id)
    {
        WeaponPowerInventorySlot slot;
        return slotsById.TryGetValue(id, out slot) && slot != null && slot.IsAvailable;
    }

    private bool HasConfiguredPreview(WeaponPowerId id)
    {
        return previewsById.ContainsKey(id);
    }
}
