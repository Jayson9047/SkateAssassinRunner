using UnityEngine;

namespace Elroi.Tutorials
{
    public static class TutorialScreenUtility
    {
        public static bool TryGetScreenRect(GameObject target, TutorialTargetType targetType, Camera camera, Vector2 fallbackSize, out Rect screenRect, out bool usedFallback)
        {
            usedFallback = false;
            screenRect = default;
            if (target == null) return false;
            if (targetType == TutorialTargetType.TwoDimensional)
            {
                RectTransform rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform == null) return false;
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                Camera uiCamera = ResolveCanvasCamera(rectTransform);
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
                screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                return true;
            }

            Camera resolvedCamera = camera != null ? camera : Camera.main;
            if (resolvedCamera == null) return false;
            if (TryGetWorldBounds(target, out Bounds bounds))
                return ProjectBounds(resolvedCamera, bounds, out screenRect);

            Vector3 point3 = resolvedCamera.WorldToScreenPoint(target.transform.position);
            if (point3.z <= 0f) return false;
            usedFallback = true;
            screenRect = new Rect(point3.x - fallbackSize.x * 0.5f, point3.y - fallbackSize.y * 0.5f, fallbackSize.x, fallbackSize.y);
            return true;
        }

        public static bool IsActuallyVisible(GameObject target, TutorialTargetType targetType, Camera camera)
        {
            if (targetType == TutorialTargetType.ThreeDimensional)
            {
                Camera resolvedCamera = camera != null ? camera : Camera.main;
                if (resolvedCamera == null || target == null) return false;
                if (TryGetWorldBounds(target, out Bounds bounds)) return BoundsIntersectViewport(resolvedCamera, bounds);
                Vector3 viewportPoint = resolvedCamera.WorldToViewportPoint(target.transform.position);
                return viewportPoint.z > 0f && viewportPoint.x >= 0f && viewportPoint.x <= 1f && viewportPoint.y >= 0f && viewportPoint.y <= 1f;
            }
            if (!TryGetScreenRect(target, targetType, camera, new Vector2(32f, 32f), out Rect rect, out _)) return false;
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            return rect.Overlaps(screen, true);
        }

        private static bool BoundsIntersectViewport(Camera camera, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            bool anyInFront = false;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 world = center + Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 viewport = camera.WorldToViewportPoint(world);
                if (viewport.z <= 0f) continue;
                anyInFront = true;
                min = Vector2.Min(min, viewport);
                max = Vector2.Max(max, viewport);
            }
            return anyInFront && Rect.MinMaxRect(min.x, min.y, max.x, max.y).Overlaps(new Rect(0f, 0f, 1f, 1f), true);
        }

        private static Camera ResolveCanvasCamera(RectTransform target)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera;
        }

        private static bool TryGetWorldBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            bool found = false;
            bounds = new Bounds(target.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
            }
            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled) continue;
                if (!found) { bounds = collider.bounds; found = true; } else bounds.Encapsulate(collider.bounds);
            }
            return found;
        }

        private static bool ProjectBounds(Camera camera, Bounds bounds, out Rect rect)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            bool anyInFront = false;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 world = center + Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f) continue;
                anyInFront = true;
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            rect = anyInFront ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default;
            return anyInFront;
        }
    }
}
