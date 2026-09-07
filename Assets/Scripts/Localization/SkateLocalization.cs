using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;

/// <summary>
/// Small display-only facade over Unity Localization. Unity Localization remains
/// the authoritative locale and string database; gameplay/save identifiers never
/// pass through this class.
/// </summary>
public static class SkateLocalization
{
    public const string SelectedLocalePlayerPrefsKey = "SkateRunner.Localization.SelectedLocale";
    public const string DefaultLocaleCode = "en";

    public static event Action<Locale> LocaleChanged
    {
        add => LocalizationSettings.SelectedLocaleChanged += value;
        remove => LocalizationSettings.SelectedLocaleChanged -= value;
    }

    public static string CurrentLocaleCode
    {
        get
        {
            Locale locale = LocalizationSettings.SelectedLocale;
            return locale != null ? locale.Identifier.Code : DefaultLocaleCode;
        }
    }

    public static string Get(string table, string key, params object[] arguments)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return LocalizationSettings.StringDatabase.GetLocalizedString(
            table,
            key,
            LocalizationSettings.SelectedLocale,
            FallbackBehavior.UseProjectSettings,
            arguments);
    }

    public static bool SelectLocale(string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode))
        {
            return false;
        }

        string normalizedCode = NormalizeLocaleCode(localeCode);
        if (!IsProductionLocaleCode(normalizedCode))
        {
            return false;
        }

        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(normalizedCode));
        if (locale == null || locale is PseudoLocale)
        {
            return false;
        }

        PlayerPrefs.SetString(SelectedLocalePlayerPrefsKey, locale.Identifier.Code);
        PlayerPrefs.Save();
        LocalizationSettings.SelectedLocale = locale;
        return true;
    }

    public static string GetNativeLanguageName(string localeCode)
    {
        switch (NormalizeLocaleCode(localeCode))
        {
            case "es": return "Español";
            case "nl": return "Nederlands";
            case "fr": return "Français";
            case "pt-BR": return "Português (Brasil)";
            case "de": return "Deutsch";
            default: return "English";
        }
    }

    public static string FormatNumber(long value)
    {
        return value.ToString("N0", GetCulture());
    }

    public static string FormatNumber(int value)
    {
        return value.ToString("N0", GetCulture());
    }

    public static CultureInfo GetCulture()
    {
        string code = NormalizeLocaleCode(CurrentLocaleCode);
        try
        {
            return CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    public static string GetAbilityDisplayName(WeaponPowerId id, bool includePower = false)
    {
        string suffix = includePower ? "_power" : string.Empty;
        switch (id)
        {
            case WeaponPowerId.Fire: return Get("Items", "items.ability.fire" + suffix);
            case WeaponPowerId.Ice: return Get("Items", "items.ability.ice" + suffix);
            case WeaponPowerId.Electricity: return Get("Items", "items.ability.electricity" + suffix);
            case WeaponPowerId.Poison: return Get("Items", "items.ability.poison" + suffix);
            case WeaponPowerId.Magic: return Get("Items", "items.ability.magic" + suffix);
            default: return string.Empty;
        }
    }

    public static string GetSwordDisplayName(SwordId id)
    {
        switch (id)
        {
            case SwordId.Bloodreaver: return Get("Items", "items.sword.bloodreaver");
            case SwordId.Emberguard: return Get("Items", "items.sword.emberguard");
            case SwordId.GlacierCipher: return Get("Items", "items.sword.glacier_cipher");
            case SwordId.Gravebreaker: return Get("Items", "items.sword.gravebreaker");
            case SwordId.HellForge: return Get("Items", "items.sword.hellforge");
            case SwordId.Sunspire: return Get("Items", "items.sword.sunspire");
            case SwordId.Wyrmshade: return Get("Items", "items.sword.wyrmshade");
            default: return Get("Items", "items.sword.katana");
        }
    }

    public static string GetRollerbladeDisplayName(RollerbladeId id)
    {
        switch (id)
        {
            case RollerbladeId.UrbanRush: return Get("Items", "items.rollerblade.urbanrush");
            case RollerbladeId.NeonVelocity: return Get("Items", "items.rollerblade.neonvelocity");
            case RollerbladeId.FrostbiteGlide: return Get("Items", "items.rollerblade.frostbiteglide");
            case RollerbladeId.InfernoDrift: return Get("Items", "items.rollerblade.infernodrift");
            case RollerbladeId.CelestialApex: return Get("Items", "items.rollerblade.celestialapex");
            default: return Get("Items", "items.rollerblade.default");
        }
    }

    public static string BuildShopConfirmation(ShopPaymentType type, int cost, string realMoneyPrice, string itemName)
    {
        if (type == ShopPaymentType.RealMoney)
            return Get("Shop", "shop.confirm_real_money", realMoneyPrice, itemName);
        string amount = FormatNumber(cost);
        return Get("Shop", type == ShopPaymentType.Gems ? "shop.confirm_gems" : "shop.confirm_cash", amount, itemName);
    }

    public static string LocalizeKnownItemDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return string.Empty;
        string compact = displayName.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        switch (compact)
        {
            case "fire": return Get("Items", "items.ability.fire");
            case "firepower": return Get("Items", "items.ability.fire_power");
            case "ice": return Get("Items", "items.ability.ice");
            case "icepower": return Get("Items", "items.ability.ice_power");
            case "electricity": return Get("Items", "items.ability.electricity");
            case "electricitypower": return Get("Items", "items.ability.electricity_power");
            case "poison": return Get("Items", "items.ability.poison");
            case "poisonpower": return Get("Items", "items.ability.poison_power");
            case "magic": return Get("Items", "items.ability.magic");
            case "magicpower": return Get("Items", "items.ability.magic_power");
            case "bloodreaver": return Get("Items", "items.sword.bloodreaver");
            case "emberguard": return Get("Items", "items.sword.emberguard");
            case "hellforge": return Get("Items", "items.sword.hellforge");
            case "gravebreaker": return Get("Items", "items.sword.gravebreaker");
            case "glaciercipher": return Get("Items", "items.sword.glacier_cipher");
            case "wyrmshade": return Get("Items", "items.sword.wyrmshade");
            case "sunspire": return Get("Items", "items.sword.sunspire");
            case "urbanrush": return Get("Items", "items.rollerblade.urbanrush");
            case "neonvelocity": return Get("Items", "items.rollerblade.neonvelocity");
            case "frostbiteglide": return Get("Items", "items.rollerblade.frostbiteglide");
            case "infernodrift": return Get("Items", "items.rollerblade.infernodrift");
            case "celestialapex": return Get("Items", "items.rollerblade.celestialapex");
            case "energen": return Get("Items", "items.rollerblade.energen");
            case "firo": return Get("Items", "items.rollerblade.firo");
            default: return displayName;
        }
    }

    public static string NormalizeLocaleCode(string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode)) return DefaultLocaleCode;
        string code = localeCode.Replace('_', '-');
        if (code.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return "pt-BR";
        if (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-Hans";
        if (code.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return "es";
        if (code.StartsWith("nl", StringComparison.OrdinalIgnoreCase)) return "nl";
        if (code.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return "fr";
        if (code.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return "de";
        return "en";
    }

    public static bool IsProductionLocaleCode(string localeCode)
    {
        switch (localeCode)
        {
            case "en":
            case "es":
            case "nl":
            case "fr":
            case "pt-BR":
            case "de":
                return true;
            default:
                return false;
        }
    }
}
