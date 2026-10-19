using UnityEngine;

/// <summary>
/// Obstáculo inflable de la pista. Necesita un Collider en modo Trigger (en este objeto o en un hijo).
/// Si isFake está activo es un holograma de Tito: se ve, pero se atraviesa sin daño.
/// </summary>
[DisallowMultipleComponent]
public class Obstacle : MonoBehaviour
{
    [Header("Tipo")]
    [Tooltip("Holograma: el concursante lo atraviesa sin perder vida.")]
    [SerializeField] private bool isFake;

    [Header("Animación")]
    [Tooltip("Parte visual que respira y vibra (sin collider).")]
    [SerializeField] private Transform visual;
    [Tooltip("Cuánto se infla y desinfla (0,04 = 4%).")]
    [SerializeField, Min(0f)] private float breatheAmount = 0.04f;
    [Tooltip("Velocidad de la respiración.")]
    [SerializeField, Min(0f)] private float breatheSpeed = 2f;
    [Tooltip("Segundos que vibra al chocarlo.")]
    [SerializeField, Min(0f)] private float wobbleTime = 0.35f;
    [Tooltip("Fuerza de la vibración al chocarlo.")]
    [SerializeField, Min(0f)] private float wobbleAmount = 0.15f;

    private Vector3 baseScale = Vector3.one;
    private float phase;
    private float wobbleUntil;

    public bool IsFake => isFake;

    private void Awake()
    {
        if (visual != null) baseScale = visual.localScale;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        if (visual == null) return;
        float breathe = 1f + Mathf.Sin(Time.time * breatheSpeed + phase) * breatheAmount;
        float wobble = 0f;
        if (Time.time < wobbleUntil)
        {
            float left = (wobbleUntil - Time.time) / Mathf.Max(wobbleTime, Mathf.Epsilon);
            wobble = Mathf.Sin(Time.time * 60f) * wobbleAmount * left;
        }
        visual.localScale = new Vector3(baseScale.x * (breathe + wobble), baseScale.y * (breathe - wobble), baseScale.z * breathe);
    }

    /// <summary>El concursante lo chocó: vibra un momento.</summary>
    public void Wobble() => wobbleUntil = Time.time + wobbleTime;

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los prefabs.</summary>
    public void EditorSetup(bool isFake, Transform visual)
    {
        this.isFake = isFake;
        this.visual = visual;
    }
#endif
}
