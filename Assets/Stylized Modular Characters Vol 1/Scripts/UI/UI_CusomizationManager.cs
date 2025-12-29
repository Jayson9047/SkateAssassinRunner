using Character;
using UnityEngine;

namespace UI
{
    public class UI_CusomizationManager : MonoBehaviour
    {
        public static UI_CusomizationManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        public GameObject categoryButtonPrefab;
        public Transform categoryButtonContainer;

        public CharacterCustomizationManager customizationManager;
        public CharacterCustomizationCategory selectedCategory;

        [ContextMenu("Initialize")]
        public void Initialize(CharacterCustomizationManager customizationManager)
        {
            this.customizationManager = customizationManager;
            foreach (CharacterCustomizationCategory category in customizationManager.categories)
            {
                GameObject categoryPrefab = Instantiate(categoryButtonPrefab, categoryButtonContainer);

                categoryPrefab.GetComponent<UI_CategoryButton>().Initialize(category);
            }
        }

        public void SelectCategory(CharacterCustomizationCategory category)
        {
            selectedCategory = category;
        }

        public void SelectOption(string optionID)
        {
            customizationManager.SelectOption(selectedCategory.id, optionID);
        }
    }
}