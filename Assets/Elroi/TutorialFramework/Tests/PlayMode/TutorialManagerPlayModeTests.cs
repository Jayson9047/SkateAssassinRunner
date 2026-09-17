using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Elroi.Tutorials.Tests
{
    public sealed class TutorialTestGameplayAdapter : MonoBehaviour, ITutorialGameplayAdapter
    {
        public bool Blocked { get; private set; }
        public bool WasBlockedWhenActionExecuted { get; private set; }
        public int ExecutionCount { get; private set; }

        public void SetGameplayInputBlocked(bool blocked) => Blocked = blocked;
        public bool CanExecuteGameplayAction(string actionId, out string reason) { reason = null; return true; }
        public void ExecuteGameplayAction(string actionId)
        {
            WasBlockedWhenActionExecuted = Blocked;
            ExecutionCount++;
        }
    }

    public sealed class TutorialAsyncTestGameplayAdapter : MonoBehaviour, ITutorialGameplayAdapter, ITutorialAsyncGameplayAdapter
    {
        public bool Blocked { get; private set; }
        public bool EveryStepExecutedWhileBlocked { get; private set; } = true;
        public int ExecutionCount { get; private set; }

        public void SetGameplayInputBlocked(bool blocked) => Blocked = blocked;
        public bool CanExecuteGameplayAction(string actionId, out string reason) { reason = null; return true; }
        public void ExecuteGameplayAction(string actionId)
        {
            EveryStepExecutedWhileBlocked &= Blocked;
            ExecutionCount++;
        }

        public IEnumerator ExecuteGameplayActionRoutine(string actionId)
        {
            ExecuteGameplayAction(actionId);
            yield return null;
            ExecuteGameplayAction(actionId);
        }
    }

    public sealed class TutorialManagerPlayModeTests
    {
        private GameObject root;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (root != null) Object.Destroy(root);
            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                if (eventSystem.name.StartsWith("ELROI_TEST_")) Object.Destroy(eventSystem.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualOnlyManagerStaysIdleAndCallableWithoutSequencers()
        {
            TutorialManager manager = CreateManager(null);
            TutorialDefinition definition = MakeDefinition("Manual Only", TutorialCompletionType.Manual, TutorialGesture.None, null);
            manager.MutableTutorialsForAuthoring.Add(definition);
            root.SetActive(true);
            yield return null;

            Assert.That(manager.enabled, Is.True);
            Assert.That(manager.TutorialSequencers.Count, Is.Zero);
            Assert.That(manager.HasContinuousAutomaticWork, Is.False);
            Assert.That(manager.TryPlayTutorial("Manual Only"), Is.True);
            Assert.That(manager.IsTutorialPlaying, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            manager.CompleteCurrentTutorial();
            yield return null;
            yield return null;
            Assert.That(manager.IsTutorialPlaying, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(manager.HasContinuousAutomaticWork, Is.False);
        }

        [UnityTest]
        public IEnumerator ConsumedGestureExecutesRealActionExactlyOnceBeforeUnlock()
        {
            TutorialTestGameplayAdapter adapter = null;
            TutorialManager manager = CreateManager(value => adapter = value);
            TutorialDefinition definition = MakeDefinition("Handoff", TutorialCompletionType.Gesture, TutorialGesture.Tap, "Jump");
            manager.MutableTutorialsForAuthoring.Add(definition);
            new GameObject("ELROI_TEST_EventSystem", typeof(EventSystem));
            root.SetActive(true);
            yield return null;

            Assert.That(manager.TryPlayTutorial("Handoff"), Is.True);
            TutorialGestureInput input = Object.FindFirstObjectByType<TutorialGestureInput>();
            Assert.That(input, Is.Not.Null);
            PointerEventData pointer = new PointerEventData(EventSystem.current) { position = new Vector2(300f, 300f) };
            input.OnPointerDown(pointer);
            input.OnPointerUp(pointer);

            Assert.That(adapter.ExecutionCount, Is.EqualTo(1));
            Assert.That(adapter.WasBlockedWhenActionExecuted, Is.True);
            Assert.That(adapter.Blocked, Is.True, "Input must remain locked through the consumed pointer-release frame.");
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(adapter.ExecutionCount, Is.EqualTo(1));
            Assert.That(adapter.Blocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator CompositeDoubleTapActionFinishesBothStepsBeforeUnlock()
        {
            TutorialAsyncTestGameplayAdapter adapter = null;
            TutorialManager manager = CreateManagerWithAdapter<TutorialAsyncTestGameplayAdapter>(value => adapter = value);
            TutorialDefinition definition = MakeDefinition("Composite Handoff", TutorialCompletionType.Gesture, TutorialGesture.DoubleTap, "DoubleJump");
            manager.MutableTutorialsForAuthoring.Add(definition);
            new GameObject("ELROI_TEST_EventSystem", typeof(EventSystem));
            root.SetActive(true);
            yield return null;

            Assert.That(manager.TryPlayTutorial("Composite Handoff"), Is.True);
            TutorialGestureInput input = Object.FindFirstObjectByType<TutorialGestureInput>();
            PointerEventData pointer = new PointerEventData(EventSystem.current) { position = new Vector2(300f, 300f) };
            input.OnPointerDown(pointer);
            input.OnPointerUp(pointer);
            input.OnPointerDown(pointer);
            input.OnPointerUp(pointer);

            for (int frame = 0; frame < 5 && adapter.ExecutionCount < 2; frame++) yield return null;
            Assert.That(adapter.ExecutionCount, Is.EqualTo(2), "A composite DoubleJump must replay both production tap steps.");
            Assert.That(adapter.EveryStepExecutedWhileBlocked, Is.True);
            Assert.That(adapter.Blocked, Is.True, "The framework must retain the lock until the composite action finishes.");

            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(adapter.Blocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private TutorialManager CreateManager(System.Action<TutorialTestGameplayAdapter> adapterCallback)
        {
            return CreateManagerWithAdapter(adapterCallback);
        }

        private TutorialManager CreateManagerWithAdapter<TAdapter>(System.Action<TAdapter> adapterCallback)
            where TAdapter : MonoBehaviour, ITutorialGameplayAdapter
        {
#if UNITY_EDITOR
            TutorialPresentation presentation = UnityEditor.AssetDatabase.LoadAssetAtPath<TutorialPresentation>("Assets/Elroi/TutorialFramework/Prefabs/ELROI_TutorialCanvas.prefab");
            TutorialTheme theme = UnityEditor.AssetDatabase.LoadAssetAtPath<TutorialTheme>("Assets/Elroi/TutorialFramework/Materials/ELROI_MangaTheme.asset");
#else
            TutorialPresentation presentation = null;
            TutorialTheme theme = null;
#endif
            Assert.That(presentation, Is.Not.Null);
            root = new GameObject("ELROI_TEST_Manager");
            root.SetActive(false);
            TutorialManager manager = root.AddComponent<TutorialManager>();
            TAdapter adapter = root.AddComponent<TAdapter>();
            adapterCallback?.Invoke(adapter);
            manager.ConfigureProvidersForAuthoring(presentation, theme, null, adapter, null, null);
            return manager;
        }

        private static TutorialDefinition MakeDefinition(string name, TutorialCompletionType completion, TutorialGesture gesture, string action)
        {
            TutorialDefinition definition = new TutorialDefinition
            {
                TutorialName = name,
                TargetType = TutorialTargetType.None,
                CompletionType = completion,
                RequiredGesture = gesture,
                GameplayActionId = action,
                FreezeWorld = true
            };
            definition.EnsureStableId();
            return definition;
        }
    }
}
