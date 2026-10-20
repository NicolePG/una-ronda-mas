using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Arma Scenes/Game.unity (Otoniel): estudio con público (StudioSetCreator), agua, pantalla gigante con la cara de
/// Tito (HostFace, de Tania), Tito en persona en su tarima, el concursante, GameSystems (Nicole), la emoción y el
/// post-procesado (GameFeelCreator) y las pantallas de UI (Tania).
/// Antes crea obstáculos, tramos y personajes. Se puede repetir: vuelve a generar la escena.
/// Desde la terminal (Unity cerrado): tools/crear-escena-game.ps1
/// </summary>
public static class GameSceneCreator
{
    public const string ScenePath = ProjectPaths.Scenes + "/Game.unity";

    private const string StudioColor = "#24193F";
    private static readonly Vector3 CameraPosition = new Vector3(0f, 4f, -7f);
    private const float CameraPitch = 15f;
    private const float FogStart = 50f;
    private const float FogEnd = 115f;

    // Pantalla gigante, fija en el mundo al fondo de la pista.
    private static readonly Vector3 ScreenPosition = new Vector3(0f, 11.5f, 110f);
    private const float ScreenScale = 1.4f;   // más grande para que Tito se vea desde la pista
    private static readonly Vector3 ScreenSize = new Vector3(16f, 10f, 0.6f);
    private const float HostFaceScale = 1.7f;

    // Tito en persona, al costado derecho de la pista.
    private static readonly Vector3 PodiumPosition = new Vector3(7.5f, 0f, 6f);
    private const float PodiumHeight = 0.9f;
    private const float PodiumTop = 0.35f;
    private static readonly Vector3 HostLooksAt = new Vector3(0f, 0f, -5f);

    private const float WaterHeight = -0.6f;
    private const float PoolFloorHeight = -2.5f;
    private const float PoolWidth = 20f;   // entre las paredes de la piscina (StudioSetCreator)

    private static readonly string[] UiPrefabs =
        { "HUD_Canvas", "HostBanner_Canvas", "HostLines_Canvas", "PrizeScreen_Canvas", "PauseMenu_Canvas", "GameOver_Canvas" };

    [MenuItem("Una Ronda Más/Escena/Crear todo lo de Otoniel (Game.unity)", false, 1)]
    public static void CreateAllMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CreateAll();
    }

    /// <summary>Obstáculos, efectos, tramos, personajes, GameSystems y Game.unity, en ese orden.</summary>
    public static void CreateAll()
    {
        MaterialCreator.CreateAll();
        DataAssetCreator.CreateAll();
        StarPrefabCreator.CreateAll();
        ObstaclePrefabCreator.CreateAll();
        var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        SegmentCreator.CreateAll(balance);
        CharacterPrefabCreator.CreateAll(AssetDatabase.LoadAssetAtPath<ParticleSystem>(ObstaclePrefabCreator.SplashPath));
        GameObject systems = GameSystemsPrefabCreator.CreateOrUpdate();
        CreateScene(balance, systems);
        // El menú usa tramos y confeti: al regenerar los prefabs sus referencias cambian, así que se rearma también.
        MainMenuStudioCreator.Create();
        WebGLBuildTools.UpdateBuildScenes();
        AssetDatabase.SaveAssets();

        string segments = SegmentValidator.ValidateAll(out int problems);
        if (problems > 0) Debug.LogWarning("Validar tramos: " + segments);
        else Debug.Log("Validar tramos: " + segments);
        Debug.Log($"Listo: {ScenePath} con el concursante, Tito, el estudio, los tramos y la UI. Abre la escena y dale Play (o empieza desde MainMenu).");
    }

    /// <summary>Para la terminal: arma todo y guarda una captura en Logs/Previews/game_preview.png.</summary>
    public static void CreateAllAndCapture()
    {
        CreateAll();
        CapturePreview();
    }

    private static void CreateScene(BalanceConfig balance, GameObject systemsPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ColorUtility.TryParseHtmlString(StudioColor, out Color studio);
        SetupEnvironment(studio);

        // Soporte que sigue al concursante; la cámara (con la sacudida) va adentro en (0, 0, 0).
        var rig = new GameObject("CameraRig");
        rig.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraPitch, 0f, 0f));
        var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraGo.transform.SetParent(rig.transform, false);
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = studio;
        camera.farClipPlane = 200f;
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<CameraShake>();

        Light mainLight = CreateMainLight();
        Light[] studioLights = CreateStudioLights();
        CreatePool(balance);
        CreateScreen();
        CreateHost();
        ParticleSystem[] cannons = StudioSetCreator.Build();

        PrefabUtility.InstantiatePrefab(systemsPrefab, scene);

        var contestantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabCreator.ContestantPath);
        Light contestantSpot = null;
        ContestantController contestantController = null;
        if (contestantPrefab != null)
        {
            var contestant = (GameObject)PrefabUtility.InstantiatePrefab(contestantPrefab, scene);
            contestant.transform.position = Vector3.zero;
            Transform spot = contestant.transform.Find("BlackoutSpot");
            if (spot != null) contestantSpot = spot.GetComponent<Light>();
            rig.AddComponent<CameraFollow>().EditorSetup(contestant.transform, camera);
            contestantController = contestant.GetComponent<ContestantController>();
        }
        else Debug.LogError("Falta Contestant.prefab.");
        GameFeelCreator.Build(camera, rig.transform, contestantController, cannons);

        var effects = new GameObject("RuleEffects").AddComponent<RuleEffects>();
        effects.EditorSetup(mainLight, studioLights, contestantSpot);
        new GameObject("SurpriseEvents").AddComponent<SurpriseEvents>().EditorSetup(
            AssetDatabase.LoadAssetAtPath<GameObject>(StarPrefabCreator.NormalPath),
            AssetDatabase.LoadAssetAtPath<GameObject>(StarPrefabCreator.GoldenPath), effects);

        foreach (string name in UiPrefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ProjectPaths.Root}/Prefabs/UI/{name}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"No está el prefab de UI {name} (lo crea Tania).");
                continue;
            }
            LinkOverlayToPanel((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene));
        }
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        ProjectPaths.EnsureFolder(ProjectPaths.Scenes);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    // El «Overlay» oscuro de cada pantalla debe verse solo cuando su panel está visible (ver PanelOverlayLink).
    private static void LinkOverlayToPanel(GameObject canvasRoot)
    {
        Transform overlay = canvasRoot.transform.Find("Overlay");
        if (overlay == null) return;
        GameObject panel = null;
        foreach (MonoBehaviour behaviour in canvasRoot.GetComponents<MonoBehaviour>())
        {
            SerializedProperty property = new SerializedObject(behaviour).FindProperty("panel");
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                panel = property.objectReferenceValue as GameObject;
            if (panel != null) break;
        }
        if (panel == null || overlay.IsChildOf(panel.transform)) return;
        canvasRoot.AddComponent<PanelOverlayLink>().EditorSetup(panel, overlay.gameObject);
        overlay.gameObject.SetActive(false);
    }

    private static void SetupEnvironment(Color studio)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat; // en modo Color para poder bajarla en el Apagón
        RenderSettings.ambientLight = new Color(0.55f, 0.5f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = studio;
        RenderSettings.fogStartDistance = FogStart;
        RenderSettings.fogEndDistance = FogEnd;
    }

    private static Light CreateMainLight()
    {
        var go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.shadows = LightShadows.Soft;
        return light;
    }

    // Focos de colores a los costados del estudio.
    private static Light[] CreateStudioLights()
    {
        string[] colors = { "#F7B11D", "#4FA3E0", "#FFFFFF", "#F7B11D" };
        Vector3[] positions = { new Vector3(-9f, 7f, 10f), new Vector3(9f, 7f, 10f), new Vector3(-9f, 7f, 35f), new Vector3(9f, 7f, 35f) };
        var lights = new Light[positions.Length];
        var root = new GameObject("StudioLights").transform;
        for (int i = 0; i < positions.Length; i++)
        {
            var go = new GameObject("Spot");
            go.transform.SetParent(root, false);
            go.transform.position = positions[i];
            // Apuntan al agua de cada lado para no encandilar la pista.
            go.transform.LookAt(new Vector3(Mathf.Sign(positions[i].x) * 4.5f, WaterHeight, positions[i].z + 8f));
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            ColorUtility.TryParseHtmlString(colors[i], out Color color);
            light.color = color;
            light.intensity = 2f;
            light.range = 28f;
            light.spotAngle = 60f;
            lights[i] = light;
        }
        return lights;
    }

    private static void CreatePool(BalanceConfig balance)
    {
        float length = balance.SpawnAheadDistance + balance.SegmentLength * 3f;
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane); // Plane = 10 × 10 m
        water.name = "Water";
        water.transform.position = new Vector3(0f, WaterHeight, balance.SpawnAheadDistance * 0.5f);
        water.transform.localScale = new Vector3(PoolWidth / 10f, 1f, length / 10f);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        var waterRenderer = water.GetComponent<Renderer>();
        waterRenderer.sharedMaterial = StudioSetCreator.WaterMaterial();
        waterRenderer.shadowCastingMode = ShadowCastingMode.Off;
        water.AddComponent<WaterSurface>();

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "PoolFloor";
        floor.transform.position = new Vector3(0f, PoolFloorHeight, balance.SpawnAheadDistance * 0.5f);
        floor.transform.localScale = new Vector3(PoolWidth / 10f, 1f, length / 10f);
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.GetComponent<Renderer>().sharedMaterial = StudioSetCreator.PoolFloorMaterial();
    }

    private static void CreateScreen()
    {
        Transform screen = new GameObject("TitoScreen").transform;
        screen.position = ScreenPosition;
        screen.localScale = Vector3.one * ScreenScale;
        Material frame = GameMaterials.GetOrCreateNoFog("Mat_ScreenFrame", "#3A3150", 0f);
        Material panel = GameMaterials.GetOrCreateNoFog("Mat_ScreenPanel", "#F2EFE8", 0.35f);
        Material onAir = GameMaterials.GetOrCreateNoFog("Mat_ScreenOnAir", "#D13B3B", 0.8f);

        ObstaclePrefabCreator.Part(PrimitiveType.Cube, screen, "Frame", Vector3.zero, ScreenSize, frame);
        ObstaclePrefabCreator.Part(PrimitiveType.Cube, screen, "Panel", new Vector3(0f, 0f, -ScreenSize.z * 0.5f - 0.03f),
            new Vector3(ScreenSize.x - 1f, ScreenSize.y - 1f, 0.06f), panel);
        ObstaclePrefabCreator.Part(PrimitiveType.Sphere, screen, "OnAir",
            new Vector3(-ScreenSize.x * 0.5f + 1.3f, ScreenSize.y * 0.5f - 1.2f, -ScreenSize.z * 0.5f - 0.1f),
            new Vector3(0.6f, 0.6f, 0.2f), onAir);
        float legHeight = ScreenPosition.y - ScreenSize.y * 0.5f - WaterHeight;
        foreach (float side in new[] { -1f, 1f })
        {
            ObstaclePrefabCreator.Part(PrimitiveType.Cube, screen, "Leg",
                new Vector3(side * ScreenSize.x * 0.3f, -ScreenSize.y * 0.5f - legHeight * 0.5f, 0f),
                new Vector3(0.6f, legHeight, 0.6f), frame);
        }

        // Cara de Tito (Tania) con materiales sin niebla para que se vea desde lejos.
        var facePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ProjectPaths.Root}/Prefabs/UI/HostFace.prefab");
        if (facePrefab == null)
        {
            Debug.LogWarning("No está Prefabs/UI/HostFace.prefab (cara de Tito, Tania): la pantalla queda vacía.");
            return;
        }
        var face = (GameObject)PrefabUtility.InstantiatePrefab(facePrefab, screen);
        face.transform.localPosition = new Vector3(0f, -0.6f, -ScreenSize.z * 0.5f - 0.6f);
        face.transform.localScale = Vector3.one * HostFaceScale;
        foreach (Renderer renderer in face.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = GameMaterials.NoFogCopyOf(materials[i], 0.25f);
            renderer.sharedMaterials = materials;
        }
    }

    private static void CreateHost()
    {
        Material podium = GameMaterials.GetOrCreate("Mat_Podium", "#33255A");
        Transform root = new GameObject("TitoPodium").transform;
        root.position = PodiumPosition;
        ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, root, "Base", new Vector3(0f, PodiumTop - PodiumHeight * 0.5f, 0f),
            new Vector3(2.6f, PodiumHeight * 0.5f, 2.6f), podium);
        ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, root, "GoldRing", new Vector3(0f, PodiumTop - 0.02f, 0f),
            new Vector3(2.7f, 0.03f, 2.7f), MaterialCreator.Load("Mat_Gold"));

        var hostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabCreator.HostPath);
        if (hostPrefab == null) return;
        var host = (GameObject)PrefabUtility.InstantiatePrefab(hostPrefab, root);
        host.transform.localPosition = new Vector3(0f, PodiumTop, 0f);
        Vector3 look = HostLooksAt - PodiumPosition;
        host.transform.rotation = Quaternion.LookRotation(new Vector3(look.x, 0f, look.z));
    }

    // ---------- Captura para revisar sin entrar en Play ----------

    [MenuItem("Una Ronda Más/Pruebas/Capturar vista de Game.unity")]
    public static void CapturePreview()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var track = Object.FindObjectOfType<TrackGenerator>();
        var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        if (track != null && balance != null)
        {
            const int round = 4;
            track.BuildRound(round, null, true, balance.GetBaseSpeed(round) * balance.RoundDuration);
            track.SetTravelled(balance.SegmentLength * 0.6f);
        }
        Pose(CharacterPrefabCreator.ContestantModel, "sprint", 0.12f);
        Pose(CharacterPrefabCreator.HostModel, "interact-right", 0.3f);
        PoseAudience();

        Directory.CreateDirectory(PreviewRenderer.OutputFolder);
        Texture2D shot = PreviewRenderer.Capture(Camera.main, 1280, 720);
        string path = Path.Combine(PreviewRenderer.OutputFolder, "game_preview.png");
        File.WriteAllBytes(path, shot.EncodeToPNG());
        Object.DestroyImmediate(shot);
        Debug.Log("Captura guardada: " + path);

        // No guardar: la pista y las poses eran solo para la foto.
        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
    }

    // El público, cada uno con el «idle» de su modelo y en un momento distinto.
    private static void PoseAudience()
    {
        var random = new System.Random(3);
        foreach (Animator animator in Object.FindObjectsOfType<Animator>())
        {
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller == null || !controller.name.StartsWith("Audience")) continue;
            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip.name != "idle") continue;
                clip.SampleAnimation(animator.gameObject, (float)random.NextDouble() * clip.length);
                break;
            }
        }
    }

    private static void Pose(string model, string clipName, float time)
    {
        foreach (Animator animator in Object.FindObjectsOfType<Animator>())
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(animator.gameObject);
            if (source == null || source.name != model) continue;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(CharacterPrefabCreator.ModelPath(model)))
            {
                if (asset is AnimationClip clip && clip.name == clipName) clip.SampleAnimation(animator.gameObject, time);
            }
        }
    }
}
