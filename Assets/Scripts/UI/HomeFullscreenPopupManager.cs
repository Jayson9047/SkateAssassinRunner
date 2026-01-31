using UnityEngine;

public class HomeFullscreenPopupManager : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject homePageRoot;         // heavy homepage-only UI
    [SerializeField] private GameObject fullscreenPopupRoot;  // fader + pages (starts inactive)

    [Header("Top Bar")]
    [SerializeField] private GameObject homeButtonTopBar;     // the Home button object in top bar (disabled by default)

    [Header("Pages")]
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject missionsPage;
    [SerializeField] private GameObject rewardsPage;
    [SerializeField] private GameObject shopPage;

    private GameObject _currentPage;

    private void Awake()
    {
        OpenHome(); // ensures correct default state
    }

    // Button hooks
    public void OpenInventory() => Open(inventoryPage);
    public void OpenMissions() => Open(missionsPage);
    public void OpenRewards() => Open(rewardsPage);
    public void OpenShop() => Open(shopPage);

    // Top bar Home button hook
    public void OpenHome()
    {
        // Turn off all pages
        SetAllPagesInactive();
        _currentPage = null;

        // Hide popup root
        if (fullscreenPopupRoot) fullscreenPopupRoot.SetActive(false);

        // Show homepage
        if (homePageRoot) homePageRoot.SetActive(true);

        // Hide Home button in top bar (because we're already home)
        if (homeButtonTopBar) homeButtonTopBar.SetActive(false);
    }

    private void Open(GameObject page)
    {
        if (!page) return;

        // Make it bulletproof: ensure no other page is active
        SetAllPagesInactive();

        // Hide homepage heavy stuff
        if (homePageRoot) homePageRoot.SetActive(false);

        // Show popup root
        if (fullscreenPopupRoot) fullscreenPopupRoot.SetActive(true);

        // Show the requested page
        page.SetActive(true);
        _currentPage = page;

        // Show Home button in top bar (because we're not on home)
        if (homeButtonTopBar) homeButtonTopBar.SetActive(true);
    }

    private void SetAllPagesInactive()
    {
        if (inventoryPage) inventoryPage.SetActive(false);
        if (missionsPage) missionsPage.SetActive(false);
        if (rewardsPage) rewardsPage.SetActive(false);
        if (shopPage) shopPage.SetActive(false);
    }
}
