using System;
using UnityEngine;

namespace Elroi.Tutorials
{
    [AddComponentMenu("ELROI/Tutorial Target Marker")]
    [DisallowMultipleComponent]
    public sealed class TutorialTargetMarker : MonoBehaviour
    {
        [SerializeField] private string targetId;

        public string TargetId
        {
            get => targetId;
            set
            {
                string normalized = value == null ? string.Empty : value.Trim();
                if (string.Equals(targetId, normalized, StringComparison.Ordinal)) return;
                TutorialTargetRegistry.Unregister(this);
                targetId = normalized;
                if (isActiveAndEnabled) TutorialTargetRegistry.Register(this);
            }
        }

        private void OnEnable() => TutorialTargetRegistry.Register(this);
        private void OnDisable() => TutorialTargetRegistry.Unregister(this);

        private void OnValidate()
        {
            targetId = targetId == null ? string.Empty : targetId.Trim();
            TutorialTargetRegistry.Unregister(this);
            if (isActiveAndEnabled) TutorialTargetRegistry.Register(this);
        }
    }
}
