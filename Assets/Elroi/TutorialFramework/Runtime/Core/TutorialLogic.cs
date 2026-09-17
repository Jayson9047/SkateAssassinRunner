using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Tutorials
{
    public static class TutorialConditionEvaluator
    {
        public static bool TryEvaluate(TutorialValue actual, TutorialComparisonOperator operation, TutorialValue expected, out bool result, out string error)
        {
            result = false;
            error = null;
            bool numeric = (actual.Type == TutorialVariableType.Integer || actual.Type == TutorialVariableType.Float) &&
                           (expected.Type == TutorialVariableType.Integer || expected.Type == TutorialVariableType.Float);

            if (numeric)
            {
                double left = actual.Type == TutorialVariableType.Integer ? actual.IntegerValue : actual.FloatValue;
                double right = expected.Type == TutorialVariableType.Integer ? expected.IntegerValue : expected.FloatValue;
                switch (operation)
                {
                    case TutorialComparisonOperator.Equal: result = Math.Abs(left - right) <= 0.00001d; return true;
                    case TutorialComparisonOperator.NotEqual: result = Math.Abs(left - right) > 0.00001d; return true;
                    case TutorialComparisonOperator.Greater: result = left > right; return true;
                    case TutorialComparisonOperator.GreaterOrEqual: result = left >= right; return true;
                    case TutorialComparisonOperator.Less: result = left < right; return true;
                    case TutorialComparisonOperator.LessOrEqual: result = left <= right; return true;
                }
            }

            if (actual.Type != expected.Type)
            {
                error = $"Cannot compare {actual.Type} with {expected.Type}.";
                return false;
            }

            if (operation != TutorialComparisonOperator.Equal && operation != TutorialComparisonOperator.NotEqual)
            {
                error = $"{operation} is only valid for numeric tutorial variables.";
                return false;
            }

            bool equal = actual.Equals(expected);
            result = operation == TutorialComparisonOperator.Equal ? equal : !equal;
            return true;
        }

        public static bool EvaluateProvider(ITutorialVariableProvider provider, TutorialVariableCondition condition, out string error)
        {
            error = null;
            if (provider == null) { error = "No tutorial variable provider is assigned."; return false; }
            if (condition == null || string.IsNullOrWhiteSpace(condition.VariableId)) { error = "Variable condition has an empty variable ID."; return false; }
            if (!provider.TryGetValue(condition.VariableId, out TutorialValue actual)) { error = $"Unknown tutorial variable '{condition.VariableId}'."; return false; }
            return TryEvaluate(actual, condition.Comparison, condition.ExpectedValue, out bool result, out error) && result;
        }
    }

    public sealed class TutorialGestureRecognizer
    {
        private Vector2 downPosition;
        private float downTime;
        private bool tracking;
        private float firstTapTime = -1f;

        public void PointerDown(Vector2 position, float unscaledTime)
        {
            downPosition = position;
            downTime = unscaledTime;
            tracking = true;
        }

        public bool PointerUp(Vector2 position, float unscaledTime, TutorialGesture required, TutorialGestureSettings settings)
        {
            if (!tracking) return false;
            tracking = false;
            Vector2 delta = position - downPosition;
            float distance = delta.magnitude;

            if (required == TutorialGesture.Tap)
                return distance <= settings.TapMovementTolerance;

            if (required == TutorialGesture.DoubleTap)
            {
                if (distance > settings.TapMovementTolerance) { firstTapTime = -1f; return false; }
                if (firstTapTime >= 0f && unscaledTime - firstTapTime <= settings.DoubleTapMaximumInterval)
                {
                    firstTapTime = -1f;
                    return true;
                }
                firstTapTime = unscaledTime;
                return false;
            }

            if (distance < settings.SwipeMinimumDistance) return false;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return required == (delta.x >= 0f ? TutorialGesture.SwipeRight : TutorialGesture.SwipeLeft);
            return required == (delta.y >= 0f ? TutorialGesture.SwipeUp : TutorialGesture.SwipeDown);
        }

        public void Reset()
        {
            tracking = false;
            firstTapTime = -1f;
        }
    }

    public static class TutorialSpatialLogic
    {
        public static bool IsWithinDistance(Vector3 reference, Vector3 target, float maxAbsoluteX, bool useYRange, float relativeYMin, float relativeYMax)
        {
            if (Mathf.Abs(target.x - reference.x) > Mathf.Max(0f, maxAbsoluteX)) return false;
            if (!useYRange) return true;
            float relativeY = target.y - reference.y;
            return relativeY >= relativeYMin && relativeY <= relativeYMax;
        }
    }

    public sealed class TutorialVisibilityDelay
    {
        private float visibleDuration;
        public float VisibleDuration => visibleDuration;

        public bool Step(bool visible, float deltaTime, float requiredDelay)
        {
            if (!visible) { visibleDuration = 0f; return false; }
            visibleDuration += Mathf.Max(0f, deltaTime);
            return visibleDuration >= Mathf.Max(0f, requiredDelay);
        }

        public void Reset() => visibleDuration = 0f;
    }

    public static class TutorialSequenceLogic
    {
        public static int FindNextEnabledEntry(IReadOnlyList<TutorialSequenceEntry> entries, int afterIndex)
        {
            if (entries == null) return -1;
            for (int i = Mathf.Max(0, afterIndex + 1); i < entries.Count; i++)
                if (entries[i] != null && entries[i].Enabled) return i;
            return -1;
        }

        public static bool IsFinalEnabledEntry(IReadOnlyList<TutorialSequenceEntry> entries, int index) => FindNextEnabledEntry(entries, index) < 0;
    }

    public static class TutorialRunPolicyLogic
    {
        public static bool CanRun(TutorialRunPolicy policy, bool completedThisScene, bool completedThisSession, bool persistedComplete)
        {
            switch (policy)
            {
                case TutorialRunPolicy.EveryTime: return true;
                case TutorialRunPolicy.OncePerSceneLoad: return !completedThisScene;
                case TutorialRunPolicy.OncePerSession: return !completedThisSession;
                case TutorialRunPolicy.OnceEver: return !persistedComplete;
                default: return true;
            }
        }
    }

    public sealed class TimeScaleTutorialFreeze : ITutorialFreezeService
    {
        private float previousTimeScale;
        public bool IsFrozen { get; private set; }

        public void Freeze()
        {
            if (IsFrozen) return;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            IsFrozen = true;
        }

        public void Restore()
        {
            if (!IsFrozen) return;
            Time.timeScale = previousTimeScale;
            IsFrozen = false;
        }
    }
}
