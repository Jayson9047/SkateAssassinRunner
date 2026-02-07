using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AccuracyMeter
{
    public class AccuracyMeterArcDriver : MonoBehaviour
    {
        public enum MeterShape
        {
            Arc,
            Linear
        }

        [Header("Meter Shape")]
        [SerializeField] private MeterShape meterShape = MeterShape.Arc;

        [Header("Points (UI)")]
        [SerializeField] private RectTransform startPoint;
        [SerializeField] private RectTransform endPoint;

        [Tooltip("Only required for Arc meters.")]
        [SerializeField] private RectTransform centerPoint;

        [SerializeField] private RectTransform ticker;

        [Header("Partitions (UI)")]
        [Tooltip("Partition visuals in order (P0..Pn). Partitions are evenly spaced across [0..1].")]
        [SerializeField] private List<Graphic> partitionVisuals = new();

        [Header("Highlight (Optional)")]
        [SerializeField] private bool enableHoverHighlight = true;
        [Range(0f, 1f)]
        [SerializeField] private float inactiveAlpha = 0.45f;
        [Range(0f, 1f)]
        [SerializeField] private float activeAlpha = 1f;
        [SerializeField] private float inactiveScale = 1f;
        [SerializeField] private float activeScale = 1.08f;

        [SerializeField] private float highlightScaleTweenDuration = 0.12f;
        [SerializeField] private Ease highlightScaleEase = Ease.OutBack;

        [Header("Motion")]
        [Tooltip("Normalized units per second (1 = traverse full path in 1 second).")]
        [SerializeField] private float speed = 1.25f;

        [Tooltip("If true, ticker rotates to follow tangent (Arc) or direction (Linear).")]
        [SerializeField] private bool rotateTickerToTangent = true;

        [Header("Arc Selection (Arc only)")]
        [Tooltip("Use the shorter arc between start and end angles (recommended for meters).")]
        [SerializeField] private bool useShorterArc = true;
        [Tooltip("Flip traversal direction along the chosen arc.")]
        [SerializeField] private bool invertArcAngle = false;
        [Tooltip("If start and end radii differ from center, lerp radius across t instead of forcing a single radius.")]
        [SerializeField] private bool lerpRadiusIfMismatch = true;

        [Header("Demo Controls (can disable later)")]
        [SerializeField] private bool autoStartOnEnable = true;

        [Tooltip("If true, this component will poll input (key/mouse/touch) to stop. " +
                 "UI button stopping via RequestStop() works regardless of this flag.")]
        [SerializeField] private bool listenForInput = true;

        [SerializeField] private TMP_Text debugText;

        [Header("Stop Input (Configurable)")]
        [SerializeField] private bool stopOnKey = true;
        [SerializeField] private KeyCode stopKey = KeyCode.Space;

        [SerializeField] private bool stopOnMouseClick = true;
        [SerializeField] private bool stopOnTouch = true;

        [Tooltip("If true, a UI button can call RequestStop() to stop the meter.")]
        [SerializeField] private bool stopOnButtonRequest = true;

        [Serializable] public class PartitionHoverChangedEvent : UnityEvent<int, string, float> { } // idx, name, t01
        [Serializable] public class PartitionStopEvent : UnityEvent<AccuracyStopResult> { }

        [Tooltip("Fired when hover partition changes. (idx, partitionName, normalizedT)")]
        public PartitionHoverChangedEvent onHoverChanged = new PartitionHoverChangedEvent();

        [Tooltip("Fired when meter stops and lands.")]
        public PartitionStopEvent onStopped = new PartitionStopEvent();

        [Header("Partition Event Routing (No-Param)")]
        [SerializeField] private bool enablePartitionEvents = true;

        [Serializable]
        public class PartitionEvents
        {
            [Tooltip("Fires when ticker begins hovering this partition (i.e. hover changed to this index).")]
            public UnityEvent onHoverEnter = new UnityEvent();

            [Tooltip("Fires if StopMeter lands on this partition.")]
            public UnityEvent onStopLanded = new UnityEvent();
        }

        [SerializeField] private PartitionEvents[] partitionEvents;

        // base scale cache for smooth highlight scaling
        [SerializeField] private List<Vector3> _baseScales = new(); // same index as partitionVisuals
        private readonly List<Tween> _scaleTweens = new();

        // runtime
        private Tween _tween;
        private float _t; // 0..1
        private int _activePartitionIndex = -1;
        private bool _isRunning;

        // cached ARC data
        private Vector2 _c, _s, _e;
        private float _startAngle, _deltaAngle;
        private float _rStart, _rEnd;

        private void OnEnable()
        {
            EnsurePartitionEventsSize();

            if (autoStartOnEnable)
                StartMeter();
            else
                ApplyHighlight(-1);
        }

        private void OnDisable()
        {
            KillTween();
        }

        private void OnValidate()
        {
            EnsurePartitionEventsSize();
            if (!enableHoverHighlight)
                ApplyHighlight(-1);
        }

        private void EnsurePartitionEventsSize()
        {
            int n = partitionVisuals != null ? partitionVisuals.Count : 0;

            if (n <= 0)
            {
                partitionEvents = Array.Empty<PartitionEvents>();
                return;
            }

            if (partitionEvents == null || partitionEvents.Length != n)
            {
                var old = partitionEvents;
                partitionEvents = new PartitionEvents[n];

                for (int i = 0; i < n; i++)
                {
                    if (old != null && i < old.Length && old[i] != null)
                        partitionEvents[i] = old[i];
                    else
                        partitionEvents[i] = new PartitionEvents();
                }
            }
        }

        private void Update()
        {
            if (!listenForInput) return;

            if (_tween == null || !_tween.IsActive() || !_tween.IsPlaying())
                return;

            if (ShouldStopThisFrame())
                StopMeter();
        }

        private bool ShouldStopThisFrame()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (stopOnKey && Input.GetKeyDown(stopKey)) return true;
            if (stopOnMouseClick && Input.GetMouseButtonDown(0)) return true;
            if (stopOnTouch && LegacyAnyTouchBegan()) return true;
#endif

#if ENABLE_INPUT_SYSTEM
            if (stopOnKey && NewInputKeyDown(stopKey)) return true;

            if (stopOnMouseClick)
            {
                var m = Mouse.current;
                if (m != null && m.leftButton.wasPressedThisFrame) return true;
            }

            if (stopOnTouch)
            {
                var ts = Touchscreen.current;
                if (ts != null)
                {
                    if (ts.primaryTouch.press.wasPressedThisFrame) return true;
                    foreach (var touch in ts.touches)
                        if (touch.press.wasPressedThisFrame) return true;
                }
            }
#endif
            return false;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private bool LegacyAnyTouchBegan()
        {
            if (Input.touchCount <= 0) return false;
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == UnityEngine.TouchPhase.Began) return true;
            return false;
        }
#endif

#if ENABLE_INPUT_SYSTEM
        private bool NewInputKeyDown(KeyCode keyCode)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;

            switch (keyCode)
            {
                case KeyCode.Space: return kb.spaceKey.wasPressedThisFrame;
                case KeyCode.Return: return kb.enterKey.wasPressedThisFrame;
                case KeyCode.KeypadEnter: return kb.numpadEnterKey.wasPressedThisFrame;
                case KeyCode.Escape: return kb.escapeKey.wasPressedThisFrame;
                case KeyCode.Backspace: return kb.backspaceKey.wasPressedThisFrame;
                case KeyCode.Tab: return kb.tabKey.wasPressedThisFrame;

                case KeyCode.LeftArrow: return kb.leftArrowKey.wasPressedThisFrame;
                case KeyCode.RightArrow: return kb.rightArrowKey.wasPressedThisFrame;
                case KeyCode.UpArrow: return kb.upArrowKey.wasPressedThisFrame;
                case KeyCode.DownArrow: return kb.downArrowKey.wasPressedThisFrame;

                case KeyCode.Alpha0: return kb.digit0Key.wasPressedThisFrame;
                case KeyCode.Alpha1: return kb.digit1Key.wasPressedThisFrame;
                case KeyCode.Alpha2: return kb.digit2Key.wasPressedThisFrame;
                case KeyCode.Alpha3: return kb.digit3Key.wasPressedThisFrame;
                case KeyCode.Alpha4: return kb.digit4Key.wasPressedThisFrame;
                case KeyCode.Alpha5: return kb.digit5Key.wasPressedThisFrame;
                case KeyCode.Alpha6: return kb.digit6Key.wasPressedThisFrame;
                case KeyCode.Alpha7: return kb.digit7Key.wasPressedThisFrame;
                case KeyCode.Alpha8: return kb.digit8Key.wasPressedThisFrame;
                case KeyCode.Alpha9: return kb.digit9Key.wasPressedThisFrame;

                case KeyCode.A: return kb.aKey.wasPressedThisFrame;
                case KeyCode.B: return kb.bKey.wasPressedThisFrame;
                case KeyCode.C: return kb.cKey.wasPressedThisFrame;
                case KeyCode.D: return kb.dKey.wasPressedThisFrame;
                case KeyCode.E: return kb.eKey.wasPressedThisFrame;
                case KeyCode.F: return kb.fKey.wasPressedThisFrame;
                case KeyCode.G: return kb.gKey.wasPressedThisFrame;
                case KeyCode.H: return kb.hKey.wasPressedThisFrame;
                case KeyCode.I: return kb.iKey.wasPressedThisFrame;
                case KeyCode.J: return kb.jKey.wasPressedThisFrame;
                case KeyCode.K: return kb.kKey.wasPressedThisFrame;
                case KeyCode.L: return kb.lKey.wasPressedThisFrame;
                case KeyCode.M: return kb.mKey.wasPressedThisFrame;
                case KeyCode.N: return kb.nKey.wasPressedThisFrame;
                case KeyCode.O: return kb.oKey.wasPressedThisFrame;
                case KeyCode.P: return kb.pKey.wasPressedThisFrame;
                case KeyCode.Q: return kb.qKey.wasPressedThisFrame;
                case KeyCode.R: return kb.rKey.wasPressedThisFrame;
                case KeyCode.S: return kb.sKey.wasPressedThisFrame;
                case KeyCode.T: return kb.tKey.wasPressedThisFrame;
                case KeyCode.U: return kb.uKey.wasPressedThisFrame;
                case KeyCode.V: return kb.vKey.wasPressedThisFrame;
                case KeyCode.W: return kb.wKey.wasPressedThisFrame;
                case KeyCode.X: return kb.xKey.wasPressedThisFrame;
                case KeyCode.Y: return kb.yKey.wasPressedThisFrame;
                case KeyCode.Z: return kb.zKey.wasPressedThisFrame;

                default:
                    return false;
            }
        }
#endif

        /// <summary>
        /// Wire a UI button OnClick() to this if you want stop-by-button.
        /// Stops immediately (does not rely on Update or listenForInput).
        /// </summary>
        public void RequestStop()
        {
            if (!stopOnButtonRequest) return;

            if (_tween == null || !_tween.IsActive() || !_tween.IsPlaying())
                return;

            StopMeter();
        }

        public void StartMeter()
        {
            if (!ValidateRefs()) return;

            EnsurePartitionEventsSize();

            if (meterShape == MeterShape.Arc)
                CacheArc();

            ApplyHighlight(-1);
            _activePartitionIndex = -1;

            float duration = Mathf.Max(0.05f, 1f / Mathf.Max(0.01f, speed));

            KillTween();
            _t = 0f;

            _isRunning = true;

            _tween = DOVirtual.Float(0f, 1f, duration, value =>
            {
                _t = value;
                UpdateTicker(_t);
                UpdateHoverPartition(_t);
            })
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);

            if (debugText) debugText.text = "RUNNING...";
        }

        public AccuracyStopResult StopMeter()
        {
            if (_tween == null || !_tween.IsActive() || !_tween.IsPlaying())
                return AccuracyStopResult.Invalid("No active tween.");

            _isRunning = false;
            _tween.Pause();

            int idx = GetPartitionIndex(_t);
            string name = GetPartitionName(idx);

            var result = new AccuracyStopResult
            {
                IsValid = idx >= 0,
                PartitionIndex = idx,
                PartitionName = name,
                NormalizedT = Mathf.Clamp01(_t),
            };

            if (debugText)
                debugText.text = result.IsValid
                    ? $"STOP: {result.PartitionName} (idx {result.PartitionIndex}) t={result.NormalizedT:0.000}"
                    : $"STOP: INVALID t={result.NormalizedT:0.000}";

            onStopped?.Invoke(result);

            if (enablePartitionEvents && result.IsValid)
            {
                if (partitionEvents != null && idx >= 0 && idx < partitionEvents.Length)
                    partitionEvents[idx]?.onStopLanded?.Invoke();
            }

            return result;
        }

        public void ResumeMeter()
        {
            if (!ValidateRefs()) return;
            if (_tween == null) { StartMeter(); return; }

            _isRunning = true;
            _tween.Play();
            if (debugText) debugText.text = "RUNNING...";
        }

        public void ResetMeter()
        {
            if (!ValidateRefs()) return;

            EnsurePartitionEventsSize();

            if (meterShape == MeterShape.Arc)
                CacheArc();

            _t = 0f;
            UpdateTicker(_t);

            ApplyHighlight(-1);
            _activePartitionIndex = -1;

            if (debugText) debugText.text = "RESET";
        }

        private void CacheArc()
        {
            // IMPORTANT: rect space is whatever your UI uses (anchoredPosition).
            // Your original implementation used: _c = -centerPoint.anchoredPosition
            // Keeping it exactly.
            _c = -centerPoint.anchoredPosition;
            _s = startPoint.anchoredPosition;
            _e = endPoint.anchoredPosition;

            if(invertArcAngle)
            {
                _c = centerPoint.anchoredPosition;
            }

            Vector2 vs = _s - _c;
            Vector2 ve = _e - _c;

            _rStart = vs.magnitude;
            _rEnd = ve.magnitude;

            _startAngle = Mathf.Atan2(vs.y, vs.x);

            float endAngle = Mathf.Atan2(ve.y, ve.x);
            float rawDelta = endAngle - _startAngle;

            float shortDelta = Mathf.Atan2(Mathf.Sin(rawDelta), Mathf.Cos(rawDelta));
            float longDelta = shortDelta > 0 ? shortDelta - (Mathf.PI * 2f) : shortDelta + (Mathf.PI * 2f);

            _deltaAngle = useShorterArc ? shortDelta : longDelta;
        }

        private void UpdateTicker(float t01)
        {
            float t = Mathf.Clamp01(t01);
            if (meterShape == MeterShape.Linear)
            {
                Vector2 s = startPoint.anchoredPosition;
                Vector2 e = endPoint.anchoredPosition;

                Vector2 pos = Vector2.LerpUnclamped(s, e, t);
                ticker.anchoredPosition = pos;

                // IMPORTANT:
                // Do NOT touch ticker.localRotation here.
                // User-defined rotation stays as-is.
                return;
            }

            // ARC
            float angle = _startAngle + (_deltaAngle * t);

            float radius;
            if (lerpRadiusIfMismatch)
                radius = Mathf.Lerp(_rStart, _rEnd, t);
            else
                radius = _rStart;

            Vector2 posArc = _c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            ticker.anchoredPosition = posArc;

            if (rotateTickerToTangent)
            {
                float dirSign = Mathf.Approximately(_deltaAngle, 0f) ? 1f : Mathf.Sign(_deltaAngle);
                float tangentAngle = angle + (dirSign * Mathf.PI * 0.5f);
                float zDeg = tangentAngle * Mathf.Rad2Deg;
                ticker.localRotation = Quaternion.Euler(0f, 0f, zDeg);
            }
        }

        private void UpdateHoverPartition(float t01)
        {
            int idx = GetPartitionIndex(t01);

            // Hover change detection should ALWAYS run (logic layer)
            if (idx == _activePartitionIndex)
                return;

            _activePartitionIndex = idx;

            // Only the visual highlight is gated by enableHoverHighlight
            if (enableHoverHighlight)
                ApplyHighlight(idx);

            // Events should NOT be blocked by enableHoverHighlight
            string name = GetPartitionName(idx);
            float t = Mathf.Clamp01(t01);
            onHoverChanged?.Invoke(idx, name, t);

            if (enablePartitionEvents)
            {
                if (partitionEvents != null && idx >= 0 && idx < partitionEvents.Length)
                    partitionEvents[idx]?.onHoverEnter?.Invoke();
            }
        }

        private int GetPartitionIndex(float t01)
        {
            if (partitionVisuals == null || partitionVisuals.Count == 0) return -1;

            float t = Mathf.Clamp01(t01);
            int n = partitionVisuals.Count;

            float seg = 1f / n;

            int idx = Mathf.Min(n - 1, Mathf.FloorToInt(t / seg));
            return idx;
        }

        private string GetPartitionName(int idx)
        {
            if (idx < 0 || partitionVisuals == null || idx >= partitionVisuals.Count)
                return "Invalid";

            return partitionVisuals[idx] ? partitionVisuals[idx].gameObject.name : $"Partition_{idx}";
        }

        private void CacheBaseScalesIfNeeded()
        {
            if (partitionVisuals == null) return;

            if (_baseScales == null) _baseScales = new List<Vector3>();

            if (_baseScales.Count != partitionVisuals.Count)
            {
                _baseScales.Clear();
                _scaleTweens.Clear();

                for (int i = 0; i < partitionVisuals.Count; i++)
                {
                    var g = partitionVisuals[i];
                    _baseScales.Add(g ? g.rectTransform.localScale : Vector3.one);
                    _scaleTweens.Add(null);
                }
            }
        }

        private void ApplyHighlight(int activeIdx)
        {
            if (partitionVisuals == null) return;

            CacheBaseScalesIfNeeded();

            for (int i = 0; i < partitionVisuals.Count; i++)
            {
                var g = partitionVisuals[i];
                if (!g) continue;

                bool isActive = (i == activeIdx);

                var c = g.color;
                c.a = isActive ? activeAlpha : inactiveAlpha;
                g.color = c;

                var tr = g.rectTransform;
                var baseScale = (i < _baseScales.Count) ? _baseScales[i] : tr.localScale;

                Vector3 targetScale;
                if (isActive)
                {
                    float s = activeScale;
                    targetScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
                }
                else
                {
                    targetScale = baseScale;
                }

                if (i < _scaleTweens.Count && _scaleTweens[i] != null && _scaleTweens[i].IsActive())
                    _scaleTweens[i].Kill(false);

                var tw = tr.DOScale(targetScale, highlightScaleTweenDuration)
                           .SetEase(highlightScaleEase)
                           .SetUpdate(true);

                if (i < _scaleTweens.Count) _scaleTweens[i] = tw;
            }
        }

        private bool ValidateRefs()
        {
            if (!startPoint || !endPoint || !ticker)
            {
                Debug.LogWarning("[AccuracyMeterArcDriver] Missing required references: Start, End, or Ticker.", this);
                return false;
            }

            if (meterShape == MeterShape.Arc && !centerPoint)
            {
                throw new InvalidOperationException(
                    "[AccuracyMeterArcDriver] MeterShape is Arc but CenterPoint is not assigned. Assign CenterPoint or switch MeterShape to Linear."
                );
            }

            return true;
        }

        private void KillTween()
        {
            if (_tween != null && _tween.IsActive())
            {
                _tween.Kill();
                _tween = null;
            }
            _isRunning = false;
        }

        [Serializable]
        public struct AccuracyStopResult
        {
            public bool IsValid;
            public int PartitionIndex;
            public string PartitionName;
            public float NormalizedT;

            public static AccuracyStopResult Invalid(string _)
            {
                return new AccuracyStopResult
                {
                    IsValid = false,
                    PartitionIndex = -1,
                    PartitionName = "Invalid",
                    NormalizedT = 0f
                };
            }
        }
    }
}
