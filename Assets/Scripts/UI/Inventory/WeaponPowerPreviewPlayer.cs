using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Plays one of the power-specific turntable clips through a real Animator.
/// The shared controller contains Turntable.anim; an AnimatorOverrideController
/// swaps that state to the selected power's clip without creating preview objects.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image), typeof(Animator))]
public sealed class WeaponPowerPreviewPlayer : MonoBehaviour
{
    [SerializeField] private Image previewImage;
    [SerializeField] private Animator previewAnimator;
    [SerializeField] private RuntimeAnimatorController turntableController;
    [SerializeField] private AnimationClip turntableTemplate;

    private AnimationClip currentClip;
    private AnimatorOverrideController overrideController;
    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> clipOverrides =
        new List<KeyValuePair<AnimationClip, AnimationClip>>(1);

    public AnimationClip CurrentClip => currentClip;

    private void Awake()
    {
        if (previewImage == null)
            previewImage = GetComponent<Image>();

        if (previewAnimator == null)
            previewAnimator = GetComponent<Animator>();

        ConfigureAnimator();
    }

    public void Play(AnimationClip clip)
    {
        currentClip = clip;

        if (previewImage == null)
            previewImage = GetComponent<Image>();

        if (previewAnimator == null)
            previewAnimator = GetComponent<Animator>();

        if (clip == null || previewImage == null || previewAnimator == null ||
            turntableController == null || turntableTemplate == null)
        {
            Clear();
            return;
        }

        ConfigureAnimator();
        clipOverrides.Clear();
        overrideController.GetOverrides(clipOverrides);

        bool replacedTemplate = false;
        for (int i = 0; i < clipOverrides.Count; i++)
        {
            if (clipOverrides[i].Key == turntableTemplate)
            {
                clipOverrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                    turntableTemplate,
                    clip);
                replacedTemplate = true;
                break;
            }
        }

        if (!replacedTemplate)
        {
            Debug.LogWarning("Turntable template is not present in the preview Animator Controller.", this);
            Clear();
            return;
        }

        overrideController.ApplyOverrides(clipOverrides);
        previewImage.enabled = true;
        previewAnimator.enabled = true;
        previewAnimator.Rebind();
        previewAnimator.Play("Turntable", 0, 0f);
        previewAnimator.Update(0f);
    }

    private void ConfigureAnimator()
    {
        if (previewAnimator == null || turntableController == null)
            return;

        if (overrideController == null ||
            overrideController.runtimeAnimatorController != turntableController)
        {
            overrideController = new AnimatorOverrideController(turntableController);
        }

        previewAnimator.runtimeAnimatorController = overrideController;
        previewAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        previewAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    public void Clear()
    {
        currentClip = null;

        if (previewAnimator != null)
            previewAnimator.enabled = false;

        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;
        }
    }
}
