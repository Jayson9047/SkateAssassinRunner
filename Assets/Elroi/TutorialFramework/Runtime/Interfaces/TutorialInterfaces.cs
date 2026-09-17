using System;
using System.Collections;

namespace Elroi.Tutorials
{
    public interface ITutorialGameplayAdapter
    {
        void SetGameplayInputBlocked(bool blocked);
        bool CanExecuteGameplayAction(string actionId, out string reason);
        void ExecuteGameplayAction(string actionId);
    }

    /// <summary>
    /// Optional extension for gameplay actions that span multiple frames, such as a replayed double tap.
    /// TutorialManager keeps its input lock until the returned routine completes.
    /// </summary>
    public interface ITutorialAsyncGameplayAdapter
    {
        IEnumerator ExecuteGameplayActionRoutine(string actionId);
    }

    public interface ITutorialVariableProvider
    {
        event Action<string, TutorialValue> VariableChanged;
        bool TryGetValue(string variableId, out TutorialValue value);
    }

    public interface ITutorialPersistenceProvider
    {
        bool IsTutorialComplete(string stableId);
        void MarkTutorialComplete(string stableId);
        bool IsSequencerComplete(string stableId);
        void MarkSequencerComplete(string stableId);
    }

    public interface ITutorialFreezeService
    {
        bool IsFrozen { get; }
        void Freeze();
        void Restore();
    }
}
