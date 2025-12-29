using UnityEngine;

namespace IndieKit
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField]
        private Transform cameraRig;

        [SerializeField]
        public float cameraRotationSpeed = 20f;

        [Header("Damage Settings")]
        [SerializeField]
        private float damageAmount = 10f;

        private void Update()
        {
            if (cameraRig != null)
            {
                cameraRig.eulerAngles = new Vector3(
                    cameraRig.eulerAngles.x,
                    cameraRig.eulerAngles.y - Time.deltaTime * cameraRotationSpeed,
                    cameraRig.eulerAngles.z
                );
            }

            HandleFireInput();
            HandleQuitInput();
        }

        private void HandleFireInput()
        {
            if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                Vector2 mousePos = Input.mousePosition;
                Ray ray = Camera.main.ScreenPointToRay(mousePos);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                    damageable?.ApplyDamage(damageAmount, hit.point);
                }
            }
        }

        private void HandleQuitInput()
        {
#if UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
#endif
        }
    }
}
