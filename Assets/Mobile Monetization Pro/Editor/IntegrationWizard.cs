using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MobileMonetizationPro
{
    public class IntegrationWizard : EditorWindow
    {

        private int selectedIntegration = 0;
        private int selectedAdNetwork = 0;
        private string[] integrationOptions = new string[] { "Consent Manager","Mobile Ads", "In App Purchase", "Firebase Analytics", "Mobile Notifications", "App Tracking Transparency Popup",
                                                            "In App Update","User Ratings Popup","Remote Notifications","Cross Promotion" };
        private string[] adNetworkOptions = new string[] { "Admob", "LevelPlay Or UnityAds", "Applovin", "LiftOff(Vungle)" };
        private string[] adNetworkUrls = new string[] { "https://developers.google.com/admob/unity/quick-start", "https://developers.is.com/ironsource-mobile/unity/unity-plugin/#step-1",
            "https://dash.applovin.com/documentation/mediation/unity/getting-started/integration","https://support.vungle.com/hc/en-us/articles/360003455452-Integrate-Vungle-SDK-for-Unity" };

        [MenuItem("Tools/Mobile Monetization Pro/Integration Tool", false, 0)]
        public static void OpenGettingStartedWindow()
        {
            IntegrationWizard window = GetWindow<IntegrationWizard>(true, "Mobile Monetization Pro", true);
            window.ShowUtility();
        }
        private void OnGUI()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 25,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUILayout.Label("Integration Tool", labelStyle);

            selectedIntegration = EditorGUILayout.Popup("Choose Integration", selectedIntegration, integrationOptions);

            switch (selectedIntegration)
            {
                case 0:
                    DrawConsentManagerImportButton();
                    break;
                case 1: // Mobile Ads
                    DrawMobileAdsIntegration();
                    break;
                case 2: // In App Purchase
                    DrawInAppPurchaseIntegration();
                    break;
                case 3: // Firebase Analytics
                    DrawFirebaseAnalyticsIntegration();
                    break;
                case 4: // Mobile Notifications
                    DrawMobileNotificationsIntegration();
                    break;
                case 5: // App Tracking Transparency Popup
                    DrawATTIntegration();
                    break;
                case 6: // In App Update
                    DrawInAppUpdatePackageImportButton();
                    break;
                case 7:
                    DrawUserRatingPopupIntegration();
                    break;
                case 8:
                    DrawRemoteNotificationsImportButton();
                    break;
                case 9: 
                    DrawCrossPromotionIntegration();
                    break;
            }
            
            GUILayout.Space(10);
        }
        private Dictionary<string, string> adNetworkUnityPackages = new Dictionary<string, string>()
     {
    { "Admob", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_AdmobAds.unitypackage" },
    { "LevelPlay Or UnityAds", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_LevelPlayAds.unitypackage" },
    { "Applovin", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_ApplovinAds.unitypackage" },
    { "LiftOff(Vungle)", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_LiftOffAds.unitypackage" }
    };

        private void DrawMobileAdsIntegration()
        {
            selectedAdNetwork = EditorGUILayout.Popup("Choose AdNetwork", selectedAdNetwork, adNetworkOptions);

            GUILayout.Label("Step 1: Download" + " " + adNetworkOptions[selectedAdNetwork] + " " + "SDK");

            if (selectedAdNetwork != 0)
            {
                if (GUILayout.Button("Download" + " " + adNetworkOptions[selectedAdNetwork] + " " + "SDK"))
                {
                    if (selectedAdNetwork == 1)
                    {
                        OpenPackageManagerAndSearch();
                    }
                    else
                    {
                        if (selectedAdNetwork > 0 && selectedAdNetwork < adNetworkUrls.Length)
                        {
                            string documentationLink = adNetworkUrls[selectedAdNetwork];
                            Application.OpenURL(documentationLink);
                        }
                        else
                        {
                            Debug.LogWarning("Please choose an Ad Network.");
                        }
                    }

                }
            }


            if (selectedAdNetwork == 0) // Assuming "Admob(GDPR)" is the first option in adNetworkOptions
            {
                if (GUILayout.Button("Download" + " " + adNetworkOptions[selectedAdNetwork] + " " + "SDK"))
                {
                    string documentationLink = adNetworkUrls[selectedAdNetwork];
                    Application.OpenURL(documentationLink);
                }

                // GUILayout.Label("Admob Native SDK is Deprecated so for now you don't have to import it");

                GUILayout.Label("Step 2: AdMob Native SDK has been deprecated, so you no longer need to import it.");

                //GUILayout.Label("Step 2: Download Admob Native SDK");

                //if (GUILayout.Button("Download Admob Native SDK"))
                //{
                //    Application.OpenURL("https://developers.google.com/admob/unity/native");
                //}

                GUILayout.Label("Step 3: Download Adapters For Mediation (Ignore this step if you only want to use Admob)");

                if (GUILayout.Button("Download Adapters"))
                {
                    Application.OpenURL("https://developers.google.com/admob/unity/choose-networks");
                }

                GUILayout.Label("Step 4: Import Mobile Monetization Pro Required Scripts");

                if (GUILayout.Button("Import Required Scripts"))
                {
                    string selectedAdNetworkOption = adNetworkOptions[selectedAdNetwork];
                    if (adNetworkUnityPackages.ContainsKey(selectedAdNetworkOption))
                    {
                        string packagePath = adNetworkUnityPackages[selectedAdNetworkOption];
                        AssetDatabase.ImportPackage(packagePath, true);
                        Debug.Log($"Imported {selectedAdNetworkOption} Unity package.");
                    }
                    else
                    {
                        Debug.LogWarning($"No Unity package found for {selectedAdNetworkOption}.");
                    }
                }
            }
            else
            {
                GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

                if (GUILayout.Button("Import Required Scripts"))
                {
                    if (selectedAdNetwork > 0 && selectedAdNetwork < adNetworkOptions.Length)
                    {
                        string selectedAdNetworkOption = adNetworkOptions[selectedAdNetwork];
                        if (adNetworkUnityPackages.ContainsKey(selectedAdNetworkOption))
                        {
                            string packagePath = adNetworkUnityPackages[selectedAdNetworkOption];
                            AssetDatabase.ImportPackage(packagePath, true);
                            Debug.Log($"Imported {selectedAdNetworkOption} Unity package.");
                        }
                        else
                        {
                            Debug.LogWarning($"No Unity package found for {selectedAdNetworkOption}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Please choose an Ad Network.");
                    }
                }
            }

            GUILayout.Space(10);

            if (selectedAdNetwork == 0 || selectedAdNetwork == 2 || selectedAdNetwork == 3)
            {
                if (GUILayout.Button("Download Google Mobile Ads SDK For GDPR"))
                {
                    Application.OpenURL("https://developers.google.com/admob/unity/quick-start");
                }

            }

            GUILayout.Space(10);

            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }


            //if (selectedAdNetwork == 0) // Assuming "Admob(GDPR)" is the first option in adNetworkOptions
            //{
            //    GUILayout.Label("Step 3: Download Adapters For Mediation");

            //    if (GUILayout.Button("Download Adapters"))
            //    {
            //        Application.OpenURL("https://developers.google.com/admob/unity/choose-networks");
            //    }
            //}
        }

        private void DrawInAppPurchaseIntegration()
        {
            GUILayout.Label("Step 1: Download IAP from Package manager");
            if (GUILayout.Button("Download IAP from Package manager"))
            {
                OpenPackageManagerAndSearch();
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportQuickMonetizationIAPManager();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }
        private void DrawUserRatingPopupIntegration()
        {
            GUILayout.Label("Step 1: Download Android In App Review");
            if (GUILayout.Button("Download Android In App Review Unity Package"))
            {
                Application.OpenURL("https://developer.android.com/guide/playcore/in-app-review/unity");
            }
            GUILayout.Label("Step 2: Import User Rating Popup Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportQuickMonetizationUserRatingManager();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }
        private void DrawCrossPromotionIntegration()
        {
            GUILayout.Label("Already exist in project.");

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }

        private void DrawFirebaseAnalyticsIntegration()
        {
            GUILayout.Label("Step 1: Download Firebase Sdk");
            if (GUILayout.Button("Download Firebase Sdk"))
            {
                Application.OpenURL("https://firebase.google.com/docs/unity/setup");
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportFirebaseUnityPackage();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }

        private void DrawATTIntegration()
        {
            GUILayout.Label("Step 1: Download ATT From package manager");
            if (GUILayout.Button("Download ATT From package manager"))
            {
                OpenPackageManagerAndSearch();
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportATTUnityPackage();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }

        private void DrawMobileNotificationsIntegration()
        {
            GUILayout.Label("Step 1: Download Mobile Notifications from Package manager");
            if (GUILayout.Button("Download Mobile Notifications from Package manager"))
            {
                OpenPackageManagerAndSearch();
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportMobileNotifUnityPackage();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }
        private void DrawInAppUpdatePackageImportButton()
        {
            GUILayout.Label("Download Android In App Update");
            if (GUILayout.Button("Download Android In App Update Unity Package"))
            {
                Application.OpenURL("https://developer.android.com/guide/playcore/in-app-updates/unity");
            }

            GUILayout.Label("Import Mobile Monetization Pro Required Scripts");
            if (GUILayout.Button("Import Required script"))
            {
                string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_InAppUpdate.unitypackage";

                AssetDatabase.ImportPackage(packagePath, true);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }
        private void DrawRemoteNotificationsImportButton()
        {
            GUILayout.Label("Import Mobile Monetization Pro Required Scripts");
            if (GUILayout.Button("Import Required script"))
            {
                string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_RemoteNotifications.unitypackage";

                AssetDatabase.ImportPackage(packagePath, true);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }
        private void DrawConsentManagerImportButton()
        {
            GUILayout.Label("Import Mobile Monetization Pro Required Scripts");
            if (GUILayout.Button("Import Required script"))
            {
                string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_ConsentManager.unitypackage";

                AssetDatabase.ImportPackage(packagePath, true);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLHcU8LK4tXHxxhGk7g993czfDWD9_BVNL");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sourav1570.github.io/Mobile-Monetization-Pro-Doc/");
            }
        }
        private static void OpenPackageManagerAndSearch()
        {
            // Open Unity Package Manager
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }
        private static void ImportQuickMonetizationIAPManager()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_InAppPurchases.unitypackage";

            // Import QuickMonetization IAPManager package
            AssetDatabase.ImportPackage(packagePath, true);
        }
        private static void ImportQuickMonetizationUserRatingManager()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_UserRatingsPopup.unitypackage";

            // Import QuickMonetization IAPManager package
            AssetDatabase.ImportPackage(packagePath, true);
        }

        private static void ImportFirebaseUnityPackage()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_FirebaseImplementation.unitypackage";

            // Import Firebase Unity package
            AssetDatabase.ImportPackage(packagePath, true);
        }
        private static void ImportMobileNotifUnityPackage()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_MobileNotifications.unitypackage";

            // Import Firebase Unity package
            AssetDatabase.ImportPackage(packagePath, true);
        }

        private static void ImportATTUnityPackage()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_ATTScreenManager.unitypackage";

            // Import ATT Unity package
            AssetDatabase.ImportPackage(packagePath, true);
        }
    }
}