using UnityEngine;

public class CashRewardOnDestroyed : MonoBehaviour
{
    [Header("Cash Reward")]
    [SerializeField] private bool enabledReward = true;
    [SerializeField] private int minCash = 5;
    [SerializeField] private int maxCash = 10;

    public bool EnabledReward => enabledReward;

    public int GetRandomCash()
    {
        if (!enabledReward) return 0;

        if (maxCash < minCash)
        {
            maxCash = minCash;
        }

        // Unity int Random.Range is max-exclusive
        return Random.Range(minCash, maxCash + 1);
    }
}
