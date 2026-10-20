using TMPro;
using UnityEngine;

/// <summary>
/// Carteles cortos con barrido tipo TV (franjas diagonales que cruzan la pantalla y un título que «golpea»):
/// «¡YA!» al empezar la carrera, «¡META!» al llegar a las estrellas pedidas y el nombre de cada evento
/// sorpresa, con un contador del tiempo que le queda al evento. Tiempo sin escala.
/// </summary>
[DisallowMultipleComponent]
public class RoundAnnouncer : MonoBehaviour
{
    [Header("Partes")]
    [Tooltip("Franjas diagonales que barren la pantalla.")]
    [SerializeField] private RectTransform[] stripes = new RectTransform[0];
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitle;
    [Tooltip("Cartel chico con el evento activo y sus segundos.")]
    [SerializeField] private CanvasGroup eventChip;
    [SerializeField] private TMP_Text eventChipText;

    [Header("Colores")]
    [SerializeField] private Color goColor = new Color32(247, 177, 29, 255);
    [SerializeField] private Color goalColor = new Color32(247, 177, 29, 255);
    [SerializeField] private Color rainColor = new Color32(251, 217, 122, 255);
    [SerializeField] private Color doubleColor = new Color32(247, 177, 29, 255);
    [SerializeField] private Color blackoutColor = new Color32(183, 156, 255, 255);

    [Header("Animación")]
    [Tooltip("Segundos que tardan las franjas en cruzar.")]
    [SerializeField, Min(0.1f)] private float sweepTime = 0.5f;
    [Tooltip("Ancho de pantalla de referencia que deben cruzar las franjas (px).")]
    [SerializeField, Min(100f)] private float sweepDistance = 1100f;

    private float shownAt = -10f;
    private float showFor;
    private float eventEndsAt = -1f;
    private string eventName;
    private int lastStars;
    private int lastGoal;

    private void OnEnable()
    {
        GameEvents.PhaseChanged += HandlePhaseChanged;
        GameEvents.StarsChanged += HandleStarsChanged;
        GameEvents.RoundStarted += HandleRoundStarted;
        SurpriseEvents.SurpriseStarted += HandleSurpriseStarted;
        SurpriseEvents.SurpriseEnded += HandleSurpriseEnded;
    }

    private void OnDisable()
    {
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        GameEvents.StarsChanged -= HandleStarsChanged;
        GameEvents.RoundStarted -= HandleRoundStarted;
        SurpriseEvents.SurpriseStarted -= HandleSurpriseStarted;
        SurpriseEvents.SurpriseEnded -= HandleSurpriseEnded;
    }

    private void Start()
    {
        SetVisible(false);
        if (eventChip != null) eventChip.alpha = 0f;
    }

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        lastStars = 0;
        lastGoal = goal;
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Running) Show("¡YA!", string.Empty, goColor, 0.8f);
        else eventEndsAt = -1f;
    }

    private void HandleStarsChanged(int stars, int goal)
    {
        GameManager gm = GameManager.Instance;
        bool crossed = lastStars < goal && stars >= goal && goal > 0 && lastGoal == goal;
        lastStars = stars;
        lastGoal = goal;
        if (crossed && gm != null && gm.Phase == RoundPhase.Running) Show("¡META!", "Lo que sumes ahora es puntaje", goalColor, 1.2f);
    }

    private void HandleSurpriseStarted(SurpriseType type, float duration)
    {
        switch (type)
        {
            case SurpriseType.StarRain:
                Show("¡LLUVIA DE ESTRELLAS!", "¡Atrápalas antes de que pasen!", rainColor, 1.5f);
                eventName = "LLUVIA DE ESTRELLAS";
                break;
            case SurpriseType.DoubleStars:
                Show("¡ESTRELLAS DOBLES!", "Cada estrella vale el doble", doubleColor, 1.5f);
                eventName = "ESTRELLAS DOBLES";
                break;
            default:
                Show("¡APAGÓN!", "Tito apagó las luces...", blackoutColor, 1.3f);
                eventName = "APAGÓN";
                break;
        }
        eventEndsAt = Time.unscaledTime + duration;
    }

    private void HandleSurpriseEnded(SurpriseType type) => eventEndsAt = -1f;

    /// <summary>Muestra un cartel con barrido.</summary>
    public void Show(string text, string sub, Color color, float duration)
    {
        if (title == null) return;
        title.text = text;
        title.color = color;
        if (subtitle != null) subtitle.text = sub;
        shownAt = Time.unscaledTime;
        showFor = duration;
        SetVisible(true);
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        float t = now - shownAt;

        // Franjas: cruzan de izquierda a derecha, una detrás de otra.
        for (int i = 0; i < stripes.Length; i++)
        {
            if (stripes[i] == null) continue;
            float s = Mathf.Clamp01((t - i * 0.06f) / sweepTime);
            bool moving = s > 0f && s < 1f;
            if (stripes[i].gameObject.activeSelf != moving) stripes[i].gameObject.SetActive(moving);
            if (moving) stripes[i].anchoredPosition = new Vector2(Mathf.Lerp(-sweepDistance, sweepDistance, EaseInOut(s)), 0f);
        }

        if (title != null && title.gameObject.activeSelf)
        {
            if (t >= showFor) SetVisible(false);
            else
            {
                // Golpe de entrada, quieto, y se va creciendo un poco y desvaneciendo.
                float scale = t < 0.15f ? Mathf.Lerp(2.2f, 1f, t / 0.15f) : 1f + Mathf.Max(0f, t - showFor + 0.25f) * 0.6f;
                float alpha = t > showFor - 0.25f ? (showFor - t) / 0.25f : 1f;
                title.rectTransform.localScale = Vector3.one * scale;
                title.alpha = alpha;
                if (subtitle != null) subtitle.alpha = alpha * Mathf.Clamp01((t - 0.15f) / 0.15f);
            }
        }

        if (eventChip != null)
        {
            bool on = eventEndsAt > now;
            eventChip.alpha = Mathf.MoveTowards(eventChip.alpha, on ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            if (on && eventChipText != null) eventChipText.text = $"{eventName}  {Mathf.CeilToInt(eventEndsAt - now)}s";
        }
    }

    private void SetVisible(bool value)
    {
        if (title != null) title.gameObject.SetActive(value);
        if (subtitle != null) subtitle.gameObject.SetActive(value);
    }

    private static float EaseInOut(float t) => t * t * (3f - 2f * t);

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(RectTransform[] stripes, TMP_Text title, TMP_Text subtitle, CanvasGroup eventChip, TMP_Text eventChipText)
    {
        this.stripes = stripes;
        this.title = title;
        this.subtitle = subtitle;
        this.eventChip = eventChip;
        this.eventChipText = eventChipText;
    }
#endif
}
