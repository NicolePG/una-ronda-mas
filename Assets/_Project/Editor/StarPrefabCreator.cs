using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Crea la malla de estrella y los prefabs Star_Normal y Star_Golden.</summary>
public static class StarPrefabCreator
{
    public const string MeshPath = ProjectPaths.Art + "/StarMesh.asset";
    public const string NormalPath = ProjectPaths.PrefabsGameplay + "/Star_Normal.prefab";
    public const string GoldenPath = ProjectPaths.PrefabsGameplay + "/Star_Golden.prefab";

    private const float TriggerRadius = 0.5f;
    private const float GoldenScale = 1.35f;

    [MenuItem("Una Ronda Más/Prefabs/Crear estrellas")]
    public static void CreateAll()
    {
        MaterialCreator.CreateAll();
        Mesh mesh = GetOrCreateMesh();
        CreateStar(NormalPath, false, mesh, MaterialCreator.Load("Mat_StarLight"), 1f);
        CreateStar(GoldenPath, true, mesh, MaterialCreator.Load("Mat_Gold"), GoldenScale);
        AssetDatabase.SaveAssets();
    }

    private static Mesh GetOrCreateMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh != null) return mesh;
        ProjectPaths.EnsureFolder(ProjectPaths.Art);
        mesh = StarMeshBuilder.Build();
        AssetDatabase.CreateAsset(mesh, MeshPath);
        return mesh;
    }

    private static void CreateStar(string path, bool golden, Mesh mesh, Material material, float scale)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        ProjectPaths.EnsureFolder(ProjectPaths.PrefabsGameplay);

        var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        var trigger = go.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = TriggerRadius;

        var so = new SerializedObject(go.AddComponent<Star>());
        so.FindProperty("isGolden").boolValue = golden;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
