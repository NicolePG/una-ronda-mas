using TMPro;
using UnityEngine;

/// <summary>
/// Textos 3D que saltan sobre el concursante: «+1» / «+3» al sumar estrellas, «¡Uff!» al esquivar a último
/// momento y «¡Auch!» / «¡Al agua!» al perder una vida. Usa una plantilla (hija desactivada) y un pool fijo.
/// </summary>
[DisallowMultipleComponent]
public class FloatingTexts : MonoBehaviour
{
    [Header("Partes")]
    [Tooltip("Texto de plantilla (desactivado); se copia para el pool.")]
    [SerializeField] private TextMeshPro template;
    [SerializeField, Min(1)] private int poolSize = 12;

    [Header("Colores")]
    [SerializeField] private Color starColor = new Color(0.98f, 0.85f, 0.48f);
    [SerializeField] private Color bigStarColor = new Color(0.97f, 0.69f, 0.11f);
    [SerializeField] private Color dodgeColor = new Color(0.55f, 0.85f, 1f);
    [SerializeField] private Color hitColor = new Color(0.82f, 0.23f, 0.23f);

    [Header("Movimiento")]
    [Tooltip("Altura sobre el concursante donde aparece (m).")]
    [SerializeField] private float spawnHeight = 2.1f;
    [Tooltip("Metros por segundo que sube.")]
    [SerializeField] private float riseSpeed = 1.6f;
    [Tooltip("Segundos que dura en pantalla.")]
    [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
    [Tooltip("Desde cuántas estrellas de una vez se usa el texto grande y dorado.")]
    [SerializeField, Min(1)] private int bigStarAmount = 3;

    private struct Entry
    {
        public TextMeshPro Text;
        public float Age;
        public float Scale;
        public Color Color;
    }

    private Entry[] pool = new Entry[0];
    private int next;
    private int lastStars;
    private int lastStarEntry = -1;
    private int lastStarAmount;
    private float lastStarTime = -10f;
    private Camera cachedCamera;

    private void Awake()
    {
        if (template == null) return;
        template.gameObject.SetActive(false);
        pool = new Entry[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            TextMeshPro text = Instantiate(template, transform);
            text.name = "FloatingText";
            pool[i] = new Entry { Text = text, Age = -1f };
        }
    }

    private void OnEnable()
    {
        GameEvents.StarsChanged += HandleStarsChanged;
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.ContestantHit += HandleHit;
        Obstacle.NearMiss += HandleNearMiss;
    }

    private void OnDisable()
    {
        GameEvents.StarsChanged -= HandleStarsChanged;
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.ContestantHit -= HandleHit;
        Obstacle.NearMiss -= HandleNearMiss;
    }

    private void HandleRoundStarted(int round, int goal, float speed) => lastStars = 0;

    private void HandleStarsChanged(int stars, int goal)
    {
        int gained = stars - lastStars;
        lastStars = stars;
        GameManager gm = GameManager.Instance;
        if (gained <= 0 || gm == null || gm.Phase != RoundPhase.Running) return;
        // Lo que llega casi junto (estrella + premio de racha o estrellas dobles) se suma en un solo texto.
        if (lastStarEntry >= 0 && Time.unscaledTime - lastStarTime < 0.12f && pool[lastStarEntry].Age >= 0f)
        {
            lastStarAmount += gained;
            ref Entry entry = ref pool[lastStarEntry];
            bool bigger = lastStarAmount >= bigStarAmount;
            entry.Text.text = $"+{lastStarAmount}";
            entry.Color = bigger ? bigStarColor : starColor;
            entry.Scale = bigger ? 1.35f : 1f;
            return;
        }
        bool big = gained >= bigStarAmount;
        lastStarEntry = next;
        lastStarAmount = gained;
        lastStarTime = Time.unscaledTime;
        Spawn($"+{gained}", Vector3.zero, big ? bigStarColor : starColor, big ? 1.35f : 1f);
    }

    private void HandleNearMiss(Obstacle obstacle)
    {
        // Un poco hacia el lado del obstáculo esquivado.
        ContestantController contestant = ContestantController.Current;
        float side = contestant != null ? Mathf.Sign(obstacle.transform.position.x - contestant.transform.position.x) : 0f;
        Spawn("¡Uff!", new Vector3(side * 0.7f, 0.3f, 0f), dodgeColor, 1.2f);
    }

    private void HandleHit(HitCause cause) =>
        Spawn(cause == HitCause.Water ? "¡Al agua!" : "¡Auch!", new Vector3(0f, 0.2f, 0f), hitColor, 1.25f);

    /// <summary>Muestra un texto sobre el concursante (offset en metros).</summary>
    public void Spawn(string value, Vector3 offset, Color color, float scale)
    {
        if (pool.Length == 0) return;
        ContestantController contestant = ContestantController.Current;
        Vector3 origin = contestant != null ? contestant.transform.position : Vector3.zero;
        float height = spawnHeight * (contestant != null ? contestant.transform.localScale.y : 1f);

        ref Entry entry = ref pool[next];
        next = (next + 1) % pool.Length;
        entry.Age = 0f;
        entry.Scale = scale;
        entry.Color = color;
        entry.Text.text = value;
        entry.Text.color = color;
        entry.Text.transform.position = origin + Vector3.up * height + offset + Vector3.right * Random.Range(-0.25f, 0.25f);
        entry.Text.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < pool.Length; i++)
        {
            ref Entry entry = ref pool[i];
            if (entry.Age < 0f) continue;
            entry.Age += dt;
            float t = entry.Age / lifetime;
            if (t >= 1f)
            {
                entry.Age = -1f;
                entry.Text.gameObject.SetActive(false);
                continue;
            }
            Transform tr = entry.Text.transform;
            tr.position += Vector3.up * riseSpeed * dt * (1f - t);
            if (cachedCamera != null) tr.rotation = cachedCamera.transform.rotation;
            // Aparece con rebote y se desvanece al final.
            float pop = t < 0.15f ? Mathf.Lerp(0.3f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));
            tr.localScale = Vector3.one * entry.Scale * pop;
            Color c = entry.Color;
            c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            entry.Text.color = c;
        }
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(TextMeshPro template) => this.template = template;
#endif
}
