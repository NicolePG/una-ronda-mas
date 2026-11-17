using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Halo de neón alrededor del botón: se enciende al pasar el cursor o al seleccionarlo con el teclado.
/// El botón principal late un poco aunque nadie lo toque, para que se note dónde hacer clic. Tiempo sin escala.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class ButtonGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("Imagen del halo (borde que brilla hacia afuera).")]
    [SerializeField] private Graphic glow;
    [Tooltip("Brillo en reposo (0 = apagado). El botón principal usa un poco para latir.")]
    [SerializeField, Range(0f, 1f)] private float idleAlpha;
    [SerializeField, Range(0f, 1f)] private float hoverAlpha = 0.9f;
    [SerializeField, Min(0.1f)] private float fadeSpeed = 10f;

    private Selectable selectable;
    private bool pointerInside;
    private bool selected;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        SetAlpha(0f);
    }

    private void OnDisable()
    {
        pointerInside = false;
        selected = false;
        SetAlpha(0f);
    }

    private void Update()
    {
        if (glow == null) return;
        bool on = selectable != null && selectable.IsInteractable() && (pointerInside || selected);
        float pulse = idleAlpha * (0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f));
        float target = on ? hoverAlpha : pulse;
        SetAlpha(Mathf.MoveTowards(glow.color.a, target, Time.unscaledDeltaTime * fadeSpeed));
    }

    private void SetAlpha(float alpha)
    {
        if (glow == null) return;
        Color c = glow.color;
        c.a = alpha;
        glow.color = c;
    }

    public void OnPointerEnter(PointerEventData eventData) => pointerInside = true;
    public void OnPointerExit(PointerEventData eventData) => pointerInside = false;
    public void OnSelect(BaseEventData eventData) => selected = true;
    public void OnDeselect(BaseEventData eventData) => selected = false;

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la interfaz.</summary>
    public void EditorSetup(Graphic glow, float idleAlpha)
    {
        this.glow = glow;
        this.idleAlpha = idleAlpha;
    }
#endif
}
