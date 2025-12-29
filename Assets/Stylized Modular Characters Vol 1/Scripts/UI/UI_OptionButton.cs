using Character;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UI_OptionButton : MonoBehaviour
    {
        public CharacterCustomizationOption option;

        public Image icon;
        public Button selectButton;

        public void Initialize(CharacterCustomizationOption option)
        {
            this.option = option;
            icon.sprite = option.icon;
            selectButton.onClick.AddListener(SelectOption);
        }

        public void SelectOption()
        {
            UI_CusomizationManager.Instance.SelectOption(option.id);
        }
    }
}
