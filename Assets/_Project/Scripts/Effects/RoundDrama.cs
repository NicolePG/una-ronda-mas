using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Emoción de la ronda (Otoniel): destello rojo y micro-pausa al chocar, borde rojo que late en los últimos
/// segundos, cuenta regresiva grande, cámara lenta al cruzar la meta, cartel de racha y cañones de confeti.
/// Los números de juego están en BalanceConfig («Emoción»); aquí solo los de la animación.
/// Respeta la pausa: nunca cambia Time.timeScale mientras el juego está pausado.
/// </summary>
[DisallowMultipleComponent]
public class RoundDrama : MonoBehaviour
{
    [Header("Partes")]
    [SerializeField] private Image hitFlash;
    [SerializeField] private Image dangerVignette;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private RectTransform streakBanner;
    [SerializeField] private TMP_Text streakTitle;
    [SerializeField] private TMP_Text streakBonus;
    [SerializeField] private CanvasGroup streakChip;
    [SerializeField] private TMP_Text streakChipText;
    [Tooltip("Cañones de confeti del estudio (se disparan al ganar la ronda).")]
    [SerializeField] private ParticleSystem[] confettiCannons = new ParticleSystem[0];

    [Header("Destello al chocar")]
    [SerializeField, Range(0f, 1f)] private float hitFlashAlpha = 0.45f;
    [SerializeField, Min(0.05f)] private float hitFlashTime = 0.35f;

    [Header("Peligro y cuenta regresiva")]
    [SerializeField, Range(0f, 1f)] private float dangerAlphaMin = 0.12f;
    [SerializeField, Range(0f, 1f)] private float dangerAlphaMax = 0.45f;
    [Tooltip("Latidos por segundo del borde rojo (al final late el doble).")]
    [SerializeField, Min(0.1f)] private float dangerBeats = 1.3f;
    [Tooltip("Borde rojo suave cuando queda una sola vida.")]
    [SerializeField, Range(0f, 1f)] private float lastLifeAlpha = 0.14f;
    [SerializeField] private Color countdownColor = new Color(0.82f, 0.23f, 0.23f);
    [SerializeField] private Color countdownGoalColor = new Color(0.97f, 0.69f, 0.11f);

    [Header("Racha")]
    [SerializeField, Min(0.1f)] private float streakBannerTime = 1.4f;
    [Tooltip("Desde qué racha se muestra el contador chico.")]
    [SerializeField, Min(1)] private int streakChipFrom = 3;

    private float hitFlashAt = -10f;
    private float hitStopUntil = -10f;
    private bool controllingTime;
    private int lastCountdown = -1;
    private float countdownAt = -10f;
    private float bannerAt = -10f;
    private float chipBumpAt = -10f;
    private int streak;

    private void OnEnable()
    {
        GameEvents.ContestantHit += HandleHit;
        GameEvents.RoundWon += HandleRoundWon;
        GameEvents.PhaseChanged += HandlePhaseChanged;
        StarStreak.StreakChanged += HandleStreakChanged;
        StarStreak.MilestoneReached += HandleMilestone;
    }

    private void OnDisable()
    {
        GameEvents.ContestantHit -= HandleHit;
        GameEvents.RoundWon -= HandleRoundWon;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        StarStreak.StreakChanged -= HandleStreakChanged;
        StarStreak.MilestoneReached -= HandleMilestone;
        if (controllingTime) Time.timeScale = 1f;
        controllingTime = false;
    }

    private void Start()
    {
        SetAlpha(hitFlash, 0f);
        SetAlpha(dangerVignette, 0f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (streakBanner != null) streakBanner.gameObject.SetActive(false);
        if (streakChip != null) streakChip.alpha = 0f;
    }

    private void HandleHit(HitCause cause)
    {
        GameManager gm = GameManager.Instance;
        hitFlashAt = Time.unscaledTime;
        if (gm != null && !gm.IsPaused) hitStopUntil = Time.unscaledTime + gm.Balance.HitStopDuration;
    }

    private void HandleRoundWon(int round, int prizes)
    {
        foreach (ParticleSystem cannon in confettiCannons)
        {
            if (cannon != null) cannon.Play(true);
        }
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        lastCountdown = -1;
        if (phase != RoundPhase.Running && countdownText != null) countdownText.gameObject.SetActive(false);
    }

    private void HandleStreakChanged(int value)
    {
        streak = value;
        if (value >= streakChipFrom)
        {
            if (streakChipText != null) streakChipText.text = $"RACHA ×{value}";
            chipBumpAt = Time.unscaledTime;
        }
    }

    private void HandleMilestone(int value, int bonus)
    {
        if (streakTitle != null) streakTitle.text = $"¡RACHA ×{value}!";
        if (streakBonus != null) streakBonus.text = bonus > 0 ? $"+{bonus} {(bonus == 1 ? "estrella" : "estrellas")}" : "";
        bannerAt = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayStar(true);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        float now = Time.unscaledTime;
        UpdateTimeScale(gm, now);

        // Destello rojo.
        float flash = 1f - (now - hitFlashAt) / hitFlashTime;
        SetAlpha(hitFlash, Mathf.Clamp01(flash) * hitFlashAlpha);

        bool running = gm != null && gm.Phase == RoundPhase.Running;
        UpdateDanger(gm, running, now);
        UpdateCountdown(gm, running, now);
        UpdateStreak(running, now);
    }

    private void UpdateTimeScale(GameManager gm, float now)
    {
        if (gm == null || gm.IsPaused) return;   // la pausa la maneja GameManager
        BalanceConfig b = gm.Balance;
        float target = 1f;
        if (now < hitStopUntil) target = b.HitStopTimeScale;
        else if (gm.Phase == RoundPhase.Running && gm.TimeRemaining <= b.FinishSlowMoWindow && gm.RoundStars >= gm.CurrentGoal)
            target = b.FinishSlowMoTimeScale;

        if (!Mathf.Approximately(target, 1f))
        {
            Time.timeScale = target;
            controllingTime = true;
        }
        else if (controllingTime)
        {
            Time.timeScale = 1f;
            controllingTime = false;
        }
    }

    private void UpdateDanger(GameManager gm, bool running, float now)
    {
        if (dangerVignette == null) return;
        float alpha = 0f;
        if (running)
        {
            BalanceConfig b = gm.Balance;
            if (gm.TimeRemaining <= b.DangerSeconds && gm.RoundStars < gm.CurrentGoal)
            {
                float urgency = 1f - gm.TimeRemaining / Mathf.Max(b.DangerSeconds, 0.01f);
                float beat = 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f * dangerBeats * (1f + urgency));
                alpha = Mathf.Lerp(dangerAlphaMin, dangerAlphaMax, urgency) * (0.55f + 0.45f * beat);
            }
            else if (gm.Lives == 1)
            {
                alpha = lastLifeAlpha * (0.7f + 0.3f * Mathf.Sin(now * Mathf.PI * 2f * 0.6f));
            }
        }
        SetAlpha(dangerVignette, alpha);
    }

    private void UpdateCountdown(GameManager gm, bool running, float now)
    {
        if (countdownText == null) return;
        int seconds = running ? Mathf.CeilToInt(gm.TimeRemaining) : 0;
        bool show = running && seconds >= 1 && seconds <= gm.Balance.CountdownSeconds;
        if (!show)
        {
            if (countdownText.gameObject.activeSelf) countdownText.gameObject.SetActive(false);
            return;
        }
        if (seconds != lastCountdown)
        {
            lastCountdown = seconds;
            countdownAt = now;
            countdownText.text = seconds.ToString();
            countdownText.gameObject.SetActive(true);
        }
        float t = Mathf.Clamp01(now - countdownAt);
        bool goalReached = gm.RoundStars >= gm.CurrentGoal;
        Color color = goalReached ? countdownGoalColor : countdownColor;
        color.a = Mathf.Lerp(0.95f, 0.15f, t);
        countdownText.color = color;
        countdownText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(t / 0.25f));
    }

    private void UpdateStreak(bool running, float now)
    {
        if (streakBanner != null)
        {
            float t = (now - bannerAt) / streakBannerTime;
            bool show = t >= 0f && t < 1f;
            if (streakBanner.gameObject.activeSelf != show) streakBanner.gameObject.SetActive(show);
            if (show)
            {
                float pop = t < 0.12f ? Mathf.Lerp(0.2f, 1.2f, t / 0.12f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((t - 0.12f) / 0.12f));
                streakBanner.localScale = Vector3.one * pop;
                float alpha = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
                if (streakTitle != null) streakTitle.alpha = alpha;
                if (streakBonus != null) streakBonus.alpha = alpha;
            }
        }
        if (streakChip != null)
        {
            float target = running && streak >= streakChipFrom ? 1f : 0f;
            streakChip.alpha = Mathf.MoveTowards(streakChip.alpha, target, Time.unscaledDeltaTime * 6f);
            float bump = 1f + 0.25f * Mathf.Clamp01(1f - (now - chipBumpAt) / 0.2f);
            streakChip.transform.localScale = Vector3.one * bump;
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        bool visible = alpha > 0.001f;
        if (graphic.enabled != visible) graphic.enabled = visible;
        Color c = graphic.color;
        if (Mathf.Approximately(c.a, alpha)) return;
        c.a = alpha;
        graphic.color = c;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(Image hitFlash, Image dangerVignette, TMP_Text countdownText, RectTransform streakBanner,
        TMP_Text streakTitle, TMP_Text streakBonus, CanvasGroup streakChip, TMP_Text streakChipText, ParticleSystem[] confettiCannons)
    {
        this.hitFlash = hitFlash;
        this.dangerVignette = dangerVignette;
        this.countdownText = countdownText;
        this.streakBanner = streakBanner;
        this.streakTitle = streakTitle;
        this.streakBonus = streakBonus;
        this.streakChip = streakChip;
        this.streakChipText = streakChipText;
        this.confettiCannons = confettiCannons;
    }
#endif
}
