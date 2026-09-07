using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Settings language picker. Native language names are intentionally not localized.</summary>
public sealed class LocalizationLanguageMenu : MonoBehaviour
{
    [Serializable]
    public sealed class Option
    {
        public string localeCode;
        public Button button;
        public TMP_Text label;
        public GameObject selectedVisual;
    }

    [SerializeField] private TMP_Text currentLanguageLabel;
    [SerializeField] private Option[] options = Array.Empty<Option>();
    private bool bound;
    private UnityAction[] callbacks = Array.Empty<UnityAction>();

    private void OnEnable()
    {
        Bind();
        SkateLocalization.LocaleChanged += OnLocaleChanged;
        Refresh();
    }

    private void OnDisable()
    {
        SkateLocalization.LocaleChanged -= OnLocaleChanged;
        Unbind();
    }

    public void Configure(TMP_Text currentLabel, Option[] languageOptions)
    {
        currentLanguageLabel = currentLabel;
        options = languageOptions ?? Array.Empty<Option>();
    }

    public void Refresh()
    {
        string selectedCode = SkateLocalization.NormalizeLocaleCode(SkateLocalization.CurrentLocaleCode);
        if (currentLanguageLabel != null)
        {
            currentLanguageLabel.text = SkateLocalization.GetNativeLanguageName(selectedCode);
        }

        foreach (Option option in options)
        {
            if (option == null) continue;
            if (option.label != null) option.label.text = SkateLocalization.GetNativeLanguageName(option.localeCode);
            if (option.selectedVisual != null)
            {
                option.selectedVisual.SetActive(string.Equals(
                    SkateLocalization.NormalizeLocaleCode(option.localeCode), selectedCode, StringComparison.Ordinal));
            }
        }
    }

    private void Bind()
    {
        if (bound) return;
        callbacks = new UnityAction[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            Option option = options[i];
            if (option?.button == null) continue;
            string code = option.localeCode;
            callbacks[i] = () => SkateLocalization.SelectLocale(code);
            option.button.onClick.AddListener(callbacks[i]);
        }
        bound = true;
    }

    private void Unbind()
    {
        if (!bound) return;
        // These rows are owned exclusively by this component; clearing avoids
        // closure/listener duplication when the inactive page is reopened.
        for (int i = 0; i < options.Length; i++)
        {
            Option option = options[i];
            if (option?.button != null && i < callbacks.Length && callbacks[i] != null)
            {
                option.button.onClick.RemoveListener(callbacks[i]);
            }
        }
        callbacks = Array.Empty<UnityAction>();
        bound = false;
    }

    private void OnLocaleChanged(Locale locale)
    {
        Refresh();
    }
}
