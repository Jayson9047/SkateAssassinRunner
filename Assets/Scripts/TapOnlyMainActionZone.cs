using UnityEngine;
using UnityEngine.EventSystems;
using MoreMountains.InfiniteRunnerEngine;
using TMPro; // add this

public class TapOnlyMainActionZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Ruthless Tap Mode UI")]
    [SerializeField] private TextMeshProUGUI comboText;  // assign in inspector OR auto-find
    [SerializeField] private string comboFormat = "COMBO x{0}";
    [SerializeField] private float comboFadeOutSeconds = 0.25f;

    [Header("Ruthless Tap Cash")]
    [SerializeField] private bool awardCashOnRuthlessTap = true;
    [SerializeField] private int minCashPerTap = 1;
    [SerializeField] private int maxCashPerTap = 7;
    [SerializeField] private SwipeRightAttackDetector swipeRightAttackDetector;
    [SerializeField] private GUIPulse comboPulse;
    // Missions hooks (Phase 2 / Ruthless Tap Mode)
    public static System.Action<int> OnPhase2ComboUpdated; // sends current combo count
    public static System.Action<int> OnPhase2CashEarned;   // sends cash earned (delta)

    private Coroutine _fadeCo;
    private bool _lastRuthlessState;

    [Header("Tap vs Swipe")]
    [Tooltip("Max finger movement (in pixels) that still counts as a tap.")]
    public float tapMaxMovePixels = 25f;

    [Tooltip("Optional: max duration that still counts as a tap (0 = ignore).")]
    public float tapMaxTimeSeconds = 0f;

    private Vector2 _downPos;
    private float _downTime;
    private bool _isTapCandidate;

    public void OnPointerDown(PointerEventData eventData)
    {
        _downPos = eventData.position;
        _downTime = Time.unscaledTime;
        _isTapCandidate = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // If the finger moves too far, it's not a tap anymore
        if (!_isTapCandidate) return;

        float moved = Vector2.Distance(_downPos, eventData.position);
        if (moved > tapMaxMovePixels)
        {
            _isTapCandidate = false;
        }
    }

    private void Awake()
    {
        if (comboText == null)
        {
            // Find a child named "ComboText" anywhere under this button/tap zone
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t != null && t.name == "ComboText")
                {
                    comboText = t;
                    break;
                }
            }
        }

        if (comboText != null)
        {
            SetComboAlpha(0f);
            comboText.text = "";
        }
        if (swipeRightAttackDetector == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                swipeRightAttackDetector = player.GetComponentInChildren<SwipeRightAttackDetector>();
        }
    }
    private void ShowCombo(int count)
    {
        if (comboText == null) return;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = null;

        comboText.text = string.Format(comboFormat, count);
        SetComboAlpha(1f);
        comboPulse?.Pulse();
    }

    private void FadeOutCombo()
    {
        if (comboText == null) return;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeComboCo(0f, comboFadeOutSeconds));
    }

    private System.Collections.IEnumerator FadeComboCo(float target, float seconds)
    {
        float start = comboText.color.a;
        float t = 0f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, target, seconds <= 0f ? 1f : (t / seconds));
            SetComboAlpha(a);
            yield return null;
        }

        SetComboAlpha(target);
        if (Mathf.Approximately(target, 0f))
            comboText.text = "";
        _fadeCo = null;
    }

    private void SetComboAlpha(float a)
    {
        if (comboText == null) return;
        var c = comboText.color;
        c.a = a;
        comboText.color = c;
    }

    private void Update()
    {
        var lm = LevelManager.Instance;
        bool ruthless = (lm != null && lm.RuthlessTapModeEntered);

        if (_lastRuthlessState && !ruthless)
        {
            // Mode ended -> fade out combo
            FadeOutCombo();
        }

        _lastRuthlessState = ruthless;
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isTapCandidate) return;

        if (tapMaxTimeSeconds > 0f && (Time.unscaledTime - _downTime) > tapMaxTimeSeconds)
        {
            return;
        }

        // Confirm movement is still within tap threshold
        float moved = Vector2.Distance(_downPos, eventData.position);
        if (moved > tapMaxMovePixels) return;

        var lm = LevelManager.Instance;
        if (lm != null)
        {
            // If inputs are locked but we're in ruthless mode -> count taps instead of jump
            if (lm.GameplayInputsLocked && lm.RuthlessTapModeEntered)
            {
                lm.RuthlessTapCount++;
                ShowCombo(lm.RuthlessTapCount);
                OnPhase2ComboUpdated?.Invoke(lm.RuthlessTapCount);
                if (awardCashOnRuthlessTap)
                {
                    if (maxCashPerTap < minCashPerTap) maxCashPerTap = minCashPerTap;

                    int cash = Random.Range(minCashPerTap, maxCashPerTap + 1); // 1–7 inclusive
                    OnPhase2CashEarned?.Invoke(cash);
                    SkateRunnerGameManager.SkateRunnerGameManagerAccessor?.AddCash(cash);
                    SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.RefreshCash();

                    // Spawn popup at Enemy Type 3 (ruthless target)
                    var target = lm.RuthlessTapTarget;
                    if (target != null)
                    {
                        SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.SpawnCashPopup(
                            target.position, cash, 0.5f
                        );
                    }
                }

                return;
            }


            if (lm.GameplayInputsLocked && !lm.RuthlessTapModeEntered)
                return;
        }

        // Fire MM Main Action as a "tap"
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SendMessage("MainActionButtonDown");
            InputManager.Instance.SendMessage("MainActionButtonUp");
        }
    }
}
