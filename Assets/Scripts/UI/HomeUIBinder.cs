using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class HomeUIBinder : MonoBehaviour
{
    private const string ES3_TOTAL_CASH = "TotalCash";
    private const string ES3_TOTAL_GEMS = "TotalGems";
    private const string ES3_LEVEL_NUM = "LevelNum";

    public TextMeshProUGUI CashText;
    public TextMeshProUGUI GemsText;
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI PlayButtonLevelText;
    [SerializeField, Range(.25f, 2f)] private float balanceAnimationDuration = .9f;
    private Coroutine balanceAnimation;
    private float displayedCash, displayedGems;

    private void Start() => RefreshFromSave();
    private void OnEnable() => SkateLocalization.LocaleChanged += OnLocaleChanged;
    private void OnDisable() => SkateLocalization.LocaleChanged -= OnLocaleChanged;
    private void OnLocaleChanged(Locale locale) => RefreshFromSave();

    public void RefreshFromSave()
    {
        float totalCash = ES3.Load<float>(ES3_TOTAL_CASH, 0f);
        float totalGems = ES3.Load<float>(ES3_TOTAL_GEMS, 0f);
        int levelNum = ES3.Load<int>(ES3_LEVEL_NUM, 1);

        if (CashText != null) CashText.text = LocalizedAmount(totalCash);
        if (GemsText != null) GemsText.text = LocalizedAmount(totalGems);
        displayedCash = totalCash;
        displayedGems = totalGems;
        if (LevelText != null)
            LevelText.text = SkateLocalization.Get("Home", "home.level", SkateLocalization.FormatNumber(levelNum));
        if (PlayButtonLevelText != null)
            PlayButtonLevelText.text = SkateLocalization.Get("Home", "home.level", SkateLocalization.FormatNumber(levelNum + 1));
    }

    public void AnimateBalances(float oldCash, float newCash, float oldGems, float newGems)
    {
        if (!isActiveAndEnabled) { RefreshFromSave(); return; }
        float startCash = balanceAnimation != null ? displayedCash : oldCash;
        float startGems = balanceAnimation != null ? displayedGems : oldGems;
        if (balanceAnimation != null) StopCoroutine(balanceAnimation);
        balanceAnimation = StartCoroutine(AnimateRoutine(startCash, newCash, startGems, newGems));
    }

    private IEnumerator AnimateRoutine(float aCash, float bCash, float aGems, float bGems)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(.01f, balanceAnimationDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            displayedCash = Mathf.Lerp(aCash, bCash, eased);
            displayedGems = Mathf.Lerp(aGems, bGems, eased);
            if (CashText && !Mathf.Approximately(aCash, bCash)) CashText.text = LocalizedAmount(displayedCash);
            if (GemsText && !Mathf.Approximately(aGems, bGems)) GemsText.text = LocalizedAmount(displayedGems);
            yield return null;
        }
        displayedCash = bCash;
        displayedGems = bGems;
        if (CashText) CashText.text = LocalizedAmount(bCash);
        if (GemsText) GemsText.text = LocalizedAmount(bGems);
        balanceAnimation = null;
    }

    private static string LocalizedAmount(float value)
        => SkateLocalization.FormatNumber((long)Math.Round(value));
}
