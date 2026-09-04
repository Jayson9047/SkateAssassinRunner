using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Isolated visual-plumbing smoke tests. Never buys, equips or saves a power.</summary>
public static class WeaponPowerPreviewPipelineChecks
{
    public static IEnumerator CheckPlayback()
    {
        var root = new GameObject("Ability preview verification");
        root.SetActive(false);
        try
        {
            var widget = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Animator));
            widget.transform.SetParent(root.transform, false);
            var player = widget.AddComponent<WeaponPowerPreviewPlayer>();
            var animator = widget.GetComponent<Animator>();
            var image = widget.GetComponent<Image>();
            string folder = WeaponPowerPreviewPipelineBuilder.PreviewFolder;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "/AbilityKatanaPreview.controller");
            var template = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/DefaultPowerPreview.anim");
            var settings = new SerializedObject(player);
            settings.FindProperty("previewImage").objectReferenceValue = image;
            settings.FindProperty("previewAnimator").objectReferenceValue = animator;
            settings.FindProperty("turntableController").objectReferenceValue = controller;
            settings.FindProperty("turntableTemplate").objectReferenceValue = template;
            settings.FindProperty("turntableStateName").stringValue = "AbilityKatanaPreview";
            settings.ApplyModifiedPropertiesWithoutUndo();

            // Keep OnEnable disabled: it reads ownership and may repair an invalid
            // saved power. Tests use temporary available cards, not the user's save.
            var inventory = root.AddComponent<WeaponPowerInventoryController>();
            inventory.enabled = false;
            var equip = new GameObject("Equip", typeof(RectTransform), typeof(Button));
            equip.transform.SetParent(root.transform, false);
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(equip.transform, false);
            var button = equip.GetComponent<Button>();
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            var ids = new[] { WeaponPowerId.None, WeaponPowerId.Fire, WeaponPowerId.Ice,
                WeaponPowerId.Electricity, WeaponPowerId.Poison, WeaponPowerId.Magic };
            var serialized = new SerializedObject(inventory);
            serialized.FindProperty("previewPlayer").objectReferenceValue = player;
            serialized.FindProperty("equipButton").objectReferenceValue = button;
            serialized.FindProperty("equipButtonLabel").objectReferenceValue = label;
            var slots = serialized.FindProperty("powerSlots"); slots.arraySize = ids.Length;
            var previews = serialized.FindProperty("previewAnimations"); previews.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var card = new GameObject(ids[i] + " verification card").AddComponent<WeaponPowerInventorySlot>();
                card.transform.SetParent(root.transform, false);
                var cardSettings = new SerializedObject(card);
                cardSettings.FindProperty("powerId").intValue = (int)ids[i];
                cardSettings.FindProperty("controller").objectReferenceValue = inventory;
                cardSettings.ApplyModifiedPropertiesWithoutUndo();
                slots.GetArrayElementAtIndex(i).objectReferenceValue = card;
                string name = ids[i] == WeaponPowerId.None ? "Default" : ids[i].ToString();
                var entry = previews.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("powerId").intValue = (int)ids[i];
                entry.FindPropertyRelative("animationClip").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/" + name + "PowerPreview.anim");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            player.Play(template);
            root.SetActive(true);
            yield return null; // Awake and Start must run before testing Animator playback.
            foreach (var id in ids)
            {
                inventory.SelectPower(id);
                yield return null;
                animator.Update(0.5f);
                Require(inventory.SelectedPowerId == id, "Selection failed for " + id);
                Require(player.CurrentClip && player.CurrentClip.name == (id == WeaponPowerId.None ? "Default" : id.ToString()) + "PowerPreview", "Wrong clip for " + id);
                Require(image.enabled && image.sprite && animator.GetCurrentAnimatorStateInfo(0).IsName("AbilityKatanaPreview"), "Animator did not play " + id);
                Require(button.interactable == (id != WeaponPowerId.None), "Equip button state changed for " + id);
                Require(label.text == (id == WeaponPowerId.None ? "EQUIPPED" : "EQUIP"), "Equip label changed for " + id);
            }

            foreach (string category in new[] { "WeaponSprites", "RollerbladeSprites" })
            {
                string path = "Assets/Prefabs/PreviewSprites/" + category;
                var other = AssetDatabase.FindAssets("t:AnimatorController", new[] { path })
                    .Select(g => AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(g))).Single();
                var state = other.layers[0].stateMachine.defaultState;
                var motion = state.motion as AnimationClip;
                Require(motion, "Missing existing " + category + " template");
                player.ConfigurePlayback(other, motion, state.name);
                player.Play(motion);
                yield return null;
                animator.Update(0.5f);
                Require(image.enabled && image.sprite && animator.GetCurrentAnimatorStateInfo(0).IsName(state.name), category + " playback failed");
                player.RestoreDefaultPlayback();
                player.Play(template);
                yield return null;
                animator.Update(0.5f);
                Require(player.CurrentClip == template && image.enabled && image.sprite && animator.GetCurrentAnimatorStateInfo(0).IsName("AbilityKatanaPreview"), "Ability restore failed after " + category);
            }
            Debug.Log("[Ability Previews] PASS: six selections, Image/Animator playback, Equip/Equipped labels, Sword/Rollerblade switching and Ability restore. No ownership/equip saves were written.");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Ability preview verification: " + message);
    }
}
