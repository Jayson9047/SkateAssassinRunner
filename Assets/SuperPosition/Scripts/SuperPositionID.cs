#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace SuperPosition
{
    [ExecuteInEditMode]
    public class SuperPositionID : MonoBehaviour
    {
        public long ID = 0;

        [SerializeField] public int instanceID = 0;

        //#if UNITY_EDITOR
        public void Awake()
        {
            if (Application.isPlaying) return;

            if (instanceID != GetInstanceID())
            {
                Scene scene = SceneManager.GetActiveScene();

                if (instanceID == 0 || !scene.isLoaded)
                {
                    instanceID = GetInstanceID();
                }
                else
                {
                    DestroyImmediate(this);
                }
            }
        }
        //#endif

    }
}
#endif