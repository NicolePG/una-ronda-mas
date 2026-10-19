using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Ajustes rápidos sobre Game.unity sin regenerar la escena.</summary>
public static class SceneTweaks
{
    private const string GameScenePath = ProjectPaths.Scenes + "/Game.unity";
    private static readonly Vector3 ScreenPosition = new Vector3(0f, 11.5f, 110f);
    private const float ScreenScale = 1.4f;

    [MenuItem("Una Ronda Más/Escena/Agrandar pantalla de Tito")]
    public static void EnlargeTitoScreen()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "TitoScreen") continue;
            root.transform.position = ScreenPosition;
            root.transform.localScale = Vector3.one * ScreenScale;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Pantalla de Tito agrandada (×1,4).");
            return;
        }
        Debug.LogError("No encontré TitoScreen en Game.unity.");
    }
}
