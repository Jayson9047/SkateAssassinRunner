#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PLAYMAKER
using HutongGames.PlayMakerEditor;
#endif

namespace SuperPosition
{
    [InitializeOnLoad]
    public class SuperPositionEditorWindow : EditorWindow, IHasCustomMenu
    {
        public static EditorWindow window;

        private GUIStyle lockButtonStyle;
        private bool locked = false, lockedPrev = false;
        private GameObject lockedObject = null;

        public static Texture2D tLogo, tLogoAlpha, tLocked,
            tBackground, tOptions, tGear;

        public static bool isInit = false;
        public static float screenWidth = 0f, screenHeight = 0f;
        public static GUIStyle style = new GUIStyle();

        public static Vector2 scrollSquareButtons, scrollWideView;
        int trueSelected = -1, selectedTab;

        [Serializable]
        public class ColorTexture
        {
            [SerializeField]
            public Texture2D texture;
            [SerializeField]
            public Color color;
        }

        [SerializeField]
        static public List<ColorTexture> colorTextures;


        [MenuItem("Window/ShrinkRay Entertainment/SuperPosition/Open SuperPosition Window")]
        public static void OpenLandmarksWindow()
        {
            window = GetWindow<SuperPositionEditorWindow>();

            if (SuperPositionEditor.data.wideView)
            {
                window.minSize = new Vector2(1, 156);
                window.maxSize = new Vector2(4000, 4000);
            }
            else
            {
                window.minSize = new Vector2(1, 156);
                window.maxSize = new Vector2(4000, 156);
            }
            //Debug.Log("Show Window");
            window.Show();
            window.titleContent.text = "SuperPosition";
        }

        public static void QueueRepaint()
        {
            if (SuperPositionEditor.data == null)
            {
                //Debug.Log("Is null");
                SuperPositionEditor.SetupSP();
                return;
            }

            if (window == null) window = GetWindow<SuperPositionEditorWindow>();

            //Debug.Log(SuperPositionEditor.data);
            //Debug.Log(SuperPositionEditor.data.wideView);

            if (SuperPositionEditor.data.wideView)
            {
                window.minSize = new Vector2(1, 156);
                window.maxSize = new Vector2(4000, 4000);
            }
            else
            {
                window.minSize = new Vector2(1, 156);
                window.maxSize = new Vector2(4000, 156);
            }
            window.titleContent.text = "SuperPosition";
            //Debug.Log("Repaint Window");
            window.Repaint();
        }

        public static void CloseWindow()
        {
            if (window != null) window.Close();
        }

        static public void Init()
        {
            isInit = true;

            if (EditorGUIUtility.isProSkin)
            {
                tLogo = Resources.Load<Texture2D>("SuperPositionLogoWhite");
            }
            else
            {
                tLogo = Resources.Load<Texture2D>("SuperPositionLogoBlack");
            }

            tLogoAlpha = Resources.Load<Texture2D>("SuperPositionLogoAlpha");
            tLocked = Resources.Load<Texture2D>("SuperPositionLocked");
            tOptions = Resources.Load<Texture2D>("SuperPositionOptions");
            tGear = Resources.Load<Texture2D>("SuperPositionGear");
        }

        static bool EditTitle = false;

        void OnGUI()
        {
            if (Application.isPlaying) return;
            if (!isInit) Init();

            if (window == null)
            {
                screenWidth = 0;
                screenHeight = 0;
                QueueRepaint();
            }
            else
            {
                screenWidth = window.position.width;
                screenHeight = window.position.height;
            }

            if (locked != lockedPrev)
            {
                if (locked) lockedObject = SuperPositionEditor.selectedObject;
                lockedPrev = locked;
            }

            if (locked) SuperPositionEditor.selectedObject = lockedObject;
            if (locked && SuperPositionEditor.selectedObject == null) locked = false;

            if (SuperPositionEditor.selectedObject != null)
            {
                var goSnapshots = SuperPositionEditor.data.states.GetGOSnapshots(SuperPositionEditor.selectedObject);
                var GOs = SuperPositionEditor.data.states.GetGO(SuperPositionEditor.selectedObject);

                tBackground = MakeTex(1, 1, new Color32(0x00, 0x00, 0x80, 255));


                void SaveTitle()
                {
                    EditTitle = false;
                    goSnapshots[GOs.selected].oldName = goSnapshots[GOs.selected].name;
                }

                void CancelTitle()
                {
                    EditTitle = false;
                    goSnapshots[GOs.selected].name = goSnapshots[GOs.selected].oldName;
                }

                if (GOs != null && GOs.isOptions)
                {
                    GUILayout.BeginArea(new Rect(0, 0, screenWidth, screenHeight));
                    {
                        GUILayout.BeginVertical();
                        {
                            if (GUILayout.Button("Back"))
                            {
                                GOs.isOptions = false;
                            }
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();

                                GUILayout.Box(tOptions, GUIStyle.none, GUILayout.Width(128), GUILayout.Height(32));
                                GUILayout.FlexibleSpace();
                            }
                            GUILayout.EndHorizontal();

                            //GUILayout.FlexibleSpace();
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.BeginVertical(GUILayout.Width(100));
                                {
                                    GUILayout.Label("This GameObject Only:");
                                    //style.fontStyle = FontStyle.Bold;
                                    //style.fontSize = 8;
                                    style = new GUIStyle();
                                    style.alignment = TextAnchor.MiddleLeft;
                                    style.padding = new RectOffset();
                                    style.normal.textColor = Color.white;
                                    string text;
                                    if (GOs.forceCompile)
                                    {
                                        text = "✔️ Force Full Recompile (Slow!)";
                                        style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    else
                                    {
                                        text = "✖️ Force Full Recompile (Slow!)";
                                        //style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    GOs.forceCompile = GUILayout.Toggle(GOs.forceCompile, new GUIContent(text, "Recompile Scripts After Restoring"), style);
                                    GUILayout.Space(4);

                                    style = new GUIStyle();
                                    style.alignment = TextAnchor.MiddleLeft;
                                    style.padding = new RectOffset();
                                    style.normal.textColor = Color.white;
                                    if (GOs.forceAwake)
                                    {
                                        text = "✔️ Run Awake() Method";
                                        style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    else
                                    {
                                        text = "✖️ Run Awake() Method";
                                        //style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    GOs.forceAwake = GUILayout.Toggle(GOs.forceAwake, new GUIContent(text, "Run each components Awake() method when restoring state"), style);
                                    GUILayout.Space(4);

                                    style = new GUIStyle();
                                    style.alignment = TextAnchor.MiddleLeft;
                                    style.padding = new RectOffset();
                                    style.normal.textColor = Color.white;
                                    if (GOs.forceOnEnable)
                                    {
                                        text = "✔️ Run OnEnable() Method";
                                        style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    else
                                    {
                                        text = "✖️ Run OnEnable() Method";
                                        //style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    GOs.forceOnEnable = GUILayout.Toggle(GOs.forceOnEnable, new GUIContent(text, "Run each components OnEnable() method when restoring state"), style);
                                    GUILayout.Space(4);

                                    style = new GUIStyle();
                                    style.alignment = TextAnchor.MiddleLeft;
                                    style.padding = new RectOffset();
                                    style.normal.textColor = Color.white;
                                    if (GOs.forceStart)
                                    {
                                        text = "✔️ Run Start() Method";
                                        style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    else
                                    {
                                        text = "✖️ Run Start() Method";
                                        //style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    GOs.forceStart = GUILayout.Toggle(GOs.forceStart, new GUIContent(text, "Run each components Start() method when restoring state"), style);
                                }
                                GUILayout.EndVertical();
                                GUILayout.Space(4);
                                GUILayout.BeginVertical();
                                {
                                    GUILayout.Label("General Options:");

                                    style = new GUIStyle();
                                    style.alignment = TextAnchor.MiddleLeft;
                                    style.padding = new RectOffset();
                                    style.normal.textColor = Color.white;
                                    string text;
                                    if (SuperPositionEditor.data.wideView)
                                    {
                                        text = "✔️ Show States as Wide List";
                                        style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    else
                                    {
                                        text = "✖️ Show States as Wide List";
                                    }
                                    var prev = SuperPositionEditor.data.wideView;
                                    SuperPositionEditor.data.wideView = GUILayout.Toggle(SuperPositionEditor.data.wideView, new GUIContent(text, "If unchecked will show as compressed Square Boxes"), style);
                                    if (prev != SuperPositionEditor.data.wideView) QueueRepaint();

                                    style = new GUIStyle();
                                    style.alignment = TextAnchor.MiddleLeft;
                                    style.padding = new RectOffset();
                                    style.normal.textColor = Color.white;
                                    if (SuperPositionEditor.data.doubleClickRestore)
                                    {
                                        text = "✔️ Doubleclick to Restore State";
                                        style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                    }
                                    else
                                    {
                                        text = "✖️ Doubleclick to Restore State";
                                    }
                                    SuperPositionEditor.data.doubleClickRestore = GUILayout.Toggle(SuperPositionEditor.data.doubleClickRestore, new GUIContent(text, "Double Click the State box to restore the state"), style);
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndHorizontal();

                        }
                        GUILayout.EndVertical();





                        GUILayout.FlexibleSpace();

                    }
                    GUILayout.EndArea();
                }
                else
                if (goSnapshots != null)
                {

                    if (GOs.selected >= goSnapshots.Count || GOs.selected < 0) GOs.selected = 0;
                    if (GOs.single) tBackground = MakeTex(1, 1, new Color32(0x80, 0x80, 0x00, 255));


                    #region TitleRibbon


                    GUILayout.BeginArea(new Rect(0, 0, screenWidth, 54f + 24f));
                    {
                        GUILayout.BeginHorizontal();
                        {
                            style = new GUIStyle();
                            style.name = "label";
                            style.alignment = TextAnchor.MiddleCenter;
                            style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x80, 0x00, 255));
                            style.normal.textColor = Color.white;
                            if (GUILayout.Button("This Object Only", style)) { GOs.single = true; }
                            style.normal.background = MakeTex(1, 1, new Color32(0x00, 0x00, 0x80, 255));
                            if (GUILayout.Button("Full Object Hierarchy", style)) { GOs.single = false; }
                        }
                        GUILayout.EndHorizontal();


                        style = new GUIStyle();
                        style.normal.background = tBackground;

                        GUILayout.BeginVertical(style);
                        {
                            GUILayout.Space(2);

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(8);

                                GUILayout.BeginVertical();
                                {
                                    if (trueSelected > -1)
                                    {
                                        style = new GUIStyle();
                                        style.name = "label";
                                        style.normal.textColor = Color.white;
                                        style.fontStyle = FontStyle.Bold;
                                        style.fontSize = 24;

                                        if (EditTitle && Event.current.type == EventType.KeyDown)
                                        {
                                            if (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return)
                                            {
                                                SaveTitle();
                                                window.Repaint();
                                            }
                                            if (Event.current.keyCode == KeyCode.Escape)
                                            {
                                                CancelTitle();
                                                window.Repaint();
                                            }
                                        }

                                        if (EditTitle)
                                        {

                                            style.name = "textfield";
                                            if (GOs.single)
                                            {
                                                style.normal.background = MakeTex(1, 1, new Color32(0x40, 0x40, 0x00, 255));
                                            }
                                            else
                                            {
                                                style.normal.background = MakeTex(1, 1, new Color32(0x00, 0x00, 0x40, 255));
                                            }
                                            style.fixedWidth = screenWidth - 80;
                                            style.clipping = TextClipping.Clip;

                                            GUILayout.BeginHorizontal();
                                            {
                                                goSnapshots[GOs.selected].name = EditorGUILayout.TextField(goSnapshots[GOs.selected].name, style, GUILayout.Height(32), GUILayout.Width(screenWidth - 80));

                                                GUILayout.Space(4);
                                                style = new GUIStyle();
                                                style.name = "";
                                                style.normal.background = MakeTex(1, 1, new Color32(255, 255, 255, 255));
                                                style.fontSize = 20;
                                                style.normal.textColor = new Color32(0x00, 0x80, 0x00, 255);
                                                style.hover.textColor = Color.white;
                                                style.alignment = TextAnchor.MiddleCenter;
                                                if (GUILayout.Button("✔️", style, GUILayout.Width(28), GUILayout.Height(28)))
                                                {
                                                    SaveTitle();
                                                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                                                }

                                                GUILayout.Space(4);
                                                style = new GUIStyle();
                                                style.name = "";
                                                style.normal.background = MakeTex(1, 1, new Color32(255, 255, 255, 255));
                                                style.fontSize = 20;
                                                style.normal.textColor = new Color32(0x80, 0x00, 0x00, 255); ;
                                                style.hover.textColor = Color.white;
                                                style.alignment = TextAnchor.MiddleCenter;
                                                if (GUILayout.Button("✖️", style, GUILayout.Width(28), GUILayout.Height(28)))
                                                {
                                                    CancelTitle();
                                                }

                                                GUILayout.FlexibleSpace();
                                            }
                                            GUILayout.EndHorizontal();
                                        }
                                        else
                                        {
                                            style.hover.textColor = Color.white;

                                            if (GUILayout.Button(goSnapshots[GOs.selected].name, style))
                                            {
                                                EditTitle = true;
                                            }
                                        }

                                        GUILayout.BeginHorizontal();
                                        {

                                            GUILayout.Space(8); style = new GUIStyle();
                                            style.normal.textColor = Color.white;
                                            style.fontStyle = FontStyle.Bold;
                                            style.fontSize = 12;
                                            GUILayout.Label("This object and", style);
                                            style = new GUIStyle();
                                            style.normal.textColor = Color.black;
                                            style.normal.background = MakeTex(1, 1, Color.white);
                                            style.fontStyle = FontStyle.Bold;
                                            style.fontSize = 12;
                                            GUILayout.Label("  " + goSnapshots[GOs.selected].totalChildCount.ToString() + " Children  ", style);
                                            style = new GUIStyle();
                                            style.normal.textColor = Color.white;
                                            style.fontStyle = FontStyle.Bold;
                                            style.fontSize = 12;
                                            GUILayout.Label("would be restored by this state...", style);
                                            GUILayout.FlexibleSpace();
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                    else
                                    {
                                        style = new GUIStyle();
                                        style.name = "label";
                                        style.normal.textColor = Color.white;
                                        style.fontStyle = FontStyle.Bold;
                                        style.fontSize = 24;
                                        GUILayout.Label("No States Saved Here...", style);
                                    }
                                }
                                GUILayout.EndVertical();

                                GUILayout.FlexibleSpace();
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(54f);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndArea();


                    float h = 84;
                    if (SuperPositionEditor.data.wideView) h = screenHeight-70;
                    
                    GUILayout.BeginArea(new Rect(0, 70f, screenWidth, h));
                    {
                        GUILayout.BeginVertical();
                        {
                            style = new GUIStyle(GUI.skin.horizontalScrollbar);
                            GUI.Box(new Rect(screenWidth - 256 - 8, 8, 256, 36), tLogoAlpha, GUIStyle.none);
                            if (locked) GUI.Box(new Rect(screenWidth - 74 - 0, 8 + 36 + 2, 74, 36), tLocked, GUIStyle.none);

                            if (!SuperPositionEditor.data.wideView) scrollSquareButtons = GUILayout.BeginScrollView(scrollSquareButtons, true, false, style, GUIStyle.none, GUILayout.Width(screenWidth));
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.BeginVertical();
                                    {

                                        style = new GUIStyle();
                                        style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                        style.fontStyle = FontStyle.Bold;
                                        style.fontSize = 32;
                                        style.alignment = TextAnchor.MiddleCenter;
                                        style.padding = new RectOffset();
                                        style.normal.textColor = Color.white;
                                        if (GUILayout.Button("+", style, GUILayout.Width(48), GUILayout.Height(48)))
                                        {
                                            CancelTitle();
                                            AssetDatabase.SaveAssets();
                                            SuperPositionEditor.data.states.NewState(SuperPositionEditor.selectedObject, GOs.single);
                                            
                                        }

                                        style = new GUIStyle("button");
                                        //style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x80, 255));
                                        //style.fontStyle = FontStyle.Bold;
                                        //style.fontSize = 8;
                                        //style.alignment = TextAnchor.MiddleCenter;
                                        //style.padding = new RectOffset();
                                        //style.normal.textColor = Color.white;
                                        //string text = "✖️ C#";
                                        //if (GOs.forceCompile) text = "✔️ C#";
                                        //GOs.forceCompile = GUILayout.Toggle(GOs.forceCompile, new GUIContent(text, "Recompile Scripts After Restoring"), style, GUILayout.Width(40),GUILayout.Height(20));
                                        if (GUILayout.Button(tGear, GUILayout.Width(44), GUILayout.Height(20)))
                                        {
                                            GOs.isOptions = true;
                                        }

                                    }
                                    GUILayout.EndVertical();
                                    GUILayout.Space(4);

                                    if (SuperPositionEditor.data.wideView) scrollWideView = GUILayout.BeginScrollView(scrollWideView, false, true, GUILayout.Width(screenWidth - 56));
                                    if (SuperPositionEditor.data.states != null)
                                    {


                                        if (goSnapshots != null)
                                        {
                                            trueSelected = -1;
                                            for (int i = goSnapshots.Count - 1; i >= 0; i--)
                                            {
                                                if (goSnapshots[i].single == GOs.single)
                                                {
                                                    if (trueSelected < 0) trueSelected = i;
                                                    int pressed = SquareButton(GOs.selected == i, goSnapshots[i].name, new DateTime(goSnapshots[i].timestamp).ToString("MMM d"), new DateTime(goSnapshots[i].timestamp).ToString("h:mm tt"));
                                                    if (pressed > 0)
                                                    {
                                                        CancelTitle();
                                                    }
                                                    if (pressed == 1)
                                                    {
                                                        GOs.selected = i;
                                                    }
                                                    if (pressed == 2)
                                                    {
                                                        //if (EditorUtility.DisplayDialog("Restore State", "Are you sure you want to restore the state called " + goSnapshots[GOs.selected].name + "?", "OK", "Cancel"))
                                                        //{

                                                        goSnapshots[GOs.selected].RebuildAll(SuperPositionEditor.selectedObject);
                                                        string specialAssets = goSnapshots[GOs.selected].RestoreAll(SuperPositionEditor.selectedObject, GOs);

#if PLAYMAKER
                                                        if (specialAssets.Contains("PLAYMAKER"))
                                                        {
                                                            FsmEditor.ReloadFSM();
                                                        }
#endif

                                                        if(specialAssets.Contains("TERRAIN"))
                                                        {
                                                                AssetDatabase.Refresh();
                                                        }

                                                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                                                        if (GOs.forceCompile) CompilationPipeline.RequestScriptCompilation();
                                                        //}
                                                    }

                                                    if (pressed == 3)
                                                    {
                                                        if (EditorUtility.DisplayDialog("Delete State", "Are you sure you want to delete the state called " + goSnapshots[GOs.selected].name + "?  This does NOT delete your GameObject, just this state.", "OK", "Cancel"))
                                                        {
                                                            goSnapshots[GOs.selected].Delete(SuperPositionEditor.selectedObject);
                                                            goSnapshots.RemoveAt(GOs.selected);
                                                            GOs.selected--;
                                                            if (GOs.selected < 0) GOs.selected = 0;
                                                            if (goSnapshots.Count == 0) SuperPositionEditor.data.states.DeleteGO(SuperPositionEditor.selectedObject);
                                                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                                                        }
                                                    }
                                                }

                                            }
                                            if (goSnapshots.Count != 0)
                                                if (goSnapshots[GOs.selected].single != GOs.single) GOs.selected = trueSelected;
                                        }
                                        //GUILayout.FlexibleSpace();
                                    }
                                    if (SuperPositionEditor.data.wideView) GUILayout.EndScrollView();
                                    GUILayout.FlexibleSpace();
                                }
                                GUILayout.EndHorizontal();
                            }
                            if (!SuperPositionEditor.data.wideView) GUILayout.EndScrollView();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndArea();
                    #endregion
                }
                else
                {
                    GUILayout.BeginArea(new Rect(0, 0, screenWidth, screenHeight));
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Box(tLogo, GUILayout.Width(screenWidth), GUILayout.Height(36));
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        if (SuperPositionEditor.selectedObject.scene.IsValid())
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("No States Saved Yet for " + SuperPositionEditor.selectedObject.name);
                                GUILayout.FlexibleSpace();
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                style = new GUIStyle();
                                style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x80, 0x00, 255));
                                style.fontStyle = FontStyle.Bold;
                                style.fontSize = 12;
                                style.alignment = TextAnchor.MiddleCenter;
                                style.padding = new RectOffset();
                                style.normal.textColor = Color.white;
                                if (GUILayout.Button("Capture Just This Object State", style, GUILayout.Width(256)))
                                {
                                    SuperPositionEditor.data.states.NewState(SuperPositionEditor.selectedObject, true);
                                }
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                                GUILayout.Space(8);
                                GUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                style = new GUIStyle();
                                style.normal.background = MakeTex(1, 1, new Color32(0x00, 0x00, 0x80, 255));
                                style.fontStyle = FontStyle.Bold;
                                style.fontSize = 12;
                                style.alignment = TextAnchor.MiddleCenter;
                                style.padding = new RectOffset();
                                style.normal.textColor = Color.white;
                                if (GUILayout.Button("Capture This and All Children States", style, GUILayout.Width(256)))
                                {

                                    SuperPositionEditor.data.states.NewState(SuperPositionEditor.selectedObject, false);
                                }
                                GUILayout.FlexibleSpace();
                            }
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Can only create States for Objects in Hierarchy");
                                GUILayout.FlexibleSpace();
                            }
                            GUILayout.EndHorizontal();

                        }




                        GUILayout.FlexibleSpace();

                    }
                    GUILayout.EndArea();
                }
            }
            else
            {
                GUILayout.BeginArea(new Rect(0, 0, screenWidth, screenHeight));
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Box(tLogo, GUILayout.Width(screenWidth), GUILayout.Height(36));
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("No Objects Selected");
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();

                }
                GUILayout.EndArea();
            }
        }

        static double clickTime;


        static int SquareButton(bool isSelected, string name, string date, string time)
        {
            int ret = 0;
            GUILayout.BeginVertical();
            {

                style = new GUIStyle();
                if (isSelected)
                    style.normal.background = tBackground;
                else
                    style.normal.background = MakeTex(1, 1, new Color32(0x80, 0x00, 0x00, 255));

                if (SuperPositionEditor.data.wideView)
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("", style, GUILayout.Height(48)))
                        {
                            ret = 1;

                            if (SuperPositionEditor.data.doubleClickRestore)
                            {
                                if (isSelected && (EditorApplication.timeSinceStartup - clickTime) < 0.3)
                                {
                                    ret = 2;
                                }
                                clickTime = EditorApplication.timeSinceStartup;
                            }
                        }

                        Rect r = GUILayoutUtility.GetLastRect();

                        style = new GUIStyle();
                        style.fontSize = 8;
                        style.normal.textColor = Color.white;
                        style.fontSize = 12;
                        GUI.Label(new Rect(r.x + 2, r.y + 2, 38f, 16f), date + "   " + time, style);
                        if (name.Length > 50) name = name.Substring(0, 47) + "...";
                        style.fontSize = 16;
                        GUI.Label(new Rect(r.x + 2, r.y + 20, 38f, 12f), name, style);

                        //GUILayout.EndVertical();
                        GUILayout.BeginVertical(GUILayout.Width(20), GUILayout.Height(48));
                        {
                            if (isSelected)
                            {

                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("✔️", GUILayout.Width(20)))
                                {
                                    ret = 2;
                                }
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("✖️", GUILayout.Width(20)))
                                {
                                    ret = 3;
                                }
                                GUILayout.FlexibleSpace();

                            }
                            else
                            {
                                //GUILayout.Space(244);
                            }
                            
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);
                    //GUILayout.BeginVertical();
                }
                else
                {
                    if (GUILayout.Button("", style, GUILayout.Width(48), GUILayout.Height(48)))
                    {
                        ret = 1;

                        if (SuperPositionEditor.data.doubleClickRestore)
                        {
                            if (isSelected && (EditorApplication.timeSinceStartup - clickTime) < 0.3)
                            {
                                ret = 2;
                            }
                            clickTime = EditorApplication.timeSinceStartup;
                        }
                    }

                    Rect r = GUILayoutUtility.GetLastRect();

                    style = new GUIStyle();
                    style.fontSize = 8;
                    style.normal.textColor = Color.white;
                    if (name.Length > 10) name = name.Substring(0, 9) + "...";
                    GUI.Label(new Rect(r.x + 2, r.y + 2, 38f, 12f), name, style);
                    style.fontSize = 12;
                    GUI.Label(new Rect(r.x + 2, r.y + 2 + 12, 38f, 16f), date, style);
                    style.fontSize = 10;
                    GUI.Label(new Rect(r.x + 2, r.y + 2 + 12 + 16, 38f, 16f), time, style);

                    if (isSelected)
                    {
                        GUILayout.BeginHorizontal(GUILayout.Width(42));
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("✔️", GUILayout.Width(20)))
                            {
                                ret = 2;
                            }
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("✖️", GUILayout.Width(20)))
                            {
                                ret = 3;
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                    }
                }


            }
            GUILayout.EndVertical();
            GUILayout.Space(4);
            return ret;
        }

        static public Texture2D MakeTex(int width, int height, Color col)
        {
            if (colorTextures == null) colorTextures = new List<ColorTexture>();
            for (int i = 0; i < colorTextures.Count; i++)
            {
                if (col == colorTextures[i].color && colorTextures[i].texture != null)
                {
                    return colorTextures[i].texture;
                }
            }
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            colorTextures.Add(new ColorTexture() { texture = result, color = col });

            return result;
        }

        private void ShowButton(Rect position)
        {
            if (this.lockButtonStyle == null)
            {
                this.lockButtonStyle = "IN LockButton";
            }
            this.locked = GUI.Toggle(position, this.locked, GUIContent.none, this.lockButtonStyle);
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Lock"), this.locked, () =>
            {
                this.locked = !this.locked;
            });
        }

    }
}
#endif