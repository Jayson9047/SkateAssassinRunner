using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace Modern_2D_Animated_Chests_Pack.Demo
{
    public class DemoManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] chestParentGameObjects;
        private int _chestParentIndex;
        private static readonly int NextKey = Animator.StringToHash("Next");

        private void Awake()
        {
            SetChestParent();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame)
                {
                    PreviousChestParent();
                }
                else if (Keyboard.current.dKey.wasPressedThisFrame)
                {
                    NextChestParent();
                }
                else if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    SetAnimation();
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.A))
            {
                PreviousChestParent();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                NextChestParent();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                SetAnimation();
            }
#endif
        }

        private void SetChestParent()
        {
            for (int i = 0; i < chestParentGameObjects.Length; i++)
            {
                chestParentGameObjects[i].SetActive(i==_chestParentIndex);
            }
        }

        private void SetAnimation()
        {
            Animator[] animators = chestParentGameObjects[_chestParentIndex].GetComponentsInChildren<Animator>();
            foreach (var t in animators)
            {
                t.SetTrigger(NextKey);
            }
        }

        private void NextChestParent()
        {
            _chestParentIndex++;
            if (_chestParentIndex >= chestParentGameObjects.Length)
            {
                _chestParentIndex = 0;
            }

            SetChestParent();
        }

        private void PreviousChestParent()
        {
            _chestParentIndex--;
            if (_chestParentIndex < 0)
            {
                _chestParentIndex = chestParentGameObjects.Length - 1;
            }

            SetChestParent();
        }
    }
}