using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Tutorials
{
    public static class TutorialTargetRegistry
    {
        private static readonly Dictionary<string, List<TutorialTargetMarker>> Targets = new Dictionary<string, List<TutorialTargetMarker>>(StringComparer.Ordinal);

        public static void Register(TutorialTargetMarker marker)
        {
            if (marker == null || string.IsNullOrWhiteSpace(marker.TargetId)) return;
            if (!Targets.TryGetValue(marker.TargetId, out List<TutorialTargetMarker> list))
            {
                list = new List<TutorialTargetMarker>();
                Targets.Add(marker.TargetId, list);
            }
            list.RemoveAll(item => item == null);
            if (!list.Contains(marker)) list.Add(marker);
            list.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            if (list.Count > 1)
                Debug.LogWarning($"[ELROI Tutorials] Duplicate active runtime target ID '{marker.TargetId}'. The lowest Unity instance ID is selected deterministically.", marker);
        }

        public static void Unregister(TutorialTargetMarker marker)
        {
            if (marker == null) return;

            List<string> emptyIds = null;
            foreach (KeyValuePair<string, List<TutorialTargetMarker>> pair in Targets)
            {
                pair.Value.Remove(marker);
                pair.Value.RemoveAll(item => item == null);
                if (pair.Value.Count != 0) continue;
                if (emptyIds == null) emptyIds = new List<string>();
                emptyIds.Add(pair.Key);
            }

            if (emptyIds == null) return;
            foreach (string id in emptyIds) Targets.Remove(id);
        }

        public static bool TryResolve(string targetId, out GameObject target)
        {
            target = null;
            if (string.IsNullOrWhiteSpace(targetId) || !Targets.TryGetValue(targetId, out List<TutorialTargetMarker> list)) return false;
            list.RemoveAll(item => item == null || !item.isActiveAndEnabled);
            if (list.Count == 0)
            {
                Targets.Remove(targetId);
                return false;
            }
            list.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            target = list[0].gameObject;
            return true;
        }

        public static void ClearForTests() => Targets.Clear();
    }
}
