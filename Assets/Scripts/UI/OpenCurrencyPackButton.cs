using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OpenCurrencyPackButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private HomeFullscreenPopupManager popupManager;
    [SerializeField] private UIClickToggle currencyPackToggle;
    private Coroutine routine;
    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) { button.onClick.RemoveListener(OpenCurrencyPack); button.onClick.AddListener(OpenCurrencyPack); }
    }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(OpenCurrencyPack); }
    public void OpenCurrencyPack()
    {
        if (routine != null) StopCoroutine(routine);
        popupManager?.OpenShop();
        routine = StartCoroutine(ApplyNextFrame());
    }
    private IEnumerator ApplyNextFrame()
    {
        yield return null;
        currencyPackToggle?.ApplyToggle();
        routine = null;
    }
}
