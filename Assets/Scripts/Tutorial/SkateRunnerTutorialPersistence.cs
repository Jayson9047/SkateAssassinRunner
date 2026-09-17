using Elroi.Tutorials;
using UnityEngine;

public sealed class SkateRunnerTutorialPersistence : MonoBehaviour, ITutorialPersistenceProvider
{
    private const string Prefix = "ELROI.Tutorials.v1.";

    public bool IsTutorialComplete(string stableId) => Read("Tutorial.", stableId);
    public void MarkTutorialComplete(string stableId) => Write("Tutorial.", stableId);
    public bool IsSequencerComplete(string stableId) => Read("Sequencer.", stableId);
    public void MarkSequencerComplete(string stableId) => Write("Sequencer.", stableId);

    private static bool Read(string kind, string stableId) => !string.IsNullOrWhiteSpace(stableId) && ES3.Load(Prefix + kind + stableId, false);

    private static void Write(string kind, string stableId)
    {
        if (string.IsNullOrWhiteSpace(stableId)) return;
        ES3.Save(Prefix + kind + stableId, true);
    }
}
