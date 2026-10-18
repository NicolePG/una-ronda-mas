using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reproduce música y efectos de audio durante toda la sesión.</summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [Header("Fuentes")]
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("Música")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Interfaz y gameplay")]
    [SerializeField] private AudioClip menuClick;
    [SerializeField] private AudioClip menuHover;
    [SerializeField] private AudioClip obstacleHit;
    [SerializeField] private AudioClip waterHit;
    [SerializeField] private AudioClip laneChange;
    [SerializeField] private AudioClip jump;
    [SerializeField] private AudioClip[] slideClips;
    [SerializeField] private AudioClip star;
    [SerializeField] private AudioClip goldenStar;
    [SerializeField] private AudioClip badPrize;
    [SerializeField] private AudioClip goodPrize;
    [SerializeField] private AudioClip roundStart;
    [SerializeField] private AudioClip elimination;
    [SerializeField] private AudioClip victory;

    [Header("Voces del presentador")]
    [SerializeField] private AudioClip[] hitVoices;
    [SerializeField] private AudioClip[] victoryVoices;
    [SerializeField] private AudioClip[] gameOverVoices;
    [SerializeField] private AudioClip[] generalVoices;

    private AudioClip lastVoice;

    public static AudioManager Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentAudioManager()
    {
        if (instance != null) return;

        AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab == null)
        {
            Debug.LogError("AudioManager: falta Resources/AudioManager.prefab. Ejecuta Una Ronda Más/Audio/Crear o actualizar AudioManager.");
            return;
        }

        Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null || sfxSource == null || voiceSource == null)
        {
            Debug.LogError("AudioManager: el prefab debe tener asignadas sus tres fuentes de audio.", this);
            enabled = false;
            return;
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
        voiceSource.playOnAwake = false;
        ApplyMusicVolume(SaveManager.MusicVolume);
        ApplySfxVolume(SaveManager.SfxVolume);
    }

    private void Start()
    {
        RefreshAudioListener();
        PlayMusicForScene(SceneManager.GetActiveScene());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameEvents.ContestantHit += OnContestantHit;
        GameEvents.RoundStarted += OnRoundStarted;
        GameEvents.RoundWon += OnRoundWon;
        GameEvents.GameOver += OnGameOver;
        GameEvents.PrizeChosen += OnPrizeChosen;
        GameEvents.CheatCalled += OnCheatCalled;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameEvents.ContestantHit -= OnContestantHit;
        GameEvents.RoundStarted -= OnRoundStarted;
        GameEvents.RoundWon -= OnRoundWon;
        GameEvents.GameOver -= OnGameOver;
        GameEvents.PrizeChosen -= OnPrizeChosen;
        GameEvents.CheatCalled -= OnCheatCalled;
    }

    public void SetMusicVolume(float value)
    {
        ApplyMusicVolume(value);
    }

    public void SetSfxVolume(float value)
    {
        ApplySfxVolume(value);
    }

    public void PlayMenuClick()
    {
        PlaySfx(menuClick);
    }

    public void PlayMenuHover()
    {
        PlaySfx(menuHover);
    }

    public void PlayLaneChange()
    {
        PlaySfx(laneChange);
    }

    public void PlayJump()
    {
        PlaySfx(jump);
    }

    public void PlaySlide()
    {
        PlayRandomSfx(slideClips);
    }

    public void PlayStar(bool golden)
    {
        PlaySfx(golden ? goldenStar : star);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAudioListener();
        PlayMusicForScene(scene);
    }

    private void RefreshAudioListener()
    {
        if (audioListener == null)
        {
            Debug.LogError("AudioManager: falta el AudioListener en el prefab.", this);
            return;
        }

        bool anotherListenerIsActive = false;
        foreach (AudioListener listener in FindObjectsOfType<AudioListener>())
        {
            if (listener != audioListener && listener.isActiveAndEnabled)
            {
                anotherListenerIsActive = true;
                break;
            }
        }

        audioListener.enabled = !anotherListenerIsActive;
    }

    private void PlayMusicForScene(Scene scene)
    {
        AudioClip clip = scene.name == "MainMenu" ? menuMusic : gameplayMusic;
        if (clip == null)
        {
            Debug.LogError($"AudioManager: no hay música asignada para la escena {scene.name}.", this);
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    private void OnContestantHit(HitCause cause)
    {
        PlaySfx(cause == HitCause.Water ? waterHit : obstacleHit);
        PlayVoice(hitVoices);
    }

    private void OnRoundStarted(int round, int goal, float speed)
    {
        PlaySfx(roundStart);
    }

    private void OnRoundWon(int round, int prizesToChoose)
    {
        PlaySfx(victory);
        PlayVoice(victoryVoices);
    }

    private void OnGameOver(GameStats stats)
    {
        PlaySfx(elimination);
        PlayVoice(gameOverVoices);
    }

    private void OnPrizeChosen(PrizeData prize)
    {
        if (prize == null) return;
        PlaySfx(prize.IsPoisoned ? badPrize : goodPrize);
    }

    private void OnCheatCalled(bool correct)
    {
        PlayVoice(generalVoices);
    }

    private void ApplyMusicVolume(float value)
    {
        if (musicSource != null) musicSource.volume = Mathf.Clamp01(value);
    }

    private void ApplySfxVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        if (sfxSource != null) sfxSource.volume = volume;
        if (voiceSource != null) voiceSource.volume = volume;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    private void PlayRandomSfx(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySfx(clips[Random.Range(0, clips.Length)]);
    }

    private void PlayVoice(AudioClip[] clips)
    {
        if (voiceSource == null || voiceSource.isPlaying || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        if (clips.Length > 1 && clip == lastVoice)
            clip = clips[(System.Array.IndexOf(clips, clip) + 1) % clips.Length];
        lastVoice = clip;
        voiceSource.clip = clip;
        voiceSource.Play();
    }
}
