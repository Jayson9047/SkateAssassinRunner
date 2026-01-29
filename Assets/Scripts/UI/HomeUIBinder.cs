using TMPro;
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
            CashText.text = totalCash.ToString("0");

        if (GemsText != null)
            GemsText.text = totalGems.ToString("0");

        if (LevelText != null)
            LevelText.text = $"LEVEL {levelNum}";

        // NEXT LEVEL (Play Button)
        if (PlayButtonLevelText != null)
            PlayButtonLevelText.text = $"LEVEL {levelNum + 1}";
    }

}
