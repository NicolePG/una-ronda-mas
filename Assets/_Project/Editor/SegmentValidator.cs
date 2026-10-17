using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Revisa los tramos de Prefabs/Segments: largo de 20 m, Rigidbody cinemático, obstáculos fuera de los bordes,
/// estrellas y cantidad por dificultad. Muestra el resultado en la Consola.
/// </summary>
public static class SegmentValidator
{
    private const float LengthTolerance = 0.5f;
    private const int SegmentsPerDifficulty = 4;

    [MenuItem("Una Ronda Más/Pruebas/Validar tramos")]
    public static void ValidateMenu()
    {
        string summary = ValidateAll(out int problems);
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Validar tramos", summary + "\n\nDetalle en la Consola.", "OK");
        if (problems > 0) Debug.LogWarning(summary);
        else Debug.Log(summary);
    }

    public static string ValidateAll(out int problems)
    {
        problems = 0;
        var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        if (balance == null)
        {
            problems = 1;
            return "Falta Balance.asset (menú Una Ronda Más/Datos).";
        }

        var counts = new int[3];
        List<TrackSegment> segments = ProjectPaths.LoadPrefabsWith<TrackSegment>(ProjectPaths.PrefabsSegments);
        foreach (TrackSegment segment in segments)
        {
            if (segment.name.Contains("Safe")) continue;
            counts[(int)segment.Difficulty]++;
            List<string> issues = Check(segment, balance);
            string info = $"{segment.name}: {segment.Difficulty}, estrellas {segment.GetStarValue(balance)} pts" +
                          $"{(segment.RequiresJump ? ", obliga a saltar" : "")}{(segment.RequiresSlide ? ", obliga a deslizarse" : "")}";
            if (issues.Count == 0)
            {
                Debug.Log("OK  " + info, segment);
                continue;
            }
            problems += issues.Count;
            Debug.LogWarning($"REVISAR  {info}\n - {string.Join("\n - ", issues)}", segment);
        }

        var sb = new StringBuilder();
        sb.Append($"{segments.Count} tramos. Fáciles {counts[0]}, medios {counts[1]}, difíciles {counts[2]} ");
        sb.Append($"(meta MVP: {SegmentsPerDifficulty} de cada uno). Problemas: {problems}.");
        return sb.ToString();
    }

    /// <summary>Lista de problemas de un tramo (vacía si está bien).</summary>
    public static List<string> Check(TrackSegment segment, BalanceConfig balance)
    {
        var issues = new List<string>();
        Transform root = segment.transform;
        float length = balance.SegmentLength;
        float margin = balance.SegmentEdgeMargin;

        Rigidbody body = segment.GetComponent<Rigidbody>();
        if (body == null || !body.isKinematic) issues.Add("La raíz necesita Rigidbody con Is Kinematic.");

        // Largo según las mallas.
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (MeshFilter filter in segment.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponentInParent<Star>(true) != null) continue;
            Bounds bounds = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float z = root.InverseTransformPoint(filter.transform.TransformPoint(corner)).z;
                minZ = Mathf.Min(minZ, z);
                maxZ = Mathf.Max(maxZ, z);
            }
        }
        if (minZ <= maxZ)
        {
            if (minZ < -LengthTolerance || maxZ > length + LengthTolerance || maxZ < length - LengthTolerance)
                issues.Add($"Va de z={minZ:0.0} a z={maxZ:0.0}; debe ir de 0 a {length:0} (pivote al inicio del tramo).");
        }

        // Obstáculos (triggers que no son estrellas) en los bordes.
        int stars = 0;
        foreach (Collider collider in segment.GetComponentsInChildren<Collider>(true))
        {
            if (collider.GetComponentInParent<Star>(true) != null)
            {
                stars++;
                continue;
            }
            if (!collider.isTrigger)
            {
                issues.Add($"«{collider.name}» tiene un collider que no es Trigger (no detecta choques; quítalo o márcalo Trigger).");
                continue;
            }
            GetZRange(collider, root, out float start, out float end);
            if (start < margin || end > length - margin)
                issues.Add($"«{collider.name}» está en z={start:0.0}–{end:0.0}: deja libres los primeros y últimos {margin:0} m.");
        }
        if (stars == 0) issues.Add("No tiene estrellas.");
        return issues;
    }

    private static void GetZRange(Collider collider, Transform root, out float start, out float end)
    {
        Transform t = collider.transform;
        Vector3 center = t.position;
        float halfDepth = 0f;
        switch (collider)
        {
            case BoxCollider box:
                center = t.TransformPoint(box.center);
                halfDepth = box.size.z * 0.5f * Mathf.Abs(t.lossyScale.z);
                break;
            case SphereCollider sphere:
                center = t.TransformPoint(sphere.center);
                halfDepth = sphere.radius * Mathf.Abs(t.lossyScale.z);
                break;
            case CapsuleCollider capsule:
                center = t.TransformPoint(capsule.center);
                halfDepth = (capsule.direction == 2 ? capsule.height * 0.5f : capsule.radius) * Mathf.Abs(t.lossyScale.z);
                break;
        }
        float z = root.InverseTransformPoint(center).z;
        start = z - halfDepth;
        end = z + halfDepth;
    }
}
