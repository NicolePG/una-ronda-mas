using System.Collections.Generic;
using UnityEngine;

public enum SegmentDifficulty { Easy, Mid, Hard }

/// <summary>
/// Raíz de un tramo de pista de 20 m. El pivote va al INICIO del tramo y el tramo se extiende hacia +Z.
/// Dejar libres de obstáculos los primeros y últimos 3 m (para que el cambio entre tramos siempre tenga camino).
/// Marcar qué obstáculos tiene: el generador lo usa para decidir desde qué ronda aparece.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class TrackSegment : MonoBehaviour
{
    // Solo para dibujar la guía en la escena (los valores de juego están en BalanceConfig).
    private const float GizmoLength = 20f;
    private const float GizmoLaneWidth = 2f;
    private const float GizmoEdgeMargin = 3f;

    [Header("Clasificación")]
    [Tooltip("El generador elige fácil, medio o difícil según la ronda.")]
    [SerializeField] private SegmentDifficulty difficulty = SegmentDifficulty.Easy;

    [Header("Obstáculos que contiene")]
    [Tooltip("Tiene barra alta (aparece desde la ronda 2).")]
    [SerializeField] private bool hasHighBar;
    [Tooltip("Tiene martillo péndulo (desde la ronda 3).")]
    [SerializeField] private bool hasHammer;
    [Tooltip("Tiene hueco al agua (desde la ronda 4).")]
    [SerializeField] private bool hasWaterGap;

    [Header("Caminos (reglas Sin salto y Gigante)")]
    [Tooltip("Marcar si NO se puede pasar sin saltar (barra baja o hueco en los 3 carriles). No sale con «Sin salto».")]
    [SerializeField] private bool requiresJump;
    [Tooltip("Marcar si NO se puede pasar sin deslizarse (barra alta en los 3 carriles). No sale con «Gigante».")]
    [SerializeField] private bool requiresSlide;

    [Header("Hologramas")]
    [Tooltip("Puntos vacíos (hijos) donde puede aparecer un holograma cuando Tito miente.")]
    [SerializeField] private Transform[] hologramSlots = new Transform[0];

    public SegmentDifficulty Difficulty => difficulty;
    public bool HasHighBar => hasHighBar;
    public bool HasHammer => hasHammer;
    public bool HasWaterGap => hasWaterGap;
    public bool RequiresJump => requiresJump;
    public bool RequiresSlide => requiresSlide;
    public IReadOnlyList<Transform> HologramSlots => hologramSlots;

    /// <summary>Suma de los puntos de todas las estrellas del tramo.</summary>
    public int GetStarValue(BalanceConfig balance)
    {
        if (balance == null) return 0;
        int total = 0;
        foreach (Star star in GetComponentsInChildren<Star>(true)) total += star.GetValue(balance);
        return total;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Para herramientas de Editor y pruebas.</summary>
    public void DebugSetup(SegmentDifficulty difficulty, bool hasHighBar, bool hasHammer, bool hasWaterGap,
        bool requiresJump, bool requiresSlide, Transform[] hologramSlots)
    {
        this.difficulty = difficulty;
        this.hasHighBar = hasHighBar;
        this.hasHammer = hasHammer;
        this.hasWaterGap = hasWaterGap;
        this.requiresJump = requiresJump;
        this.requiresSlide = requiresSlide;
        this.hologramSlots = hologramSlots ?? new Transform[0];
    }
#endif

    private void Reset() => ConfigureRigidbody();

    private void Awake() => ConfigureRigidbody();

    private void ConfigureRigidbody()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnDrawGizmos()
    {
        // Guía: borde del tramo, carriles y zonas que deben quedar libres.
        Gizmos.matrix = transform.localToWorldMatrix;
        float width = GizmoLaneWidth * 3f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(0f, 0f, GizmoLength * 0.5f), new Vector3(width, 0.01f, GizmoLength));
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        for (int lane = -1; lane <= 1; lane++)
        {
            float x = lane * GizmoLaneWidth;
            Gizmos.DrawLine(new Vector3(x, 0f, 0f), new Vector3(x, 0f, GizmoLength));
        }
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.15f);
        Gizmos.DrawCube(new Vector3(0f, 0f, GizmoEdgeMargin * 0.5f), new Vector3(width, 0.01f, GizmoEdgeMargin));
        Gizmos.DrawCube(new Vector3(0f, 0f, GizmoLength - GizmoEdgeMargin * 0.5f), new Vector3(width, 0.01f, GizmoEdgeMargin));
    }
}
