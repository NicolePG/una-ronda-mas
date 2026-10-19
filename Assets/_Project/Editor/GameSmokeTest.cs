using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Prueba rápida de Game.unity en Play, pensada para la terminal (tools/crear-escena-game.ps1 -Probar):
/// espera que empiece la ronda 1, provoca un choque y una caída al agua, revisa que se descuenten las vidas,
/// guarda una captura con la UI en Logs/Previews/game_play.png y cierra Unity con código 0 (bien) o 1 (falló).
/// </summary>
[InitializeOnLoad]
public static class GameSmokeTest
{
    private const string ActiveKey = "UnaRondaMas.Smoke.Active";
    private const string StartKey = "UnaRondaMas.Smoke.Start";
    private const string ErrorsKey = "UnaRondaMas.Smoke.Errors";
    private const float TimeoutSeconds = 180f;
    private const float CaptureAt = 5f;      // segundos de juego (la ronda empieza tras el anuncio de 3 s)
    private const float CaptureFrames = 0.4f;
    private const float CheckDelay = 0.3f;   // los golpes forzados se notan enseguida
    private const float SinkWait = 1f;       // se hunde 0,6 s y reaparece

    private enum Step { WaitRunning, Capture, Hit, CheckHit, Water, CheckWater, Read, Done }

    private static Step step;
    private static float stepTime;
    private static int livesBefore;
    private static RenderTexture captureTarget;
    private static readonly System.Text.StringBuilder Report = new System.Text.StringBuilder();

    static GameSmokeTest()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
    }

    [MenuItem("Una Ronda Más/Pruebas/Prueba rápida de Game.unity")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(GameSceneCreator.ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(StartKey, (float)EditorApplication.timeSinceStartup);
        SessionState.SetInt(ErrorsKey, 0);
        Subscribe();
        EditorApplication.isPlaying = true;
    }

    private static void Subscribe()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (!EditorApplication.isPlaying || message.StartsWith("SMOKE")) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            SessionState.SetInt(ErrorsKey, SessionState.GetInt(ErrorsKey, 0) + 1);
    }

    private static void Tick()
    {
        float realElapsed = (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(StartKey, 0f);
        if (realElapsed > TimeoutSeconds)
        {
            Finish(false, "se acabó el tiempo sin terminar la prueba");
            return;
        }
        if (!EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameManager gm = GameManager.Instance;
        var contestant = Object.FindObjectOfType<ContestantController>();
        if (gm == null || contestant == null) return;

        switch (step)
        {
            case Step.WaitRunning:
                if (gm.Phase != RoundPhase.Running || Time.time < CaptureAt) return;
                BeginCapture();
                Next(Step.Capture);
                break;

            case Step.Capture:
                if (Time.time < stepTime + CaptureFrames) return;
                EndCapture();
                Check(contestant.State == ContestantState.Running, $"el concursante corre ({contestant.State})");
                Check(gm.Track != null && gm.Track.Travelled > 1f, $"la pista avanza ({gm.Track?.Travelled:0.0} m)");
                Check(gm.Track != null && gm.Track.ActiveSegmentCount > 0, $"hay tramos ({gm.Track?.ActiveSegmentCount})");
                Next(Step.Hit);
                break;

            case Step.Hit:
                if (contestant.IsInvulnerable || contestant.State != ContestantState.Running) return;
                livesBefore = gm.Lives;
                Obstacle obstacle = FindRealObstacle();
                Check(obstacle != null, "hay un obstáculo en la pista");
                if (obstacle != null) obstacle.transform.position = contestant.transform.position;
                Next(Step.CheckHit);
                break;

            case Step.CheckHit:
                if (Time.time < stepTime + CheckDelay) return;
                Check(gm.Lives == livesBefore - 1, $"el choque quita una vida ({livesBefore} → {gm.Lives})");
                Next(Step.Water);
                break;

            case Step.Water:
                if (contestant.IsInvulnerable || contestant.State != ContestantState.Running) return;
                livesBefore = gm.Lives;
                var water = new GameObject("SmokeWater");
                var box = water.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(2f, 0.9f, 2f);
                water.AddComponent<WaterZone>();
                water.transform.position = contestant.transform.position + Vector3.down * 0.35f;
                Object.Destroy(water, CheckDelay); // si queda, lo vuelve a hacer caer al reaparecer
                Next(Step.CheckWater);
                break;

            case Step.CheckWater:
                if (Time.time < stepTime + SinkWait) return;
                Check(gm.Lives == livesBefore - 1, $"caer al agua quita una vida ({livesBefore} → {gm.Lives})");
                Check(Mathf.Abs(contestant.transform.position.y) < 0.01f, "reaparece arriba después de caer");
                Next(Step.Read);
                break;

            case Step.Read:
                int errors = SessionState.GetInt(ErrorsKey, 0);
                Check(errors == 0, $"sin errores en la Consola ({errors})");
                step = Step.Done;
                Finish(!Report.ToString().Contains("FALLÓ"), $"ronda {gm.CurrentRound}, fase {gm.Phase}, vidas {gm.Lives}, estrellas {gm.RoundStars}");
                break;
        }
    }

    private static void Next(Step next)
    {
        step = next;
        stepTime = Time.time;
    }

    private static Obstacle FindRealObstacle()
    {
        foreach (Obstacle o in Object.FindObjectsOfType<Obstacle>())
        {
            if (!o.IsFake && o.transform.position.z > 2f) return o;
        }
        return null;
    }

    // La UI está en Screen Space Overlay: para que salga en la captura se pasa un momento a Screen Space Camera.
    private static void BeginCapture()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        captureTarget = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
        camera.targetTexture = captureTarget;
        foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>())
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }
    }

    private static void EndCapture()
    {
        if (captureTarget == null) return;
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = captureTarget;
        var texture = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, captureTarget.width, captureTarget.height), 0, 0);
        texture.Apply();
        RenderTexture.active = old;
        Directory.CreateDirectory(PreviewRenderer.OutputFolder);
        string path = Path.Combine(PreviewRenderer.OutputFolder, "game_play.png");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        if (Camera.main != null) Camera.main.targetTexture = null;
        Report.AppendLine("Captura: " + path);
    }

    private static void Check(bool ok, string what) => Report.AppendLine((ok ? "OK     " : "FALLÓ  ") + what);

    private static void Finish(bool ok, string summary)
    {
        SessionState.EraseBool(ActiveKey);
        EditorApplication.update -= Tick;
        Debug.Log($"SMOKE {(ok ? "BIEN" : "FALLÓ")}: {summary}\n{Report}");
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
