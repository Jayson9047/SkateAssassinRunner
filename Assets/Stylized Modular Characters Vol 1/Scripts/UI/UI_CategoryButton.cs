using Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UI_CategoryButton : MonoBehaviour
    {
        public CharacterCustomizationCategory category;

        public TextMeshProUGUI categoryName;

        public Button selectButton;

        public void Initialize(CharacterCustomizationCategory _category)
        {
            this.category = _category;
            categoryName.text = _category.name;
            selectButton.onClick.AddListener(SelectCategory);
        }

        public void SelectCategory()
        {
            UI_OptionsPanel.Instance.Initialize(category);
            UI_CusomizationManager.Instance.SelectCategory(category);
        }
    }
}
