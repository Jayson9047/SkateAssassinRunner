using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elroi.Kanban
{
    public static class ElroiKanbanStorage
    {
        private const string RelativePath = "ProjectSettings/ElroiKanban.json";

        // Live sync signal for all windows
        public static event Action OnDataSaved;
        private static long _saveRevision = 0;
        public static long SaveRevision => _saveRevision;

        public static string AbsolutePath
        {
            get
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.Combine(projectRoot, RelativePath);
            }
        }

        public static BoardData LoadOrCreate()
        {
            BoardData data = null;

            try
            {
                var path = AbsolutePath;
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    data = JsonUtility.FromJson<BoardData>(json);
                }
            }
            catch
            {
                // corrupted file -> recreate
            }

            if (data == null || data.milestones == null || data.milestones.Count == 0)
            {
                data = CreateFresh();
                Save(data, fireEvent: false);
                return data;
            }

            MigrateAndFixup(data);
            Save(data, fireEvent: false);
            return data;
        }

        private static BoardData CreateFresh()
        {
            var ms = new Milestone
            {
                id = KanbanUtils.NewId(),
                name = "Milestone 1",
            };

            var general = new FeatureItem
            {
                id = KanbanUtils.NewId(),
                name = "General",
                description = "Default feature bucket.",
                createdUtcTicks = DateTime.UtcNow.Ticks,
                updatedUtcTicks = DateTime.UtcNow.Ticks
            };

            ms.features.Add(general);

            var d = new BoardData();
            d.milestones.Add(ms);
            d.selectedMilestoneId = ms.id;
            d.selectedFeatureId = "";
            d.columnNames = new System.Collections.Generic.List<string> { "Backlog", "In Progress", "Review", "Done" };
            d.today = new TodayList { dateIso = KanbanUtils.TodayIso() };
            return d;
        }

        private static void MigrateAndFixup(BoardData data)
        {
            if (data.schemaVersion < 5)
                data.schemaVersion = 5;

            // Column names (ensure 4)
            data.columnNames ??= new System.Collections.Generic.List<string>();
            if (data.columnNames.Count != 4)
            {
                data.columnNames.Clear();
                data.columnNames.Add("Backlog");
                data.columnNames.Add("In Progress");
                data.columnNames.Add("Review");
                data.columnNames.Add("Done");
            }
            for (int i = 0; i < 4; i++)
            {
                if (string.IsNullOrWhiteSpace(data.columnNames[i]))
                    data.columnNames[i] = i switch
                    {
                        0 => "Backlog",
                        1 => "In Progress",
                        2 => "Review",
                        _ => "Done"
                    };
            }

            // Today list
            if (data.today == null) data.today = new TodayList();
            var todayIso = KanbanUtils.TodayIso();
            if (string.IsNullOrWhiteSpace(data.today.dateIso) || data.today.dateIso != todayIso)
            {
                data.today.dateIso = todayIso;
                data.today.items ??= new System.Collections.Generic.List<TodayItem>();
            }
            data.today.items ??= new System.Collections.Generic.List<TodayItem>();

            foreach (var ms in data.milestones)
            {
                if (string.IsNullOrWhiteSpace(ms.id))
                    ms.id = KanbanUtils.NewId();

                ms.features ??= new System.Collections.Generic.List<FeatureItem>();
                ms.tasks ??= new System.Collections.Generic.List<TaskCard>();

                if (ms.features.Count == 0)
                {
                    ms.features.Add(new FeatureItem
                    {
                        id = KanbanUtils.NewId(),
                        name = "General",
                        description = "Auto-created during migration.",
                        createdUtcTicks = DateTime.UtcNow.Ticks,
                        updatedUtcTicks = DateTime.UtcNow.Ticks
                    });
                }

                foreach (var f in ms.features)
                {
                    if (string.IsNullOrWhiteSpace(f.id)) f.id = KanbanUtils.NewId();
                    if (string.IsNullOrWhiteSpace(f.name)) f.name = "Feature";
                }

                var fallbackFeatureId = ms.features[0].id;

                foreach (var t in ms.tasks)
                {
                    if (string.IsNullOrWhiteSpace(t.id)) t.id = KanbanUtils.NewId();
                    if (string.IsNullOrWhiteSpace(t.featureId)) t.featureId = fallbackFeatureId;
                    if (string.IsNullOrWhiteSpace(t.title)) t.title = "Untitled task";
                    if (string.IsNullOrWhiteSpace(t.dueDateIso)) t.dueDateIso = KanbanUtils.TodayIso();

                    t.attachmentGuids ??= new System.Collections.Generic.List<string>();

                    // migrate legacy screenshot into attachments
                    if (!string.IsNullOrWhiteSpace(t.screenshotAssetGuid))
                    {
                        if (!t.attachmentGuids.Contains(t.screenshotAssetGuid))
                            t.attachmentGuids.Add(t.screenshotAssetGuid);
                        t.screenshotAssetGuid = null;
                    }

                    // clamp invalid enum values
                    if (!Enum.IsDefined(typeof(TaskType), t.type))
                        t.type = TaskType.Task;

                    if (!Enum.IsDefined(typeof(TaskStatus), t.status))
                        t.status = TaskStatus.Backlog;

                    // archive fields safe defaults
                    // (JsonUtility handles missing fields, but we ensure consistent)
                    if (t.archivedUtcTicks < 0) t.archivedUtcTicks = 0;
                }
            }

            // selected milestone valid
            if (string.IsNullOrWhiteSpace(data.selectedMilestoneId) ||
                !data.milestones.Any(m => m.id == data.selectedMilestoneId))
            {
                data.selectedMilestoneId = data.milestones[0].id;
            }

            // feature selection valid (or "")
            var cur = data.milestones.FirstOrDefault(m => m.id == data.selectedMilestoneId);
            if (cur != null && !string.IsNullOrWhiteSpace(data.selectedFeatureId))
            {
                if (!cur.features.Any(f => f.id == data.selectedFeatureId))
                    data.selectedFeatureId = "";
            }

            // Remove Today entries that point to missing OR archived tasks
            data.today.items.RemoveAll(i =>
            {
                var t = FindTask(data, i.milestoneId, i.taskId);
                return t == null || t.isArchived;
            });
        }

        public static void Save(BoardData data) => Save(data, fireEvent: true);

        private static void Save(BoardData data, bool fireEvent)
        {
            if (data == null) return;

            try
            {
                var path = AbsolutePath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);

                _saveRevision++;
                if (fireEvent)
                    OnDataSaved?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"ElroiKanban: Failed to save. {e.Message}");
            }
        }

        public static void RevealFile()
        {
            var path = AbsolutePath;
            if (File.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                Debug.LogWarning($"ElroiKanban: No file yet at {path}");
        }

        // Helpers
        public static Milestone GetSelectedMilestone(BoardData data)
        {
            if (data == null || data.milestones == null || data.milestones.Count == 0) return null;
            return data.milestones.FirstOrDefault(m => m.id == data.selectedMilestoneId) ?? data.milestones[0];
        }

        public static TaskCard FindTask(BoardData data, string milestoneId, string taskId)
        {
            var ms = data?.milestones?.FirstOrDefault(m => m.id == milestoneId);
            return ms?.tasks?.FirstOrDefault(t => t.id == taskId);
        }

        public static FeatureItem FindFeature(BoardData data, string milestoneId, string featureId)
        {
            var ms = data?.milestones?.FirstOrDefault(m => m.id == milestoneId);
            return ms?.features?.FirstOrDefault(f => f.id == featureId);
        }
    }
}
