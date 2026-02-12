using UnityEngine;

public class ScenarioChunkReset : MonoBehaviour
{
    private void OnEnable()
    {
        // Ensure children are re-enabled when chunk is reused
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (!child.gameObject.activeSelf)
                child.gameObject.SetActive(true);
        }
    }
}
