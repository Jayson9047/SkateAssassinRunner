using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns Rollerblade selection, shared preview playback, and Equip while the tab is active.</summary>
[DisallowMultipleComponent]
public sealed class RollerbladeInventoryController : MonoBehaviour
{
    [Serializable]
    private sealed class PreviewEntry
    {
        public RollerbladeId rollerbladeId;
        public AnimationClip animationClip;
    }

    [Header("Rollerblade Cards")]
    [SerializeField] private RollerbladeInventorySlot[] rollerbladeSlots;

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

    private readonly Dictionary<RollerbladeId, RollerbladeInventorySlot> slotsById =
        new Dictionary<RollerbladeId, RollerbladeInventorySlot>();

    private readonly Dictionary<RollerbladeId, AnimationClip> previewsById =
        new Dictionary<RollerbladeId, AnimationClip>();

    private RollerbladeId selectedRollerbladeId = RollerbladeId.Default;
    private RollerbladeId equippedRollerbladeId = RollerbladeId.Default;
    private bool mapsBuilt;

    public RollerbladeId SelectedRollerbladeId => selectedRollerbladeId;
    public RollerbladeId EquippedRollerbladeId => equippedRollerbladeId;

    private void OnEnable()
    {
        BuildMaps();
        RefreshAvailability();

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(EquipSelectedRollerblade);
            equipButton.onClick.AddListener(EquipSelectedRollerblade);
        }

        if (previewPlayer != null)
            previewPlayer.ConfigurePlayback(previewController, previewTemplate, previewStateName);

        RollerbladeId savedId;
        bool hasSavedPair = RollerbladeSave.TryLoad(out savedId);
        bool savedPairIsUsable = hasSavedPair && IsRollerbladeUsable(savedId);

        equippedRollerbladeId = savedPairIsUsable ? savedId : RollerbladeId.Default;

        if (!hasSavedPair || !savedPairIsUsable)
            RollerbladeSave.Save(RollerbladeId.Default);

        RefreshEquippedHighlights();
        SelectRollerbladeInternal(equippedRollerbladeId, true);
    }

    private void OnDisable()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(EquipSelectedRollerblade);
    }

    public void SelectRollerblade(RollerbladeId id)
    {
        SelectRollerbladeInternal(id, false);
    }

    public void RefreshAvailability()
    {
        BuildMaps();

        // Future Shop/ownership integration:
        // Query the ownership backend by RollerbladeId here, then call slot.SetAvailable(isOwned).
        foreach (KeyValuePair<RollerbladeId, RollerbladeInventorySlot> pair in slotsById)
            pair.Value.SetAvailable(RollerbladeOwnershipSave.IsOwned(pair.Key));
    }

    public void SetRollerbladeAvailable(RollerbladeId id, bool available)
    {
        BuildMaps();

        RollerbladeInventorySlot slot;
        if (!slotsById.TryGetValue(id, out slot) || slot == null)
            return;

        slot.SetAvailable(id == RollerbladeId.Default ||
                          (available && RollerbladeOwnershipSave.IsOwned(id)));

        if (!slot.IsAvailable && selectedRollerbladeId == id)
        {
            SelectRollerbladeInternal(
                IsRollerbladeUsable(equippedRollerbladeId)
                    ? equippedRollerbladeId
                    : RollerbladeId.Default,
                true);
        }
    }

    private void SelectRollerbladeInternal(RollerbladeId id, bool allowFallback)
    {
        BuildMaps();

        if (!IsRollerbladeUsable(id))
        {
            if (!allowFallback || id == RollerbladeId.Default)
                return;

            id = RollerbladeId.Default;
            if (!IsRollerbladeUsable(id))
                return;
        }

        selectedRollerbladeId = id;

        AnimationClip clip;
        if (previewPlayer != null && previewsById.TryGetValue(id, out clip))
            previewPlayer.Play(clip);

        UpdateEquipButtonState();
    }

    private void EquipSelectedRollerblade()
    {
        if (!IsRollerbladeUsable(selectedRollerbladeId))
            return;

        RollerbladeSave.Save(selectedRollerbladeId);
        equippedRollerbladeId = selectedRollerbladeId;
        RefreshEquippedHighlights();
        UpdateEquipButtonState();
    }

    private void RefreshEquippedHighlights()
    {
        foreach (KeyValuePair<RollerbladeId, RollerbladeInventorySlot> pair in slotsById)
            pair.Value.SetEquipped(pair.Key == equippedRollerbladeId);
    }

    private void UpdateEquipButtonState()
    {
        bool isEquipped = selectedRollerbladeId == equippedRollerbladeId;

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

    private bool IsRollerbladeUsable(RollerbladeId id)
    {
        if (!RollerbladeOwnershipSave.IsValidRollerbladeId(id) ||
            !RollerbladeOwnershipSave.IsOwned(id))
        {
            return false;
        }

        RollerbladeInventorySlot slot;
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

        if (rollerbladeSlots != null)
        {
            for (int i = 0; i < rollerbladeSlots.Length; i++)
            {
                RollerbladeInventorySlot slot = rollerbladeSlots[i];
                if (slot == null)
                    continue;

                if (slotsById.ContainsKey(slot.RollerbladeId))
                {
                    Debug.LogWarning(
                        "Duplicate Rollerblade Inventory mapping for " + slot.RollerbladeId + ".",
                        slot);
                }

                slotsById[slot.RollerbladeId] = slot;
            }
        }

        if (previewAnimations != null)
        {
            for (int i = 0; i < previewAnimations.Length; i++)
            {
                PreviewEntry entry = previewAnimations[i];
                if (entry == null || entry.animationClip == null)
                    continue;

                if (previewsById.ContainsKey(entry.rollerbladeId))
                {
                    Debug.LogWarning(
                        "Duplicate Rollerblade preview mapping for " + entry.rollerbladeId + ".",
                        this);
                }

                previewsById[entry.rollerbladeId] = entry.animationClip;
            }
        }

        if (previewPlayer == null || previewController == null || previewTemplate == null ||
            equipButton == null || equipButtonLabel == null)
        {
            Debug.LogWarning("Rollerblade Inventory UI has one or more missing serialized references.", this);
        }

        if (!previewsById.ContainsKey(RollerbladeId.Default))
            Debug.LogWarning("Rollerblade Inventory requires a configured Default preview.", this);
    }
}
