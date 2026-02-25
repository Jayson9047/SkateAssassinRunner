using UnityEngine;
using UnityEngine.VFX;

namespace AfterimageFX
{
    public class VFXEffectTest : MonoBehaviour, IAfterimageEffect
    {
        public VisualEffect vfxInstance;

        [Tooltip("Exposed bool property name in the VFX Graph (exact match).")]
        public string trailBoolParameter = "CanDrawTrail";

        private void Awake()
        {
            AutoWireIfMissing();

            if (!IsReady()) return;

            vfxInstance.Reinit();

            // Only set if it exists
            if (vfxInstance.HasBool(trailBoolParameter))
                vfxInstance.SetBool(trailBoolParameter, false);
            else
                Debug.LogError($"[VFXEffectTest] Bool parameter '{trailBoolParameter}' NOT found on VFX asset '{vfxInstance.visualEffectAsset.name}'. " +
                               $"Fix: set trailBoolParameter to the exact exposed property name in the graph.", this);
        }

        public void InitializeAfterimage(Mesh snapshotMesh, float lifetime)
        {
            StartTrail();
        }

        public void StartTrail()
        {
            AutoWireIfMissing();
            if (!IsReady()) return;

            if (vfxInstance.HasBool(trailBoolParameter))
            {
                vfxInstance.SetBool(trailBoolParameter, true);
                vfxInstance.Play();
            }
            else
            {
                Debug.LogError($"[VFXEffectTest] StartTrail failed: Bool '{trailBoolParameter}' not found on asset '{vfxInstance.visualEffectAsset.name}'.", this);
                // Fallback: at least try playing the graph
                vfxInstance.Play();
            }
        }

        public void StopTrail()
        {
            AutoWireIfMissing();
            if (!IsReady()) return;

            if (vfxInstance.HasBool(trailBoolParameter))
                vfxInstance.SetBool(trailBoolParameter, false);
            else
                vfxInstance.Stop(); // fallback
        }

        private void AutoWireIfMissing()
        {
            if (vfxInstance == null)
                vfxInstance = GetComponentInChildren<VisualEffect>(true);
        }

        private bool IsReady()
        {
            if (vfxInstance == null)
            {
                Debug.LogError("[VFXEffectTest] No VisualEffect found/assigned.", this);
                return false;
            }

            if (vfxInstance.visualEffectAsset == null)
            {
                Debug.LogError("[VFXEffectTest] VisualEffectAsset is NULL on the VisualEffect component. Assign the .vfx graph.", this);
                return false;
            }

            return true;
        }
    }
}