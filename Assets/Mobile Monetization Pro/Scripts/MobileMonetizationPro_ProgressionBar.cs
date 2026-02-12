using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_ProgressionBar : MonoBehaviour
    {
        public Image fillImage;  // Image component to fill
        public float fillSpeed = 0.1f;  // Speed at which the fill completes

        public Button LoadLevelButton;

        private void Start()
        {
            if (fillImage == null)
            {
                Debug.LogError("Fill Image is not assigned!");
                return;  // Exit if no image assigned
            }

            fillImage.fillAmount = 0f; // Ensure starting from 0
            StartFilling(); // Start filling normally if assigned
        }
        private void Update()
        {
            // FOR ADMOB ONLY -- PLEASE UNCOMMENT BOTH OF THESE LINES AFTER ADDING THE ADMOB SDK
            //if(MobileMonetizationPro.MobileMonetizationPro_AdmobAdsInitializer.instance.IsRewardedAdCompleted == true)
            //{
            //    // give reward
            //    LoadLevelButton.gameObject.SetActive(true);
            //    MobileMonetizationPro.MobileMonetizationPro_AdmobAdsInitializer.instance.IsRewardedAdCompleted = false;
            //}


            // FOR LEVELPLAY ONLY -- PLEASE UNCOMMENT BOTH OF THESE LINES AFTER ADDING THE LEVELPLAY SDK
            //if (MobileMonetizationPro.MobileMonetizationPro_LevelPlayInitializer.instance.IsRewardedAdCompleted == true)
            //{
            //    // give reward
            //    LoadLevelButton.gameObject.SetActive(true);
            //    MobileMonetizationPro.MobileMonetizationPro_LevelPlayInitializer.instance.IsRewardedAdCompleted = false;
            //}
        }
        public void StartFilling()
        {
            StartCoroutine(FillProgressionBar());
        }

        private System.Collections.IEnumerator FillProgressionBar()
        {
            while (fillImage.fillAmount < 1f)
            {
                fillImage.fillAmount += fillSpeed * Time.deltaTime;  // Increment fill amount based on time
                fillImage.fillAmount = Mathf.Clamp01(fillImage.fillAmount); // Clamp to make sure it doesn't overshoot
                yield return null;  // Wait for next frame
            }

            // Fill completed
            Debug.Log("Fill Completed!");

            // FOR ADMOB ONLY -- PLEASE UNCOMMENT BOTH OF THESE LINES AFTER ADDING THE ADMOB SDK
            //if (MobileMonetizationPro.MobileMonetizationPro_AdmobAdsInitializer.instance != null)
            //    MobileMonetizationPro.MobileMonetizationPro_AdmobAdsInitializer.instance.ShowRewardedAd();

            //FOR LEVELPLAY ONLY-- PLEASE UNCOMMENT BOTH OF THESE LINES AFTER ADDING THE LEVELPLAY SDK
            //if (MobileMonetizationPro.MobileMonetizationPro_LevelPlayInitializer.instance != null)
            //    MobileMonetizationPro.MobileMonetizationPro_LevelPlayInitializer.instance.ShowRewarded();
        }
    }
}