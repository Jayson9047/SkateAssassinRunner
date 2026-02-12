using UnityEngine;

public class ScenarioChunkResets : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
