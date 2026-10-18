using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Muestra el resultado de la partida y permite reiniciar o volver al menú.</summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Panel que se muestra al terminar la partida.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Resumen de ronda alcanzada, estrellas y récords.")]
    [SerializeField] private TMP_Text statsText;
    [Header("Botones")]
    [Tooltip("Botón principal «¿Una ronda más?».")]
    [SerializeField] private Button restartButton;
    [Tooltip("Botón para volver al menú principal.")]
    [SerializeField] private Button menuButton;

    private void Awake()
    {
        if (panel == null || statsText == null || restartButton == null || menuButton == null)
        {
            Debug.LogError("GameOverScreen: asigna el panel, el texto de resultados y ambos botones.", this);
        }
    }

    private void OnEnable()
    {
        GameEvents.GameOver += HandleGameOver;
        GameEvents.PhaseChanged += HandlePhaseChanged;

        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMainMenu);

        GameManager game = GameManager.Instance;
        if (game != null && game.Phase == RoundPhase.GameOver && game.LastStats != null)
            HandleGameOver(game.LastStats);
        else if (panel != null)
            panel.SetActive(false);
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.PhaseChanged -= HandlePhaseChanged;

        if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
        if (menuButton != null) menuButton.onClick.RemoveListener(GoToMainMenu);
    }

    private void HandleGameOver(GameStats stats)
    {
        if (stats == null)
        {
            Debug.LogError("GameOverScreen recibió un resultado vacío.");
            return;
        }

        if (statsText != null)
        {
            string newRoundRecord = stats.IsNewBestRound ? "\n¡Nuevo récord de ronda!" : string.Empty;
            string newStarsRecord = stats.IsNewBestStars ? "\n¡Nuevo récord de estrellas!" : string.Empty;
            statsText.text =
                $"Llegaste a la ronda {stats.RoundReached}\n" +
                $"Estrellas de la partida: {stats.TotalStars}\n\n" +
                $"Récord de ronda: {stats.BestRound}\n" +
                $"Récord de estrellas: {stats.BestStars}" +
                $"{newRoundRecord}{newStarsRecord}";
        }

        if (panel != null) panel.SetActive(true);
        SelectRestartButton();
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase != RoundPhase.GameOver && panel != null) panel.SetActive(false);
    }

    private void RestartGame()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("No se puede reiniciar: no hay GameManager activo.", this);
            return;
        }
        game.RestartGame();
    }

    private void GoToMainMenu()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("No se puede volver al menú: no hay GameManager activo.", this);
            return;
        }
        game.GoToMainMenu();
    }

    private void SelectRestartButton()
    {
        if (restartButton == null) return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem",
                    typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }
        }
        eventSystem.SetSelectedGameObject(restartButton.gameObject);
    }
}
