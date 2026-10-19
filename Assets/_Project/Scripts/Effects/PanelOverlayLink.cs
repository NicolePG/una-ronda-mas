using UnityEngine;

/// <summary>
/// Prende y apaga la capa oscura («Overlay») de una pantalla junto con su panel.
/// En los prefabs de UI el Overlay es hermano del panel y los scripts solo esconden el panel, así que la capa
/// quedaba siempre encima del juego. Va en la raíz del Canvas (siempre activa); lo agrega el menú de Game.unity.
/// Se puede quitar cuando el Overlay quede dentro del panel en los prefabs.
/// </summary>
[DisallowMultipleComponent]
public class PanelOverlayLink : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject overlay;

    private void LateUpdate()
    {
        if (panel == null || overlay == null) return;
        bool visible = panel.activeInHierarchy;
        if (overlay.activeSelf != visible) overlay.SetActive(visible);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(GameObject panel, GameObject overlay)
    {
        this.panel = panel;
        this.overlay = overlay;
    }
#endif
}
