using System.Collections;
using UnityEngine;

/// <summary>Controla las expresiones de Tito y sus reacciones a los eventos de la partida.</summary>
public class HostFace : MonoBehaviour
{
    [Header("Partes de la cara")]
    [Tooltip("Raíz visual que tiembla cuando Tito miente.")]
    [SerializeField] private Transform faceVisual;
    [Tooltip("Ojo abierto para la expresión de guiño.")]
    [SerializeField] private GameObject winkOpenEye;
    [Tooltip("Párpado que reemplaza al ojo durante el guiño.")]
    [SerializeField] private GameObject winkClosedEye;
    [Tooltip("Sonrisa normal de Tito.")]
    [SerializeField] private GameObject smileMouth;
    [Tooltip("Boca abierta para la risa tramposa.")]
    [SerializeField] private GameObject laughMouth;
    [Tooltip("Peluquín que vuela cuando el jugador descubre una mentira.")]
    [SerializeField] private Transform wig;

    [Header("Animación")]
    [Tooltip("Desplazamiento máximo del temblor de Tito.")]
    [SerializeField, Min(0f)] private float trembleDistance = 0.08f;
    [Tooltip("Frecuencia del temblor mientras Tito miente.")]
    [SerializeField, Min(0f)] private float trembleFrequency = 22f;
    [Tooltip("Altura que alcanza el peluquín al salir volando.")]
    [SerializeField, Min(0f)] private float wigFlightHeight = 2f;
    [Tooltip("Duración del vuelo del peluquín.")]
    [SerializeField, Min(0.01f)] private float wigFlightDuration = 0.8f;

    private Vector3 visualHomePosition;
    private Vector3 wigHomePosition;
    private Quaternion wigHomeRotation;
    private bool isLying;
    private Coroutine wigFlight;

    private void Awake()
    {
        if (faceVisual == null || winkOpenEye == null || winkClosedEye == null
            || smileMouth == null || laughMouth == null || wig == null)
        {
            Debug.LogError("HostFace: asigna la raíz visual, ojos, bocas y peluquín.", this);
            enabled = false;
            return;
        }

        visualHomePosition = faceVisual.localPosition;
        wigHomePosition = wig.localPosition;
        wigHomeRotation = wig.localRotation;
        SetExpression(false, false);
    }

    private void OnEnable()
    {
        GameEvents.RuleAnnounced += HandleRuleAnnounced;
        GameEvents.PhaseChanged += HandlePhaseChanged;
        GameEvents.CheatCalled += HandleCheatCalled;
        GameEvents.ContestantHit += HandleContestantHit;
    }

    private void OnDisable()
    {
        GameEvents.RuleAnnounced -= HandleRuleAnnounced;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        GameEvents.CheatCalled -= HandleCheatCalled;
        GameEvents.ContestantHit -= HandleContestantHit;
        StopWigFlight();
        if (faceVisual != null) faceVisual.localPosition = visualHomePosition;
    }

    private void Update()
    {
        if (!isLying || faceVisual == null) return;
        float shake = Mathf.Sin(Time.unscaledTime * trembleFrequency) * trembleDistance;
        faceVisual.localPosition = visualHomePosition + new Vector3(shake, -shake * 0.5f, 0f);
    }

    private void HandleRuleAnnounced(RuleData rule, bool isLie)
    {
        isLying = isLie;
        if (isLie) SetExpression(false, false);
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Announcement)
        {
            ResetWig();
            isLying = GameManager.Instance != null && GameManager.Instance.Rules.IsLie;
            SetExpression(false, false);
        }
        else if (phase == RoundPhase.Running && !isLying)
        {
            SetExpression(true, false);
        }
    }

    private void HandleCheatCalled(bool correct)
    {
        isLying = false;
        if (correct)
        {
            SetExpression(false, false);
            StartWigFlight();
        }
        else
        {
            SetExpression(false, true);
        }
    }

    private void HandleContestantHit(HitCause cause)
    {
        if (GameManager.Instance == null || GameManager.Instance.Phase != RoundPhase.Running) return;
        isLying = false;
        SetExpression(false, true);
    }

    private void SetExpression(bool wink, bool laugh)
    {
        if (winkOpenEye != null) winkOpenEye.SetActive(!wink);
        if (winkClosedEye != null) winkClosedEye.SetActive(wink);
        if (smileMouth != null) smileMouth.SetActive(!laugh);
        if (laughMouth != null) laughMouth.SetActive(laugh);
    }

    private void StartWigFlight()
    {
        StopWigFlight();
        if (wig != null) wigFlight = StartCoroutine(FlyWig());
    }

    private IEnumerator FlyWig()
    {
        float elapsed = 0f;
        Vector3 startPosition = wigHomePosition;
        Quaternion startRotation = wigHomeRotation;

        while (elapsed < wigFlightDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / wigFlightDuration);
            wig.localPosition = startPosition + Vector3.up * (wigFlightHeight * t);
            wig.localRotation = startRotation * Quaternion.Euler(0f, 0f, 540f * t);
            yield return null;
        }

        wigFlight = null;
    }

    private void StopWigFlight()
    {
        if (wigFlight == null) return;
        StopCoroutine(wigFlight);
        wigFlight = null;
    }

    private void ResetWig()
    {
        StopWigFlight();
        if (wig == null) return;
        wig.localPosition = wigHomePosition;
        wig.localRotation = wigHomeRotation;
    }
}
