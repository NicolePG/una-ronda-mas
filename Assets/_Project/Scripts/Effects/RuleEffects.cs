using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Efectos visibles de la regla que realmente se aplica.
/// Apagón: luz ambiental casi negra, luces del estudio apagadas y solo el reflector del concursante
/// (también lo usa el evento sorpresa de apagón corto, ver SurpriseEvents).
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

    private bool blackoutActive;
    private bool ruleBlackout;
    private bool eventBlackout;
    private Color savedAmbient;
    private AmbientMode savedAmbientMode;

    private void OnEnable()
    {
        GameEvents.RuleApplied += HandleRuleApplied;
        GameEvents.PhaseChanged += HandlePhaseChanged;
        if (contestantSpot != null) contestantSpot.enabled = false;
    }

    private void OnDisable()
    {
        GameEvents.RuleApplied -= HandleRuleApplied;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        SetBlackout(false); // RenderSettings sobrevive al cambio de escena
    }

    private void HandleRuleApplied(RuleData rule)
    {
        ruleBlackout = rule != null && rule.Type == RuleType.Blackout;
        SetBlackout(ruleBlackout || eventBlackout);
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Running) return;
        ruleBlackout = false;
        eventBlackout = false;
        SetBlackout(false);
    }

    /// <summary>Apagón corto del evento sorpresa (se suma a la regla Apagón si la hubiera).</summary>
    public void SetEventBlackout(bool on)
    {
        eventBlackout = on;
        SetBlackout(ruleBlackout || eventBlackout);
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
