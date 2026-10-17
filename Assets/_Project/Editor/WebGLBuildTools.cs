using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Configuración y build WebGL para itch.io (Fase 0, paso 6 y Fase 4).
/// Desde la terminal, con Unity CERRADO:
/// Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod WebGLBuildTools.BuildFromCommandLine -logFile Logs/build.log
/// </summary>
public static class WebGLBuildTools
{
    public const string BuildFolder = "Builds/WebGL";
    public const string ProductName = "¡Una Ronda Más!";
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    private static readonly string[] SceneOrder =
    {
        ProjectPaths.Scenes + "/MainMenu.unity",
        ProjectPaths.Scenes + "/Game.unity",
    };

    public static bool IsWebGLInstalled => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);

    [MenuItem("Una Ronda Más/Build/Configurar WebGL")]
    public static void ConfigureMenu()
    {
        if (Configure(true)) Debug.Log("WebGL configurado: Gzip + Decompression Fallback, 1280 × 720.");
    }

    /// <summary>Ajusta Player Settings y Build Settings. Devuelve false si falta el módulo WebGL.</summary>
    public static bool Configure(bool switchPlatform)
    {
        PlayerSettings.productName = ProductName;
        PlayerSettings.defaultWebScreenWidth = ScreenWidth;
        PlayerSettings.defaultWebScreenHeight = ScreenHeight;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        UpdateBuildScenes();

        if (!IsWebGLInstalled)
        {
            const string message = "Falta el módulo «WebGL Build Support». Instálalo en Unity Hub → Installs → 2022.3.62f2 → ⚙ → Add modules.";
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("WebGL", message, "OK");
            Debug.LogError(message);
            return false;
        }
        if (switchPlatform && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        return true;
    }

    /// <summary>Pone MainMenu y Game (si existen) en Build Settings, en ese orden.</summary>
    public static void UpdateBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        foreach (string path in SceneOrder)
        {
            if (File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        if (scenes.Count == 0)
        {
            Debug.LogWarning("Todavía no existen Scenes/MainMenu.unity ni Scenes/Game.unity: no se cambian las Build Settings.");
            return;
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [MenuItem("Una Ronda Más/Build/Hacer build WebGL")]
    public static void BuildMenu()
    {
        BuildReport report = Build();
        if (report != null && report.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(BuildFolder);
    }

    public static BuildReport Build()
    {
        if (!Configure(true)) return null;
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }
        if (scenes.Count == 0)
        {
            Debug.LogError("No hay escenas en Build Settings.");
            return null;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = BuildFolder,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"Build WebGL: {summary.result}, {summary.totalSize / (1024f * 1024f):0.0} MB, " +
                  $"{summary.totalErrors} errores, en {BuildFolder}.");
        return report;
    }

    public static void BuildFromCommandLine()
    {
        BuildReport report = Build();
        bool ok = report != null && report.summary.result == BuildResult.Succeeded;
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
