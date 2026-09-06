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
    [SerializeField] AudioClip homepageMusic1;
    [SerializeField] AudioClip homepageMusic2;
    [Header("Gameplay Music")]
    [SerializeField] List<AudioClip> gameplayMusicTracks = new List<AudioClip>();
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
    [SerializeField] AudioClip levelEndingOutroStinger;
    [Header("Playback Settings")]
    [SerializeField, Range(4, 32)] int oneShotPoolSize = 12;
    [SerializeField] string homepageSceneName = "SkateRunnerStartScreen";
    [SerializeField] string gameplaySceneName = "SkateRunner";

    readonly List<AudioSource> oneShots = new List<AudioSource>();
    readonly HashSet<Button> registeredButtons = new HashSet<Button>();
    AudioSource musicA, musicB, wheelLoopSource, stingerSource, rewardRevealMusicSource;
    Coroutine musicRoutine;
    bool gameplayStarted;
    bool rewardRevealActive;
    int homepageIndex;
    int lastGameplayIndex = -1;

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
        wheelLoopSource = AddSource("Wheel Loop"); stingerSource = AddSource("Ending Stinger");
        rewardRevealMusicSource = AddSource("Crystal Reward Reveal Music");
        for (int i = 0; i < oneShotPoolSize; i++) oneShots.Add(AddSource("SFX " + (i + 1)));
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
        UnregisterButtons();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopCrystalRewardAudioInternal();
        gameplayStarted = false;
        RegisterSceneButtons(scene);
        if (scene.name == homepageSceneName) StartHomepageMusic(); else StopMusic(musicTransitionFade);
    }

    void RegisterSceneButtons(Scene scene)
    {
        UnregisterButtons();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            { if (registeredButtons.Add(button)) button.onClick.AddListener(PlayUIButtonClick); }
    }

    void UnregisterButtons()
    {
        foreach (Button button in registeredButtons) if (button) button.onClick.RemoveListener(PlayUIButtonClick);
        registeredButtons.Clear();
    }

    public void RegisterRuntimeButton(Button button)
    { if (button && registeredButtons.Add(button)) button.onClick.AddListener(PlayUIButtonClick); }

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
        musicA.mute = musicB.mute = stingerSource.mute = rewardRevealMusicSource.mute = !musicOn;
        musicA.volume = musicB.volume = CurrentBackgroundMusicVolume;
        rewardRevealMusicSource.volume = crystalRewardRevealMusicVolume * MusicVolume;
        if (!sfxOn && wheelLoopSource) wheelLoopSource.Stop();
        if (musicOn && rewardRevealActive && crystalRewardRevealMusic && !rewardRevealMusicSource.isPlaying)
            PlayCrystalRewardMusic();
        if (musicOn && !musicA.isPlaying && !musicB.isPlaying && !stingerSource.isPlaying)
        { if (gameplayStarted) StartGameplayMusic(false); else if (SceneManager.GetActiveScene().name == homepageSceneName) StartHomepageMusic(); }
    }

    void Play(SkateRunnerAudioCue cue)
    {
        if (!SfxEnabled || cue == null || !cue.clip || Time.unscaledTime - cue.lastPlayedAt < cue.minimumRetriggerInterval) return;
        cue.lastPlayedAt = Time.unscaledTime;
        AudioSource source = null;
        for (int i = 0; i < oneShots.Count; i++) if (!oneShots[i].isPlaying) { source = oneShots[i]; break; }
        if (!source) source = oneShots[0];
        source.clip = cue.clip; source.loop = false; source.mute = false;
        source.volume = Mathf.Clamp01(cue.volume * SfxVolume);
        source.pitch = Mathf.Max(0.01f, cue.pitch + UnityEngine.Random.Range(-cue.randomPitchRange, cue.randomPitchRange));
        source.Play();
    }

    public static void PlayUIButtonClick() => Instance?.Play(Instance.uiButtonClick);
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
    public static void PlayRuthlessTapSwordHit() => Instance?.Play(Instance.ruthlessTapSwordHit);
    public static void PlayRuthlessFinalCut() => Instance?.Play(Instance.ruthlessFinalCut);
    public static void PlayPhase2CarExplosion() => Instance?.Play(Instance.phase2CarExplosion);
    public static void PlayPhase2CarGroundImpact() => Instance?.Play(Instance.phase2CarGroundImpact);
    public static void PlayPhase2CountdownTick() => Instance?.Play(Instance.phase2CountdownTick);
    public static void PlayPhase2SniperGunshot() => Instance?.Play(Instance.phase2SniperGunshot);
    public static void PlayPhase2SniperImpact() => Instance?.Play(Instance.phase2SniperImpactOnPlayer);
    public static void PlayFlyingDroneShot() => Instance?.Play(Instance.flyingDroneShot);

    float CurrentBackgroundMusicVolume => MusicVolume * (rewardRevealActive && crystalRewardRevealMusic ? rewardRevealBackgroundMusicDuck : 1f);

    void StartCrystalRewardAudioInternal()
    {
        rewardRevealActive = true;
        musicA.volume = musicB.volume = CurrentBackgroundMusicVolume;
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
        if (musicA) musicA.volume = MusicVolume;
        if (musicB) musicB.volume = MusicVolume;
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
        List<AudioClip> tracks = new List<AudioClip>(); if (homepageMusic1) tracks.Add(homepageMusic1); if (homepageMusic2) tracks.Add(homepageMusic2);
        StartMusicRoutine(tracks, false);
    }

    void StartGameplayMusic(bool selectNew = true)
    {
        gameplayStarted = true;
        List<AudioClip> valid = gameplayMusicTracks.FindAll(x => x);
        if (valid.Count == 0) { StopMusic(musicTransitionFade); return; }
        int index = lastGameplayIndex;
        if (selectNew || index < 0 || index >= valid.Count)
        { do index = UnityEngine.Random.Range(0, valid.Count); while (valid.Count > 1 && index == lastGameplayIndex); lastGameplayIndex = index; }
        StartMusicRoutine(new List<AudioClip> { valid[index] }, true);
    }

    void StartMusicRoutine(List<AudioClip> tracks, bool loopSingle)
    {
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(MusicPlaylist(tracks, loopSingle));
    }

    IEnumerator MusicPlaylist(List<AudioClip> tracks, bool loopSingle)
    {
        musicA.Stop(); musicB.Stop(); stingerSource.Stop();
        if (tracks.Count == 0 || !MusicEnabled) yield break;
        while (true)
        {
            AudioClip clip = tracks[homepageIndex++ % tracks.Count];
            musicA.clip = clip; musicA.loop = loopSingle || tracks.Count == 1; musicA.pitch = 1f; musicA.volume = CurrentBackgroundMusicVolume; musicA.mute = !MusicEnabled; musicA.Play();
            if (musicA.loop) yield break;
            while (musicA.isPlaying) yield return null;
        }
    }

    void StopMusic(float fade) { if (musicRoutine != null) StopCoroutine(musicRoutine); musicRoutine = StartCoroutine(FadeAndStop(fade, false)); }
    IEnumerator FadeAndStop(float duration, bool playStinger)
    {
        float a = musicA.volume, b = musicB.volume;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime) { float k = duration <= 0 ? 1 : t / duration; musicA.volume = Mathf.Lerp(a, 0, k); musicB.volume = Mathf.Lerp(b, 0, k); yield return null; }
        musicA.Stop(); musicB.Stop(); musicA.volume = musicB.volume = CurrentBackgroundMusicVolume; musicA.pitch = musicB.pitch = 1f;
        if (playStinger && MusicEnabled && levelEndingOutroStinger) { stingerSource.clip = levelEndingOutroStinger; stingerSource.volume = MusicVolume; stingerSource.pitch = 1f; stingerSource.Play(); }
    }
    public static void EndGameplayMusicAtFinalLanding() { if (Instance) { if (Instance.musicRoutine != null) Instance.StopCoroutine(Instance.musicRoutine); Instance.musicRoutine = Instance.StartCoroutine(Instance.FadeAndStop(Instance.gameplayMusicFadeOutDuration, true)); } }
}
