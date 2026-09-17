using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.InfiniteRunnerEngine
{
    [Serializable]
    public sealed class LevelObstacleSequenceOverride
    {
        [Min(1)] public int LevelNumber = 1;
        public List<GameObject> Sequence = new List<GameObject>();
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MMMultipleObjectPooler))]
    [DefaultExecutionOrder(-10000)]
    [AddComponentMenu("ELROI/Skate Runner/Obstacle Sequence Configurator")]
    public sealed class SkateRunnerObstacleSequenceConfigurator : MonoBehaviour
    {
        private const string LevelNumberSaveKey = "LevelNum";

        [Header("Authored Level Sequences")]
        [SerializeField] private List<LevelObstacleSequenceOverride> levelOverrides =
            new List<LevelObstacleSequenceOverride>();

        [Header("Debugging")]
        [SerializeField] private bool logResolvedSequence;

        private void Awake()
        {
            MMMultipleObjectPooler pooler = GetComponent<MMMultipleObjectPooler>();
            if (pooler == null)
            {
                Debug.LogWarning("[ObstacleSequence] MMMultipleObjectPooler is missing; sequence configuration was skipped.", this);
                return;
            }

            pooler.PoolingMethod = MMPoolingMethods.OriginalOrder;

            List<MMMultipleObjectPoolerObject> masterPool = ClonePool(pooler.Pool, true);
            if (masterPool.Count == 0)
            {
                Debug.LogWarning("[ObstacleSequence] The source pool is empty; sequence configuration was skipped.", this);
                return;
            }

            int levelNumber = ResolveCurrentLevelNumber();
            List<MMMultipleObjectPoolerObject> resolvedPool = ResolvePoolForLevel(levelNumber, masterPool, out string mode);
            pooler.Pool = resolvedPool;

            if (logResolvedSequence)
            {
                Debug.Log($"[ObstacleSequence] Level {levelNumber} | {mode} | {BuildEnabledSequenceLabel(resolvedPool)}", this);
            }
        }

        private int ResolveCurrentLevelNumber()
        {
            SkateRunnerGameManager gameManager = SkateRunnerGameManager.SkateRunnerGameManagerAccessor;
            if (gameManager != null)
            {
                return gameManager.LevelNum;
            }

            try
            {
                return ES3.Load(LevelNumberSaveKey, 1);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[ObstacleSequence] Could not load '{LevelNumberSaveKey}' before GameManager initialization. " +
                    $"Using Level 1. {exception.GetType().Name}: {exception.Message}",
                    this);
                return 1;
            }
        }

        private List<MMMultipleObjectPoolerObject> ResolvePoolForLevel(
            int levelNumber,
            List<MMMultipleObjectPoolerObject> masterPool,
            out string mode)
        {
            if (TryBuildAuthoredSequence(levelNumber, masterPool, out List<MMMultipleObjectPoolerObject> authored))
            {
                mode = "AUTHORED";
                return authored;
            }

            List<MMMultipleObjectPoolerObject> shuffled = ClonePool(masterPool, false);
            try
            {
                SecureFisherYatesShuffle(shuffled);
            }
            catch (CryptographicException exception)
            {
                Debug.LogWarning(
                    $"[ObstacleSequence] Secure shuffle failed; preserving the copied master order for this run. " +
                    $"{exception.GetType().Name}: {exception.Message}",
                    this);
            }

            mode = "SHUFFLED";
            return shuffled;
        }

        private bool TryBuildAuthoredSequence(
            int levelNumber,
            List<MMMultipleObjectPoolerObject> masterPool,
            out List<MMMultipleObjectPoolerObject> resolved)
        {
            resolved = null;
            if (levelOverrides == null || levelOverrides.Count == 0)
            {
                return false;
            }

            List<LevelObstacleSequenceOverride> matches = new List<LevelObstacleSequenceOverride>();
            for (int index = 0; index < levelOverrides.Count; index++)
            {
                LevelObstacleSequenceOverride levelOverride = levelOverrides[index];
                if (levelOverride == null)
                {
                    Debug.LogWarning($"[ObstacleSequence] Override entry {index} is null and will be ignored.", this);
                    continue;
                }

                if (levelOverride.LevelNumber < 1)
                {
                    Debug.LogWarning(
                        $"[ObstacleSequence] Override entry {index} has invalid LevelNumber {levelOverride.LevelNumber} and will be ignored.",
                        this);
                    continue;
                }

                if (levelOverride.LevelNumber == levelNumber)
                {
                    matches.Add(levelOverride);
                }
            }

            if (matches.Count == 0)
            {
                return false;
            }

            if (matches.Count > 1)
            {
                Debug.LogWarning(
                    $"[ObstacleSequence] Level {levelNumber} has {matches.Count} overrides. The first usable override will be used.",
                    this);
            }

            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                LevelObstacleSequenceOverride candidate = matches[matchIndex];
                if (candidate.Sequence == null || candidate.Sequence.Count == 0)
                {
                    Debug.LogWarning(
                        $"[ObstacleSequence] Level {levelNumber} override {matchIndex} has an empty sequence and will be ignored.",
                        this);
                    continue;
                }

                List<MMMultipleObjectPoolerObject> candidatePool = new List<MMMultipleObjectPoolerObject>();
                for (int sequenceIndex = 0; sequenceIndex < candidate.Sequence.Count; sequenceIndex++)
                {
                    GameObject requestedObject = candidate.Sequence[sequenceIndex];
                    if (requestedObject == null)
                    {
                        Debug.LogWarning(
                            $"[ObstacleSequence] Level {levelNumber} contains a null object at sequence index {sequenceIndex}; it will be skipped.",
                            this);
                        continue;
                    }

                    MMMultipleObjectPoolerObject source = FindSourceEntry(masterPool, requestedObject);
                    if (source == null)
                    {
                        Debug.LogWarning(
                            $"[ObstacleSequence] Level {levelNumber} references '{requestedObject.name}', which is not in the source pool; it will be skipped.",
                            this);
                        continue;
                    }

                    candidatePool.Add(CloneEntry(source));
                }

                if (candidatePool.Count > 0)
                {
                    resolved = candidatePool;
                    return true;
                }

                Debug.LogWarning(
                    $"[ObstacleSequence] Level {levelNumber} override {matchIndex} has no usable entries and will be ignored.",
                    this);
            }

            Debug.LogWarning(
                $"[ObstacleSequence] Level {levelNumber} has no usable authored sequence; secure shuffled mode will be used.",
                this);
            return false;
        }

        private static List<MMMultipleObjectPoolerObject> ClonePool(
            List<MMMultipleObjectPoolerObject> sourcePool,
            bool warnAboutInvalidEntries)
        {
            List<MMMultipleObjectPoolerObject> copy = new List<MMMultipleObjectPoolerObject>();
            if (sourcePool == null)
            {
                return copy;
            }

            for (int index = 0; index < sourcePool.Count; index++)
            {
                MMMultipleObjectPoolerObject source = sourcePool[index];
                if (source == null || source.GameObjectToPool == null)
                {
                    if (warnAboutInvalidEntries)
                    {
                        Debug.LogWarning($"[ObstacleSequence] Source pool entry {index} is null or has no GameObject and will be skipped.");
                    }
                    continue;
                }

                copy.Add(CloneEntry(source));
            }

            return copy;
        }

        private static MMMultipleObjectPoolerObject CloneEntry(MMMultipleObjectPoolerObject source)
        {
            return new MMMultipleObjectPoolerObject
            {
                GameObjectToPool = source.GameObjectToPool,
                PoolSize = source.PoolSize,
                PoolCanExpand = source.PoolCanExpand,
                Enabled = source.Enabled
            };
        }

        private static MMMultipleObjectPoolerObject FindSourceEntry(
            List<MMMultipleObjectPoolerObject> masterPool,
            GameObject requestedObject)
        {
            for (int index = 0; index < masterPool.Count; index++)
            {
                MMMultipleObjectPoolerObject entry = masterPool[index];
                if (entry != null && entry.GameObjectToPool == requestedObject)
                {
                    return entry;
                }
            }

            return null;
        }

        private static void SecureFisherYatesShuffle(List<MMMultipleObjectPoolerObject> entries)
        {
            for (int index = entries.Count - 1; index > 0; index--)
            {
                int swapIndex = RandomNumberGenerator.GetInt32(index + 1);
                MMMultipleObjectPoolerObject temporary = entries[index];
                entries[index] = entries[swapIndex];
                entries[swapIndex] = temporary;
            }
        }

        private static string BuildEnabledSequenceLabel(List<MMMultipleObjectPoolerObject> entries)
        {
            List<string> names = new List<string>();
            for (int index = 0; index < entries.Count; index++)
            {
                MMMultipleObjectPoolerObject entry = entries[index];
                if (entry != null && entry.Enabled && entry.GameObjectToPool != null)
                {
                    names.Add(entry.GameObjectToPool.name);
                }
            }

            return names.Count > 0 ? string.Join(" -> ", names) : "<no enabled entries>";
        }
    }
}
