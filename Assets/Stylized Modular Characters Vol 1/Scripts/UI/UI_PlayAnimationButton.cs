using UnityEngine;

namespace UI
{
    public class UI_PlayAnimationButton : MonoBehaviour
    {
        public void PlayAnimation()
        {
            UI_CusomizationManager.Instance.customizationManager.PlayAnimation();
        }
    }
}