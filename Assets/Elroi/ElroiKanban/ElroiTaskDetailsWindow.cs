using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elroi.Kanban
{
    public class ElroiTaskDetailsWindow : EditorWindow
    {
        private const string ImportFolder = "Assets/ElroiKanbanImports";

        private BoardData _data;
        private string _milestoneId;
        private string _taskId;

        private Vector2 _scroll;

        public static void Open(string milestoneId, string taskId)
        {
            var w = GetWindow<ElroiTaskDetailsWindow>("Task Details");
            w.minSize = new Vector2(540, 480);
            w._milestoneId = milestoneId;
            w._taskId = taskId;
            w.Load();
            w.ShowUtility();
            w.Focus();
        }

        private void OnEnable()
        {
            Load();
        }

        private void OnFocus()
        {
            // Stay in sync if JSON changed
            Load();
        }

        private void Load()
        {
            _data = ElroiKanbanStorage.LoadOrCreate();
        }

        private void Save()
        {
            ElroiKanbanStorage.Save(_data);
        }

        private void OnGUI()
        {
            if (_data == null) Load();

            var ms = _data?.milestones?.FirstOrDefault(m => m.id == _milestoneId);
            var task = ms?.tasks?.FirstOrDefault(t => t.id == _taskId);

            if (ms == null || task == null)
            {
                EditorGUILayout.HelpBox("Task not found (maybe it was deleted).", MessageType.Warning);
                if (GUILayout.Button("Close")) Close();
                return;
            }

            task.attachmentGuids ??= new List<string>();

            DrawHeader(ms, task);

            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawMainFields(ms, task);

            EditorGUILayout.Space(10);

            DrawAttachments(ms, task);

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
                Save();
        }

        private void DrawHeader(Milestone ms, TaskCard task)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"Task — {task.title}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open JSON", EditorStyles.toolbarButton))
                    ElroiKanbanStorage.RevealFile();

                if (GUILayout.Button("Close", EditorStyles.toolbarButton))
                    Close();
            }
        }

        private void DrawMainFields(Milestone ms, TaskCard task)
        {
            GUILayout.Label("Details", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                task.title = EditorGUILayout.TextField("Title", task.title ?? "");
                task.type = (TaskType)EditorGUILayout.EnumPopup("Type", task.type);
                task.dueDateIso = EditorGUILayout.TextField("Due (yyyy-MM-dd)", task.dueDateIso ?? "");

                // Milestone dropdown (moves the task between milestone buckets)
                var msNames = _data.milestones.Select(m => m.name).ToArray();
                var msIds = _data.milestones.Select(m => m.id).ToArray();

                int msIdx = Array.IndexOf(msIds, _milestoneId);
                if (msIdx < 0) msIdx = 0;

                int newMsIdx = EditorGUILayout.Popup("Milestone", msIdx, msNames);
                var newMilestoneId = msIds[Mathf.Clamp(newMsIdx, 0, msIds.Length - 1)];

                if (newMilestoneId != _milestoneId)
                {
                    MoveTaskToMilestone(_milestoneId, newMilestoneId, task.id);
                    _milestoneId = newMilestoneId;

                    // reload references after move
                    ms = _data.milestones.First(m => m.id == _milestoneId);
                    task = ms.tasks.First(t => t.id == _taskId);
                }

                // Feature dropdown (required)
                var featureNames = ms.features.Select(f => f.name).ToArray();
                var featureIds = ms.features.Select(f => f.id).ToArray();

                var idx = Array.IndexOf(featureIds, task.featureId);
                if (idx < 0) idx = 0;

                var newIdx = EditorGUILayout.Popup("Feature", idx, featureNames);
                task.featureId = featureIds[Mathf.Clamp(newIdx, 0, featureIds.Length - 1)];

                EditorGUILayout.Space(6);
                GUILayout.Label("Description");
                task.description = EditorGUILayout.TextArea(task.description ?? "", GUILayout.MinHeight(180));
            }
        }

        private void MoveTaskToMilestone(string fromMilestoneId, string toMilestoneId, string taskId)
        {
            if (_data == null) return;

            var from = _data.milestones.FirstOrDefault(m => m.id == fromMilestoneId);
            var to = _data.milestones.FirstOrDefault(m => m.id == toMilestoneId);
            if (from == null || to == null) return;

            from.tasks ??= new List<TaskCard>();
            to.tasks ??= new List<TaskCard>();

            var task = from.tasks.FirstOrDefault(t => t.id == taskId);
            if (task == null) return;

            // Ensure destination has at least one feature
            to.features ??= new List<FeatureItem>();
            if (to.features.Count == 0)
            {
                to.features.Add(new FeatureItem
                {
                    id = KanbanUtils.NewId(),
                    name = "General",
                    description = "Auto-created.",
                    createdUtcTicks = DateTime.UtcNow.Ticks,
                    updatedUtcTicks = DateTime.UtcNow.Ticks
                });
            }

            // If the task's current featureId doesn't exist in destination, remap to destination's first feature
            if (!to.features.Any(f => f.id == task.featureId))
                task.featureId = to.features[0].id;

            task.updatedUtcTicks = DateTime.UtcNow.Ticks;

            // Move between lists
            from.tasks.Remove(task);
            to.tasks.Add(task);

            // If it's on Today list, update its milestoneId there too
            _data.today?.items?.ForEach(i =>
            {
                if (i.taskId == taskId && i.milestoneId == fromMilestoneId)
                    i.milestoneId = toMilestoneId;
            });

            GUI.changed = true;
            Save();
        }


        private void DrawAttachments(Milestone ms, TaskCard task)
        {
            GUILayout.Label("Attachments", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox("Drag assets from the Project window here (Assets/ or Packages/). Use Import File… for external files.", MessageType.None);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Import File…", GUILayout.Width(120)))
                        ImportAndAttach(task);

                    GUILayout.FlexibleSpace();

                    GUILayout.Label($"{task.attachmentGuids.Count} attached", EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(6);

                var dropRect = GUILayoutUtility.GetRect(10, 70, GUILayout.ExpandWidth(true));
                GUI.Box(dropRect, "Drop Project assets here", EditorStyles.helpBox);
                HandleDragDrop(dropRect, task);

                EditorGUILayout.Space(8);

                DrawAttachmentGrid(task);
            }
        }

        private void HandleDragDrop(Rect dropRect, TaskCard task)
        {
            var e = Event.current;
            if (!dropRect.Contains(e.mousePosition)) return;

            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                // Only allow Unity objects that are real assets in Assets/ or Packages/
                var objs = DragAndDrop.objectReferences;
                bool valid = objs != null && objs.Length > 0 && objs.All(IsValidAttachableAsset);

                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    if (!valid)
                    {
                        // Explicitly enforce: no OS drag/drop into this window.
                        EditorUtility.DisplayDialog(
                            "Invalid drop",
                            "Only Unity Project assets can be attached.\n\nImport the file into Unity first, then drag it from the Project window.",
                            "OK"
                        );
                    }
                    else
                    {
                        foreach (var o in objs)
                            AttachAssetObject(task, o);
                    }

                    GUI.changed = true;
                }

                e.Use();
            }
        }

        private bool IsValidAttachableAsset(UnityEngine.Object obj)
        {
            if (obj == null) return false;
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrWhiteSpace(path)) return false;

            // Allow Assets/ and Packages/ per your requirement
            return path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
        }

        private void AttachAssetObject(TaskCard task, UnityEngine.Object obj)
        {
            if (obj == null) return;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrWhiteSpace(path)) return;

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid)) return;

            if (!task.attachmentGuids.Contains(guid))
                task.attachmentGuids.Add(guid);
        }

        private void ImportAndAttach(TaskCard task)
        {
            var file = EditorUtility.OpenFilePanel("Import file into Unity", "", "");
            if (string.IsNullOrWhiteSpace(file)) return;

            if (!Directory.Exists(ImportFolder))
                Directory.CreateDirectory(ImportFolder);

            var fileName = Path.GetFileName(file);
            var targetPath = GetUniqueAssetPath(Path.Combine(ImportFolder, fileName).Replace("\\", "/"));

            try
            {
                File.Copy(file, targetPath, overwrite: false);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Import failed", ex.Message, "OK");
                return;
            }

            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath);
            if (asset != null)
                AttachAssetObject(task, asset);

            GUI.changed = true;
        }

        private static string GetUniqueAssetPath(string desiredPath)
        {
            desiredPath = desiredPath.Replace("\\", "/");

            if (!File.Exists(desiredPath))
                return desiredPath;

            var dir = Path.GetDirectoryName(desiredPath)?.Replace("\\", "/");
            var name = Path.GetFileNameWithoutExtension(desiredPath);
            var ext = Path.GetExtension(desiredPath);

            int i = 1;
            while (true)
            {
                var candidate = $"{dir}/{name} ({i}){ext}";
                if (!File.Exists(candidate))
                    return candidate;
                i++;
            }
        }

        private void DrawAttachmentGrid(TaskCard task)
        {
            if (task.attachmentGuids.Count == 0)
            {
                GUILayout.Label("No attachments yet.", EditorStyles.miniLabel);
                return;
            }

            const float tileW = 96f;
            const float tileH = 92f;
            const float padding = 6f;

            var viewW = EditorGUIUtility.currentViewWidth - 60f;
            int cols = Mathf.Max(1, Mathf.FloorToInt(viewW / (tileW + padding)));

            int indexToRemove = -1;

            for (int i = 0; i < task.attachmentGuids.Count; i++)
            {
                if (i % cols == 0)
                    EditorGUILayout.BeginHorizontal();

                DrawAttachmentTile(task.attachmentGuids[i], i, ref indexToRemove);

                if (i % cols == cols - 1)
                    EditorGUILayout.EndHorizontal();
            }

            // Close any open horizontal group
            if (task.attachmentGuids.Count % cols != 0)
                EditorGUILayout.EndHorizontal();

            if (indexToRemove >= 0 && indexToRemove < task.attachmentGuids.Count)
            {
                task.attachmentGuids.RemoveAt(indexToRemove);
                GUI.changed = true;
            }
        }

        private void DrawAttachmentTile(string guid, int index, ref int indexToRemove)
        {
            const float tileW = 96f;
            const float tileH = 92f;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(tileW), GUILayout.Height(tileH)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object obj = null;

                bool missing = string.IsNullOrWhiteSpace(path);
                if (!missing)
                    obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                if (obj == null) missing = true;

                Texture icon = null;
                string label = "";

                if (!missing)
                {
                    icon = AssetPreview.GetMiniThumbnail(obj);
                    label = obj.name;
                }
                else
                {
                    icon = EditorGUIUtility.IconContent("console.warnicon").image;
                    label = "Missing";
                }

                var iconRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                if (GUI.Button(iconRect, icon, GUIStyle.none))
                {
                    if (!missing && obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }

                GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(tileW - 6));

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                        indexToRemove = index;
                }
            }
        }
    }
}
