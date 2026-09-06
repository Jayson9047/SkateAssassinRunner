using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopPaymentType
{
    Gems = 0,
    Cash = 1,
    RealMoney = 2
}

[CreateAssetMenu(menuName = "Skate Runner/Shop Pricing Catalog", fileName = "ShopPricingCatalog")]
public sealed class ShopPricingCatalog : ScriptableObject
{
    [Serializable] public sealed class AbilityPrice
    {
        public WeaponPowerId id;
        public ShopPaymentType paymentType;
        [Min(0)] public int cost;
        public string realMoneyPrice;
        public string storeProductId;
    }

    [Serializable] public sealed class SwordPrice
    {
        public SwordId id;
        public ShopPaymentType paymentType;
        [Min(0)] public int cost;
        public string realMoneyPrice;
        public string storeProductId;
    }

    [Serializable] public sealed class RollerbladePrice
    {
        public RollerbladeId id;
        public ShopPaymentType paymentType;
        [Min(0)] public int cost;
        public string realMoneyPrice;
        public string storeProductId;
    }

    [Serializable] public sealed class CurrencyPackPrice
    {
        public CurrencyPackProductId id;
        public ShopPaymentType paymentType;
        [Min(0)] public int cost;
        public string realMoneyPrice;
        public string storeProductId;
        [Min(0)] public int gemsGranted;
        [Min(0)] public int cashGranted;
        public string displayName;
    }

    [Header("ABILITIES")]
    [SerializeField] private List<AbilityPrice> abilities = new List<AbilityPrice>();
    [Header("SWORDS")]
    [SerializeField] private List<SwordPrice> swords = new List<SwordPrice>();
    [Header("ROLLERBLADES")]
    [SerializeField] private List<RollerbladePrice> rollerblades = new List<RollerbladePrice>();
    [Header("CURRENCY PACKS")]
    [SerializeField] private List<CurrencyPackPrice> currencyPacks = new List<CurrencyPackPrice>();

    public IReadOnlyList<AbilityPrice> Abilities => abilities;
    public IReadOnlyList<SwordPrice> Swords => swords;
    public IReadOnlyList<RollerbladePrice> Rollerblades => rollerblades;
    public IReadOnlyList<CurrencyPackPrice> CurrencyPacks => currencyPacks;

    public bool TryGet(WeaponPowerId id, out AbilityPrice result) { result = abilities.Find(x => x != null && x.id == id); return result != null; }
    public bool TryGet(SwordId id, out SwordPrice result) { result = swords.Find(x => x != null && x.id == id); return result != null; }
    public bool TryGet(RollerbladeId id, out RollerbladePrice result) { result = rollerblades.Find(x => x != null && x.id == id); return result != null; }
    public bool TryGet(CurrencyPackProductId id, out CurrencyPackPrice result) { result = currencyPacks.Find(x => x != null && x.id == id); return result != null; }

    public static string FormatCardPrice(ShopPaymentType paymentType, int cost, string realMoneyPrice)
    {
        return paymentType == ShopPaymentType.RealMoney ? realMoneyPrice : cost.ToString("N0");
    }

    public static string FormatConfirmationPrice(ShopPaymentType paymentType, int cost, string realMoneyPrice)
    {
        if (paymentType == ShopPaymentType.RealMoney) return realMoneyPrice;
        return cost.ToString("N0") + (paymentType == ShopPaymentType.Gems ? " Gems" : " Cash");
    }
}
