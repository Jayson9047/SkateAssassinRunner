using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(ShopPricingCatalog))]
public sealed class ShopPricingCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(12f);
        if (GUILayout.Button("UPDATE PRICE", GUILayout.Height(34f)))
            ShopPricingCatalogSync.UpdateLoadedShop((ShopPricingCatalog)target, true);
        EditorGUILayout.HelpBox(
            "Prices become live only when UPDATE PRICE is pressed. The button synchronizes the loaded Start Screen cards and retained legacy fields, then saves explicitly.",
            MessageType.Info);
    }
}

public static class ShopPricingCatalogSync
{
    public const string CatalogPath = "Assets/Prefabs/UI/ShopPricingCatalog.asset";

    [MenuItem("Tools/Skate Runner/Shop/Select Pricing Catalog")]
    public static void SelectCatalog()
    {
        ShopPricingCatalog catalog = EnsureCatalog();
        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
    }

    [MenuItem("Tools/Skate Runner/Shop/UPDATE PRICE From Catalog")]
    public static void UpdateFromMenu()
    {
        UpdateLoadedShop(EnsureCatalog(), true);
    }

    public static ShopPricingCatalog EnsureCatalog()
    {
        ShopPricingCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopPricingCatalog>(CatalogPath);
        if (catalog != null) return catalog;

        catalog = ScriptableObject.CreateInstance<ShopPricingCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        SerializedObject so = new SerializedObject(catalog);

        SerializedProperty abilities = so.FindProperty("abilities");
        AddOneTime(abilities, (int)WeaponPowerId.Fire, ShopPaymentType.Gems, 500, "", "");
        AddOneTime(abilities, (int)WeaponPowerId.Ice, ShopPaymentType.Gems, 500, "", "");
        AddOneTime(abilities, (int)WeaponPowerId.Poison, ShopPaymentType.Gems, 750, "", "");
        AddOneTime(abilities, (int)WeaponPowerId.Electricity, ShopPaymentType.Gems, 1000, "", "");
        AddOneTime(abilities, (int)WeaponPowerId.Magic, ShopPaymentType.RealMoney, 0, "$0.99 USD", "ability_magic");

        SerializedProperty swords = so.FindProperty("swords");
        AddOneTime(swords, (int)SwordId.HellForge, ShopPaymentType.RealMoney, 0, "$1.99 USD", "sword_hellforge");
        AddOneTime(swords, (int)SwordId.Emberguard, ShopPaymentType.Cash, 25000, "", "");
        AddOneTime(swords, (int)SwordId.Gravebreaker, ShopPaymentType.Cash, 35000, "", "");
        AddOneTime(swords, (int)SwordId.GlacierCipher, ShopPaymentType.Gems, 1000, "", "");
        AddOneTime(swords, (int)SwordId.Wyrmshade, ShopPaymentType.Gems, 1000, "", "");
        AddOneTime(swords, (int)SwordId.Sunspire, ShopPaymentType.Gems, 1500, "", "");
        AddOneTime(swords, (int)SwordId.Bloodreaver, ShopPaymentType.Gems, 2000, "", "");

        SerializedProperty rollers = so.FindProperty("rollerblades");
        AddOneTime(rollers, (int)RollerbladeId.InfernoDrift, ShopPaymentType.RealMoney, 0, "$1.99 USD", "roller_inferno_drift");
        AddOneTime(rollers, (int)RollerbladeId.UrbanRush, ShopPaymentType.Cash, 25000, "", "");
        AddOneTime(rollers, (int)RollerbladeId.NeonVelocity, ShopPaymentType.Cash, 45000, "", "");
        AddOneTime(rollers, (int)RollerbladeId.CelestialApex, ShopPaymentType.Gems, 1500, "", "");
        AddOneTime(rollers, (int)RollerbladeId.FrostbiteGlide, ShopPaymentType.Gems, 2500, "", "");

        SerializedProperty packs = so.FindProperty("currencyPacks");
        AddPack(packs, CurrencyPackProductId.Gems500, ShopPaymentType.RealMoney, 0, "$0.99 USD", "gems_500", 500, 0, "500 Gems");
        AddPack(packs, CurrencyPackProductId.Gems1050, ShopPaymentType.RealMoney, 0, "$1.99 USD", "gems_1050", 1050, 0, "1,050 Gems");
        AddPack(packs, CurrencyPackProductId.Gems1600, ShopPaymentType.RealMoney, 0, "$2.99 USD", "gems_1600", 1600, 0, "1,600 Gems");
        AddPack(packs, CurrencyPackProductId.Gems2750, ShopPaymentType.RealMoney, 0, "$4.99 USD", "gems_2750", 2750, 0, "2,750 Gems");
        AddPack(packs, CurrencyPackProductId.Gems5750, ShopPaymentType.RealMoney, 0, "$9.99 USD", "gems_5750", 5750, 0, "5,750 Gems");
        AddPack(packs, CurrencyPackProductId.FeaturedGem13000, ShopPaymentType.RealMoney, 0, "$19.99 USD", "gems_13000", 13000, 0, "13,000 Gems");
        AddPack(packs, CurrencyPackProductId.FeaturedCash110000, ShopPaymentType.RealMoney, 0, "$4.99 USD", "cash_110000", 0, 110000, "110,000 Cash");
        AddPack(packs, CurrencyPackProductId.Cash200For5Gems, ShopPaymentType.Gems, 5, "", "", 0, 200, "200 Cash");
        AddPack(packs, CurrencyPackProductId.Cash4000For100Gems, ShopPaymentType.Gems, 100, "", "", 0, 4000, "4,000 Cash");
        AddPack(packs, CurrencyPackProductId.Cash20000For500Gems, ShopPaymentType.Gems, 500, "", "", 0, 20000, "20,000 Cash");
        AddPack(packs, CurrencyPackProductId.Cash40000For1000Gems, ShopPaymentType.Gems, 1000, "", "", 0, 40000, "40,000 Cash");
        AddPack(packs, CurrencyPackProductId.Cash60000For1500Gems, ShopPaymentType.Gems, 1500, "", "", 0, 60000, "60,000 Cash");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    public static void UpdateLoadedShop(ShopPricingCatalog catalog, bool logSummary)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        int controllers = 0, cards = 0;
        var modifiedScenes = new HashSet<Scene>();
        foreach (WeaponPowerShopController c in UnityEngine.Object.FindObjectsByType<WeaponPowerShopController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { SetCatalog(c, catalog); modifiedScenes.Add(c.gameObject.scene); controllers++; }
        foreach (SwordShopController c in UnityEngine.Object.FindObjectsByType<SwordShopController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { SetCatalog(c, catalog); modifiedScenes.Add(c.gameObject.scene); controllers++; }
        foreach (RollerbladeShopController c in UnityEngine.Object.FindObjectsByType<RollerbladeShopController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { SetCatalog(c, catalog); modifiedScenes.Add(c.gameObject.scene); controllers++; }
        foreach (CurrencyPackShopController c in UnityEngine.Object.FindObjectsByType<CurrencyPackShopController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { SetCatalog(c, catalog); modifiedScenes.Add(c.gameObject.scene); controllers++; }

        foreach (WeaponPowerShopItem item in UnityEngine.Object.FindObjectsByType<WeaponPowerShopItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ShopPricingCatalog.AbilityPrice p; if (!catalog.TryGet(item.PowerId, out p)) continue;
            SerializedObject so = new SerializedObject(item);
            SetEnum(so, "purchaseType", p.paymentType == ShopPaymentType.Gems ? 0 : 1);
            SetInt(so, "gemCost", p.cost); SetString(so, "realMoneyDisplayPrice", p.realMoneyPrice);
            SetIconActive(so, "gemIcon", p.paymentType == ShopPaymentType.Gems);
            ApplyCard(so, p.paymentType, p.cost, p.realMoneyPrice); modifiedScenes.Add(item.gameObject.scene); cards++;
        }
        foreach (SwordShopItem item in UnityEngine.Object.FindObjectsByType<SwordShopItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ShopPricingCatalog.SwordPrice p; if (!catalog.TryGet(item.SwordId, out p)) continue;
            SerializedObject so = new SerializedObject(item);
            SetEnum(so, "purchaseType", LegacyOneTimeType(p.paymentType));
            SetInt(so, "gemCost", p.paymentType == ShopPaymentType.Gems ? p.cost : 0);
            SetInt(so, "cashCost", p.paymentType == ShopPaymentType.Cash ? p.cost : 0);
            SetString(so, "realMoneyConfirmationPrice", p.realMoneyPrice); SetString(so, "realMoneyCardPrice", p.realMoneyPrice);
            SetIconActive(so, "currencyIcon", p.paymentType != ShopPaymentType.RealMoney);
            ApplyCard(so, p.paymentType, p.cost, p.realMoneyPrice); modifiedScenes.Add(item.gameObject.scene); cards++;
        }
        foreach (RollerbladeShopItem item in UnityEngine.Object.FindObjectsByType<RollerbladeShopItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ShopPricingCatalog.RollerbladePrice p; if (!catalog.TryGet(item.RollerbladeId, out p)) continue;
            SerializedObject so = new SerializedObject(item);
            SetEnum(so, "purchaseType", LegacyOneTimeType(p.paymentType));
            SetInt(so, "gemCost", p.paymentType == ShopPaymentType.Gems ? p.cost : 0);
            SetInt(so, "cashCost", p.paymentType == ShopPaymentType.Cash ? p.cost : 0);
            SetString(so, "realMoneyConfirmationPrice", p.realMoneyPrice); SetString(so, "realMoneyCardPrice", p.realMoneyPrice);
            SetIconActive(so, "currencyIcon", p.paymentType != ShopPaymentType.RealMoney);
            ApplyCard(so, p.paymentType, p.cost, p.realMoneyPrice); modifiedScenes.Add(item.gameObject.scene); cards++;
        }
        foreach (CurrencyPackShopItem item in UnityEngine.Object.FindObjectsByType<CurrencyPackShopItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ShopPricingCatalog.CurrencyPackPrice p; if (!catalog.TryGet(item.ProductId, out p)) continue;
            SerializedObject so = new SerializedObject(item);
            int legacyType = p.paymentType == ShopPaymentType.Gems ? 2 : (p.gemsGranted > 0 ? 0 : 1);
            SetEnum(so, "purchaseType", legacyType); SetInt(so, "gemsCost", p.paymentType == ShopPaymentType.Gems ? p.cost : 0);
            SetInt(so, "gemsGranted", p.gemsGranted); SetInt(so, "cashGranted", p.cashGranted);
            SetString(so, "realMoneyDisplayPrice", p.realMoneyPrice); SetString(so, "cardCostText", ShopPricingCatalog.FormatCardPrice(p.paymentType, p.cost, p.realMoneyPrice));
            SetString(so, "displayProductName", p.displayName); SetString(so, "storeProductId", p.storeProductId);
            ApplyCard(so, p.paymentType, p.cost, p.realMoneyPrice); modifiedScenes.Add(item.gameObject.scene); cards++;
        }

        EditorUtility.SetDirty(catalog);
        foreach (Scene scene in modifiedScenes)
        {
            if (scene.IsValid() && scene.isLoaded) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        AssetDatabase.SaveAssets();
        if (logSummary) Debug.Log(string.Format(CultureInfo.InvariantCulture, "UPDATE PRICE complete: {0} catalog references and {1} Shop cards synchronized and saved.", controllers, cards));
    }

    private static void SetCatalog(UnityEngine.Object controller, ShopPricingCatalog catalog)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("pricingCatalog").objectReferenceValue = catalog;
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(controller);
    }

    private static void ApplyCard(SerializedObject so, ShopPaymentType payment, int cost, string realPrice)
    {
        SerializedProperty textProperty = so.FindProperty("costText");
        TMPro.TMP_Text text = textProperty != null ? textProperty.objectReferenceValue as TMPro.TMP_Text : null;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (text != null) { text.text = ShopPricingCatalog.FormatCardPrice(payment, cost, realPrice); EditorUtility.SetDirty(text); }
        EditorUtility.SetDirty(so.targetObject);
    }

    private static void SetIconActive(SerializedObject so, string propertyName, bool active)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        GameObject icon = property != null ? property.objectReferenceValue as GameObject : null;
        if (icon != null) { icon.SetActive(active); EditorUtility.SetDirty(icon); }
    }

    private static int LegacyOneTimeType(ShopPaymentType value) { return value == ShopPaymentType.Gems ? 0 : value == ShopPaymentType.Cash ? 1 : 2; }
    private static void SetEnum(SerializedObject so, string name, int value) { so.FindProperty(name).enumValueIndex = value; }
    private static void SetInt(SerializedObject so, string name, int value) { so.FindProperty(name).intValue = value; }
    private static void SetString(SerializedObject so, string name, string value) { so.FindProperty(name).stringValue = value ?? string.Empty; }

    private static void AddOneTime(SerializedProperty list, int id, ShopPaymentType payment, int cost, string realPrice, string storeId)
    {
        int index = list.arraySize++; SerializedProperty p = list.GetArrayElementAtIndex(index);
        p.FindPropertyRelative("id").enumValueIndex = id; p.FindPropertyRelative("paymentType").enumValueIndex = (int)payment;
        p.FindPropertyRelative("cost").intValue = cost; p.FindPropertyRelative("realMoneyPrice").stringValue = realPrice;
        p.FindPropertyRelative("storeProductId").stringValue = storeId;
    }

    private static void AddPack(SerializedProperty list, CurrencyPackProductId id, ShopPaymentType payment, int cost, string realPrice, string storeId, int gems, int cash, string displayName)
    {
        int index = list.arraySize++; SerializedProperty p = list.GetArrayElementAtIndex(index);
        p.FindPropertyRelative("id").enumValueIndex = (int)id; p.FindPropertyRelative("paymentType").enumValueIndex = (int)payment;
        p.FindPropertyRelative("cost").intValue = cost; p.FindPropertyRelative("realMoneyPrice").stringValue = realPrice;
        p.FindPropertyRelative("storeProductId").stringValue = storeId; p.FindPropertyRelative("gemsGranted").intValue = gems;
        p.FindPropertyRelative("cashGranted").intValue = cash; p.FindPropertyRelative("displayName").stringValue = displayName;
    }
}
