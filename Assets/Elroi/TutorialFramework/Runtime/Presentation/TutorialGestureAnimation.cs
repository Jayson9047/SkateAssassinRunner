using UnityEngine;

namespace Elroi.Tutorials
{
    [CreateAssetMenu(menuName = "ELROI/Tutorials/Gesture Animation", fileName = "TutorialGestureAnimation")]
    public sealed class TutorialGestureAnimation : ScriptableObject
    {
        [SerializeField] private Sprite[] spriteFrames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 12f;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0.1f)] private float playbackScale = 1f;

        public Sprite[] SpriteFrames => spriteFrames;
        public float FramesPerSecond => Mathf.Max(0.1f, framesPerSecond);
        public bool Loop => loop;
        public float PlaybackScale => Mathf.Max(0.1f, playbackScale);

        public void ConfigureForAuthoring(Sprite[] frames, float fps, bool shouldLoop, float scale = 1f)
        {
            spriteFrames = frames;
            framesPerSecond = Mathf.Max(0.1f, fps);
            loop = shouldLoop;
            playbackScale = Mathf.Max(0.1f, scale);
        }
    }
}
