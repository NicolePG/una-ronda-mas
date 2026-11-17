using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Zonas oscuras (Otoniel): desde la ronda DarkZoneFromRound, uno o dos tramos de la pista se quedan a oscuras.
/// Un cartel «ZONA OSCURA» marca la entrada y, unos metros antes, flota una linterna dorada en un carril:
/// si el concursante pasa por ella, ve lejos dentro de la zona; si no, corre casi a ciegas con una luz corta
/// (solo brillan las estrellas). Cada linterna sirve para su zona.
/// Todo se ubica con los metros recorridos de TrackGenerator, así se mueve igual que la pista.
/// Con la regla Apagón no hay zonas (ya está todo oscuro). Números en BalanceConfig («Zonas oscuras»).
/// </summary>
[DisallowMultipleComponent]
public class DarkZones : MonoBehaviour
{
    /// <summary>Se acerca una zona oscura (todavía falta agarrar su linterna).</summary>
    public static event Action ZoneWarning;
    /// <summary>El concursante agarró la linterna.</summary>
    public static event Action FlashlightCollected;
    /// <summary>Entró en la zona: true = con linterna.</summary>
    public static event Action<bool> ZoneEntered;
    /// <summary>Salió de la zona (vuelve la luz).</summary>
    public static event Action ZoneExited;

    /// <summary>¿Está el concursante dentro de una zona oscura ahora?</summary>
    public static bool InZone { get; private set; }

    [Header("Partes (las pone la herramienta que arma Game.unity)")]
    [Tooltip("Apaga las luces del estudio y ajusta el reflector del concursante.")]
    [SerializeField] private RuleEffects ruleEffects;
    [Tooltip("Cuerpo de la linterna (Mat_Gold: dorado = algo bueno para agarrar).")]
    [SerializeField] private Material flashlightMaterial;
    [Tooltip("Vidrio de la linterna (Mat_StarLight, brilla).")]
    [SerializeField] private Material lensMaterial;
    [Tooltip("Cartel de la entrada (Mat_Red).")]
    [SerializeField] private Material signMaterial;
    [Tooltip("Postes del cartel y aro de la linterna (Mat_Grey).")]
    [SerializeField] private Material postMaterial;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Material fontMaterial;

    [Header("Linterna")]
    [Tooltip("Altura a la que flota (m).")]
    [SerializeField, Min(0.2f)] private float flashlightHeight = 1.1f;
    [SerializeField, Min(0.5f)] private float flashlightScale = 1.8f;
    [Tooltip("Grados por segundo que gira.")]
    [SerializeField] private float spinSpeed = 140f;
    [SerializeField, Min(0f)] private float bobHeight = 0.15f;
    [Tooltip("Distancia lateral máxima para agarrarla (m).")]
    [SerializeField, Min(0.2f)] private float pickupReach = 1f;
    [Tooltip("Ventana hacia adelante y hacia atrás del concursante para agarrarla (m).")]
    [SerializeField, Min(0.2f)] private float pickupDepth = 0.9f;
    [Tooltip("Luz de la linterna en la pista: alcance (m) e intensidad.")]
    [SerializeField, Min(0.5f)] private float glowRange = 4f;
    [SerializeField, Min(0f)] private float glowIntensity = 2.5f;
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.5f);
    [Tooltip("A cuántos metros de la linterna aparece el aviso «¡Zona oscura adelante!».")]
    [SerializeField, Min(10f)] private float warnDistance = 85f;

    [Header("Cartel de entrada")]
    [Tooltip("Altura del cartel: por encima de la cámara (y = 4) para no atravesarla.")]
    [SerializeField, Min(2f)] private float signHeight = 5.6f;
    [Tooltip("Distancia del poste al carril de afuera (m).")]
    [SerializeField, Min(0f)] private float signSideMargin = 1.9f;

    [Header("Ubicación")]
    [Tooltip("Metros libres entre una zona y la linterna de la siguiente.")]
    [SerializeField, Min(0f)] private float zoneGap = 15f;
    [Tooltip("Metros detrás del concursante donde se borran el cartel y la linterna.")]
    [SerializeField, Min(1f)] private float cleanupBehind = 12f;

    private class Zone
    {
        public float Start, End, PickupAt;
        public int Lane;
        public bool HasLight, Warned, Entered, Exited;
        public Transform Flashlight, Sign;
    }

    private readonly List<Zone> zones = new List<Zone>();
    private float spin;

    private void OnEnable()
    {
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        Clear();
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase != RoundPhase.Running) Clear();
    }

    // Reparte las zonas de la ronda dentro de la ventana, sin que se pisen.
    private void HandleRoundStarted(int round, int goal, float speed)
    {
        Clear();
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Track == null) return;
        BalanceConfig b = gm.Balance;
        int count = b.GetDarkZoneCount(round);
        if (count <= 0 || (gm.Rules != null && gm.Rules.IsApplied(RuleType.Blackout))) return;

        float distance = gm.Track.RoundDistance;
        float from = distance * b.DarkZoneWindowStart;
        float to = distance * b.DarkZoneWindowEnd;
        float need = b.FlashlightLead + b.DarkZoneLength + zoneGap;
        count = Mathf.Min(count, Mathf.FloorToInt((to - from) / need));
        if (count <= 0) return;

        float slot = (to - from) / count;
        for (int i = 0; i < count; i++)
        {
            float start = from + i * slot + b.FlashlightLead + UnityEngine.Random.Range(0f, slot - need);
            var zone = new Zone
            {
                Start = start,
                End = start + b.DarkZoneLength,
                PickupAt = start - b.FlashlightLead,
                Lane = UnityEngine.Random.Range(-1, 2),
            };
            zone.Flashlight = BuildFlashlight();
            zone.Sign = BuildSign(b);
            zones.Add(zone);
        }
        UpdateZones(gm);
    }

    private void Update()
    {
        if (zones.Count == 0) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != RoundPhase.Running || gm.Track == null) return;
        spin += spinSpeed * Time.deltaTime;
        UpdateZones(gm);
    }

    private void UpdateZones(GameManager gm)
    {
        BalanceConfig b = gm.Balance;
        float travelled = gm.Track.Travelled;
        float baseZ = gm.Track.transform.position.z;
        ContestantController contestant = ContestantController.Current;
        Vector3 player = contestant != null ? contestant.transform.position : Vector3.zero;

        foreach (Zone zone in zones)
        {
            if (zone.Flashlight != null)
            {
                float z = baseZ + zone.PickupAt - travelled;
                float x = zone.Lane * b.LaneWidth;
                zone.Flashlight.position = new Vector3(x, flashlightHeight + Mathf.Sin(Time.time * 3f) * bobHeight, z);
                zone.Flashlight.rotation = Quaternion.Euler(0f, spin, 0f);

                if (!zone.Warned && z <= warnDistance)
                {
                    zone.Warned = true;
                    ZoneWarning?.Invoke();
                }
                float dz = z - player.z;
                if (Mathf.Abs(dz) <= pickupDepth && Mathf.Abs(player.x - x) <= pickupReach) Collect(zone);
                else if (dz < -cleanupBehind) DestroyPart(ref zone.Flashlight); // se la perdió
            }

            if (zone.Sign != null)
            {
                float z = baseZ + zone.Start - travelled;
                zone.Sign.position = new Vector3(0f, 0f, z);
                if (z - player.z < -cleanupBehind) DestroyPart(ref zone.Sign);
            }

            if (!zone.Entered && travelled >= zone.Start)
            {
                zone.Entered = true;
                InZone = true;
                if (ruleEffects != null) ruleEffects.SetZoneDark(true, zone.HasLight);
                ZoneEntered?.Invoke(zone.HasLight);
            }
            if (zone.Entered && !zone.Exited && travelled >= zone.End)
            {
                zone.Exited = true;
                InZone = false;
                if (ruleEffects != null) ruleEffects.SetZoneDark(false, false);
                ZoneExited?.Invoke();
            }
        }
    }

    private void Collect(Zone zone)
    {
        zone.HasLight = true;
        DestroyPart(ref zone.Flashlight);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayStar(true);
        FlashlightCollected?.Invoke();
    }

    private void Clear()
    {
        foreach (Zone zone in zones)
        {
            DestroyPart(ref zone.Flashlight);
            DestroyPart(ref zone.Sign);
        }
        zones.Clear();
        if (InZone && ruleEffects != null) ruleEffects.SetZoneDark(false, false);
        InZone = false;
    }

    private static void DestroyPart(ref Transform part)
    {
        if (part != null) Destroy(part.gameObject);
        part = null;
    }

    // ---------- Piezas (primitivas, sin colisionadores) ----------

    private Transform BuildFlashlight()
    {
        var root = new GameObject("Flashlight").transform;
        root.SetParent(transform, false);

        // Tumbada: el largo de los cilindros (eje Y) queda horizontal.
        var model = new GameObject("Model").transform;
        model.SetParent(root, false);
        model.localRotation = Quaternion.Euler(90f, 0f, 0f);
        model.localScale = Vector3.one * flashlightScale;
        Part(PrimitiveType.Cylinder, model, flashlightMaterial, new Vector3(0f, -0.04f, 0f), new Vector3(0.16f, 0.2f, 0.16f));
        Part(PrimitiveType.Cylinder, model, postMaterial, new Vector3(0f, 0.2f, 0f), new Vector3(0.25f, 0.06f, 0.25f));
        Part(PrimitiveType.Sphere, model, lensMaterial, new Vector3(0f, 0.26f, 0f), new Vector3(0.21f, 0.04f, 0.21f));
        Part(PrimitiveType.Cylinder, model, postMaterial, new Vector3(0f, -0.08f, 0f), new Vector3(0.17f, 0.015f, 0.17f));

        var glow = root.gameObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = glowRange;
        glow.intensity = glowIntensity;
        glow.color = glowColor;
        glow.shadows = LightShadows.None;
        return root;
    }

    private Transform BuildSign(BalanceConfig b)
    {
        var root = new GameObject("DarkZoneSign").transform;
        root.SetParent(transform, false);
        float half = b.LaneWidth + signSideMargin;
        const float bannerHeight = 1.2f;
        float postHeight = signHeight + bannerHeight * 0.5f;

        Part(PrimitiveType.Cylinder, root, postMaterial, new Vector3(-half, postHeight * 0.5f, 0f), new Vector3(0.25f, postHeight * 0.5f, 0.25f));
        Part(PrimitiveType.Cylinder, root, postMaterial, new Vector3(half, postHeight * 0.5f, 0f), new Vector3(0.25f, postHeight * 0.5f, 0.25f));
        Part(PrimitiveType.Cube, root, signMaterial, new Vector3(0f, signHeight, 0f), new Vector3(half * 2f + 0.5f, bannerHeight, 0.25f));

        // Texto del lado que mira al concursante (la cámara mira hacia +Z).
        var label = new GameObject("Label").AddComponent<TextMeshPro>();
        label.transform.SetParent(root, false);
        label.transform.localPosition = new Vector3(0f, signHeight, -0.15f);
        if (font != null) label.font = font;
        if (fontMaterial != null) label.fontSharedMaterial = fontMaterial;
        label.text = "ZONA OSCURA";
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 1f;
        label.fontSizeMax = 14f;
        label.rectTransform.sizeDelta = new Vector2(half * 2f, bannerHeight * 0.85f);
        return root;
    }

    private static void Part(PrimitiveType type, Transform parent, Material material, Vector3 position, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        if (material != null) renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(RuleEffects ruleEffects, Material flashlightMaterial, Material lensMaterial,
        Material signMaterial, Material postMaterial, TMP_FontAsset font, Material fontMaterial)
    {
        this.ruleEffects = ruleEffects;
        this.flashlightMaterial = flashlightMaterial;
        this.lensMaterial = lensMaterial;
        this.signMaterial = signMaterial;
        this.postMaterial = postMaterial;
        this.font = font;
        this.fontMaterial = fontMaterial;
    }
#endif
}
