using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Cierra el panel de créditos mediante su botón Volver.</summary>
public class CreditsUI : MonoBehaviour
{
    [Tooltip("Botón para cerrar los créditos y volver al menú.")]
    [SerializeField] private Button closeButton;
    [Tooltip("Texto con integrantes, herramientas y créditos de assets.")]
    [SerializeField] private TMP_Text creditsText;

    private void Awake()
    {
        if (closeButton == null || creditsText == null)
        {
            Debug.LogError("CreditsUI: asigna el botón Volver y el texto de créditos.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (creditsText != null)
        {
            creditsText.text =
                "<b>Equipo</b>\n" +
                "Otoniel — Jugabilidad, pista y animación\n" +
                "Nicole — Sistemas y lógica\n" +
                "Tanina — Interfaz, presentador y audio\n\n" +
                "<b>Hecho con</b>\n" +
                "Unity 2022.3 LTS · C# · TextMeshPro\n\n" +
                "<b>Gráficos</b>\n" +
                "Personajes: Kenney Mini Characters (CC0) · kenney.nl\n" +
                "Fondos, íconos y estrellas: hechos por el equipo\n\n" +
                "<b>Audio</b>\n" +
                "Música: Denis-Pavlov-Music y Jalastram\n" +
                "Voces del presentador: VoiceBosch\n\n" +
                "Hecho en 48 horas para GameJamPlus 26/27.";
        }
    }

    private void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
