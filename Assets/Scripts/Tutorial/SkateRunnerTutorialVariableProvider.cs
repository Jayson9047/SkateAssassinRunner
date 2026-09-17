using System;
using Elroi.Tutorials;
using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

public sealed class SkateRunnerTutorialVariableProvider : MonoBehaviour, ITutorialVariableProvider
{
    public event Action<string, TutorialValue> VariableChanged;

    private void OnEnable() => SkateRunnerGameManager.OnLevelChanged += HandleLevelChanged;
    private void OnDisable() => SkateRunnerGameManager.OnLevelChanged -= HandleLevelChanged;

    public bool TryGetValue(string variableId, out TutorialValue value)
    {
        if (string.Equals(variableId, "CurrentLevel", StringComparison.Ordinal) && SkateRunnerGameManager.SkateRunnerGameManagerAccessor != null)
        {
            value = TutorialValue.From(SkateRunnerGameManager.SkateRunnerGameManagerAccessor.LevelNum);
            return true;
        }
        value = default;
        return false;
    }

    private void HandleLevelChanged(int level) => VariableChanged?.Invoke("CurrentLevel", TutorialValue.From(level));
}
