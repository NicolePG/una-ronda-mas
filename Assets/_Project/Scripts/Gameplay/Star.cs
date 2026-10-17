using UnityEngine;

/// <summary>
/// Estrella que se recoge al tocar al concursante (tag «Player»).
/// Normal: +1. Dorada: +3 (valores en BalanceConfig). Gira, flota y la atrae el Imán.
/// Necesita un Collider en modo Trigger.
/// </summary>
[DisallowMultipleComponent]
public class Star : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private const float MagnetArrivalFactor = 0.6f;    // llega al carril del jugador antes de alcanzarlo
    private const float MagnetSnapDistance = 0.05f;

    [Header("Valor")]
    [Tooltip("La dorada vale más puntos (ver BalanceConfig).")]
    [SerializeField] private bool isGolden;

    [Header("Animación")]
    [Tooltip("Grados por segundo.")]
    [SerializeField] private float spinSpeed = 120f;
    [Tooltip("Cuánto sube y baja (m).")]
    [SerializeField] private float bobHeight = 0.15f;
    [Tooltip("Velocidad del flotar.")]
    [SerializeField] private float bobSpeed = 2f;

    [Header("Efectos")]
    [Tooltip("Partícula de destello al recogerla (opcional, la hace Otoniel).")]
    [SerializeField] private GameObject collectEffectPrefab;

    private static Transform player;
    private Vector3 baseLocalPosition;
    private float bobPhase;
    private bool collected;

    public bool IsGolden => isGolden;

    public int GetValue(BalanceConfig balance) => isGolden ? balance.GoldenStarValue : balance.NormalStarValue;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        bobPhase = Random.value * Mathf.PI * 2f;

        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh == null) filter.sharedMesh = StarMeshBuilder.SharedMesh;

        Collider trigger = GetComponent<Collider>();
        if (trigger == null) Debug.LogWarning("Star: falta un Collider (Trigger).", this);
        else trigger.isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        ApplyMagnet();
        float bob = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
        transform.localPosition = baseLocalPosition + Vector3.up * bob;
    }

    private void ApplyMagnet()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.Modifiers.HasMagnet || gm.CurrentSpeed <= 0f) return;
        if (player == null && !FindPlayer()) return;

        Transform parent = transform.parent;
        Vector3 worldBase = parent != null ? parent.TransformPoint(baseLocalPosition) : baseLocalPosition;
        float ahead = worldBase.z - player.position.z;
        float dx = player.position.x - worldBase.x;
        BalanceConfig b = gm.Balance;
        if (ahead <= 0f || ahead > b.MagnetRange) return;
        if (Mathf.Abs(dx) < MagnetSnapDistance || Mathf.Abs(dx) > b.MagnetLateralReach) return;

        // Velocidad lateral para llegar al carril del jugador antes de que la estrella lo alcance.
        float timeToReach = Mathf.Max(ahead / gm.CurrentSpeed * MagnetArrivalFactor, Time.deltaTime);
        float step = Mathf.Abs(dx) / timeToReach * Time.deltaTime;
        worldBase.x = Mathf.MoveTowards(worldBase.x, player.position.x, step);
        baseLocalPosition = parent != null ? parent.InverseTransformPoint(worldBase) : worldBase;
    }

    private static bool FindPlayer()
    {
        GameObject found = GameObject.FindWithTag(PlayerTag);
        player = found != null ? found.transform : null;
        return player != null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !IsPlayer(other)) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != RoundPhase.Running) return;

        collected = true;
        gm.CollectStar(isGolden);
        if (collectEffectPrefab != null)
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity, transform.parent);
        gameObject.SetActive(false);
    }

    private static bool IsPlayer(Collider other)
    {
        if (other.CompareTag(PlayerTag)) return true;
        Rigidbody body = other.attachedRigidbody;
        return body != null && body.CompareTag(PlayerTag);
    }
}
