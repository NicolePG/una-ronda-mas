using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea o actualiza Prefabs/Gameplay/GameSystems.prefab (GameManager + RuleManager + TrackGenerator)
/// con los datos y los tramos que haya en Prefabs/Segments. Se pone UNA vez en Game.unity.
/// Volver a ejecutarlo cuando Otoniel agregue tramos nuevos.
/// </summary>
public static class GameSystemsPrefabCreator
{
    public const string PrefabPath = ProjectPaths.PrefabsGameplay + "/GameSystems.prefab";
    public const string FinishArchPath = ProjectPaths.PrefabsGameplay + "/FinishArch.prefab";

    [MenuItem("Una Ronda Más/Prefabs/Crear o actualizar GameSystems (tramos incluidos)")]
    public static void CreateOrUpdateMenu()
    {
        GameObject prefab = CreateOrUpdate();
        if (prefab != null) Selection.activeObject = prefab;
    }

    public static GameObject CreateOrUpdate()
    {
        ProjectPaths.EnsureFolder(ProjectPaths.PrefabsGameplay);
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("GameSystems");
        try
        {
            Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static void Configure(GameObject root)
    {
        GameManager gm = GetOrAdd<GameManager>(root);
        RuleManager rules = GetOrAdd<RuleManager>(root);
        TrackGenerator track = GetOrAdd<TrackGenerator>(root);
        var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        if (balance == null) Debug.LogWarning("Falta Balance.asset: ejecuta «Una Ronda Más/Datos/Crear Balance, premios y reglas».");

        var gmSo = new SerializedObject(gm);
        ProjectPaths.SetObject(gmSo, "balance", balance);
        ProjectPaths.SetArray(gmSo, "prizePool", ProjectPaths.LoadAll<PrizeData>(ProjectPaths.Prizes));
        ProjectPaths.SetObject(gmSo, "ruleManager", rules);
        ProjectPaths.SetObject(gmSo, "trackGenerator", track);
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        var rulesSo = new SerializedObject(rules);
        ProjectPaths.SetArray(rulesSo, "rules", ProjectPaths.LoadAll<RuleData>(ProjectPaths.Rules));
        rulesSo.ApplyModifiedPropertiesWithoutUndo();

        // Tramos: los que tienen "Safe" en el nombre son el tramo vacío; el resto entra al sorteo.
        var segments = new List<TrackSegment>();
        GameObject safe = null;
        foreach (TrackSegment segment in ProjectPaths.LoadPrefabsWith<TrackSegment>(ProjectPaths.PrefabsSegments))
        {
            if (segment.name.Contains("Safe")) safe = segment.gameObject;
            else segments.Add(segment);
        }

        var trackSo = new SerializedObject(track);
        ProjectPaths.SetObject(trackSo, "balance", balance);
        ProjectPaths.SetArray(trackSo, "segmentPrefabs", segments);
        ProjectPaths.SetObject(trackSo, "safeSegmentPrefab", safe);
        ProjectPaths.SetObject(trackSo, "finishArchPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(FinishArchPath));
        ProjectPaths.SetObject(trackSo, "hologramPrefab", FindHologram());
        ProjectPaths.SetObject(trackSo, "fallbackTrackMaterial", MaterialCreator.Load("Mat_Track"));
        ProjectPaths.SetObject(trackSo, "fallbackArchMaterial", MaterialCreator.Load("Mat_Gold"));
        trackSo.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"GameSystems: {segments.Count} tramos{(safe != null ? " + tramo seguro" : "")}, " +
                  $"holograma {(FindHologram() != null ? "sí" : "no")}, arco {(AssetDatabase.LoadAssetAtPath<GameObject>(FinishArchPath) != null ? "sí" : "de respaldo")}.");
    }

    private static GameObject FindHologram()
    {
        foreach (GameObject prefab in ProjectPaths.LoadAll<GameObject>(ProjectPaths.PrefabsObstacles))
        {
            if (prefab.name.Contains("Holo")) return prefab;
        }
        return null;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
