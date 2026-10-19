using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Muestra el anuncio de Tito y permite responder a su apuesta.</summary>
public class HostBanner : MonoBehaviour
{
    [Header("Panel del anuncio")]
    [Tooltip("Panel que se muestra mientras Tito anuncia la ronda.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Título de la ronda y la meta.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("Regla que Tito anuncia; no revela si es mentira.")]
    [SerializeField] private TMP_Text ruleText;
    [Tooltip("Instrucción de la apuesta o del inicio de ronda.")]
    [SerializeField] private TMP_Text promptText;
    [Header("Apuesta")]
    [Tooltip("Botón que acepta la apuesta de Tito.")]
    [SerializeField] private Button acceptBetButton;
    [Tooltip("Texto del botón para aceptar la apuesta.")]
    [SerializeField] private TMP_Text acceptBetText;
    [Tooltip("Botón que rechaza la apuesta o inicia una ronda sin apuesta.")]
    [SerializeField] private Button declineBetButton;
    [Tooltip("Texto del botón para rechazar la apuesta.")]
    [SerializeField] private TMP_Text declineBetText;

    private void Awake()
    {
        if (panel == null || titleText == null || ruleText == null || promptText == null
            || acceptBetButton == null || acceptBetText == null
            || declineBetButton == null || declineBetText == null)
        {
            Debug.LogError("HostBanner: asigna el panel, textos y botones en el Inspector.", this);
        }
    }

    private void OnEnable()
    {
        GameEvents.PhaseChanged += HandlePhaseChanged;
        GameEvents.RuleAnnounced += HandleRuleAnnounced;

        if (acceptBetButton != null) acceptBetButton.onClick.AddListener(AcceptBet);
        if (declineBetButton != null) declineBetButton.onClick.AddListener(DeclineBet);

        RefreshFromGameManager();
    }

    private void OnDisable()
    {
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        GameEvents.RuleAnnounced -= HandleRuleAnnounced;

        if (acceptBetButton != null) acceptBetButton.onClick.RemoveListener(AcceptBet);
        if (declineBetButton != null) declineBetButton.onClick.RemoveListener(DeclineBet);
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Announcement)
        {
            RefreshFromGameManager();
            return;
        }

        if (panel != null) panel.SetActive(false);
    }

    private void HandleRuleAnnounced(RuleData rule, bool isLie)
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.Phase != RoundPhase.Announcement) return;

        UpdateRule(rule);
    }

    private void RefreshFromGameManager()
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.Phase != RoundPhase.Announcement)
        {
            if (panel != null) panel.SetActive(false);
            return;
        }

        if (titleText != null)
            titleText.text = $"¡RONDA {game.CurrentRound}!  META: {game.BaseGoal}";

        UpdateRule(game.Rules.AnnouncedRule);

        bool betOffered = game.IsBetOffered;
        if (promptText != null)
            promptText.text = betOffered
                ? $"¿Aceptas la apuesta? La meta sube a {game.BetGoal} estrellas."
                : "¡Prepárate para correr!";

        if (acceptBetButton != null) acceptBetButton.gameObject.SetActive(betOffered);
        if (declineBetButton != null) declineBetButton.gameObject.SetActive(betOffered);
        if (acceptBetText != null) acceptBetText.text = "¡Acepto! (1)";
        if (declineBetText != null) declineBetText.text = betOffered ? "No, gracias (2)" : "¡A correr!";
        if (panel != null) panel.SetActive(true);
    }

    private void Update()
    {
        // Teclas 1 y 2 para la apuesta (además de los botones).
        if (panel == null || !panel.activeSelf) return;
        GameManager game = GameManager.Instance;
        if (game == null || game.Phase != RoundPhase.Announcement || !game.IsBetOffered) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) AcceptBet();
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) DeclineBet();
    }

    private void UpdateRule(RuleData rule)
    {
        if (ruleText == null) return;
        ruleText.text = rule == null
            ? "Sin regla especial esta ronda."
            : $"{rule.DisplayName}\n{rule.Description}";
    }

    private void AcceptBet()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("No se puede aceptar la apuesta: no hay GameManager activo.", this);
            return;
        }
        game.AcceptBet();
    }

    private void DeclineBet()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("No se puede rechazar la apuesta: no hay GameManager activo.", this);
            return;
        }
        game.DeclineBet();
    }
}
