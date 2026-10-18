using UnityEditor;
using UnityEngine;

/// <summary>Crea el prefab 3D de Tito con materiales y expresiones conectadas.</summary>
public static class HostFacePrefabCreator
{
    private const string MaterialsFolder = "Assets/_Project/Materials";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/HostFace.prefab";

    [MenuItem("Una Ronda Más/UI/Crear prefab cara 3D de Tito")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.Log($"El prefab ya existe: {PrefabPath}");
            return;
        }

        ProjectPaths.EnsureFolder(MaterialsFolder);
        Material skin = GetOrCreateMaterial("Mat_HostSkin", "#E6A968");
        Material hair = GetOrCreateMaterial("Mat_HostHair", "#17131F");
        Material feature = GetOrCreateMaterial("Mat_HostFeature", "#24193F");
        Material tongue = GetOrCreateMaterial("Mat_HostTongue", "#D13B3B");
        if (skin == null || hair == null || feature == null || tongue == null) return;

        var root = new GameObject("HostFace", typeof(HostFace));
        Transform visual = new GameObject("FaceVisual").transform;
        visual.SetParent(root.transform, false);

        CreatePart(visual, "Face", PrimitiveType.Sphere, Vector3.zero,
            new Vector3(3.6f, 4f, 0.55f), skin);
        Transform wig = CreatePart(visual, "Wig", PrimitiveType.Sphere,
            new Vector3(0f, 1.62f, -0.04f), new Vector3(3.15f, 1.15f, 0.62f), hair);
        CreatePart(wig, "WigTuftLeft", PrimitiveType.Sphere,
            new Vector3(-0.65f, 0.35f, -0.1f), new Vector3(1.2f, 0.65f, 0.75f), hair);
        CreatePart(wig, "WigTuftRight", PrimitiveType.Sphere,
            new Vector3(0.55f, 0.4f, -0.08f), new Vector3(1.1f, 0.62f, 0.72f), hair);

        CreatePart(visual, "LeftEye", PrimitiveType.Sphere,
            new Vector3(-0.78f, 0.42f, -0.29f), new Vector3(0.38f, 0.48f, 0.16f), feature);
        GameObject rightEye = CreatePart(visual, "RightEye", PrimitiveType.Sphere,
            new Vector3(0.78f, 0.42f, -0.29f), new Vector3(0.38f, 0.48f, 0.16f), feature).gameObject;
        Transform winkGroup = new GameObject("WinkEye").transform;
        winkGroup.SetParent(visual, false);
        winkGroup.localPosition = new Vector3(0.78f, 0.42f, -0.3f);
        CreatePart(winkGroup, "ClosedLid", PrimitiveType.Capsule, Vector3.zero,
            new Vector3(0.42f, 0.1f, 0.12f), feature).localRotation = Quaternion.Euler(0f, 0f, 90f);
        winkGroup.gameObject.SetActive(false);

        Transform smile = new GameObject("Smile").transform;
        smile.SetParent(visual, false);
        Vector3[] smilePoints =
        {
            new Vector3(-0.48f, -0.63f, -0.32f),
            new Vector3(-0.25f, -0.82f, -0.32f),
            new Vector3(0f, -0.88f, -0.32f),
            new Vector3(0.25f, -0.82f, -0.32f),
            new Vector3(0.48f, -0.63f, -0.32f),
        };
        for (int i = 0; i < smilePoints.Length; i++)
            CreatePart(smile, $"SmilePoint_{i}", PrimitiveType.Sphere, smilePoints[i],
                new Vector3(0.17f, 0.14f, 0.08f), feature);

        GameObject laugh = CreatePart(visual, "LaughMouth", PrimitiveType.Sphere,
            new Vector3(0f, -0.7f, -0.34f), new Vector3(0.72f, 0.9f, 0.14f), feature).gameObject;
        CreatePart(laugh.transform, "Tongue", PrimitiveType.Sphere,
            new Vector3(0f, -0.22f, -0.12f), new Vector3(0.48f, 0.24f, 0.12f), tongue);
        laugh.SetActive(false);

        HostFace face = root.GetComponent<HostFace>();
        SerializedObject serializedFace = new SerializedObject(face);
        SetReference(serializedFace, "faceVisual", visual);
        SetReference(serializedFace, "winkOpenEye", rightEye);
        SetReference(serializedFace, "winkClosedEye", winkGroup.gameObject);
        SetReference(serializedFace, "smileMouth", smile.gameObject);
        SetReference(serializedFace, "laughMouth", laugh);
        SetReference(serializedFace, "wig", wig);
        serializedFace.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        if (prefab == null) Debug.LogError($"No se pudo crear el prefab: {PrefabPath}");
        else Debug.Log($"Prefab 3D de la cara de Tito creado: {PrefabPath}");
    }

    private static Material GetOrCreateMaterial(string name, string hex)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader standard = Shader.Find("Standard");
        if (standard == null)
        {
            Debug.LogError("No se encontró el shader Standard para crear los materiales de Tito.");
            return null;
        }

        if (!ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            Debug.LogError($"Color inválido para el material {name}: {hex}");
            return null;
        }

        material = new Material(standard) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Transform CreatePart(Transform parent, string objectName, PrimitiveType type,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part.transform;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No existe el campo «{propertyName}» en HostFace.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
