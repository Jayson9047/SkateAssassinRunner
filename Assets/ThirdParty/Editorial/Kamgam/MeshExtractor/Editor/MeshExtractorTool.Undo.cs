using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kamgam.MeshExtractor
{
    partial class MeshExtractorTool
    {
        public enum UndoStateType { Selection, ExtractAndReplaceInScene, NativeUndo }

        public class UndoState
        {
            public UndoStateType Type;

            // Selection
            public HashSet<SelectedTriangle> SelectionData;

            // Extraction
            public List<Component> ExtractComponents;
            // Used for restoring to original mesh
            public List<Mesh> ExtractMeshReferences;
            // Use for restoring generated mesh assets that have been replaced.
            public List<Mesh> ExtractMeshCopies;
            public List<string> ExtractMeshAssetPaths;
            public List<Material[]> ExtractMaterials;

            public string NativeUndoName;

            public UndoState(string nativeUndoName)
            {
                NativeUndoName = nativeUndoName;
            }

            public UndoState(HashSet<SelectedTriangle> selectionData)
            {
                Type = UndoStateType.Selection;
                SelectionData = new HashSet<SelectedTriangle>();
                foreach (var tri in selectionData)
                {
                    var copy = tri.Copy();
                    SelectionData.Add(copy);
                }
            }

            public UndoState(List<Component> components, List<Mesh> meshes, List<string> meshAssetPaths, List<Material[]> materials)
            {
                Type = UndoStateType.ExtractAndReplaceInScene;
                ExtractComponents = components;
                ExtractMeshReferences = meshes;
                ExtractMeshCopies = new List<Mesh>(meshes.Count);
                foreach (var mesh in meshes)
                {
                    ExtractMeshCopies.Add(Mesh.Instantiate(mesh));
                }
                ExtractMeshAssetPaths = meshAssetPaths;

                ExtractMaterials = new List<Material[]>();
                foreach (var materialsList in materials)
                {
                    var mats = new Material[materialsList.Length];
                    materialsList.CopyTo(mats, 0);
                    ExtractMaterials.Add(mats);
                }
                
            }
        }

        protected UndoStack<UndoState> _undoStack;

        protected void initUndoStack()
        {
            _undoStack = new UndoStack<UndoState>(copyUndoStep, applyUndoStep);
        }

        protected UndoState copyUndoStep(UndoState state)
        {
            // Nothing to do since the UndoStep usually already is a completely
            // new deep-copied object and need not be copied again.
            return state;
        }

        protected void applyUndoStep(UndoState state, bool undo)
        {
            if (state.Type == UndoStateType.Selection)
            {
                if (state.SelectionData == null)
                    _selectedTriangles.Clear();
                else
                {
                    _selectedTriangles = new HashSet<SelectedTriangle>();
                    foreach (var tri in state.SelectionData)
                    {
                        _selectedTriangles.Add(tri.Copy());
                    }
                }
            }
            else if (state.Type == UndoStateType.ExtractAndReplaceInScene)
            {
                // Sadly forwarding undo / redo to the default undo system did not work.
                // Thus we now do it manually.

                var settings = MeshExtractorSettings.GetOrCreateSettings();

                for (int i = 0; i < state.ExtractComponents.Count; i++)
                {
                    var path = state.ExtractMeshAssetPaths[i];
                    if (!string.IsNullOrEmpty(path))
                    {
                        bool assetWasGenerated = path.Contains(settings.ExtractedFilesLocation);
                        // TODO: Record the extracted file locations in SessionStorage and then
                        //       use that to determine whether or not to extract. Reason: with settings.RelativeExtractedFilesLocation
                        //       it is now possible that extracted files are NOT in the settings.ExtractedFilesLocation.

                        var comp = state.ExtractComponents[i];
                        Mesh mesh;
                        if (assetWasGenerated)
                        {
                            mesh = Mesh.Instantiate(state.ExtractMeshCopies[i]);
                        }
                        else
                        {
                            mesh = state.ExtractMeshReferences[i];
                        }
                        var materials = state.ExtractMaterials[i];
                        var skinnedRenderer = comp as SkinnedMeshRenderer;
                        if (skinnedRenderer != null)
                        {
                            skinnedRenderer.sharedMesh = mesh;
                            skinnedRenderer.sharedMaterials = materials;
                        }
                        else
                        {
                            var meshFilter = comp as MeshFilter;
                            if (meshFilter != null)
                            {
                                meshFilter.sharedMesh = mesh;
                                var renderer = meshFilter.transform.GetComponent<MeshRenderer>();
                                if (renderer != null)
                                {
                                    renderer.sharedMaterials = materials;
                                }
                            }
                        }

                        if (assetWasGenerated)
                        {
                            var existingAsset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                            if (existingAsset != null)
                            {
                                AssetDatabase.DeleteAsset(path);
                            }
                            AssetDatabase.CreateAsset(mesh, path);
                        }
                    }
                }
            }
            else if (state.Type == UndoStateType.NativeUndo)
            {
                // Forward to default undo (that did not work as planned).
                if (undo)
                {
                    Undo.PerformUndo();
                }
                else
                {
                    Undo.PerformRedo();
                }

                // Make sure whatever the native undo did it does not interfer with the tool (i.e. reset everything).
                TriangleCache.Clear();
                _selectedObjects = new GameObject[] { };
                resetSelect();
                resetExtract();
                resetToPickObjectModeIfNecessary();
                GUIUtility.ExitGUI();
            }

            TriangleCache.Clear();
            TriangleCache.RebuildBakedMeshesCache(_selectedObjects);
            TriangleCache.CacheTriangles();
        }
    }
}
