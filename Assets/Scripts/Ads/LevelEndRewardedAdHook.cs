using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LevelEndRewardedAdHook : MonoBehaviour
{
    [SerializeField] private Button button;
    private bool processing, completed;
    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) { button.onClick.RemoveListener(RequestAd); button.onClick.AddListener(RequestAd); }
    }
    private void OnEnable() { processing = false; completed = false; }
    private void RequestAd()
    {
        if (processing || completed) return;
        processing = true;
        if (!RewardedAdBridge.ShowRewardedAd("level_end_multiplier",
            () => { processing = false; completed = true; /* Future multiplier reward hook. */ },
            () => processing = false)) processing = false;
    }
}
