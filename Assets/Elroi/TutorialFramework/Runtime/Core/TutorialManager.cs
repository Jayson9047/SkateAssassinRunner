using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Tutorials
{
    [DisallowMultipleComponent]
    public sealed class TutorialManager : MonoBehaviour
    {
        [Header("Lifecycle")]
        [SerializeField] private TutorialLifecyclePolicy lifecyclePolicy = TutorialLifecyclePolicy.Automatic;
        [SerializeField] private TutorialAutomaticCompletionPolicy whenAutomaticWorkCompletes = TutorialAutomaticCompletionPolicy.StayIdle;

        [Header("Providers / Integration")]
        [SerializeField] private TutorialPresentation presentationPrefab;
        [SerializeField] private TutorialTheme theme;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private MonoBehaviour gameplayAdapterComponent;
        [SerializeField] private MonoBehaviour variableProviderComponent;
        [SerializeField] private MonoBehaviour persistenceProviderComponent;
        [SerializeField] private TutorialGestureSettings gestureSettings = default;

        [Header("Tutorials")]
        [SerializeField] private List<TutorialDefinition> tutorials = new List<TutorialDefinition>();

        [Header("Tutorial Sequencers")]
        [SerializeField] private List<TutorialSequencerDefinition> tutorialSequencers = new List<TutorialSequencerDefinition>();

        private static readonly HashSet<string> SessionCompletedSequencers = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> sceneCompletedSequencers = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, TutorialDefinition> tutorialsByName = new Dictionary<string, TutorialDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, TutorialDefinition> tutorialsById = new Dictionary<string, TutorialDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, TutorialSequencerDefinition> sequencersByName = new Dictionary<string, TutorialSequencerDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<TutorialSequencerDefinition>> startVariableDependencies = new Dictionary<string, List<TutorialSequencerDefinition>>(StringComparer.Ordinal);
        private readonly HashSet<string> warnedMessages = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<TutorialSequencerDefinition, Coroutine> startWatchers = new Dictionary<TutorialSequencerDefinition, Coroutine>();

        private ITutorialGameplayAdapter gameplayAdapter;
        private ITutorialVariableProvider variableProvider;
        private ITutorialPersistenceProvider persistenceProvider;
        private readonly TimeScaleTutorialFreeze freezeService = new TimeScaleTutorialFreeze();
        private TutorialPresentation presentation;
        private TutorialDefinition currentTutorial;
        private TutorialSequencerDefinition activeSequencer;
        private int activeEntryIndex = -1;
        private Coroutine activeEntryWatcher;
        private bool completionInProgress;
        private bool inputBlocked;
        private float managerScaledStartTime;
        private float managerUnscaledStartTime;
        private float sequencerScaledStartTime;
        private float sequencerUnscaledStartTime;
        private float previousCompletionScaledTime;
        private float previousCompletionUnscaledTime;

        public IReadOnlyList<TutorialDefinition> Tutorials => tutorials;
        public IReadOnlyList<TutorialSequencerDefinition> TutorialSequencers => tutorialSequencers;
        public bool IsTutorialPlaying => currentTutorial != null;
        public bool IsSequencerPlaying => activeSequencer != null;
        public TutorialDefinition CurrentTutorial => currentTutorial;
        public TutorialSequencerDefinition ActiveSequencer => activeSequencer;
        public bool HasContinuousAutomaticWork => activeEntryWatcher != null || startWatchers.Count > 0;

        public event Action<TutorialDefinition> TutorialStarted;
        public event Action<TutorialDefinition> TutorialCompleted;
        public event Action<TutorialSequencerDefinition> SequencerStarted;
        public event Action<TutorialSequencerDefinition> SequencerCompleted;

        private void Reset()
        {
            gestureSettings = TutorialGestureSettings.Default;
        }

        private void Awake()
        {
            if (gestureSettings.TapMovementTolerance <= 0f && gestureSettings.SwipeMinimumDistance <= 0f)
                gestureSettings = TutorialGestureSettings.Default;
            managerScaledStartTime = Time.time;
            managerUnscaledStartTime = Time.unscaledTime;
            ResolveProviders();
            BuildIndexes();
            CreatePresentation();
        }

        private void Start()
        {
            if (lifecyclePolicy != TutorialLifecyclePolicy.Manual)
                InitializeAutomaticSequencers();
            if (!HasAnyEnabledContent())
                SafeDisableComponent();
        }

        private void OnDisable() => CleanupRuntimeState();
        private void OnDestroy() => CleanupRuntimeState();

        private void OnValidate()
        {
            EnsureStableIds();
            if (gestureSettings.DoubleTapMaximumInterval <= 0f) gestureSettings.DoubleTapMaximumInterval = 0.3f;
            if (gestureSettings.TapMovementTolerance <= 0f) gestureSettings.TapMovementTolerance = 25f;
            if (gestureSettings.SwipeMinimumDistance <= 0f) gestureSettings.SwipeMinimumDistance = 100f;
        }

        public TutorialDefinition GetTutorial(string tutorialName)
        {
            EnsureRuntimeIndexes();
            tutorialsByName.TryGetValue(tutorialName ?? string.Empty, out TutorialDefinition tutorial);
            return tutorial;
        }

        public TutorialDefinition GetTutorialByStableId(string stableId)
        {
            EnsureRuntimeIndexes();
            tutorialsById.TryGetValue(stableId ?? string.Empty, out TutorialDefinition tutorial);
            return tutorial;
        }

        public void PlayTutorial(string tutorialName)
        {
            if (!TryPlayTutorial(tutorialName))
                throw new InvalidOperationException($"ELROI Tutorial '{tutorialName}' could not be played. Check the Unity Console for validation details.");
        }

        public void PlayTutorial(string tutorialName, GameObject runtimeTarget) => PlayTutorial(tutorialName, new TutorialContext(runtimeTarget));

        public void PlayTutorial(string tutorialName, TutorialContext context)
        {
            if (!TryPlayTutorial(tutorialName, context))
                throw new InvalidOperationException($"ELROI Tutorial '{tutorialName}' could not be played. Check the Unity Console for validation details.");
        }

        public bool TryPlayTutorial(string tutorialName) => TryPlayTutorial(tutorialName, default(TutorialContext));

        public bool TryPlayTutorial(string tutorialName, GameObject runtimeTarget) => TryPlayTutorial(tutorialName, new TutorialContext(runtimeTarget));

        public bool TryPlayTutorial(string tutorialName, TutorialContext context)
        {
            TutorialDefinition tutorial = GetTutorial(tutorialName);
            if (tutorial == null) { WarnOnce($"missing-name:{tutorialName}", $"[ELROI Tutorials] No Tutorial named '{tutorialName}' exists on '{name}'."); return false; }
            if (tutorial.ManualRunPolicy == TutorialManualRunPolicy.RespectCompletion && IsTutorialPersistedComplete(tutorial)) return false;
            return BeginTutorial(tutorial, context, false);
        }

        public void StopCurrentTutorial()
        {
            if (currentTutorial == null) return;
            StopAllActiveWatchers();
            presentation?.Hide();
            freezeService.Restore();
            currentTutorial = null;
            completionInProgress = false;
            if (activeSequencer != null) CompleteSequencer(false);
            SetGameplayBlocked(false);
            EvaluateShutdownPolicy();
        }

        public void CompleteCurrentTutorial()
        {
            if (currentTutorial != null && !completionInProgress)
                StartCoroutine(CompleteTutorialRoutine(false));
        }

        public void RaiseEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)) { WarnOnce("empty-event", "[ELROI Tutorials] Ignored an empty Tutorial event ID."); return; }

            if (currentTutorial != null && currentTutorial.CompletionType == TutorialCompletionType.Event &&
                string.Equals(currentTutorial.CompletionEventId, eventId, StringComparison.Ordinal))
            {
                CompleteCurrentTutorial();
                return;
            }

            if (activeSequencer != null && currentTutorial == null && activeEntryIndex >= 0)
            {
                TutorialSequenceEntry entry = activeSequencer.Entries[activeEntryIndex];
                if (entry.Trigger.TriggerType == TutorialTriggerType.Event && string.Equals(entry.Trigger.EventId, eventId, StringComparison.Ordinal))
                    ActivatePendingEntry();
            }

            foreach (TutorialSequencerDefinition sequencer in tutorialSequencers)
            {
                if (sequencer == null || !sequencer.Enabled || sequencer == activeSequencer) continue;
                if (sequencer.StartTrigger.TriggerType == TutorialTriggerType.Event && string.Equals(sequencer.StartTrigger.EventId, eventId, StringComparison.Ordinal))
                    TryStartSequencerInternal(sequencer, false);
            }
        }

        public void PlaySequencer(string sequenceName)
        {
            if (!TryPlaySequencer(sequenceName))
                throw new InvalidOperationException($"ELROI Tutorial Sequencer '{sequenceName}' could not be played.");
        }

        public bool TryPlaySequencer(string sequenceName)
        {
            EnsureRuntimeIndexes();
            if (!sequencersByName.TryGetValue(sequenceName ?? string.Empty, out TutorialSequencerDefinition sequencer))
            {
                WarnOnce($"missing-sequence:{sequenceName}", $"[ELROI Tutorials] No Tutorial Sequencer named '{sequenceName}' exists on '{name}'.");
                return false;
            }
            return TryStartSequencerInternal(sequencer, true);
        }

        public void StopSequencer(string sequenceName)
        {
            if (activeSequencer == null || !string.Equals(activeSequencer.SequenceName, sequenceName, StringComparison.Ordinal)) return;
            StopCurrentTutorial();
        }

        public bool TriggerPendingEntry()
        {
            if (activeSequencer == null || currentTutorial != null || activeEntryIndex < 0) return false;
            TutorialSequenceEntry entry = activeSequencer.Entries[activeEntryIndex];
            if (entry.Trigger.TriggerType != TutorialTriggerType.Manual) return false;
            ActivatePendingEntry();
            return true;
        }

        public IReadOnlyList<string> GetValidationIssues()
        {
            ResolveProviders();
            BuildIndexes();
            List<string> issues = new List<string>();
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (TutorialDefinition tutorial in tutorials)
            {
                if (tutorial == null) { issues.Add("Tutorial list contains a null entry."); continue; }
                if (string.IsNullOrWhiteSpace(tutorial.TutorialName)) issues.Add("A Tutorial has an empty Tutorial Name.");
                else if (!names.Add(tutorial.TutorialName)) issues.Add($"Duplicate Tutorial Name: '{tutorial.TutorialName}'.");
                if (string.IsNullOrWhiteSpace(tutorial.StableId)) issues.Add($"Tutorial '{tutorial.TutorialName}' has no stable ID.");
                else if (!ids.Add(tutorial.StableId)) issues.Add($"Duplicate Tutorial stable ID: '{tutorial.StableId}'.");
                if (tutorial.TargetType == TutorialTargetType.TwoDimensional && tutorial.TargetSource == TutorialTargetSource.DirectObject &&
                    tutorial.SpotlightTarget != null && tutorial.SpotlightTarget.GetComponent<RectTransform>() == null)
                    issues.Add($"Tutorial '{tutorial.TutorialName}' is 2D but its target has no RectTransform.");
                if (tutorial.CompletionType == TutorialCompletionType.Gesture && gameplayAdapter == null && !string.IsNullOrWhiteSpace(tutorial.GameplayActionId))
                    issues.Add($"Tutorial '{tutorial.TutorialName}' has Gameplay Action ID '{tutorial.GameplayActionId}' but no gameplay adapter.");
                if (tutorial.CompletionType == TutorialCompletionType.Event && string.IsNullOrWhiteSpace(tutorial.CompletionEventId))
                    issues.Add($"Tutorial '{tutorial.TutorialName}' uses Event completion but has an empty event ID.");
            }

            HashSet<string> sequenceNames = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> sequenceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (TutorialSequencerDefinition sequencer in tutorialSequencers)
            {
                if (sequencer == null) { issues.Add("Tutorial Sequencer list contains a null entry."); continue; }
                if (string.IsNullOrWhiteSpace(sequencer.SequenceName)) issues.Add("A Tutorial Sequencer has an empty Sequence Name.");
                else if (!sequenceNames.Add(sequencer.SequenceName)) issues.Add($"Duplicate Tutorial Sequencer name: '{sequencer.SequenceName}'.");
                if (!sequenceIds.Add(sequencer.StableId)) issues.Add($"Duplicate Tutorial Sequencer stable ID: '{sequencer.StableId}'.");
                foreach (TutorialSequenceEntry entry in sequencer.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.TutorialStableId) || !tutorialsById.ContainsKey(entry.TutorialStableId))
                        issues.Add($"Sequencer '{sequencer.SequenceName}' has an entry with a missing Tutorial reference.");
                    if (entry != null && entry.Trigger.UseYRange && entry.Trigger.RelativeYMinimum > entry.Trigger.RelativeYMaximum)
                        issues.Add($"Sequencer '{sequencer.SequenceName}' has an invalid Y range.");
                }
            }
            if (presentationPrefab == null) issues.Add("Presentation Prefab is not assigned.");
            return issues;
        }

        private void InitializeAutomaticSequencers()
        {
            if (tutorialSequencers.Count == 0) return;
            BuildVariableDependencyIndex();
            if (variableProvider != null) variableProvider.VariableChanged += HandleVariableChanged;

            foreach (TutorialSequencerDefinition sequencer in tutorialSequencers)
            {
                if (sequencer == null || !sequencer.Enabled || !CanRun(sequencer)) continue;
                ArmSequencerStart(sequencer);
            }
        }

        private void ArmSequencerStart(TutorialSequencerDefinition sequencer)
        {
            TutorialTriggerDefinition trigger = sequencer.StartTrigger;
            switch (trigger.TriggerType)
            {
                case TutorialTriggerType.Immediate:
                    TryStartSequencerInternal(sequencer, false);
                    break;
                case TutorialTriggerType.Time:
                    startWatchers[sequencer] = StartCoroutine(WaitForStartTime(sequencer));
                    break;
                case TutorialTriggerType.Distance:
                    startWatchers[sequencer] = StartCoroutine(WaitForStartDistance(sequencer));
                    break;
                case TutorialTriggerType.VisibleOnScreen:
                    startWatchers[sequencer] = StartCoroutine(WaitForStartVisibility(sequencer));
                    break;
                case TutorialTriggerType.VariableCondition:
                    EvaluateSequencerVariableStart(sequencer);
                    break;
            }
        }

        private bool TryStartSequencerInternal(TutorialSequencerDefinition sequencer, bool manualOverride)
        {
            if (sequencer == null || !sequencer.Enabled || activeSequencer != null || (!manualOverride && !CanRun(sequencer))) return false;
            StopStartWatcher(sequencer);
            activeSequencer = sequencer;
            activeEntryIndex = TutorialSequenceLogic.FindNextEnabledEntry(sequencer.Entries, -1);
            sequencerScaledStartTime = Time.time;
            sequencerUnscaledStartTime = Time.unscaledTime;
            previousCompletionScaledTime = sequencerScaledStartTime;
            previousCompletionUnscaledTime = sequencerUnscaledStartTime;
            if (sequencer.LockGameplayBetweenTutorials) SetGameplayBlocked(true);
            SequencerStarted?.Invoke(sequencer);
            if (activeEntryIndex < 0) { CompleteSequencer(true); return true; }
            ArmActiveEntry();
            return true;
        }

        private void ArmActiveEntry()
        {
            StopActiveEntryWatcher();
            if (activeSequencer == null || activeEntryIndex < 0) return;
            TutorialSequenceEntry entry = activeSequencer.Entries[activeEntryIndex];
            TutorialTriggerDefinition trigger = entry.Trigger;
            switch (trigger.TriggerType)
            {
                case TutorialTriggerType.Immediate: ActivatePendingEntry(); break;
                case TutorialTriggerType.Time: activeEntryWatcher = StartCoroutine(WaitForEntryTime()); break;
                case TutorialTriggerType.Distance: activeEntryWatcher = StartCoroutine(WaitForEntryDistance()); break;
                case TutorialTriggerType.VisibleOnScreen: activeEntryWatcher = StartCoroutine(WaitForEntryVisibility()); break;
                case TutorialTriggerType.VariableCondition: EvaluateActiveEntryVariable(); break;
            }
        }

        private void ActivatePendingEntry()
        {
            StopActiveEntryWatcher();
            if (activeSequencer == null || activeEntryIndex < 0 || currentTutorial != null) return;
            TutorialSequenceEntry entry = activeSequencer.Entries[activeEntryIndex];
            if (!tutorialsById.TryGetValue(entry.TutorialStableId ?? string.Empty, out TutorialDefinition tutorial) || tutorial == null || !tutorial.Enabled)
            {
                WarnOnce($"bad-entry:{activeSequencer.StableId}:{activeEntryIndex}", $"[ELROI Tutorials] Sequencer '{activeSequencer.SequenceName}' entry {activeEntryIndex} has no enabled Tutorial.");
                AdvanceSequencer();
                return;
            }
            BeginTutorial(tutorial, default, true);
        }

        private bool BeginTutorial(TutorialDefinition tutorial, TutorialContext context, bool fromSequencer)
        {
            if (tutorial == null || !tutorial.Enabled || currentTutorial != null || completionInProgress) return false;
            if (presentation == null) { WarnOnce("missing-presentation", "[ELROI Tutorials] Cannot play because no presentation prefab is assigned or instantiable."); return false; }

            GameObject resolvedTarget = ResolveTutorialTarget(tutorial, context);
            if (tutorial.TargetType != TutorialTargetType.None && resolvedTarget == null)
            {
                WarnOnce($"missing-target:{tutorial.StableId}", $"[ELROI Tutorials] Tutorial '{tutorial.TutorialName}' could not resolve its configured target.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(tutorial.GameplayActionId) && gameplayAdapter != null &&
                !gameplayAdapter.CanExecuteGameplayAction(tutorial.GameplayActionId, out string reason))
            {
                WarnOnce($"bad-action:{tutorial.GameplayActionId}", $"[ELROI Tutorials] Gameplay action '{tutorial.GameplayActionId}' is unavailable: {reason}");
            }

            currentTutorial = tutorial;
            SetGameplayBlocked(true);
            if (tutorial.FreezeWorld) freezeService.Freeze();
            presentation.Show(tutorial, resolvedTarget, gameplayCamera != null ? gameplayCamera : Camera.main, theme, gestureSettings,
                () => RequestTutorialCompletion(false), () => RequestTutorialCompletion(true));
            TutorialStarted?.Invoke(tutorial);
            return true;
        }

        private void RequestTutorialCompletion(bool executeGameplayAction)
        {
            if (currentTutorial == null || completionInProgress) return;
            StartCoroutine(CompleteTutorialRoutine(executeGameplayAction));
        }

        private IEnumerator CompleteTutorialRoutine(bool executeGameplayAction)
        {
            completionInProgress = true;
            TutorialDefinition completed = currentTutorial;
            string actionId = executeGameplayAction ? completed.GameplayActionId : null;
            presentation?.Hide();
            freezeService.Restore();

            if (completed.MarkComplete && persistenceProvider != null) persistenceProvider.MarkTutorialComplete(completed.StableId);
            currentTutorial = null;
            TutorialCompleted?.Invoke(completed);

            if (!string.IsNullOrWhiteSpace(actionId))
            {
                if (gameplayAdapter == null)
                {
                    WarnOnce($"no-adapter:{actionId}", $"[ELROI Tutorials] Gesture completed, but no gameplay adapter can execute '{actionId}'.");
                }
                else if (gameplayAdapter.CanExecuteGameplayAction(actionId, out string reason))
                {
                    if (gameplayAdapter is ITutorialAsyncGameplayAdapter asyncAdapter)
                    {
                        IEnumerator actionRoutine = asyncAdapter.ExecuteGameplayActionRoutine(actionId);
                        if (actionRoutine != null) yield return actionRoutine;
                    }
                    else
                    {
                        gameplayAdapter.ExecuteGameplayAction(actionId);
                    }
                }
                else
                {
                    WarnOnce($"action-rejected:{actionId}", $"[ELROI Tutorials] Gameplay action '{actionId}' was rejected: {reason}");
                }
            }

            // Keep normal input locked until the completing pointer release has passed every Update/EventSystem phase.
            yield return new WaitForEndOfFrame();
            yield return null;
            completionInProgress = false;
            previousCompletionScaledTime = Time.time;
            previousCompletionUnscaledTime = Time.unscaledTime;

            if (activeSequencer != null) AdvanceSequencer();
            else
            {
                SetGameplayBlocked(false);
                EvaluateShutdownPolicy();
            }
        }

        private void AdvanceSequencer()
        {
            int next = TutorialSequenceLogic.FindNextEnabledEntry(activeSequencer.Entries, activeEntryIndex);
            if (next < 0) { CompleteSequencer(true); return; }
            activeEntryIndex = next;
            if (!activeSequencer.LockGameplayBetweenTutorials) SetGameplayBlocked(false);
            else SetGameplayBlocked(true);
            ArmActiveEntry();
        }

        private void CompleteSequencer(bool markComplete)
        {
            TutorialSequencerDefinition completed = activeSequencer;
            StopActiveEntryWatcher();
            activeSequencer = null;
            activeEntryIndex = -1;
            if (completed != null && markComplete)
            {
                sceneCompletedSequencers.Add(completed.StableId);
                SessionCompletedSequencers.Add(completed.StableId);
                if (completed.RunPolicy == TutorialRunPolicy.OnceEver && persistenceProvider != null)
                    persistenceProvider.MarkSequencerComplete(completed.StableId);
                SequencerCompleted?.Invoke(completed);
            }
            SetGameplayBlocked(false);
            EvaluateShutdownPolicy();
        }

        private bool CanRun(TutorialSequencerDefinition sequencer)
        {
            return TutorialRunPolicyLogic.CanRun(
                sequencer.RunPolicy,
                sceneCompletedSequencers.Contains(sequencer.StableId),
                SessionCompletedSequencers.Contains(sequencer.StableId),
                persistenceProvider != null && persistenceProvider.IsSequencerComplete(sequencer.StableId));
        }

        private bool IsTutorialPersistedComplete(TutorialDefinition tutorial) => persistenceProvider != null && persistenceProvider.IsTutorialComplete(tutorial.StableId);

        private IEnumerator WaitForStartTime(TutorialSequencerDefinition sequencer)
        {
            yield return WaitForTime(sequencer.StartTrigger, managerScaledStartTime, managerUnscaledStartTime);
            startWatchers.Remove(sequencer);
            TryStartSequencerInternal(sequencer, false);
        }

        private IEnumerator WaitForEntryTime()
        {
            TutorialTriggerDefinition trigger = activeSequencer.Entries[activeEntryIndex].Trigger;
            float scaledOrigin = trigger.TimeOrigin == TutorialTimeOrigin.ManagerStart ? managerScaledStartTime :
                trigger.TimeOrigin == TutorialTimeOrigin.SequencerStart ? sequencerScaledStartTime : previousCompletionScaledTime;
            float unscaledOrigin = trigger.TimeOrigin == TutorialTimeOrigin.ManagerStart ? managerUnscaledStartTime :
                trigger.TimeOrigin == TutorialTimeOrigin.SequencerStart ? sequencerUnscaledStartTime : previousCompletionUnscaledTime;
            yield return WaitForTime(trigger, scaledOrigin, unscaledOrigin);
            activeEntryWatcher = null;
            ActivatePendingEntry();
        }

        private static IEnumerator WaitForTime(TutorialTriggerDefinition trigger, float scaledOrigin, float unscaledOrigin)
        {
            while ((trigger.UseUnscaledTime ? Time.unscaledTime - unscaledOrigin : Time.time - scaledOrigin) < trigger.DelaySeconds)
                yield return null;
        }

        private IEnumerator WaitForStartDistance(TutorialSequencerDefinition sequencer)
        {
            TutorialTriggerDefinition trigger = sequencer.StartTrigger;
            while (!EvaluateDistance(trigger)) yield return null;
            startWatchers.Remove(sequencer);
            TryStartSequencerInternal(sequencer, false);
        }

        private IEnumerator WaitForEntryDistance()
        {
            TutorialTriggerDefinition trigger = activeSequencer.Entries[activeEntryIndex].Trigger;
            while (!EvaluateDistance(trigger)) yield return null;
            activeEntryWatcher = null;
            ActivatePendingEntry();
        }

        private IEnumerator WaitForStartVisibility(TutorialSequencerDefinition sequencer)
        {
            TutorialTriggerDefinition trigger = sequencer.StartTrigger;
            TutorialVisibilityDelay delay = new TutorialVisibilityDelay();
            while (!delay.Step(EvaluateVisibility(trigger), Time.unscaledDeltaTime, trigger.VisibilityDelay)) yield return null;
            startWatchers.Remove(sequencer);
            TryStartSequencerInternal(sequencer, false);
        }

        private IEnumerator WaitForEntryVisibility()
        {
            TutorialTriggerDefinition trigger = activeSequencer.Entries[activeEntryIndex].Trigger;
            TutorialVisibilityDelay delay = new TutorialVisibilityDelay();
            while (!delay.Step(EvaluateVisibility(trigger), Time.unscaledDeltaTime, trigger.VisibilityDelay)) yield return null;
            activeEntryWatcher = null;
            ActivatePendingEntry();
        }

        private bool EvaluateDistance(TutorialTriggerDefinition trigger)
        {
            GameObject reference = ResolveTriggerObject(trigger.ReferenceObject, trigger.ReferenceTargetId);
            GameObject target = ResolveTriggerObject(trigger.TargetObject, trigger.TargetId);
            return reference != null && target != null && TutorialSpatialLogic.IsWithinDistance(reference.transform.position, target.transform.position,
                trigger.MaximumAbsoluteXDistance, trigger.UseYRange, trigger.RelativeYMinimum, trigger.RelativeYMaximum);
        }

        private bool EvaluateVisibility(TutorialTriggerDefinition trigger)
        {
            GameObject target = ResolveTriggerObject(trigger.TargetObject, trigger.TargetId);
            if (target == null) return false;
            TutorialTargetType type = target.GetComponent<RectTransform>() != null ? TutorialTargetType.TwoDimensional : TutorialTargetType.ThreeDimensional;
            Camera camera = trigger.VisibilityCamera != null ? trigger.VisibilityCamera : gameplayCamera != null ? gameplayCamera : Camera.main;
            return TutorialScreenUtility.IsActuallyVisible(target, type, camera);
        }

        private GameObject ResolveTriggerObject(GameObject direct, string targetId)
        {
            if (direct != null) return direct;
            return TutorialTargetRegistry.TryResolve(targetId, out GameObject resolved) ? resolved : null;
        }

        private void EvaluateSequencerVariableStart(TutorialSequencerDefinition sequencer)
        {
            if (TutorialConditionEvaluator.EvaluateProvider(variableProvider, sequencer.StartTrigger.VariableCondition, out string error))
                TryStartSequencerInternal(sequencer, false);
            else if (!string.IsNullOrWhiteSpace(error) && !error.StartsWith("Unknown tutorial variable", StringComparison.Ordinal))
                WarnOnce($"sequence-condition:{sequencer.StableId}:{error}", $"[ELROI Tutorials] Sequencer '{sequencer.SequenceName}' condition: {error}");
        }

        private void EvaluateActiveEntryVariable()
        {
            if (activeSequencer == null || activeEntryIndex < 0) return;
            TutorialVariableCondition condition = activeSequencer.Entries[activeEntryIndex].Trigger.VariableCondition;
            if (TutorialConditionEvaluator.EvaluateProvider(variableProvider, condition, out string error)) ActivatePendingEntry();
            else if (!string.IsNullOrWhiteSpace(error) && !error.StartsWith("Unknown tutorial variable", StringComparison.Ordinal))
                WarnOnce($"entry-condition:{activeSequencer.StableId}:{activeEntryIndex}:{error}", $"[ELROI Tutorials] Sequencer '{activeSequencer.SequenceName}' entry condition: {error}");
        }

        private void HandleVariableChanged(string variableId, TutorialValue value)
        {
            if (activeSequencer != null && currentTutorial == null && activeEntryIndex >= 0)
            {
                TutorialTriggerDefinition trigger = activeSequencer.Entries[activeEntryIndex].Trigger;
                if (trigger.TriggerType == TutorialTriggerType.VariableCondition && string.Equals(trigger.VariableCondition.VariableId, variableId, StringComparison.Ordinal))
                    EvaluateActiveEntryVariable();
            }

            if (!startVariableDependencies.TryGetValue(variableId, out List<TutorialSequencerDefinition> dependents)) return;
            foreach (TutorialSequencerDefinition sequencer in dependents)
                if (sequencer != activeSequencer && sequencer.Enabled && CanRun(sequencer)) EvaluateSequencerVariableStart(sequencer);
        }

        private void BuildVariableDependencyIndex()
        {
            startVariableDependencies.Clear();
            foreach (TutorialSequencerDefinition sequencer in tutorialSequencers)
            {
                if (sequencer == null || sequencer.StartTrigger.TriggerType != TutorialTriggerType.VariableCondition) continue;
                string id = sequencer.StartTrigger.VariableCondition.VariableId;
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!startVariableDependencies.TryGetValue(id, out List<TutorialSequencerDefinition> list))
                {
                    list = new List<TutorialSequencerDefinition>();
                    startVariableDependencies.Add(id, list);
                }
                list.Add(sequencer);
            }
        }

        private GameObject ResolveTutorialTarget(TutorialDefinition tutorial, TutorialContext context)
        {
            switch (tutorial.TargetSource)
            {
                case TutorialTargetSource.DirectObject: return tutorial.SpotlightTarget;
                case TutorialTargetSource.RuntimeTargetId:
                    return TutorialTargetRegistry.TryResolve(tutorial.RuntimeTargetId, out GameObject resolved) ? resolved : null;
                case TutorialTargetSource.RuntimeContextTarget: return context.Target;
                default: return null;
            }
        }

        private void ResolveProviders()
        {
            gameplayAdapter = gameplayAdapterComponent as ITutorialGameplayAdapter;
            variableProvider = variableProviderComponent as ITutorialVariableProvider;
            persistenceProvider = persistenceProviderComponent as ITutorialPersistenceProvider;
        }

        private void CreatePresentation()
        {
            if (presentationPrefab == null) return;
            presentation = Instantiate(presentationPrefab, transform);
            presentation.name = presentationPrefab.name;
            presentation.Hide();
        }

        private void BuildIndexes()
        {
            EnsureStableIds();
            tutorialsByName.Clear();
            tutorialsById.Clear();
            sequencersByName.Clear();
            foreach (TutorialDefinition tutorial in tutorials)
            {
                if (tutorial == null) continue;
                if (!string.IsNullOrWhiteSpace(tutorial.TutorialName) && !tutorialsByName.ContainsKey(tutorial.TutorialName)) tutorialsByName.Add(tutorial.TutorialName, tutorial);
                if (!string.IsNullOrWhiteSpace(tutorial.StableId) && !tutorialsById.ContainsKey(tutorial.StableId)) tutorialsById.Add(tutorial.StableId, tutorial);
            }
            foreach (TutorialSequencerDefinition sequencer in tutorialSequencers)
                if (sequencer != null && !string.IsNullOrWhiteSpace(sequencer.SequenceName) && !sequencersByName.ContainsKey(sequencer.SequenceName)) sequencersByName.Add(sequencer.SequenceName, sequencer);
        }

        private void EnsureRuntimeIndexes()
        {
            if (tutorialsByName.Count == 0 && tutorials.Count > 0) BuildIndexes();
        }

        private void EnsureStableIds()
        {
            foreach (TutorialDefinition tutorial in tutorials) tutorial?.EnsureStableId();
            foreach (TutorialSequencerDefinition sequencer in tutorialSequencers) sequencer?.EnsureStableId();
        }

        private void SetGameplayBlocked(bool blocked)
        {
            if (inputBlocked == blocked) return;
            inputBlocked = blocked;
            gameplayAdapter?.SetGameplayInputBlocked(blocked);
        }

        private void StopStartWatcher(TutorialSequencerDefinition sequencer)
        {
            if (!startWatchers.TryGetValue(sequencer, out Coroutine watcher)) return;
            if (watcher != null) StopCoroutine(watcher);
            startWatchers.Remove(sequencer);
        }

        private void StopActiveEntryWatcher()
        {
            if (activeEntryWatcher != null) StopCoroutine(activeEntryWatcher);
            activeEntryWatcher = null;
        }

        private void StopAllActiveWatchers()
        {
            StopActiveEntryWatcher();
            foreach (Coroutine watcher in startWatchers.Values) if (watcher != null) StopCoroutine(watcher);
            startWatchers.Clear();
        }

        private void CleanupRuntimeState()
        {
            if (variableProvider != null) variableProvider.VariableChanged -= HandleVariableChanged;
            StopAllActiveWatchers();
            presentation?.Hide();
            freezeService.Restore();
            if (inputBlocked)
            {
                inputBlocked = false;
                gameplayAdapter?.SetGameplayInputBlocked(false);
            }
        }

        private bool HasAnyEnabledContent()
        {
            foreach (TutorialDefinition tutorial in tutorials) if (tutorial != null && tutorial.Enabled) return true;
            foreach (TutorialSequencerDefinition sequencer in tutorialSequencers) if (sequencer != null && sequencer.Enabled) return true;
            return false;
        }

        private void EvaluateShutdownPolicy()
        {
            if (lifecyclePolicy == TutorialLifecyclePolicy.StayEnabled || whenAutomaticWorkCompletes != TutorialAutomaticCompletionPolicy.DisableComponent) return;
            bool callableTutorial = false;
            foreach (TutorialDefinition tutorial in tutorials)
            {
                if (tutorial == null || !tutorial.Enabled) continue;
                if (tutorial.ManualRunPolicy == TutorialManualRunPolicy.AlwaysAllow || !IsTutorialPersistedComplete(tutorial)) { callableTutorial = true; break; }
            }
            if (!callableTutorial && activeSequencer == null && startWatchers.Count == 0) SafeDisableComponent();
        }

        private void SafeDisableComponent()
        {
            CleanupRuntimeState();
            enabled = false;
        }

        private void WarnOnce(string key, string message)
        {
            if (warnedMessages.Add(key)) Debug.LogWarning(message, this);
        }

        // Used by the original ELROI demo/installer and by automated tests. It deliberately preserves stable IDs.
        public List<TutorialDefinition> MutableTutorialsForAuthoring => tutorials;
        public List<TutorialSequencerDefinition> MutableSequencersForAuthoring => tutorialSequencers;
        public void ConfigureProvidersForAuthoring(TutorialPresentation prefab, TutorialTheme selectedTheme, Camera camera,
            MonoBehaviour adapter, MonoBehaviour variables, MonoBehaviour persistence)
        {
            presentationPrefab = prefab;
            theme = selectedTheme;
            gameplayCamera = camera;
            gameplayAdapterComponent = adapter;
            variableProviderComponent = variables;
            persistenceProviderComponent = persistence;
        }
    }
}
