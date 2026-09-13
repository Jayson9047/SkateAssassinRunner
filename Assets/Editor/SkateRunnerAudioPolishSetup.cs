using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MoreMountains.Tools;

/// <summary>Idempotent authoring wiring; never runs in a player or automatically on load.</summary>
public static class SkateRunnerAudioPolishSetup
{
    public static int WireClicks(GameObject root)
    {
        var targets = root.GetComponentsInChildren<Button>(true).Select(x => x.gameObject)
            .Concat(root.GetComponentsInChildren<MMTouchButton>(true).Select(x => x.gameObject))
            .Concat(root.GetComponentsInChildren<UIClickToggle>(true).Select(x => x.gameObject)).Distinct();
        int count = 0;
        foreach (var target in targets)
        {
            var bridge = target.GetComponent<SkateRunnerUIClickAudio>() ?? Undo.AddComponent<SkateRunnerUIClickAudio>(target);
            var button = target.GetComponent<Button>();
            if (button)
            {
                bool bound = false;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    bound |= button.onClick.GetPersistentTarget(i) == bridge && button.onClick.GetPersistentMethodName(i) == nameof(bridge.OnButtonClick);
                if (!bound)
                {
                    Undo.RecordObject(button, "Wire central UI click audio");
                    UnityEventTools.AddPersistentListener(button.onClick, bridge.OnButtonClick);
                    // First callback captures interactivity before gameplay closes/disables the control.
                    var so = new SerializedObject(button);
                    var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                    calls.MoveArrayElement(calls.arraySize - 1, 0);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(button);
                }
            }
            count++;
        }
        return count;
    }

    public static string ConfigureOpenScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        int clicks = 0, frames = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var gui in root.GetComponentsInChildren<MoreMountains.InfiniteRunnerEngine.SkateRunnerGUIManager>(true))
            {
                var field = new SerializedObject(gui).FindProperty("DownslamButton");
                var down = field.objectReferenceValue as Button;
                if (down && !down.GetComponent<UIClickAudioOptOut>()) Undo.AddComponent<UIClickAudioOptOut>(down.gameObject);
            }
            clicks += WireClicks(root);
        }
        var rewards = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DailyRewardsPage>(true)).FirstOrDefault();
        if (rewards)
        {
            var source = rewards.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Focus");
            foreach (var card in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InventoryEquippedCardVisual>(true)))
            {
                var so = new SerializedObject(card);
                var existingFocus = so.FindProperty("equippedFocus").objectReferenceValue as GameObject;
                if (existingFocus)
                {
                    existingFocus.transform.SetAsFirstSibling();
                    continue;
                }
                var focus = UnityEngine.Object.Instantiate(source.gameObject, card.transform, false);
                Undo.RegisterCreatedObjectUndo(focus, "Add Rewards-style Inventory Focus");
                focus.name = "EquippedFocus";
                focus.transform.SetAsFirstSibling();
                // Keep the exact sliced sprite/material/color and border proportions.
                foreach (var child in focus.GetComponentsInChildren<Transform>(true).Where(t => t.name == "MessageBox").ToArray())
                    Undo.DestroyObjectImmediate(child.gameObject);
                foreach (var graphic in focus.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                var rt = (RectTransform)focus.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(.5f, .5f);
                rt.offsetMin = new Vector2(-8f, -8f);
                rt.offsetMax = new Vector2(8f, 8f);
                rt.localScale = Vector3.one;
                var layout = focus.GetComponent<LayoutElement>() ?? focus.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;
                focus.SetActive(false);
                so.FindProperty("equippedFocus").objectReferenceValue = focus;
                so.ApplyModifiedProperties();
                frames++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.path + ": " + clicks + " click bridges; " + frames + " copied Focus frames.";
    }
}
