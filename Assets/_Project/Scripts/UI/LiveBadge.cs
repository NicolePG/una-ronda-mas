using UnityEngine;
using UnityEngine.UI;

/// <summary>Cartel «● EN VIVO» de la transmisión: el punto rojo titila como en la tele. Tiempo sin escala.</summary>
[DisallowMultipleComponent]
public class LiveBadge : MonoBehaviour
{
    [Tooltip("Punto rojo que titila.")]
    [SerializeField] private Graphic dot;
    [Tooltip("Titileos por segundo.")]
    [SerializeField, Min(0.1f)] private float blinkRate = 1.2f;

    private void Update()
    {
        if (dot == null) return;
        Color c = dot.color;
        c.a = Mathf.Sin(Time.unscaledTime * blinkRate * Mathf.PI * 2f) > -0.3f ? 1f : 0.25f;
        dot.color = c;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la interfaz.</summary>
    public void EditorSetup(Graphic dot) => this.dot = dot;
#endif
}
