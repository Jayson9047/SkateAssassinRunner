using TMPro;
using System.Collections;
using UnityEngine;

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

    private void Start()
    {
        RefreshFromSave();
    }

    public void RefreshFromSave()
    {
        float totalCash = ES3.Load<float>(ES3_TOTAL_CASH, 0f);
        float totalGems = ES3.Load<float>(ES3_TOTAL_GEMS, 0f);
        int levelNum = ES3.Load<int>(ES3_LEVEL_NUM, 1);

        if (CashText != null)
            CashText.text = totalCash.ToString("N0");

        if (GemsText != null)
            GemsText.text = totalGems.ToString("N0");
        displayedCash = totalCash;
        displayedGems = totalGems;

        if (LevelText != null)
            LevelText.text = $"LEVEL {levelNum}";

        // NEXT LEVEL (Play Button)
        if (PlayButtonLevelText != null)
            PlayButtonLevelText.text = $"LEVEL {levelNum + 1}";
    }

    public void AnimateBalances(float oldCash, float newCash, float oldGems, float newGems)
    {
        if (!isActiveAndEnabled) { RefreshFromSave(); return; }
        float startCash = balanceAnimation != null ? displayedCash : oldCash;
        float startGems = balanceAnimation != null ? displayedGems : oldGems;
        if (balanceAnimation != null) StopCoroutine(balanceAnimation);
        balanceAnimation = StartCoroutine(AnimateRoutine(startCash,newCash,startGems,newGems));
    }
    private IEnumerator AnimateRoutine(float aCash,float bCash,float aGems,float bGems)
    {
        float elapsed=0,duration=Mathf.Max(.01f,balanceAnimationDuration);
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/duration),eased=1-Mathf.Pow(1-t,3);
            displayedCash=Mathf.Lerp(aCash,bCash,eased);displayedGems=Mathf.Lerp(aGems,bGems,eased);
            if(CashText&&!Mathf.Approximately(aCash,bCash))CashText.text=displayedCash.ToString("N0");
            if(GemsText&&!Mathf.Approximately(aGems,bGems))GemsText.text=displayedGems.ToString("N0");
            yield return null;
        }
        displayedCash=bCash;displayedGems=bGems;
        if(CashText)CashText.text=bCash.ToString("N0");if(GemsText)GemsText.text=bGems.ToString("N0");
        balanceAnimation=null;
    }

}
