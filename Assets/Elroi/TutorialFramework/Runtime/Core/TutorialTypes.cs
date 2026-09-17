using System;
using UnityEngine;

namespace Elroi.Tutorials
{
    public enum TutorialTargetType { None, TwoDimensional, ThreeDimensional }
    public enum TutorialTargetSource { DirectObject, RuntimeTargetId, RuntimeContextTarget }
    public enum TutorialCompletionType { OkButton, Gesture, Event, Manual }
    public enum TutorialGesture { None, Tap, DoubleTap, SwipeLeft, SwipeRight, SwipeUp, SwipeDown }
    public enum TutorialSpotlightShape { Rectangle, RoundedRectangle, Circle }
    public enum TutorialRunPolicy { EveryTime, OncePerSceneLoad, OncePerSession, OnceEver }
    public enum TutorialManualRunPolicy { AlwaysAllow, RespectCompletion }
    public enum TutorialTriggerType { Immediate, Time, Distance, VisibleOnScreen, VariableCondition, Event, Manual }
    public enum TutorialTimeOrigin { ManagerStart, SequencerStart, PreviousTutorialCompletion }
    public enum TutorialVariableType { Integer, Float, Boolean, String }
    public enum TutorialComparisonOperator { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
    public enum TutorialLifecyclePolicy { Automatic, StayEnabled, Manual }
    public enum TutorialAutomaticCompletionPolicy { StayIdle, DisableComponent, Manual }

    [Serializable]
    public struct TutorialContext
    {
        public GameObject Target;

        public TutorialContext(GameObject target)
        {
            Target = target;
        }
    }

    [Serializable]
    public struct TutorialGestureSettings
    {
        [Min(0f)] public float TapMovementTolerance;
        [Min(0f)] public float SwipeMinimumDistance;
        [Min(0.01f)] public float DoubleTapMaximumInterval;

        public static TutorialGestureSettings Default => new TutorialGestureSettings
        {
            TapMovementTolerance = 25f,
            SwipeMinimumDistance = 100f,
            DoubleTapMaximumInterval = 0.3f
        };
    }

    [Serializable]
    public struct TutorialValue : IEquatable<TutorialValue>
    {
        [SerializeField] private TutorialVariableType type;
        [SerializeField] private int integerValue;
        [SerializeField] private float floatValue;
        [SerializeField] private bool booleanValue;
        [SerializeField] private string stringValue;

        public TutorialVariableType Type => type;
        public int IntegerValue => integerValue;
        public float FloatValue => floatValue;
        public bool BooleanValue => booleanValue;
        public string StringValue => stringValue ?? string.Empty;

        public static TutorialValue From(int value) => new TutorialValue { type = TutorialVariableType.Integer, integerValue = value };
        public static TutorialValue From(float value) => new TutorialValue { type = TutorialVariableType.Float, floatValue = value };
        public static TutorialValue From(bool value) => new TutorialValue { type = TutorialVariableType.Boolean, booleanValue = value };
        public static TutorialValue From(string value) => new TutorialValue { type = TutorialVariableType.String, stringValue = value ?? string.Empty };

        public bool Equals(TutorialValue other)
        {
            if (type != other.type) return false;
            switch (type)
            {
                case TutorialVariableType.Integer: return integerValue == other.integerValue;
                case TutorialVariableType.Float: return Mathf.Approximately(floatValue, other.floatValue);
                case TutorialVariableType.Boolean: return booleanValue == other.booleanValue;
                case TutorialVariableType.String: return string.Equals(StringValue, other.StringValue, StringComparison.Ordinal);
                default: return false;
            }
        }

        public override bool Equals(object obj) => obj is TutorialValue other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(type, integerValue, floatValue, booleanValue, StringValue);
        public override string ToString()
        {
            switch (type)
            {
                case TutorialVariableType.Integer: return integerValue.ToString();
                case TutorialVariableType.Float: return floatValue.ToString("G");
                case TutorialVariableType.Boolean: return booleanValue.ToString();
                case TutorialVariableType.String: return StringValue;
                default: return string.Empty;
            }
        }
    }

    [Serializable]
    public sealed class TutorialVariableCondition
    {
        [SerializeField] private string variableId;
        [SerializeField] private TutorialComparisonOperator comparison;
        [SerializeField] private TutorialValue expectedValue;

        public string VariableId { get => variableId; set => variableId = value; }
        public TutorialComparisonOperator Comparison { get => comparison; set => comparison = value; }
        public TutorialValue ExpectedValue { get => expectedValue; set => expectedValue = value; }
    }
}
