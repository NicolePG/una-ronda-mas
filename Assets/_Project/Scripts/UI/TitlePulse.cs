using UnityEngine;

/// <summary>Logo del menú que «late» y se balancea un poco, como un cartel de programa de TV.</summary>
[DisallowMultipleComponent]
public class TitlePulse : MonoBehaviour
{
    [Tooltip("Cuánto crece en cada latido (0,04 = 4%).")]
    [SerializeField, Min(0f)] private float scaleAmount = 0.04f;
    [Tooltip("Latidos por segundo.")]
    [SerializeField, Min(0f)] private float beatsPerSecond = 0.8f;
    [Tooltip("Grados que se balancea hacia cada lado.")]
    [SerializeField, Range(0f, 10f)] private float swayAngle = 1.5f;

    private Vector3 baseScale;
    private Quaternion baseRotation;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        // Latido corto y marcado: sube rápido y baja suave.
        float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * beatsPerSecond * Mathf.PI)), 8f);
        transform.localScale = baseScale * (1f + beat * scaleAmount);
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * swayAngle);
    }
}
