using UnityEngine;
using UnityEngine.UI;

namespace Elroi.Tutorials
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialTriangleGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Color32 tint = color;
            vertexHelper.AddVert(new Vector3(rect.center.x, rect.yMin), tint, new Vector2(0.5f, 0f));
            vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMax), tint, new Vector2(0f, 1f));
            vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMax), tint, new Vector2(1f, 1f));
            vertexHelper.AddTriangle(0, 1, 2);
        }
    }
}
