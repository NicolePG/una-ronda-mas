using UnityEngine;

/// <summary>
/// Haz de luz decorativo de las torres del estudio: barre de un lado a otro y se acelera al ganar la ronda.
/// El haz es un cono con material aditivo (no es una luz real, así no cuesta en WebGL).
/// </summary>
[DisallowMultipleComponent]
public class StudioLightBeam : MonoBehaviour
{
    [Tooltip("Grados que barre hacia cada lado.")]
    [SerializeField, Range(0f, 60f)] private float sweepAngle = 22f;
    [Tooltip("Barridos por segundo.")]
    [SerializeField, Min(0f)] private float sweepSpeed = 0.18f;
    [Tooltip("Punto de partida del barrido (0 a 1); cada haz con uno distinto.")]
    [SerializeField, Range(0f, 1f)] private float phase;
    [Tooltip("Cuánto más rápido barre mientras se celebra.")]
    [SerializeField, Min(1f)] private float celebrateSpeedMultiplier = 4f;
    [Tooltip("Segundos que dura la celebración.")]
    [SerializeField, Min(0f)] private float celebrateTime = 3f;

    private Quaternion baseRotation;
    private float sweep;
    private float celebrateUntil = -1f;

    private void Awake() => baseRotation = transform.localRotation;

    private void OnEnable() => GameEvents.RoundWon += HandleRoundWon;

    private void OnDisable() => GameEvents.RoundWon -= HandleRoundWon;

    private void HandleRoundWon(int round, int prizes) => celebrateUntil = Time.unscaledTime + celebrateTime;

    private void Update()
    {
        float speed = sweepSpeed * (Time.unscaledTime < celebrateUntil ? celebrateSpeedMultiplier : 1f);
        sweep += speed * Time.unscaledDeltaTime;
        float angle = Mathf.Sin((sweep + phase) * Mathf.PI * 2f) * sweepAngle;
        transform.localRotation = baseRotation * Quaternion.Euler(angle * 0.35f, angle, 0f);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman el estudio.</summary>
    public void EditorSetup(float phase, float sweepAngle)
    {
        this.phase = phase;
        this.sweepAngle = sweepAngle;
    }
#endif
}
