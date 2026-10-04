using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Elroi.Tutorials
{
    [DisallowMultipleComponent]
    public sealed class TutorialPresentation : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private TutorialSpotlightGraphic spotlight;
        [SerializeField] private RectTransform dialogueBubble;
        [SerializeField] private Image dialogueFill;
        [SerializeField] private Outline dialogueBorder;
        [SerializeField] private TutorialTriangleGraphic dialogueTail;
        [SerializeField] private TMP_Text introductionText;
        [SerializeField] private TMP_Text bottomInstructionText;
        [SerializeField] private Image gestureImage;
        [SerializeField] private Button okButton;
        [SerializeField] private TMP_Text okButtonText;
        [SerializeField] private TutorialGestureInput gestureInput;

        private TutorialDefinition definition;
        private TutorialTheme theme;
        private GameObject target;
        private Camera targetCamera;
        private Coroutine trackingRoutine;
        private Coroutine gestureRoutine;
        private Rect currentTargetRect;

        public bool IsVisible => gameObject.activeSelf;

        public void ConfigureForAuthoring(Canvas rootCanvas, TutorialSpotlightGraphic spotlightGraphic, RectTransform bubble,
            Image bubbleFill, Outline bubbleOutline, TutorialTriangleGraphic tail, TMP_Text introduction,
            TMP_Text instruction, Image gesture, Button ok, TMP_Text okLabel, TutorialGestureInput input)
        {
            canvas = rootCanvas;
            spotlight = spotlightGraphic;
            dialogueBubble = bubble;
            dialogueFill = bubbleFill;
            dialogueBorder = bubbleOutline;
            dialogueTail = tail;
            introductionText = introduction;
            bottomInstructionText = instruction;
            gestureImage = gesture;
            okButton = ok;
            okButtonText = okLabel;
            gestureInput = input;
        }

        public void Show(TutorialDefinition tutorial, GameObject resolvedTarget, Camera camera, TutorialTheme selectedTheme,
            TutorialGestureSettings gestureSettings, Action onOk, Action onGesture)
        {
            definition = tutorial;
            target = resolvedTarget;
            targetCamera = camera;
            theme = selectedTheme;
            gameObject.SetActive(true);
            ApplyTheme();

            introductionText.text = tutorial.IntroductionText ?? string.Empty;
            dialogueTail.gameObject.SetActive(tutorial.TargetType != TutorialTargetType.None);
            bottomInstructionText.text = tutorial.BottomInstructionText ?? string.Empty;
            bottomInstructionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(tutorial.BottomInstructionText));
            okButton.gameObject.SetActive(tutorial.CompletionType == TutorialCompletionType.OkButton);
            okButton.onClick.RemoveAllListeners();
            if (tutorial.CompletionType == TutorialCompletionType.OkButton) okButton.onClick.AddListener(() => onOk?.Invoke());

            gestureInput.End();
            if (tutorial.CompletionType == TutorialCompletionType.Gesture)
                gestureInput.Begin(tutorial.RequiredGesture, gestureSettings, onGesture);

            if (gestureRoutine != null) StopCoroutine(gestureRoutine);
            gestureRoutine = StartCoroutine(PlayGestureAnimation(tutorial.GestureAnimation));
            if (trackingRoutine != null) StopCoroutine(trackingRoutine);
            trackingRoutine = StartCoroutine(TrackTarget());
        }

        public void Hide()
        {
            gestureInput.End();
            okButton.onClick.RemoveAllListeners();
            if (trackingRoutine != null) StopCoroutine(trackingRoutine);
            if (gestureRoutine != null) StopCoroutine(gestureRoutine);
            trackingRoutine = null;
            gestureRoutine = null;
            definition = null;
            target = null;
            gameObject.SetActive(false);
        }

        private IEnumerator TrackTarget()
        {
            while (definition != null)
            {
                UpdateTargetPresentation();
                yield return null;
            }
        }

        private void UpdateTargetPresentation()
        {
            Rect rect;
            if (definition.TargetType == TutorialTargetType.None || target == null ||
                !TutorialScreenUtility.TryGetScreenRect(target, definition.TargetType, targetCamera, definition.FallbackTargetSize, out rect, out _))
            {
                rect = new Rect(Screen.width * 0.5f - 60f, Screen.height * 0.5f - 60f, 120f, 120f);
            }

            float pulse = theme != null && theme.Pulse ? (Mathf.Sin(Time.unscaledTime * theme.PulseSpeed) * 0.5f + 0.5f) * theme.PulseAmount : 0f;
            float padding = definition.SpotlightPadding + pulse;
            rect.xMin -= padding;
            rect.xMax += padding;
            rect.yMin -= padding;
            rect.yMax += padding;
            currentTargetRect = rect;

            float opacity = theme != null ? theme.OverlayOpacity : 0.78f;
            float radius = theme != null ? theme.SpotlightCornerRadius : 24f;
            float feather = theme != null ? theme.SpotlightFeather : 10f;
            if (definition.TargetType == TutorialTargetType.None)
                rect = new Rect(-Screen.width * 2f, -Screen.height * 2f, 1f, 1f);
            spotlight.Configure(rect, definition.SpotlightShape, radius, feather, opacity);
            PositionDialogue(rect);
        }

        private void PositionDialogue(Rect targetRect)
        {
            // Target bounds are screen pixels; dialogue dimensions are Canvas units.
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            float margin = theme != null ? theme.SafeMargin : 28f;
            float width = Mathf.Clamp(theme != null ? theme.BubblePreferredWidth : 520f,
                theme != null ? theme.BubbleMinWidth : 260f,
                Mathf.Min(theme != null ? theme.BubbleMaxWidth : 720f, Screen.width / scale - margin * 2f));
            introductionText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - (theme != null ? theme.BubblePadding * 2f : 48f));
            introductionText.ForceMeshUpdate();
            float height = Mathf.Clamp(introductionText.preferredHeight + (theme != null ? theme.BubblePadding * 2f : 48f), 110f, Screen.height / scale * 0.45f);
            dialogueBubble.sizeDelta = new Vector2(width, height);

            if (definition.TargetType == TutorialTargetType.None)
            {
                dialogueBubble.anchoredPosition = Vector2.zero;
                return;
            }

            Vector2 targetCenter = targetRect.center;
            float pixelWidth = width * scale, pixelHeight = height * scale;
            float pixelMargin = margin * scale, gap = 34f * scale;
            float bottom = pixelMargin;
            foreach (var control in new[] { okButton.gameObject, bottomInstructionText.gameObject })
                if (control.activeInHierarchy && TutorialScreenUtility.TryGetScreenRect(control,
                    TutorialTargetType.TwoDimensional, null, Vector2.zero, out Rect controlRect, out _))
                    bottom = Mathf.Max(bottom, controlRect.yMax + pixelMargin);

            Vector2[] candidates = {
                new Vector2(targetCenter.x, targetRect.yMax + gap + pixelHeight * 0.5f),
                new Vector2(targetCenter.x, targetRect.yMin - gap - pixelHeight * 0.5f),
                new Vector2(targetRect.xMax + gap + pixelWidth * 0.5f, targetCenter.y),
                new Vector2(targetRect.xMin - gap - pixelWidth * 0.5f, targetCenter.y)
            };
            Vector2 desired = candidates[0];
            float bestOverlap = float.PositiveInfinity;
            foreach (Vector2 candidate in candidates)
            {
                Vector2 clamped = new Vector2(
                    Mathf.Clamp(candidate.x, pixelMargin + pixelWidth * 0.5f, Screen.width - pixelMargin - pixelWidth * 0.5f),
                    Mathf.Clamp(candidate.y, bottom + pixelHeight * 0.5f, Screen.height - pixelMargin - pixelHeight * 0.5f));
                Rect bubble = new Rect(clamped.x - pixelWidth * 0.5f, clamped.y - pixelHeight * 0.5f, pixelWidth, pixelHeight);
                float overlap = Mathf.Max(0f, Mathf.Min(bubble.xMax, targetRect.xMax) - Mathf.Max(bubble.xMin, targetRect.xMin)) *
                    Mathf.Max(0f, Mathf.Min(bubble.yMax, targetRect.yMax) - Mathf.Max(bubble.yMin, targetRect.yMin));
                if (overlap >= bestOverlap) continue;
                bestOverlap = overlap;
                desired = clamped;
                if (overlap == 0f) break;
            }
            Vector2 tailDirection = targetCenter - desired;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, desired, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out Vector2 local);
            dialogueBubble.anchoredPosition = local;

            Vector2 localTarget;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dialogueBubble, targetCenter, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localTarget);
            Vector2 edge = new Vector2(Mathf.Clamp(localTarget.x, -width * 0.42f, width * 0.42f), Mathf.Clamp(localTarget.y, -height * 0.42f, height * 0.42f));
            dialogueTail.rectTransform.anchoredPosition = edge;
            dialogueTail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tailDirection.y, tailDirection.x) * Mathf.Rad2Deg + 90f);
        }

        private void ApplyTheme()
        {
            if (theme == null) return;
            dialogueFill.color = theme.BubbleFill;
            dialogueBorder.effectColor = theme.BubbleBorder;
            dialogueBorder.effectDistance = new Vector2(theme.BorderThickness, -theme.BorderThickness);
            dialogueTail.color = theme.BubbleFill;
            introductionText.color = theme.TextColor;
            introductionText.fontSize = theme.IntroductionFontSize;
            introductionText.enableAutoSizing = true;
            introductionText.fontSizeMin = theme.MinimumFontSize;
            introductionText.fontSizeMax = theme.IntroductionFontSize;
            bottomInstructionText.color = theme.InstructionColor;
            bottomInstructionText.fontSize = theme.InstructionFontSize;
            gestureImage.rectTransform.sizeDelta = Vector2.one * theme.GestureSize;
            if (okButton.targetGraphic != null) okButton.targetGraphic.color = theme.OkButtonFill;
            if (okButtonText != null) okButtonText.color = theme.OkButtonText;
        }

        private IEnumerator PlayGestureAnimation(TutorialGestureAnimation animation)
        {
            gestureImage.gameObject.SetActive(animation != null && animation.SpriteFrames != null && animation.SpriteFrames.Length > 0);
            if (!gestureImage.gameObject.activeSelf) yield break;
            gestureImage.rectTransform.localScale = Vector3.one * animation.PlaybackScale;
            int frame = 0;
            float elapsed = 0f;
            float frameDuration = 1f / animation.FramesPerSecond;
            while (true)
            {
                gestureImage.sprite = animation.SpriteFrames[frame];
                while (elapsed < frameDuration) { elapsed += Time.unscaledDeltaTime; yield return null; }
                elapsed -= frameDuration;
                frame++;
                if (frame < animation.SpriteFrames.Length) continue;
                if (!animation.Loop) yield break;
                frame = 0;
            }
        }
    }
}
