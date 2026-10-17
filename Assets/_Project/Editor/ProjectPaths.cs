using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Rutas del proyecto y utilidades comunes de las herramientas de Editor.</summary>
public static class ProjectPaths
{
    public const string Root = "Assets/_Project";
    public const string Data = Root + "/Data";
    public const string Prizes = Data + "/Prizes";
    public const string Rules = Data + "/Rules";
    public const string Balance = Data + "/Balance.asset";
    public const string Materials = Root + "/Materials";
    public const string Art = Root + "/Art";
    public const string Scenes = Root + "/Scenes";
    public const string PrefabsGameplay = Root + "/Prefabs/Gameplay";
    public const string PrefabsSegments = Root + "/Prefabs/Segments";
    public const string PrefabsObstacles = Root + "/Prefabs/Obstacles";
    public const string TestPrefabs = PrefabsGameplay + "/Test";

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    /// <summary>Todos los assets de tipo T dentro de la carpeta, ordenados por nombre.</summary>
    public static List<T> LoadAll<T>(string folder) where T : Object
    {
        var result = new List<T>();
        if (!AssetDatabase.IsValidFolder(folder)) return result;
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) result.Add(asset);
        }
        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    /// <summary>Prefabs de la carpeta que tienen el componente T en la raíz.</summary>
    public static List<T> LoadPrefabsWith<T>(string folder) where T : Component
    {
        var result = new List<T>();
        foreach (GameObject prefab in LoadAll<GameObject>(folder))
        {
            T component = prefab.GetComponent<T>();
            if (component != null) result.Add(component);
        }
        return result;
    }

    public static void SetObject(SerializedObject so, string property, Object value)
    {
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null) Debug.LogError($"No existe el campo «{property}» en {so.targetObject.GetType().Name}.");
        else prop.objectReferenceValue = value;
    }

    public static void SetArray<T>(SerializedObject so, string property, IList<T> values) where T : Object
    {
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null)
        {
            Debug.LogError($"No existe el campo «{property}» en {so.targetObject.GetType().Name}.");
            return;
        }
        prop.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
