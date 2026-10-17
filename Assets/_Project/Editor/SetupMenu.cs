using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Atajos generales del menú «Una Ronda Más».</summary>
public static class SetupMenu
{
    [MenuItem("Una Ronda Más/Preparar todo (Nicole)", false, 0)]
    public static void PrepareAllMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareAll();
    }

    /// <summary>Materiales, datos, estrellas, GameSystems, escena de prueba y ajustes de WebGL (sin cambiar de plataforma).</summary>
    public static void PrepareAll()
    {
        MaterialCreator.CreateAll();
        DataAssetCreator.CreateAll();
        StarPrefabCreator.CreateAll();
        GameSystemsPrefabCreator.CreateOrUpdate();
        TestSceneCreator.Create();
        WebGLBuildTools.Configure(false);
        AssetDatabase.SaveAssets();
        Debug.Log("Listo: materiales, Balance, premios, reglas, estrellas, GameSystems y Test_Nicole.unity.");
    }

    [MenuItem("Una Ronda Más/Pruebas/Borrar datos guardados")]
    public static void DeleteSave()
    {
        SaveManager.DeleteAll();
        Debug.Log("Datos guardados borrados (récords, partidas jugadas y volúmenes).");
    }

    [MenuItem("Una Ronda Más/Pruebas/Sumar una partida jugada")]
    public static void AddGamePlayed()
    {
        SaveManager.DebugSetGamesPlayed(SaveManager.GamesPlayed + 1);
        Debug.Log($"Partidas jugadas: {SaveManager.GamesPlayed}");
    }
}
