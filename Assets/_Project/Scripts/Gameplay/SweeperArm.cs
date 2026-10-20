using UnityEngine;

/// <summary>
/// Rodillo barredor: un brazo bajo que gira sobre un poste corto y barre los 3 carriles. Se pasa saltando.
/// Grados por segundo en BalanceConfig («Obstáculos que se mueven»).
/// </summary>
[DisallowMultipleComponent]
public class SweeperArm : MonoBehaviour
{
    [Tooltip("Punto de giro (centro del poste). El brazo y su collider van como hijos.")]
    [SerializeField] private Transform pivot;
    [Tooltip("Cada rodillo empieza en un ángulo distinto.")]
    [SerializeField] private bool randomStart = true;
    [Tooltip("Gira al revés (para variar entre tramos).")]
    [SerializeField] private bool reverse;

    private void Awake()
    {
        if (pivot == null) pivot = transform;
        if (randomStart) pivot.localRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        float speed = gm != null ? gm.Balance.SweeperDegreesPerSecond : 0f;
        pivot.Rotate(0f, (reverse ? -speed : speed) * Time.deltaTime, 0f, Space.Self);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los prefabs.</summary>
    public void EditorSetup(Transform pivot, bool reverse)
    {
        this.pivot = pivot;
        this.reverse = reverse;
    }
#endif
}
