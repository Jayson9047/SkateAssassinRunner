using UnityEngine;
using UnityEngine.UI;
using MoreMountains.InfiniteRunnerEngine;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Same idea as ParallaxOffset, but ignores Time.timeScale (uses unscaled time).
    /// Great for cinematic FX like anime speedlines during slowmo.
    /// </summary>
    public class ParallaxOffsetUnscaled : MonoBehaviour
    {
        [Tooltip("Relative speed of the offset movement.")]
        public float Speed = 0f;

        [Tooltip("If true, multiplies by LevelManager.Instance.Speed (like original script). If false, uses Speed only.")]
        public bool UseLevelSpeed = false;

        [Tooltip("Constant multiplier when UseLevelSpeed is false. Think of it as your 'world scroll speed'.")]
        public float ConstantMultiplier = 1f;

        [Tooltip("Optional fixed Y offset.")]
        public float YOffset = 0f;

        private RawImage _rawImage;
        private Renderer _renderer;
        private Vector2 _newOffset;
        private float _position;

        protected virtual void Start()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                _rawImage = GetComponent<RawImage>();
            }

            // IMPORTANT: ensure we don't modify a shared material across objects
            if (_renderer != null) { _ = _renderer.material; }
            if (_rawImage != null) { _ = _rawImage.material; }
        }

        protected virtual void Update()
        {
            if (_rawImage == null && _renderer == null) return;

            float dt = Time.unscaledDeltaTime;

            float multiplier = ConstantMultiplier;

            if (UseLevelSpeed && LevelManager.Instance != null)
            {
                multiplier = LevelManager.Instance.Speed;
            }

            _position += (Speed / 300f) * multiplier * dt;

            // Wrap cleanly even if speed is high
            _position = _position % 1f;
            if (_position < 0f) _position += 1f;

            _newOffset.x = _position;
            _newOffset.y = YOffset;

            if (_renderer != null)
            {
                _renderer.material.mainTextureOffset = _newOffset;
            }
            else
            {
                _rawImage.material.mainTextureOffset = _newOffset;
            }
        }
    }
}
