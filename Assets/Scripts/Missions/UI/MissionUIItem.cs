using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Elroi.Missions.UI
{
    [System.Serializable]
    public class MissionUIItem
    {
        [Header("Assign in Inspector")]
        public TMP_Text missionText;
        public Image tickImage; // this is the tick (disabled by default)

        [Header("Style")]
        public Color incompleteColor = Color.white;
        public Color completeColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        public void SetInitial(string text)
        {
            if (missionText != null)
            {
                missionText.text = text;
                missionText.color = incompleteColor;
                missionText.fontStyle &= ~TMPro.FontStyles.Strikethrough;
            }

            if (tickImage != null)
                tickImage.gameObject.SetActive(false); // <-- IMPORTANT
        }

        public void SetComplete()
        {
            if (tickImage != null)
                tickImage.gameObject.SetActive(true);  // <-- IMPORTANT

            if (missionText != null)
            {
                missionText.color = completeColor;
                missionText.fontStyle |= TMPro.FontStyles.Strikethrough;
            }
        }


        public void SetProgress(string text)
        {
            if (missionText != null)
                missionText.text = text;
        }
    }
}
