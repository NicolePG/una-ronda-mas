using System.Collections;
using UnityEngine;

public enum ContestantState { Idle, Running, Jumping, Sliding, Falling, Hit, Celebrating, Eliminated }

/// <summary>
/// Concursante: 3 carriles, salto y deslizamiento por código, choques, caída al agua e invulnerabilidad.
/// Nunca avanza: queda en z = 0 y la pista se mueve hacia él. Los números salen de BalanceConfig
/// (sección «Concursante») y de los premios (GameManager.Modifiers); respeta las reglas Espejo, Gigante y Sin salto.
/// Necesita el tag «Player» (lo usan las estrellas).
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
[DisallowMultipleComponent]
public class ContestantController : MonoBehaviour
{
    [Header("Partes")]
    [Tooltip("Modelo visual (hijo con el Animator). Parpadea, gira y se aplasta.")]
    [SerializeField] private Transform model;
    [Tooltip("Salpicadura al caer al agua (opcional).")]
    [SerializeField] private ParticleSystem splashPrefab;

    [Header("Cápsula")]
    [Tooltip("Alto de la cápsula de pie (m).")]
    [SerializeField, Min(0.1f)] private float standingHeight = 1.6f;
    [SerializeField, Min(0.05f)] private float radius = 0.35f;

    [Header("Regla Gigante")]
    [Tooltip("Tamaño del concursante con la regla Gigante (GDD: el doble).")]
    [SerializeField, Min(1f)] private float giantScale = 2f;
    [Tooltip("Velocidad a la que crece o se achica.")]
    [SerializeField, Min(0.1f)] private float scaleSpeed = 4f;

    [Header("Sensación")]
    [Tooltip("Veces por segundo que parpadea mientras es invulnerable.")]
    [SerializeField, Min(1f)] private float blinkRate = 12f;
    [Tooltip("Segundos hundiéndose en el agua antes de reaparecer.")]
    [SerializeField, Min(0f)] private float waterSinkTime = 0.6f;
    [Tooltip("Cuánto se hunde (m).")]
    [SerializeField, Min(0f)] private float waterSinkDepth = 1.4f;
    [Tooltip("Vueltas que da al chocar.")]
    [SerializeField] private float hitSpinTurns = 1f;
    [Tooltip("Segundos del giro al chocar.")]
    [SerializeField, Min(0.05f)] private float hitSpinTime = 0.35f;
    [Tooltip("Alto relativo del modelo al deslizarse (se aplasta).")]
    [SerializeField, Range(0.2f, 1f)] private float slideSquash = 0.55f;
    [Tooltip("Al deslizarse en el aire, el salto termina así de rápido.")]
    [SerializeField, Min(1f)] private float fastFallMultiplier = 2.5f;

    [Header("Controles")]
    [Tooltip("Segundos que se recuerda un salto apretado justo antes de aterrizar.")]
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.15f;
    [Tooltip("Grados que se inclina el modelo al cambiar de carril.")]
    [SerializeField, Range(0f, 30f)] private float leanAngle = 12f;
    [Tooltip("Qué tan rápido se inclina y se endereza.")]
    [SerializeField, Min(0.1f)] private float leanSpeed = 14f;
    [Tooltip("Alto relativo del modelo justo al aterrizar.")]
    [SerializeField, Range(0.5f, 1f)] private float landSquash = 0.82f;
    [Tooltip("Segundos que dura el aplastado al aterrizar.")]
    [SerializeField, Min(0.01f)] private float landSquashTime = 0.14f;

    private CapsuleCollider capsule;
    private Renderer[] modelRenderers = new Renderer[0];
    private Vector3 modelBaseScale = Vector3.one;
    private Quaternion modelBaseRotation = Quaternion.identity;

    private int lane;
    private bool jumping;
    private bool fastFall;
    private bool slideQueued;
    private float jumpTime;
    private float jumpDuration = 1f;
    private float jumpHeight;
    private bool sliding;
    private float slideTimer;
    private bool inWater;
    private float invulnerableUntil;
    private float hitAnimUntil;
    private float currentScale = 1f;
    private float jumpBufferedUntil = -1f;
    private float laneStartX;
    private float laneTime;
    private float lateralSpeedRatio;   // −1 a 1: hacia dónde y qué tan rápido se mueve de carril
    private float lean;
    private float landedAt = -1f;
    private int dodgedFromLane;
    private float dodgedAt = -10f;

    /// <summary>El concursante de la escena (para efectos que necesitan saber dónde está).</summary>
    public static ContestantController Current { get; private set; }

    /// <summary>Tocó una estrella durante la carrera (rachas, textos flotantes).</summary>
    public event System.Action<Star> StarCollected;

    /// <summary>Lo que está haciendo ahora (para animaciones).</summary>
    public ContestantState State
    {
        get
        {
            GameManager gm = GameManager.Instance;
            if (inWater) return ContestantState.Falling;
            if (Time.time < hitAnimUntil) return ContestantState.Hit;
            if (gm != null)
            {
                if (gm.Phase == RoundPhase.GameOver) return ContestantState.Eliminated;
                if (gm.Phase == RoundPhase.PrizeSelection) return ContestantState.Celebrating;
                if (gm.Phase != RoundPhase.Running) return ContestantState.Idle;
            }
            if (jumping) return ContestantState.Jumping;
            if (sliding) return ContestantState.Sliding;
            return ContestantState.Running;
        }
    }

    /// <summary>Duración del salto actual (con premios), para ajustar la animación.</summary>
    public float CurrentJumpDuration => jumpDuration;

    public int Lane => lane;

    /// <summary>−1 a 1: hacia dónde y qué tan rápido se mueve de carril (cámara, estela).</summary>
    public float LateralSpeedRatio => lateralSpeedRatio;
    public bool IsGrounded => !jumping && !inWater;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    /// <summary>
    /// ¿Esquivó a último momento un obstáculo que estaba en x? (dejó ese carril hace menos de NearMissWindow s).
    /// </summary>
    public bool DodgedAt(float obstacleX, BalanceConfig b)
    {
        if (IsInvulnerable || inWater || lane == dodgedFromLane) return false;
        if (Time.time - dodgedAt > b.NearMissWindow) return false;
        return Mathf.Abs(obstacleX - dodgedFromLane * b.LaneWidth) < b.LaneWidth * 0.5f;
    }

    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        capsule = GetComponent<CapsuleCollider>();
        capsule.isTrigger = false;
        capsule.direction = 1;
        SetCapsuleHeight(standingHeight);

        if (model != null)
        {
            modelRenderers = model.GetComponentsInChildren<Renderer>();
            modelBaseScale = model.localScale;
            modelBaseRotation = model.localRotation;
        }
        if (!CompareTag("Player")) Debug.LogWarning("ContestantController: el concursante necesita el tag «Player».", this);
    }

    private void OnEnable()
    {
        Current = this;
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (Current == this) Current = null;
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
    }

    private void HandleRoundStarted(int round, int goal, float speed) => ResetPose();

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Announcement) ResetPose();
    }

    /// <summary>Vuelve al carril central, de pie y sin estados pendientes.</summary>
    public void ResetPose()
    {
        StopAllCoroutines();
        jumping = false;
        fastFall = false;
        slideQueued = false;
        inWater = false;
        hitAnimUntil = 0f;
        invulnerableUntil = 0f;
        jumpBufferedUntil = -1f;
        EndSlide();
        transform.position = new Vector3(0f, 0f, transform.position.z);
        SetLane(0);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        UpdateScale(gm);
        bool running = gm.Phase == RoundPhase.Running && !gm.IsPaused;
        if (running && !inWater) ReadInput(gm);
        if (!inWater) Move(gm, Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (model == null) return;

        // Parpadeo mientras es invulnerable.
        bool visible = !IsInvulnerable || Mathf.Repeat(Time.time * blinkRate, 1f) < 0.5f;
        foreach (Renderer r in modelRenderers)
        {
            if (r != null && r.enabled != visible) r.enabled = visible;
        }

        // Giro al chocar e inclinación hacia el carril al que va.
        float spin = 0f;
        if (Time.time < hitAnimUntil)
            spin = 360f * hitSpinTurns * (1f - (hitAnimUntil - Time.time) / hitSpinTime);
        lean = Mathf.Lerp(lean, -lateralSpeedRatio * leanAngle, 1f - Mathf.Exp(-leanSpeed * Time.deltaTime));
        model.localRotation = modelBaseRotation * Quaternion.Euler(0f, spin, lean);

        // Aplastado al deslizarse y al aterrizar (ensancha un poco para que no pierda volumen).
        float squash = sliding ? slideSquash : 1f;
        float sinceLanding = (Time.time - landedAt) / landSquashTime;
        if (landedAt >= 0f && sinceLanding < 1f) squash *= Mathf.Lerp(landSquash, 1f, sinceLanding);
        float widen = 1f + (1f - squash) * 0.5f;
        model.localScale = new Vector3(modelBaseScale.x * widen, modelBaseScale.y * squash, modelBaseScale.z * widen);
    }

    // ---------- Controles ----------

    private void ReadInput(GameManager gm)
    {
        int direction = 0;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) direction -= 1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) direction += 1;
        if (IsRuleApplied(gm, RuleType.Mirror)) direction = -direction;
        if (direction != 0) ChangeLane(direction);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            // Apretado en el aire: se guarda un momento y sale al aterrizar.
            if (!TryJump(gm) && jumping) jumpBufferedUntil = Time.time + jumpBufferTime;
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            jumpBufferedUntil = -1f;
            TrySlide(gm);
        }
    }

    private void ChangeLane(int direction)
    {
        int target = Mathf.Clamp(lane + direction, -1, 1);
        if (target == lane) return;
        dodgedFromLane = lane;
        dodgedAt = Time.time;
        SetLane(target);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLaneChange();
    }

    // El cambio de carril empieza desde donde está ahora (sirve también si cambia a mitad de camino).
    private void SetLane(int target)
    {
        if (target != lane && Time.time != dodgedAt) dodgedAt = -10f;   // choques y reinicios no cuentan como esquive
        lane = target;
        laneStartX = transform.position.x;
        laneTime = 0f;
    }

    private bool TryJump(GameManager gm)
    {
        if (jumping || IsRuleApplied(gm, RuleType.NoJump)) return false;
        if (sliding) EndSlide();

        BalanceConfig b = gm.Balance;
        jumpDuration = Mathf.Max(0.05f, b.JumpDuration * gm.Modifiers.JumpDurationMultiplier);
        jumpHeight = b.JumpHeight * gm.Modifiers.JumpHeightMultiplier;
        jumpTime = 0f;
        jumping = true;
        fastFall = false;
        jumpBufferedUntil = -1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
        return true;
    }

    private void TrySlide(GameManager gm)
    {
        if (IsRuleApplied(gm, RuleType.Giant)) return; // gigante: no cabe bajo las barras altas
        if (jumping)
        {
            // En el aire: baja rápido y se desliza al tocar el piso.
            fastFall = true;
            slideQueued = true;
            return;
        }
        StartSlide(gm.Balance);
    }

    private void StartSlide(BalanceConfig b)
    {
        sliding = true;
        slideTimer = b.SlideDuration;
        SetCapsuleHeight(b.SlideHeight);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySlide();
    }

    private void EndSlide()
    {
        sliding = false;
        slideTimer = 0f;
        if (capsule != null) SetCapsuleHeight(standingHeight);
    }

    private void SetCapsuleHeight(float height)
    {
        capsule.height = height;
        capsule.radius = Mathf.Min(radius, height * 0.5f);
        capsule.center = new Vector3(0f, height * 0.5f, 0f);
    }

    // ---------- Movimiento ----------

    private void Move(GameManager gm, float dt)
    {
        BalanceConfig b = gm.Balance;
        Vector3 position = transform.position;

        // Cambio de carril con salida rápida y llegada suave (ease-out), en LaneChangeDuration.
        float laneDuration = Mathf.Max(b.LaneChangeDuration * gm.Modifiers.LaneChangeDurationMultiplier, Mathf.Epsilon);
        float targetX = lane * b.LaneWidth;
        float previousX = position.x;
        laneTime += dt;
        float progress = Mathf.Clamp01(laneTime / laneDuration);
        float eased = 1f - (1f - progress) * (1f - progress) * (1f - progress);
        position.x = Mathf.Lerp(laneStartX, targetX, eased);
        float maxLateralSpeed = b.LaneWidth / laneDuration;
        lateralSpeedRatio = dt > 0f ? Mathf.Clamp((position.x - previousX) / dt / maxLateralSpeed, -1f, 1f) : 0f;

        if (jumping)
        {
            jumpTime += dt * (fastFall ? fastFallMultiplier : 1f);
            float t = jumpTime / jumpDuration;
            if (t >= 1f)
            {
                jumping = false;
                fastFall = false;
                position.y = 0f;
                landedAt = Time.time;
                if (slideQueued && !IsRuleApplied(gm, RuleType.Giant)) StartSlide(b);
                slideQueued = false;
                if (Time.time <= jumpBufferedUntil) TryJump(gm);
            }
            else
            {
                position.y = 4f * jumpHeight * t * (1f - t); // parábola: arriba a la mitad del salto
            }
        }
        else
        {
            position.y = 0f;
        }

        if (sliding)
        {
            slideTimer -= dt;
            if (slideTimer <= 0f) EndSlide();
        }

        transform.position = position;
    }

    private void UpdateScale(GameManager gm)
    {
        float target = IsRuleApplied(gm, RuleType.Giant) && gm.Phase == RoundPhase.Running ? giantScale : 1f;
        currentScale = Mathf.MoveTowards(currentScale, target, scaleSpeed * Time.deltaTime);
        transform.localScale = Vector3.one * currentScale;
    }

    private static bool IsRuleApplied(GameManager gm, RuleType type) => gm.Rules != null && gm.Rules.IsApplied(type);

    // ---------- Choques ----------

    private void OnTriggerEnter(Collider other) => HandleTrigger(other);

    // Por si aterriza dentro de un hueco: el agua se revisa mientras siga encima.
    private void OnTriggerStay(Collider other)
    {
        if (!inWater && other.GetComponentInParent<WaterZone>() != null) HandleTrigger(other);
    }

    private void HandleTrigger(Collider other)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != RoundPhase.Running || gm.IsPaused || inWater) return;
        Star star = other.GetComponentInParent<Star>(true);
        if (star != null)
        {
            StarCollected?.Invoke(star);
            return;
        }

        if (other.GetComponentInParent<WaterZone>() != null)
        {
            if (IsGrounded && !IsInvulnerable) StartCoroutine(FallIntoWater(gm));
            return;
        }

        Obstacle obstacle = other.GetComponentInParent<Obstacle>();
        if (obstacle == null || obstacle.IsFake || IsInvulnerable) return;
        HitObstacle(gm, obstacle);
    }

    private void HitObstacle(GameManager gm, Obstacle obstacle)
    {
        invulnerableUntil = Time.time + gm.Balance.InvulnerabilityTime;
        hitAnimUntil = Time.time + hitSpinTime;
        SetLane(0); // reaparece en el carril central
        slideQueued = false;
        EndSlide();
        obstacle.Wobble();
        GameEvents.RaiseContestantHit(HitCause.Obstacle);
    }

    private IEnumerator FallIntoWater(GameManager gm)
    {
        inWater = true;
        jumping = false;
        slideQueued = false;
        EndSlide();
        if (splashPrefab != null) Instantiate(splashPrefab, transform.position, Quaternion.identity);
        GameEvents.RaiseContestantHit(HitCause.Water);

        Vector3 start = transform.position;
        float t = 0f;
        while (t < waterSinkTime)
        {
            t += Time.deltaTime;
            transform.position = new Vector3(start.x, Mathf.Lerp(0f, -waterSinkDepth, t / waterSinkTime), start.z);
            yield return null;
        }

        transform.position = new Vector3(0f, 0f, start.z);
        SetLane(0);
        invulnerableUntil = Time.time + gm.Balance.InvulnerabilityTime;
        inWater = false;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean el prefab.</summary>
    public void EditorSetup(Transform model, ParticleSystem splashPrefab)
    {
        this.model = model;
        this.splashPrefab = splashPrefab;
    }
#endif
}
