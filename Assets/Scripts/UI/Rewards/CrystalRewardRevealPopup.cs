using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CrystalRewardRevealPopup : MonoBehaviour
{
    static CrystalRewardRevealPopup instance;

    [Header("Popup References")]
    [SerializeField] GameObject popupRoot;
    [SerializeField] CanvasGroup rootCanvasGroup;
    [SerializeField] RectTransform presentationRoot;
    [SerializeField] TMP_Text titleText;
    [SerializeField] GameObject chestWorldRoot;
    [SerializeField] Camera chestCamera;
    [SerializeField] RawImage chestDisplay;
    [SerializeField] Animator chestAnimator;
    [SerializeField] AnimationClip chestOpenClip;
    [SerializeField] GameObject rewardGroup;
    [SerializeField] CanvasGroup rewardCanvasGroup;
    [SerializeField] GameObject glowRoot;
    [SerializeField] CanvasGroup glowCanvasGroup;
    [SerializeField] RectTransform glowRect;
    [SerializeField] Image primaryIcon;
    [SerializeField] RectTransform primaryIconRect;
    [SerializeField] UIPulse primaryIconPulse;
    [SerializeField] WeaponPowerPreviewPlayer primaryAbilityPreview;
    [SerializeField] Image secondaryIcon;
    [SerializeField] RectTransform secondaryIconRect;
    [SerializeField] UIPulse secondaryIconPulse;
    [SerializeField] TMP_Text rewardText;
    [SerializeField] Button okButton;
    [SerializeField] CanvasGroup okButtonCanvasGroup;

    [Header("Default Currency Art")]
    [SerializeField] Sprite cashIcon;
    [SerializeField] Sprite gemIcon;

    [Header("Crystal Chest Timing")]
    [SerializeField] string chestOpenStateName = "ANIM_Chest_Crystal_Open";
    [SerializeField, Range(0f, 1f)] float chestRevealNormalizedTime = 0.8625f;
    [SerializeField, Min(0f)] float rewardRevealLeadTime = 0.18f;
    [SerializeField, Min(256)] int chestRenderTextureResolution = 768;
    [SerializeField, Min(0f)] float chestRenderTailDuration = 5.6f;
    [SerializeField, Min(0.01f)] float entranceDuration = 0.2f;
    [SerializeField, Min(0.01f)] float rewardPopDuration = 0.35f;
    [SerializeField, Min(0f)] float okButtonDelay = 0.15f;
    [SerializeField, Min(0.01f)] float exitDuration = 0.16f;

    [Header("Reward Motion")]
    [SerializeField, Min(0f)] float rewardUpwardDistance = 45f;
    [SerializeField, Range(0.1f, 1f)] float rewardStartScale = 0.35f;
    [SerializeField, Range(0f, 1f)] float rewardStagedAlpha = 0.35f;
    [SerializeField] Vector2 currencyIconSize = new Vector2(240f, 240f);
    [SerializeField] Vector2 shopItemFallbackSize = new Vector2(404.21f, 412.8f);

    [Header("Fx_Rotate_Light03 Glow")]
    [SerializeField, Range(0f, 1f)] float glowIntensity = 1f;
    [SerializeField, Min(0.1f)] float glowScale = 1f;

    readonly Queue<RewardRevealRequest> pending = new Queue<RewardRevealRequest>();
    RewardRevealRequest current;
    ParticleSystem[] chestParticles;
    ParticleSystem[] glowParticles;
    RenderTexture chestRenderTexture;
    Sequence sequence;
    Sequence rewardTween;
    Vector2 rewardBasePosition;
    Vector3 glowBaseScale;
    bool closing;

    public bool IsShowing => current != null;

    void Awake()
    {
        if (instance && instance != this)
        {
            Debug.LogWarning("A duplicate CrystalRewardRevealPopup controller was ignored.", this);
            enabled = false;
            return;
        }

        instance = this;
        chestParticles = chestWorldRoot ? chestWorldRoot.GetComponentsInChildren<ParticleSystem>(true) : new ParticleSystem[0];
        glowParticles = glowRoot ? glowRoot.GetComponentsInChildren<ParticleSystem>(true) : new ParticleSystem[0];
        rewardBasePosition = rewardGroup ? ((RectTransform)rewardGroup.transform).anchoredPosition : Vector2.zero;
        glowBaseScale = glowRect ? glowRect.localScale : Vector3.one;
        EnsureChestRenderTexture();

        if (okButton)
        {
            okButton.onClick.RemoveListener(Close);
            okButton.onClick.AddListener(Close);
            SkateRunnerAudioManager.Instance?.RegisterRuntimeButton(okButton);
        }

        if (popupRoot) popupRoot.SetActive(false);
        SetChestRendering(false);
    }

    void OnDestroy()
    {
        KillTweens();
        StopAndClearChestParticles();
        StopAndClearGlowParticles();
        if (chestCamera) chestCamera.targetTexture = null;
        if (chestDisplay) chestDisplay.texture = null;
        if (chestRenderTexture)
        {
            chestRenderTexture.Release();
            Destroy(chestRenderTexture);
        }
        if (instance == this) instance = null;
        if (current != null) SkateRunnerAudioManager.StopCrystalRewardRevealAudio();
    }

    public static bool TryShow(RewardRevealRequest request)
    {
        if (request == null || request.primary == null) return false;
        CrystalRewardRevealPopup popup = ResolveInstance();
        if (!popup)
        {
            Debug.LogWarning("Crystal reward reveal is not present in the active scene.");
            request.onClosed?.Invoke();
            return false;
        }

        popup.Enqueue(request);
        return true;
    }

    public static void CloseActiveImmediate()
    {
        CrystalRewardRevealPopup popup = ResolveInstance();
        if (popup) popup.CloseImmediate(true);
    }

    static CrystalRewardRevealPopup ResolveInstance()
    {
        if (instance) return instance;
        instance = Object.FindFirstObjectByType<CrystalRewardRevealPopup>(FindObjectsInactive.Include);
        return instance;
    }

    void Enqueue(RewardRevealRequest request)
    {
        if (current != null)
        {
            pending.Enqueue(request);
            return;
        }
        Show(request);
    }

    void Show(RewardRevealRequest request)
    {
        current = request;
        closing = false;
        KillTweens();
        StopRewardPulses();
        ConfigureReward(request);
        EnsureChestRenderTexture();

        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;
        presentationRoot.localScale = Vector3.one * 0.98f;

        okButton.gameObject.SetActive(false);
        ResetChestAnimation();
        SetChestRendering(true);
        PrepareStagedReward();

        SkateRunnerAudioManager.StartCrystalRewardRevealAudio();

        float revealDelay = Mathf.Max(0.01f, chestOpenClip.length * chestRevealNormalizedTime);
        float rewardStartDelay = Mathf.Max(0f, revealDelay - rewardRevealLeadTime);
        sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(rootCanvasGroup.DOFade(1f, entranceDuration).SetEase(Ease.OutCubic));
        sequence.Join(presentationRoot.DOScale(1f, entranceDuration).SetEase(Ease.OutBack));
        sequence.InsertCallback(rewardStartDelay, BeginRewardReveal);
        sequence.InsertCallback(revealDelay, PlayBreakFeedback);
        sequence.InsertCallback(chestOpenClip.length, FinishChestAnimation);
        sequence.InsertCallback(revealDelay + chestRenderTailDuration, EndChestRenderingAfterTail);
    }

    void ConfigureReward(RewardRevealRequest request)
    {
        titleText.text = string.IsNullOrWhiteSpace(request.title) ? "REWARD UNLOCKED!" : request.title;
        if (primaryAbilityPreview)
        {
            primaryAbilityPreview.Clear();
            primaryAbilityPreview.enabled = false;
        }
        ConfigureEntry(primaryIcon, request.primary, true);

        bool hasSecondary = request.HasSecondary;
        secondaryIcon.gameObject.SetActive(hasSecondary);
        if (hasSecondary) ConfigureEntry(secondaryIcon, request.secondary, false);

        primaryIconRect.anchoredPosition = hasSecondary ? new Vector2(-120f, 32f) : new Vector2(0f, 32f);
        secondaryIconRect.anchoredPosition = new Vector2(120f, 32f);
        primaryIconRect.sizeDelta = ResolveDisplaySize(request.primary);
        if (hasSecondary) secondaryIconRect.sizeDelta = ResolveDisplaySize(request.secondary);
        rewardText.text = hasSecondary
            ? request.primary.BuildLabel() + "\n" + request.secondary.BuildLabel()
            : request.primary.BuildLabel();
    }

    Vector2 ResolveDisplaySize(RewardRevealEntry entry)
    {
        if (entry == null) return currencyIconSize;
        if (entry.displaySize.x > 0f && entry.displaySize.y > 0f) return entry.displaySize;
        return entry.IsCurrency ? currencyIconSize : shopItemFallbackSize;
    }

    void ConfigureEntry(Image view, RewardRevealEntry entry, bool isPrimary)
    {
        view.color = Color.white;
        view.preserveAspect = true;

        if (isPrimary && entry.type == RewardRevealType.Ability &&
            entry.previewAnimation && primaryAbilityPreview)
        {
            // Do not show the Shop's static thumbnail for Abilities. The same
            // sprite animation used by the Inventory preview starts behind the chest.
            view.sprite = null;
            view.enabled = false;
            primaryAbilityPreview.enabled = true;
            primaryAbilityPreview.Play(entry.previewAnimation);
            return;
        }

        Sprite icon = entry.icon;
        if (!icon && entry.type == RewardRevealType.Cash) icon = cashIcon;
        if (!icon && entry.type == RewardRevealType.Gems) icon = gemIcon;
        view.sprite = icon;
        view.enabled = icon;
    }

    void ResetChestAnimation()
    {
        StopAndClearChestParticles();
        chestWorldRoot.SetActive(true);
        chestAnimator.enabled = true;
        chestAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        chestAnimator.Rebind();
        chestAnimator.Update(0f);
        chestAnimator.Play(chestOpenStateName, 0, 0f);
        chestAnimator.Update(0f);
    }

    void PrepareStagedReward()
    {
        rewardGroup.SetActive(true);
        RectTransform rewardRect = (RectTransform)rewardGroup.transform;
        rewardRect.anchoredPosition = rewardBasePosition - Vector2.up * rewardUpwardDistance;
        rewardRect.localScale = Vector3.one * rewardStartScale;
        rewardCanvasGroup.alpha = rewardStagedAlpha;
        rewardText.alpha = 0f;
        glowRoot.SetActive(true);
        RestartGlowParticles();
        glowCanvasGroup.alpha = 0f;
        glowRect.localScale = glowBaseScale * glowScale;
    }

    void BeginRewardReveal()
    {
        if (current == null || closing) return;

        RectTransform rewardRect = (RectTransform)rewardGroup.transform;

        rewardTween = DOTween.Sequence().SetUpdate(true);
        rewardTween.Join(rewardCanvasGroup.DOFade(1f, rewardPopDuration * 0.7f).SetEase(Ease.OutCubic));
        rewardTween.Join(rewardRect.DOAnchorPos(rewardBasePosition, rewardPopDuration).SetEase(Ease.OutCubic));
        rewardTween.Join(rewardRect.DOScale(1f, rewardPopDuration).SetEase(Ease.OutBack));
        rewardTween.Join(rewardText.DOFade(1f, rewardPopDuration * 0.7f).SetEase(Ease.OutCubic));
        rewardTween.Join(glowCanvasGroup.DOFade(glowIntensity, rewardPopDuration).SetEase(Ease.OutCubic));
        rewardTween.AppendCallback(StartRewardPulses);
        rewardTween.AppendInterval(okButtonDelay);
        rewardTween.AppendCallback(RevealOkButton);
    }

    void PlayBreakFeedback()
    {
        if (current == null || closing) return;
        SkateRunnerAudioManager.PlayCrystalChestBreak();
        SkateRunnerAudioManager.PlayRewardReveal();
    }

    void FinishChestAnimation()
    {
        if (chestAnimator) chestAnimator.enabled = false;
        for (int i = 0; i < chestParticles.Length; i++)
        {
            ParticleSystem particle = chestParticles[i];
            if (particle && particle.main.loop)
                particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        // Non-looping authored break particles continue naturally over the reward.
    }

    void EndChestRenderingAfterTail()
    {
        if (chestCamera) chestCamera.enabled = false;
        if (chestDisplay) chestDisplay.gameObject.SetActive(false);
        if (chestWorldRoot) chestWorldRoot.SetActive(false);
    }

    void RevealOkButton()
    {
        if (closing || current == null) return;
        okButton.gameObject.SetActive(true);
        okButtonCanvasGroup.alpha = 0f;
        okButtonCanvasGroup.DOFade(1f, 0.12f).SetUpdate(true);
        okButton.Select();
    }

    public void Close()
    {
        if (current == null || closing) return;
        closing = true;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = true;
        KillTweens();

        sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(rootCanvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic));
        sequence.Join(presentationRoot.DOScale(0.98f, exitDuration).SetEase(Ease.InCubic));
        sequence.OnComplete(() => CloseImmediate(false));
    }

    void CloseImmediate(bool clearQueue)
    {
        KillTweens();
        StopRewardPulses();
        StopAndClearChestParticles();
        StopAndClearGlowParticles();
        SetChestRendering(false);
        if (glowRoot) glowRoot.SetActive(false);
        if (primaryAbilityPreview)
        {
            primaryAbilityPreview.Clear();
            primaryAbilityPreview.enabled = false;
        }
        if (popupRoot) popupRoot.SetActive(false);
        SkateRunnerAudioManager.StopCrystalRewardRevealAudio();

        RewardRevealRequest completed = current;
        current = null;
        closing = false;
        completed?.onClosed?.Invoke();

        if (clearQueue) pending.Clear();
        else if (pending.Count > 0) Show(pending.Dequeue());
    }

    void EnsureChestRenderTexture()
    {
        if (chestRenderTexture) return;
        int resolution = Mathf.Clamp(chestRenderTextureResolution, 256, 1024);
        chestRenderTexture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32)
        {
            name = "Crystal Reward Chest RT",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.DontSave
        };
        chestRenderTexture.Create();
        chestCamera.targetTexture = chestRenderTexture;
        chestDisplay.texture = chestRenderTexture;
    }

    void SetChestRendering(bool active)
    {
        if (chestWorldRoot) chestWorldRoot.SetActive(active);
        if (chestCamera) chestCamera.enabled = active;
        if (chestDisplay) chestDisplay.gameObject.SetActive(active);
    }

    void StopAndClearChestParticles()
    {
        for (int i = 0; i < chestParticles.Length; i++)
            if (chestParticles[i]) chestParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void RestartGlowParticles()
    {
        StopAndClearGlowParticles();
        for (int i = 0; i < glowParticles.Length; i++)
            if (glowParticles[i]) glowParticles[i].Play(true);
    }

    void StopAndClearGlowParticles()
    {
        for (int i = 0; i < glowParticles.Length; i++)
            if (glowParticles[i]) glowParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void StartRewardPulses()
    {
        if (primaryIconPulse) primaryIconPulse.enabled = true;
        if (secondaryIconPulse) secondaryIconPulse.enabled = secondaryIcon.gameObject.activeSelf;
    }

    void StopRewardPulses()
    {
        if (primaryIconPulse)
        {
            primaryIconPulse.enabled = false;
            primaryIconPulse.transform.localScale = Vector3.one;
        }
        if (secondaryIconPulse)
        {
            secondaryIconPulse.enabled = false;
            secondaryIconPulse.transform.localScale = Vector3.one;
        }
    }

    void KillTweens()
    {
        sequence?.Kill();
        sequence = null;
        rewardTween?.Kill();
        rewardTween = null;
        if (okButtonCanvasGroup) okButtonCanvasGroup.DOKill();
        if (glowRect) glowRect.DOKill();
        if (glowCanvasGroup) glowCanvasGroup.DOKill();
        if (rewardText) rewardText.DOKill();
        if (rewardGroup) rewardGroup.transform.DOKill();
    }
}
