using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Elroi.DailyMissions.UI
{
    [Serializable]
    public sealed class FreeCashDailyState
    {
        public string utcDay;
        public bool[] claimed = new bool[5];
        public int unlockedStep;
    }

    public sealed class FreeCashDailyPopup : MonoBehaviour
    {
        public const string SaveKey = "FreeCashDaily.StateV1";

        static readonly int[] Cash = { 100, 500, 1000, 2000, 4000 };
        static readonly int[] Gems = { 0, 0, 0, 3, 5 };
        static readonly int OpenTrigger = Animator.StringToHash("Open");
        static readonly int CloseTrigger = Animator.StringToHash("Close");
        static readonly int ClosedParameter = Animator.StringToHash("Closed");

        [SerializeField] FreeCashRewardRowUI[] rows;
        [SerializeField] TMP_Text resetTimerText;
        [SerializeField] Button closeButton;
        [SerializeField] HomeUIBinder homeUIBinder;
        [SerializeField] Sprite cashIcon;
        [SerializeField] Sprite gemIcon;

        [Header("No Ads Style Dialog Animation")]
        [SerializeField] Animator dialogAnimator;
        [SerializeField, Min(0.01f)] float closeAnimationDuration = 0.34f;

        FreeCashDailyState state;
        bool processing;
        float nextTick;
        Coroutine closeRoutine;

        void Awake()
        {
            if (closeButton)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                if (rows[i]) rows[i].SetHandler(() => Claim(index));
            }
        }

        void OnEnable()
        {
            processing = false;
            EnsureDay();
            Refresh();
        }

        void OnDisable()
        {
            processing = false;
            closeRoutine = null;
        }

        void Update()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 1f;
            if (EnsureDay()) CrystalRewardRevealPopup.CloseActiveImmediate();
            Refresh();
        }

        public void Open()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (!dialogAnimator) return;

            dialogAnimator.ResetTrigger(CloseTrigger);
            dialogAnimator.SetBool(ClosedParameter, false);
            dialogAnimator.SetTrigger(OpenTrigger);
        }

        public void Close()
        {
            processing = false;
            if (!gameObject.activeSelf) return;

            if (!dialogAnimator)
            {
                gameObject.SetActive(false);
                return;
            }

            dialogAnimator.ResetTrigger(OpenTrigger);
            dialogAnimator.SetBool(ClosedParameter, true);
            dialogAnimator.SetTrigger(CloseTrigger);
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(DisableAfterClose());
        }

        IEnumerator DisableAfterClose()
        {
            yield return new WaitForSecondsRealtime(closeAnimationDuration);
            closeRoutine = null;
            gameObject.SetActive(false);
        }

        void Claim(int index)
        {
            if (processing || index < 0 || index >= 5) return;
            EnsureDay();
            if (state.claimed[index] || index != state.unlockedStep) return;

            processing = true;
            Refresh();
            if (index == 0)
            {
                Complete(index);
                return;
            }

            if (!RewardedAdBridge.ShowRewardedAd(
                    "free_cash_step_" + (index + 1),
                    () => Complete(index),
                    () =>
                    {
                        processing = false;
                        Refresh();
                    }))
            {
                processing = false;
                Refresh();
            }
        }

        void Complete(int index)
        {
            EnsureDay();
            if (!processing || state.claimed[index] || index != state.unlockedStep)
            {
                processing = false;
                Refresh();
                return;
            }

            CurrencyChangeResult result;
            if (!CurrencyRewardService.TryGrantCurrency(
                    Cash[index], Gems[index], CurrencyGrantSource.FreeCash, true, out result))
            {
                processing = false;
                Refresh();
                return;
            }

            state.claimed[index] = true;
            state.unlockedStep = Mathf.Min(5, index + 1);
            ES3.Save(SaveKey, state);
            if (homeUIBinder)
            {
                homeUIBinder.AnimateBalances(
                    result.previousCash,
                    result.newCash,
                    result.previousGems,
                    result.newGems);
            }

            CrystalRewardRevealPopup.TryShow(RewardRevealRequest.ForCurrencies(
                Cash[index], Gems[index], cashIcon, gemIcon));
            processing = false;
            Refresh();
        }

        bool EnsureDay()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (state == null) state = ES3.Load(SaveKey, new FreeCashDailyState());
            if (state.claimed != null && state.claimed.Length == 5 && state.utcDay == today)
                return false;

            state = new FreeCashDailyState
            {
                utcDay = today,
                claimed = new bool[5],
                unlockedStep = 0
            };
            ES3.Save(SaveKey, state);
            processing = false;
            return true;
        }

        void Refresh()
        {
            if (state == null) return;
            for (int i = 0; i < rows.Length && i < 5; i++)
            {
                if (rows[i])
                {
                    rows[i].Bind(
                        Cash[i], Gems[i], i > 0, state.claimed[i],
                        i == state.unlockedStep, processing);
                }
            }

            TimeSpan remaining = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            if (resetTimerText)
            {
                resetTimerText.text = string.Format(
                    "RESETS IN {0:00}:{1:00}:{2:00}",
                    (int)remaining.TotalHours,
                    remaining.Minutes,
                    remaining.Seconds);
            }
        }
    }
}
