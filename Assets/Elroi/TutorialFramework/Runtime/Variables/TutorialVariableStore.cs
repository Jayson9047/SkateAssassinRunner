using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elroi.Tutorials
{
    [Serializable]
    public sealed class TutorialVariableEntry
    {
        [SerializeField] private string id;
        [SerializeField] private TutorialValue value;

        public string Id { get => id; set => id = value; }
        public TutorialValue Value { get => value; set => this.value = value; }
    }

    [DisallowMultipleComponent]
    public sealed class TutorialVariableStore : MonoBehaviour, ITutorialVariableProvider
    {
        [SerializeField] private List<TutorialVariableEntry> variables = new List<TutorialVariableEntry>();
        private readonly Dictionary<string, TutorialVariableEntry> byId = new Dictionary<string, TutorialVariableEntry>(StringComparer.Ordinal);

        public event Action<string, TutorialValue> VariableChanged;
        public List<TutorialVariableEntry> Variables => variables;

        private void Awake() => RebuildIndex();
        private void OnValidate() => RebuildIndex();

        public void RebuildIndex()
        {
            byId.Clear();
            foreach (TutorialVariableEntry entry in variables)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || byId.ContainsKey(entry.Id)) continue;
                byId.Add(entry.Id, entry);
            }
        }

        public bool TryGetValue(string variableId, out TutorialValue value)
        {
            if (byId.Count == 0 && variables.Count > 0) RebuildIndex();
            if (!string.IsNullOrWhiteSpace(variableId) && byId.TryGetValue(variableId, out TutorialVariableEntry entry))
            {
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool SetValue(string variableId, TutorialValue value)
        {
            if (!byId.TryGetValue(variableId, out TutorialVariableEntry entry)) return false;
            if (entry.Value.Equals(value)) return false;
            entry.Value = value;
            VariableChanged?.Invoke(variableId, value);
            return true;
        }

        public bool SetInteger(string id, int value) => SetValue(id, TutorialValue.From(value));
        public bool SetFloat(string id, float value) => SetValue(id, TutorialValue.From(value));
        public bool SetBoolean(string id, bool value) => SetValue(id, TutorialValue.From(value));
        public bool SetString(string id, string value) => SetValue(id, TutorialValue.From(value));
    }
}
