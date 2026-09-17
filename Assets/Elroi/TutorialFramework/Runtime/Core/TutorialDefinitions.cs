using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Tutorials
{
    [Serializable]
    public sealed class TutorialDefinition
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private string tutorialName = "New Tutorial";
        [SerializeField, HideInInspector] private string stableId;
        [SerializeField] private TutorialManualRunPolicy manualRunPolicy = TutorialManualRunPolicy.AlwaysAllow;

        [SerializeField] private TutorialTargetType targetType = TutorialTargetType.ThreeDimensional;
        [SerializeField] private TutorialTargetSource targetSource = TutorialTargetSource.DirectObject;
        [SerializeField] private GameObject spotlightTarget;
        [SerializeField] private string runtimeTargetId;

        [SerializeField, TextArea(2, 5)] private string introductionText;
        [SerializeField, TextArea(1, 3)] private string bottomInstructionText;
        [SerializeField] private TutorialGestureAnimation gestureAnimation;
        [SerializeField] private TutorialSpotlightShape spotlightShape = TutorialSpotlightShape.RoundedRectangle;
        [SerializeField, Min(0f)] private float spotlightPadding = 24f;
        [SerializeField, Min(0f)] private float fallbackTargetWidth = 120f;
        [SerializeField, Min(0f)] private float fallbackTargetHeight = 120f;
        [SerializeField] private bool freezeWorld = true;

        [SerializeField] private TutorialCompletionType completionType = TutorialCompletionType.Gesture;
        [SerializeField] private TutorialGesture requiredGesture = TutorialGesture.Tap;
        [SerializeField] private string completionEventId;
        [SerializeField] private string gameplayActionId;
        [SerializeField] private bool markComplete = true;

        public bool Enabled { get => enabled; set => enabled = value; }
        public string TutorialName { get => tutorialName; set => tutorialName = value; }
        public string StableId => stableId;
        public TutorialManualRunPolicy ManualRunPolicy { get => manualRunPolicy; set => manualRunPolicy = value; }
        public TutorialTargetType TargetType { get => targetType; set => targetType = value; }
        public TutorialTargetSource TargetSource { get => targetSource; set => targetSource = value; }
        public GameObject SpotlightTarget { get => spotlightTarget; set => spotlightTarget = value; }
        public string RuntimeTargetId { get => runtimeTargetId; set => runtimeTargetId = value; }
        public string IntroductionText { get => introductionText; set => introductionText = value; }
        public string BottomInstructionText { get => bottomInstructionText; set => bottomInstructionText = value; }
        public TutorialGestureAnimation GestureAnimation { get => gestureAnimation; set => gestureAnimation = value; }
        public TutorialSpotlightShape SpotlightShape { get => spotlightShape; set => spotlightShape = value; }
        public float SpotlightPadding { get => spotlightPadding; set => spotlightPadding = Mathf.Max(0f, value); }
        public Vector2 FallbackTargetSize => new Vector2(Mathf.Max(1f, fallbackTargetWidth), Mathf.Max(1f, fallbackTargetHeight));
        public bool FreezeWorld { get => freezeWorld; set => freezeWorld = value; }
        public TutorialCompletionType CompletionType { get => completionType; set => completionType = value; }
        public TutorialGesture RequiredGesture { get => requiredGesture; set => requiredGesture = value; }
        public string CompletionEventId { get => completionEventId; set => completionEventId = value; }
        public string GameplayActionId { get => gameplayActionId; set => gameplayActionId = value; }
        public bool MarkComplete { get => markComplete; set => markComplete = value; }

        public void EnsureStableId()
        {
            if (string.IsNullOrWhiteSpace(stableId)) stableId = Guid.NewGuid().ToString("N");
        }

        public void SetStableIdForMigration(string value) => stableId = value;
    }

    [Serializable]
    public sealed class TutorialTriggerDefinition
    {
        [SerializeField] private TutorialTriggerType triggerType = TutorialTriggerType.Immediate;
        [SerializeField, Min(0f)] private float delaySeconds;
        [SerializeField] private TutorialTimeOrigin timeOrigin = TutorialTimeOrigin.PreviousTutorialCompletion;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private GameObject referenceObject;
        [SerializeField] private GameObject targetObject;
        [SerializeField] private string referenceTargetId;
        [SerializeField] private string targetId;
        [SerializeField, Min(0f)] private float maximumAbsoluteXDistance = 4f;
        [SerializeField] private bool useYRange;
        [SerializeField] private float relativeYMinimum = -0.5f;
        [SerializeField] private float relativeYMaximum = 0.5f;
        [SerializeField, Min(0f)] private float visibilityDelay = 0.4f;
        [SerializeField] private Camera visibilityCamera;
        [SerializeField] private TutorialVariableCondition variableCondition = new TutorialVariableCondition();
        [SerializeField] private string eventId;

        public TutorialTriggerType TriggerType { get => triggerType; set => triggerType = value; }
        public float DelaySeconds { get => delaySeconds; set => delaySeconds = Mathf.Max(0f, value); }
        public TutorialTimeOrigin TimeOrigin { get => timeOrigin; set => timeOrigin = value; }
        public bool UseUnscaledTime { get => useUnscaledTime; set => useUnscaledTime = value; }
        public GameObject ReferenceObject { get => referenceObject; set => referenceObject = value; }
        public GameObject TargetObject { get => targetObject; set => targetObject = value; }
        public string ReferenceTargetId { get => referenceTargetId; set => referenceTargetId = value; }
        public string TargetId { get => targetId; set => targetId = value; }
        public float MaximumAbsoluteXDistance { get => maximumAbsoluteXDistance; set => maximumAbsoluteXDistance = Mathf.Max(0f, value); }
        public bool UseYRange { get => useYRange; set => useYRange = value; }
        public float RelativeYMinimum { get => relativeYMinimum; set => relativeYMinimum = value; }
        public float RelativeYMaximum { get => relativeYMaximum; set => relativeYMaximum = value; }
        public float VisibilityDelay { get => visibilityDelay; set => visibilityDelay = Mathf.Max(0f, value); }
        public Camera VisibilityCamera { get => visibilityCamera; set => visibilityCamera = value; }
        public TutorialVariableCondition VariableCondition => variableCondition;
        public string EventId { get => eventId; set => eventId = value; }
    }

    [Serializable]
    public sealed class TutorialSequenceEntry
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private string tutorialStableId;
        [SerializeField] private TutorialTriggerDefinition trigger = new TutorialTriggerDefinition();

        public bool Enabled { get => enabled; set => enabled = value; }
        public string TutorialStableId { get => tutorialStableId; set => tutorialStableId = value; }
        public TutorialTriggerDefinition Trigger => trigger;
    }

    [Serializable]
    public sealed class TutorialSequencerDefinition
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private string sequenceName = "New Tutorial Sequence";
        [SerializeField, HideInInspector] private string stableId;
        [SerializeField] private TutorialRunPolicy runPolicy = TutorialRunPolicy.OnceEver;
        [SerializeField] private TutorialTriggerDefinition startTrigger = new TutorialTriggerDefinition();
        [SerializeField] private bool lockGameplayBetweenTutorials = true;
        [SerializeField] private List<TutorialSequenceEntry> entries = new List<TutorialSequenceEntry>();

        public bool Enabled { get => enabled; set => enabled = value; }
        public string SequenceName { get => sequenceName; set => sequenceName = value; }
        public string StableId => stableId;
        public TutorialRunPolicy RunPolicy { get => runPolicy; set => runPolicy = value; }
        public TutorialTriggerDefinition StartTrigger => startTrigger;
        public bool LockGameplayBetweenTutorials { get => lockGameplayBetweenTutorials; set => lockGameplayBetweenTutorials = value; }
        public List<TutorialSequenceEntry> Entries => entries;

        public void EnsureStableId()
        {
            if (string.IsNullOrWhiteSpace(stableId)) stableId = Guid.NewGuid().ToString("N");
        }

        public void SetStableIdForMigration(string value) => stableId = value;
    }
}
