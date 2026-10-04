using Elroi.Tutorials;
using Febucci.TextAnimatorCore.Time;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using UnityEngine;

/// <summary>Keeps tutorial effects alive while the world is paused.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(TextAnimator_TMP))]
public sealed class SkateRunnerTutorialTextAnimator : MonoBehaviour, ITutorialTextPresenter
{
    [SerializeField, TextArea] private string standaloneText;
    [SerializeField] private TMPro.TMP_FontAsset accentFont;
    private TextAnimator_TMP animator;

    private void Awake() => Configure();

    private void OnEnable()
    {
        Configure();
        if (!string.IsNullOrEmpty(standaloneText)) SetTutorialText(standaloneText);
    }

    private void Configure()
    {
        if (animator == null) animator = GetComponent<TextAnimator_TMP>();
        if (accentFont != null) TMPro.MaterialReferenceManager.AddFontAsset(accentFont);
        animator.sharedSettings = null;
        animator.localSettings.timeScale = TimeScale.Unscaled;
        animator.animationLoop = AnimationLoop.LateUpdate;
    }

    public void SetTutorialText(string text)
    {
        Configure();
        animator.SetText(text, false);
    }
}
