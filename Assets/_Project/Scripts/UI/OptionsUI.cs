using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Guarda las opciones de volumen y pantalla completa del jugador.</summary>
public class OptionsUI : MonoBehaviour
{
    [Header("Controles")]
    [Tooltip("Volumen de música entre silencio y máximo.")]
    [SerializeField] private Slider musicSlider;
    [Tooltip("Volumen de efectos entre silencio y máximo.")]
    [SerializeField] private Slider sfxSlider;
    [Tooltip("Activa o desactiva pantalla completa.")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("Texto que muestra el valor del volumen de música.")]
    [SerializeField] private TMP_Text musicValueText;
    [Tooltip("Texto que muestra el valor del volumen de efectos.")]
    [SerializeField] private TMP_Text sfxValueText;
    [Tooltip("Botón que cierra las opciones.")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (musicSlider == null || sfxSlider == null || fullscreenToggle == null)
        {
            Debug.LogError("OptionsUI: asigna los dos sliders y el toggle de pantalla completa.", this);
            enabled = false;
            return;
        }

        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;
        musicSlider.SetValueWithoutNotify(SaveManager.MusicVolume);
        sfxSlider.SetValueWithoutNotify(SaveManager.SfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        RefreshValueLabels();
    }

    private void OnEnable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    private void SetMusicVolume(float value)
    {
        SaveManager.MusicVolume = value;
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
        RefreshValueLabels();
    }

    private void SetSfxVolume(float value)
    {
        SaveManager.SfxVolume = value;
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value);
        RefreshValueLabels();
    }

    private static void SetFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void RefreshValueLabels()
    {
        if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(musicSlider.value * 100f)}%";
        if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
    }
}
