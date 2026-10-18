using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Muestra los controles de pausa y delega las acciones al GameManager.</summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panel de pausa")]
    [Tooltip("Panel que se muestra mientras la partida está pausada.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Botón para volver a la partida.")]
    [SerializeField] private Button resumeButton;
    [Tooltip("Botón para reiniciar desde la ronda 1.")]
    [SerializeField] private Button restartButton;
    [Tooltip("Botón para volver al menú principal.")]
    [SerializeField] private Button menuButton;

    private void Awake()
    {
        if (panel == null || resumeButton == null || restartButton == null || menuButton == null)
        {
            Debug.LogError("PauseMenu: asigna el panel y los botones Continuar, Reiniciar y Menú.", this);
        }
    }

    private void OnEnable()
    {
        GameEvents.PauseToggled += HandlePauseToggled;
        GameEvents.PhaseChanged += HandlePhaseChanged;

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMainMenu);

        GameManager game = GameManager.Instance;
        SetPanelVisible(game != null && game.IsPaused);
    }

    private void OnDisable()
    {
        GameEvents.PauseToggled -= HandlePauseToggled;
        GameEvents.PhaseChanged -= HandlePhaseChanged;

        if (resumeButton != null) resumeButton.onClick.RemoveListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
        if (menuButton != null) menuButton.onClick.RemoveListener(GoToMainMenu);
    }

    private void HandlePauseToggled(bool paused)
    {
        SetPanelVisible(paused);
        if (paused) SelectResumeButton();
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase != RoundPhase.Running) SetPanelVisible(false);
    }

    private void ResumeGame()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("No se puede reanudar: no hay GameManager activo.", this);
            return;
        }
        game.SetPaused(false);
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

    private void SetPanelVisible(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    private void SelectResumeButton()
    {
        if (resumeButton == null) return;
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null) eventSystem.SetSelectedGameObject(resumeButton.gameObject);
    }
}
