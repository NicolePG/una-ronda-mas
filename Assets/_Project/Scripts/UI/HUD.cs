using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HUD de la ronda (diseño del lienzo): estrellas contra la meta y puntaje total, vidas con corazones,
/// premios elegidos, tiempo y ronda, regla activa y botón «¡Trampa!» (también con la tecla T).
/// Animado: los números suben de a poco, la barra de meta se llena y brilla al llegar, y cada estrella
/// que toma el concursante vuela desde la pista hasta el contador.
/// Lo arma el menú «Una Ronda Más/UI/HUD con el diseño del lienzo».
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Estrellas")]
    [Tooltip("Estrellas de la ronda contra la meta, por ejemplo «12 / 20».")]
    [SerializeField] private TMP_Text starsText;
    [Tooltip("Puntaje total de la partida.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("Grupo que rebota al juntar estrellas.")]
    [SerializeField] private RectTransform starsBump;
    [SerializeField] private Color starsColor = Color.white;
    [Tooltip("Color de las estrellas cuando ya alcanzó la meta.")]
    [SerializeField] private Color goalReachedColor = new Color32(247, 177, 29, 255);

    [Header("Barra de meta")]
    [Tooltip("Relleno de la barra (Image en modo Filled horizontal).")]
    [SerializeField] private Image goalFill;
    [SerializeField] private Color goalFillColor = new Color32(251, 217, 122, 255);
    [Tooltip("Color de la barra cuando ya llegó a la meta (late).")]
    [SerializeField] private Color goalDoneColor = new Color32(247, 177, 29, 255);

    [Header("Estrellas que vuelan")]
    [Tooltip("Ícono de estrella del HUD: destino de las estrellas que vuelan (y su sprite).")]
    [SerializeField] private Image starTarget;
    [SerializeField, Min(0)] private int flyingStars = 10;
    [Tooltip("Segundos que tarda una estrella en llegar al contador.")]
    [SerializeField, Min(0.05f)] private float flyTime = 0.45f;
    [Tooltip("Tamaño de la estrella que vuela (px).")]
    [SerializeField, Min(8f)] private float flySize = 40f;

    [Header("Contadores")]
    [Tooltip("Unidades por segundo mínimas al subir los números.")]
    [SerializeField, Min(1f)] private float countSpeed = 10f;

    [Header("Vidas y premios")]
    [Tooltip("Corazones en orden; se ven tantos como vidas.")]
    [SerializeField] private Image[] hearts = new Image[0];
    [Tooltip("Fila de corazones (tiembla al perder una vida).")]
    [SerializeField] private RectTransform heartsRow;
    [Tooltip("Fila donde aparecen los íconos de los premios elegidos.")]
    [SerializeField] private RectTransform prizeRow;
    [Tooltip("Plantilla desactivada de un ícono de premio.")]
    [SerializeField] private Image prizeIconTemplate;
    [Tooltip("Cuántos íconos de premios caben en la fila.")]
    [SerializeField, Min(1)] private int maxPrizeIcons = 9;

    [Header("Tiempo y ronda")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text roundText;
    [Tooltip("Segundos finales en que el tiempo se pone en rojo.")]
    [SerializeField, Min(0f)] private float warningSeconds = 10f;
    [SerializeField] private Color timeColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0.45f);

    [Header("Regla")]
    [Tooltip("Cartel de la regla activa (se oculta si no hay regla).")]
    [SerializeField] private GameObject ruleChip;
    [SerializeField] private Image ruleIcon;
    [SerializeField] private TMP_Text ruleText;

    [Header("¡Trampa!")]
    [Tooltip("Botón visible mientras se puede acusar a Tito.")]
    [SerializeField] private Button cheatButton;

    [Header("Animación")]
    [Tooltip("Tamaño máximo del rebote al juntar estrellas.")]
    [SerializeField, Min(1f)] private float bumpScale = 1.18f;
    [Tooltip("Segundos del rebote.")]
    [SerializeField, Min(0.01f)] private float bumpTime = 0.18f;
    [Tooltip("Cuánto tiemblan los corazones al perder una vida (px).")]
    [SerializeField, Min(0f)] private float heartShake = 6f;
    [Tooltip("Segundos que tiemblan.")]
    [SerializeField, Min(0.01f)] private float heartShakeTime = 0.35f;

    private float bumpUntil = -1f;
    private float shakeUntil = -1f;
    private int shownLives = -1;
    private int prizeIcons;
    private Vector2 heartsHome;

    private int targetStars;
    private int targetGoal;
    private int targetScore;
    private float shownStars;
    private float shownScore;
    private float shownFill;

    private struct Flyer
    {
        public RectTransform Rect;
        public Vector2 From;
        public float Age;
    }

    private Flyer[] flyers = new Flyer[0];
    private RectTransform canvasRect;
    private ContestantController contestant;

    private void Awake()
    {
        if (starsText == null || timeText == null || roundText == null || ruleChip == null || cheatButton == null)
            Debug.LogError("HUD: faltan referencias (usa el menú Una Ronda Más/UI/HUD con el diseño del lienzo).", this);
        if (heartsRow != null) heartsHome = heartsRow.anchoredPosition;
        if (prizeIconTemplate != null) prizeIconTemplate.gameObject.SetActive(false);
        if (cheatButton != null) cheatButton.onClick.AddListener(CallCheat);
        canvasRect = transform as RectTransform;
        CreateFlyers();
    }

    private void OnDestroy()
    {
        if (cheatButton != null) cheatButton.onClick.RemoveListener(CallCheat);
    }

    private void OnEnable()
    {
        GameEvents.PhaseChanged += HandlePhaseChanged;
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.StarsChanged += HandleStarsChanged;
        GameEvents.LivesChanged += HandleLivesChanged;
        GameEvents.TimeChanged += HandleTimeChanged;
        GameEvents.RuleApplied += HandleRuleApplied;
        GameEvents.PrizeChosen += HandlePrizeChosen;

        RefreshFromGameManager();
    }

    private void OnDisable()
    {
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.StarsChanged -= HandleStarsChanged;
        GameEvents.LivesChanged -= HandleLivesChanged;
        GameEvents.TimeChanged -= HandleTimeChanged;
        GameEvents.RuleApplied -= HandleRuleApplied;
        GameEvents.PrizeChosen -= HandlePrizeChosen;
        SetContestant(null);
    }

    private void Update()
    {
        RefreshCheatButton();
        if (contestant != ContestantController.Current) SetContestant(ContestantController.Current);
        AnimateCounters();
        AnimateFlyers();

        // Animaciones con tiempo real: siguen aunque la partida esté en pausa.
        if (starsBump != null)
        {
            float left = Mathf.Clamp01((bumpUntil - Time.unscaledTime) / bumpTime);
            starsBump.localScale = Vector3.one * Mathf.Lerp(1f, bumpScale, left);
        }
        if (heartsRow != null)
        {
            float left = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / heartShakeTime);
            heartsRow.anchoredPosition = heartsHome + new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * heartShake * left, 0f);
        }
    }

    private void HandlePhaseChanged(RoundPhase phase) => RefreshFromGameManager();

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        SetRound(round);
        SetStars(0, goal, true);
    }

    private void HandleStarsChanged(int stars, int goal)
    {
        SetStars(stars, goal, false);
        bumpUntil = Time.unscaledTime + bumpTime;
    }

    private void HandleLivesChanged(int lives) => SetLives(lives);

    private void HandleTimeChanged(float secondsLeft)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, secondsLeft));
        if (timeText == null) return;
        timeText.text = $"{seconds / 60}:{seconds % 60:00}";
        GameManager game = GameManager.Instance;
        bool warning = game != null && game.Phase == RoundPhase.Running && secondsLeft <= warningSeconds;
        timeText.color = warning ? warningColor : timeColor;
    }

    private void HandleRuleApplied(RuleData rule) => SetRule(rule);

    private void HandlePrizeChosen(PrizeData prize)
    {
        if (prize == null || prize.Icon == null || prizeRow == null || prizeIconTemplate == null) return;
        if (prizeIcons >= maxPrizeIcons) return;
        Image icon = Instantiate(prizeIconTemplate, prizeRow);
        icon.sprite = prize.Icon;
        icon.gameObject.SetActive(true);
        prizeIcons++;
    }

    private void RefreshFromGameManager()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        SetRound(game.CurrentRound);
        SetStars(game.RoundStars, game.CurrentGoal, true);
        SetLives(game.Lives);
        HandleTimeChanged(game.TimeRemaining);
        SetRule(game.Phase == RoundPhase.Running ? game.Rules.AppliedRule : null);
        RefreshCheatButton();
    }

    private void SetRound(int round)
    {
        if (roundText != null) roundText.text = $"Ronda {round}";
    }

    // Guarda el valor real; AnimateCounters lo muestra subiendo de a poco (instant = sin animación).
    private void SetStars(int stars, int goal, bool instant)
    {
        GameManager game = GameManager.Instance;
        targetStars = stars;
        targetGoal = goal;
        targetScore = game != null ? game.TotalStars : stars;
        if (instant || stars < shownStars)
        {
            shownStars = stars;
            shownScore = targetScore;
            shownFill = goal > 0 ? Mathf.Clamp01((float)stars / goal) : 0f;
        }
        WriteCounters();
    }

    private void AnimateCounters()
    {
        float dt = Time.unscaledDeltaTime;
        shownStars = Approach(shownStars, targetStars, dt);
        shownScore = Approach(shownScore, targetScore, dt);
        float fill = targetGoal > 0 ? Mathf.Clamp01(shownStars / targetGoal) : 0f;
        shownFill = Mathf.Lerp(shownFill, fill, 1f - Mathf.Exp(-12f * dt));
        WriteCounters();
    }

    private float Approach(float shown, float target, float dt)
    {
        float step = Mathf.Max(countSpeed, Mathf.Abs(target - shown) * 8f) * dt;
        return Mathf.MoveTowards(shown, target, step);
    }

    private void WriteCounters()
    {
        bool reached = targetGoal > 0 && targetStars >= targetGoal;
        if (starsText != null)
        {
            starsText.SetText("{0} / {1}", Mathf.RoundToInt(shownStars), targetGoal);
            starsText.color = reached ? goalReachedColor : starsColor;
        }
        if (scoreText != null) scoreText.SetText("PUNTAJE {0}", Mathf.RoundToInt(shownScore));
        if (goalFill != null)
        {
            goalFill.fillAmount = shownFill;
            // Al llegar a la meta la barra late en dorado.
            float pulse = reached ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f) : 1f;
            Color c = reached ? goalDoneColor : goalFillColor;
            goalFill.color = new Color(c.r * pulse + (1f - pulse), c.g * pulse + (1f - pulse), c.b * pulse + (1f - pulse), 1f);
        }
    }

    // ---------- Estrellas que vuelan al contador ----------

    private void CreateFlyers()
    {
        if (starTarget == null || flyingStars <= 0) return;
        flyers = new Flyer[flyingStars];
        for (int i = 0; i < flyers.Length; i++)
        {
            var go = new GameObject("FlyingStar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.sizeDelta = Vector2.one * flySize;
            var image = go.GetComponent<Image>();
            image.sprite = starTarget.sprite;
            image.raycastTarget = false;
            go.SetActive(false);
            flyers[i] = new Flyer { Rect = rect, Age = -1f };
        }
    }

    private void SetContestant(ContestantController value)
    {
        if (contestant != null) contestant.StarCollected -= HandleStarCollected;
        contestant = value;
        if (contestant != null) contestant.StarCollected += HandleStarCollected;
    }

    private void HandleStarCollected(Star star)
    {
        Camera cam = Camera.main;
        if (cam == null || canvasRect == null || flyers.Length == 0) return;
        Vector3 screen = cam.WorldToScreenPoint(star.transform.position);
        if (screen.z <= 0f) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 from)) return;
        for (int i = 0; i < flyers.Length; i++)
        {
            if (flyers[i].Age >= 0f) continue;
            flyers[i].Age = 0f;
            flyers[i].From = from;
            flyers[i].Rect.anchoredPosition = from;
            flyers[i].Rect.gameObject.SetActive(true);
            return;
        }
    }

    private void AnimateFlyers()
    {
        if (flyers.Length == 0 || starTarget == null) return;
        Vector2 screenTarget = RectTransformUtility.WorldToScreenPoint(null, starTarget.rectTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenTarget, null, out Vector2 to);
        for (int i = 0; i < flyers.Length; i++)
        {
            if (flyers[i].Age < 0f) continue;
            flyers[i].Age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(flyers[i].Age / flyTime);
            float eased = t * t * (3f - 2f * t);
            // Curva hacia arriba (control a mitad de camino y más alto).
            Vector2 from = flyers[i].From;
            Vector2 control = Vector2.Lerp(from, to, 0.35f) + Vector2.up * 160f;
            Vector2 a = Vector2.Lerp(from, control, eased), b = Vector2.Lerp(control, to, eased);
            RectTransform rect = flyers[i].Rect;
            rect.anchoredPosition = Vector2.Lerp(a, b, eased);
            rect.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.7f, t);
            rect.localRotation = Quaternion.Euler(0f, 0f, t * 360f);
            if (t < 1f) continue;
            flyers[i].Age = -1f;
            rect.gameObject.SetActive(false);
            bumpUntil = Time.unscaledTime + bumpTime;
        }
    }

    private void SetLives(int lives)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null) hearts[i].gameObject.SetActive(i < lives);
        }
        if (shownLives >= 0 && lives < shownLives) shakeUntil = Time.unscaledTime + heartShakeTime;
        shownLives = lives;
    }

    private void SetRule(RuleData rule)
    {
        if (ruleChip != null) ruleChip.SetActive(rule != null);
        if (rule == null) return;
        if (ruleText != null) ruleText.text = rule.DisplayName;
        if (ruleIcon != null)
        {
            ruleIcon.sprite = rule.Icon;
            ruleIcon.enabled = rule.Icon != null;
        }
    }

    private void RefreshCheatButton()
    {
        GameManager game = GameManager.Instance;
        bool canCallCheat = game != null && game.Rules != null && game.Rules.CanCallCheat;
        if (cheatButton != null && cheatButton.gameObject.activeSelf != canCallCheat)
            cheatButton.gameObject.SetActive(canCallCheat);
    }

    private void CallCheat()
    {
        // Sin dejarlo seleccionado: si no, Espacio (saltar) volvería a «apretar» el botón.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        GameManager game = GameManager.Instance;
        if (game != null && game.Rules != null) game.Rules.CallCheat();
    }

#if UNITY_EDITOR
    /// <summary>Solo para la herramienta de Editor que arma el HUD.</summary>
    public void EditorSetup(TMP_Text starsText, TMP_Text scoreText, RectTransform starsBump, Image[] hearts,
        RectTransform heartsRow, RectTransform prizeRow, Image prizeIconTemplate, TMP_Text timeText, TMP_Text roundText,
        GameObject ruleChip, Image ruleIcon, TMP_Text ruleText, Button cheatButton, Image goalFill, Image starTarget)
    {
        this.goalFill = goalFill;
        this.starTarget = starTarget;
        this.starsText = starsText;
        this.scoreText = scoreText;
        this.starsBump = starsBump;
        this.hearts = hearts;
        this.heartsRow = heartsRow;
        this.prizeRow = prizeRow;
        this.prizeIconTemplate = prizeIconTemplate;
        this.timeText = timeText;
        this.roundText = roundText;
        this.ruleChip = ruleChip;
        this.ruleIcon = ruleIcon;
        this.ruleText = ruleText;
        this.cheatButton = cheatButton;
    }
#endif
}
