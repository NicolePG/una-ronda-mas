using UnityEngine;

/// <summary>
/// Arco de meta inflable (solo visual; el temporizador manda). Rebota un poco y tira confeti al ganar la ronda.
/// Lo crea TrackGenerator en cada ronda a partir del prefab Prefabs/Gameplay/FinishArch.
/// </summary>
[DisallowMultipleComponent]
public class FinishArch : MonoBehaviour
{
    [Tooltip("Confeti que se lanza al ganar.")]
    [SerializeField] private ParticleSystem confetti;
    [Tooltip("Parte visual que rebota como inflable.")]
    [SerializeField] private Transform visual;
    [SerializeField, Min(0f)] private float bounceAmount = 0.05f;
    [SerializeField, Min(0f)] private float bounceSpeed = 3f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (visual != null) baseScale = visual.localScale;
    }

    private void OnEnable() => GameEvents.RoundWon += HandleRoundWon;

    private void OnDisable() => GameEvents.RoundWon -= HandleRoundWon;

    private void HandleRoundWon(int round, int prizes)
    {
        if (confetti != null) confetti.Play(true);
    }

    private void Update()
    {
        if (visual == null) return;
        float s = 1f + Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
        visual.localScale = new Vector3(baseScale.x * s, baseScale.y * (2f - s), baseScale.z * s);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los prefabs.</summary>
    public void EditorSetup(ParticleSystem confetti, Transform visual)
    {
        this.confetti = confetti;
        this.visual = visual;
    }
#endif
}
