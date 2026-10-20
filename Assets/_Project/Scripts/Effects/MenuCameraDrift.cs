using UnityEngine;

/// <summary>Cámara del menú que se mece despacio (como una grúa de TV), con tiempo sin escala.</summary>
[DisallowMultipleComponent]
public class MenuCameraDrift : MonoBehaviour
{
    [Tooltip("Grados que gira hacia cada lado.")]
    [SerializeField, Range(0f, 20f)] private float yawAmount = 3f;
    [Tooltip("Metros que sube y baja.")]
    [SerializeField, Min(0f)] private float bobAmount = 0.15f;
    [Tooltip("Metros que se acerca y aleja.")]
    [SerializeField, Min(0f)] private float dollyAmount = 0.4f;
    [Tooltip("Segundos de un vaivén completo.")]
    [SerializeField, Min(1f)] private float period = 14f;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float t = Time.unscaledTime / period * Mathf.PI * 2f;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(t) * yawAmount, 0f);
        transform.localPosition = basePosition
            + Vector3.up * Mathf.Sin(t * 1.7f) * bobAmount
            + transform.forward * Mathf.Sin(t * 0.8f) * dollyAmount;
    }
}
