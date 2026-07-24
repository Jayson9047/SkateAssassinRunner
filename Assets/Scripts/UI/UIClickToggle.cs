using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIClickToggle : MonoBehaviour, IPointerClickHandler
{
    [Header("Objects To Enable On Click")]
    [SerializeField] private GameObject[] objectsToEnable;

    [Header("Objects To Disable On Click")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("TMP Texts To Turn White")]
    [SerializeField] private TMP_Text[] textsToTurnWhite;

    [Header("TMP Texts To Set To Custom Color")]
    [SerializeField] private TMP_Text[] textsToSetCustomColor;

    [SerializeField] private Color customTextColor = Color.gray;

    [Header("Optional")]
    [SerializeField] private bool disableThisObjectAfterClick = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        ApplyToggle();
    }

    public void ApplyToggle()
    {
        SetObjectsActive(objectsToEnable, true);
        SetObjectsActive(objectsToDisable, false);

        SetTextColors(textsToTurnWhite, Color.white);
        SetTextColors(textsToSetCustomColor, customTextColor);

        if (disableThisObjectAfterClick)
            gameObject.SetActive(false);
    }

    private void SetObjectsActive(GameObject[] targets, bool state)
    {
        if (targets == null) return;

        foreach (GameObject obj in targets)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    private void SetTextColors(TMP_Text[] texts, Color color)
    {
        if (texts == null) return;

        foreach (TMP_Text text in texts)
        {
            if (text != null)
                text.color = color;
        }
    }
}
