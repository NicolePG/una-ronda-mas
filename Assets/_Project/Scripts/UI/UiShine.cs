using UnityEngine;

/// <summary>
/// Destello que cruza el botón cada tanto (como el brillo de un cartel de TV). Va en una imagen hija dentro de una
/// máscara (RectMask2D) del tamaño del botón. Tiempo sin escala.
/// </summary>
[DisallowMultipleComponent]
public class UiShine : MonoBehaviour
{
    [Tooltip("Imagen del destello (franja diagonal) que se mueve de izquierda a derecha.")]
    [SerializeField] private RectTransform shine;
    [Tooltip("Segundos entre destellos.")]
    [SerializeField, Min(0.2f)] private float interval = 2.8f;
    [Tooltip("Segundos que tarda en cruzar.")]
    [SerializeField, Min(0.05f)] private float sweepTime = 0.55f;

    private RectTransform area;
    private float nextAt;

    private void Awake() => area = transform as RectTransform;

    private void OnEnable()
    {
        // Cada botón con su propio ritmo para que no brillen todos juntos.
        nextAt = Time.unscaledTime + Random.Range(0.3f, interval);
        if (shine != null) shine.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (shine == null || area == null) return;
        float t = (Time.unscaledTime - nextAt) / sweepTime;
        bool moving = t >= 0f && t <= 1f;
        if (shine.gameObject.activeSelf != moving) shine.gameObject.SetActive(moving);
        if (t > 1f) nextAt = Time.unscaledTime + interval;
        if (!moving) return;
        float width = area.rect.width + shine.rect.width;
        shine.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.5f, width * 0.5f, t * t * (3f - 2f * t)), 0f);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la interfaz.</summary>
    public void EditorSetup(RectTransform shine) => this.shine = shine;
#endif
}
