using UnityEngine;

namespace Elroi.Tutorials
{
    [DisallowMultipleComponent]
    public sealed class PlayerPrefsTutorialPersistence : MonoBehaviour, ITutorialPersistenceProvider
    {
        [SerializeField] private string keyPrefix = "elroi.tutorials.v1.";

        public bool IsTutorialComplete(string stableId) => Read("tutorial.", stableId);
        public void MarkTutorialComplete(string stableId) => Write("tutorial.", stableId);
        public bool IsSequencerComplete(string stableId) => Read("sequencer.", stableId);
        public void MarkSequencerComplete(string stableId) => Write("sequencer.", stableId);

        private bool Read(string kind, string id) => !string.IsNullOrWhiteSpace(id) && PlayerPrefs.GetInt(keyPrefix + kind + id, 0) == 1;
        private void Write(string kind, string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            PlayerPrefs.SetInt(keyPrefix + kind + id, 1);
            PlayerPrefs.Save();
        }

        [ContextMenu("Clear ELROI Tutorial Progress")]
        public void ClearKnownProgress()
        {
            Debug.Log("PlayerPrefs does not support safe prefix enumeration. Delete known keys through your game's save UI or change the prefix for a fresh profile.", this);
        }
    }
}
