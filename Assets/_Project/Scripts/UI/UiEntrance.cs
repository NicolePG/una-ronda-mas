using UnityEngine;

/// <summary>
/// Entrada animada al activarse (estilo gráfica de TV): aparece con fundido, sube un poco desde abajo y, si se
/// pide, crece desde más chico con un pequeño rebote. Con delay se arman entradas en cascada (tarjetas, botones).
/// Tiempo sin escala: funciona en pausa. En botones no se anima la escala (la maneja ButtonHoverEffect).
/// </summary>
[DisallowMultipleComponent]
public class UiEntrance : MonoBehaviour
{
    [Tooltip("Segundos de espera antes de entrar (para entradas en cascada).")]
    [SerializeField, Min(0f)] private float delay;
    [Tooltip("Segundos que dura la entrada.")]
    [SerializeField, Min(0.05f)] private float duration = 0.35f;
    [Tooltip("Desde dónde llega, relativo a su posición (px).")]
    [SerializeField] private Vector2 fromOffset = new Vector2(0f, -40f);
    [Tooltip("Animar la escala (no en botones).")]
    [SerializeField] private bool animateScale = true;
    [Tooltip("Escala desde la que crece.")]
    [SerializeField, Min(0f)] private float fromScale = 0.88f;

    private RectTransform rect;
    private CanvasGroup group;
    private Vector2 homePosition;
    private Vector3 homeScale;
    private bool captured;
    private float startedAt;
    private bool playing;

    private void Awake() => Capture();

    private void Capture()
    {
        if (captured) return;
        captured = true;
        rect = transform as RectTransform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (rect != null) homePosition = rect.anchoredPosition;
        homeScale = transform.localScale;
    }

    private void OnEnable()
    {
        Capture();
        startedAt = Time.unscaledTime + delay;
        playing = true;
        Apply(0f);
    }

    private void OnDisable()
    {
        if (!playing) return;
        playing = false;
        Apply(1f);
    }

    private void Update()
    {
        if (!playing) return;
        float t = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
        Apply(t);
        if (t >= 1f) playing = false;
    }

    private void Apply(float t)
    {
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        if (group != null) group.alpha = Mathf.Clamp01(t * 1.6f);
        if (rect != null) rect.anchoredPosition = homePosition + fromOffset * (1f - eased);
        if (!animateScale) return;
        // Rebote suave al final (sobrepasa un poco y vuelve).
        float back = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f);
        transform.localScale = homeScale * Mathf.LerpUnclamped(fromScale, 1f, back);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la interfaz.</summary>
    public void EditorSetup(float delay, Vector2 fromOffset, bool animateScale, float duration = 0.35f)
    {
        this.delay = delay;
        this.fromOffset = fromOffset;
        this.animateScale = animateScale;
        this.duration = duration;
    }
#endif
}
