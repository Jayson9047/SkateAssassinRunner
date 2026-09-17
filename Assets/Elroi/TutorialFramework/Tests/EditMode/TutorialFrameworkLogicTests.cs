using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Elroi.Tutorials.Tests
{
    public sealed class TutorialFrameworkLogicTests
    {
        [TearDown]
        public void TearDown()
        {
            TutorialTargetRegistry.ClearForTests();
            Time.timeScale = 1f;
            foreach (GameObject value in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (value.name.StartsWith("ELROI_TEST_", StringComparison.Ordinal)) UnityEngine.Object.DestroyImmediate(value);
        }

        [Test]
        public void TutorialLookupUsesUniqueName()
        {
            TutorialManager manager = NewManager();
            TutorialDefinition tutorial = NewTutorial("Boss Weak Point");
            manager.MutableTutorialsForAuthoring.Add(tutorial);
            Assert.That(manager.GetTutorial("Boss Weak Point"), Is.SameAs(tutorial));
            Assert.That(manager.GetTutorial("Missing"), Is.Null);
        }

        [Test]
        public void DuplicateNamesAndStableIdsAreValidationErrors()
        {
            TutorialManager manager = NewManager();
            TutorialDefinition first = NewTutorial("Duplicate");
            TutorialDefinition second = NewTutorial("Duplicate");
            second.SetStableIdForMigration(first.StableId);
            manager.MutableTutorialsForAuthoring.Add(first);
            manager.MutableTutorialsForAuthoring.Add(second);
            string combined = string.Join("\n", manager.GetValidationIssues());
            StringAssert.Contains("Duplicate Tutorial Name", combined);
            StringAssert.Contains("Duplicate Tutorial stable ID", combined);
        }

        [Test]
        public void StableIdIsGeneratedOnceAndSurvivesRename()
        {
            TutorialDefinition tutorial = NewTutorial("Before");
            string id = tutorial.StableId;
            tutorial.TutorialName = "After";
            tutorial.EnsureStableId();
            Assert.That(tutorial.StableId, Is.EqualTo(id));
        }

        [TestCase(TutorialComparisonOperator.Equal, 5, 5, true)]
        [TestCase(TutorialComparisonOperator.NotEqual, 5, 4, true)]
        [TestCase(TutorialComparisonOperator.Greater, 5, 4, true)]
        [TestCase(TutorialComparisonOperator.GreaterOrEqual, 5, 5, true)]
        [TestCase(TutorialComparisonOperator.Less, 4, 5, true)]
        [TestCase(TutorialComparisonOperator.LessOrEqual, 5, 5, true)]
        public void NumericComparisonOperatorsWork(TutorialComparisonOperator operation, int left, int right, bool expected)
        {
            Assert.That(TutorialConditionEvaluator.TryEvaluate(TutorialValue.From(left), operation, TutorialValue.From(right), out bool result, out string error), Is.True, error);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void IncompatibleComparisonFailsSafely()
        {
            Assert.That(TutorialConditionEvaluator.TryEvaluate(TutorialValue.From(true), TutorialComparisonOperator.Greater,
                TutorialValue.From(false), out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void VariableStoreEmitsOnlyWhenValueActuallyChanges()
        {
            TutorialVariableStore store = NewObject("VariableStore").AddComponent<TutorialVariableStore>();
            store.Variables.Add(new TutorialVariableEntry { Id = "Coins", Value = TutorialValue.From(10) });
            store.RebuildIndex();
            int events = 0;
            store.VariableChanged += (_, __) => events++;
            Assert.That(store.SetInteger("Coins", 10), Is.False);
            Assert.That(store.SetInteger("Coins", 11), Is.True);
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void RealDoubleTapRequiresMaximumInterval()
        {
            TutorialGestureRecognizer recognizer = new TutorialGestureRecognizer();
            TutorialGestureSettings settings = TutorialGestureSettings.Default;
            recognizer.PointerDown(Vector2.zero, 0f);
            Assert.That(recognizer.PointerUp(Vector2.zero, 0.05f, TutorialGesture.DoubleTap, settings), Is.False);
            recognizer.PointerDown(Vector2.zero, 5.05f);
            Assert.That(recognizer.PointerUp(Vector2.zero, 5.1f, TutorialGesture.DoubleTap, settings), Is.False, "Tap + five seconds + tap must not count.");
            recognizer.PointerDown(Vector2.zero, 5.2f);
            Assert.That(recognizer.PointerUp(Vector2.zero, 5.25f, TutorialGesture.DoubleTap, settings), Is.True);
        }

        [TestCase(TutorialGesture.SwipeLeft, -120f, 0f)]
        [TestCase(TutorialGesture.SwipeRight, 120f, 0f)]
        [TestCase(TutorialGesture.SwipeUp, 0f, 120f)]
        [TestCase(TutorialGesture.SwipeDown, 0f, -120f)]
        public void SwipeDirectionDetectionWorks(TutorialGesture gesture, float x, float y)
        {
            TutorialGestureRecognizer recognizer = new TutorialGestureRecognizer();
            recognizer.PointerDown(Vector2.zero, 0f);
            Assert.That(recognizer.PointerUp(new Vector2(x, y), 0.1f, gesture, TutorialGestureSettings.Default), Is.True);
        }

        [Test]
        public void TapRespectsMovementTolerance()
        {
            TutorialGestureRecognizer recognizer = new TutorialGestureRecognizer();
            recognizer.PointerDown(Vector2.zero, 0f);
            Assert.That(recognizer.PointerUp(new Vector2(10f, 10f), 0.1f, TutorialGesture.Tap, TutorialGestureSettings.Default), Is.True);
        }

        [Test]
        public void NextEnabledAndFinalEntryIgnoreDisabledEntries()
        {
            TutorialSequenceEntry first = new TutorialSequenceEntry { Enabled = true };
            TutorialSequenceEntry disabled = new TutorialSequenceEntry { Enabled = false };
            TutorialSequenceEntry final = new TutorialSequenceEntry { Enabled = true };
            TutorialSequenceEntry[] entries = { first, disabled, final };
            Assert.That(TutorialSequenceLogic.FindNextEnabledEntry(entries, 0), Is.EqualTo(2));
            Assert.That(TutorialSequenceLogic.IsFinalEnabledEntry(entries, 2), Is.True);
            final.Enabled = false;
            Assert.That(TutorialSequenceLogic.IsFinalEnabledEntry(entries, 0), Is.True);
        }

        [Test]
        public void DistanceUsesAbsoluteXAndOptionalRelativeYRange()
        {
            Assert.That(TutorialSpatialLogic.IsWithinDistance(Vector3.zero, new Vector3(-3f, 0.4f, 100f), 4f, true, -0.5f, 0.5f), Is.True);
            Assert.That(TutorialSpatialLogic.IsWithinDistance(Vector3.zero, new Vector3(5f, 0f, 0f), 4f, false, 0f, 0f), Is.False);
            Assert.That(TutorialSpatialLogic.IsWithinDistance(Vector3.zero, new Vector3(3f, 0.6f, 0f), 4f, true, -0.5f, 0.5f), Is.False);
        }

        [Test]
        public void VisibilityDelayResetsWhenTargetDisappears()
        {
            TutorialVisibilityDelay delay = new TutorialVisibilityDelay();
            Assert.That(delay.Step(true, 0.3f, 0.4f), Is.False);
            Assert.That(delay.Step(false, 0.1f, 0.4f), Is.False);
            Assert.That(delay.VisibleDuration, Is.Zero);
            Assert.That(delay.Step(true, 0.2f, 0.4f), Is.False);
            Assert.That(delay.Step(true, 0.2f, 0.4f), Is.True);
        }

        [Test]
        public void VisibleOnScreenRejectsTargetBehindCamera()
        {
            GameObject cameraObject = NewObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.identity;
            GameObject target = NewObject("Target");
            target.AddComponent<BoxCollider>();
            target.transform.position = new Vector3(0f, 0f, -5f);
            Assert.That(TutorialScreenUtility.IsActuallyVisible(target, TutorialTargetType.ThreeDimensional, camera), Is.False);
            target.transform.position = new Vector3(0f, 0f, 5f);
            Assert.That(TutorialScreenUtility.IsActuallyVisible(target, TutorialTargetType.ThreeDimensional, camera), Is.True);
        }

        [Test]
        public void RuntimeTargetRegistryResolvesDuplicateIdsDeterministically()
        {
            TutorialTargetMarker a = NewObject("A").AddComponent<TutorialTargetMarker>();
            TutorialTargetMarker b = NewObject("B").AddComponent<TutorialTargetMarker>();
            a.TargetId = "Drone";
            b.TargetId = "Drone";
            TutorialTargetRegistry.Register(b);
            TutorialTargetRegistry.Register(a);
            Assert.That(TutorialTargetRegistry.TryResolve("Drone", out GameObject resolved), Is.True);
            int expected = Math.Min(a.GetInstanceID(), b.GetInstanceID());
            Assert.That(resolved.GetInstanceID(), Is.EqualTo(expected == a.GetInstanceID() ? a.gameObject.GetInstanceID() : b.gameObject.GetInstanceID()));
        }

        [Test]
        public void RuntimeTargetMarkerReregistersWhenItsIdChanges()
        {
            TutorialTargetMarker marker = NewObject("Retargetable").AddComponent<TutorialTargetMarker>();
            marker.TargetId = "OldId";
            Assert.That(TutorialTargetRegistry.TryResolve("OldId", out GameObject oldTarget), Is.True);
            Assert.That(oldTarget, Is.EqualTo(marker.gameObject));

            marker.TargetId = "NewId";
            Assert.That(TutorialTargetRegistry.TryResolve("OldId", out _), Is.False);
            Assert.That(TutorialTargetRegistry.TryResolve("NewId", out GameObject newTarget), Is.True);
            Assert.That(newTarget, Is.EqualTo(marker.gameObject));
        }

#if UNITY_EDITOR
        [Test]
        public void RuntimeTargetMarkerUsesAUnityDiscoverableDedicatedScriptFile()
        {
            TutorialTargetMarker marker = NewObject("Discoverable").AddComponent<TutorialTargetMarker>();
            UnityEditor.MonoScript script = UnityEditor.MonoScript.FromMonoBehaviour(marker);
            Assert.That(script, Is.Not.Null);
            Assert.That(System.IO.Path.GetFileName(UnityEditor.AssetDatabase.GetAssetPath(script)), Is.EqualTo("TutorialTargetMarker.cs"));
            Assert.That(Attribute.GetCustomAttribute(typeof(TutorialTargetMarker), typeof(AddComponentMenu)), Is.Not.Null);
        }
#endif

        [Test]
        public void TimeScaleFreezeRestoresExactPriorValue()
        {
            Time.timeScale = 0.37f;
            TimeScaleTutorialFreeze freeze = new TimeScaleTutorialFreeze();
            freeze.Freeze();
            Assert.That(Time.timeScale, Is.Zero);
            freeze.Restore();
            Assert.That(Time.timeScale, Is.EqualTo(0.37f).Within(0.0001f));
        }

        [TestCase(TutorialRunPolicy.EveryTime, true, true, true, true)]
        [TestCase(TutorialRunPolicy.OncePerSceneLoad, true, false, false, false)]
        [TestCase(TutorialRunPolicy.OncePerSession, false, true, false, false)]
        [TestCase(TutorialRunPolicy.OnceEver, false, false, true, false)]
        public void RunPoliciesAreDeterministic(TutorialRunPolicy policy, bool scene, bool session, bool persisted, bool expected)
        {
            Assert.That(TutorialRunPolicyLogic.CanRun(policy, scene, session, persisted), Is.EqualTo(expected));
        }

        [Test]
        public void ManagerHasNoUniversalUpdateLoop()
        {
            MethodInfo update = typeof(TutorialManager).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.That(update, Is.Null);
        }

        private static TutorialManager NewManager() => NewObject("Manager").AddComponent<TutorialManager>();

        private static TutorialDefinition NewTutorial(string name)
        {
            TutorialDefinition value = new TutorialDefinition { TutorialName = name };
            value.EnsureStableId();
            return value;
        }

        private static GameObject NewObject(string suffix) => new GameObject("ELROI_TEST_" + suffix);
    }
}
