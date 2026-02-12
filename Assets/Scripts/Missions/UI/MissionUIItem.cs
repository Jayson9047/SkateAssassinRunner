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

        [Header("Pulse On Complete (optional)")]
        public GUIPulse completePulse;     // assign on the row root OR on tick OR on text
        public GUIPulse tickPulse;         // optional: pulse the tick separately

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
                tickImage.gameObject.SetActive(true);

            if (missionText != null)
            {
                missionText.color = completeColor;
                missionText.fontStyle |= TMPro.FontStyles.Strikethrough;
            }

            // pulse animation(s)
            completePulse?.Pulse();
            tickPulse?.Pulse();
        }



        public void SetProgress(string text)
        {
            if (missionText != null)
                missionText.text = text;
        }
    }
}
