using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú «Una Ronda Más»: un solo botón, «Crear todo», que arma el proyecto entero en orden.
/// Debajo quedan solo las herramientas que no crean nada (Build, Pitch y Pruebas).
/// </summary>
public static class SetupMenu
{
    private const string MainMenuPath = ProjectPaths.Scenes + "/MainMenu.unity";
    private const string ProgressTitle = "Crear todo";

    [MenuItem("Una Ronda Más/Crear todo", false, 0)]
    public static void CreateAllMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (TmpEssentialsImporter.IsImported)
        {
            PrepareAll();
            return;
        }
        // TMP Essentials se importan en segundo plano: se sigue cuando terminan.
        AssetDatabase.importPackageCompleted += ContinueAfterTmp;
        if (!TmpEssentialsImporter.Import()) AssetDatabase.importPackageCompleted -= ContinueAfterTmp;
    }

    private static void ContinueAfterTmp(string packageName)
    {
        AssetDatabase.importPackageCompleted -= ContinueAfterTmp;
        PrepareAll();
    }

    /// <summary>
    /// Materiales, datos, estrellas, UI de Tania, HUD, audio, MainMenu, Game.unity (con obstáculos, tramos y personajes),
    /// escena de prueba, informe de balance y ajustes de WebGL. Se puede repetir: no pisa los datos ni los paneles del menú.
    /// Desde la terminal (Unity cerrado): tools/preparar-todo.ps1
    /// </summary>
    public static void PrepareAll()
    {
        try
        {
            Step(0.00f, "Materiales y datos", () =>
            {
                MaterialCreator.CreateAll();
                DataAssetCreator.CreateAll();
                PrizeIconCreator.CreateAll();
                StarPrefabCreator.CreateAll();
            });
            Step(0.15f, "Pantallas de la UI", () =>
            {
                UiAssetCreator.CreateUiAssets();
                HostFacePrefabCreator.Create();
                HostBannerPrefabCreator.Create();
                HostLinesPrefabCreator.Create();
                PrizeScreenPrefabCreator.Create();
                PauseMenuPrefabCreator.Create();
                GameOverPrefabCreator.Create();
                UiLayoutFixer.FixAll();
                // Después del arreglo de Nicole: el HUD del lienzo se arma entero de nuevo.
                HudDesignCreator.Create();
            });
            Step(0.35f, "Audio", () =>
            {
                AudioImportOptimizer.Optimize();
                AudioManagerPrefabCreator.CreateOrUpdate();
            });
            Step(0.45f, "Menú principal", () =>
            {
                if (!File.Exists(MainMenuPath)) MainMenuSceneCreator.Create();
                HowToPlayMenuInstaller.Install();
                MainMenuPanelsInstaller.Install();
                UiPolishInstaller.Install();
                // Antes del estudio 3D del menú, que cambia el botón Jugar y oculta el fondo.
                MenuStyler.Apply();
            });
            Step(0.55f, "Escena de prueba", TestSceneCreator.Create);
            Step(0.65f, "Game.unity, tramos, personajes y estudio 3D del menú", GameSceneCreator.CreateAll);
            // Al final: edita en su lugar los prefabs de UI y el menú ya armados (estilo de transmisión de TV).
            Step(0.88f, "Interfaz moderna", UiModernizer.Apply);
            Step(0.95f, "Informe de balance y WebGL", () =>
            {
                BalanceReport.Generate();
                WebGLBuildTools.ApplySettings();
            });
            AssetDatabase.SaveAssets();
            if (!Application.isBatchMode) EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Debug.Log("Crear todo: listo. Dale Play en MainMenu.unity.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void Step(float progress, string label, System.Action action)
    {
        EditorUtility.DisplayProgressBar(ProgressTitle, label + "…", progress);
        action();
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
