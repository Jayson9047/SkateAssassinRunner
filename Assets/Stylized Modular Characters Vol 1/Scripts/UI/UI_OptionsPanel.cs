using Character;
using UnityEngine;

namespace UI
{
    public class UI_OptionsPanel : MonoBehaviour
    {
        public static UI_OptionsPanel Instance;
    
        private void Awake()
        {
            Instance = this;
        }

        public GameObject optionButtonPrefab;

        public Transform optionButtonContainer;

        public void Initialize(CharacterCustomizationCategory category)
        {
            foreach (Transform child in optionButtonContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (CharacterCustomizationOption option in category.options)
            {
                GameObject optionButton = Instantiate(optionButtonPrefab, optionButtonContainer);
                optionButton.GetComponentInChildren<UI_OptionButton>().Initialize(option);
            }
        }
    }
}
