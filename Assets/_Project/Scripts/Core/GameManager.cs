using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Máquina de estados de la partida: Announcement → Running → PrizeSelection (o GameOver) → Announcement…
/// Lleva ronda, meta, estrellas, vidas, tiempo, velocidad, apuesta y premios.
/// Cuando llega cualquier evento, las propiedades de este componente ya están actualizadas;
/// PhaseChanged siempre se lanza al final.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Datos")]
    [SerializeField] private BalanceConfig balance;
    [Tooltip("Premios posibles: los normales y los envenenados (Data/Prizes).")]
    [SerializeField] private PrizeData[] prizePool;

    [Header("Sistemas")]
    [Tooltip("Si se deja vacío, se busca en este mismo GameObject.")]
    [SerializeField] private RuleManager ruleManager;
    [Tooltip("Si se deja vacío, se busca en la escena.")]
    [SerializeField] private TrackGenerator trackGenerator;

    [Header("Escenas")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private readonly List<PrizeData> prizeOffers = new List<PrizeData>();
    private bool[] prizeTaken = new bool[0];
    private bool poisonChosenThisRound;
    private bool advancingToNextRound;
    private float announcementTimer;

    public BalanceConfig Balance => balance;
    public RuleManager Rules => ruleManager;
    public TrackGenerator Track => trackGenerator;
    public PlayerModifiers Modifiers { get; private set; }

    public RoundPhase Phase { get; private set; } = RoundPhase.Announcement;
    public int CurrentRound { get; private set; }

    /// <summary>Meta que vale ahora (con apuesta si la aceptó).</summary>
    public int CurrentGoal { get; private set; }

    /// <summary>Meta sin apuesta (incluye la penalización del corazón envenenado).</summary>
    public int BaseGoal { get; private set; }

    /// <summary>Meta si acepta la apuesta.</summary>
    public int BetGoal { get; private set; }

    public int RoundStars { get; private set; }
    public int TotalStars { get; private set; }
    public int Lives { get; private set; }
    public float TimeRemaining { get; private set; }
    public float RoundElapsed => balance.RoundDuration - TimeRemaining;

    /// <summary>Velocidad de la pista de esta ronda (aunque esté detenida).</summary>
    public float RoundSpeed { get; private set; }

    /// <summary>Velocidad real en este momento: 0 fuera de Running o en pausa.</summary>
    public float CurrentSpeed => Phase == RoundPhase.Running && !IsPaused ? RoundSpeed : 0f;

    public bool IsBetOffered { get; private set; }
    public bool BetAccepted { get; private set; }
    public bool IsPaused { get; private set; }

    /// <summary>Tarjetas de premio de la pantalla Premios (en PrizeSelection).</summary>
    public IReadOnlyList<PrizeData> PrizeOffers => prizeOffers;
    public int PrizePicksRemaining { get; private set; }

    /// <summary>Resultado de la última partida (en GameOver).</summary>
    public GameStats LastStats { get; private set; }

    public bool IsPrizeTaken(int index) => index >= 0 && index < prizeTaken.Length && prizeTaken[index];

    private void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
        {
            Debug.LogError("Hay dos GameManager en la escena; se borra el segundo.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;

        if (ruleManager == null) ruleManager = GetComponent<RuleManager>();
        if (trackGenerator == null) trackGenerator = FindObjectOfType<TrackGenerator>();

        if (balance == null)
        {
            Debug.LogError("GameManager: falta asignar BalanceConfig (Data/Balance.asset).", this);
            enabled = false;
            return;
        }
        if (ruleManager == null)
        {
            Debug.LogError("GameManager: falta RuleManager en el mismo GameObject.", this);
            enabled = false;
            return;
        }
        if (trackGenerator == null) Debug.LogWarning("GameManager: no hay TrackGenerator en la escena.", this);

        Modifiers = new PlayerModifiers(balance);
    }

    private void OnEnable()
    {
        GameEvents.ContestantHit += HandleContestantHit;
    }

    private void OnDisable()
    {
        GameEvents.ContestantHit -= HandleContestantHit;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        StartNewGame();
    }

    private void Update()
    {
        switch (Phase)
        {
            case RoundPhase.Announcement:
                UpdateAnnouncement();
                break;
            case RoundPhase.Running:
                UpdateRunning();
                break;
        }
    }

    // ---------- Flujo de la partida ----------

    private void StartNewGame()
    {
        Modifiers.Reset();
        TotalStars = 0;
        Lives = balance.StartLives;
        LastStats = null;
        BeginAnnouncement(1);
    }

    private void BeginAnnouncement(int round)
    {
        StopAllCoroutines();
        advancingToNextRound = false;
        poisonChosenThisRound = false;
        CurrentRound = round;
        RoundStars = 0;
        BetAccepted = false;
        prizeOffers.Clear();
        prizeTaken = new bool[0];
        PrizePicksRemaining = 0;

        BaseGoal = balance.GetGoal(round) + Modifiers.ConsumeGoalPenalty();
        IsBetOffered = round >= balance.FirstBetRound;
        BetGoal = IsBetOffered ? balance.GetBetGoal(BaseGoal) : BaseGoal;
        CurrentGoal = BaseGoal;

        ruleManager.PrepareRound(round, balance);
        RoundSpeed = ComputeRoundSpeed();
        TimeRemaining = balance.RoundDuration;
        announcementTimer = 0f;

        if (trackGenerator != null)
        {
            trackGenerator.BuildRound(round, ruleManager.AppliedRule, ruleManager.IsLie,
                RoundSpeed * balance.RoundDuration);
        }

        Phase = RoundPhase.Announcement;
        ruleManager.AnnounceRule();
        GameEvents.RaiseLivesChanged(Lives);
        GameEvents.RaiseStarsChanged(RoundStars, CurrentGoal);
        GameEvents.RaiseTimeChanged(TimeRemaining);
        GameEvents.RaisePhaseChanged(Phase);
    }

    private float ComputeRoundSpeed()
    {
        float speed = balance.GetBaseSpeed(CurrentRound) * Modifiers.TrackSpeedMultiplier;
        if (ruleManager.IsApplied(RuleType.DoubleValue)) speed *= balance.DoubleValueSpeedMultiplier;
        return speed;
    }

    private void UpdateAnnouncement()
    {
        // Con apuesta se espera a que el jugador elija; sin apuesta empieza sola.
        if (IsBetOffered || balance.AnnouncementDuration <= 0f) return;
        announcementTimer += Time.deltaTime;
        if (announcementTimer >= balance.AnnouncementDuration) ConfirmAnnouncement(false);
    }

    /// <summary>Termina el anuncio y empieza a correr. acceptBet solo cuenta si hay apuesta.</summary>
    public void ConfirmAnnouncement(bool acceptBet)
    {
        if (Phase != RoundPhase.Announcement) return;

        BetAccepted = IsBetOffered && acceptBet;
        CurrentGoal = BetAccepted ? BetGoal : BaseGoal;
        Modifiers.BeginRound();
        Phase = RoundPhase.Running;

        if (IsBetOffered) GameEvents.RaiseBetResolved(BetAccepted);
        ruleManager.ApplyRule();
        GameEvents.RaiseRoundStarted(CurrentRound, CurrentGoal, RoundSpeed);
        GameEvents.RaiseStarsChanged(RoundStars, CurrentGoal);
        GameEvents.RaiseLivesChanged(Lives);
        GameEvents.RaiseTimeChanged(TimeRemaining);
        GameEvents.RaisePhaseChanged(Phase);
    }

    /// <summary>Para el botón «Aceptar» de la apuesta.</summary>
    public void AcceptBet() => ConfirmAnnouncement(true);

    /// <summary>Para el botón «Rechazar» de la apuesta o «¡A correr!».</summary>
    public void DeclineBet() => ConfirmAnnouncement(false);

    private void UpdateRunning()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (IsPaused) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // No pasarse del final: así el arco llega justo a z = 0.
        TimeRemaining = Mathf.Max(0f, TimeRemaining - dt);
        if (trackGenerator != null) trackGenerator.SetTravelled(RoundSpeed * RoundElapsed);
        GameEvents.RaiseTimeChanged(TimeRemaining);

        if (Phase == RoundPhase.Running && TimeRemaining <= 0f) EndRound();
    }

    private void EndRound()
    {
        if (RoundStars >= CurrentGoal) WinRound();
        else TriggerGameOver();
    }

    private void WinRound()
    {
        prizeOffers.Clear();
        prizeOffers.AddRange(PrizeOfferBuilder.Build(prizePool, CurrentRound, Lives, balance));
        prizeTaken = new bool[prizeOffers.Count];
        int picks = BetAccepted ? balance.PrizesPerBetWin : balance.PrizesPerWin;
        PrizePicksRemaining = Mathf.Min(picks, prizeOffers.Count);
        Phase = RoundPhase.PrizeSelection;

        GameEvents.RaiseRoundWon(CurrentRound, PrizePicksRemaining);
        GameEvents.RaisePhaseChanged(Phase);

        if (PrizePicksRemaining == 0)
        {
            Debug.LogWarning("GameManager: no hay premios para elegir (¿prizePool vacío?). Se pasa a la siguiente ronda.");
            StartCoroutine(AdvanceAfterDelay(balance.AfterPrizeDelay));
        }
    }

    /// <summary>Elige la tarjeta index (0, 1 o 2). Devuelve false si no se puede.</summary>
    public bool ChoosePrize(int index)
    {
        if (Phase != RoundPhase.PrizeSelection || advancingToNextRound || PrizePicksRemaining <= 0) return false;
        if (index < 0 || index >= prizeOffers.Count || prizeTaken[index]) return false;

        PrizeData prize = prizeOffers[index];
        prizeTaken[index] = true;
        PrizePicksRemaining--;
        if (prize.IsPoisoned) poisonChosenThisRound = true;
        ApplyPrize(prize);
        GameEvents.RaisePrizeChosen(prize);

        if (PrizePicksRemaining == 0)
        {
            float delay = balance.AfterPrizeDelay + (poisonChosenThisRound ? balance.PoisonRevealDelay : 0f);
            StartCoroutine(AdvanceAfterDelay(delay));
        }
        return true;
    }

    private void ApplyPrize(PrizeData prize)
    {
        Modifiers.AddPrize(prize.Type, prize.IsPoisoned);
        if (prize.Type == PrizeType.ExtraHeart)
        {
            Lives = Mathf.Min(Lives + 1, balance.MaxLives);
            GameEvents.RaiseLivesChanged(Lives);
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay)
    {
        advancingToNextRound = true;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        BeginAnnouncement(CurrentRound + 1);
    }

    private void TriggerGameOver()
    {
        if (Phase == RoundPhase.GameOver) return;
        if (IsPaused) SetPaused(false);
        Phase = RoundPhase.GameOver;
        LastStats = SaveManager.RecordGame(CurrentRound, TotalStars);
        GameEvents.RaiseGameOver(LastStats);
        GameEvents.RaisePhaseChanged(Phase);
    }

    // ---------- Estrellas, golpes y vidas ----------

    /// <summary>La llama Star al tocar al concursante.</summary>
    public void CollectStar(bool golden)
    {
        if (Phase != RoundPhase.Running) return;
        int value = golden ? balance.GoldenStarValue : balance.NormalStarValue;
        if (ruleManager.IsApplied(RuleType.DoubleValue)) value *= balance.DoubleValueStarMultiplier;
        AddStars(value);
    }

    /// <summary>Estrellas extra que no se multiplican (premio por acertar «¡Trampa!»).</summary>
    public void AddBonusStars(int amount)
    {
        if (Phase != RoundPhase.Running || amount <= 0) return;
        AddStars(amount);
    }

    private void AddStars(int amount)
    {
        RoundStars += amount;
        TotalStars += amount;
        GameEvents.RaiseStarsChanged(RoundStars, CurrentGoal);
    }

    private void HandleContestantHit(HitCause cause)
    {
        if (Instance != this || Phase != RoundPhase.Running || IsPaused) return;
        if (Modifiers.TryAbsorbHit(cause, RoundElapsed)) return;
        LoseLife(1);
    }

    public void LoseLife(int amount = 1)
    {
        if (Phase != RoundPhase.Running || amount <= 0) return;
        Lives = Mathf.Max(0, Lives - amount);
        GameEvents.RaiseLivesChanged(Lives);
        if (Lives == 0) TriggerGameOver();
    }

    // ---------- Pausa y escenas ----------

    public void TogglePause() => SetPaused(!IsPaused);

    public void SetPaused(bool paused)
    {
        if (paused == IsPaused) return;
        if (paused && Phase != RoundPhase.Running) return;
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        GameEvents.RaisePauseToggled(paused);
    }

    /// <summary>«¿Una ronda más?»: recarga la escena y empieza en la ronda 1.</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }
#if UNITY_EDITOR
        // Escena de prueba que no está en Build Settings.
        EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.name);
#endif
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(mainMenuScene)) SceneManager.LoadScene(mainMenuScene);
        else Debug.LogWarning($"La escena «{mainMenuScene}» no está en Build Settings (menú Una Ronda Más/Build/Configurar WebGL).");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ---------- Atajos para probar ----------

    public void DebugAddStars(int amount) => AddBonusStars(amount);

    public void DebugSetTimeRemaining(float seconds)
    {
        if (Phase == RoundPhase.Running) TimeRemaining = Mathf.Clamp(seconds, 0f, TimeRemaining);
    }

    /// <summary>Salta al anuncio de la ronda indicada, sin perder premios ni vidas.</summary>
    public void DebugStartRound(int round)
    {
        if (IsPaused) SetPaused(false);
        BeginAnnouncement(Mathf.Max(1, round));
    }
#endif
}
