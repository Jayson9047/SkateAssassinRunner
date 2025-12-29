using System;
using UnityEngine;

namespace Kamgam.MeshExtractor
{
    /// <summary>
    /// A serializable representation of some data in a selected triangle.
    /// </summary>
    [System.Serializable]
    public class SelectedTriangleData
    {
        /// <summary>
        /// 0 = MeshFilter,
        /// 1 = SkinnedMeshRenderer
        /// </summary>
        public int ComponentType;
        public int SubMeshIndex;
        public Vector3Int TriangleIndices;
        public Vector3 VertexLocal0;
        public Vector3 VertexLocal1;
        public Vector3 VertexLocal2;

        public SelectedTriangleData(SelectedTriangle tri)
        {
            SubMeshIndex = tri.SubMeshIndex;
            TriangleIndices = tri.TriangleIndices;
            ComponentType = tri.Component is SkinnedMeshRenderer ? 1 : 0;
            VertexLocal0 = tri.VertexLocal0;
            VertexLocal1 = tri.VertexLocal1;
            VertexLocal2 = tri.VertexLocal2;
        }
    }
}
