using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HUD de la ronda (diseño del lienzo): estrellas contra la meta y puntaje total, vidas con corazones,
/// premios elegidos, tiempo y ronda, regla activa y botón «¡Trampa!» (también con la tecla T).
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

    private void Awake()
    {
        if (starsText == null || timeText == null || roundText == null || ruleChip == null || cheatButton == null)
            Debug.LogError("HUD: faltan referencias (usa el menú Una Ronda Más/UI/HUD con el diseño del lienzo).", this);
        if (heartsRow != null) heartsHome = heartsRow.anchoredPosition;
        if (prizeIconTemplate != null) prizeIconTemplate.gameObject.SetActive(false);
        if (cheatButton != null) cheatButton.onClick.AddListener(CallCheat);
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
    }

    private void Update()
    {
        RefreshCheatButton();

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
        SetStars(0, goal);
    }

    private void HandleStarsChanged(int stars, int goal)
    {
        SetStars(stars, goal);
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
        SetStars(game.RoundStars, game.CurrentGoal);
        SetLives(game.Lives);
        HandleTimeChanged(game.TimeRemaining);
        SetRule(game.Phase == RoundPhase.Running ? game.Rules.AppliedRule : null);
        RefreshCheatButton();
    }

    private void SetRound(int round)
    {
        if (roundText != null) roundText.text = $"Ronda {round}";
    }

    private void SetStars(int stars, int goal)
    {
        GameManager game = GameManager.Instance;
        if (starsText != null)
        {
            starsText.text = $"{stars} / {goal}";
            starsText.color = goal > 0 && stars >= goal ? goalReachedColor : starsColor;
        }
        if (scoreText != null) scoreText.text = $"PUNTAJE {(game != null ? game.TotalStars : stars)}";
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
        GameObject ruleChip, Image ruleIcon, TMP_Text ruleText, Button cheatButton)
    {
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
