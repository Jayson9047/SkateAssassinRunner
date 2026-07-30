using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Swaps an inventory card's existing frame while it is equipped and restores
/// the original frame when it is not. Selection remains controller-owned.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class InventoryEquippedCardVisual : MonoBehaviour
{
    [SerializeField] private Image cardBackground;
    [SerializeField] private Sprite equippedCardSprite;

    private Sprite normalCardSprite;
    private bool normalSpriteCached;

    public void SetEquipped(bool equipped)
    {
        CacheNormalSprite();

        if (cardBackground == null)
            return;

        cardBackground.sprite = equipped && equippedCardSprite != null
            ? equippedCardSprite
            : normalCardSprite;
    }

    private void CacheNormalSprite()
    {
        if (normalSpriteCached)
            return;

        if (cardBackground == null)
            cardBackground = GetComponent<Image>();

        normalCardSprite = cardBackground != null ? cardBackground.sprite : null;
        normalSpriteCached = true;
    }
}
