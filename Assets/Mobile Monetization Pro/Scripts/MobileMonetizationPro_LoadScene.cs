using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_LoadScene : MonoBehaviour
    {
        public void LoadTheLevel(int LevelNumber)
        {
            SceneManager.LoadScene(LevelNumber);
        }
    }
}