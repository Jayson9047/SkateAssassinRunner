using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kamgam.MeshExtractor
{
    public static class AssetExporter
    {
#if UNITY_EDITOR
        public static void SaveMeshAsAsset(
            Mesh mesh, BoneData boneData, string assetPath, bool logFilePaths = true,
            bool recordUndo = true, System.Action<Mesh, string> createMeshUndo = null, System.Action<BoneData, string> createBoneDataUndo = null)
        {
            if (mesh == null)
                return;

            // Ensure the path starts with "Assets/".
            if (!assetPath.StartsWith("Assets"))
            {
                if (assetPath.StartsWith("/"))
                {
                    assetPath = "Assets" + assetPath;
                }
                else
                {
                    assetPath = "Assets/" + assetPath;
                }
            }

            string dirPath = System.IO.Path.GetDirectoryName(Application.dataPath + "/../" + assetPath);
            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }

            // Create or replace Mesh asset
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (recordUndo && existingMesh != null)
            {
                Undo.RegisterCompleteObjectUndo(existingMesh, "ME: Updating mesh");
            }
            AssetDatabase.CreateAsset(mesh, assetPath);
            if (recordUndo && existingMesh == null)
            {
                Undo.RegisterCreatedObjectUndo(mesh, "ME: Creating new mesh");
            }

            // Create or replace BoneData asset
            if (boneData != null)
            {
                var extension = System.IO.Path.GetExtension(assetPath); // extension with dot
                var path = assetPath.Substring(0, assetPath.Length - extension.Length);
                var boneDataAssetPath = path + "_BoneData" + extension;
                var existingBoneData = AssetDatabase.LoadAssetAtPath<BoneData>(boneDataAssetPath);
                if (recordUndo && existingBoneData != null)
                {
                    Undo.RegisterCompleteObjectUndo(existingBoneData, "ME: Updating bone data");
                }
                AssetDatabase.CreateAsset(boneData, boneDataAssetPath);
                if (recordUndo && existingBoneData == null)
                {
                    Undo.RegisterCreatedObjectUndo(boneData, "ME: Creating new bone data");
                }
            }

            try
            {
                AssetDatabase.SaveAssets();
            }
            catch(System.Exception e)
            {
                Logger.LogWarning("Save Assets triggered an error (not sure how to handle this, will ignore it for now): " + e.Message);
            }

            // Important to force the reimport to avoid the "SkinnedMeshRenderer: Mesh has
            // been changed to one which is not compatibile with the expected mesh data size
            // and vertex stride." error.
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            if (logFilePaths)
                Logger.LogMessage($"Saved new mesh under <color=yellow>'{assetPath}'</color>.");
        }
#endif

    }
}

