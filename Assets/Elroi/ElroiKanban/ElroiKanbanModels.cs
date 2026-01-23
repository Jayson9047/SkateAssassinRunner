using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Kanban
{
    public enum TaskType { Task, Bug }
    public enum TaskStatus { Backlog, InProgress, Review, Done }

    [Serializable]
    public class FeatureItem
    {
        public string id;
        public string name;
        [TextArea(2, 6)] public string description;
        public long createdUtcTicks;
        public long updatedUtcTicks;
    }

    [Serializable]
    public class TaskCard
    {
        public string id;
        public string featureId;

        public string title;
        [TextArea(2, 10)] public string description;

        public string dueDateIso; // yyyy-MM-dd (local)
        public TaskType type;
        public TaskStatus status;

        public long createdUtcTicks;
        public long updatedUtcTicks;

        // Attachments (GUIDs)
        public List<string> attachmentGuids = new();

        // Archive / history
        public bool isArchived = false;
        public long archivedUtcTicks = 0;

        // LEGACY (migration only)
        public string screenshotAssetGuid;
    }

    [Serializable]
    public class Milestone
    {
        public string id;
        public string name;

        public List<FeatureItem> features = new();
        public List<TaskCard> tasks = new();
    }

    [Serializable]
    public class TodayItem
    {
        public string milestoneId;
        public string taskId;
        public bool done;
        public long addedUtcTicks;
    }

    [Serializable]
    public class TodayList
    {
        public string dateIso; // yyyy-MM-dd local
        public List<TodayItem> items = new();
    }

    [Serializable]
    public class BoardData
    {
        public int schemaVersion = 5;

        public string selectedMilestoneId = "";
        public string selectedFeatureId = ""; // "" = all features

        // Display names for the 4 columns (renamable)
        public List<string> columnNames = new() { "Backlog", "In Progress", "Review", "Done" };

        public List<Milestone> milestones = new();

        public TodayList today = new TodayList { dateIso = "" };
    }

    public static class KanbanUtils
    {
        public static string NewId() => Guid.NewGuid().ToString("N");

        public static string TodayIso()
        {
            var d = DateTime.Now.Date;
            return d.ToString("yyyy-MM-dd");
        }

        public static TaskStatus Next(TaskStatus s) => s switch
        {
            TaskStatus.Backlog => TaskStatus.InProgress,
            TaskStatus.InProgress => TaskStatus.Review,
            TaskStatus.Review => TaskStatus.Done,
            TaskStatus.Done => TaskStatus.Done,
            _ => TaskStatus.Backlog
        };
    }
}
