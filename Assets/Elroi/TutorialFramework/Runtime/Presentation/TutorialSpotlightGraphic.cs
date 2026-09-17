using UnityEngine;
using UnityEngine.UI;

namespace Elroi.Tutorials
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialSpotlightGraphic : MaskableGraphic
    {
        private static readonly int HoleRectId = Shader.PropertyToID("_HoleRect");
        private static readonly int ShapeId = Shader.PropertyToID("_Shape");
        private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
        private static readonly int FeatherId = Shader.PropertyToID("_Feather");
        private Material runtimeMaterial;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            EnsureMaterial();
        }

        protected override void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeMaterial); else DestroyImmediate(runtimeMaterial);
            }
            base.OnDestroy();
        }

        public void Configure(Rect screenRect, TutorialSpotlightShape shape, float cornerRadiusPixels, float featherPixels, float opacity)
        {
            EnsureMaterial();
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            Vector4 normalized = new Vector4(screenRect.xMin / width, screenRect.yMin / height, screenRect.xMax / width, screenRect.yMax / height);
            runtimeMaterial.SetVector(HoleRectId, normalized);
            runtimeMaterial.SetFloat(ShapeId, (float)shape);
            runtimeMaterial.SetFloat(CornerRadiusId, cornerRadiusPixels / Mathf.Min(width, height));
            runtimeMaterial.SetFloat(FeatherId, Mathf.Max(0.00001f, featherPixels / Mathf.Min(width, height)));
            color = new Color(0f, 0f, 0f, Mathf.Clamp01(opacity));
            SetMaterialDirty();
        }

        private void EnsureMaterial()
        {
            if (runtimeMaterial != null) return;
            Shader shader = Shader.Find("UI/ELROI Tutorial Spotlight");
            if (shader == null) return;
            runtimeMaterial = new Material(shader) { name = "ELROI Tutorial Spotlight (Runtime)" };
            material = runtimeMaterial;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Color32 tint = color;
            vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMin), tint, new Vector2(0f, 0f));
            vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMax), tint, new Vector2(0f, 1f));
            vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMax), tint, new Vector2(1f, 1f));
            vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMin), tint, new Vector2(1f, 0f));
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }
    }

}
