using UnityEngine;

namespace Elroi.Tutorials
{
    [CreateAssetMenu(menuName = "ELROI/Tutorials/Theme", fileName = "ELROI_TutorialTheme")]
    public sealed class TutorialTheme : ScriptableObject
    {
        [Header("Spotlight")]
        [Range(0f, 1f)] public float OverlayOpacity = 0.78f;
        [Min(0f)] public float SpotlightFeather = 12f;
        [Min(0f)] public float SpotlightCornerRadius = 28f;
        [Min(0f)] public float TransitionDuration = 0.2f;
        public bool Pulse = true;
        [Min(0f)] public float PulseAmount = 8f;
        [Min(0.01f)] public float PulseSpeed = 2f;

        [Header("Manga Dialogue")]
        public Color BubbleFill = new Color(1f, 0.985f, 0.94f, 1f);
        public Color BubbleBorder = Color.black;
        public Color TextColor = Color.black;
        [Min(0f)] public float BorderThickness = 5f;
        [Min(0f)] public float BubbleCornerRadius = 16f;
        [Min(0f)] public float BubblePadding = 24f;
        [Min(100f)] public float BubbleMinWidth = 260f;
        [Min(100f)] public float BubblePreferredWidth = 520f;
        [Min(100f)] public float BubbleMaxWidth = 720f;
        [Min(8f)] public float IntroductionFontSize = 34f;
        [Min(8f)] public float MinimumFontSize = 18f;
        [Min(0f)] public float SafeMargin = 28f;

        [Header("Instruction and Gesture")]
        public Color InstructionColor = Color.white;
        [Min(8f)] public float InstructionFontSize = 38f;
        [Min(16f)] public float GestureSize = 140f;

        [Header("OK Button")]
        public Color OkButtonFill = new Color(1f, 0.78f, 0.12f, 1f);
        public Color OkButtonText = Color.black;
    }
}
