using UnityEditor;

/// <summary>
/// Crea el prefab del HUD. Ahora lo arma HudDesignCreator con el diseño del lienzo
/// (corazones, panel de estrellas, cartel de regla y botón «¡Trampa!»).
/// </summary>
public static class HudPrefabCreator
{
    [MenuItem("Una Ronda Más/UI/Crear prefab HUD")]
    public static void Create() => HudDesignCreator.Create();
}
