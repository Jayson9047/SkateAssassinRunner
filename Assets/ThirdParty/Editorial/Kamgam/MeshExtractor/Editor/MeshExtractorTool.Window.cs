using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Globalization;
using System;
using UnityEngine.TestTools.Utils;

namespace Kamgam.MeshExtractor
{
    public static class BackgroundTexture
    {
        private static Dictionary<Color, Texture2D> textures = new Dictionary<Color, Texture2D>();

        public static Texture2D Get(Color color)
        {
            if (textures.ContainsKey(color) && textures[color] != null) 
                return textures[color];

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            if (textures.ContainsKey(color))
                textures[color] = texture;
            else
                textures.Add(color, texture);

            return texture;
        }
    }

    partial class MeshExtractorTool
    {
        Rect windowRect;

        [System.NonSerialized]
        string _newMeshName = "Mesh";

        [System.NonSerialized]
        bool _replaceOldMesh = true;

        [System.NonSerialized]
        bool _preserveSubMeshes = true;

        [System.NonSerialized]
        bool _combineSubMeshesBasedOnMaterials = true;

        [System.NonSerialized]
        bool _combineMeshes = true;

        [System.NonSerialized]
        bool _saveAsObj = false;
        
        [System.NonSerialized]
        bool _extractTextures = true;
        
        [System.NonSerialized]
        bool _extractBoneWeights = false;
        
        [System.NonSerialized]
        bool _extractBoneTransforms = true;

        [System.NonSerialized]
        bool _extractBlendShapes = false;

        [System.NonSerialized]
        bool _transformToLocalSpace = true;

        [System.NonSerialized]
        bool _replaceMeshInScene = false;

        [System.NonSerialized]
        protected double _lastSettingsOpenTimestamp;

        /// <summary>
        /// 0 = hidden
        /// 1 = save
        /// 2 = load
        /// </summary>
        [System.NonSerialized]
        protected int _saveLoadVisibility;

        [System.NonSerialized]
        protected string _saveSelectionName = "Quicksave";

        [System.NonSerialized]
        protected Vector2 _loadSelectionScrollViewPos;

        void initWindowSize()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                var settings = MeshExtractorSettings.GetOrCreateSettings();
                windowRect.position = settings.WindowPosition;
                windowRect.width = 250;
                windowRect.height = 90;

                // If the window is not yet set or if it's outside the scene view then reset position.
                if (
                       windowRect.position.x > SceneView.lastActiveSceneView.position.width
                    || windowRect.position.x < 0
                    || windowRect.position.y > SceneView.lastActiveSceneView.position.height
                    || windowRect.position.y < 0
                    )
                {
                    // center
                    windowRect.position = new Vector2(
                        SceneView.lastActiveSceneView.position.width * 0.5f,
                        SceneView.lastActiveSceneView.position.height * 0.5f
                        );
                    settings.WindowPosition = windowRect.position;
                    EditorUtility.SetDirty(settings);
                }
            }
        }

        [MenuItem("Tools/Mesh Extractor/Debug/Recenter Window", priority = 220)]
        static void RecenterWindowMenu()
        {
            if (Instance != null)
                Instance.RecenterWindow();
        }

        public void RecenterWindow()
        {
            var settings = MeshExtractorSettings.GetOrCreateSettings();
            
            // center
            windowRect.position = new Vector2(
                SceneView.lastActiveSceneView.position.width * 0.5f,
                SceneView.lastActiveSceneView.position.height * 0.5f
                );

            // dimensions
            windowRect.width = 250;
            windowRect.height = 90;

            settings.WindowPosition = windowRect.position;
            EditorUtility.SetDirty(settings);

            Logger.LogWarning("Please consider upgrading Unity. There is a bug in Unity 2021.0 to 2021.2.3f1 and 2022.0 - 2022.1.0a15, see: https://issuetracker.unity3d.com/issues/tool-handles-are-invisible-in-scene-view-when-certain-objects-are-selected");
        }

        void drawWindow(SceneView sceneView, int controlID)
        {
            Handles.BeginGUI();

            var oldRect = windowRect;
            windowRect = GUILayout.Window(controlID, windowRect, drawWindowContent, "Mesh Extractor v" + MeshExtractorSettings.Version);

            // Auto save window position in settings if changed.
            if (Vector2.SqrMagnitude(oldRect.position - windowRect.position) > 0.01f)
            {
                var settings = MeshExtractorSettings.GetOrCreateSettings();
                settings.WindowPosition = windowRect.position;
                EditorUtility.SetDirty(settings);
            }

            GUI.enabled = true;
            Handles.EndGUI();
        }

        void drawWindowContent(int controlID)
        {
            var settings = MeshExtractorSettings.GetOrCreateSettings();

            var bgColor = UtilsEditor.IsLightTheme() ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.25f, 0.25f, 0.25f);
            var tex = BackgroundTexture.Get(bgColor);
            if (UtilsEditor.IsLightTheme())
            {
                GUI.skin.label.normal.textColor = Color.black;
                GUI.skin.label.hover.textColor = new Color(0.2f, 0.2f, 0.2f);

                GUI.skin.toggle.normal.textColor = Color.black;
                GUI.skin.toggle.hover.textColor = new Color(0.2f, 0.2f, 0.2f);

                // After switching to light theme the button bg textures are suddenly white but ONLY until Unity is restarted
                // then the buttons turn back to black .. WTH?!?
                //GUI.skin.button.normal.textColor = Color.black;
                //GUI.skin.button.active.textColor = Color.black;
                //GUI.skin.button.focused.textColor = Color.black;
                //GUI.skin.button.hover.textColor = new Color(0.2f, 0.2f, 0.2f);
                GUI.skin.button.normal.textColor = Color.white;
                GUI.skin.button.active.textColor = Color.white;
                GUI.skin.button.focused.textColor = Color.white;
                GUI.skin.button.hover.textColor = new Color(0.8f, 0.8f, 0.8f);
            }
            GUI.DrawTexture(new Rect(5, 22, windowRect.width - 10, windowRect.height - 26), tex);

            BeginHorizontalIndent(5, beginVerticalInside: true);

            GUILayout.Space(5);

            // settings button
            var settingsBtnStyle = GUIStyle.none;
            settingsBtnStyle.normal.background = BackgroundTexture.Get(UtilsEditor.IsLightTheme() ? new Color(0.45f, 0.45f, 0.45f) : bgColor);
            settingsBtnStyle.hover.background = BackgroundTexture.Get(new Color(0.5f, 0.5f, 0.5f));
            var settingsBtnContent = EditorGUIUtility.IconContent("d_Settings");
            settingsBtnContent.tooltip = "Close the tool (Esc).";
            if (GUI.Button(new Rect(windowRect.width - 40, 2, 16, 20), settingsBtnContent, settingsBtnStyle))
            {
                MeshExtractorSettings.OpenSettings();
                _lastSettingsOpenTimestamp = EditorApplication.timeSinceStartup;
            }

            // close button
            var closeBtnStyle = GUIStyle.none;
            closeBtnStyle.normal.background = BackgroundTexture.Get(UtilsEditor.IsLightTheme() ? new Color(0.45f, 0.45f, 0.45f) : bgColor);
            closeBtnStyle.hover.background = BackgroundTexture.Get(new Color(0.5f, 0.5f, 0.5f));
#if UNITY_2023_1_OR_NEWER
            var closeBtnContent = EditorGUIUtility.IconContent("d_clear@2x");
#else
            var closeBtnContent = EditorGUIUtility.IconContent("d_winbtn_win_close_a@2x");
#endif
            closeBtnContent.tooltip = "Close the tool (Esc).";
            if (GUI.Button(new Rect(windowRect.width - 21, 2, 16, 20), closeBtnContent, closeBtnStyle))
            {
                exitTool(); 
            }

            // Button bar
            drawButtonsInWindow();

            // Content
            switch (_mode)
            {
                case Mode.PickObjects:
                    drawPickObjectsWindowContent();
                    _firstExtractDraw = true;
                    break;

                case Mode.PaintSelection:
                    drawSelectWindowContentGUI(settings);
                    _firstExtractDraw = true;
                    break;

                case Mode.ExtractMesh:
                    drawExtractMeshWindowContentGUI();
                    break;

                default:
                    break;
            }


            GUILayout.Space(2);

            EndHorizontalIndent(bothSides: true);

            GUILayout.Space(4);

            GUI.SetNextControlName("BG");
            GUI.DragWindow();
        }

        private void drawButtonsInWindow()
        {
            GUILayout.BeginHorizontal();

            GUI.enabled = _mode != Mode.PickObjects;
            string icon = UtilsEditor.IsLightTheme() ? "FilterByType@2x" : "d_FilterByType@2x"; 
            if (DrawButton("",
                icon: icon,
                style: "ButtonLeft",
                tooltip: "Select objects",
                options: GUILayout.Height(22)))
            {
                SetMode(Mode.PickObjects);
            }
            GUI.enabled = true;

            GUI.enabled = _mode != Mode.PaintSelection;
            icon = UtilsEditor.IsLightTheme() ? "pick@2x" : "d_pick@2x";
            if (DrawButton("",
                icon: icon,
                style: "ButtonMid",
                tooltip: "Start painting the selection.",
                options: GUILayout.Height(22)))
            {
                if (_selectedObjects.Length > 0)
                {
                    SetMode(Mode.PaintSelection);
                }
                else
                {
                    EditorUtility.DisplayDialog("Select an object first!", "Please select an object before entering the triangle selection tab.", "Ok");
                    GUIUtility.ExitGUI();
                }
            }
            GUI.enabled = true;

            GUI.enabled = _mode != Mode.ExtractMesh;
            icon = UtilsEditor.IsLightTheme() ? "PreMatCube" : "d_PreMatCube";
            if (DrawButton("",
                icon: icon,
                style: "ButtonRight",
                tooltip: "Extract the selected mesh. Available only if at least one triangle is selected.",
                options: GUILayout.Height(22)))
            {
                if (_selectedObjects.Length > 0 && _selectedTriangles.Count > 0)
                {
                    SetMode(Mode.ExtractMesh);
                }
                else
                {
                    EditorUtility.DisplayDialog("Select a triangle first!", "Please select at least one triangle before entering the extraction tab.", "Ok");
                    GUIUtility.ExitGUI();
                }
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        void drawPickObjectsWindowContent()
        {
            DrawLabel("Select objects", bold: true);
            DrawLabel("Select one or more objects to extract meshes from.", "This is useful to avoid selecting background meshes by accident. You can return to this step at any time and add or remove objects.", wordwrap: true);
            
            if (DrawButton("Reset", "Clears the current selection, deselects any object and resets all configurations to default."))
            {
                clearSelection();
                Selection.objects = new GameObject[] { };
                resetSelect();
                resetExtract();
            }

            GUILayout.Space(5);

            GUI.enabled = _selectedObjects.Length > 0;
            var col = GUI.color;
            if (_selectedObjects.Length > 0)
                GUI.color = new Color(0.8f, 1f, 0.8f);
            else
                GUI.color = new Color(1f, 0.8f, 0.8f);
            if (DrawButton("Next >", "If you have some objects (with meshes) selected then this button will enable you to go to the next step."))
            {
                SetMode(Mode.PaintSelection);
            }
            GUI.color = col;
            GUI.enabled = true;
        }

        void drawSelectWindowContentGUI(MeshExtractorSettings settings)
        {
            GUILayout.BeginHorizontal();
            DrawLabel("Select polygons", "Paint on the objects to select polyons.", bold: true);

            // Tab menu for selection
            GUI.enabled = _saveLoadVisibility != 0;
            string icon = UtilsEditor.IsLightTheme() ? "pick@2x": "d_pick@2x";
            if (DrawButton("", "Show SELECT options.", icon, "ButtonLeft", GUILayout.Width(24), GUILayout.Height(24)))
            {
                _saveLoadVisibility = 0;
                GUI.FocusControl("BG");
            }
            GUI.enabled = _saveLoadVisibility != 1;
            icon = UtilsEditor.IsLightTheme() ? "CloudConnect@2x": "d_CloudConnect@2x";
            if (DrawButton("", "Show SAVE options.", icon, "ButtonMid", GUILayout.Width(24), GUILayout.Height(24)))
            {
                _saveLoadVisibility = 1;
                GUI.FocusControl("BG");
            }
            GUI.enabled = _saveLoadVisibility != 2;
            icon = UtilsEditor.IsLightTheme() ? "scrolldown@2x": "d_scrolldown@2x";
            if (DrawButton("", "Show LOAD options.", icon, "ButtonRight", GUILayout.Width(24), GUILayout.Height(24)))
            {
                _saveLoadVisibility = 2;
                GUI.FocusControl("BG");
            }
            GUI.enabled = true;
            icon = UtilsEditor.IsLightTheme() ? "FrameCapture@2x": "d_FrameCapture@2x";
            if (DrawButton("", "Quicksave: Saves the current selection into the last saved selection without asking for confirmation.", icon, null, GUILayout.Width(24), GUILayout.Height(24)))
            {
                saveSelectionFromUI(suppressReplacementConfirmation: true);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();


            if (_saveLoadVisibility == 0)
                drawSelectTrianglesGUI();
            else
                drawSelectSaveLoadGUI(settings);

            // footer buttons
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (DrawButton("< Pick Object", "Click here if you want to start working on another object."))
            {
                SetMode(Mode.PickObjects);
            }

            GUI.enabled = HasValidSelection();
            var col = GUI.color;
            if (_selectedTriangles.Count > 0)
                GUI.color = new Color(0.8f, 1f, 0.8f);
            else
                GUI.color = new Color(1f, 0.8f, 0.8f);
            if (DrawButton("Next >", "Once you have selected some triangles you can proceed to the extraction step."))
            {
                SetMode(Mode.ExtractMesh);
            }
            GUI.color = col;
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        private void drawSelectTrianglesGUI()
        {
            GUI.enabled = _selectedObjects.Length > 0;

            _selectCullBack = !EditorGUILayout.ToggleLeft(new GUIContent("X-Ray", "X-Ray mode allows you to select front and back facing triangles at the same time."), !_selectCullBack);

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Brush Size:", "Reduce the brush size to 0 to select only one triangle at a time.\n\nYou can also use SHIFT + MOUSE WHEEL to change the brush size."), GUILayout.MaxWidth(75));
            _selectBrushSize = GUILayout.HorizontalSlider(_selectBrushSize, 0f, 1f);
            GUILayout.Label((_selectBrushSize * 10).ToString("f1", CultureInfo.InvariantCulture), GUILayout.MaxWidth(22));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Brush Depth:", "Brush depth defines how far into the object the selection will go. This helps to avoid selecting background polygons by accident. If you want infinite depth then simply turn on X-Ray."), GUILayout.MaxWidth(75));
            _selectBrushDepth = GUILayout.HorizontalSlider(_selectBrushDepth, 0f, 2f);
            _selectBrushDepth = EditorGUILayout.FloatField(_selectBrushDepth, GUILayout.MaxWidth(32));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = _lastSelectedTriangle != null;
            if (DrawButton("Select Linked", "Selects all triangles that are connected to the last selected triangle.\n\nHold SHIFT while clicking the button to deselect linked.\n\nHINT: You can press S or SHIFT + S while selecting to trigger this action."))
            {
                // Delayed execute
                var evt = Event.current;
                EditorApplication.delayCall += () => addLinkedToSelection(remove: evt.shift);
            }
            if (DrawButton("Deselect", "Deselects all triangles that are connected to the last selected triangle."))
            {
                // Delayed execute
                EditorApplication.delayCall += () => addLinkedToSelection(remove: true);
            }
            _limitLinkedSearchToSubMesh = EditorGUILayout.ToggleLeft(
                new GUIContent("Limit", "Enable to limit selection to a single sub mesh.\nIt will use the sub mesh of the last selected triangle."),
                _limitLinkedSearchToSubMesh,
                GUILayout.Width(50)
                );
            if (DrawButton("SM", "Select all triangles that have the same material as the last selected triangle."))
            {
                addSameMaterialToSelection(remove: Event.current.shift);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            if (DrawButton("Clear", "Clears the current selection."))
            {
                clearSelection();
            }
            if (DrawButton("Invert All", "Inverts the current selection. It takes ALL selected objects into account.", null, "ButtonLeft"))
            {
                invertSelection(limitToObjectsWithSelections: false);
            }
            if (DrawButton("Invert Object", "Inverts the current selection. It takes only those objects into account that have at least one triangle selected.", null, "ButtonRight"))
            {
                invertSelection(limitToObjectsWithSelections: true);
            }
            GUILayout.EndHorizontal();
        }

        private void drawSelectSaveLoadGUI(MeshExtractorSettings settings)
        {
            // Save Section
            if (_saveLoadVisibility == 1)
            {
                GUILayout.BeginHorizontal();
                DrawLabel("Save:", "Enter a name for your saved selection.", wordwrap: false);
                _saveSelectionName = EditorGUILayout.TextField(_saveSelectionName);
                GUILayout.EndHorizontal();
                if (DrawButton("Save", "Save the selection now."))
                {
                    saveSelectionFromUI(suppressReplacementConfirmation: false);
                }
            }
            // Load Section
            else if (_saveLoadVisibility == 2)
            {
                GUILayout.BeginHorizontal();
                DrawLabel("Snapshots:", "A list of all available snapshots", wordwrap: false);
                GUILayout.FlexibleSpace();
                if (DrawButton("Cear", "Clears the current selection.", null, null, GUILayout.Width(40), GUILayout.Height(18)))
                {
                    clearSelection();
                }
                
                GUILayout.EndHorizontal();

                _loadSelectionScrollViewPos = GUILayout.BeginScrollView(_loadSelectionScrollViewPos, GUILayout.MinHeight(45), GUILayout.MaxHeight(100));
                foreach (var item in settings.SelectionSnapshots)
                {
                    GUILayout.BeginHorizontal();
                    if (DrawButton("X", null, null, null, GUILayout.Width(22)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            deleteSavedSelection(item.Name);
                            SceneView.RepaintAll();
                        };
                    }
                    if (DrawButton(item.Name, "Replaces the current selection with this selection."))
                    {
                        EditorUtility.DisplayProgressBar("Loading Selection", "...", 0.2f);
                        _saveLoadVisibility = 0;
                        try
                        {
                            loadSelection(item, additive: false);
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                        }
                    }
                    if (DrawButton("+", "Adds this selection to the current selection.", null, null, GUILayout.Width(22)))
                    {
                        EditorUtility.DisplayProgressBar("Loading Selection", "...", 0.2f);
                        try
                        {
                            loadSelection(item, additive: true);
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

            GUILayout.Space(5);
        }

        protected void saveSelectionFromUI(bool suppressReplacementConfirmation)
        {
            EditorUtility.DisplayProgressBar("Saving Selection", "...", 0.2f);
            try
            {
                bool didSave = saveSelection(_saveSelectionName, suppressReplacementConfirmation);
                if (didSave)
                {
                    _saveLoadVisibility = 0;
                    Logger.LogMessage("Selection saved as '" + _saveSelectionName + "'.");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        protected bool _firstExtractDraw = true;

        protected List<Component> _uniqueComponentsInSelectedTriangles = new List<Component>();
        protected List<BoneWeight> _tmpUniqueComponentsBoneWeights = new List<BoneWeight>();

        void drawExtractMeshWindowContentGUI()
        {
            // Check if there is a skinned mesh renderer
            bool hasBones = false;
            bool hasBlendShapes = false;
            getUniqueComponentsInSeletedTris(_uniqueComponentsInSelectedTriangles);
            _tmpUniqueComponentsBoneWeights.Clear();
            foreach (var comp in _uniqueComponentsInSelectedTriangles)
            {
                var skinnedRenderer = comp as SkinnedMeshRenderer;
                if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
                {
                    hasBlendShapes |= skinnedRenderer.sharedMesh.blendShapeCount > 0;
                    skinnedRenderer.sharedMesh.GetBoneWeights(_tmpUniqueComponentsBoneWeights);
                    hasBones |= _tmpUniqueComponentsBoneWeights.Count > 0;
                }
            }
            _tmpUniqueComponentsBoneWeights.Clear();

            GUI.enabled = _selectedObjects.Length > 0 && _selectedTriangles.Count > 0;

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            DrawLabel("Name:", "Enter the file name of the new mesh.", wordwrap: false);
            _newMeshName = EditorGUILayout.TextField(_newMeshName);
            _replaceOldMesh = EditorGUILayout.ToggleLeft(new GUIContent("Replace", "Replace existing meshes? If off then a number will be appended to every new file."), _replaceOldMesh, GUILayout.MaxWidth(65));
            GUILayout.EndHorizontal();

            _preserveSubMeshes = EditorGUILayout.ToggleLeft(
                new GUIContent("Preserve SubMeshes", "Enable to preserve (copy) sub meshes in the new mesh. If disabled then all meshes within one renderer will be merged into a single mesh and the very first material found will be used."),
                _preserveSubMeshes
                );

            GUI.enabled = _preserveSubMeshes;
            _combineSubMeshesBasedOnMaterials = EditorGUILayout.ToggleLeft(
                new GUIContent("Combine SubMeshes by Material", "If multiple sub meshes have the same material assigned to them then these will be merged into one submesh if this option is enabled. This has no effect if 'Preserve SubMeshes' is disabled."),
                _combineSubMeshesBasedOnMaterials
                );
            GUI.enabled = true;

            _combineMeshes = EditorGUILayout.ToggleLeft(
                new GUIContent("Combine Meshes", "If multiple objects (renderers) are selected then this defines whether or not all these meshes should be combined into one mesh. If disabled then the result will be one mesh per selected object."),
                _combineMeshes
                );

            bool oldSaveAsObj = _saveAsObj;
            _saveAsObj = EditorGUILayout.ToggleLeft(
                new GUIContent("Save as .obj", "Export the mesh as .obj & .mtl files instead of a .asset file.\nNOTICE: The obj format does only support one set of UVs.\n\nNOTICE: There is an FBX exporter plugin by Unity, see: https://docs.unity3d.com/Packages/com.unity.formats.fbx@5.1/manual/index.html. You can use that to export the default Unity mesh assets as FBX files."),
                _saveAsObj
                );
            if (_saveAsObj && (_extractBoneWeights || _extractBlendShapes))
            {
                Logger.LogMessage("Bone and blend shape extraction has been disabled because the OBJ format does not support these. HINT: There is an FBX exporter plugin by Unity, see: https://docs.unity3d.com/Packages/com.unity.formats.fbx@5.1/manual/index.html. You can use that to export the default Unity mesh assets as FBX files.");
                _extractBlendShapes = false;
                _extractBoneWeights = false;
            }

            _extractTextures = EditorGUILayout.ToggleLeft(
                new GUIContent("Extract Textures", "Extract the parts of the texture that are used by the selection and create a new (possibly smaller) texture from it." +
                "\n\nNOTICE:" +
                "\n* Textures are searched by common property names like '_MainTex' or '_BaseMap'. Please check the manual for more details." +
                "\n* It does ignore tiling and offests set in shaders." + 
                "\n* The reduction of the texture size depends on the original UV layout (it uses a bounding box)."),
                _extractTextures
                );

            GUILayout.BeginHorizontal();
            GUI.enabled = hasBones && !_saveAsObj;
            _extractBoneWeights = EditorGUILayout.ToggleLeft(
               new GUIContent("Extract Bone Weights", 
               _saveAsObj ? "'Save as .obj' is enabled but the OBJ format does not support bones, thus none can be extracted.\n\nHINT: There is an FBX exporter plugin by Unity, see: https://docs.unity3d.com/Packages/com.unity.formats.fbx@5.1/manual/index.html. You can use that to export the default Unity mesh assets as FBX files." : 
               !hasBones ? "The current selection has no bones, thus none can be extracted." : 
                   "Extracts the bone weights of the source model." +
                   "\n\nNOTICE: This also means it will NOT bake the current pose of the mesh. Instead it will export the default pose." +
                   "\n\nThe expectation is that you will use this on the same bone setup (aka 'Rig' or 'Armature') you exported it from. " +
                   "It will most likely NOT work on another rig. Please read up on skinning and rigging meshes if you are not sure what this means." +
                   "\n\nBone weight information is NOT saved in .obj files (it's just not supported by that file format)."
               ),
               _extractBoneWeights,
               GUILayout.Width(160)
               );
            GUI.enabled = true;

            GUI.enabled = _extractBoneWeights && hasBones && !_saveAsObj;
            _extractBoneTransforms = EditorGUILayout.ToggleLeft(
               new GUIContent("Transf.", "Adds a copy of the bone transforms to the exported prefab." +
               "\n\nUse this if you want to export the bone transforms along with the object. This means you will have a fully functional rig (if it was functional before)." +
               "\n\nNOTICE: If you turn this off then you will have to create (copy from original) or assign new bones that match the bind poses yourself." +
               "\n\nIf you are not sure what this means then please read up on rigging / skinning meshes in Unity (it's just too much to explain in a tiny tooltip, sorry)."
               ),
               _extractBoneTransforms,
               GUILayout.Width(60)
               );
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUI.enabled = hasBlendShapes && !_saveAsObj;
            _extractBlendShapes = EditorGUILayout.ToggleLeft(
               new GUIContent("Extract Blend Shapes", 
               _saveAsObj ? "'Save as .obj' is enabled but the OBJ format does not support blend shapes, thus none can be extracted.\n\nHINT: There is an FBX exporter plugin by Unity, see: https://docs.unity3d.com/Packages/com.unity.formats.fbx@5.1/manual/index.html. You can use that to export the default Unity mesh assets as FBX files." :
               !hasBlendShapes ? "The current selection has no blend shapes, thus none can be extracted." :
               "Extracts the blend shapes of the source model." +
               "\n\nNOTICE: This also means it will NOT bake the current pose of the mesh. Instead it will export the default pose." +
               "\n\nThe expectation is that you will use this on the same animator setup (aka 'Rig' or 'Armature') you exported it from. " +
               "It will most likely NOT work on another rig. Please read up on skinning and rigging meshes if you are not sure what this means." +
               "\n\nBlend shape information is NOT saved in .obj files (it's just not supported by that file format)."
                ),
                _extractBlendShapes,
                GUILayout.Width(160)
            );
            GUI.enabled = true;

            // Replace in Scene
            bool replaceMeshInScenePreviousValue = _replaceMeshInScene;
            _replaceMeshInScene = EditorGUILayout.ToggleLeft(new GUIContent("Replace in Scene (BETA)", "Should the existing mesh in the scene be replaced with the newly generated one?\n\nHandy for in-place iteration.\n\nNOTICE: This has no UNDO / REDO support.\n\nNOTICE: To get the expected result I recommend using this with the Pivot settings: Origin & Local."), _replaceMeshInScene);
            if (_replaceMeshInScene != replaceMeshInScenePreviousValue)
            {
                if (_replaceMeshInScene && _extractBoneWeights && (_pivotBehaviour != PivotBehaviour.Origin || !_transformToLocalSpace || _combineMeshes))
                {
                    // We no longer ask, we simply change it.
                    Logger.LogMessage("Setting up pivot for in-scene replacement (Combine Meshes = OFF, Pivot position and pivot rotation alignment  = ORIGIN).");

                    //bool updatePivotOptions = EditorUtility.DisplayDialog(
                    //    "Set up pivot for in-scene replacement?",
                    //    "You have chosen to replace the existing mesh in the scene but it seems you do not have configured the options yet.\n\n" +
                    //    "Recommended Options:\n" +
                    //    "* Combine Meshes should be OFF\n" +
                    //    "* Pivot position and pivot rotation alignment should both be set to ORIGIN\n" +
                    //    "\n" +
                    //    "Do you want me to set these for you now?",
                    //    "Yes (Recommended)", "No (I did this on purpose)");
                    //if (updatePivotOptions)
                    {
                        _combineMeshes = false;

                        if (_pivotBehaviour != PivotBehaviour.Origin)
                        {
                            _pivotModified = false;
                            _pivotBehaviour = PivotBehaviour.Origin;
                            MeshExtractorTool.Instance.CenterPivotOnLocalOrigin();
                        }
                        if (!_transformToLocalSpace)
                        {
                            _transformToLocalSpace = true;
                            MeshExtractorTool.Instance.AlignPivotRotationWith(localSpace: _transformToLocalSpace);
                        }
                    }
                    GUIUtility.ExitGUI();
                }
            }

            // PIVOT
            GUILayout.BeginHorizontal();
            GUI.enabled = !_extractBoneWeights;
            GUILayout.Label("Pivot:", GUILayout.Width(36));
            string pivotMsg = _extractBoneWeights ? "\n\nPivot modifications will be ignored if bone weights are exported. So it makes not sense to allow this." : "";
            GUI.enabled = _pivotBehaviour != PivotBehaviour.Center;
            if (GUILayout.Button(new GUIContent("Center", "Centers the pivot relative to all selected triangles.\n\nNOTICE: The rotation is controlled by the 'World/Local' setting." + pivotMsg), "ButtonLeft"))
            {
                MeshExtractorTool.Instance.CenterPivotOnTriangles(); 
                _pivotModified = false;
                _pivotBehaviour = PivotBehaviour.Center;
            }
            GUI.enabled = true;
            GUI.enabled = _pivotBehaviour != PivotBehaviour.Origin;
            if (GUILayout.Button(new GUIContent("Origin", "Sets the pivot to 0/0/0 in the local transform of the last selected object ("+LastSelectedObject.name+").\n\nNOTICE: The rotation is controlled by the 'World/Local' setting." + pivotMsg), "ButtonRight"))
            {
                MeshExtractorTool.Instance.CenterPivotOnLocalOrigin();
                _pivotModified = false;
                _pivotBehaviour = PivotBehaviour.Origin;
            }
            GUI.enabled = true;


            // Absolute or Relative
            GUI.enabled = _transformToLocalSpace;
            if (GUILayout.Button(new GUIContent("World", "Extract the mesh in WORLD space by baking the position, scale and rotation into the mesh.\n\nNOTICE: For skinned meshes this will be forced to OFF. Skinned meshes are always extracted in local space to preserve blend shapes and bones." + pivotMsg), "ButtonLeft"))
            {
                _transformToLocalSpace = false;
                MeshExtractorTool.Instance.AlignPivotRotationWith(_transformToLocalSpace);
            }
            GUI.enabled = !_transformToLocalSpace;
            if (GUILayout.Button(new GUIContent("Local", "Extract the mesh in local space of the last selected object ("+LastSelectedObject.name+")."), "ButtonRight"))
            {
                _transformToLocalSpace = true;
                MeshExtractorTool.Instance.AlignPivotRotationWith(_transformToLocalSpace);
            }
            GUI.enabled = true;

            if(_firstExtractDraw)
            {
                MeshExtractorTool.Instance.AlignPivotRotationWith(_transformToLocalSpace);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (DrawButton("< Select"))
            {
                SetMode(Mode.PaintSelection);
            }

            var col = GUI.color;

            GUI.color = new Color(0.8f, 0.8f, 1f);
            if (DrawButton("Split"))
            {
                MeshExtractorTool.Instance.Split(
                    _newMeshName, _replaceOldMesh, _preserveSubMeshes, _combineSubMeshesBasedOnMaterials, _combineMeshes, _saveAsObj, _extractTextures,
                    _extractBoneWeights, _extractBoneTransforms,
                    _extractBlendShapes, _replaceMeshInScene
                    );
                _pivotModified = false;
            }

            GUI.color = new Color(1f, 0.8f, 0.8f);
            if (DrawButton("Remove"))
            {
                // Make extra sure the settings are applied.
                if (_pivotBehaviour == PivotBehaviour.Origin)
                {
                    MeshExtractorTool.Instance.CenterPivotOnLocalOrigin();
                }
                else
                {
                    MeshExtractorTool.Instance.CenterPivotOnTriangles();
                }
                MeshExtractorTool.Instance.AlignPivotRotationWith(_transformToLocalSpace);

                MeshExtractorTool.Instance.Remove(
                    _newMeshName, _replaceOldMesh, _preserveSubMeshes, _combineSubMeshesBasedOnMaterials, _combineMeshes, _saveAsObj, _extractTextures,
                    _extractBoneWeights, _extractBoneTransforms,
                    _extractBlendShapes, _replaceMeshInScene
                    );
            }

            GUI.color = new Color(0.8f, 1f, 0.8f);
            if (DrawButton("Extract"))
            {
                MeshExtractorTool.Instance.Extract(
                    _newMeshName, _replaceOldMesh, _preserveSubMeshes, _combineSubMeshesBasedOnMaterials, _combineMeshes, _saveAsObj, _extractTextures,
                    _extractBoneWeights, _extractBoneTransforms,
                    _extractBlendShapes, _replaceMeshInScene
                    );
            }
            GUI.color = col;
            GUILayout.EndHorizontal();

            _firstExtractDraw = false;
        }


#region GUI Helpers
        public static bool DrawButton(string text, string tooltip = null, string icon = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            GUIContent content;

            // After switching to light theme the button bg textures are suddenly white but ONLY until Unity is restarted
            // then the buttons turn back to black .. WTH?!?
            //if(UtilsEditor.IsLightTheme() && !string.IsNullOrEmpty(icon) && icon.StartsWith("d_"))
            //{
            //    icon = icon.Substring(2);
            //}

            // icon
            if (!string.IsNullOrEmpty(icon))
                content = EditorGUIUtility.IconContent(icon);
            else
                content = new GUIContent();

            // text
            content.text = text;

            // tooltip
            if (!string.IsNullOrEmpty(tooltip))
                content.tooltip = tooltip;

            if (style == null)
                style = new GUIStyle(GUI.skin.button);
            
            // After switching to light theme the button bg textures are suddenly white but ONLY until Unity is restarted
            // then the buttons turn back to black .. WTH?!?


            // But then the styles for "ButtonLeft", "ButtonMid", "ButtonRight" have dark text color on dark ground.
            // What the hell is going on here.
            if (UtilsEditor.IsLightTheme())
            {
                style.normal.textColor = new Color(0.1f, 0.1f, 0.1f, 1f);  
                style.hover.textColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                style.active.textColor = new Color(0.1f, 0.1f, 0.1f, 1f);   
            }

            var btn = GUILayout.Button(content, style, options);
            // GUI.color = col;

            return btn;
        }

        public static void BeginHorizontalIndent(int indentAmount = 10, bool beginVerticalInside = true)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Space(indentAmount);

            if (beginVerticalInside)
            {
                GUILayout.BeginVertical();
            }
        }

        public static void EndHorizontalIndent(float indentAmount = 10, bool begunVerticalInside = true, bool bothSides = false)
        {
            if (begunVerticalInside)
            {
                GUILayout.EndVertical();
            }

            if (bothSides)
                GUILayout.Space(indentAmount);

            GUILayout.EndHorizontal();
        }

        public static void DrawLabel(string text, string tooltip = null, Color? color = null, bool bold = false, bool wordwrap = true, bool richText = true, Texture icon = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (!color.HasValue)
                color = GUI.skin.label.normal.textColor;

            if (style == null)
                style = new GUIStyle(GUI.skin.label);
            if (bold)
                style.fontStyle = FontStyle.Bold;
            else
                style.fontStyle = FontStyle.Normal;

            style.normal.textColor = color.Value;
            style.hover.textColor = color.Value;
            style.wordWrap = wordwrap;
            style.richText = richText;
            style.imagePosition = ImagePosition.ImageLeft;

            var content = new GUIContent(text);
            if (tooltip != null)
                content.tooltip = tooltip;
            if (icon != null)
            {
                GUILayout.Space(16);
                var position = GUILayoutUtility.GetRect(content, style, options);
                GUI.DrawTexture(new Rect(position.x - 16, position.y, 16, 16), icon);
                GUI.Label(position, content, style);
            }
            else
            {
                GUILayout.Label(content, style, options);
            }
        }
#endregion
    }
}
