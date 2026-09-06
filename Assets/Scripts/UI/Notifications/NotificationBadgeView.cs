using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NotificationBadgeView : MonoBehaviour
{
    [SerializeField] private GameObject badgeRoot;
    [SerializeField] private TMP_Text countText;

    private void Awake()
    {
        if (badgeRoot == null) badgeRoot = gameObject;
        if (countText == null) countText = GetComponentInChildren<TMP_Text>(true);
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;
    }

    public void SetCount(int count)
    {
        int safeCount = Mathf.Max(0, count);
        if (countText != null) countText.text = safeCount > 99 ? "99+" : safeCount.ToString();
        GameObject root = badgeRoot != null ? badgeRoot : gameObject;
        if (root.activeSelf != (safeCount > 0)) root.SetActive(safeCount > 0);
    }
}
