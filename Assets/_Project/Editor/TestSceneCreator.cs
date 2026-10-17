using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea Scenes/Test_Nicole.unity: cámara, luz, agua, GameSystems con tramos de prueba,
/// un concursante de prueba (DebugContestant) y el panel de depuración.
/// Sirve para probar los sistemas de Nicole sin tocar Game.unity (es de Otoniel).
/// </summary>
public static class TestSceneCreator
{
    public const string ScenePath = ProjectPaths.Scenes + "/Test_Nicole.unity";
    public const string SafePath = ProjectPaths.TestPrefabs + "/TestSegment_Safe.prefab";
    public const string StarsPath = ProjectPaths.TestPrefabs + "/TestSegment_Stars.prefab";
    public const string BlocksPath = ProjectPaths.TestPrefabs + "/TestSegment_Blocks.prefab";

    private const string StudioColor = "#24193F";
    private static readonly Vector3 CameraPosition = new Vector3(0f, 4f, -7f);
    private const float CameraPitch = 15f;
    private const float FogStart = 50f;
    private const float FogEnd = 115f;
    private const float StarHeight = 1f;
    private const float BlockSize = 1.6f;
    private const float FloorThickness = 0.2f;
    private const float FloorSideMargin = 0.4f;
    private const float ContestantScale = 0.8f;   // cápsula de 2 m × 0,8 = 1,6 m

    [MenuItem("Una Ronda Más/Pruebas/Crear escena de prueba (Nicole)")]
    public static void CreateMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Create();
    }

    public static void Create()
    {
        MaterialCreator.CreateAll();
        DataAssetCreator.CreateAll();
        StarPrefabCreator.CreateAll();
        GameObject systemsPrefab = GameSystemsPrefabCreator.CreateOrUpdate();
        BalanceConfig balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        CreateTestSegments(balance);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ColorUtility.TryParseHtmlString(StudioColor, out Color studio);
        SetupEnvironment(studio);
        CreateCamera(studio);
        CreateLight();
        CreateWater(balance);

        var systems = (GameObject)PrefabUtility.InstantiatePrefab(systemsPrefab, scene);
        var trackSo = new SerializedObject(systems.GetComponent<TrackGenerator>());
        ProjectPaths.SetArray(trackSo, "segmentPrefabs", new[]
        {
            AssetDatabase.LoadAssetAtPath<TrackSegment>(StarsPath),
            AssetDatabase.LoadAssetAtPath<TrackSegment>(BlocksPath),
        });
        ProjectPaths.SetObject(trackSo, "safeSegmentPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(SafePath));
        trackSo.ApplyModifiedPropertiesWithoutUndo();

        new GameObject("DevTools").AddComponent<DebugOverlay>();
        CreateContestant(balance);

        ProjectPaths.EnsureFolder(ProjectPaths.Scenes);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Escena de prueba lista: " + ScenePath + " (dale Play).");
    }

    private static void SetupEnvironment(Color studio)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.5f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = studio;
        RenderSettings.fogStartDistance = FogStart;
        RenderSettings.fogEndDistance = FogEnd;
    }

    private static void CreateCamera(Color studio)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraPitch, 0f, 0f));
        var camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = studio;
        go.AddComponent<AudioListener>();
    }

    private static void CreateLight()
    {
        var go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateWater(BalanceConfig balance)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane); // Plane = 10 × 10 m
        water.name = "Water";
        water.transform.position = new Vector3(0f, -0.6f, balance.SpawnAheadDistance * 0.5f);
        water.transform.localScale = new Vector3(4f, 1f, (balance.SpawnAheadDistance + balance.SegmentLength * 3f) / 10f);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.GetComponent<Renderer>().sharedMaterial = MaterialCreator.Load("Mat_Water");
    }

    private static void CreateContestant(BalanceConfig balance)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "TestContestant";
        go.tag = "Player";
        go.transform.position = new Vector3(0f, ContestantScale, 0f);
        go.transform.localScale = Vector3.one * ContestantScale;
        go.GetComponent<Renderer>().sharedMaterial = MaterialCreator.Load("Mat_Player");
        var body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        go.AddComponent<DebugContestant>();
    }

    // ---------- Tramos de prueba (no son los de Otoniel) ----------

    public static void CreateTestSegments(BalanceConfig balance)
    {
        ProjectPaths.EnsureFolder(ProjectPaths.TestPrefabs);
        var normal = AssetDatabase.LoadAssetAtPath<GameObject>(StarPrefabCreator.NormalPath);
        var golden = AssetDatabase.LoadAssetAtPath<GameObject>(StarPrefabCreator.GoldenPath);
        float lane = balance.LaneWidth;

        // Vacío
        GameObject safe = CreateRoot("TestSegment_Safe", balance, false);
        Save(safe, SafePath);

        // Estrellas en el centro y doradas a los lados
        GameObject stars = CreateRoot("TestSegment_Stars", balance, true);
        foreach (float z in new[] { 4f, 8f, 12f, 16f }) AddPrefab(normal, stars.transform, new Vector3(0f, StarHeight, z));
        AddPrefab(golden, stars.transform, new Vector3(-lane, StarHeight, 10f));
        AddPrefab(golden, stars.transform, new Vector3(lane, StarHeight, 10f));
        Save(stars, StarsPath);

        // Bloques a los lados; el centro queda libre con estrellas
        GameObject blocks = CreateRoot("TestSegment_Blocks", balance, true);
        AddBlock(blocks.transform, new Vector3(-lane, BlockSize * 0.5f, 10f));
        AddBlock(blocks.transform, new Vector3(lane, BlockSize * 0.5f, 10f));
        foreach (float z in new[] { 5f, 10f, 15f }) AddPrefab(normal, blocks.transform, new Vector3(0f, StarHeight, z));
        Save(blocks, BlocksPath);
    }

    private static GameObject CreateRoot(string name, BalanceConfig balance, bool withSegment)
    {
        var root = new GameObject(name);
        if (withSegment)
        {
            root.AddComponent<TrackSegment>().DebugSetup(SegmentDifficulty.Easy, false, false, false, false, false, null);
            root.GetComponent<Rigidbody>().isKinematic = true;
            root.GetComponent<Rigidbody>().useGravity = false;
        }
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = new Vector3(0f, -FloorThickness * 0.5f, balance.SegmentLength * 0.5f);
        floor.transform.localScale = new Vector3(balance.LaneWidth * 3f + FloorSideMargin * 2f, FloorThickness, balance.SegmentLength);
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.GetComponent<Renderer>().sharedMaterial = MaterialCreator.Load("Mat_Track");
        return root;
    }

    private static void AddPrefab(GameObject prefab, Transform parent, Vector3 localPosition)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = localPosition;
    }

    private static void AddBlock(Transform parent, Vector3 localPosition)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Block";
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = Vector3.one * BlockSize;
        block.GetComponent<Collider>().isTrigger = true;
        block.GetComponent<Renderer>().sharedMaterial = MaterialCreator.Load("Mat_Red");
    }

    private static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
