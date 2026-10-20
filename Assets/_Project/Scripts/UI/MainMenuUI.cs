using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Muestra el récord y permite entrar al juego desde el menú principal.</summary>
public class MainMenuUI : MonoBehaviour
{
    private const string GameSceneName = "Game";

    [Header("Elementos del menú")]
    [Tooltip("Texto del botón principal.")]
    [SerializeField] private TMP_Text playButtonText;
    [Tooltip("Texto con los récords guardados.")]
    [SerializeField] private TMP_Text recordText;
    [Tooltip("Mensajes de estado, por ejemplo si la escena de juego aún no está disponible.")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Botón principal para empezar o repetir una partida.")]
    [SerializeField] private Button playButton;
    [Tooltip("Botón que abre las instrucciones desde el menú.")]
    [SerializeField] private Button howToPlayButton;
    [Tooltip("Panel de instrucciones, inicialmente oculto.")]
    [SerializeField] private GameObject howToPlayPanel;
    [Tooltip("Botón para cerrar las instrucciones.")]
    [SerializeField] private Button closeHowToPlayButton;
    [Tooltip("Botón que abre las opciones.")]
    [SerializeField] private Button optionsButton;
    [Tooltip("Panel de opciones, inicialmente oculto.")]
    [SerializeField] private GameObject optionsPanel;
    [Tooltip("Botón que abre los créditos.")]
    [SerializeField] private Button creditsButton;
    [Tooltip("Panel de créditos, inicialmente oculto.")]
    [SerializeField] private GameObject creditsPanel;
    [Tooltip("Capa negra con CanvasGroup para las transiciones de entrada y salida.")]
    [SerializeField] private CanvasGroup transitionOverlay;
    [Tooltip("Duración de las transiciones de pantalla, en segundos.")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.35f;

    private bool isTransitioning;

    private void Awake()
    {
        if (playButtonText == null || recordText == null || statusText == null || playButton == null)
        {
            Debug.LogError("MainMenuUI: asigna el texto del botón, récord, estado y botón principal.", this);
        }
    }

    private void OnEnable()
    {
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (howToPlayButton != null) howToPlayButton.onClick.AddListener(OpenHowToPlay);
        if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.onClick.RemoveListener(PlayGame);
        if (howToPlayButton != null) howToPlayButton.onClick.RemoveListener(OpenHowToPlay);
        if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.RemoveListener(CloseHowToPlay);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(OpenOptions);
        if (creditsButton != null) creditsButton.onClick.RemoveListener(OpenCredits);
    }

    private void Start()
    {
        if (playButtonText != null)
            playButtonText.text = SaveManager.GamesPlayed > 0 ? "¿Una ronda más?" : "Jugar";

        if (recordText != null)
            recordText.text = $"RÉCORD DE RONDA: {SaveManager.BestRound}\nRÉCORD DE ESTRELLAS: {SaveManager.BestStars}";

        if (statusText != null) statusText.text = string.Empty;
        // Enter juega directo (botón principal seleccionado).
        if (playButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        if (transitionOverlay != null)
        {
            transitionOverlay.alpha = 1f;
            transitionOverlay.blocksRaycasts = true;
            StartCoroutine(FadeOverlay(0f, false));
        }
    }

    private void PlayGame()
    {
        if (isTransitioning) return;
        if (!Application.CanStreamedLevelBeLoaded(GameSceneName))
        {
            const string message = "La escena del juego aún no está disponible en Build Settings.";
            if (statusText != null) statusText.text = message;
            Debug.LogError(message, this);
            return;
        }

        StartCoroutine(LoadGame());
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (howToPlayPanel != null && howToPlayPanel.activeSelf) CloseHowToPlay();
        else if (optionsPanel != null && optionsPanel.activeSelf) optionsPanel.SetActive(false);
        else if (creditsPanel != null && creditsPanel.activeSelf) creditsPanel.SetActive(false);
    }

    private void OpenHowToPlay()
    {
        if (isTransitioning) return;
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    private void CloseHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    private void OpenOptions()
    {
        if (isTransitioning) return;
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    private void OpenCredits()
    {
        if (isTransitioning) return;
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    private IEnumerator LoadGame()
    {
        isTransitioning = true;
        if (playButton != null) playButton.interactable = false;
        if (transitionOverlay != null) yield return FadeOverlay(1f, true);
        SceneManager.LoadScene(GameSceneName);
    }

    private IEnumerator FadeOverlay(float targetAlpha, bool blockDuringFade)
    {
        if (transitionOverlay == null) yield break;

        transitionOverlay.blocksRaycasts = blockDuringFade || targetAlpha > 0f;
        float startAlpha = transitionOverlay.alpha;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            transitionOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        transitionOverlay.alpha = targetAlpha;
        transitionOverlay.blocksRaycasts = targetAlpha > 0f;
    }
}
