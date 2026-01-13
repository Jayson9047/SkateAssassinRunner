using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

public class ReviveButtonAction : MonoBehaviour
{
    public void Revive()
    {
        // Safety checks
        if (GameManager.Instance == null || LevelManager.Instance == null)
            return;

        // 1) Give the player ONE life
        GameManager.Instance.SetLives(1);

        // 2) Hide Game Over UI
        if (GUIManager.Instance != null)
        {
            GUIManager.Instance.SetGameOverScreen(false);
        }

        // 3) Put game back into LifeLost state
        GameManager.Instance.SetStatus(GameManager.GameStatus.LifeLost);

        // 4) Continue like a normal life-lost recovery
        LevelManager.Instance.LifeLostAction();
    }
}
