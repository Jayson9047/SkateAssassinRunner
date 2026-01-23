using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elroi.Kanban
{
    public class ElroiTodayWindow : EditorWindow
    {
        private BoardData _data;
        private Vector2 _scroll;
        private long _lastSeenRev = -1;

        private static GUIStyle _titleStyle;
        private static GUIStyle _titleStyleDone;

        [MenuItem("Tools/Elroi/Today (Sticky)")]
        public static void Open()
        {
            var w = GetWindow<ElroiTodayWindow>("Today");
            w.minSize = new Vector2(420, 380);
            w.ShowUtility();
        }

        public static void OpenAndFocus()
        {
            var w = GetWindow<ElroiTodayWindow>("Today");
            w.minSize = new Vector2(420, 380);
            w.ShowUtility();
            w.Focus();
        }

        private void OnEnable()
        {
            _data = ElroiKanbanStorage.LoadOrCreate();
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

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.label) { wordWrap = false };
                _titleStyleDone = new GUIStyle(EditorStyles.label) { wordWrap = false };
                _titleStyleDone.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            // If another window saved, reload
            if (_lastSeenRev != ElroiKanbanStorage.SaveRevision)
            {
                _data = ElroiKanbanStorage.LoadOrCreate();
                _lastSeenRev = ElroiKanbanStorage.SaveRevision;
            }

            if (_data == null) _data = ElroiKanbanStorage.LoadOrCreate();

            var todayIso = KanbanUtils.TodayIso();
            if (_data.today == null) _data.today = new TodayList();
            _data.today.items ??= new System.Collections.Generic.List<TodayItem>();
            if (_data.today.dateIso != todayIso) _data.today.dateIso = todayIso;

            // Remove missing/archived
            _data.today.items.RemoveAll(i =>
            {
                var t = ElroiKanbanStorage.FindTask(_data, i.milestoneId, i.taskId);
                return t == null || t.isArchived;
            });

            // Ensure Today tasks are InProgress unless Done/Archived
            foreach (var i in _data.today.items)
            {
                var t = ElroiKanbanStorage.FindTask(_data, i.milestoneId, i.taskId);
                if (t != null && !t.isArchived && t.status != TaskStatus.Done && t.status != TaskStatus.InProgress)
                    t.status = TaskStatus.InProgress;
            }

            DrawHeader();
            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_data.today.items.Count == 0)
            {
                EditorGUILayout.HelpBox("No tasks picked for today. Select cards in Kanban and hit “Let’s Do These Today!”.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _data.today.items.Count; i++)
                {
                    DrawTodayItemRow(_data.today.items[i], i);
                    GUILayout.Space(4);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            DrawFooter();

            if (GUI.changed)
                ElroiKanbanStorage.Save(_data);
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"Today — {_data.today.dateIso}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Kanban", EditorStyles.toolbarButton))
                    ElroiKanbanWindow.OpenAndFocus();

                if (GUILayout.Button("Open JSON", EditorStyles.toolbarButton))
                    ElroiKanbanStorage.RevealFile();
            }
        }

        private void DrawTodayItemRow(TodayItem item, int index)
        {
            var task = ElroiKanbanStorage.FindTask(_data, item.milestoneId, item.taskId);
            if (task == null || task.isArchived) return;

            var ms = _data.milestones.FirstOrDefault(m => m.id == item.milestoneId);
            var feature = ElroiKanbanStorage.FindFeature(_data, item.milestoneId, task.featureId);

            using (new EditorGUILayout.VerticalScope("helpbox"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var doneNew = GUILayout.Toggle(item.done, "", GUILayout.Width(18));
                    if (doneNew != item.done)
                    {
                        item.done = doneNew;
                        GUI.changed = true;
                    }

                    var titleRect = GUILayoutUtility.GetRect(
                        new GUIContent(task.title),
                        item.done ? _titleStyleDone : _titleStyle,
                        GUILayout.ExpandWidth(true)
                    );

                    GUI.Label(titleRect, task.title, item.done ? _titleStyleDone : _titleStyle);

                    if (item.done)
                    {
                        var y = titleRect.y + titleRect.height * 0.55f;
                        Handles.BeginGUI();
                        Handles.color = new Color(1f, 1f, 1f, 0.55f);
                        Handles.DrawLine(new Vector3(titleRect.x, y), new Vector3(titleRect.xMax, y));
                        Handles.EndGUI();
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("View", GUILayout.Width(60)))
                        ElroiTaskDetailsWindow.Open(item.milestoneId, item.taskId);

                    if (GUILayout.Button("Board", GUILayout.Width(60)))
                    {
                        ElroiKanbanWindow.OpenAndFocus();
                        ElroiKanbanWindow.FocusTaskOnBoard(item.milestoneId, item.taskId);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(28)))
                    {
                        // If marked done, move to final column, but do NOT archive yet.
                        if (item.done)
                        {
                            task.status = TaskStatus.Done;
                            task.updatedUtcTicks = DateTime.UtcNow.Ticks;
                        }

                        _data.today.items.RemoveAt(index);
                        GUI.changed = true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Milestone: {(ms?.name ?? "-")}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Feature: {(feature?.name ?? "-")}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Due: {task.dueDateIso}", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                if (GUILayout.Button("Clear completed", GUILayout.Width(130)))
                {
                    _data.today.items.RemoveAll(i => i.done);
                    GUI.changed = true;
                }

                if (GUILayout.Button("Clear all", GUILayout.Width(90)))
                {
                    if (EditorUtility.DisplayDialog("Clear today list?", "Remove all tasks from Today list?", "Clear", "Cancel"))
                    {
                        _data.today.items.Clear();
                        GUI.changed = true;
                    }
                }

                GUILayout.FlexibleSpace();

                var total = _data.today.items.Count;
                var done = _data.today.items.Count(i => i.done);
                GUILayout.Label($"{done}/{total} done", EditorStyles.miniLabel);
            }
        }
    }
}
