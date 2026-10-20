using System;
using System.Collections.Generic;
using UnityEngine;

public enum SurpriseType { StarRain, DoubleStars, Blackout }

/// <summary>
/// Eventos sorpresa de Tito a mitad de ronda (Otoniel): lluvia de estrellas que caen del techo, estrellas dobles
/// por unos segundos o un apagón corto. Desde la ronda SurpriseFromRound, con probabilidad SurpriseChance, una vez
/// por ronda. No cambia la velocidad de la pista (el arco de meta sigue llegando justo a tiempo).
/// Números en BalanceConfig («Eventos sorpresa»).
/// </summary>
[DisallowMultipleComponent]
public class SurpriseEvents : MonoBehaviour
{
    /// <summary>Empezó un evento: tipo y duración (s).</summary>
    public static event Action<SurpriseType, float> SurpriseStarted;
    /// <summary>Terminó el evento.</summary>
    public static event Action<SurpriseType> SurpriseEnded;

    /// <summary>¿Hay estrellas dobles ahora? (para los efectos).</summary>
    public static bool DoubleStarsActive { get; private set; }

    [Header("Partes")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject goldenStarPrefab;
    [Tooltip("Efectos del apagón (las mismas luces que la regla Apagón).")]
    [SerializeField] private RuleEffects ruleEffects;

    [Header("Lluvia de estrellas")]
    [Tooltip("A cuántos metros por delante del concursante caen.")]
    [SerializeField, Min(5f)] private float rainAhead = 38f;
    [Tooltip("Altura desde donde caen (m).")]
    [SerializeField, Min(1f)] private float dropHeight = 9f;
    [Tooltip("Segundos que tardan en caer.")]
    [SerializeField, Min(0.05f)] private float dropTime = 0.6f;
    [Tooltip("Altura de la estrella sobre la pista (m).")]
    [SerializeField] private float starHeight = 1f;

    private struct Drop
    {
        public Transform Holder;
        public float Age;
    }

    private readonly List<Drop> drops = new List<Drop>();
    private Transform carrier;
    private HostLines hostLines;
    private ContestantController contestant;

    private bool scheduled;
    private float startAtElapsed;
    private SurpriseType pendingType;
    private bool active;
    private SurpriseType activeType;
    private float activeLeft;
    private float nextRainAt;
#if UNITY_EDITOR
    private int debugNext;
#endif

    private void OnEnable()
    {
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        SetContestant(null);
        EndEvent();
    }

    private void Start() => hostLines = FindObjectOfType<HostLines>();

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        BalanceConfig b = gm.Balance;
        scheduled = round >= b.SurpriseFromRound && UnityEngine.Random.value < b.SurpriseChance;
        if (!scheduled) return;

        startAtElapsed = UnityEngine.Random.Range(b.SurpriseWindowStart, b.SurpriseWindowEnd);
        // Con la regla Apagón no hay apagón sorpresa (ya está oscuro).
        bool blackoutRule = gm.Rules != null && gm.Rules.IsApplied(RuleType.Blackout);
        int count = blackoutRule ? 2 : 3;
        pendingType = (SurpriseType)UnityEngine.Random.Range(0, count);
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Running) return;
        scheduled = false;
        EndEvent();
        ClearRain();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != RoundPhase.Running || gm.IsPaused) return;
        if (contestant != ContestantController.Current) SetContestant(ContestantController.Current);
        BalanceConfig b = gm.Balance;
        float dt = Time.deltaTime;

#if UNITY_EDITOR
        // Para probar en el Editor: F7 lanza el siguiente evento.
        if (Input.GetKeyDown(KeyCode.F7) && !active) DebugBegin((SurpriseType)(debugNext++ % 3));
#endif
        float elapsed = b.RoundDuration - gm.TimeRemaining;
        if (scheduled && elapsed >= startAtElapsed)
        {
            scheduled = false;
            BeginEvent(pendingType, b);
        }

        if (active)
        {
            activeLeft -= dt;
            if (activeType == SurpriseType.StarRain && activeLeft > 0f && Time.time >= nextRainAt)
            {
                nextRainAt = Time.time + b.StarRainInterval;
                SpawnRainStar(b);
            }
            if (activeLeft <= 0f) EndEvent();
        }

        UpdateRain(gm, dt);
    }

    private void BeginEvent(SurpriseType type, BalanceConfig b)
    {
        active = true;
        activeType = type;
        switch (type)
        {
            case SurpriseType.StarRain:
                activeLeft = b.StarRainDuration;
                nextRainAt = Time.time;
                Show("surprise_star_rain");
                break;
            case SurpriseType.DoubleStars:
                activeLeft = b.DoubleStarsDuration;
                DoubleStarsActive = true;
                Show("surprise_double_stars");
                break;
            case SurpriseType.Blackout:
                activeLeft = b.BlackoutEventDuration;
                if (ruleEffects != null) ruleEffects.SetEventBlackout(true);
                Show("surprise_blackout");
                break;
        }
        SurpriseStarted?.Invoke(type, activeLeft);
    }

    private void EndEvent()
    {
        if (!active) return;
        active = false;
        DoubleStarsActive = false;
        if (activeType == SurpriseType.Blackout && ruleEffects != null) ruleEffects.SetEventBlackout(false);
        SurpriseEnded?.Invoke(activeType);
    }

    private void Show(string moment)
    {
        if (hostLines != null) hostLines.ShowMoment(moment);
    }

    // ---------- Estrellas dobles ----------

    private void SetContestant(ContestantController value)
    {
        if (contestant != null) contestant.StarCollected -= HandleStarCollected;
        contestant = value;
        if (contestant != null) contestant.StarCollected += HandleStarCollected;
    }

    private void HandleStarCollected(Star star)
    {
        GameManager gm = GameManager.Instance;
        if (!DoubleStarsActive || gm == null || gm.Phase != RoundPhase.Running) return;
        gm.AddBonusStars(star.GetValue(gm.Balance));
    }

    // ---------- Lluvia de estrellas ----------

    private void SpawnRainStar(BalanceConfig b)
    {
        bool golden = UnityEngine.Random.value < b.StarRainGoldenChance;
        GameObject prefab = golden && goldenStarPrefab != null ? goldenStarPrefab : starPrefab;
        if (prefab == null) return;
        if (carrier == null) carrier = new GameObject("StarRain").transform;

        // Cada estrella cuelga de un «soporte» que cae; la estrella sigue flotando sola dentro de él.
        var holder = new GameObject("Drop").transform;
        holder.SetParent(carrier, false);
        int lane = UnityEngine.Random.Range(-1, 2);
        holder.position = new Vector3(lane * b.LaneWidth, dropHeight, rainAhead);
        // Con posición al instanciar: Star guarda su altura en Awake.
        Instantiate(prefab, holder.position + Vector3.up * starHeight, Quaternion.identity, holder);
        drops.Add(new Drop { Holder = holder, Age = 0f });
    }

    private void UpdateRain(GameManager gm, float dt)
    {
        if (drops.Count == 0) return;
        float speed = gm.CurrentSpeed;
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            Drop drop = drops[i];
            if (drop.Holder == null)
            {
                drops.RemoveAt(i);
                continue;
            }
            drop.Age += dt;
            // Cae con aceleración y viaja con la pista hacia el concursante.
            float t = Mathf.Clamp01(drop.Age / dropTime);
            Vector3 p = drop.Holder.position;
            p.y = Mathf.Lerp(dropHeight, 0f, t * t);
            p.z -= speed * dt;
            drop.Holder.position = p;
            drops[i] = drop;
            if (p.z < -10f)
            {
                Destroy(drop.Holder.gameObject);
                drops.RemoveAt(i);
            }
        }
    }

    private void ClearRain()
    {
        drops.Clear();
        if (carrier != null) Destroy(carrier.gameObject);
        carrier = null;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(GameObject starPrefab, GameObject goldenStarPrefab, RuleEffects ruleEffects)
    {
        this.starPrefab = starPrefab;
        this.goldenStarPrefab = goldenStarPrefab;
        this.ruleEffects = ruleEffects;
    }

    /// <summary>Para probar: lanza un evento ya.</summary>
    public void DebugBegin(SurpriseType type)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.Phase == RoundPhase.Running) BeginEvent(type, gm.Balance);
    }
#endif
}
