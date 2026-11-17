using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Efectos visibles de la regla que realmente se aplica.
/// Apagón: luz ambiental casi negra, luces del estudio apagadas y solo el reflector del concursante
/// (también lo usa el evento sorpresa de apagón corto, ver SurpriseEvents, y las zonas oscuras, ver DarkZones).
/// Espejo, Gigante y Sin salto los resuelve ContestantController.
/// </summary>
[DisallowMultipleComponent]
public class RuleEffects : MonoBehaviour
{
    [Header("Luces")]
    [Tooltip("Luz direccional principal.")]
    [SerializeField] private Light mainLight;
    [Tooltip("Focos de colores del estudio.")]
    [SerializeField] private Light[] studioLights = new Light[0];
    [Tooltip("Reflector que sale del concursante hacia adelante (apagado normalmente).")]
    [SerializeField] private Light contestantSpot;

    [Header("Apagón")]
    [SerializeField] private Color blackoutAmbient = new Color(0.02f, 0.02f, 0.04f);

    [Header("Zonas oscuras (alcance e intensidad en BalanceConfig)")]
    [Tooltip("Apertura del reflector en una zona oscura sin linterna (grados).")]
    [SerializeField, Range(10f, 120f)] private float darkSpotAngle = 40f;
    [Tooltip("Apertura del reflector con linterna (grados).")]
    [SerializeField, Range(10f, 120f)] private float flashlightSpotAngle = 62f;

    private bool blackoutActive;
    private bool ruleBlackout;
    private bool eventBlackout;
    private bool zoneDark;
    private bool zoneFlashlight;
    private Color savedAmbient;
    private AmbientMode savedAmbientMode;
    private bool spotDefaultsSaved;
    private float defaultSpotRange, defaultSpotIntensity, defaultSpotAngle;

    private bool DarkNow => ruleBlackout || eventBlackout || zoneDark;

    private void OnEnable()
    {
        GameEvents.RuleApplied += HandleRuleApplied;
        GameEvents.PhaseChanged += HandlePhaseChanged;
        SaveSpotDefaults();
        if (contestantSpot != null) contestantSpot.enabled = false;
    }

    private void OnDisable()
    {
        GameEvents.RuleApplied -= HandleRuleApplied;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        zoneDark = zoneFlashlight = false;
        ApplySpotSettings();
        SetBlackout(false); // RenderSettings sobrevive al cambio de escena
    }

    private void HandleRuleApplied(RuleData rule)
    {
        ruleBlackout = rule != null && rule.Type == RuleType.Blackout;
        SetBlackout(DarkNow);
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Running) return;
        ruleBlackout = false;
        eventBlackout = false;
        zoneDark = zoneFlashlight = false;
        ApplySpotSettings();
        SetBlackout(false);
    }

    /// <summary>Apagón corto del evento sorpresa (se suma a la regla Apagón si la hubiera).</summary>
    public void SetEventBlackout(bool on)
    {
        eventBlackout = on;
        SetBlackout(DarkNow);
    }

    /// <summary>Zona oscura (DarkZones): todo se apaga y el reflector alumbra lejos solo con la linterna.</summary>
    public void SetZoneDark(bool on, bool flashlight)
    {
        zoneDark = on;
        zoneFlashlight = on && flashlight;
        ApplySpotSettings();
        SetBlackout(DarkNow);
    }

    private void SaveSpotDefaults()
    {
        if (spotDefaultsSaved || contestantSpot == null) return;
        spotDefaultsSaved = true;
        defaultSpotRange = contestantSpot.range;
        defaultSpotIntensity = contestantSpot.intensity;
        defaultSpotAngle = contestantSpot.spotAngle;
    }

    // En la zona oscura la luz es corta (a ciegas) o larga (linterna); fuera de ella, la del apagón normal.
    private void ApplySpotSettings()
    {
        SaveSpotDefaults();
        if (contestantSpot == null || !spotDefaultsSaved) return;
        BalanceConfig b = GameManager.Instance != null ? GameManager.Instance.Balance : null;
        if (zoneDark && b != null)
        {
            contestantSpot.range = zoneFlashlight ? b.FlashlightSpotRange : b.DarkSpotRange;
            contestantSpot.intensity = zoneFlashlight ? b.FlashlightSpotIntensity : b.DarkSpotIntensity;
            contestantSpot.spotAngle = zoneFlashlight ? flashlightSpotAngle : darkSpotAngle;
        }
        else
        {
            contestantSpot.range = defaultSpotRange;
            contestantSpot.intensity = defaultSpotIntensity;
            contestantSpot.spotAngle = defaultSpotAngle;
        }
    }

    private void SetBlackout(bool on)
    {
        if (on == blackoutActive) return;
        blackoutActive = on;

        if (on)
        {
            savedAmbient = RenderSettings.ambientLight;
            savedAmbientMode = RenderSettings.ambientMode;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = blackoutAmbient;
        }
        else
        {
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientLight = savedAmbient;
        }

        if (mainLight != null) mainLight.enabled = !on;
        foreach (Light l in studioLights)
        {
            if (l != null) l.enabled = !on;
        }
        if (contestantSpot != null) contestantSpot.enabled = on;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(Light mainLight, Light[] studioLights, Light contestantSpot)
    {
        this.mainLight = mainLight;
        this.studioLights = studioLights;
        this.contestantSpot = contestantSpot;
    }
#endif
}
