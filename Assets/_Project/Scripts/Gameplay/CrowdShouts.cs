using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Gritos del público (Otoniel): textos 3D que salen sobre la cabeza de gente de las gradas, suben y se desvanecen.
/// Si el jugador lo hace bien: «¡WOW!», «¡BRAVO!», «¡OLÉ!» (racha de estrellas, meta, pillar a Tito, ganar la ronda);
/// «¡UUUH!» al esquivar por un pelo; «¡JAJAJA!» al caer al agua; «¡BUUU!» al chocar (más abucheos si choca seguido)
/// o con una falsa alarma. Va junto a AudienceCrowd, que pone las animaciones. Usa un pool fijo de textos.
/// </summary>
[RequireComponent(typeof(AudienceCrowd))]
[DisallowMultipleComponent]
public class CrowdShouts : MonoBehaviour
{
    private static readonly string[] Cheers = { "¡WOW!", "¡BRAVO!", "¡OLÉ!", "¡GENIAL!", "¡CRACK!", "¡ASÍ SE HACE!" };
    private static readonly string[] Gasps = { "¡UUUH!", "¡OOOH!", "¡POR POCO!" };
    private static readonly string[] Boos = { "¡BUUU!", "¡BUUUU!", "¡NOOO!", "¡AUCH!" };
    private static readonly string[] Laughs = { "¡JAJAJA!", "¡AL AGUA!", "¡SPLASH!" };
    private static readonly string[] Winners = { "¡OTRA!", "¡BRAVO!", "¡CAMPEÓN!", "¡WOW!" };

    [Header("Partes (las pone la herramienta que arma el estudio)")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Material material;

    [Header("Textos")]
    [SerializeField, Min(1)] private int poolSize = 14;
    [SerializeField, Min(1f)] private float fontSize = 10f;
    [Tooltip("Segundos que dura cada grito.")]
    [SerializeField, Min(0.2f)] private float lifetime = 1.5f;
    [SerializeField, Min(0f)] private float riseSpeed = 1.2f;
    [Tooltip("Altura sobre la persona donde aparece (m).")]
    [SerializeField, Min(0f)] private float headHeight = 2.1f;
    [Tooltip("Solo grita gente entre estas z (la que se ve desde la cámara).")]
    [SerializeField] private float minZ = -2f;
    [SerializeField] private float maxZ = 42f;
    [Tooltip("Segundos en los que un segundo choque cuenta como «choca seguido».")]
    [SerializeField, Min(0f)] private float repeatHitWindow = 8f;

    [Header("Colores")]
    [SerializeField] private Color cheerColor = new Color32(247, 177, 29, 255);
    [SerializeField] private Color gaspColor = new Color32(140, 200, 255, 255);
    [SerializeField] private Color booColor = new Color32(255, 90, 100, 255);
    [SerializeField] private Color laughColor = new Color32(183, 156, 255, 255);

    private struct Shout
    {
        public TextMeshPro Text;
        public float Age;
        public Color Color;
    }

    private struct Pending
    {
        public float At;
        public string Word;
        public Color Color;
    }

    private readonly List<Pending> pending = new List<Pending>();
    private Shout[] pool = new Shout[0];
    private int next;
    private AudienceCrowd crowd;
    private Camera cachedCamera;
    private float lastHitAt = -100f;
    private int lastStars;
    private int lastGoal;

    private void Awake()
    {
        crowd = GetComponent<AudienceCrowd>();
        var holder = new GameObject("Shouts").transform;
        holder.SetParent(transform, false);
        pool = new Shout[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("Shout");
            go.transform.SetParent(holder, false);
            var text = go.AddComponent<TextMeshPro>();
            if (font != null) text.font = font;
            if (material != null) text.fontSharedMaterial = material;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.rectTransform.sizeDelta = new Vector2(10f, 3f);
            go.SetActive(false);
            pool[i] = new Shout { Text = text, Age = -1f };
        }
    }

    private void OnEnable()
    {
        StarStreak.MilestoneReached += HandleMilestone;
        Obstacle.NearMiss += HandleNearMiss;
        GameEvents.StarsChanged += HandleStarsChanged;
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.ContestantHit += HandleHit;
        GameEvents.CheatCalled += HandleCheatCalled;
        GameEvents.RoundWon += HandleRoundWon;
        GameEvents.GameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        StarStreak.MilestoneReached -= HandleMilestone;
        Obstacle.NearMiss -= HandleNearMiss;
        GameEvents.StarsChanged -= HandleStarsChanged;
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.ContestantHit -= HandleHit;
        GameEvents.CheatCalled -= HandleCheatCalled;
        GameEvents.RoundWon -= HandleRoundWon;
        GameEvents.GameOver -= HandleGameOver;
    }

    // ---------- Qué gritan ----------

    private void HandleMilestone(int streak, int bonus) => Queue(Cheers, cheerColor, 3);
    private void HandleNearMiss(Obstacle obstacle) => Queue(Gasps, gaspColor, 1);
    private void HandleRoundWon(int round, int prizes) => Queue(Winners, cheerColor, 5);
    private void HandleGameOver(GameStats stats) => Queue(Boos, booColor, 4);

    private void HandleCheatCalled(bool correct)
    {
        if (correct) Queue(new[] { "¡PILLADO!", "¡BRAVO!", "¡WOW!" }, cheerColor, 4);
        else Queue(Boos, booColor, 3);
    }

    private void HandleHit(HitCause cause)
    {
        if (cause == HitCause.Water)
        {
            Queue(Laughs, laughColor, 3);
            return;
        }
        // Choca seguido: el público se impacienta y abuchea más.
        bool repeated = Time.time - lastHitAt < repeatHitWindow;
        lastHitAt = Time.time;
        Queue(Boos, booColor, repeated ? 5 : 2);
    }

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        lastStars = 0;
        lastGoal = goal;
    }

    private void HandleStarsChanged(int stars, int goal)
    {
        bool reachedBefore = lastGoal > 0 && lastStars >= lastGoal;
        bool crossed = !reachedBefore && goal > 0 && stars >= goal;
        lastStars = stars;
        lastGoal = goal;
        GameManager gm = GameManager.Instance;
        if (crossed && gm != null && gm.Phase == RoundPhase.Running) Queue(Cheers, cheerColor, 4);
    }

    private void Queue(string[] words, Color color, int count)
    {
        float now = Time.time;
        for (int i = 0; i < count; i++)
            pending.Add(new Pending { At = now + i * 0.12f + Random.Range(0f, 0.1f), Word = words[Random.Range(0, words.Length)], Color = color });
    }

    // ---------- Animación ----------

    private void LateUpdate()
    {
        float now = Time.time;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            if (now < pending[i].At) continue;
            Spawn(pending[i].Word, pending[i].Color);
            pending.RemoveAt(i);
        }

        if (cachedCamera == null) cachedCamera = Camera.main;
        float dt = Time.deltaTime;
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i].Age < 0f) continue;
            pool[i].Age += dt;
            float t = pool[i].Age / lifetime;
            Transform tr = pool[i].Text.transform;
            if (t >= 1f)
            {
                pool[i].Age = -1f;
                tr.gameObject.SetActive(false);
                continue;
            }
            tr.position += Vector3.up * riseSpeed * dt;
            // Golpe de entrada y se desvanece al final; siempre mirando a la cámara.
            tr.localScale = Vector3.one * (t < 0.12f ? Mathf.Lerp(0.4f, 1.15f, t / 0.12f) : Mathf.Lerp(1.15f, 1f, (t - 0.12f) * 3f));
            Color c = pool[i].Color;
            c.a = Mathf.Clamp01((1f - t) / 0.35f);
            pool[i].Text.color = c;
            if (cachedCamera != null) tr.rotation = Quaternion.LookRotation(tr.position - cachedCamera.transform.position);
        }
    }

    private void Spawn(string word, Color color)
    {
        if (pool.Length == 0 || crowd == null) return;
        Transform member = crowd.PickVisibleMember(minZ, maxZ);
        if (member == null) return;
        ref Shout shout = ref pool[next];
        next = (next + 1) % pool.Length;
        shout.Age = 0f;
        shout.Color = color;
        shout.Text.text = word;
        shout.Text.color = color;
        shout.Text.transform.position = member.position + Vector3.up * headHeight + Random.insideUnitSphere * 0.3f;
        shout.Text.gameObject.SetActive(true);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman el estudio.</summary>
    public void EditorSetup(TMP_FontAsset font, Material material)
    {
        this.font = font;
        this.material = material;
    }
#endif
}
