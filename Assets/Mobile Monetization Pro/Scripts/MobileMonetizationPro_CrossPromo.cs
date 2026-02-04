using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_CrossPromo : MonoBehaviour
    {
        public static MobileMonetizationPro_CrossPromo instance;

        [System.Serializable]
        public enum CrossPromotype
        {
            [Tooltip("Choose to display a video cross-promotion.")]
            VideoType,
            [Tooltip("Choose to display an image cross-promotion.")]
            ImageType
        }

        [Tooltip("Select the type of cross-promotion to display.")]
        public CrossPromotype ChooseCrossPromoType;

        [System.Serializable]
        public enum OptionsForDisplayingSprites
        {
            [Tooltip("Display the sprites in a random order.")]
            DisplayRandomly,
            [Tooltip("Display the sprites sequentially.")]
            DisplaySequencly,
        }

        [Tooltip("Video player component used to play video cross-promotions.")]
        public VideoPlayer videoPlayer;

        [Tooltip("Choose how to display the sprites.")]
        public OptionsForDisplayingSprites ChooseDisplayOption;

        [System.Serializable]
        public class SpritesWithlink
        {
            [Tooltip("The sprite that will be displayed for the cross-promotion.")]
            public Sprite SpriteToDisplay;
            [Tooltip("The link to the game associated with this sprite.")]
            public string LinkToGame;
            [Tooltip("Button used for downloading the game associated with this sprite.")]
            public Button DownloadButton;
        }

        [System.Serializable]
        public class VideosWithlink
        {
            [Tooltip("The video clip to display as a cross-promotion.")]
            public VideoClip VideoToDisplay;
            [Tooltip("The link to the game associated with this video.")]
            public string LinkToGame;
            [Tooltip("Button used for downloading the game associated with this video.")]
            public Button DownloadButton;
        }

        [Tooltip("RawImage component for displaying video or image.")]
        public RawImage RawImageComponent;

        [Tooltip("Render texture component for the video player.")]
        public RenderTexture RenderTextureComponent;

        [Tooltip("Image component for displaying image cross-promotions.")]
        public Image ImageComponent;

        [Tooltip("List of video cross-promotions to display.")]
        public List<VideosWithlink> AddVideos = new List<VideosWithlink>();

        [Tooltip("List of image cross-promotions to display.")]
        public List<SpritesWithlink> AddSprites = new List<SpritesWithlink>();

        [System.Serializable]
        public enum OptionsToChangeSprites
        {
            [Tooltip("Change the sprite based on a timer.")]
            BasedOnTimer,
            [Tooltip("Change the sprite based on the number of sessions.")]
            BasedOnSession,
            [Tooltip("Change the sprite based on the number of app opens.")]
            BasedOnAppOpens,
        }

        [Tooltip("Determine when the next promo should be displayed.")]
        public OptionsToChangeSprites DecideWhenToShowNextPromo;

        [Tooltip("Number of app opens to check before showing a new cross-promotion.")]
        public int NoOfAppOpensToCheckBeforeNewPromo = 2;

        [Tooltip("Number of sessions to check before showing a new cross-promotion.")]
        public int NoOfSessionsToCheckBeforeNewPromo = 20;

        [Tooltip("Minimum time (in seconds) to wait before changing the cross-promotion.")]
        public float MinTimeToWaitBeforeChangingPromo = 10;

        [Tooltip("Maximum time (in seconds) to wait before changing the cross-promotion.")]
        public float MaxTimeToWaitBeforeChangingPromo = 20;

        [Tooltip("If true, stop cross-promotion after the user clicks.")]
        public bool StopCrossPromotionAfterClick = true;

        [Tooltip("GameObject to deactivate when cross-promotion ends.")]
        public GameObject CrossPromotionToDeactive;

        int Count;
        int AppOpenCount;
        string DownloadLink;
        string link;


        private void Start()
        {
            ShowSprites();
        }
        public void ShowSprites()
        {
            if (PlayerPrefs.GetInt("DoNotDisplayCrossPromo") == 0)
            {
                Count = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                int GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);

                bool GoForIt = false;
                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                {
                    if (GetSpriteNumber < AddSprites.Count)
                    {
                        GoForIt = true;
                    }
                }
                else
                {
                    if (GetSpriteNumber < AddVideos.Count)
                    {
                        GoForIt = true;
                    }
                }

                if (GoForIt)
                {
                    if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnTimer)
                    {
                        if (MobileMonetizationPro_CrossPromoManager.instance.CanChangePromoImage == true || MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession == false)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplayRandomly)
                            {
                                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                                {
                                    Count = Random.Range(0, AddSprites.Count);
                                }
                                else
                                {
                                    Count = Random.Range(0, AddVideos.Count);
                                }

                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            MobileMonetizationPro_CrossPromoManager.instance.BeginTimerForChangingCrossPromo(MinTimeToWaitBeforeChangingPromo, MaxTimeToWaitBeforeChangingPromo);
                            MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession = true;
                        }
                    }
                    else if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnSession)
                    {
                        if (MobileMonetizationPro_CrossPromoManager.instance.SessionCounts >= NoOfSessionsToCheckBeforeNewPromo || MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession == false)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplayRandomly)
                            {
                                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                                {
                                    Count = Random.Range(0, AddSprites.Count);
                                }
                                else
                                {
                                    Count = Random.Range(0, AddVideos.Count);
                                }
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplaySequencly && MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession == true)
                            {
                                ++Count;
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            MobileMonetizationPro_CrossPromoManager.instance.SessionCounts = 0;
                            MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession = true;
                        }
                        MobileMonetizationPro_CrossPromoManager.instance.SessionChecks();
                    }
                    else if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnAppOpens)
                    {
                        if (PlayerPrefs.GetInt("CrossPromoAppOpenCount") >= NoOfAppOpensToCheckBeforeNewPromo)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplayRandomly)
                            {
                                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                                {
                                    Count = Random.Range(0, AddSprites.Count);
                                }
                                else
                                {
                                    Count = Random.Range(0, AddVideos.Count);
                                }
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplaySequencly)
                            {
                                ++Count;
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            PlayerPrefs.SetInt("CrossPromoAppOpenCount", 0);
                        }

                        if (PlayerPrefs.GetInt("CrossPromoIsAppOpened") == 0)
                        {
                            AppOpenCount = PlayerPrefs.GetInt("CrossPromoAppOpenCount", 0);
                            AppOpenCount++;
                            PlayerPrefs.SetInt("CrossPromoAppOpenCount", AppOpenCount);
                            PlayerPrefs.Save();

                            PlayerPrefs.SetInt("CrossPromoIsAppOpened", 1);
                        }
                    }
                }

                GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);

                bool CheckForSprite = false;

                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                {
                    if (GetSpriteNumber >= AddSprites.Count)
                    {
                        PlayerPrefs.SetInt("PromoSpriteToDisplay", 0);
                        MobileMonetizationPro_CrossPromoManager.instance.BeginTimerForChangingCrossPromo(MinTimeToWaitBeforeChangingPromo, MaxTimeToWaitBeforeChangingPromo);
                    }
                    GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                    ImageComponent.sprite = AddSprites[GetSpriteNumber].SpriteToDisplay;
                    link = AddSprites[GetSpriteNumber].LinkToGame;
                    GameToDownload();
                    AddSprites[GetSpriteNumber].DownloadButton.onClick.AddListener(() => ButtonToClick());
                }
                else
                {
                    if (GetSpriteNumber >= AddVideos.Count)
                    {
                        PlayerPrefs.SetInt("PromoSpriteToDisplay", 0);
                        MobileMonetizationPro_CrossPromoManager.instance.BeginTimerForChangingCrossPromo(MinTimeToWaitBeforeChangingPromo, MaxTimeToWaitBeforeChangingPromo);
                    }

                    GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                    videoPlayer.clip = AddVideos[GetSpriteNumber].VideoToDisplay;
                    videoPlayer.targetTexture = RenderTextureComponent;
                    link = AddVideos[GetSpriteNumber].LinkToGame;
                    GameToDownload();
                    AddVideos[GetSpriteNumber].DownloadButton.onClick.AddListener(() => ButtonToClick());
                }



            }
            else
            {
                CrossPromotionToDeactive.SetActive(false);
            }
        }
        public void GameToDownload()
        {
            DownloadLink = link;
        }
        public void ButtonToClick()
        {
            Application.OpenURL(DownloadLink);
            if (StopCrossPromotionAfterClick == true)
            {
                CrossPromotionToDeactive.SetActive(false);
                PlayerPrefs.SetInt("DoNotDisplayCrossPromo", 1);
            }
        }
        private void Update()
        {
            if (PlayerPrefs.GetInt("DoNotDisplayCrossPromo") == 0)
            {
                if (MobileMonetizationPro_CrossPromoManager.instance != null)
                {
                    if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnTimer)
                    {
                        if (MobileMonetizationPro_CrossPromoManager.instance.CanChangePromoImage == true)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplaySequencly)
                            {
                                Count = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                                ++Count;
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            ShowSprites();
                            MobileMonetizationPro_CrossPromoManager.instance.CanChangePromoImage = false;
                        }
                    }
                }
            }
        }

    }
}
