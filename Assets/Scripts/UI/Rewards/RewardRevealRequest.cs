using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public enum RewardRevealType
{
    Cash,
    Gems,
    Sword,
    Ability,
    Rollerblade
}

[Serializable]
public sealed class RewardRevealEntry
{
    public RewardRevealType type;
    public string displayName;
    public int amount;
    public Sprite icon;
    public Vector2 displaySize;
    public AnimationClip previewAnimation;

    public bool IsCurrency => type == RewardRevealType.Cash || type == RewardRevealType.Gems;

    public string BuildLabel()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.ToUpperInvariant();

        if (!IsCurrency)
            return type.ToString().ToUpperInvariant();

        string currencyName = type == RewardRevealType.Cash ? "CASH" : "GEMS";
        return Mathf.Max(0, amount).ToString("N0", CultureInfo.InvariantCulture) + " " + currencyName;
    }
}

/// <summary>Data-only request consumed by the shared Crystal reward reveal.</summary>
public sealed class RewardRevealRequest
{
    public string title;
    public RewardRevealEntry primary;
    public RewardRevealEntry secondary;
    public Action onClosed;

    public bool HasSecondary => secondary != null;

    public static RewardRevealRequest ForCurrencies(
        int cash,
        int gems,
        Sprite cashIcon = null,
        Sprite gemIcon = null,
        Action onClosed = null,
        string title = "REWARD UNLOCKED!")
    {
        RewardRevealEntry primary = null;
        RewardRevealEntry secondary = null;

        if (cash > 0)
            primary = Currency(RewardRevealType.Cash, cash, cashIcon);
        if (gems > 0)
        {
            RewardRevealEntry gemsEntry = Currency(RewardRevealType.Gems, gems, gemIcon);
            if (primary == null) primary = gemsEntry;
            else secondary = gemsEntry;
        }

        return primary == null
            ? null
            : new RewardRevealRequest
            {
                title = title,
                primary = primary,
                secondary = secondary,
                onClosed = onClosed
            };
    }

    public static RewardRevealRequest ForItem(
        RewardRevealType type,
        string displayName,
        Sprite icon,
        Action onClosed = null,
        string title = "NEW ITEM UNLOCKED!",
        AnimationClip previewAnimation = null)
    {
        if (type == RewardRevealType.Cash || type == RewardRevealType.Gems)
            throw new ArgumentException("Use ForCurrencies for Cash and Gems rewards.", nameof(type));

        return new RewardRevealRequest
        {
            title = title,
            primary = new RewardRevealEntry
            {
                type = type,
                displayName = displayName,
                icon = icon,
                previewAnimation = previewAnimation
            },
            onClosed = onClosed
        };
    }

    static RewardRevealEntry Currency(RewardRevealType type, int amount, Sprite icon)
    {
        return new RewardRevealEntry { type = type, amount = amount, icon = icon };
    }
}

public static class RewardRevealIconUtility
{
    /// <summary>Finds the product art already displayed by a Shop card.</summary>
    public static Sprite FindProductSprite(Transform cardRoot)
    {
        Image image = FindProductImage(cardRoot);
        return image ? image.sprite : null;
    }

    /// <summary>Copies the actual on-screen product footprint from its Shop card.</summary>
    public static Vector2 FindProductDisplaySize(Transform cardRoot)
    {
        Image image = FindProductImage(cardRoot);
        RectTransform product = image ? image.transform as RectTransform : null;
        if (!product) return Vector2.zero;

        Vector3 scale = product.localScale;
        Vector2 size = product.rect.size;
        return new Vector2(size.x * Mathf.Abs(scale.x), size.y * Mathf.Abs(scale.y));
    }

    /// <summary>Copies the title text visibly printed on an equipment Shop card.</summary>
    public static string FindProductTitle(Transform cardRoot, string fallback)
    {
        if (!cardRoot) return fallback;

        TMPro.TMP_Text[] labels = cardRoot.GetComponentsInChildren<TMPro.TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMPro.TMP_Text label = labels[i];
            if (!label || string.IsNullOrWhiteSpace(label.text)) continue;

            string labelName = label.name;
            bool isCost = labelName.IndexOf("cost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          labelName.IndexOf("price", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isCost && labelName.StartsWith("Text_", StringComparison.OrdinalIgnoreCase))
                return label.text.Trim();
        }

        return fallback;
    }

    /// <summary>Returns the same per-Ability animation used by the Inventory preview.</summary>
    public static AnimationClip FindAbilityPreviewAnimation(WeaponPowerId powerId)
    {
        WeaponPowerInventoryController controller = UnityEngine.Object.FindFirstObjectByType<WeaponPowerInventoryController>(
            FindObjectsInactive.Include);
        if (!controller) return null;

        AnimationClip clip;
        return controller.TryGetPreviewAnimation(powerId, out clip) ? clip : null;
    }

    static Image FindProductImage(Transform cardRoot)
    {
        if (!cardRoot) return null;

        string[] preferredPaths =
        {
            "ProductImageMask/ProductImage",
            "ProductImage",
            "Chest/Image_SpecialChest",
            "Image_Cash",
            "Image_Gem"
        };

        for (int i = 0; i < preferredPaths.Length; i++)
        {
            Transform candidate = cardRoot.Find(preferredPaths[i]);
            Image image = candidate ? candidate.GetComponent<Image>() : null;
            if (image && image.sprite) return image;
        }

        Image fallback = null;
        Image[] images = cardRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (!image || !image.sprite || image.transform == cardRoot) continue;
            if (image.name == "Image_SpecialChest" || image.name == "Image_Cash" || image.name == "Image_Gem")
                return image;
            if (!fallback && image.name.IndexOf("product", StringComparison.OrdinalIgnoreCase) >= 0)
                fallback = image;
        }

        return fallback;
    }
}
