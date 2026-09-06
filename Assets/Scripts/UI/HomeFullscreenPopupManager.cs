using DG.Tweening;
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

    [Header("Page Open Pulse")]
    [SerializeField, Range(0.8f, 1f)] private float pageOpenStartScale = 0.96f;
    [SerializeField, Range(1f, 1.1f)] private float pageOpenOvershootScale = 1.018f;
    [SerializeField, Min(0.01f)] private float pageOpenGrowDuration = 0.14f;
    [SerializeField, Min(0.01f)] private float pageOpenSettleDuration = 0.11f;

    private GameObject _currentPage;
    private Vector3 _currentPageBaseScale = Vector3.one;
    private Sequence _pageOpenPulse;

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
        StopPageOpenPulse();

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
        PlayPageOpenPulse(page);

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

    private void PlayPageOpenPulse(GameObject page)
    {
        StopPageOpenPulse();
        if (!page) return;

        _currentPage = page;
        _currentPageBaseScale = page.transform.localScale;
        page.transform.localScale = _currentPageBaseScale * pageOpenStartScale;

        _pageOpenPulse = DOTween.Sequence().SetUpdate(true);
        _pageOpenPulse.Append(page.transform
            .DOScale(_currentPageBaseScale * pageOpenOvershootScale, pageOpenGrowDuration)
            .SetEase(Ease.OutCubic));
        _pageOpenPulse.Append(page.transform
            .DOScale(_currentPageBaseScale, pageOpenSettleDuration)
            .SetEase(Ease.InOutSine));
        _pageOpenPulse.OnComplete(() => _pageOpenPulse = null);
    }

    private void StopPageOpenPulse()
    {
        _pageOpenPulse?.Kill();
        _pageOpenPulse = null;
        if (_currentPage) _currentPage.transform.localScale = _currentPageBaseScale;
    }

    private void OnDestroy()
    {
        StopPageOpenPulse();
    }
}
