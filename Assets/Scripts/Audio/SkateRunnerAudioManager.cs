using System;
using System.Collections;
using System.Collections.Generic;
using IndieKit;
using MoreMountains.InfiniteRunnerEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public sealed class SkateRunnerAudioCue
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.25f, 3f)] public float pitch = 1f;
    [Range(0f, 0.25f)] public float randomPitchRange;
    [Min(0f)] public float minimumRetriggerInterval;
    [NonSerialized] public float lastPlayedAt = float.NegativeInfinity;
}

[DisallowMultipleComponent]
public sealed class SkateRunnerAudioManager : MonoBehaviour, MMEventListener<MMGameEvent>
{
    public static SkateRunnerAudioManager Instance { get; private set; }

    [Header("Global UI")]
    [SerializeField] SkateRunnerAudioCue uiButtonClick = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue levelEndMultiplierFocus = new SkateRunnerAudioCue();
    [Header("Spin Wheel")]
    [SerializeField] SkateRunnerAudioCue wheelSpinningLoop = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue wheelWinningLanding = new SkateRunnerAudioCue();
    [Header("Purchase Success")]
    [SerializeField] SkateRunnerAudioCue purchaseSuccess = new SkateRunnerAudioCue();
    [Header("Crystal Reward Reveal")]
    [SerializeField] AudioClip crystalRewardRevealMusic;
    [SerializeField, Range(0f, 1f)] float crystalRewardRevealMusicVolume = 0.75f;
    [SerializeField] SkateRunnerAudioCue crystalChestBreak = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue rewardReveal = new SkateRunnerAudioCue();
    [SerializeField, Range(0f, 1f)] float rewardRevealBackgroundMusicDuck = 0.35f;
    [Header("Homepage Music")]
    [SerializeField, InspectorName("Homepage Music 1")] HomepageMusicEntry homepageTrack1 = new HomepageMusicEntry();
    [SerializeField, InspectorName("Homepage Music 2")] HomepageMusicEntry homepageTrack2 = new HomepageMusicEntry();
    [SerializeField, Min(0f)] float homepageMusicGapSeconds = 7f;
    [Header("Gameplay Music")]
    [SerializeField, InspectorName("Gameplay Music Tracks")] List<GameplayMusicTrack> gameplayTracks = new List<GameplayMusicTrack>();
    [Header("Player Actions")]
    [SerializeField] SkateRunnerAudioCue playerJump = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue dashAttack = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue downAttack = new SkateRunnerAudioCue();
    [Header("Player Death")]
    [SerializeField] SkateRunnerAudioCue playerDeath = new SkateRunnerAudioCue();
    [Header("Enemy Death")]
    [SerializeField] SkateRunnerAudioCue enemyType1Death = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue enemyType2Death = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue enemyType3Death = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue flyingDroneEnemyDeath = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue unknownEnemyDeathFallback = new SkateRunnerAudioCue();
    [Header("Enemy Death Supporting Audio (Optional)")]
    [SerializeField] SkateRunnerAudioCue enemyType1DeathSupport = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue enemyType2DeathSupport = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue enemyType3DeathSupport = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue flyingDroneEnemyDeathSupport = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue unknownEnemyDeathFallbackSupport = new SkateRunnerAudioCue();
    [Header("Destruction")]
    [SerializeField] SkateRunnerAudioCue barrelDestruction = new SkateRunnerAudioCue();
    [Header("Optional Arcade Announcers (SFX)")]
    [Tooltip("Unscaled seconds after successful Ruthless Tap completion before any of the three rank voices plays. Independent of text delay; does not delay Powerslam, Outro or results.")]
    [SerializeField, Min(0f)] float rankAudioDelaySeconds = 0f;
    [SerializeField] SkateRunnerAudioCue powerslamAnnouncer = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue killerAssassinAnnouncer = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue brutalAnnouncer = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue ruthlessAnnouncer = new SkateRunnerAudioCue();
    [Header("Currency Pickups")]
    [Tooltip("Limit cash pickup sounds only. Uncheck to allow a sound for every pickup. Cash uses these controls instead of its cue's Minimum Retrigger Interval.")]
    [SerializeField] bool enableCashPickupSoundCooldown = true;
    [Tooltip("Real-time seconds after a cash sound before another may play. Skipped pickups do not extend the cooldown or queue sounds. Rewards and other SFX are unaffected.")]
    [SerializeField, Min(0f)] float cashPickupSoundCooldownSeconds = 0.2f;
    [SerializeField] SkateRunnerAudioCue cashPickup = new SkateRunnerAudioCue();
    [Header("Phase Banners")]
    [SerializeField] SkateRunnerAudioCue phase1BannerImpact = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue phase2BannerImpact = new SkateRunnerAudioCue();
    [Header("Phase 2")]
    [SerializeField] SkateRunnerAudioCue phase2SlowMotionStart = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue ruthlessTapSwordHit = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue ruthlessFinalCut = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue phase2CarExplosion = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue phase2CarGroundImpact = new SkateRunnerAudioCue();
    

    [Header("Scenario Obstacles")]
    [SerializeField] SkateRunnerAudioCue compactorMove = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue compactorImpact = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue checkpointGateMove = new SkateRunnerAudioCue();
[SerializeField] SkateRunnerAudioCue phase2CountdownTick = new SkateRunnerAudioCue();
    [Header("Projectile Audio")]
    [SerializeField] SkateRunnerAudioCue phase2SniperGunshot = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue phase2SniperImpactOnPlayer = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue flyingDroneShot = new SkateRunnerAudioCue();
    [Header("Music Transitions")]
    [SerializeField, Min(0f)] float musicTransitionFade = 0.5f;
    [SerializeField, Min(0f)] float gameplayDeathFadeDuration = 0.25f;
    [SerializeField, Min(0f)] float gameplayOutroCrossfadeDuration = 0.4f;
    [Tooltip("Unscaled seconds after Ruthless Tap succeeds before the existing Intro/Body-to-Outro crossfade begins.")]
    [SerializeField, Min(0f)] float gameplayOutroDelayAfterRuthlessSeconds;
    // Preserve old prefab data without exposing obsolete audio-owned level timing.
    [SerializeField, HideInInspector] float gameplayMusicFadeOutDuration = 2f;
    [SerializeField, HideInInspector] float finalLandingWaitTimeout = 15f;
    [Header("Playback Settings")]
    [SerializeField, Range(4, 32)] int oneShotPoolSize = 12;
    [SerializeField, Range(4, 32)] int ruthlessTapPoolSize = 16;
    [SerializeField] string homepageSceneName = "SkateRunnerStartScreen";
    [SerializeField] string gameplaySceneName = "SkateRunner";

    readonly List<AudioSource> oneShots = new List<AudioSource>();
    readonly List<AudioSource> ruthlessTapSources = new List<AudioSource>();
    AudioSource musicA, musicB, musicOutro, announcerSource, wheelLoopSource, rewardRevealMusicSource;
    Coroutine musicRoutine;
    Coroutine pendingRankAudio;
    Coroutine pendingGameplayEnding;
    bool gameplayStarted;
    bool rewardRevealActive;
    int homepageIndex;
    int lastGameplayIndex = -1;
    int ruthlessVoiceIndex;
    float activeTrackVolume = 1f, musicFade = 1f, homepageGapRemaining;
    bool homepageInGap, musicWasEnabled, gameplayPlaybackBegun, endingRequested, gameplayRunDead;
    float outroFade = 1f;
    double musicIntroStartDsp, musicBodyStartDsp;
    GameplayMusicTrack selectedGameplayTrack;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance) return;
        GameObject prefab = Resources.Load<GameObject>("SkateRunnerAudio");
        GameObject go = prefab ? Instantiate(prefab) : new GameObject("SkateRunnerAudio");
        if (!go.GetComponent<SkateRunnerAudioManager>()) go.AddComponent<SkateRunnerAudioManager>();
    }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.name = "SkateRunnerAudio";
        DontDestroyOnLoad(gameObject);
        musicA = AddSource("Music A"); musicB = AddSource("Music B");
        musicOutro = AddSource("Gameplay Outro");
        announcerSource = AddSource("Arcade Announcer");
        announcerSource.priority = 16;
        announcerSource.ignoreListenerPause = true;
        announcerSource.dopplerLevel = 0f;
        wheelLoopSource = AddSource("Wheel Loop");
        rewardRevealMusicSource = AddSource("Crystal Reward Reveal Music");
        for (int i = 0; i < oneShotPoolSize; i++) oneShots.Add(AddSource("SFX " + (i + 1)));
        for (int i = 0; i < ruthlessTapPoolSize; i++)
        {
            var source = AddSource("Ruthless Tap " + (i + 1));
            source.priority = 32;
            // Tap feedback is real-time UI feedback, not slowed/paused world audio.
            // AudioSource already runs on the audio clock; never scale its pitch
            // with Time.timeScale, and let it play through listener-paused hitstop.
            source.ignoreListenerPause = true;
            source.dopplerLevel = 0f;
            ruthlessTapSources.Add(source);
        }
        // This latency-sensitive cue is authored with preload disabled. Warm it once,
        // not on the first accepted tap in the slow-motion sequence.
        if (ruthlessTapSwordHit != null && ruthlessTapSwordHit.clip) ruthlessTapSwordHit.clip.LoadAudioData();
    }

    AudioSource AddSource(string childName)
    {
        GameObject child = new GameObject(childName); child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0f;
        return source;
    }

    void OnEnable()
    {
        this.MMEventStartListening<MMGameEvent>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SkateRunnerDestructibleObject.OnDestroyed += OnDestructibleDestroyed;
        RuthlessTapModeController.CompletedSuccessfully += OnRuthlessCompleted;
        SoundManager.SettingsChanged += OnSettingsChanged;
    }

    void Start() { OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); }

    void OnDisable()
    {
        this.MMEventStopListening<MMGameEvent>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SkateRunnerDestructibleObject.OnDestroyed -= OnDestructibleDestroyed;
        RuthlessTapModeController.CompletedSuccessfully -= OnRuthlessCompleted;
        SoundManager.SettingsChanged -= OnSettingsChanged;
        CancelPendingRankAudio();
        CancelPendingGameplayEnding();
        CancelMusicRoutine();
        StopMusicSources();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterSceneButtons(scene);
        if (mode == LoadSceneMode.Additive) return;
        ResetCashPickupSoundCooldown();
        CancelPendingRankAudio();
        CancelPendingGameplayEnding();
        StopCrystalRewardAudioInternal();
        gameplayStarted = false;
        gameplayPlaybackBegun = endingRequested = gameplayRunDead = false;
        selectedGameplayTrack = null;
        homepageIndex = 0;
        homepageInGap = false;
        homepageGapRemaining = 0f;
        musicWasEnabled = MusicEnabled;
        if (scene.name == homepageSceneName) StartHomepageMusic(); else StopMusic(musicTransitionFade);
    }

    void RegisterSceneButtons(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                RegisterRuntimeButton(button);
            foreach (MMTouchButton touch in root.GetComponentsInChildren<MMTouchButton>(true))
                SkateRunnerUIClickAudio.Ensure(touch.gameObject);
            foreach (UIClickToggle tab in root.GetComponentsInChildren<UIClickToggle>(true))
                SkateRunnerUIClickAudio.Ensure(tab.gameObject);
        }
    }

    public void RegisterRuntimeButton(Button button)
    { if (button) SkateRunnerUIClickAudio.Ensure(button.gameObject); }

    public void OnMMEvent(MMGameEvent e)
    {
        switch (e.EventName)
        {
            case "GameStart": ResetCashPickupSoundCooldown(); CancelPendingRankAudio(); CancelPendingGameplayEnding(); StartGameplayMusic(); break;
            case "Jump": Play(playerJump); break;
            case "LifeLost":
                CancelPendingRankAudio();
                CancelPendingGameplayEnding();
                Play(playerDeath);
                HandleGameplayDeath();
                break;
        }
    }

    bool SfxEnabled => SoundManager.Instance && SoundManager.Instance.Settings != null && SoundManager.Instance.Settings.SfxOn;
    bool MusicEnabled => SoundManager.Instance && SoundManager.Instance.Settings != null && SoundManager.Instance.Settings.MusicOn;
    float SfxVolume => SoundManager.Instance ? SoundManager.Instance.SfxVolume : 1f;
    float MusicVolume => SoundManager.Instance ? SoundManager.Instance.MusicVolume : 1f;

    void OnSettingsChanged(bool musicOn, bool sfxOn)
    {
        ApplyMusicVolume();
        rewardRevealMusicSource.volume = crystalRewardRevealMusicVolume * MusicVolume;
        announcerSource.volume = SfxVolume;
        if (!sfxOn)
        {
            CancelPendingRankAudio();
            wheelLoopSource.Stop();
            foreach (var source in oneShots) source.Stop();
            foreach (var source in ruthlessTapSources) source.Stop();
            announcerSource.Stop();
        }
        if (!musicOn)
        {
            CancelMusicRoutine();
            StopMusicSources();
            rewardRevealMusicSource.Stop();
        }
        if (musicOn && rewardRevealActive && crystalRewardRevealMusic && !rewardRevealMusicSource.isPlaying)
            PlayCrystalRewardMusic();
        if (musicOn && !musicWasEnabled && !endingRequested && !gameplayRunDead)
        {
            if (gameplayStarted) StartSelectedGameplayMusic();
            else if (SceneManager.GetActiveScene().name == homepageSceneName) StartHomepageMusic();
        }
        musicWasEnabled = musicOn;
    }

    void Play(SkateRunnerAudioCue cue, bool ignoreRetrigger = false)
    {
        if (!SfxEnabled || cue == null || !cue.clip || (!ignoreRetrigger && Time.unscaledTime - cue.lastPlayedAt < cue.minimumRetriggerInterval)) return;
        cue.lastPlayedAt = Time.unscaledTime;
        AudioSource source = null;
        for (int i = 0; i < oneShots.Count; i++) if (!oneShots[i].isPlaying) { source = oneShots[i]; break; }
        if (!source) source = oneShots[0];
        source.clip = cue.clip; source.loop = false; source.mute = false;
        source.volume = Mathf.Clamp01(cue.volume * SfxVolume);
        source.pitch = Mathf.Max(0.01f, cue.pitch + UnityEngine.Random.Range(-cue.randomPitchRange, cue.randomPitchRange));
        source.Play();
    }

    public static void PlayUIButtonClick() => Instance?.Play(Instance.uiButtonClick, true);
    public static void PlayLevelEndMultiplierFocus() => Instance?.Play(Instance.levelEndMultiplierFocus);
    public static void PlayPurchaseSuccess() => Instance?.Play(Instance.purchaseSuccess);
    public static void StartCrystalRewardRevealAudio() => Instance?.StartCrystalRewardAudioInternal();
    public static void PlayCrystalChestBreak() => Instance?.Play(Instance.crystalChestBreak);
    public static void PlayRewardReveal() => Instance?.Play(Instance.rewardReveal);
    public static void StopCrystalRewardRevealAudio() => Instance?.StopCrystalRewardAudioInternal();
    public static void PlayDashAttack() => Instance?.Play(Instance.dashAttack);
    public static void PlayDownAttack() => Instance?.Play(Instance.downAttack);
    public static void PlayCashPickup() => Instance?.PlayCashPickupInternal();

    void PlayCashPickupInternal()
    {
        if (cashPickup == null) return;
        if (enableCashPickupSoundCooldown &&
            Time.unscaledTime - cashPickup.lastPlayedAt < Mathf.Max(0f, cashPickupSoundCooldownSeconds)) return;

        // Play updates lastPlayedAt only when SFX is enabled and a clip exists.
        // Bypass the generic cue gate so the checkbox is the sole cash limiter.
        Play(cashPickup, true);
    }

    void ResetCashPickupSoundCooldown()
    {
        if (cashPickup != null) cashPickup.lastPlayedAt = float.NegativeInfinity;
    }
    public static void PlayPhase1BannerImpact() => Instance?.Play(Instance.phase1BannerImpact);
    public static void PlayPhase2BannerImpact() => Instance?.Play(Instance.phase2BannerImpact);
    public static void PlayPhase2SlowMotionStart() => Instance?.Play(Instance.phase2SlowMotionStart);
    public static void PlayRuthlessTapSwordHit() => Instance?.PlayRuthlessTapInternal();
    public static void PlayRuthlessFinalCut() => Instance?.Play(Instance.ruthlessFinalCut);
    public static void PlayPhase2CarExplosion() => Instance?.Play(Instance.phase2CarExplosion);
    public static void PlayPhase2CarGroundImpact() => Instance?.Play(Instance.phase2CarGroundImpact);
    
    public static void PlayCompactorMove() => Instance?.Play(Instance.compactorMove);
    public static void PlayCompactorImpact() => Instance?.Play(Instance.compactorImpact);
    public static void PlayCheckpointGateMove() => Instance?.Play(Instance.checkpointGateMove);
public static void PlayPhase2CountdownTick() => Instance?.Play(Instance.phase2CountdownTick);
    public static void PlayPhase2SniperGunshot() => Instance?.Play(Instance.phase2SniperGunshot);
    public static void PlayPhase2SniperImpact() => Instance?.Play(Instance.phase2SniperImpactOnPlayer);
    public static void PlayFlyingDroneShot() => Instance?.Play(Instance.flyingDroneShot);
    public static void PlayPowerslamAnnouncer() => Instance?.PlayAnnouncer(Instance.powerslamAnnouncer);

    void PlayAnnouncer(SkateRunnerAudioCue cue)
    {
        if (!SfxEnabled || cue == null || !cue.clip || !announcerSource) return;
        announcerSource.mute = false;
        announcerSource.volume = SfxVolume;
        announcerSource.pitch = Mathf.Max(0.01f, cue.pitch + UnityEngine.Random.Range(-cue.randomPitchRange, cue.randomPitchRange));
        // Dedicated reusable voice: combat-pool voice stealing cannot cut this off.
        announcerSource.PlayOneShot(cue.clip, Mathf.Clamp01(cue.volume));
    }

    void PlayRuthlessTapInternal()
    {
        var cue = ruthlessTapSwordHit;
        if (!SfxEnabled || cue == null || !cue.clip || ruthlessTapSources.Count == 0) return;
        // PlayOneShot overlaps even when this preallocated voice wraps. No Stop/Play
        // restart, no shared-pool voice stealing, and no legitimate-tap throttling.
        var source = ruthlessTapSources[ruthlessVoiceIndex++ % ruthlessTapSources.Count];
        source.mute = false;
        source.volume = Mathf.Clamp01(cue.volume * SfxVolume);
        source.pitch = Mathf.Max(0.01f, cue.pitch + UnityEngine.Random.Range(-cue.randomPitchRange, cue.randomPitchRange));
        source.PlayOneShot(cue.clip);
    }

    float CurrentBackgroundMusicVolume => activeTrackVolume * MusicVolume * musicFade *
        (rewardRevealActive ? rewardRevealBackgroundMusicDuck : 1f);

    void ApplyMusicVolume()
    {
        if (musicA) { musicA.volume = CurrentBackgroundMusicVolume; musicA.mute = !MusicEnabled; }
        if (musicB) { musicB.volume = CurrentBackgroundMusicVolume; musicB.mute = !MusicEnabled; }
        if (musicOutro)
        {
            musicOutro.volume = activeTrackVolume * MusicVolume * outroFade *
                (rewardRevealActive ? rewardRevealBackgroundMusicDuck : 1f);
            musicOutro.mute = !MusicEnabled;
        }
    }

    void StartCrystalRewardAudioInternal()
    {
        if (rewardRevealActive) return;
        rewardRevealActive = true;
        ApplyMusicVolume();
        PlayCrystalRewardMusic();
    }

    void PlayCrystalRewardMusic()
    {
        if (!MusicEnabled || !crystalRewardRevealMusic || !rewardRevealMusicSource) return;
        rewardRevealMusicSource.clip = crystalRewardRevealMusic;
        rewardRevealMusicSource.loop = true;
        rewardRevealMusicSource.pitch = 1f;
        rewardRevealMusicSource.volume = crystalRewardRevealMusicVolume * MusicVolume;
        rewardRevealMusicSource.mute = !MusicEnabled;
        rewardRevealMusicSource.Play();
    }

    void StopCrystalRewardAudioInternal()
    {
        rewardRevealActive = false;
        if (rewardRevealMusicSource) rewardRevealMusicSource.Stop();
        ApplyMusicVolume();
    }

    public static void StartWheelSpin() { if (Instance) Instance.StartLoop(Instance.wheelLoopSource, Instance.wheelSpinningLoop); }
    public static void StopWheelSpinAndLand() { if (!Instance) return; StopWheelSpin(); Instance.Play(Instance.wheelWinningLanding); }
    public static void StopWheelSpin() { if (Instance && Instance.wheelLoopSource) Instance.wheelLoopSource.Stop(); }
    void StartLoop(AudioSource source, SkateRunnerAudioCue cue)
    { if (!SfxEnabled || cue == null || !cue.clip || source.isPlaying) return; source.clip = cue.clip; source.loop = true; source.volume = cue.volume * SfxVolume; source.pitch = cue.pitch; source.Play(); }

    void OnDestructibleDestroyed(SkateRunnerDestructibleObject destroyed)
    {
        if (!destroyed) return;
        var kind = destroyed.ResolveAudioKind();
        if (kind == DestructibleAudioKind.Barrel) { Play(barrelDestruction, true); return; }
        if (!destroyed.CountsAsEnemyKill) return;
        switch (kind)
        {
            case DestructibleAudioKind.FlyingDrone: PlayEnemyDeath(flyingDroneEnemyDeath, flyingDroneEnemyDeathSupport); break;
            case DestructibleAudioKind.EnemyType1: PlayEnemyDeath(enemyType1Death, enemyType1DeathSupport); break;
            case DestructibleAudioKind.EnemyType2: PlayEnemyDeath(enemyType2Death, enemyType2DeathSupport); break;
            case DestructibleAudioKind.EnemyType3: PlayEnemyDeath(enemyType3Death, enemyType3DeathSupport); break;
            default: PlayEnemyDeath(unknownEnemyDeathFallback, unknownEnemyDeathFallbackSupport); break;
        }
    }

    void PlayEnemyDeath(SkateRunnerAudioCue mainCue, SkateRunnerAudioCue supportCue)
    {
        Play(mainCue, true);
        Play(supportCue, true);
    }

    void StartHomepageMusic()
    {
        gameplayStarted = false;
        CancelMusicRoutine();
        StopMusicSources();
        if (MusicEnabled) musicRoutine = StartCoroutine(HomepagePlaylist());
    }

    void StartGameplayMusic()
    {
        // ResetLevel -> PrepareStart -> LevelStart emits GameStart after a real retry.
        // Pause/UI changes do not reset the selected package or restart the music.
        if (SceneManager.GetActiveScene().name != gameplaySceneName) return;
        if (gameplayStarted)
        {
            if (!gameplayRunDead) return;
            gameplayRunDead = endingRequested = gameplayPlaybackBegun = false;
            StartSelectedGameplayMusic();
            return;
        }
        gameplayStarted = true;
        gameplayRunDead = false;
        var valid = gameplayTracks.FindAll(x => x != null && x.IsValid);
        if (valid.Count == 0) { StopMusic(musicTransitionFade); return; }
        int index;
        do index = UnityEngine.Random.Range(0, valid.Count); while (valid.Count > 1 && index == lastGameplayIndex);
        lastGameplayIndex = index;
        selectedGameplayTrack = valid[index];
        StartSelectedGameplayMusic();
    }

    void CancelMusicRoutine()
    {
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = null;
    }

    void StopMusicSources(bool includeOutro = true)
    {
        ResetMusicSource(musicA);
        ResetMusicSource(musicB);
        if (includeOutro) ResetMusicSource(musicOutro);
    }

    static void ResetMusicSource(AudioSource source)
    {
        if (!source) return;
        source.Stop(); // also cancels scheduled starts/ends
        source.loop = false;
        source.clip = null;
    }

    static double ClipSeconds(AudioClip clip) => clip ? (double)clip.samples / clip.frequency : 0;

    void Schedule(AudioSource source, AudioClip clip, double start, bool loop)
    {
        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.pitch = 1f;
        ApplyMusicVolume();
        if (clip) source.PlayScheduled(start);
    }

    IEnumerator HomepagePlaylist()
    {
        var entries = new List<HomepageMusicEntry>(2);
        if (homepageTrack1 != null && homepageTrack1.clip) entries.Add(homepageTrack1);
        if (homepageTrack2 != null && homepageTrack2.clip) entries.Add(homepageTrack2);
        if (entries.Count == 0) yield break;
        while (MusicEnabled && !gameplayStarted)
        {
            while (homepageInGap && homepageGapRemaining > 0)
            {
                yield return null;
                homepageGapRemaining -= Time.unscaledDeltaTime;
            }
            homepageInGap = false;
            var entry = entries[homepageIndex % entries.Count];
            activeTrackVolume = Mathf.Clamp01(entry.volume);
            musicFade = 1f;
            for (int play = 0; play < entry.TotalPlays; play++)
            {
                double start = AudioSettings.dspTime + 0.05;
                Schedule(musicA, entry.clip, start, false);
                double deadline = Time.realtimeSinceStartupAsDouble + ClipSeconds(entry.clip) + 5;
                while (AudioSettings.dspTime < start + ClipSeconds(entry.clip) && Time.realtimeSinceStartupAsDouble < deadline)
                    yield return null;
                musicA.Stop();
            }
            homepageIndex = (homepageIndex + 1) % entries.Count;
            homepageInGap = true;
            homepageGapRemaining = Mathf.Max(0f, homepageMusicGapSeconds);
        }
    }

    void StartSelectedGameplayMusic()
    {
        CancelMusicRoutine();
        StopMusicSources();
        if (!MusicEnabled || selectedGameplayTrack == null || endingRequested || gameplayRunDead) return;
        var track = selectedGameplayTrack;
        activeTrackVolume = Mathf.Clamp01(track.volume);
        musicFade = 1f;
        outroFade = 1f;
        double start = AudioSettings.dspTime + 0.1;
        bool intro = !gameplayPlaybackBegun && track.intro;
        musicIntroStartDsp = intro ? start : 0;
        musicBodyStartDsp = intro ? start + ClipSeconds(track.intro) : start;
        // Separate scheduled sources make the Intro -> Body handoff DSP timed.
        if (intro) Schedule(musicA, track.intro, start, false);
        if (track.body) Schedule(musicB, track.body, musicBodyStartDsp, true);
        gameplayPlaybackBegun = true;
    }

    void StopMusic(float fade)
    {
        CancelMusicRoutine();
        musicRoutine = StartCoroutine(FadeAndStop(fade));
    }

    IEnumerator FadeAndStop(float duration, bool includeOutro = true)
    {
        float initial = musicFade;
        float initialOutro = outroFade;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            musicFade = Mathf.Lerp(initial, 0, t / duration);
            if (includeOutro) outroFade = Mathf.Lerp(initialOutro, 0, t / duration);
            ApplyMusicVolume();
            yield return null;
        }
        StopMusicSources(includeOutro);
        musicFade = 1f;
        if (includeOutro) outroFade = 1f;
        ApplyMusicVolume();
    }

    void HandleGameplayDeath()
    {
        if (!gameplayStarted || gameplayRunDead) return;
        CancelPendingGameplayEnding();
        gameplayRunDead = true;
        gameplayPlaybackBegun = false;
        endingRequested = false;
        CancelMusicRoutine();
        // Do not let a scheduled Body begin during the death fade.
        CancelPendingGameplaySections();
        musicRoutine = StartCoroutine(FadeAndStop(gameplayDeathFadeDuration));
    }

    void OnRuthlessCompleted(int finalCount)
    {
        CancelPendingRankAudio();
        if (gameplayRunDead || !isActiveAndEnabled) return;
        ScheduleGameplayEnding();
        if (RuthlessTapModeController.RankForCount(finalCount) == RuthlessComboRank.None) return;
        if (rankAudioDelaySeconds <= 0f) PlayRankAnnouncer(finalCount);
        else pendingRankAudio = StartCoroutine(PlayRankAnnouncerAfterDelay(finalCount, rankAudioDelaySeconds));
    }

    IEnumerator PlayRankAnnouncerAfterDelay(int finalCount, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        pendingRankAudio = null;
        if (!gameplayRunDead) PlayRankAnnouncer(finalCount);
    }

    void CancelPendingRankAudio()
    {
        if (pendingRankAudio != null) StopCoroutine(pendingRankAudio);
        pendingRankAudio = null;
    }

    void PlayRankAnnouncer(int finalCount)
    {
        switch (RuthlessTapModeController.RankForCount(finalCount))
        {
            case RuthlessComboRank.KillerAssassin: PlayAnnouncer(killerAssassinAnnouncer); break;
            case RuthlessComboRank.Brutal: PlayAnnouncer(brutalAnnouncer); break;
            case RuthlessComboRank.Ruthless: PlayAnnouncer(ruthlessAnnouncer); break;
        }
    }

    void ScheduleGameplayEnding()
    {
        if (!gameplayStarted || gameplayRunDead || endingRequested || pendingGameplayEnding != null) return;

        float delay = Mathf.Max(0f, gameplayOutroDelayAfterRuthlessSeconds);
        if (delay <= 0f)
        {
            RequestGameplayEnding();
            return;
        }

        pendingGameplayEnding = StartCoroutine(RequestGameplayEndingAfterDelay(delay));
    }

    IEnumerator RequestGameplayEndingAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        pendingGameplayEnding = null;
        RequestGameplayEnding();
    }

    void CancelPendingGameplayEnding()
    {
        if (pendingGameplayEnding != null) StopCoroutine(pendingGameplayEnding);
        pendingGameplayEnding = null;
    }

    void RequestGameplayEnding()
    {
        if (!gameplayStarted || gameplayRunDead || endingRequested) return;
        pendingGameplayEnding = null;
        endingRequested = true;
        CancelMusicRoutine();
        // A scheduled Body must never wake up underneath the Outro.
        CancelPendingGameplaySections();
        ResetMusicSource(musicOutro);
        if (MusicEnabled && selectedGameplayTrack != null && selectedGameplayTrack.outro)
        {
            outroFade = 1f;
            musicOutro.clip = selectedGameplayTrack.outro;
            musicOutro.pitch = 1f;
            musicOutro.loop = false;
            ApplyMusicVolume();
            musicOutro.Play(); // same successful-completion callback, no clip-boundary wait
        }
        // Only the old Intro/Body fades. Outro starts at normal track volume.
        // No Outro, mute or load failure can affect the gameplay-owned result timer.
        musicRoutine = StartCoroutine(FadeAndStop(gameplayOutroCrossfadeDuration, false));
    }

    void CancelPendingGameplaySections()
    {
        double now = AudioSettings.dspTime;
        if (now < musicIntroStartDsp) ResetMusicSource(musicA);
        if (now < musicBodyStartDsp) ResetMusicSource(musicB);
        if (musicB) musicB.loop = false;
    }
}
