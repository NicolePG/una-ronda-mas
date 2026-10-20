using UnityEngine;

/// <summary>
/// Pelota gigante que rueda hacia el concursante por su carril, más rápido que la pista. Arranca cuando está
/// lo bastante cerca para pasar junto al concursante a mitad de su recorrido y nunca sale de su tramo
/// (de startZ a endZ, en coordenadas del tramo), así no tapa el camino de los tramos vecinos.
/// Velocidad y momento de arranque en BalanceConfig («Obstáculos que se mueven»).
/// </summary>
[DisallowMultipleComponent]
public class RollingBall : MonoBehaviour
{
    [Tooltip("Esfera visual que gira al rodar.")]
    [SerializeField] private Transform ball;
    [Tooltip("Radio de la pelota (m), para que gire a la velocidad justa.")]
    [SerializeField, Min(0.1f)] private float radius = 0.8f;
    [Tooltip("Hasta dónde rueda dentro del tramo (z local del tramo).")]
    [SerializeField] private float endZ = 3.9f;
    [Tooltip("Tiembla un poco antes de arrancar (m).")]
    [SerializeField, Min(0f)] private float shakeAmount = 0.05f;

    private float startZ;
    private bool rolling;
    private bool finished;
    private Vector3 ballBasePosition;

    private void Awake()
    {
        startZ = transform.localPosition.z;
        if (ball != null) ballBasePosition = ball.localPosition;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || finished || gm.Phase != RoundPhase.Running) return;
        BalanceConfig b = gm.Balance;
        float dt = Time.deltaTime;

        if (!rolling)
        {
            // Arranca para cruzarse con el concursante en RollingBallPassPoint de su recorrido.
            float travel = startZ - endZ;
            float rollTime = travel / Mathf.Max(b.RollingBallSpeed, 0.1f);
            float startAt = (gm.CurrentSpeed + b.RollingBallSpeed) * rollTime * b.RollingBallPassPoint;
            rolling = transform.position.z <= startAt;
            if (!rolling && ball != null)
                ball.localPosition = ballBasePosition + Vector3.right * Mathf.Sin(Time.time * 40f) * shakeAmount;
            return;
        }

        Vector3 local = transform.localPosition;
        float step = Mathf.Min(b.RollingBallSpeed * dt, local.z - endZ);
        local.z -= step;
        transform.localPosition = local;
        if (local.z <= endZ) finished = true;
        if (ball != null)
        {
            ball.localPosition = ballBasePosition;
            // Rueda hacia −Z: gira alrededor de −X.
            ball.Rotate(Vector3.left, step / radius * Mathf.Rad2Deg, Space.World);
        }
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los prefabs.</summary>
    public void EditorSetup(Transform ball, float radius)
    {
        this.ball = ball;
        this.radius = radius;
    }
#endif
}
