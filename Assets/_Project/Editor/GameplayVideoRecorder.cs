using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Graba un video de gameplay para el pitch (MP4 1280 × 720, 30 fps, sin sonido) con las 10 rondas:
/// menú, anuncio, carrera con piloto automático que esquiva, arco de meta, premios, apuesta, «¡Trampa!»,
/// Eliminado y «¿Una ronda más?». Sale en Builds/Video (no se sube a git).
/// Desde la terminal (Unity cerrado): Unity.exe -batchmode -projectPath . -executeMethod GameplayVideoRecorder.Record
/// </summary>
[InitializeOnLoad]
public static class GameplayVideoRecorder
{
    private const string ActiveKey = "URM.Video.Active";
    private const string StartKey = "URM.Video.Start";
    private const string OutputFolder = "Builds/Video";
    private const string OutputFile = "UnaRondaMas_gameplay.mp4";
    private const int Width = 1280;
    private const int Height = 720;
    private const int Fps = 30;
    private const float TimeoutSeconds = 900f;
    private const int LastRound = 10;

    private enum Step { Menu, LoadGame, Announcement, Running, JumpToFinish, Finish, Prize, PrizeChosen, GameOver, BackToMenu, Done }

    private static Step step;
    private static int stepFrames;
    private static int round;
    private static bool recording;
    private static MediaEncoder encoder;
    private static Texture2D frame;
    private static RenderTexture target;
    private static Camera fallbackCamera;
    private static int totalFrames;
    private static float laneCooldown;

    private static readonly MethodInfo ChangeLane = typeof(ContestantController).GetMethod("ChangeLane", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo TryJump = typeof(ContestantController).GetMethod("TryJump", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo TrySlide = typeof(ContestantController).GetMethod("TrySlide", BindingFlags.Instance | BindingFlags.NonPublic);

    static GameplayVideoRecorder()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Una Ronda Más/Pitch/Grabar video de gameplay")]
    public static void Record()
    {
        EditorSceneManager.OpenScene(ProjectPaths.Scenes + "/MainMenu.unity", OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(StartKey, (float)EditorApplication.timeSinceStartup);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if ((float)EditorApplication.timeSinceStartup - SessionState.GetFloat(StartKey, 0f) > TimeoutSeconds)
        {
            Finish("se acabó el tiempo");
            return;
        }
        if (!EditorApplication.isPlaying) return;

        if (encoder == null) BeginEncoding();
        GameManager gm = GameManager.Instance;
        stepFrames++;

        switch (step)
        {
            case Step.Menu:
                recording = true;
                if (stepFrames < 3 * Fps) break;
                SceneManager.LoadScene("Game");
                Next(Step.LoadGame, false);
                break;

            case Step.LoadGame:
                if (gm == null || gm.Phase != RoundPhase.Announcement) break;
                // Respaldo para que el video no termine antes de tiempo si el piloto falla.
                for (int i = 0; i < 4; i++)
                {
                    gm.Modifiers.AddPrize(PrizeType.Shield, false);
                    gm.Modifiers.AddPrize(PrizeType.Float, false);
                }
                round = 1;
                Next(Step.Announcement, true);
                break;

            case Step.Announcement:
                if (stepFrames < 2 * Fps) break;
                bool acceptBet = round == 3 || round == 7;
                gm.ConfirmAnnouncement(acceptBet);
                Next(Step.Running, true);
                break;

            case Step.Running:
                Autopilot(gm);
                if (round == 4 && stepFrames == 3 * Fps) gm.Rules.CallCheat();
                int runFrames = round == 1 ? 7 * Fps : 5 * Fps;
                if (gm.Phase == RoundPhase.GameOver) { Next(Step.GameOver, true); break; }
                if (stepFrames < runFrames) break;
                if (round == 1 || round == 5)
                {
                    // Corte: salta al final de la ronda para mostrar el arco y los premios.
                    gm.DebugAddStars(Mathf.Max(0, gm.CurrentGoal - gm.RoundStars));
                    gm.DebugSetTimeRemaining(3f);
                    Next(Step.JumpToFinish, false);
                }
                else if (round >= LastRound)
                {
                    gm.LoseLife(gm.Lives);
                    Next(Step.GameOver, true);
                }
                else
                {
                    StartRound(gm, round + 1);
                }
                break;

            case Step.JumpToFinish:
                if (stepFrames < 2) break;
                Next(Step.Finish, true);
                break;

            case Step.Finish:
                Autopilot(gm);
                if (gm.Phase != RoundPhase.PrizeSelection) break;
                Next(Step.Prize, true);
                break;

            case Step.Prize:
                if (stepFrames < (int)(2.5f * Fps)) break;
                gm.ChoosePrize(1);
                Next(Step.PrizeChosen, true);
                break;

            case Step.PrizeChosen:
                if (stepFrames < (int)(1.2f * Fps)) break;
                StartRound(gm, round + 1);
                break;

            case Step.GameOver:
                if (stepFrames < 4 * Fps) break;
                gm.GoToMainMenu();
                Next(Step.BackToMenu, false);
                break;

            case Step.BackToMenu:
                if (stepFrames < 10) break;
                recording = true;
                if (stepFrames < 10 + 3 * Fps) break;
                Next(Step.Done, false);
                Finish("video completo");
                return;
        }

        if (recording) CaptureFrame();
    }

    private static void StartRound(GameManager gm, int next)
    {
        round = next;
        gm.DebugStartRound(next);
        Next(Step.Announcement, true);
    }

    private static void Next(Step next, bool record)
    {
        step = next;
        stepFrames = 0;
        recording = record;
    }

    // ---------- Piloto automático: esquiva lo que viene en su carril ----------

    private static void Autopilot(GameManager gm)
    {
        var contestant = Object.FindObjectOfType<ContestantController>();
        if (contestant == null || gm == null || gm.Phase != RoundPhase.Running) return;
        float speed = Mathf.Max(gm.CurrentSpeed, 1f);
        float look = speed * 0.9f + 2f;
        float lane = gm.Balance.LaneWidth;
        laneCooldown -= Time.deltaTime;

        float[] nearest = { float.MaxValue, float.MaxValue, float.MaxValue };
        string[] kind = new string[3];
        foreach (Collider c in Object.FindObjectsOfType<Collider>())
        {
            if (!c.enabled || !c.isTrigger) continue;
            Obstacle obstacle = c.GetComponentInParent<Obstacle>();
            bool water = c.GetComponentInParent<WaterZone>() != null;
            if (!water && (obstacle == null || obstacle.IsFake)) continue;
            Bounds b = c.bounds;
            if (b.max.z < 0.2f || b.min.z > look) continue;
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * lane;
                if (b.max.x < x - lane * 0.35f || b.min.x > x + lane * 0.35f) continue;
                if (b.min.z >= nearest[i]) continue;
                nearest[i] = Mathf.Max(b.min.z, 0f);
                kind[i] = water ? "low" : b.max.y < 1f ? "low" : b.min.y > 0.75f ? "high" : "block";
            }
        }

        int current = contestant.Lane + 1;
        if (nearest[current] == float.MaxValue) return;

        // Mejor carril: el vecino libre o con el peligro más lejos.
        int best = current;
        foreach (int candidate in new[] { current - 1, current + 1 })
        {
            if (candidate < 0 || candidate > 2) continue;
            if (nearest[candidate] > nearest[best] + 3f) best = candidate;
        }
        bool mirror = gm.Rules.IsApplied(RuleType.Mirror);
        if (best != current && laneCooldown <= 0f)
        {
            int direction = best > current ? 1 : -1;
            ChangeLane.Invoke(contestant, new object[] { mirror ? direction : direction });
            laneCooldown = 0.25f;
            return;
        }

        float timeToHit = nearest[current] / speed;
        if (kind[current] == "low" && timeToHit < 0.3f) TryJump.Invoke(contestant, new object[] { gm });
        else if (kind[current] == "high" && timeToHit < 0.25f) TrySlide.Invoke(contestant, new object[] { gm });
    }

    // ---------- Grabación ----------

    private static void BeginEncoding()
    {
        Directory.CreateDirectory(OutputFolder);
        Time.captureFramerate = Fps;
        var attributes = new VideoTrackAttributes
        {
            frameRate = new MediaRational(Fps),
            width = Width,
            height = Height,
            includeAlpha = false,
            bitRateMode = VideoBitrateMode.High,
        };
        encoder = new MediaEncoder(Path.Combine(OutputFolder, OutputFile), attributes);
        // Que no se liberen al cargar otra escena (si no, el video repite el último cuadro del menú).
        frame = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontUnloadUnusedAsset };
        target = new RenderTexture(Width, Height, 24) { antiAliasing = 4, hideFlags = HideFlags.DontUnloadUnusedAsset };
    }

    private static void CaptureFrame()
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
        if (camera == null)
        {
            if (fallbackCamera == null)
            {
                fallbackCamera = new GameObject("VideoCamera").AddComponent<Camera>();
                fallbackCamera.clearFlags = CameraClearFlags.SolidColor;
                fallbackCamera.backgroundColor = new Color(0.14f, 0.1f, 0.25f);
            }
            camera = fallbackCamera;
        }
        foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>())
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }
        Canvas.ForceUpdateCanvases();

        // Igual que las capturas de prueba: textura nueva por cuadro.
        var rt = new RenderTexture(Width, Height, 24) { antiAliasing = 4 };
        RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        shot.Apply();
        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        encoder.AddFrame(shot);
        if (totalFrames % 300 == 0)
            Debug.Log($"VIDEO cuadro {totalFrames}: cámara {camera.name} en {camera.gameObject.scene.name}, paso {step}, pixel {shot.GetPixel(Width / 2, Height / 3)}");
        Object.DestroyImmediate(shot);
        rt.Release();
        Object.DestroyImmediate(rt);
        totalFrames++;
    }

    private static void Finish(string summary)
    {
        SessionState.EraseBool(ActiveKey);
        EditorApplication.update -= Tick;
        Time.captureFramerate = 0;
        encoder?.Dispose();
        encoder = null;
        if (frame != null) Object.DestroyImmediate(frame);
        if (target != null) target.Release();
        string path = Path.Combine(OutputFolder, OutputFile);
        Debug.Log($"VIDEO {summary}: {path}, {totalFrames} cuadros ({totalFrames / (float)Fps:0.0} s).");
        if (Application.isBatchMode) EditorApplication.Exit(0);
        else EditorApplication.isPlaying = false;
    }
}
