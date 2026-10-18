using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Amplía suavemente los botones al pasar el cursor o recibir foco.</summary>
[RequireComponent(typeof(Selectable))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [Tooltip("Escala objetivo mientras el botón está enfocado.")]
    [SerializeField, Min(1f)] private float hoverScale = 1.06f;
    [Tooltip("Rapidez de interpolación al entrar y salir del botón.")]
    [SerializeField, Min(0.01f)] private float transitionSpeed = 12f;

    private Selectable selectable;
    private Button button;
    private Vector3 normalScale;
    private bool pointerInside;
    private bool isSelected;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        button = GetComponent<Button>();
        normalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (button != null) button.onClick.AddListener(PlayClick);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
        pointerInside = false;
        isSelected = false;
        if (transform != null) transform.localScale = normalScale;
    }

    private void Update()
    {
        bool isHovered = selectable != null && selectable.IsInteractable() && (pointerInside || isSelected);
        Vector3 targetScale = normalScale * (isHovered ? hoverScale : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale,
            Time.unscaledDeltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (selectable != null && selectable.IsInteractable() && AudioManager.Instance != null)
            AudioManager.Instance.PlayMenuHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }

    private void PlayClick()
    {
        if (selectable != null && selectable.IsInteractable() && AudioManager.Instance != null)
            AudioManager.Instance.PlayMenuClick();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }
}
