using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kamgam.MeshExtractor
{
    partial class MeshExtractorTool
    {
        public string getExtractedFilesLocation()
        {
            var defaultLocation = MeshExtractorSettings.GetOrCreateSettings().ExtractedFilesLocation;

            var settings = MeshExtractorSettings.GetOrCreateSettings();
            if (settings.RelativeExtractedFilesLocation)
            {
                var firstTri = getFirstSelectedTriangle();
                if (firstTri != null && firstTri.SharedMesh != null)
                {
                    var path = AssetDatabase.GetAssetPath(firstTri.SharedMesh);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var dir = System.IO.Path.GetDirectoryName(path);
                        dir = dir.Replace("Assets/", "");
                        dir = dir.Replace("Assets\\", "");
                        return dir + "/";
                    }
                }
            }

            return defaultLocation;
        }

        /// <summary>
        /// Split does an extraction of the selection.<br />
        /// Then it reverts the selection and does another extraction.<br />
        /// Finally (if replaceInScene is turned on) it will place all the extracted prefab ontop of each other.<br />
        /// NOTICE: The FIRST extracted mesh will replace the existing one.
        /// </summary>
        /// <param name="splitPartName"></param>
        /// <param name="replaceOldMesh"></param>
        /// <param name="preserveSubMeshes"></param>
        /// <param name="combineSubMeshesBasedOnMaterials"></param>
        /// <param name="combineMeshes"></param>
        /// <param name="saveAsObj"></param>
        /// <param name="extractTextures"></param>
        /// <param name="extractBoneWeights"></param>
        /// <param name="extractBoneTransforms"></param>
        /// <param name="extractBlendShapes"></param>
        /// <param name="replaceInScene"></param>
        public void Split(
            string splitPartName,
            bool replaceOldMesh, bool preserveSubMeshes, bool combineSubMeshesBasedOnMaterials,
            bool combineMeshes, bool saveAsObj, bool extractTextures, bool extractBoneWeights, bool extractBoneTransforms, bool extractBlendShapes,
            bool replaceInScene)
        {
            var settings = MeshExtractorSettings.GetOrCreateSettings();
            string splitPartPath = getExtractedFilesLocation() + splitPartName;

            EditorUtility.DisplayProgressBar("Extracting Mesh", "Gathering data for " + _selectedTriangles.Count + " triangles " + (_selectedTriangles.Count > 10000 ? "(this may take a while)" : "") + " ..", 0.1f);
            try
            {
                GameObject objectToSelectAfterSplit = null;

                var existingGuid = AssetDatabase.AssetPathToGUID("Assets/" + splitPartPath);
                if (!string.IsNullOrEmpty(existingGuid))
                {
                    bool avoidConflict = EditorUtility.DisplayDialog("Name Conflict",
                        "A asset with name '" + splitPartName + "' already exists under ('Assets/"+ splitPartPath + "').\n\n" +
                        "Do you really want to replace it?\n" +
                        "This would replace your existing object and can not be undone.",
                        "Cancel",
                        "Yes (I am doing this on purpose)");
                    if (avoidConflict)
                    {
                        Logger.LogMessage("Split action aborted. We suggest you choose a new name or disable the 'Replace' option.");
                        return;
                    }
                }

                IgnoreDisableOnHierarchyForNSeconds(999);

                int undoGroup = -1;
                string undoName = null;

                // Record state for undo (only do this if the previous state wasn't a split action).
                if (replaceInScene)
                {
                    // Prepare undo
                    undoName = "ME: Split";
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName(undoName);
                    undoGroup = Undo.GetCurrentGroup();

                    var comps = getUniqueComponentsInSeletedTris();
                    
                    if (comps.Count > 0)
                    {
                        objectToSelectAfterSplit = comps[0].gameObject;
                    }

                    foreach (var comp in comps)
                    {
                        Undo.RegisterFullObjectHierarchyUndo(comp.gameObject, "ME: Split: " + comp.gameObject.name);
                    }

                    // add dummy to undo stack
                    _undoStack.Add(new UndoState(undoName));
                }

                // resultsA is the part the we split off of the mesh. resultsB below will be the "rest" (what's left of the original mesh after splitting).
                var resultsA = MeshGenerator.GenerateMesh(LastSelectedObject.transform, _transformToLocalSpace, _cursorPosition, _cursorRotation, _selectedTriangles, splitPartPath,
                    replaceOldMesh, preserveSubMeshes, combineSubMeshesBasedOnMaterials, combineMeshes, saveAsObj, extractTextures,
                    extractBoneWeights, extractBoneTransforms,
                    extractBlendShapes,
                    createPrefab: true,
                    showProgress: true,
                    recordUndo: true);

                if (replaceInScene)
                {
                    // TODO/N2H: Actually detect if mesh has bones (right now this is implicitly correct
                    // since extract.. are only available for meshes with bones but it would be cleaner
                    // if we checked here.
                    bool didTransformToLocalSpace = _transformToLocalSpace || extractBlendShapes || extractBoneWeights;

                    // Place prefab in the hierarchy right next to the selected game object
                    Transform parent = null;
                    int rootSiblingIndex = -1;
                    bool noParentDueToDeletedAsset = false;
                    foreach (var tri in _selectedTriangles)
                    {
                        // If the user has "Replace" enabled and if the current name is the same as the existing mesh then the
                        // mesh asset will be replaced and thus the Transform will no longer exist. We simply skip the whole
                        // replace in scene since it is unclear where to insert the new mesh and it probably already replaces the
                        // existing one.
                        if (tri.Component == null || tri.Component.gameObject == null)
                        {
                            noParentDueToDeletedAsset = true;
                        }

                        // If _transformToLocalSpace is disabled then the parent should be the
                        // scene root (global space) since the model will also be in global space
                        // and will have scale and rotation already baked into the mesh.
                        if (didTransformToLocalSpace && tri.Component != null && tri.Component.transform != null && tri.Component.transform.parent != null)
                        {
                            parent = tri.Component.transform.parent;
                        }
                        else
                        {
                            // Find sibling index at root so we can place the new object there
                            var p = tri.Component.transform;
                            while (p != null && p.parent != null)
                            {
                                p = p.parent;
                            }
                            rootSiblingIndex = p.GetSiblingIndex();
                        }
                        break;
                    }

                    if (!noParentDueToDeletedAsset)
                    {
                        // TODO / N2H: If parent is null due to "replace" being active with the same name then
                        //             find the instance root and apply the root bone changes etc. on that instead
                        //             of the new instance. Currently it just does nothing if parent == null.
                        foreach (var result in resultsA)
                        {
                            if (result.Prefab == null)
                                continue;

                            var instance = PrefabUtility.InstantiatePrefab(result.Prefab, parent) as GameObject;
                            bool pivotChangeWasApplied = result.PivotDelta != Vector3.zero;

                            if (didTransformToLocalSpace)
                            {
                                instance.transform.localPosition = result.Component.transform.localPosition;
                                instance.transform.localRotation = result.Component.transform.localRotation;
                                instance.transform.localScale = result.Component.transform.localScale;

                                // Place new object below the existing one in the hierarchy
                                var index = result.Component.transform.GetSiblingIndex();
                                instance.transform.SetSiblingIndex(index+1);

                                // Move the transform according to changed pivot
                                if (pivotChangeWasApplied)
                                {
                                    // Why the parent? The split instance is placed in the parent of the transform not within.
                                    if (result.Component.transform.parent == null)
                                    {
                                        // Fallback if parent is null we assume world space.
                                        instance.transform.localPosition -= result.PivotDelta;
                                    }
                                    else
                                    {
                                        instance.transform.localPosition -= result.Component.transform.parent.InverseTransformVector(result.PivotDelta);
                                    }
                                }
                            }
                            else
                            {
                                instance.transform.localPosition = Vector3.zero;
                                instance.transform.localRotation = Quaternion.identity;
                                instance.transform.localScale = Vector3.one;
                                instance.transform.SetSiblingIndex(rootSiblingIndex+1);

                                // Move the transform according to changed pivot
                                if (pivotChangeWasApplied)
                                {
                                    instance.transform.localPosition -= result.PivotDelta;
                                }
                            }

                            Undo.RegisterCreatedObjectUndo(instance, "ME: Created instance");

                            // If there is a skinned mesh renderer then try to use to the bones of that renderer.
                            var skinnedRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                            if (skinnedRenderer != null)
                            {
                                // NOTICE: If that renderer in the PARENT is a nested prefab then it's likely to be replaced
                                // by the prefab generated in "resultsB" below. Root will then become null.
                                var existingSkinnedRenderer = parent.GetComponentInChildren<SkinnedMeshRenderer>();
                                if (existingSkinnedRenderer != null)
                                {
                                    // Replace root bone with parent root bone only if that parent root bone still exists.
                                    if (existingSkinnedRenderer.rootBone != null && existingSkinnedRenderer.rootBone.gameObject != null)
                                    {
                                        // Disable root bone
                                        var localRoot = skinnedRenderer.rootBone;
                                        localRoot.gameObject.SetActive(false);
                                        // Use existing root bone
                                        skinnedRenderer.rootBone = existingSkinnedRenderer.rootBone;
                                        // Refresh bones assignments based on new existing renderer.
                                        var boneDataResolver = skinnedRenderer.GetComponentInChildren<BoneDataResolver>();
                                        boneDataResolver?.Resolve(existingSkinnedRenderer);
                                    }
                                }
                            }

                            objectToSelectAfterSplit = instance;
                            break;
                        }
                    }
                }

                invertSelection(limitToObjectsWithSelections: true);

                // Use the existing name of the mesh as the new name.
                string restName = null;
                foreach (var tri in _selectedTriangles)
                {
                    restName = tri.SharedMesh.name;
                    break;
                }
                if (string.IsNullOrEmpty(restName) || restName == splitPartName)
                {
                    restName = splitPartName + "-base";
                }

                string restPath = getExtractedFilesLocation() + restName;

                // Check if the selected components are already part of a prefab and if that prefab is about to be replaced by
                // the mesh exraction process. If yes, then copy the parts of the overrides that should be preserved and re-apply
                // them after the generation.
                Transform prefabInstanceParentInScene = null;
                int prefabInstanceSiblingIndexInScene = -1;
                PropertyModificationWithPath[] prefabOverrides = null;
                var components = getUniqueComponentsInSeletedTris();
                if (components.Count > 0 && PrefabUtility.IsPartOfPrefabInstance(components[0]))
                {
                    var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(components[0]);
                    var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(components[0]);
                    string comparablePrefabPath = prefabPath.Replace(".prefab", "");
                    string comparableRestPath = "Assets/" + restPath;
                    if (comparablePrefabPath == comparableRestPath)
                    {
                        // It's unclear to me what exact component the  modifications do reference.
                        // Since I want to store the modifications and then re-apply them to the newly
                        // created prefab I need the prefab root (to construct relative paths). Yet
                        // neither GetNearestPrefabInstanceRoot() or
                        // PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot() + PrefabUtility.LoadPrefabContents()
                        // nor AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) did give me the object that these
                        // modifications are children of. So as a last resort I simply use the first modification and
                        // traverse it upwards to the root (parent = null).
                        // Update: Maybe PrefabUtility.GetCorrespondingObjectFromSource() would do it.
                        //         This whole Prefab API is such a mess.
                        var mods = PrefabUtility.GetPropertyModifications(instanceRoot); // <- weirdly instanceRoot will not be the root
                        if (mods.Length > 0)
                        {
                            Transform root = null;
                            var go = mods[0].target as GameObject;
                            if (go != null) {
                                root = go.transform;
                            } else {
                                var comp = mods[0].target as Component;
                                if (comp != null)
                                {
                                    root = comp.transform;
                                }
                            }
                            if (root != null)
                            {
                                while (root.parent != null)
                                {
                                    root = root.parent;
                                }

                                prefabOverrides = PropertyModificationWithPath.Convert(root, mods);
                                prefabInstanceParentInScene = instanceRoot.transform.parent;
                                prefabInstanceSiblingIndexInScene = instanceRoot.transform.GetSiblingIndex();
                            }
                        }
                    }
                }

                // Keep the pivot as is for the remaining part.
                bool transformToLocalTransform = true;
                var pivotPosGlobal = LastSelectedObject.transform.position;
                var pivotRotationGlobal = LastSelectedObject.transform.rotation;

                var resultsB = MeshGenerator.GenerateMesh(LastSelectedObject.transform, transformToLocalTransform, pivotPosGlobal, pivotRotationGlobal, _selectedTriangles, restPath,
                    replaceOldMesh, preserveSubMeshes, combineSubMeshesBasedOnMaterials, combineMeshes, saveAsObj, extractTextures,
                    extractBoneWeights, extractBoneTransforms,
                    extractBlendShapes,
                    createPrefab: true,
                    showProgress: true);

                invertSelection(limitToObjectsWithSelections: true);

                if (replaceInScene)
                {
                    this.assignMeshToRendererInScene(resultsB);
                }

                // Find prefab instance root and apply the stored overrides to it.
                if (prefabInstanceParentInScene != null)
                {
                    var prefabInstanceRoot = prefabInstanceParentInScene.GetChild(prefabInstanceSiblingIndexInScene);
                    if (prefabOverrides != null)
                    {
                        if (prefabInstanceRoot != null && PrefabUtility.IsAnyPrefabInstanceRoot(prefabInstanceRoot.gameObject))
                        {
                            // Never got PrefabUtility.SetPropertyModifications() to work. Link:
                            // https://issuetracker.unity3d.com/issues/documentation-for-prefabutility-dot-setpropertymodifications-is-lacking-helpful-information
                            // PropertyModificationWithPath.SetModifications(prefabInstanceRoot, prefabOverrides);

                            // Solution: Apply the changes manually and mark them as overrides.
                            PropertyModificationWithPath.SetPartialModificationsManually(prefabInstanceRoot, prefabOverrides);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(prefabInstanceRoot);
                        }
                    }
                }

                clearSelection();
                TriangleCache.CacheTriangles();

                // Record state for undo
                if (replaceInScene)
                {
                    Undo.CollapseUndoOperations(undoGroup);

                    // Add another dummy undo
                    _undoStack.Add(new UndoState(undoName));
                }

                if (objectToSelectAfterSplit != null)
                    Selection.objects = new GameObject[] { objectToSelectAfterSplit };

                IgnoreDisableOnHierarchyForNSeconds(0.5);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public SelectedTriangle getFirstSelectedTriangle()
        {
            var e = _selectedTriangles.GetEnumerator();
            e.MoveNext();
            return e.Current;
        }

        public void Remove(
            string name,
            bool replaceOldMesh, bool preserveSubMeshes, bool combineSubMeshesBasedOnMaterials,
            bool combineMeshes, bool saveAsObj, bool extractTextures, bool extractBoneWeights, bool extractBoneTransforms, bool extractBlendShapes,
            bool replaceInScene)
        {
            List<Component> componentsForRedoUndo = null;
            if (replaceInScene)
            {
                componentsForRedoUndo = getUniqueComponentsInSeletedTris();
                if (_undoStack.Peek().Type != UndoStateType.ExtractAndReplaceInScene)
                    recordMeshGenerationUndo(componentsForRedoUndo);
            }

            string path = getExtractedFilesLocation() + name;

            EditorUtility.DisplayProgressBar("Extracting Mesh", "Gathering data for " + _selectedTriangles.Count + " triangles " + (_selectedTriangles.Count > 10000 ? "(this may take a while)" : "") + " ..", 0.1f);
            try
            {
                IgnoreDisableOnHierarchyForNSeconds(999);

                invertSelection(limitToObjectsWithSelections: true);

                var results = MeshGenerator.GenerateMesh(LastSelectedObject.transform, _transformToLocalSpace, _cursorPosition, _cursorRotation, _selectedTriangles, path,
                    replaceOldMesh, preserveSubMeshes, combineSubMeshesBasedOnMaterials, combineMeshes, saveAsObj, extractTextures,
                    extractBoneWeights, extractBoneTransforms,
                    extractBlendShapes,
                    createPrefab: true,
                    showProgress: true);

                if (replaceInScene)
                {
                    this.assignMeshToRendererInScene(results);
                }
                else
                {
                    // Undo inversion
                    invertSelection(limitToObjectsWithSelections: true);
                }

                if (replaceInScene)
                {
                    recordMeshGenerationUndo(componentsForRedoUndo);
                }

                IgnoreDisableOnHierarchyForNSeconds(0.5);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void Extract(
            string name,
            bool replaceOldFiles, bool preserveSubMeshes, bool combineSubMeshesBasedOnMaterials,
            bool combineMeshes, bool saveAsObj, bool extractTextures, bool extractBoneWeights, bool extractBoneTransforms, bool extractBlendShapes,
            bool replaceInScene)
        {
            string path = getExtractedFilesLocation() + name;

            EditorUtility.DisplayProgressBar("Extracting Mesh", "Gathering data for " + _selectedTriangles.Count + " triangles " + (_selectedTriangles.Count > 10000 ? "(this may take a while)" : "") + " ..", 0.1f);
            try
            {
                IgnoreDisableOnHierarchyForNSeconds(999);

                var results = MeshGenerator.GenerateMesh(LastSelectedObject.transform, _transformToLocalSpace, _cursorPosition, _cursorRotation, _selectedTriangles, path,
                    replaceOldFiles, preserveSubMeshes, combineSubMeshesBasedOnMaterials, combineMeshes, saveAsObj, extractTextures,
                    extractBoneWeights, extractBoneTransforms,
                    extractBlendShapes,
                    createPrefab: true,
                    showProgress: true);

                if (replaceInScene)
                    this.assignMeshToRendererInScene(results);

                IgnoreDisableOnHierarchyForNSeconds(0.5);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private List<Component> getUniqueComponentsInSeletedTris(List<Component> results = null)
        {
            if (results == null)
                results = new List<Component>();
            else
                results.Clear();

            foreach (var tri in _selectedTriangles)
            {
                if (!results.Contains(tri.Component))
                {
                    results.Add(tri.Component);
                }
            }

            return results;
        }

        private void recordMeshGenerationUndo(List<Component> components)
        {
            var meshes = new List<Mesh>();
            var assetPaths = new List<string>();
            var materials = new List<Material[]>();

            foreach (var comp in components)
            {
                Mesh sharedMesh = null;
                var skinnedRenderer = comp as SkinnedMeshRenderer;
                if (skinnedRenderer != null)
                {
                    sharedMesh = skinnedRenderer.sharedMesh;
                    materials.Add(skinnedRenderer.sharedMaterials);
                }
                else
                {
                    var meshFilter = comp.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        sharedMesh = meshFilter.sharedMesh;
                        var renderer = comp.transform.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            materials.Add(renderer.sharedMaterials);
                        }
                        else
                        {
                            materials.Add(null);
                        }
                    }
                }
                meshes.Add(sharedMesh);

                // Find the path of the asset (may return null if there is no asset). 
                if (sharedMesh != null)
                {
                    string path = AssetDatabase.GetAssetPath(sharedMesh);
                    assetPaths.Add(path);
                }
                else
                {
                    assetPaths.Add(null);
                }
            }
            
            _undoStack.Add(new UndoState(components, meshes, assetPaths, materials), 0, forceNewGroup: true);
        }

        private void assignMeshToRendererInScene(List<MeshGenerator.Result> results)
        {
            SkinnedMeshRenderer skinnedRenderer;
            MeshFilter meshFilter;
            foreach (var result in results)
            {
                // Component will be null if it was part of a prefab that got regenerated and replaced which
                // means it does not need to be replaced.
                if (result.Mesh == null || result.Component == null)
                    continue;

                skinnedRenderer = result.Component as SkinnedMeshRenderer;
                meshFilter = result.Component as MeshFilter;
                if (skinnedRenderer != null)
                {
                    skinnedRenderer.sharedMesh = result.Mesh;
                    skinnedRenderer.sharedMaterials = result.Materials.ToArray();
                }
                else if (meshFilter != null)
                {
                    meshFilter.sharedMesh = result.Mesh;
                    var renderer = meshFilter.transform.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterials = result.Materials.ToArray();
                    }
                }

                EditorUtility.SetDirty(result.Component);
                EditorSceneManager.MarkSceneDirty(result.Component.gameObject.scene);
            }

            _selectedTriangles.Clear();
            TriangleCache.CacheTriangles();
            TriangleCache.RebuildBakedMeshesCache(_selectedObjects);
        }

        void resetExtract()
        {
            _newMeshName = "Mesh";
            _replaceOldMesh = true;
            _preserveSubMeshes = true;
            _combineSubMeshesBasedOnMaterials = true;
            _combineMeshes = true;
            _saveAsObj = false;
            _extractTextures = true;

            _pivotModified = false;
        }
    }
}
