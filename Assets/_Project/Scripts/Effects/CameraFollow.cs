using UnityEngine;

/// <summary>
/// Soporte de la cámara (CameraRig): acompaña un poco al concursante al cambiar de carril y al saltar,
/// abre el campo de visión cuando la pista va más rápido y se aleja con la regla Gigante.
/// Para que se sienta dinámica: se inclina al cambiar de carril, da un «golpe» de zoom al saltar, al tomar
/// una estrella dorada y en las rachas, baja un poco al aterrizar y abre el zoom durante las estrellas dobles.
/// La cámara va como hija (con CameraShake), así la sacudida se suma sin pelear con el seguimiento.
/// </summary>
[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    private const int TopSpeedRound = 100; // a esta ronda la velocidad ya llegó a maxSpeed

    [Tooltip("Concursante a seguir.")]
    [SerializeField] private Transform target;
    [Tooltip("Cámara hija (si se deja vacío, se busca).")]
    [SerializeField] private Camera followCamera;

    [Header("Seguimiento")]
    [Tooltip("Qué parte del movimiento lateral sigue (0 = fija, 1 = pegada al concursante).")]
    [SerializeField, Range(0f, 1f)] private float followX = 0.35f;
    [Tooltip("Qué parte del salto sigue hacia arriba.")]
    [SerializeField, Range(0f, 1f)] private float followY = 0.2f;
    [Tooltip("Segundos de suavizado.")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.12f;
    [Tooltip("Grados que se inclina la cámara al cambiar de carril.")]
    [SerializeField, Range(0f, 10f)] private float laneRoll = 2.5f;
    [Tooltip("Qué tan rápido se inclina y se endereza.")]
    [SerializeField, Min(0.1f)] private float rollSpeed = 8f;

    [Header("Velocidad")]
    [Tooltip("Grados de campo de visión extra cuando la pista va a la velocidad máxima.")]
    [SerializeField, Min(0f)] private float extraFovAtMaxSpeed = 9f;
    [Tooltip("Qué tan rápido cambia el campo de visión.")]
    [SerializeField, Min(0.1f)] private float fovSpeed = 3f;
    [Tooltip("Grados extra mientras duran las estrellas dobles (sensación de turbo).")]
    [SerializeField, Min(0f)] private float turboFov = 6f;

    [Header("Golpes de cámara")]
    [Tooltip("Grados de zoom hacia afuera al saltar.")]
    [SerializeField] private float jumpKick = 2.5f;
    [Tooltip("Grados al tomar una estrella dorada.")]
    [SerializeField] private float goldenKick = 3.5f;
    [Tooltip("Grados al llegar a un hito de racha.")]
    [SerializeField] private float streakKick = 5f;
    [Tooltip("Grados al chocar (negativo = acerca).")]
    [SerializeField] private float hitKick = -4f;
    [Tooltip("Qué tan rápido se van los golpes de zoom.")]
    [SerializeField, Min(0.1f)] private float kickDecay = 6f;
    [Tooltip("Metros que baja la cámara al aterrizar.")]
    [SerializeField, Min(0f)] private float landDip = 0.12f;

    [Header("Regla Gigante")]
    [Tooltip("Cuánto se aleja la cámara con la regla Gigante (m).")]
    [SerializeField] private Vector3 giantOffset = new Vector3(0f, 1.5f, -2f);
    [Tooltip("Qué tan rápido se aleja o se acerca.")]
    [SerializeField, Min(0.1f)] private float giantBlendSpeed = 2f;

    private Vector3 home;
    private Quaternion homeRotation;
    private Vector3 velocity;
    private float baseFov;
    private float smoothedFov;
    private float giantBlend;
    private float roll;
    private float kick;
    private float dip;
    private bool wasJumping;
    private ContestantController contestant;

    private void Awake()
    {
        home = transform.position;
        homeRotation = transform.rotation;
        if (followCamera == null) followCamera = GetComponentInChildren<Camera>();
        if (followCamera != null) baseFov = followCamera.fieldOfView;
        smoothedFov = baseFov;
        if (target != null) contestant = target.GetComponent<ContestantController>();
    }

    private void OnEnable()
    {
        GameEvents.ContestantHit += HandleHit;
        StarStreak.MilestoneReached += HandleMilestone;
        if (contestant != null) contestant.StarCollected += HandleStar;
    }

    private void OnDisable()
    {
        GameEvents.ContestantHit -= HandleHit;
        StarStreak.MilestoneReached -= HandleMilestone;
        if (contestant != null) contestant.StarCollected -= HandleStar;
    }

    private void HandleHit(HitCause cause) => Kick(hitKick);
    private void HandleMilestone(int streak, int bonus) => Kick(streakKick);

    private void HandleStar(Star star)
    {
        if (star.IsGolden) Kick(goldenKick);
    }

    /// <summary>Golpe de zoom que se va solo (positivo = abre).</summary>
    public void Kick(float degrees) => kick += degrees;

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        bool running = gm != null && gm.Phase == RoundPhase.Running;
        float dt = Time.deltaTime;

        bool giant = running && gm.Rules != null && gm.Rules.IsApplied(RuleType.Giant);
        giantBlend = Mathf.MoveTowards(giantBlend, giant ? 1f : 0f, giantBlendSpeed * dt);

        // Salto y aterrizaje.
        bool jumping = contestant != null && contestant.State == ContestantState.Jumping;
        if (jumping && !wasJumping) Kick(jumpKick);
        if (!jumping && wasJumping && contestant.IsGrounded) dip = landDip;
        wasJumping = jumping;
        dip = Mathf.MoveTowards(dip, 0f, dt * landDip * 6f);

        Vector3 desired = home + giantOffset * giantBlend + Vector3.down * dip;
        if (target != null)
        {
            desired.x += target.position.x * followX;
            desired.y += Mathf.Max(0f, target.position.y) * followY; // al caer al agua no baja
        }
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        // Inclinación hacia el carril al que va.
        float targetRoll = contestant != null && running ? -contestant.LateralSpeedRatio * laneRoll : 0f;
        roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-rollSpeed * dt));
        transform.rotation = homeRotation * Quaternion.Euler(0f, 0f, roll);

        kick = Mathf.Lerp(kick, 0f, 1f - Mathf.Exp(-kickDecay * dt));
        if (followCamera == null || gm == null) return;
        BalanceConfig b = gm.Balance;
        float speedRatio = running ? Mathf.InverseLerp(b.GetBaseSpeed(1), b.GetBaseSpeed(TopSpeedRound), gm.RoundSpeed) : 0f;
        float turbo = running && SurpriseEvents.DoubleStarsActive ? turboFov : 0f;
        float fov = baseFov + extraFovAtMaxSpeed * speedRatio + turbo;
        smoothedFov = Mathf.Lerp(smoothedFov, fov, 1f - Mathf.Exp(-fovSpeed * dt));
        followCamera.fieldOfView = smoothedFov + kick;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(Transform target, Camera followCamera)
    {
        this.target = target;
        this.followCamera = followCamera;
    }
#endif
}
