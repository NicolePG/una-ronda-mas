using TMPro;
using UnityEngine;

/// <summary>Muestra durante la partida los datos actuales de la ronda.</summary>
public class HUD : MonoBehaviour
{
    [Header("Textos del HUD")]
    [Tooltip("Texto que muestra la ronda actual.")]
    [SerializeField] private TMP_Text roundText;
    [Tooltip("Texto que muestra las estrellas reunidas y la meta.")]
    [SerializeField] private TMP_Text starsText;
    [Tooltip("Texto que muestra el tiempo restante.")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("Texto que muestra las vidas restantes.")]
    [SerializeField] private TMP_Text livesText;
    [Tooltip("Texto de la regla que realmente está activa.")]
    [SerializeField] private TMP_Text ruleText;
    [Tooltip("Aviso visible mientras se puede acusar a Tito.")]
    [SerializeField] private TMP_Text cheatPrompt;

    private void Awake()
    {
        if (roundText == null || starsText == null || timeText == null
            || livesText == null || ruleText == null || cheatPrompt == null)
        {
            Debug.LogError("HUD: asigna los seis textos TMP en el Inspector.", this);
        }
    }

    private void OnEnable()
    {
        GameEvents.PhaseChanged += HandlePhaseChanged;
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.StarsChanged += HandleStarsChanged;
        GameEvents.LivesChanged += HandleLivesChanged;
        GameEvents.TimeChanged += HandleTimeChanged;
        GameEvents.RuleApplied += HandleRuleApplied;
        GameEvents.PauseToggled += HandlePauseToggled;
        GameEvents.CheatCalled += HandleCheatCalled;

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
        GameEvents.PauseToggled -= HandlePauseToggled;
        GameEvents.CheatCalled -= HandleCheatCalled;
    }

    private void Update()
    {
        RefreshCheatPrompt();
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        if (roundText != null) roundText.text = $"RONDA: {game.CurrentRound}";
        if (starsText != null) starsText.text = FormatStars(game.RoundStars, game.CurrentGoal);
        SetRuleText(game.Phase == RoundPhase.Running ? game.Rules.AppliedRule : null);
        RefreshCheatPrompt();
    }

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        if (roundText != null) roundText.text = $"RONDA: {round}";
        if (starsText != null) starsText.text = FormatStars(0, goal);
    }

    private void HandleStarsChanged(int stars, int goal)
    {
        if (starsText != null) starsText.text = FormatStars(stars, goal);
    }

    // Estrellas de la ronda contra la meta y, abajo, el puntaje total de la partida.
    private static string FormatStars(int stars, int goal)
    {
        GameManager game = GameManager.Instance;
        int total = game != null ? game.TotalStars : stars;
        return $"ESTRELLAS: {stars} / {goal}\n<size=60%><color=#FFFFFF>PUNTAJE: {total}</color></size>";
    }

    private void HandleLivesChanged(int lives)
    {
        if (livesText != null) livesText.text = $"VIDAS: {lives}";
    }

    private void HandleTimeChanged(float secondsLeft)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, secondsLeft));
        if (timeText != null) timeText.text = $"TIEMPO: {seconds / 60:00}:{seconds % 60:00}";
    }

    private void HandleRuleApplied(RuleData rule)
    {
        SetRuleText(rule);
    }

    private void HandlePauseToggled(bool paused)
    {
        RefreshCheatPrompt();
    }

    private void HandleCheatCalled(bool correct)
    {
        RefreshCheatPrompt();
    }

    private void RefreshFromGameManager()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        if (roundText != null) roundText.text = $"RONDA: {game.CurrentRound}";
        if (starsText != null) starsText.text = FormatStars(game.RoundStars, game.CurrentGoal);
        if (livesText != null) livesText.text = $"VIDAS: {game.Lives}";
        HandleTimeChanged(game.TimeRemaining);
        SetRuleText(game.Phase == RoundPhase.Running ? game.Rules.AppliedRule : null);
        RefreshCheatPrompt();
    }

    private void SetRuleText(RuleData rule)
    {
        if (ruleText != null) ruleText.text = rule == null ? "REGLA: —" : $"REGLA: {rule.DisplayName}";
    }

    private void RefreshCheatPrompt()
    {
        GameManager game = GameManager.Instance;
        bool canCallCheat = game != null && game.Rules != null && game.Rules.CanCallCheat;
        if (cheatPrompt != null) cheatPrompt.gameObject.SetActive(canCallCheat);
    }
}
