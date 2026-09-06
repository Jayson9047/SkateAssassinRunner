using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopProductImageSync
{
    private const string ScenePath = "Assets/Scenes/SkateRunnerStartScreen.unity";
    private const float AbilityProductImageScale = 1.3f;
    private const float RollerbladeProductImageScale = 0.85f;

    [MenuItem("Tools/Skate Runner/Sync Shop Product Images")]
    public static void SyncFromMenu()
    {
        int count = SyncLoadedScene(true);
        Debug.Log("Shop Product Image sync complete: " + count + " cards now use their matching Inventory Character sprite.");
    }

    public static int SyncLoadedScene(bool saveScene)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException("Open Assets/Scenes/SkateRunnerStartScreen.unity before syncing Shop images.");
        return SyncScene(scene, saveScene);
    }

    public static int SyncScene(Scene scene, bool saveScene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            throw new InvalidOperationException("The loaded Start Screen scene is required for Shop image sync.");
        var abilitySprites = FindInScene<WeaponPowerInventorySlot>(scene)
            .ToDictionary(x => x.PowerId, x => FindCharacterSprite(x.transform));
        var swordSprites = FindInScene<SwordInventorySlot>(scene)
            .ToDictionary(x => x.SwordId, x => FindCharacterSprite(x.transform));
        var rollerSprites = FindInScene<RollerbladeInventorySlot>(scene)
            .ToDictionary(x => x.RollerbladeId, x => FindCharacterSprite(x.transform));

        int count = 0;
        foreach (WeaponPowerShopItem item in FindInScene<WeaponPowerShopItem>(scene))
            if (abilitySprites.TryGetValue(item.PowerId, out Sprite sprite) && sprite != null) { SetProductImage(item.transform, sprite, AbilityProductImageScale); count++; }
        foreach (SwordShopItem item in FindInScene<SwordShopItem>(scene))
            if (swordSprites.TryGetValue(item.SwordId, out Sprite sprite) && sprite != null) { SetProductImage(item.transform, sprite, 1f); count++; }
        foreach (RollerbladeShopItem item in FindInScene<RollerbladeShopItem>(scene))
            if (rollerSprites.TryGetValue(item.RollerbladeId, out Sprite sprite) && sprite != null) { SetProductImage(item.transform, sprite, RollerbladeProductImageScale); count++; }

        EditorSceneManager.MarkSceneDirty(scene);
        if (saveScene) EditorSceneManager.SaveScene(scene);
        return count;
    }

    private static T[] FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
    }

    private static Sprite FindCharacterSprite(Transform slot)
    {
        Transform character = slot.Find("Mask/Character");
        Image image = character != null ? character.GetComponent<Image>() : null;
        return image != null ? image.sprite : null;
    }

    private static void SetProductImage(Transform card, Sprite sprite, float imageScale)
    {
        RectTransform mask = card.Find("ProductImageMask") as RectTransform;
        if (mask == null)
        {
            GameObject maskObject = new GameObject("ProductImageMask", typeof(RectTransform), typeof(RectMask2D));
            mask = (RectTransform)maskObject.transform;
            mask.SetParent(card, false);
        }
        mask.SetAsFirstSibling();
        mask.anchorMin = new Vector2(0.08f, 0.18f); mask.anchorMax = new Vector2(0.92f, 0.82f);
        mask.offsetMin = mask.offsetMax = Vector2.zero; mask.pivot = new Vector2(0.5f, 0.5f);

        RectTransform imageRect = mask.Find("ProductImage") as RectTransform;
        if (imageRect == null)
        {
            GameObject imageObject = new GameObject("ProductImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageRect = (RectTransform)imageObject.transform;
            imageRect.SetParent(mask, false);
        }
        imageRect.anchorMin = Vector2.zero; imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = imageRect.offsetMax = Vector2.zero; imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.localScale = Vector3.one * Mathf.Max(0.1f, imageScale);
        Image image = imageRect.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        UIPulse pulse = imageRect.GetComponent<UIPulse>();
        if (pulse == null) pulse = imageRect.gameObject.AddComponent<UIPulse>();
        pulse.minScale = 0.95f;
        pulse.maxScale = 1.05f;
        pulse.speed = 2.5f;
        pulse.useUnscaledTime = true;
        EditorUtility.SetDirty(pulse); EditorUtility.SetDirty(image); EditorUtility.SetDirty(card.gameObject);
    }
}

public static class StartScreenNotificationAuthoring
{
    private const string ScenePath = "Assets/Scenes/SkateRunnerStartScreen.unity";

    [MenuItem("Tools/Skate Runner/Build Notification Badges")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException("Open Assets/Scenes/SkateRunnerStartScreen.unity before building notification badges.");

        Transform template = FindPath("StartScreenCanvas/Background/HomepageRoot/Bottom (1)/MainMenu/Button_Inventory/Alert_l_Red");
        if (template == null) throw new InvalidOperationException("The existing Inventory Alert_l_Red template is missing.");

        NotificationBadgeView inventoryHome = EnsureBadge(template, template.parent, false);
        NotificationBadgeView missionsHome = EnsureBadge(FindPath("StartScreenCanvas/Background/HomepageRoot/Bottom (1)/MainMenu/Button_Missions/Alert_l_Red"), null, false);
        NotificationBadgeView rewardsHome = EnsureBadge(FindPath("StartScreenCanvas/Background/HomepageRoot/Bottom (1)/MainMenu/Button_Rewards/Alert_l_Red"), null, false);

        SwordInventoryController swordController = UnityEngine.Object.FindFirstObjectByType<SwordInventoryController>(FindObjectsInactive.Include);
        WeaponPowerInventoryController abilityController = UnityEngine.Object.FindFirstObjectByType<WeaponPowerInventoryController>(FindObjectsInactive.Include);
        RollerbladeInventoryController rollerController = UnityEngine.Object.FindFirstObjectByType<RollerbladeInventoryController>(FindObjectsInactive.Include);
        NotificationBadgeView swordCategoryBadge = BuildGroup(swordController.gameObject, InventoryNotificationCategory.Swords,
            FindPath("StartScreenCanvas/Background/FullscreenPopupRoot/InventoryPage/Left/MidScreenLeft/Tap_Menu/GameObject/Swords"),
            UnityEngine.Object.FindObjectsByType<SwordInventorySlot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(x => new KeyValuePair<int, Transform>((int)x.SwordId, x.transform)).ToArray(), template);
        NotificationBadgeView abilityCategoryBadge = BuildGroup(abilityController.gameObject, InventoryNotificationCategory.Abilities,
            FindPath("StartScreenCanvas/Background/FullscreenPopupRoot/InventoryPage/Left/MidScreenLeft/Tap_Menu/GameObject/Abilities"),
            UnityEngine.Object.FindObjectsByType<WeaponPowerInventorySlot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(x => new KeyValuePair<int, Transform>((int)x.PowerId, x.transform)).ToArray(), template);
        NotificationBadgeView rollerCategoryBadge = BuildGroup(rollerController.gameObject, InventoryNotificationCategory.Rollerblades,
            FindPath("StartScreenCanvas/Background/FullscreenPopupRoot/InventoryPage/Left/MidScreenLeft/Tap_Menu/GameObject/RollerBlades"),
            UnityEngine.Object.FindObjectsByType<RollerbladeInventorySlot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(x => new KeyValuePair<int, Transform>((int)x.RollerbladeId, x.transform)).ToArray(), template);

        Transform inventoryPage = FindPath("StartScreenCanvas/Background/FullscreenPopupRoot/InventoryPage");
        if (inventoryPage == null) throw new InvalidOperationException("InventoryPage is missing.");
        InventoryNotificationBadgeController inventoryBadges = inventoryPage.GetComponent<InventoryNotificationBadgeController>();
        if (inventoryBadges == null) inventoryBadges = inventoryPage.gameObject.AddComponent<InventoryNotificationBadgeController>();
        SerializedObject inventoryBadgeSo = new SerializedObject(inventoryBadges);
        inventoryBadgeSo.FindProperty("swordsBadge").objectReferenceValue = swordCategoryBadge;
        inventoryBadgeSo.FindProperty("abilitiesBadge").objectReferenceValue = abilityCategoryBadge;
        inventoryBadgeSo.FindProperty("rollerbladesBadge").objectReferenceValue = rollerCategoryBadge;
        inventoryBadgeSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(inventoryBadges);

        DailyRewardsPage rewardsPage = UnityEngine.Object.FindFirstObjectByType<DailyRewardsPage>(FindObjectsInactive.Include);
        string[] dayNames = { "Day1_List", "Day2_List", "Day3_LIst", "Day4_LIst", "Day5_LIst", "Day6_LIst", "Reward_Day7" };
        for (int i = 0; i < dayNames.Length; i++)
        {
            Transform day = FindDescendant(rewardsPage.transform, dayNames[i]);
            if (day != null) EnsureBadge(day.Find("Alert_l_Red"), day, true, template);
        }

        foreach (Elroi.DailyMissions.UI.DailyMissionRowUI row in UnityEngine.Object.FindObjectsByType<Elroi.DailyMissions.UI.DailyMissionRowUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            NotificationBadgeView badge = EnsureBadge(row.transform.Find("Alert_l_Red"), row.transform, true, template);
            SerializedObject so = new SerializedObject(row); so.FindProperty("notificationBadge").objectReferenceValue = badge; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(row);
        }

        GameObject canvas = GameObject.Find("StartScreenCanvas");
        HomeMenuNotificationController home = canvas.GetComponent<HomeMenuNotificationController>();
        if (home == null) home = canvas.AddComponent<HomeMenuNotificationController>();
        SerializedObject homeSo = new SerializedObject(home);
        homeSo.FindProperty("inventoryBadge").objectReferenceValue = inventoryHome;
        homeSo.FindProperty("missionsBadge").objectReferenceValue = missionsHome;
        homeSo.FindProperty("rewardsBadge").objectReferenceValue = rewardsHome;
        homeSo.FindProperty("dailyRewardsPage").objectReferenceValue = rewardsPage;
        homeSo.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(home);

        DisableDecorativeRaycasts(scene);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Notification badges built for Home, Inventory categories/cards, Daily Rewards, and Daily Missions. No Shop badge was created or wired.");
    }

    private static NotificationBadgeView BuildGroup(GameObject host, InventoryNotificationCategory category, Transform categoryTab, KeyValuePair<int, Transform>[] items, Transform template)
    {
        InventoryNotificationGroupView group = host.GetComponent<InventoryNotificationGroupView>();
        if (group == null) group = host.AddComponent<InventoryNotificationGroupView>();
        NotificationBadgeView categoryBadge = EnsureBadge(categoryTab.Find("Alert_l_Red"), categoryTab, true, template);
        SerializedObject so = new SerializedObject(group);
        so.FindProperty("category").enumValueIndex = (int)category;
        SerializedProperty array = so.FindProperty("itemBadges"); array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            NotificationBadgeView badge = EnsureBadge(items[i].Value.Find("Alert_l_Red"), items[i].Value, true, template);
            SerializedProperty entry = array.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("itemId").intValue = items[i].Key;
            entry.FindPropertyRelative("badge").objectReferenceValue = badge;
        }
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(group);
        return categoryBadge;
    }

    private static NotificationBadgeView EnsureBadge(Transform existing, Transform parent, bool positionAtCorner, Transform cloneTemplate = null)
    {
        Transform badge = existing;
        if (badge == null)
        {
            if (cloneTemplate == null) throw new InvalidOperationException("A required authored Alert_l_Red is missing.");
            badge = UnityEngine.Object.Instantiate(cloneTemplate.gameObject, parent, false).transform;
            badge.name = "Alert_l_Red";
        }
        if (positionAtCorner)
        {
            RectTransform rect = badge as RectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-6f, -6f);
            rect.localScale = Vector3.one * 0.75f;
        }
        NotificationBadgeView view = badge.GetComponent<NotificationBadgeView>();
        if (view == null) view = badge.gameObject.AddComponent<NotificationBadgeView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("badgeRoot").objectReferenceValue = badge.gameObject;
        so.FindProperty("countText").objectReferenceValue = badge.GetComponentInChildren<TMPro.TMP_Text>(true);
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach (Graphic graphic in badge.GetComponentsInChildren<Graphic>(true)) { graphic.raycastTarget = false; EditorUtility.SetDirty(graphic); }
        badge.gameObject.SetActive(false); EditorUtility.SetDirty(view); return view;
    }

    private static void DisableDecorativeRaycasts(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (NotificationBadgeView badge in root.GetComponentsInChildren<NotificationBadgeView>(true))
                foreach (Graphic graphic in badge.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
    }

    private static Transform FindPath(string path) { GameObject go = GameObject.Find(path); return go != null ? go.transform : FindAllPath(path); }
    private static Transform FindAllPath(string path)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || !go.scene.IsValid()) continue;
            string p = go.name; Transform t = go.transform; while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            if (p == path) return go.transform;
        }
        return null;
    }
    private static Transform FindDescendant(Transform root, string name) { return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name); }
}
