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

        // ELROI extension: allow verified connectivity and project-owned pause state.
        public bool ManageTimeScale = true;
        public Func<bool> ConnectionEvaluator { get; set; }
        public event Action<bool> ConnectionStatusChanged;
        public bool HasCheckedConnection { get; private set; }
        public bool IsConnected { get; private set; }

        private bool previousConnectionStatus = true;

        public void CheckNow()
        {
            CheckInternetConnection();
        }

        private void Update()
        {
            CheckInternetConnection();
        }

        private void CheckInternetConnection()
        {
            bool isConnected = ConnectionEvaluator != null
                ? ConnectionEvaluator()
                : Application.internetReachability != NetworkReachability.NotReachable;
            bool wasChecked = HasCheckedConnection;
            bool restored = wasChecked && !previousConnectionStatus && isConnected;

            if (!wasChecked || isConnected != previousConnectionStatus)
            {
                if (ManageTimeScale)
                    Time.timeScale = isConnected ? TimeScaleWhenInternet : TimeScaleWhenNoInternet;
                if (NoInternetConnectionGameObject != null)
                    NoInternetConnectionGameObject.SetActive(!isConnected);

                previousConnectionStatus = isConnected;
                HasCheckedConnection = true;
                IsConnected = isConnected;
                ConnectionStatusChanged?.Invoke(isConnected);

                if (restored && scriptWithFunction != null && !string.IsNullOrEmpty(methodName))
                {
                    MethodInfo method = scriptWithFunction.GetType().GetMethod(methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null && method.GetParameters().Length == 0)
                        method.Invoke(scriptWithFunction, null);
                }
            }
        }
    }
}