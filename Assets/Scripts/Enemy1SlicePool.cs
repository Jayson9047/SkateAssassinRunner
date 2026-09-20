using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

namespace IndieKit
{
    /// <summary>Scene-owned, fixed-capacity Enemy 1 cosmetics. Allocation/instantiation happens during prewarm.</summary>
    public sealed class Enemy1SlicePool : MonoBehaviour
    {
        public GameObject legacyDebrisPrefab;
        public Enemy1SlicePresentation bodyPrefab;
        public ParticleSystem contactPrefab;
        public ParticleSystem groundPrefab;
        [Range(1, 16)] public int bodyCapacity = 8;
        [Range(1, 16)] public int contactCapacity = 8;
        [Range(1, 8)] public int groundCapacity = 4;
        [Header("Internal cosmetic switches (not player settings)")]
        public bool directionalBlood = true;
        public bool contactStreak = true;
        public bool groundBlood = true;
        public LayerMask groundMask;
        public float scrollSpeedFactor = 0.5f;
        public float contactLifetime = 0.075f;
        public float groundLifetime = 1f;
        public float groundDelay = 0.10f;

        struct Effect
        {
            public ParticleSystem particles;
            public float remaining, delay;
            public bool active, playing;
        }
        static Enemy1SlicePool _instance;
        Enemy1SlicePresentation[] _bodies;
        Effect[] _contacts, _grounds;
        int _nextBody, _nextContact, _nextGround;
        bool _phase2;
        public int ActiveBodies { get; private set; }
        public int ActiveContacts { get; private set; }
        public int ActiveGrounds { get; private set; }
        public static Enemy1SlicePool Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _instance = null; }

        public static Enemy1SlicePool Prepare(GameObject legacyDebris)
        {
            if (_instance) return _instance.legacyDebrisPrefab == legacyDebris ? _instance : null;
            var prefab = Resources.Load<Enemy1SlicePool>("Enemy1SlicePool");
            if (!prefab || prefab.legacyDebrisPrefab != legacyDebris) return null;
            _instance = Instantiate(prefab);
            _instance.name = "Enemy1SlicePool (bounded)";
            return _instance;
        }

        void Awake()
        {
            _bodies = new Enemy1SlicePresentation[Mathf.Clamp(bodyCapacity, 1, 16)];
            for (int i = 0; i < _bodies.Length; i++)
            {
                _bodies[i] = Instantiate(bodyPrefab, transform);
                _bodies[i].Cache();
                _bodies[i].blood.Play(true);
                _bodies[i].blood.Simulate(0.02f, true, false);
                _bodies[i].Release();
            }
            _contacts = BuildEffects(contactPrefab, Mathf.Clamp(contactCapacity, 1, 16));
            _grounds = BuildEffects(groundPrefab, Mathf.Clamp(groundCapacity, 1, 8));
            SkateAssassinRunnerLevelManager.OnPhase2Started += EnterPhase2;
        }

        Effect[] BuildEffects(ParticleSystem prefab, int count)
        {
            var result = new Effect[count];
            for (int i = 0; i < count; i++)
            {
                var ps = Instantiate(prefab, transform);
                // Warm native particle storage as well as managed references before the first kill.
                ps.Play(true);
                ps.Simulate(0.02f, true, false);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                result[i].particles = ps;
            }
            return result;
        }

        public bool Play(GameObject legacyDebris, Vector3 position, Quaternion rotation, Vector3 scale, KillCause cause)
        {
            if (_phase2 || cause == KillCause.Phase2 || legacyDebris != legacyDebrisPrefab) return false;
            bool hasGround = Physics.Raycast(position + Vector3.up * 1.4f, Vector3.down,
                out RaycastHit groundHit, 20f, groundMask, QueryTriggerInteraction.Ignore);
            Enemy1SlicePresentation body = _bodies[_nextBody];
            _nextBody = (_nextBody + 1) % _bodies.Length;
            body.Begin(position, rotation, scale, cause, directionalBlood, hasGround, groundHit.point.y);
            Vector3 seam = body.transform.TransformPoint(body.seam);
            if (contactStreak)
            {
                StartEffect(ref _contacts[_nextContact], seam, Quaternion.identity, contactLifetime, 0f);
                _nextContact = (_nextContact + 1) % _contacts.Length;
            }
            if (groundBlood && hasGround)
            {
                Vector3 mark = groundHit.point + groundHit.normal * 0.014f;
                StartEffect(ref _grounds[_nextGround], mark,
                    Quaternion.FromToRotation(Vector3.forward, groundHit.normal), groundLifetime, groundDelay);
                _nextGround = (_nextGround + 1) % _grounds.Length;
            }
            return true;
        }

        static void StartEffect(ref Effect fx, Vector3 position, Quaternion rotation, float lifetime, float delay)
        {
            fx.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fx.particles.transform.SetPositionAndRotation(position, rotation);
            fx.particles.transform.localScale = Vector3.one;
            fx.remaining = lifetime;
            fx.delay = delay;
            fx.active = true;
            fx.playing = delay <= 0f;
            fx.particles.gameObject.SetActive(fx.playing);
            if (fx.playing) fx.particles.Play(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float levelSpeed = LevelManager.Instance ? LevelManager.Instance.Speed : 0f;
            Tick(dt, levelSpeed);
        }

        public void Tick(float dt, float levelSpeed)
        {
            float scroll = levelSpeed * scrollSpeedFactor * dt;
            ActiveBodies = 0;
            for (int i = 0; i < _bodies.Length; i++)
                if (_bodies[i].gameObject.activeSelf && _bodies[i].Tick(dt, scroll)) ActiveBodies++;
            ActiveContacts = TickEffects(_contacts, dt, scroll);
            ActiveGrounds = TickEffects(_grounds, dt, scroll);
        }

        static int TickEffects(Effect[] effects, float dt, float scroll)
        {
            int active = 0;
            for (int i = 0; i < effects.Length; i++)
            {
                if (!effects[i].active) continue;
                effects[i].particles.transform.position += Vector3.left * scroll;
                if (!effects[i].playing)
                {
                    effects[i].delay -= dt;
                    if (effects[i].delay > 0f) { active++; continue; }
                    effects[i].playing = true;
                    effects[i].particles.gameObject.SetActive(true);
                    effects[i].particles.Play(true);
                }
                effects[i].remaining -= dt;
                if (effects[i].remaining <= 0f)
                {
                    effects[i].particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    effects[i].particles.gameObject.SetActive(false);
                    effects[i].active = false;
                }
                else active++;
            }
            return active;
        }

        public void Clear()
        {
            for (int i = 0; i < _bodies.Length; i++) _bodies[i].Release();
            ClearEffects(_contacts);
            ClearEffects(_grounds);
            ActiveBodies = ActiveContacts = ActiveGrounds = 0;
        }

        static void ClearEffects(Effect[] effects)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                effects[i].particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effects[i].particles.gameObject.SetActive(false);
                effects[i].active = effects[i].playing = false;
                effects[i].remaining = effects[i].delay = 0f;
            }
        }

        void EnterPhase2() { _phase2 = true; Clear(); }
        void OnDisable() { if (_bodies != null) Clear(); }
        void OnDestroy()
        {
            SkateAssassinRunnerLevelManager.OnPhase2Started -= EnterPhase2;
            if (_instance == this) _instance = null;
        }
    }
}
