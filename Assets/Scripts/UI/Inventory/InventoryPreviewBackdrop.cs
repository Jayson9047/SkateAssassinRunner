using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Synchronizes each item's palette across inventory, preview, and shop cards.</summary>
[DisallowMultipleComponent]
public sealed class InventoryPreviewBackdrop : MonoBehaviour
{
    [Serializable]
    private sealed class Palette
    {
        public AnimationClip previewClip;
        public Color backgroundColor = new Color(0.06f, 0.43f, 0.90f, 1f);
        public Color glowColor = new Color(0.05f, 0.90f, 1f, 0.32f);
        public Image[] cardBackgrounds;
        public Image[] cardFaders;
        public Image[] cardGlows;
        public ParticleSystem[] cardGlowParticles;
    }

    [SerializeField] private Image background;
    [SerializeField] private Image glow;
    [SerializeField] private Color fallbackBackground = new Color(0.06f, 0.43f, 0.90f, 1f);
    [SerializeField] private Color fallbackGlow = new Color(0.05f, 0.90f, 1f, 0.32f);
    [Tooltip("Choose colors that contrast with each preview's weapon and effects.")]
    [SerializeField] private Palette[] palettes;

    public void Apply(AnimationClip clip)
    {
        Color backgroundColor = fallbackBackground;
        Color glowColor = fallbackGlow;

        if (clip != null && palettes != null)
        {
            for (int i = 0; i < palettes.Length; i++)
            {
                Palette palette = palettes[i];
                if (palette == null || palette.previewClip != clip)
                    continue;

                backgroundColor = palette.backgroundColor;
                glowColor = palette.glowColor;
                SyncPaletteCards(palette);
                break;
            }
        }

        if (background != null)
            background.color = backgroundColor;
        if (glow != null)
            glow.color = glowColor;
    }


    private static void SetColors(Image[] images, Color color, bool preserveAlpha)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
                continue;

            Color target = color;
            if (preserveAlpha)
                target.a = image.color.a;
            image.color = target;
        }
    }


    private static void SyncPaletteCards(Palette palette)
    {
        SetColors(palette.cardBackgrounds, palette.backgroundColor, false);
        SetColors(palette.cardFaders, palette.backgroundColor, true);
        SetColors(palette.cardGlows, palette.glowColor, true);
        SetParticleColors(palette.cardGlowParticles, palette.glowColor);
    }


    [ContextMenu("Sync Inventory And Shop Colors")]
    public void SyncCardColors()
    {
        if (palettes == null)
            return;

        for (int i = 0; i < palettes.Length; i++)
        {
            if (palettes[i] != null)
                SyncPaletteCards(palettes[i]);
        }
    }


    private void Awake()
    {
        SyncCardColors();
    }


    private static void SetParticleColors(ParticleSystem[] particles, Color color)
    {
        if (particles == null)
            return;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            var main = particle.main;
            Color previous = main.startColor.color;
            float brightness = Mathf.Max(previous.r, Mathf.Max(previous.g, previous.b));
            Color target = color * brightness;
            target.a = previous.a;
            if (Mathf.Abs(previous.r - target.r) < 0.0001f &&
                Mathf.Abs(previous.g - target.g) < 0.0001f &&
                Mathf.Abs(previous.b - target.b) < 0.0001f)
                continue;

            main.startColor = target;
            var lifetime = particle.colorOverLifetime;
            if (!lifetime.enabled || lifetime.color.mode != ParticleSystemGradientMode.Gradient)
                continue;

            Gradient original = lifetime.color.gradient;
            GradientColorKey[] keys = original.colorKeys;
            for (int key = 0; key < keys.Length; key++)
            {
                Color old = keys[key].color;
                float value = Mathf.Max(old.r, Mathf.Max(old.g, old.b));
                Color tint = color * value;
                tint.a = old.a;
                keys[key].color = tint;
            }

            Gradient updated = new Gradient();
            updated.SetKeys(keys, original.alphaKeys);
            updated.mode = original.mode;
            lifetime.color = new ParticleSystem.MinMaxGradient(updated);
        }
    }
}
