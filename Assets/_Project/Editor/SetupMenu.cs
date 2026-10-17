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

    /// <summary>
    /// TMP Essentials, materiales, datos, estrellas, GameSystems, escena de prueba, informe de balance
    /// y ajustes de WebGL (sin cambiar de plataforma). Se puede repetir: no pisa lo que ya existe.
    /// Desde la terminal (Unity cerrado): tools/preparar-todo.ps1
    /// </summary>
    public static void PrepareAll()
    {
        TmpEssentialsImporter.Import();
        MaterialCreator.CreateAll();
        DataAssetCreator.CreateAll();
        StarPrefabCreator.CreateAll();
        GameSystemsPrefabCreator.CreateOrUpdate();
        TestSceneCreator.Create();
        BalanceReport.Generate();
        WebGLBuildTools.Configure(false);
        AssetDatabase.SaveAssets();
        Debug.Log("Listo: TMP Essentials, materiales, Balance, premios, reglas, estrellas, GameSystems, Test_Nicole.unity y docs/BALANCE.md.");
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
