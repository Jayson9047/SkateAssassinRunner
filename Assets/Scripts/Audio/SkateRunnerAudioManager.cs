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
    [Header("Currency Pickups")]
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
    [SerializeField] SkateRunnerAudioCue phase2CountdownTick = new SkateRunnerAudioCue();
    [Header("Projectile Audio")]
    [SerializeField] SkateRunnerAudioCue phase2SniperGunshot = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue phase2SniperImpactOnPlayer = new SkateRunnerAudioCue();
    [SerializeField] SkateRunnerAudioCue flyingDroneShot = new SkateRunnerAudioCue();
    [Header("Music Transitions")]
    [SerializeField, Min(0f)] float musicTransitionFade = 0.5f;
    [SerializeField, Min(0f)] float gameplayMusicFadeOutDuration = 2f;
    [SerializeField, Min(1f)] float finalLandingWaitTimeout = 15f;
    [Header("Playback Settings")]
    [SerializeField, Range(4, 32)] int oneShotPoolSize = 12;
    [SerializeField, Range(4, 32)] int ruthlessTapPoolSize = 16;
    [SerializeField] string homepageSceneName = "SkateRunnerStartScreen";
    [SerializeField] string gameplaySceneName = "SkateRunner";

    readonly List<AudioSource> oneShots = new List<AudioSource>();
    readonly List<AudioSource> ruthlessTapSources = new List<AudioSource>();
    AudioSource musicA, musicB, wheelLoopSource, rewardRevealMusicSource;
    Coroutine musicRoutine;
    bool gameplayStarted;
    bool rewardRevealActive;
    int homepageIndex;
    int lastGameplayIndex = -1;
    int ruthlessVoiceIndex;
    float activeTrackVolume = 1f, musicFade = 1f, homepageGapRemaining;
    bool homepageInGap, musicWasEnabled, gameplayPlaybackBegun, endingRequested;
    GameplayMusicTrack selectedGameplayTrack;
    double introEndDsp, bodyStartDsp, endingDeadlineRealtime;

    public enum MusicEndingState { NotRequested, WaitingForBoundary, PlayingOutro, Completed, Fallback }
    public MusicEndingState EndingState { get; private set; }
    public event Action GameplayMusicEndingCompleted;

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
        SkateRunnerDestructibleObject.OnEnemyKilled += OnEnemyKilled;
        SoundManager.SettingsChanged += OnSettingsChanged;
    }

    void Start() { OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); }

    void OnDisable()
    {
        this.MMEventStopListening<MMGameEvent>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SkateRunnerDestructibleObject.OnEnemyKilled -= OnEnemyKilled;
        SoundManager.SettingsChanged -= OnSettingsChanged;
        CancelMusicRoutine();
        StopMusicSources();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterSceneButtons(scene);
        if (mode == LoadSceneMode.Additive) return;
        StopCrystalRewardAudioInternal();
        gameplayStarted = false;
        gameplayPlaybackBegun = endingRequested = false;
        selectedGameplayTrack = null;
        EndingState = MusicEndingState.NotRequested;
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
            case "GameStart": StartGameplayMusic(); break;
            case "Jump": Play(playerJump); break;
            case "LifeLost": Play(playerDeath); break;
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
        if (!sfxOn)
        {
            wheelLoopSource.Stop();
            foreach (var source in oneShots) source.Stop();
            foreach (var source in ruthlessTapSources) source.Stop();
        }
        if (!musicOn)
        {
            CancelMusicRoutine();
            StopMusicSources();
            rewardRevealMusicSource.Stop();
            if (endingRequested) CompleteEnding(false);
        }
        if (musicOn && rewardRevealActive && crystalRewardRevealMusic && !rewardRevealMusicSource.isPlaying)
            PlayCrystalRewardMusic();
        if (musicOn && !musicWasEnabled && !endingRequested)
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
    public static void PlayPurchaseSuccess() => Instance?.Play(Instance.purchaseSuccess);
    public static void StartCrystalRewardRevealAudio() => Instance?.StartCrystalRewardAudioInternal();
    public static void PlayCrystalChestBreak() => Instance?.Play(Instance.crystalChestBreak);
    public static void PlayRewardReveal() => Instance?.Play(Instance.rewardReveal);
    public static void StopCrystalRewardRevealAudio() => Instance?.StopCrystalRewardAudioInternal();
    public static void PlayDashAttack() => Instance?.Play(Instance.dashAttack);
    public static void PlayDownAttack() => Instance?.Play(Instance.downAttack);
    public static void PlayCashPickup() => Instance?.Play(Instance.cashPickup);
    public static void PlayPhase1BannerImpact() => Instance?.Play(Instance.phase1BannerImpact);
    public static void PlayPhase2BannerImpact() => Instance?.Play(Instance.phase2BannerImpact);
    public static void PlayPhase2SlowMotionStart() => Instance?.Play(Instance.phase2SlowMotionStart);
    public static void PlayRuthlessTapSwordHit() => Instance?.PlayRuthlessTapInternal();
    public static void PlayRuthlessFinalCut() => Instance?.Play(Instance.ruthlessFinalCut);
    public static void PlayPhase2CarExplosion() => Instance?.Play(Instance.phase2CarExplosion);
    public static void PlayPhase2CarGroundImpact() => Instance?.Play(Instance.phase2CarGroundImpact);
    public static void PlayPhase2CountdownTick() => Instance?.Play(Instance.phase2CountdownTick);
    public static void PlayPhase2SniperGunshot() => Instance?.Play(Instance.phase2SniperGunshot);
    public static void PlayPhase2SniperImpact() => Instance?.Play(Instance.phase2SniperImpactOnPlayer);
    public static void PlayFlyingDroneShot() => Instance?.Play(Instance.flyingDroneShot);

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
        (rewardRevealActive && crystalRewardRevealMusic ? rewardRevealBackgroundMusicDuck : 1f);

    void ApplyMusicVolume()
    {
        if (musicA) { musicA.volume = CurrentBackgroundMusicVolume; musicA.mute = !MusicEnabled; }
        if (musicB) { musicB.volume = CurrentBackgroundMusicVolume; musicB.mute = !MusicEnabled; }
    }

    void StartCrystalRewardAudioInternal()
    {
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

    void OnEnemyKilled(SkateRunnerDestructibleObject enemy)
    {
        if (!enemy) return;
        if (enemy.GetComponentInParent<EnemyType1>()) Play(enemyType1Death);
        else if (enemy.GetComponentInParent<EnemyType2>()) Play(enemyType2Death);
        else if (enemy.GetComponentInParent<EnemyType3>()) Play(enemyType3Death);
        else if (enemy.GetComponentInParent<EnemyTypeDrone>()) Play(flyingDroneEnemyDeath);
        else Play(unknownEnemyDeathFallback);
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
        // GameStart may be emitted again on revive. Selection is level-owned.
        if (gameplayStarted || SceneManager.GetActiveScene().name != gameplaySceneName) return;
        gameplayStarted = true;
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

    void StopMusicSources()
    {
        if (musicA) { musicA.Stop(); musicA.loop = false; }
        if (musicB) { musicB.Stop(); musicB.loop = false; }
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
        if (!MusicEnabled || selectedGameplayTrack == null || endingRequested) return;
        var track = selectedGameplayTrack;
        activeTrackVolume = Mathf.Clamp01(track.volume);
        musicFade = 1f;
        double start = AudioSettings.dspTime + 0.1;
        bool intro = !gameplayPlaybackBegun && track.intro;
        introEndDsp = intro ? start + ClipSeconds(track.intro) : 0;
        bodyStartDsp = intro ? introEndDsp : start;
        // Separate scheduled sources make the Intro -> Body handoff DSP timed.
        if (intro) Schedule(musicA, track.intro, start, false);
        if (track.body) Schedule(musicB, track.body, bodyStartDsp, true);
        gameplayPlaybackBegun = true;
    }

    void StopMusic(float fade)
    {
        CancelMusicRoutine();
        musicRoutine = StartCoroutine(FadeAndStop(fade));
    }

    IEnumerator FadeAndStop(float duration)
    {
        float initial = musicFade;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            musicFade = Mathf.Lerp(initial, 0, t / duration);
            ApplyMusicVolume();
            yield return null;
        }
        StopMusicSources();
        musicFade = 1f;
        ApplyMusicVolume();
    }

    public static void EndGameplayMusicAtFinalLanding() => Instance?.RequestGameplayEnding();

    void RequestGameplayEnding()
    {
        if (endingRequested) return;
        endingRequested = true;
        CancelMusicRoutine();
        if (!MusicEnabled || selectedGameplayTrack == null || !selectedGameplayTrack.outro)
        {
            CompleteEnding(false);
            musicRoutine = StartCoroutine(FadeAndStop(gameplayMusicFadeOutDuration));
            return;
        }
        // Establish a watchdog before scheduling, so even an unexpectedly aborted
        // audio coroutine cannot strand a presentation waiter on another component.
        endingDeadlineRealtime = Time.realtimeSinceStartupAsDouble + ClipSeconds(selectedGameplayTrack.intro) +
            ClipSeconds(selectedGameplayTrack.body) + ClipSeconds(selectedGameplayTrack.outro) + 5;
        musicRoutine = StartCoroutine(PlayGameplayEnding());
    }

    IEnumerator PlayGameplayEnding()
    {
        var track = selectedGameplayTrack;
        EndingState = MusicEndingState.WaitingForBoundary;
        double now = AudioSettings.dspTime;
        double boundary = now + 0.05;
        if (track.body && musicB.isPlaying && now >= bodyStartDsp)
        {
            double length = ClipSeconds(track.body);
            boundary = bodyStartDsp + (Math.Floor((now - bodyStartDsp) / length) + 1) * length;
            musicB.loop = false;
            musicB.SetScheduledEndTime(boundary);
        }
        else
        {
            // Landing during Intro: cancel the not-yet-started Body, finish Intro.
            musicB.Stop();
            if (musicA.isPlaying && introEndDsp > now) boundary = introEndDsp;
        }
        // Keep Intro alive to its boundary. Reuse the idle source for Outro.
        AudioSource outroSource = musicA.isPlaying && introEndDsp > now ? musicB : musicA;
        Schedule(outroSource, track.outro, boundary, false);
        double end = boundary + ClipSeconds(track.outro);
        endingDeadlineRealtime = Time.realtimeSinceStartupAsDouble + (end - now) + 5;
        while (AudioSettings.dspTime < end)
        {
            if (!MusicEnabled || Time.realtimeSinceStartupAsDouble > endingDeadlineRealtime ||
                (AudioSettings.dspTime > boundary + 0.15 && !outroSource.isPlaying))
            {
                StopMusicSources();
                CompleteEnding(false);
                yield break;
            }
            if (AudioSettings.dspTime >= boundary) EndingState = MusicEndingState.PlayingOutro;
            yield return null;
        }
        StopMusicSources();
        CompleteEnding(true);
    }

    void CompleteEnding(bool musicalEnding)
    {
        var state = musicalEnding ? MusicEndingState.Completed : MusicEndingState.Fallback;
        if (EndingState == state) return;
        EndingState = state;
        GameplayMusicEndingCompleted?.Invoke();
    }

    /// <summary>Presentation-only awaitable; no success/save/reward bookkeeping.</summary>
    public static IEnumerator WaitForLevelEndingPresentation(float fallbackSeconds = 6f)
    {
        double started = Time.realtimeSinceStartupAsDouble;
        var manager = Instance;
        if (manager)
        {
            while (manager && manager.isActiveAndEnabled && manager.MusicEnabled && manager.selectedGameplayTrack != null && manager.selectedGameplayTrack.outro)
            {
                if (manager.EndingState == MusicEndingState.Completed) yield break;
                if (manager.EndingState == MusicEndingState.Fallback) break;
                if (manager.endingRequested && Time.realtimeSinceStartupAsDouble > manager.endingDeadlineRealtime)
                {
                    manager.StopMusic(manager.gameplayMusicFadeOutDuration);
                    manager.CompleteEnding(false);
                    break;
                }
                if (!manager.endingRequested && Time.realtimeSinceStartupAsDouble - started >= manager.finalLandingWaitTimeout)
                {
                    // Missing landing callback must never strand the result screen.
                    manager.endingRequested = true;
                    manager.StopMusic(manager.gameplayMusicFadeOutDuration);
                    manager.CompleteEnding(false);
                    break;
                }
                yield return null;
            }
        }
        while (Time.realtimeSinceStartupAsDouble - started < Mathf.Max(0f, fallbackSeconds)) yield return null;
    }
}
