using UnityEngine;
using System;
using System.Reflection;

namespace MobileMonetizationPro
{
    public class InternetConnectivityCheck : MonoBehaviour
    {
        [Tooltip("Time scale when the internet connection is active (1 = normal speed).")]
        public float TimeScaleWhenInternet = 1f;

        [Tooltip("Time scale when there is no internet connection (0 = paused).")]
        public float TimeScaleWhenNoInternet = 0f;

        [Tooltip("The GameObject to display when there is no internet connection.")]
        public GameObject NoInternetConnectionGameObject;

        [Tooltip("The MonoBehaviour script that contains the method to invoke when internet is restored.")]
        public MonoBehaviour scriptWithFunction;

        [Tooltip("The name of the method to invoke from the scriptWithFunction when internet is restored.")]
        public string methodName;

        private bool previousConnectionStatus = true;

        private void Update()
        {
            CheckInternetConnection();
        }

        private void CheckInternetConnection()
        {
            bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;

            if (isConnected != previousConnectionStatus)
            {
                Time.timeScale = isConnected ? TimeScaleWhenInternet : TimeScaleWhenNoInternet;
                NoInternetConnectionGameObject.SetActive(!isConnected);

                if (isConnected && scriptWithFunction != null && !string.IsNullOrEmpty(methodName))
                {
                    Debug.Log("Connected To Internet");
                    MethodInfo method = scriptWithFunction.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null && method.GetParameters().Length == 0)
                    {
                        method.Invoke(scriptWithFunction, null);
                    }
                }

                previousConnectionStatus = isConnected;
            }
        }
    }
}