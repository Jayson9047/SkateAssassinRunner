using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_Consent_Controller : MonoBehaviour
    {
        [Tooltip("Set to true to display the GDPR consent")]
        public bool DisplayConsent = true;
        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        [Tooltip("Button to reset consent.")]
        public Button ResetConsentButton;

        [Tooltip("Button to show user consent settings again.")]
        public Button UpdateConsentButton;

        [Tooltip("GameObject with the error popup.")]
        public GameObject ErrorPopup;

        [Tooltip("Error message for the error popup,")]
        public TextMeshProUGUI ErrorText;

        [Header("Load Ads After Consent")]
        [Tooltip("Drag a MonoBehaviour from the scene with public void methods.")]
        public MonoBehaviour targetScript;

        [HideInInspector]
        [Tooltip("Name of the public void method to call.")]
        public string selectedMethodName;

        private void Start()
        {
            if(DisplayConsent == true)
            {
                if (ErrorPopup != null)
                {
                    ErrorPopup.SetActive(false);
                }

                if(UpdateConsentButton != null)
                {
                    UpdateConsentButton.onClick.AddListener(() => ShowConsentAgain());
                }

                if (ResetConsentButton != null)
                {
                    ResetConsentButton.onClick.AddListener(() => ResetConsentInformation());
                }
               

                bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;

                if (isConnected == true)
                {
                    InitializeGoogleMobileAdsConsent();
                }
            }          
        }
        private void InitializeGoogleMobileAdsConsent()
        {
            Debug.Log("Google Mobile Ads gathering consent.");

            GatherConsent((string error) =>
            {
                if (error != null)
                {
                    Debug.LogError("Failed to gather consent with error: " + error);
                }
                else
                {
                    Debug.Log("Google Mobile Ads consent updated: " + ConsentInformation.ConsentStatus);
                }
            });
        }
        public void ShowConsentAgain()
        {
            ShowConsentOptionsForm((string error) =>
            {
                if (error != null)
                {
                    Debug.LogError("Failed to show consent privacy form with error: " + error);
                }
                else
                {
                    Debug.Log("Privacy form opened successfully.");
                }
            });
        }

        public void GatherConsent(Action<string> onComplete)
        {
            GatherConsent(new ConsentRequestParameters(), onComplete);
        }

        // Project orchestration supplies official development-only UMP test parameters.
        public void GatherConsent(ConsentRequestParameters request, Action<string> onComplete)
        {
            Debug.Log("Gathering consent.");
            onComplete = (onComplete == null) ? UpdateErrorPopup : onComplete + UpdateErrorPopup;

            ConsentInformation.Update(request, (FormError updateError) =>
            {
                UpdateConsentInfo();

                if (updateError != null)
                {
                    onComplete("ConsentInformation.Update [" + updateError.ErrorCode + "]: " + updateError.Message);
                    return;
                }

                // UMP requires this check after every successful launch update,
                // including when cached consent already permits ad requests.
                ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
                {
                    UpdateConsentInfo();
                    if (showError != null)
                    {
                        onComplete?.Invoke("LoadAndShowConsentFormIfRequired [" + showError.ErrorCode + "]: " + showError.Message);
                    }
                    else
                    {
                        onComplete?.Invoke(null);
                    }

                    if (CanRequestAds)
                    {
                        CallSelectedFunction();
                    }
                });
            });
        }

        public void ShowConsentOptionsForm(Action<string> onComplete)
        {
            Debug.Log("Showing consent options form.");

            onComplete = (onComplete == null) ? UpdateErrorPopup : onComplete + UpdateErrorPopup;

            ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
            {
                UpdateConsentInfo();
                if (showError != null)
                {
                    onComplete?.Invoke("ShowPrivacyOptionsForm [" + showError.ErrorCode + "]: " + showError.Message);
                }
                else
                {
                    onComplete?.Invoke(null);
                }
            });
        }

        public void ResetConsentInformation()
        {
            ConsentInformation.Reset();
            UpdateConsentInfo();
        }

        void UpdateConsentInfo()
        {
            if (UpdateConsentButton != null)
            {
                UpdateConsentButton.interactable =
                    ConsentInformation.PrivacyOptionsRequirementStatus ==
                        PrivacyOptionsRequirementStatus.Required;
            }
        }

        void UpdateErrorPopup(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (ErrorText != null)
            {
                ErrorText.text = message;
            }

            if (ErrorPopup != null)
            {
                ErrorPopup.SetActive(true);
            }

            if (UpdateConsentButton != null)
            {
                UpdateConsentButton.interactable = true;
            }
        }

        void CallSelectedFunction()
        {
            if (targetScript != null && !string.IsNullOrEmpty(selectedMethodName))
            {
                MethodInfo method = targetScript.GetType().GetMethod(selectedMethodName, BindingFlags.Public | BindingFlags.Instance);
                if (method != null && method.GetParameters().Length == 0 && method.ReturnType == typeof(void))
                {
                    method.Invoke(targetScript, null);
                }
                else
                {
                    Debug.LogWarning("Selected method is not a valid public void method with no parameters.");
                }
            }
        }
    }
}