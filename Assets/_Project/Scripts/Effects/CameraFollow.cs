using UnityEngine;

/// <summary>
/// Soporte de la cámara (CameraRig): acompaña un poco al concursante al cambiar de carril y al saltar,
/// abre el campo de visión cuando la pista va más rápido y se aleja con la regla Gigante.
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

    [Header("Velocidad")]
    [Tooltip("Grados de campo de visión extra cuando la pista va a la velocidad máxima.")]
    [SerializeField, Min(0f)] private float extraFovAtMaxSpeed = 6f;
    [Tooltip("Qué tan rápido cambia el campo de visión.")]
    [SerializeField, Min(0.1f)] private float fovSpeed = 3f;

    [Header("Regla Gigante")]
    [Tooltip("Cuánto se aleja la cámara con la regla Gigante (m).")]
    [SerializeField] private Vector3 giantOffset = new Vector3(0f, 1.5f, -2f);
    [Tooltip("Qué tan rápido se aleja o se acerca.")]
    [SerializeField, Min(0.1f)] private float giantBlendSpeed = 2f;

    private Vector3 home;
    private Vector3 velocity;
    private float baseFov;
    private float giantBlend;

    private void Awake()
    {
        home = transform.position;
        if (followCamera == null) followCamera = GetComponentInChildren<Camera>();
        if (followCamera != null) baseFov = followCamera.fieldOfView;
    }

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        bool running = gm != null && gm.Phase == RoundPhase.Running;

        bool giant = running && gm.Rules != null && gm.Rules.IsApplied(RuleType.Giant);
        giantBlend = Mathf.MoveTowards(giantBlend, giant ? 1f : 0f, giantBlendSpeed * Time.deltaTime);

        Vector3 desired = home + giantOffset * giantBlend;
        if (target != null)
        {
            desired.x += target.position.x * followX;
            desired.y += Mathf.Max(0f, target.position.y) * followY; // al caer al agua no baja
        }
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        if (followCamera == null || gm == null) return;
        BalanceConfig b = gm.Balance;
        float speedRatio = running ? Mathf.InverseLerp(b.GetBaseSpeed(1), b.GetBaseSpeed(TopSpeedRound), gm.RoundSpeed) : 0f;
        float fov = baseFov + extraFovAtMaxSpeed * speedRatio;
        followCamera.fieldOfView = Mathf.Lerp(followCamera.fieldOfView, fov, 1f - Mathf.Exp(-fovSpeed * Time.deltaTime));
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
