using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elroi.Kanban
{
    public class ElroiKanbanWindow : EditorWindow
    {
        private BoardData _data;

        // Tabs
        private int _tabIndex = 0; // 0 = Board, 1 = History

        // Splitter sizes (milestones + features)
        private const float SplitterWidth = 6f;

        private const float MinMsWidth = 140f;
        private const float MaxMsWidth = 520f;
        private const float MinFeatureWidth = 180f;
        private const float MaxFeatureWidth = 640f;

        private const string PrefMsWidth = "ElroiKanban_MilestoneWidth";
        private const string PrefFeatureWidth = "ElroiKanban_FeatureWidth";

        private float _milestoneWidth = 220f;
        private float _featureWidth = 300f;

        private bool _dragMsSplitter;
        private bool _dragFeatureSplitter;

        private Vector2 _msScroll;
        private Vector2 _featureScroll;
        private Vector2 _boardScroll;

        private Vector2 _scrollBacklog;
        private Vector2 _scrollInProgress;
        private Vector2 _scrollReview;
        private Vector2 _scrollDone;

        // Rename state (milestones/features)
        private string _editingMilestoneId = null;
        private string _editingMilestoneText = "";

        private string _editingFeatureId = null;
        private string _editingFeatureText = "";

        // Selection for "Let's Do These Today!"
        private readonly HashSet<string> _selectedTaskIds = new();

        // Column rename state
        private int _editingColumnIndex = -1;
        private string _editingColumnText = "";

        // Live sync
        private long _lastSeenRev = -1;

        // Focus task support
        private static string _focusMilestoneId;
        private static string _focusTaskId;

        // Drag-drop for cards
        private const string DragKeyTaskId = "ElroiKanban_DragTaskId";
        private const string DragKeyMilestoneId = "ElroiKanban_DragMilestoneId";

        // Add Task UI
        private string _newTaskTitle = "";
        private TaskType _newTaskType = TaskType.Task;
        private string _newTaskDue = "";
        private string _newTaskFeatureId = ""; // restored feature dropdown selection

        [MenuItem("Tools/Elroi/Kanban Board")]
        public static void Open()
        {
            var w = GetWindow<ElroiKanbanWindow>("Elroi Kanban");
            w.minSize = new Vector2(920, 520);
            w.Show();
        }

        public static void OpenAndFocus()
        {
            var w = GetWindow<ElroiKanbanWindow>("Elroi Kanban");
            w.minSize = new Vector2(920, 520);
            w.Show();
            w.Focus();
        }

        public static void FocusTaskOnBoard(string milestoneId, string taskId)
        {
            _focusMilestoneId = milestoneId;
            _focusTaskId = taskId;
        }

        private void OnEnable()
        {
            _data = ElroiKanbanStorage.LoadOrCreate();
            _milestoneWidth = EditorPrefs.GetFloat(PrefMsWidth, _milestoneWidth);
            _featureWidth = EditorPrefs.GetFloat(PrefFeatureWidth, _featureWidth);
            var ms = ElroiKanbanStorage.GetSelectedMilestone(_data);
            if (ms != null && string.IsNullOrWhiteSpace(_data.selectedFeatureId))
            {
                _data.selectedFeatureId = ms.features[0].id;
            }
            _newTaskDue = KanbanUtils.TodayIso();

            _lastSeenRev = ElroiKanbanStorage.SaveRevision;
            ElroiKanbanStorage.OnDataSaved += HandleExternalSave;

        }

        private void OnDisable()
        {
            ElroiKanbanStorage.OnDataSaved -= HandleExternalSave;
        }

        private void HandleExternalSave()
        {
            _data = ElroiKanbanStorage.LoadOrCreate();
            _lastSeenRev = ElroiKanbanStorage.SaveRevision;
            Repaint();
        }

        private void OnGUI()
        {
            // If another window saved, reload
            if (_lastSeenRev != ElroiKanbanStorage.SaveRevision)
            {
                _data = ElroiKanbanStorage.LoadOrCreate();
                _lastSeenRev = ElroiKanbanStorage.SaveRevision;
            }

            if (_data == null) _data = ElroiKanbanStorage.LoadOrCreate();

            HandleSplittersDrag();
            DrawTopTabsAndToolbar();

            EditorGUILayout.Space(6);

            if (_tabIndex == 0)
                DrawBoardTab();
            else
                DrawHistoryTab();

            if (GUI.changed)
                ElroiKanbanStorage.Save(_data);
        }

        private void DrawTopTabsAndToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "Elroi Kanban", "Task History" }, EditorStyles.toolbarButton, GUILayout.Width(220));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Today", EditorStyles.toolbarButton))
                    ElroiTodayWindow.OpenAndFocus();

                if (GUILayout.Button("Open JSON", EditorStyles.toolbarButton))
                    ElroiKanbanStorage.RevealFile();
            }
        }

        private void DrawBoardTab()
        {
            float leftW = _milestoneWidth;
            float midW = _featureWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMilestoneSidebar(leftW);
                DrawVerticalSplitter(ref _dragMsSplitter);

                using (new EditorGUILayout.VerticalScope())
                {
                    var milestone = ElroiKanbanStorage.GetSelectedMilestone(_data);
                    if (milestone == null) return;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawFeaturePanel(milestone, midW);
                        DrawVerticalSplitter(ref _dragFeatureSplitter);

                        using (new EditorGUILayout.VerticalScope())
                        {
                            DrawAddTaskRow(milestone);

                            EditorGUILayout.Space(6);
                            DrawBoardHeader();
                            EditorGUILayout.Space(4);

                            DrawBoard(milestone);
                        }
                    }
                }
            }

            // Auto focus request
            if (!string.IsNullOrWhiteSpace(_focusTaskId) && !string.IsNullOrWhiteSpace(_focusMilestoneId))
            {
                if (_data.selectedMilestoneId != _focusMilestoneId)
                    _data.selectedMilestoneId = _focusMilestoneId;

                Repaint();
                _focusMilestoneId = null;
                _focusTaskId = null;
            }
        }

        // ---------------- Resizable Splitters ----------------

        private void DrawVerticalSplitter(ref bool draggingFlag)
        {
            var rect = GUILayoutUtility.GetRect(SplitterWidth, 10, GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

            var c = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = c;

            var e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                draggingFlag = true;
                e.Use();
            }
        }

        private void HandleSplittersDrag()
        {
            var e = Event.current;

            if (e.type == EventType.MouseUp)
            {
                if (_dragMsSplitter || _dragFeatureSplitter)
                {
                    _dragMsSplitter = false;
                    _dragFeatureSplitter = false;
                    e.Use();

                    EditorPrefs.SetFloat(PrefMsWidth, _milestoneWidth);
                    EditorPrefs.SetFloat(PrefFeatureWidth, _featureWidth);
                }
                return;
            }

            if (e.type != EventType.MouseDrag) return;

            if (_dragMsSplitter)
            {
                _milestoneWidth = Mathf.Clamp(_milestoneWidth + e.delta.x, MinMsWidth, MaxMsWidth);
                GUI.changed = true;
                e.Use();
            }
            else if (_dragFeatureSplitter)
            {
                _featureWidth = Mathf.Clamp(_featureWidth + e.delta.x, MinFeatureWidth, MaxFeatureWidth);
                GUI.changed = true;
                e.Use();
            }
        }

        // ---------------- Milestones Sidebar ----------------

        private void DrawMilestoneSidebar(float width)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Milestones", EditorStyles.boldLabel);

                _data.milestones ??= new List<Milestone>();

                // Ensure at least one milestone exists
                if (_data.milestones.Count == 0)
                {
                    var ms0 = new Milestone
                    {
                        id = KanbanUtils.NewId(),
                        name = "Milestone 1",
                        features = new List<FeatureItem>(),
                        tasks = new List<TaskCard>()
                    };
                    ms0.features.Add(new FeatureItem
                    {
                        id = KanbanUtils.NewId(),
                        name = "General",
                        description = "",
                        createdUtcTicks = DateTime.UtcNow.Ticks,
                        updatedUtcTicks = DateTime.UtcNow.Ticks
                    });

                    _data.milestones.Add(ms0);
                    _data.selectedMilestoneId = ms0.id;
                    _data.selectedFeatureId = ms0.features[0].id;

                    GUI.changed = true;
                    ElroiKanbanStorage.Save(_data);
                }

                // Fix selection if missing/invalid
                if (string.IsNullOrWhiteSpace(_data.selectedMilestoneId) ||
                    !_data.milestones.Any(m => m.id == _data.selectedMilestoneId))
                {
                    _data.selectedMilestoneId = _data.milestones[0].id;
                    _data.selectedFeatureId = "";
                    _selectedTaskIds.Clear();
                    GUI.changed = true;
                }

                _msScroll = EditorGUILayout.BeginScrollView(_msScroll);

                foreach (var ms in _data.milestones.ToList()) // safe iteration
                {
                    var msLocal = ms;
                    var selected = (msLocal.id == _data.selectedMilestoneId);
                    var btnStyle = selected ? EditorStyles.toolbarButton : EditorStyles.miniButton;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Left: inline rename OR select button
                        if (_editingMilestoneId == msLocal.id)
                        {
                            _editingMilestoneText = EditorGUILayout.TextField(_editingMilestoneText, GUILayout.ExpandWidth(true));

                            if (GUILayout.Button("OK", GUILayout.Width(34)))
                            {
                                if (!string.IsNullOrWhiteSpace(_editingMilestoneText))
                                {
                                    msLocal.name = _editingMilestoneText.Trim();
                                    GUI.changed = true;
                                    ElroiKanbanStorage.Save(_data);
                                }

                                _editingMilestoneId = null;
                                _editingMilestoneText = "";
                                GUI.changed = true;
                            }

                            if (GUILayout.Button("X", GUILayout.Width(24)))
                            {
                                _editingMilestoneId = null;
                                _editingMilestoneText = "";
                                GUI.changed = true;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(msLocal.name, btnStyle, GUILayout.ExpandWidth(true)))
                            {
                                if (_data.selectedMilestoneId != msLocal.id)
                                {
                                    _data.selectedMilestoneId = msLocal.id;

                                    // Make sure milestone has features
                                    msLocal.features ??= new List<FeatureItem>();
                                    if (msLocal.features.Count == 0)
                                    {
                                        msLocal.features.Add(new FeatureItem
                                        {
                                            id = KanbanUtils.NewId(),
                                            name = "General",
                                            description = "",
                                            createdUtcTicks = DateTime.UtcNow.Ticks,
                                            updatedUtcTicks = DateTime.UtcNow.Ticks
                                        });
                                    }

                                    // Pick a valid feature selection
                                    _data.selectedFeatureId = msLocal.features[0].id;
                                    _newTaskFeatureId = msLocal.features[0].id;

                                    _selectedTaskIds.Clear();
                                    GUI.changed = true;
                                }
                            }

                            // Right: ⋯ menu button (NO ExitGUI, use delayCall)
                            if (GUILayout.Button("⋯", GUILayout.Width(24)))
                            {
                                var menu = new GenericMenu();

                                menu.AddItem(new GUIContent("Rename"), false, () =>
                                {
                                    RunMenuAction(() =>
                                    {
                                        _editingMilestoneId = msLocal.id;
                                        _editingMilestoneText = msLocal.name;
                                    });
                                });

                                menu.AddItem(new GUIContent("Duplicate"), false, () =>
                                {
                                    RunMenuAction(() =>
                                    {
                                        var copy = new Milestone
                                        {
                                            id = KanbanUtils.NewId(),
                                            name = msLocal.name + " (Copy)",
                                            features = (msLocal.features ?? new List<FeatureItem>())
                                                .Select(f => new FeatureItem
                                                {
                                                    id = KanbanUtils.NewId(),
                                                    name = f.name,
                                                    description = f.description,
                                                    createdUtcTicks = DateTime.UtcNow.Ticks,
                                                    updatedUtcTicks = DateTime.UtcNow.Ticks
                                                }).ToList(),
                                            tasks = new List<TaskCard>() // safer: don't clone tasks/history
                                        };

                                        if (copy.features.Count == 0)
                                        {
                                            copy.features.Add(new FeatureItem
                                            {
                                                id = KanbanUtils.NewId(),
                                                name = "General",
                                                description = "",
                                                createdUtcTicks = DateTime.UtcNow.Ticks,
                                                updatedUtcTicks = DateTime.UtcNow.Ticks
                                            });
                                        }

                                        _data.milestones.Add(copy);
                                        _data.selectedMilestoneId = copy.id;
                                        _data.selectedFeatureId = copy.features[0].id;
                                        _newTaskFeatureId = copy.features[0].id;
                                        _selectedTaskIds.Clear();

                                        GUI.changed = true;
                                    });
                                });

                                if (_data.milestones.Count <= 1)
                                {
                                    menu.AddDisabledItem(new GUIContent("Delete"));
                                }
                                else
                                {
                                    menu.AddItem(new GUIContent("Delete"), false, () =>
                                    {
                                        RunMenuAction(() =>
                                        {
                                            // Re-find by id in case list changed
                                            var target = _data.milestones.FirstOrDefault(m => m.id == msLocal.id);
                                            if (target == null) return;

                                            if (!EditorUtility.DisplayDialog(
                                                    "Delete milestone?",
                                                    $"Delete milestone '{target.name}'?\n\nThis will also delete ALL tasks and features inside it (including archived history).",
                                                    "Delete",
                                                    "Cancel"))
                                                return;

                                            // Remove Today items for this milestone
                                            _data.today?.items?.RemoveAll(i => i.milestoneId == target.id);

                                            // Cancel rename if needed
                                            if (_editingMilestoneId == target.id)
                                            {
                                                _editingMilestoneId = null;
                                                _editingMilestoneText = "";
                                            }

                                            _data.milestones.Remove(target);

                                            // Fix selection
                                            var newSel = _data.milestones[0];
                                            _data.selectedMilestoneId = newSel.id;

                                            newSel.features ??= new List<FeatureItem>();
                                            if (newSel.features.Count == 0)
                                            {
                                                newSel.features.Add(new FeatureItem
                                                {
                                                    id = KanbanUtils.NewId(),
                                                    name = "General",
                                                    description = "",
                                                    createdUtcTicks = DateTime.UtcNow.Ticks,
                                                    updatedUtcTicks = DateTime.UtcNow.Ticks
                                                });
                                            }

                                            _data.selectedFeatureId = newSel.features[0].id;
                                            _newTaskFeatureId = newSel.features[0].id;

                                            _selectedTaskIds.Clear();

                                            GUI.changed = true;
                                        });
                                    });
                                }

                                menu.ShowAsContext();
                            }
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(6);

                if (GUILayout.Button("+ Milestone"))
                {
                    var ms = new Milestone
                    {
                        id = KanbanUtils.NewId(),
                        name = $"Milestone {_data.milestones.Count + 1}",
                        features = new List<FeatureItem>(),
                        tasks = new List<TaskCard>()
                    };

                    ms.features.Add(new FeatureItem
                    {
                        id = KanbanUtils.NewId(),
                        name = "General",
                        description = "",
                        createdUtcTicks = DateTime.UtcNow.Ticks,
                        updatedUtcTicks = DateTime.UtcNow.Ticks
                    });

                    _data.milestones.Add(ms);
                    _data.selectedMilestoneId = ms.id;
                    _data.selectedFeatureId = ms.features[0].id;
                    _newTaskFeatureId = ms.features[0].id;
                    _selectedTaskIds.Clear();

                    GUI.changed = true;
                    ElroiKanbanStorage.Save(_data);
                }
            }
        }


        // ---------------- Features Panel ----------------

        private void DrawFeaturePanel(Milestone milestone, float width)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Features", EditorStyles.boldLabel);

                milestone.features ??= new List<FeatureItem>();
                milestone.tasks ??= new List<TaskCard>();

                // Ensure there's always at least one feature
                if (milestone.features.Count == 0)
                {
                    milestone.features.Add(new FeatureItem
                    {
                        id = KanbanUtils.NewId(),
                        name = "General",
                        description = "",
                        createdUtcTicks = DateTime.UtcNow.Ticks,
                        updatedUtcTicks = DateTime.UtcNow.Ticks
                    });
                    GUI.changed = true;
                    ElroiKanbanStorage.Save(_data);
                }

                // If selection is empty/invalid, select first feature
                if (string.IsNullOrWhiteSpace(_data.selectedFeatureId) ||
                    !milestone.features.Any(f => f.id == _data.selectedFeatureId))
                {
                    _data.selectedFeatureId = milestone.features[0].id;
                    GUI.changed = true;
                }

                _featureScroll = EditorGUILayout.BeginScrollView(_featureScroll);

                foreach (var f in milestone.features.ToList()) // safe iteration
                {
                    var fLocal = f;
                    var selected = (_data.selectedFeatureId == fLocal.id);
                    var style = selected ? EditorStyles.toolbarButton : EditorStyles.miniButton;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Left: inline rename OR select button
                        if (_editingFeatureId == fLocal.id)
                        {
                            _editingFeatureText = EditorGUILayout.TextField(_editingFeatureText, GUILayout.ExpandWidth(true));

                            if (GUILayout.Button("OK", GUILayout.Width(34)))
                            {
                                // Rename is safe inline (in normal IMGUI flow)
                                if (!string.IsNullOrWhiteSpace(_editingFeatureText))
                                {
                                    fLocal.name = _editingFeatureText.Trim();
                                    fLocal.updatedUtcTicks = DateTime.UtcNow.Ticks;
                                    GUI.changed = true;
                                    ElroiKanbanStorage.Save(_data);
                                }

                                _editingFeatureId = null;
                                _editingFeatureText = "";
                                GUI.changed = true;
                            }

                            if (GUILayout.Button("X", GUILayout.Width(24)))
                            {
                                _editingFeatureId = null;
                                _editingFeatureText = "";
                                GUI.changed = true;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(fLocal.name, style, GUILayout.ExpandWidth(true)))
                            {
                                _data.selectedFeatureId = fLocal.id;
                                GUI.changed = true;
                            }
                            var milestoneId = milestone.id;
                            var featureId = fLocal.id;
                            // Right: ⋯ menu button (NO ExitGUI, use delayCall)
                            if (GUILayout.Button("⋯", GUILayout.Width(24)))
                            {
                                var menu = new GenericMenu();

                                menu.AddItem(new GUIContent("Rename"), false, () =>
                                {
                                    RunMenuAction(() =>
                                    {
                                        _editingFeatureId = fLocal.id;
                                        _editingFeatureText = fLocal.name;
                                    });
                                });

                                menu.AddItem(new GUIContent("Duplicate"), false, () =>
                                {
                                    RunMenuAction(() =>
                                    {
                                        var ms = _data.milestones.FirstOrDefault(m => m.id == milestoneId);
                                        if (ms == null) return;

                                        var src = ms.features.FirstOrDefault(x => x.id == featureId);
                                        if (src == null) return;

                                        ms.features ??= new List<FeatureItem>();

                                        var nf = new FeatureItem
                                        {
                                            id = KanbanUtils.NewId(),
                                            name = src.name + " (Copy)",
                                            description = src.description,
                                            createdUtcTicks = DateTime.UtcNow.Ticks,
                                            updatedUtcTicks = DateTime.UtcNow.Ticks
                                        };

                                        ms.features.Add(nf);

                                        GUI.changed = true;
                                    });
                                });

                                if (milestone.features.Count <= 1)
                                {
                                    menu.AddDisabledItem(new GUIContent("Delete"));
                                }
                                else
                                {
                                    menu.AddItem(new GUIContent("Delete"), false, () =>
                                    {
                                        RunMenuAction(() =>
                                        {
                                            var ms = _data.milestones.FirstOrDefault(m => m.id == milestoneId);
                                            if (ms == null) return;

                                            ms.features ??= new List<FeatureItem>();
                                            ms.tasks ??= new List<TaskCard>();

                                            var target = ms.features.FirstOrDefault(x => x.id == featureId);
                                            if (target == null) return;

                                            if (ms.features.Count <= 1) return; // safety

                                            if (!EditorUtility.DisplayDialog(
                                                    "Delete feature?",
                                                    $"Delete feature '{target.name}'?\n\nAll tasks under it will be moved to the first remaining feature.",
                                                    "Delete",
                                                    "Cancel"))
                                                return;

                                            var fallback = ms.features.First(x => x.id != target.id);

                                            // Re-home ALL tasks (including archived)
                                            foreach (var t in ms.tasks)
                                            {
                                                if (t.featureId == target.id)
                                                    t.featureId = fallback.id;
                                            }

                                            // Fix selections
                                            if (_data.selectedFeatureId == target.id)
                                                _data.selectedFeatureId = fallback.id;

                                            // Fix add-row dropdown
                                            if (_newTaskFeatureId == target.id)
                                                _newTaskFeatureId = fallback.id;

                                            // Cancel rename if needed
                                            if (_editingFeatureId == target.id)
                                            {
                                                _editingFeatureId = null;
                                                _editingFeatureText = "";
                                            }

                                            ms.features.Remove(target);

                                            GUI.changed = true;
                                        });
                                    });

                                }

                                menu.ShowAsContext();
                            }
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(6);

                if (GUILayout.Button("+ Feature"))
                {
                    var nf = new FeatureItem
                    {
                        id = KanbanUtils.NewId(),
                        name = $"Feature {milestone.features.Count + 1}",
                        description = "",
                        createdUtcTicks = DateTime.UtcNow.Ticks,
                        updatedUtcTicks = DateTime.UtcNow.Ticks
                    };

                    milestone.features.Add(nf);
                    _data.selectedFeatureId = nf.id;
                    _newTaskFeatureId = nf.id;

                    GUI.changed = true;
                    ElroiKanbanStorage.Save(_data);
                }
            }
        }



        // Safe GenericMenu execution
        private void RunMenuAction(Action action)
        {
            // GenericMenu callbacks are outside the IMGUI flow; delayCall makes mutations safe.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    action?.Invoke();
                    ElroiKanbanStorage.Save(_data);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    Repaint();
                }
            };
        }

        // ---------------- Add Task Row (FIXED) ----------------

        private void DrawAddTaskRow(Milestone milestone)
        {
            milestone.features ??= new List<FeatureItem>();
            if (milestone.features.Count == 0)
            {
                milestone.features.Add(new FeatureItem
                {
                    id = KanbanUtils.NewId(),
                    name = "General",
                    description = "",
                    createdUtcTicks = DateTime.UtcNow.Ticks,
                    updatedUtcTicks = DateTime.UtcNow.Ticks
                });
            }

            // keep feature dropdown selection stable + default smartly
            if (string.IsNullOrWhiteSpace(_newTaskFeatureId) || !milestone.features.Any(f => f.id == _newTaskFeatureId))
            {
                // prefer currently selected feature, otherwise first
                _newTaskFeatureId = !string.IsNullOrWhiteSpace(_data.selectedFeatureId)
                    ? _data.selectedFeatureId
                    : milestone.features[0].id;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Add Task", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _newTaskTitle = EditorGUILayout.TextField(_newTaskTitle, GUILayout.MinWidth(220));

                    _newTaskType = (TaskType)EditorGUILayout.EnumPopup(_newTaskType, GUILayout.Width(90));

                    // FEATURE DROPDOWN (restored beside date, like you asked)
                    var featureNames = milestone.features.Select(f => f.name).ToArray();
                    var featureIds = milestone.features.Select(f => f.id).ToArray();
                    int fIdx = Array.IndexOf(featureIds, _newTaskFeatureId);
                    if (fIdx < 0) fIdx = 0;

                    int newFIdx = EditorGUILayout.Popup(fIdx, featureNames, GUILayout.Width(160));
                    _newTaskFeatureId = featureIds[Mathf.Clamp(newFIdx, 0, featureIds.Length - 1)];

                    _newTaskDue = EditorGUILayout.TextField(_newTaskDue, GUILayout.Width(110));

                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newTaskTitle)))
                    {
                        if (GUILayout.Button("Add to Backlog", GUILayout.Width(130)))
                        {
                            milestone.tasks ??= new List<TaskCard>();

                            milestone.tasks.Add(new TaskCard
                            {
                                id = KanbanUtils.NewId(),
                                featureId = _newTaskFeatureId,
                                title = _newTaskTitle.Trim(),
                                description = "",
                                dueDateIso = string.IsNullOrWhiteSpace(_newTaskDue) ? KanbanUtils.TodayIso() : _newTaskDue.Trim(),
                                type = _newTaskType,
                                status = TaskStatus.Backlog,
                                createdUtcTicks = DateTime.UtcNow.Ticks,
                                updatedUtcTicks = DateTime.UtcNow.Ticks,
                                isArchived = false,
                                archivedUtcTicks = 0
                            });

                            _newTaskTitle = "";
                            _selectedTaskIds.Clear();
                            _data.selectedFeatureId = _newTaskFeatureId;
                            // IMMEDIATE SAVE so it never "does nothing"
                            ElroiKanbanStorage.Save(_data);

                            GUI.FocusControl(null);
                            Repaint();
                        }
                    }
                }

                GUILayout.Label("Date format: yyyy-MM-dd. Feature dropdown sets the new task’s feature.", EditorStyles.miniLabel);
            }
        }

        // ---------------- Board Header ----------------

        private void DrawBoardHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Board", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_selectedTaskIds.Count == 0))
                {
                    if (GUILayout.Button("Let's Do These Today!", GUILayout.Width(170)))
                    {
                        AddSelectedToTodayAndMoveInProgress();
                        ElroiTodayWindow.OpenAndFocus();
                        ElroiKanbanStorage.Save(_data);
                        GUI.changed = true;
                    }
                }
            }
        }

        private void AddSelectedToTodayAndMoveInProgress()
        {
            var milestone = ElroiKanbanStorage.GetSelectedMilestone(_data);
            if (milestone == null) return;

            _data.today ??= new TodayList { dateIso = KanbanUtils.TodayIso() };
            _data.today.items ??= new List<TodayItem>();
            if (_data.today.dateIso != KanbanUtils.TodayIso())
                _data.today.dateIso = KanbanUtils.TodayIso();

            foreach (var id in _selectedTaskIds.ToArray())
            {
                var card = milestone.tasks.FirstOrDefault(t => t.id == id);
                if (card == null || card.isArchived) continue;

                if (card.status != TaskStatus.InProgress && card.status != TaskStatus.Done)
                    card.status = TaskStatus.InProgress;

                if (!_data.today.items.Any(i => i.milestoneId == milestone.id && i.taskId == id))
                {
                    _data.today.items.Add(new TodayItem
                    {
                        milestoneId = milestone.id,
                        taskId = id,
                        done = false,
                        addedUtcTicks = DateTime.UtcNow.Ticks
                    });
                }
            }
        }

        // ---------------- Board Rendering ----------------

        private void DrawBoard(Milestone milestone)
        {
            milestone.tasks ??= new List<TaskCard>();
            IEnumerable<TaskCard> tasks = milestone.tasks.Where(t => !t.isArchived);
            if (!string.IsNullOrWhiteSpace(_data.selectedFeatureId))
                tasks = tasks.Where(t => t.featureId == _data.selectedFeatureId);

            var backlog = tasks.Where(t => t.status == TaskStatus.Backlog).ToList();
            var inprog = tasks.Where(t => t.status == TaskStatus.InProgress).ToList();
            var review = tasks.Where(t => t.status == TaskStatus.Review).ToList();
            var done = tasks.Where(t => t.status == TaskStatus.Done).ToList();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_boardScroll))
            {
                _boardScroll = scroll.scrollPosition;

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawColumn(milestone, TaskStatus.Backlog, backlog);
                    DrawColumn(milestone, TaskStatus.InProgress, inprog);
                    DrawColumn(milestone, TaskStatus.Review, review);
                    DrawColumn(milestone, TaskStatus.Done, done);
                }
            }
        }

        private string GetColumnName(TaskStatus s)
        {
            _data.columnNames ??= new List<string> { "Backlog", "In Progress", "Review", "Done" };
            if (_data.columnNames.Count != 4)
            {
                _data.columnNames.Clear();
                _data.columnNames.AddRange(new[] { "Backlog", "In Progress", "Review", "Done" });
            }

            return s switch
            {
                TaskStatus.Backlog => _data.columnNames[0],
                TaskStatus.InProgress => _data.columnNames[1],
                TaskStatus.Review => _data.columnNames[2],
                TaskStatus.Done => _data.columnNames[3],
                _ => s.ToString()
            };
        }

        private void DrawColumn(Milestone milestone, TaskStatus status, List<TaskCard> cards)
        {
            using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandWidth(true)))
            {
                DrawColumnHeaderEditable(status);

                // Pick the correct scroll var
                switch (status)
                {
                    case TaskStatus.Backlog:
                        _scrollBacklog = EditorGUILayout.BeginScrollView(_scrollBacklog, GUILayout.ExpandHeight(true));
                        break;
                    case TaskStatus.InProgress:
                        _scrollInProgress = EditorGUILayout.BeginScrollView(_scrollInProgress, GUILayout.ExpandHeight(true));
                        break;
                    case TaskStatus.Review:
                        _scrollReview = EditorGUILayout.BeginScrollView(_scrollReview, GUILayout.ExpandHeight(true));
                        break;
                    case TaskStatus.Done:
                        _scrollDone = EditorGUILayout.BeginScrollView(_scrollDone, GUILayout.ExpandHeight(true));
                        break;
                }

                // Draw cards using layout rects (stable)
                foreach (var c in cards.OrderBy(t => t.dueDateIso))
                {
                    float h = 92f;
                    var r = GUILayoutUtility.GetRect(10, h, GUILayout.ExpandWidth(true));
                    DrawCard(milestone, c, r);
                    GUILayout.Space(6);
                }

                EditorGUILayout.EndScrollView();

                // Make the whole scroll viewport a drop target (no layout scattering)
                var scrollViewportRect = GUILayoutUtility.GetLastRect();
                HandleDropArea(milestone, status, scrollViewportRect);
            }
        }



        private void DrawColumnHeaderEditable(TaskStatus status)
        {
            int idx = status switch
            {
                TaskStatus.Backlog => 0,
                TaskStatus.InProgress => 1,
                TaskStatus.Review => 2,
                TaskStatus.Done => 3,
                _ => 0
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_editingColumnIndex == idx)
                {
                    _editingColumnText = EditorGUILayout.TextField(_editingColumnText);
                    if (GUILayout.Button("OK", GUILayout.Width(40)))
                    {
                        _data.columnNames[idx] = string.IsNullOrWhiteSpace(_editingColumnText)
                            ? _data.columnNames[idx]
                            : _editingColumnText.Trim();

                        _editingColumnIndex = -1;
                        _editingColumnText = "";
                        GUI.changed = true;
                    }

                    if (GUILayout.Button("X", GUILayout.Width(26)))
                    {
                        _editingColumnIndex = -1;
                        _editingColumnText = "";
                        GUI.changed = true;
                    }
                }
                else
                {
                    GUILayout.Label(GetColumnName(status), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("✎", GUILayout.Width(26)))
                    {
                        _editingColumnIndex = idx;
                        _editingColumnText = _data.columnNames[idx];
                        GUI.changed = true;
                    }
                }
            }
        }

        private float GetCardHeight(float width)
        {
            bool compact = width < 260f;
            return compact ? 106f : 88f;
        }

        private void DrawCard(Milestone milestone, TaskCard card, Rect rect)
        {
            bool compact = rect.width < 260f;

            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            var pad = new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12);

            var chkRect = new Rect(pad.x, pad.y, 16, 16);
            bool selected = _selectedTaskIds.Contains(card.id);
            bool newSelected = GUI.Toggle(chkRect, selected, GUIContent.none);
            if (newSelected != selected)
            {
                if (newSelected) _selectedTaskIds.Add(card.id);
                else _selectedTaskIds.Remove(card.id);
                GUI.changed = true;
            }

            var dueText = card.dueDateIso ?? "";
            var dueContent = new GUIContent(dueText);
            var dueSize = EditorStyles.miniLabel.CalcSize(dueContent);
            float dueW = Mathf.Min(110f, dueSize.x + 8f);

            var titleX = chkRect.xMax + 6;
            var titleRightLimit = pad.xMax - (compact ? 0 : (dueW + 4));
            var titleRect = new Rect(titleX, pad.y, Mathf.Max(60, titleRightLimit - titleX), 18);

            var dueRect = new Rect(pad.xMax - dueW, pad.y, dueW, 18);

            var feature = ElroiKanbanStorage.FindFeature(_data, milestone.id, card.featureId);
            var featureName = feature?.name ?? "-";
            var featureRect = new Rect(titleX, pad.y + 18, pad.width - (titleX - pad.x), 16);

            var titleStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            if (GUI.Button(titleRect, card.title ?? "Untitled", titleStyle))
                ElroiTaskDetailsWindow.Open(milestone.id, card.id);

            if (!compact)
                GUI.Label(dueRect, dueText, EditorStyles.miniLabel);

            GUI.Label(featureRect, $"Feature: {featureName}", EditorStyles.miniLabel);
            // Move type label up so it never collides with buttons
            var typeRect = new Rect(titleX, pad.y + 34, pad.width - (titleX - pad.x), 16);
            GUI.Label(typeRect, card.type == TaskType.Bug ? "🐞 Bug" : "📋 Task", EditorStyles.miniLabel);

            if (!compact)
            {
                var footerY = pad.y + 54;

                float btnH = 18f;
                float gap = 4f;

                float btnDetailsW = 64f;
                float btnNextW = 86f;
                float btnLastW = 70f;

                float packW = btnDetailsW + gap + btnNextW + gap + btnLastW;
                float startX = pad.xMax - packW;

                float leftMaxX = startX - 6f; // stop before buttons

                var btnDetails = new Rect(startX, footerY, btnDetailsW, btnH);
                var btnNext = new Rect(btnDetails.xMax + gap, footerY, btnNextW, btnH);
                var btnLast = new Rect(btnNext.xMax + gap, footerY, btnLastW, btnH);

                if (GUI.Button(btnDetails, "Details"))
                    ElroiTaskDetailsWindow.Open(milestone.id, card.id);

                using (new EditorGUI.DisabledScope(card.status == TaskStatus.Done))
                {
                    if (GUI.Button(btnNext, "Move Next"))
                    {
                        card.status = KanbanUtils.Next(card.status);
                        card.updatedUtcTicks = DateTime.UtcNow.Ticks;
                        GUI.changed = true;
                    }
                }

                if (card.status == TaskStatus.Done)
                {
                    if (GUI.Button(btnLast, "Archive"))
                        ArchiveTask(milestone, card);
                }
                else
                {
                    if (GUI.Button(btnLast, "Delete"))
                        DeleteTask(milestone, card);
                }
            }
            else
            {
                var dueInlineRect = new Rect(titleX, pad.y + 34, pad.width - (titleX - pad.x), 16);
                GUI.Label(dueInlineRect, $"Due: {dueText}    •    {card.type}", EditorStyles.miniLabel);

                float btnH = 18f;
                float footerY = pad.y + 58;

                string nextLabel = "Next";
                float w = pad.width - (titleX - pad.x);
                float btnW = Mathf.Floor((w - 8) / 3f);

                var btnDetails = new Rect(titleX, footerY, btnW, btnH);
                var btnNext = new Rect(btnDetails.xMax + 4, footerY, btnW, btnH);
                var btnLast = new Rect(btnNext.xMax + 4, footerY, btnW, btnH);

                if (GUI.Button(btnDetails, "Details"))
                    ElroiTaskDetailsWindow.Open(milestone.id, card.id);

                using (new EditorGUI.DisabledScope(card.status == TaskStatus.Done))
                {
                    if (GUI.Button(btnNext, nextLabel))
                    {
                        card.status = KanbanUtils.Next(card.status);
                        card.updatedUtcTicks = DateTime.UtcNow.Ticks;
                        GUI.changed = true;
                    }
                }

                if (card.status == TaskStatus.Done)
                {
                    if (GUI.Button(btnLast, "Archive"))
                        ArchiveTask(milestone, card);
                }
                else
                {
                    if (GUI.Button(btnLast, "Delete"))
                        DeleteTask(milestone, card);
                }
            }

            HandleCardDrag(card, rect);
        }

        private void ArchiveTask(Milestone milestone, TaskCard card)
        {
            if (EditorUtility.DisplayDialog("Archive task?", $"Archive:\n\n{card.title}", "Archive", "Cancel"))
            {
                card.isArchived = true;
                card.archivedUtcTicks = DateTime.UtcNow.Ticks;
                card.updatedUtcTicks = DateTime.UtcNow.Ticks;

                _data.today?.items?.RemoveAll(i => i.milestoneId == milestone.id && i.taskId == card.id);

                _selectedTaskIds.Remove(card.id);
                GUI.changed = true;
            }
        }

        private void DeleteTask(Milestone milestone, TaskCard card)
        {
            if (EditorUtility.DisplayDialog("Delete task?", $"Delete:\n\n{card.title}", "Delete", "Cancel"))
            {
                milestone.tasks.Remove(card);
                _data.today?.items?.RemoveAll(i => i.milestoneId == milestone.id && i.taskId == card.id);
                _selectedTaskIds.Remove(card.id);
                GUI.changed = true;
            }
        }

        private void HandleCardDrag(TaskCard card, Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(DragKeyTaskId, card.id);
                DragAndDrop.SetGenericData(DragKeyMilestoneId, ElroiKanbanStorage.GetSelectedMilestone(_data).id);
                DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                DragAndDrop.StartDrag($"Task:{card.title}");
                e.Use();
            }
        }

        private void HandleDropArea(Milestone milestone, TaskStatus targetStatus, Rect dropRect)
        {
            var e = Event.current;
            if (!dropRect.Contains(e.mousePosition)) return;

            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                var draggedId = DragAndDrop.GetGenericData(DragKeyTaskId) as string;
                var msId = DragAndDrop.GetGenericData(DragKeyMilestoneId) as string;

                if (!string.IsNullOrEmpty(draggedId) && msId == milestone.id)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        var card = milestone.tasks.FirstOrDefault(t => t.id == draggedId);
                        if (card != null && !card.isArchived)
                        {
                            card.status = targetStatus;
                            card.updatedUtcTicks = DateTime.UtcNow.Ticks;
                            GUI.changed = true;
                        }

                        DragAndDrop.SetGenericData(DragKeyTaskId, null);
                        DragAndDrop.SetGenericData(DragKeyMilestoneId, null);
                    }

                    e.Use();
                }
            }
        }

        // ---------------- History Tab ----------------

        private Vector2 _historyScroll;

        private void DrawHistoryTab()
        {
            if (_data == null) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox("Archived tasks live here. Restore sends the task back to the SAME milestone/feature, and places it in Backlog.", MessageType.Info);
            }

            _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll);

            foreach (var ms in _data.milestones)
            {
                var archivedInMs = ms.tasks.Where(t => t.isArchived).ToList();
                if (archivedInMs.Count == 0) continue;

                EditorGUILayout.Space(6);
                GUILayout.Label(ms.name, EditorStyles.boldLabel);

                foreach (var featureGroup in archivedInMs.GroupBy(t => t.featureId))
                {
                    var f = ms.features.FirstOrDefault(x => x.id == featureGroup.Key);
                    var fname = f?.name ?? "Unknown Feature";

                    using (new EditorGUILayout.VerticalScope("helpbox"))
                    {
                        GUILayout.Label(fname, EditorStyles.boldLabel);

                        foreach (var t in featureGroup.OrderByDescending(x => x.archivedUtcTicks))
                            DrawArchivedRow(ms, t);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawArchivedRow(Milestone ms, TaskCard t)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(t.title ?? "Untitled", GUILayout.ExpandWidth(true));
                GUILayout.Label($"({t.dueDateIso})", EditorStyles.miniLabel, GUILayout.Width(90));

                if (GUILayout.Button("Details", GUILayout.Width(60)))
                    ElroiTaskDetailsWindow.Open(ms.id, t.id);

                if (GUILayout.Button("Restore", GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog("Restore task?", $"Restore:\n\n{t.title}\n\nIt will return to Backlog in the same milestone/feature.", "Restore", "Cancel"))
                    {
                        t.isArchived = false;
                        t.archivedUtcTicks = 0;
                        t.status = TaskStatus.Backlog;
                        t.updatedUtcTicks = DateTime.UtcNow.Ticks;
                        GUI.changed = true;
                    }
                }

                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog(
                        "PERMANENT DELETE?",
                        $"This will permanently delete the archived task:\n\n{t.title}\n\nThis cannot be undone.",
                        "Delete Forever",
                        "Cancel"))
                    {
                        ms.tasks.Remove(t);
                        _data.today?.items?.RemoveAll(i => i.milestoneId == ms.id && i.taskId == t.id);
                        GUI.changed = true;
                    }
                }
            }
        }
    }
}
