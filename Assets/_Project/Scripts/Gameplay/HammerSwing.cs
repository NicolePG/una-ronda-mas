using UnityEngine;

/// <summary>Balancea el martillo péndulo de un carril a otro con una onda seno.</summary>
[DisallowMultipleComponent]
public class HammerSwing : MonoBehaviour
{
    [Tooltip("Punto de giro (arriba, en la viga). El martillo y su collider van como hijos.")]
    [SerializeField] private Transform pivot;
    [Tooltip("Ángulo máximo hacia cada lado (grados).")]
    [SerializeField, Range(0f, 90f)] private float amplitude = 50f;
    [Tooltip("Segundos de una ida y vuelta completa.")]
    [SerializeField, Min(0.1f)] private float period = 2.6f;
    [Tooltip("Cada martillo empieza en un punto distinto del balanceo.")]
    [SerializeField] private bool randomPhase = true;

    private float phase;

    private void Awake()
    {
        if (pivot == null) pivot = transform;
        phase = randomPhase ? Random.value : 0f;
    }

    private void Update()
    {
        float angle = amplitude * Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f);
        pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los prefabs.</summary>
    public void EditorSetup(Transform pivot) => this.pivot = pivot;
#endif
}
