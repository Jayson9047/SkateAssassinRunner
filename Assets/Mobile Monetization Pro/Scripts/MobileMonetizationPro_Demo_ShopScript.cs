using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;


namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_Demo_ShopScript : MonoBehaviour
    {
        public static MobileMonetizationPro_Demo_ShopScript ins;

        public GameObject RewardPanel;
        public GameObject RestartAdsObject;
        public GameObject RemoveAdsObject;
        public GameObject SubscriptionObject;
        public GameObject NoSubscriptionObject;

        public GameObject VipActive;
        public GameObject BuyVip;

        public TextMeshProUGUI CoinsText;
        [HideInInspector]
        public int CurrentCoins;

        private void Awake()
        {
            ins = this;
        }
        private void Start()
        {
            CurrentCoins = PlayerPrefs.GetInt("coins", 0);
            CoinsText.text = CurrentCoins.ToString();


            if (PlayerPrefs.GetInt("subsc") == 1)
            {
                VipActive.SetActive(true);
                BuyVip.SetActive(false);
            }
            else
            {
                VipActive.SetActive(false);
                BuyVip.SetActive(true);
            }
        }
        public void Reload()
        {
            SceneManager.LoadScene(0);
        }
        public void BuyMoreCoins()
        {
            int Getcoins = PlayerPrefs.GetInt("coins", 0);
            CurrentCoins = Getcoins + 5000;
            CoinsText.text = CurrentCoins.ToString();
            PlayerPrefs.SetInt("coins", CurrentCoins);
        }
        public void BuyRemoveAdsFromGame()
        {
            if (PlayerPrefs.GetInt("RemoveAd") == 0)
            {
                RemoveAdsObject.SetActive(true);
                PlayerPrefs.SetInt("RemoveAd", 1);
                PlayerPrefs.SetInt("DisplayAds", 0);
            }
        }
        public void RestartAdsFromGame()
        {
            if(PlayerPrefs.GetInt("DisplayAds") == 0)
            {
                RestartAdsObject.SetActive(true);
                PlayerPrefs.SetInt("DisplayAds", 1);
            }
          
        }
        public void ActivateWeeklySubscription()
        {
            if (PlayerPrefs.GetInt("subsc") == 0)
            {
                VipActive.SetActive(true);
                BuyVip.SetActive(false);

                SubscriptionObject.SetActive(true);
                PlayerPrefs.SetInt("subsc", 1);
            }
        }
        public void DeactivateWeeklySubscription()
        {
            if (PlayerPrefs.GetInt("subsc") == 1)
            {
                VipActive.SetActive(false);
                BuyVip.SetActive(true);

                NoSubscriptionObject.SetActive(true);
                PlayerPrefs.SetInt("subsc", 0);
            }

        }
        public void ShowReward()
        {
            RewardPanel.SetActive(true);
            int Getcoins = PlayerPrefs.GetInt("coins", 0);
            CurrentCoins = Getcoins + 5000;
            CoinsText.text = CurrentCoins.ToString();
            PlayerPrefs.SetInt("coins", CurrentCoins);
        }
        public void CloseRewardPanel()
        {
            RewardPanel.SetActive(false);
        }
        public void ClosePanel(GameObject Obj)
        {
            Obj.SetActive(false);
        }
        public void SetAndBuyMoreCoins(int coins,GameObject Reward)
        {
            int Getcoins = PlayerPrefs.GetInt("coins", 0);
            CurrentCoins = Getcoins + coins;
            CoinsText.text = CurrentCoins.ToString();
            PlayerPrefs.SetInt("coins", CurrentCoins);
            Reward.SetActive(true);
        }
    }
}
