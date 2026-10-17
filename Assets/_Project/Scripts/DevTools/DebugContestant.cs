using UnityEngine;

/// <summary>
/// Concursante de prueba para la escena Test_Nicole: solo cambia de carril (A/D o flechas)
/// y avisa de choques con cualquier trigger que no sea estrella.
/// El concursante real es ContestantController (Otoniel); no usar este en Game.unity.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DebugContestant : MonoBehaviour
{
    [Tooltip("Leer el teclado. Apagado, se queda en el carril central.")]
    [SerializeField] private bool readInput = true;

    private int lane;
    private float invulnerableUntil;

    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnEnable()
    {
        GameEvents.RoundStarted += HandleRoundStarted;
    }

    private void OnDisable()
    {
        GameEvents.RoundStarted -= HandleRoundStarted;
    }

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        lane = 0;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (readInput && gm.Phase == RoundPhase.Running && !gm.IsPaused)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) lane = Mathf.Max(-1, lane - 1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) lane = Mathf.Min(1, lane + 1);
        }

        BalanceConfig b = gm.Balance;
        float duration = b.LaneChangeDuration * gm.Modifiers.LaneChangeDurationMultiplier;
        float speed = b.LaneWidth / Mathf.Max(duration, Mathf.Epsilon);
        Vector3 position = transform.position;
        position.x = Mathf.MoveTowards(position.x, lane * b.LaneWidth, speed * Time.deltaTime);
        transform.position = position;
    }

    private void OnTriggerEnter(Collider other)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != RoundPhase.Running) return;
        if (other.GetComponentInParent<Star>(true) != null) return; // true: la estrella se apaga al recogerla
        if (Time.time < invulnerableUntil) return;

        invulnerableUntil = Time.time + gm.Balance.InvulnerabilityTime;
        lane = 0;
        GameEvents.RaiseContestantHit(HitCause.Obstacle);
    }
}
