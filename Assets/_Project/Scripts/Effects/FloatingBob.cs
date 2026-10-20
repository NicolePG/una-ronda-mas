using UnityEngine;

/// <summary>Flota en el agua: sube, baja y gira despacio (flotadores de decoración).</summary>
[DisallowMultipleComponent]
public class FloatingBob : MonoBehaviour
{
    [Tooltip("Cuánto sube y baja (m).")]
    [SerializeField, Min(0f)] private float bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float bobSpeed = 1.3f;
    [Tooltip("Grados por segundo que gira.")]
    [SerializeField] private float spinSpeed = 12f;
    [Tooltip("Grados que se ladea con las olas.")]
    [SerializeField, Min(0f)] private float tilt = 6f;

    private Vector3 home;
    private Quaternion baseRotation;
    private float seed;

    private void Awake()
    {
        home = transform.localPosition;
        baseRotation = transform.localRotation;
        seed = Random.value * 10f;
    }

    private void Update()
    {
        float t = Time.time * bobSpeed + seed;
        transform.localPosition = home + Vector3.up * (Mathf.Sin(t) * bobHeight);
        transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * tilt, Time.time * spinSpeed + seed * 36f, Mathf.Cos(t * 0.8f) * tilt) * baseRotation;
    }
}
