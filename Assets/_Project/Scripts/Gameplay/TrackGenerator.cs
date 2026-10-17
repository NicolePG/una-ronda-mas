using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea tramos adelante del concursante, los mueve hacia −Z y los borra al pasar.
/// Lo controla GameManager: BuildRound() al anunciar cada ronda y Advance() en cada frame de Running.
/// El arco de meta se pone a la distancia exacta que recorre la pista en la ronda, así llega a z = 0 justo a tiempo.
/// </summary>
[DisallowMultipleComponent]
public class TrackGenerator : MonoBehaviour
{
    // Medidas de las piezas de respaldo (solo se usan si faltan prefabs).
    private const float FallbackFloorThickness = 0.2f;
    private const float FallbackSideMargin = 0.4f;
    private const float FallbackArchHeight = 4f;
    private const float FallbackArchThickness = 0.4f;

    [Header("Datos")]
    [Tooltip("Si se deja vacío, se usa el de GameManager.")]
    [SerializeField] private BalanceConfig balance;

    [Header("Tramos")]
    [Tooltip("Prefabs con TrackSegment (fáciles, medios y difíciles).")]
    [SerializeField] private TrackSegment[] segmentPrefabs = new TrackSegment[0];
    [Tooltip("Tramo sin obstáculos ni estrellas para el inicio y después de la meta. Si está vacío se crea un piso simple.")]
    [SerializeField] private GameObject safeSegmentPrefab;

    [Header("Meta y hologramas")]
    [Tooltip("Arco de meta (solo visual). Si está vacío se crea uno simple.")]
    [SerializeField] private GameObject finishArchPrefab;
    [Tooltip("Obstáculo falso (isFake = true) que aparece en los HologramSlots cuando Tito miente.")]
    [SerializeField] private GameObject hologramPrefab;

    [Header("Respaldo (si faltan prefabs)")]
    [SerializeField] private Material fallbackTrackMaterial;
    [SerializeField] private Material fallbackArchMaterial;

    private struct ActiveSegment
    {
        public GameObject Instance;
        public float Start;     // distancia en la pista donde empieza
    }

    private static readonly SegmentDifficulty[] Difficulties =
        { SegmentDifficulty.Easy, SegmentDifficulty.Mid, SegmentDifficulty.Hard };

    private readonly List<ActiveSegment> active = new List<ActiveSegment>();
    private readonly List<TrackSegment> candidates = new List<TrackSegment>();
    private readonly List<TrackSegment> pool = new List<TrackSegment>();
    private GameObject finishArch;
    private TrackSegment lastPicked;
    private RuleData rule;
    private bool spawnHolograms;
    private bool warnedUnlockFallback;
    private float nextStart;
    private int round;

    /// <summary>Metros recorridos en la ronda.</summary>
    public float Travelled { get; private set; }

    /// <summary>Metros que recorre la pista en toda la ronda (donde está el arco).</summary>
    public float RoundDistance { get; private set; }

    /// <summary>Posición z del arco de meta respecto al generador (0 = el concursante).</summary>
    public float FinishArchZ => RoundDistance - Travelled;

    /// <summary>Puntos de estrella creados antes de la meta en esta ronda (para balance).</summary>
    public int SpawnedStarValue { get; private set; }

    public int ActiveSegmentCount => active.Count;
    public GameObject FinishArch => finishArch;

    private BalanceConfig Balance =>
        balance != null ? balance : (GameManager.Instance != null ? GameManager.Instance.Balance : null);

    /// <summary>Borra la pista y arma una nueva para la ronda.</summary>
    public void BuildRound(int round, RuleData appliedRule, bool isLie, float roundDistance)
    {
        BalanceConfig b = Balance;
        if (b == null)
        {
            Debug.LogError("TrackGenerator: falta BalanceConfig.", this);
            return;
        }

        Clear();
        this.round = round;
        rule = appliedRule;
        spawnHolograms = isLie;
        warnedUnlockFallback = false;
        RoundDistance = Mathf.Max(0f, roundDistance);
        Travelled = 0f;
        SpawnedStarValue = 0;
        nextStart = -b.SegmentLength; // un tramo detrás del concursante para que la cámara vea piso

        finishArch = finishArchPrefab != null ? Instantiate(finishArchPrefab, transform) : CreateFallbackArch(b);
        finishArch.name = "FinishArch";

        FillAhead(b);
        UpdatePositions(b);
    }

    /// <summary>Mueve la pista hacia el concursante.</summary>
    public void Advance(float distance)
    {
        if (distance > 0f) SetTravelled(Travelled + distance);
    }

    /// <summary>Pone la pista en los metros recorridos indicados (GameManager usa velocidad × tiempo, sin acumular error).</summary>
    public void SetTravelled(float travelled)
    {
        BalanceConfig b = Balance;
        if (b == null || travelled <= Travelled) return;
        Travelled = travelled;
        RecycleBehind(b);
        FillAhead(b);
        UpdatePositions(b);
    }

    /// <summary>Borra todos los tramos y el arco.</summary>
    public void Clear()
    {
        foreach (ActiveSegment segment in active)
        {
            if (segment.Instance != null) SafeDestroy(segment.Instance);
        }
        active.Clear();
        if (finishArch != null) SafeDestroy(finishArch);
        finishArch = null;
        lastPicked = null;
    }

    /// <summary>¿Puede salir este tramo en esta ronda con esta regla?</summary>
    public static bool IsSegmentAllowed(TrackSegment segment, int round, RuleData appliedRule, BalanceConfig b)
    {
        if (segment.HasHighBar && round < b.HighBarFromRound) return false;
        if (segment.HasHammer && round < b.HammerFromRound) return false;
        if (segment.HasWaterGap && round < b.WaterGapFromRound) return false;
        return PassesRule(segment, appliedRule);
    }

    /// <summary>Siempre debe haber camino: sin salto no hay tramos que obliguen a saltar; gigante no puede deslizarse.</summary>
    public static bool PassesRule(TrackSegment segment, RuleData appliedRule)
    {
        if (appliedRule == null) return true;
        if (appliedRule.Type == RuleType.NoJump && segment.RequiresJump) return false;
        if (appliedRule.Type == RuleType.Giant && segment.RequiresSlide) return false;
        return true;
    }

    private void FillAhead(BalanceConfig b)
    {
        while (nextStart - Travelled < b.SpawnAheadDistance) SpawnNext(b);
    }

    private void SpawnNext(BalanceConfig b)
    {
        float start = nextStart;
        nextStart += b.SegmentLength;

        bool safe = start < b.SafeStartSegments * b.SegmentLength || start >= RoundDistance;
        TrackSegment prefab = safe ? null : PickSegment(b);
        GameObject instance;

        if (prefab != null)
        {
            TrackSegment segment = Instantiate(prefab, transform);
            instance = segment.gameObject;
            lastPicked = prefab;
            if (start < RoundDistance) SpawnedStarValue += segment.GetStarValue(b);
            if (spawnHolograms) AddHolograms(segment, b);
        }
        else
        {
            instance = safeSegmentPrefab != null ? Instantiate(safeSegmentPrefab, transform) : CreateFallbackSegment(b);
        }

        active.Add(new ActiveSegment { Instance = instance, Start = start });
    }

    private TrackSegment PickSegment(BalanceConfig b)
    {
        if (segmentPrefabs == null || segmentPrefabs.Length == 0) return null;

        candidates.Clear();
        foreach (TrackSegment segment in segmentPrefabs)
        {
            if (segment != null && IsSegmentAllowed(segment, round, rule, b)) candidates.Add(segment);
        }
        if (candidates.Count == 0)
        {
            // Respaldo: ignora la ronda de desbloqueo, pero nunca la regla (siempre debe haber camino).
            foreach (TrackSegment segment in segmentPrefabs)
            {
                if (segment != null && PassesRule(segment, rule)) candidates.Add(segment);
            }
            if (candidates.Count > 0 && !warnedUnlockFallback)
            {
                warnedUnlockFallback = true;
                Debug.LogWarning($"TrackGenerator: no hay tramos desbloqueados para la ronda {round}; se usan otros.", this);
            }
        }
        if (candidates.Count == 0) return null;

        SegmentDifficulty difficulty = PickDifficulty(b.GetDifficultyStage(round));
        pool.Clear();
        foreach (TrackSegment segment in candidates)
        {
            if (segment.Difficulty == difficulty) pool.Add(segment);
        }
        if (pool.Count == 0) pool.AddRange(candidates);
        if (pool.Count > 1) pool.Remove(lastPicked); // no repetir el mismo tramo seguido
        return pool[Random.Range(0, pool.Count)];
    }

    private SegmentDifficulty PickDifficulty(DifficultyStage stage)
    {
        // Solo cuentan las dificultades que tienen algún tramo disponible.
        float total = 0f;
        foreach (SegmentDifficulty d in Difficulties)
        {
            if (HasCandidate(d)) total += stage.GetWeight(d);
        }
        if (total <= 0f) return candidates[Random.Range(0, candidates.Count)].Difficulty;

        float roll = Random.value * total;
        SegmentDifficulty chosen = candidates[0].Difficulty;
        foreach (SegmentDifficulty d in Difficulties)
        {
            float weight = HasCandidate(d) ? stage.GetWeight(d) : 0f;
            if (weight <= 0f) continue;
            chosen = d;
            if (roll < weight) break;
            roll -= weight;
        }
        return chosen;
    }

    private bool HasCandidate(SegmentDifficulty difficulty)
    {
        foreach (TrackSegment segment in candidates)
        {
            if (segment.Difficulty == difficulty) return true;
        }
        return false;
    }

    private void AddHolograms(TrackSegment segment, BalanceConfig b)
    {
        if (hologramPrefab == null) return;
        foreach (Transform slot in segment.HologramSlots)
        {
            if (slot != null && Random.value < b.HologramChancePerSlot)
                Instantiate(hologramPrefab, slot.position, slot.rotation, slot);
        }
    }

    private void RecycleBehind(BalanceConfig b)
    {
        while (active.Count > 0 && active[0].Start + b.SegmentLength - Travelled < b.DestroyBehindZ)
        {
            if (active[0].Instance != null) SafeDestroy(active[0].Instance);
            active.RemoveAt(0);
        }
    }

    private void UpdatePositions(BalanceConfig b)
    {
        Vector3 origin = transform.position;
        foreach (ActiveSegment segment in active)
        {
            if (segment.Instance != null)
                segment.Instance.transform.position = origin + Vector3.forward * (segment.Start - Travelled);
        }

        if (finishArch != null)
        {
            float z = FinishArchZ;
            finishArch.transform.position = origin + Vector3.forward * z;
            bool visible = z <= b.SpawnAheadDistance + b.SegmentLength;
            if (finishArch.activeSelf != visible) finishArch.SetActive(visible);
        }
    }

    // ---------- Piezas de respaldo ----------

    private GameObject CreateFallbackSegment(BalanceConfig b)
    {
        var root = new GameObject("Segment_Safe (respaldo)");
        root.transform.SetParent(transform, false);
        float width = b.LaneWidth * 3f + FallbackSideMargin * 2f;
        CreatePart(PrimitiveType.Cube, root.transform, "Floor",
            new Vector3(0f, -FallbackFloorThickness * 0.5f, b.SegmentLength * 0.5f),
            new Vector3(width, FallbackFloorThickness, b.SegmentLength), fallbackTrackMaterial);
        return root;
    }

    private GameObject CreateFallbackArch(BalanceConfig b)
    {
        var root = new GameObject("FinishArch (respaldo)");
        root.transform.SetParent(transform, false);
        float halfWidth = b.LaneWidth * 1.5f + FallbackSideMargin + FallbackArchThickness;
        foreach (float side in new[] { -1f, 1f })
        {
            CreatePart(PrimitiveType.Cube, root.transform, "Post",
                new Vector3(side * halfWidth, FallbackArchHeight * 0.5f, 0f),
                new Vector3(FallbackArchThickness, FallbackArchHeight, FallbackArchThickness), fallbackArchMaterial);
        }
        CreatePart(PrimitiveType.Cube, root.transform, "Top",
            new Vector3(0f, FallbackArchHeight, 0f),
            new Vector3(halfWidth * 2f + FallbackArchThickness, FallbackArchThickness, FallbackArchThickness), fallbackArchMaterial);
        return root;
    }

    private static void CreatePart(PrimitiveType type, Transform parent, string name, Vector3 localPosition,
        Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        SafeDestroy(part.GetComponent<Collider>()); // solo visual
        if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void SafeDestroy(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
