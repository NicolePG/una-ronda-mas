using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Informe de balance (Fase 3): por ronda calcula metros, tramos, meta y cuántos puntos de estrella
/// debería tener cada tramo. Si ya hay tramos en Prefabs/Segments, compara con lo que realmente traen.
/// Lo escribe en docs/BALANCE.md y en la Consola.
/// </summary>
public static class BalanceReport
{
    public const string ReportPath = "docs/BALANCE.md";
    private const int Rounds = 12;
    private const float TooHard = 0.8f;   // hay que juntar más del 80% de las estrellas
    private const float TooEasy = 0.25f;  // alcanza con menos del 25%

    [MenuItem("Una Ronda Más/Pruebas/Informe de balance")]
    public static void GenerateMenu()
    {
        string report = Generate();
        if (report != null) Debug.Log(report);
    }

    public static string Generate()
    {
        var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        if (balance == null)
        {
            Debug.LogError("Falta Balance.asset (menú Una Ronda Más/Datos).");
            return null;
        }

        var segments = new List<TrackSegment>();
        foreach (TrackSegment segment in ProjectPaths.LoadPrefabsWith<TrackSegment>(ProjectPaths.PrefabsSegments))
        {
            if (!segment.name.Contains("Safe")) segments.Add(segment);
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Informe de balance");
        sb.AppendLine();
        sb.AppendLine("Generado por **Una Ronda Más → Pruebas → Informe de balance** con `Data/Balance.asset`. No editar a mano: se vuelve a generar.");
        sb.AppendLine();
        sb.AppendLine($"- Ronda de {balance.RoundDuration:0} s; tramos de {balance.SegmentLength:0} m; {balance.SafeStartSegments} tramo(s) vacío(s) al empezar.");
        sb.AppendLine($"- Objetivo de diseño: que para llegar a la meta haya que juntar ~{balance.TargetCollectRatio:P0} de las estrellas de la ronda.");
        sb.AppendLine($"- Tramos encontrados en `Prefabs/Segments`: {segments.Count}.");
        sb.AppendLine();
        sb.AppendLine("| Ronda | Velocidad (m/s) | Metros | Tramos | Meta | Meta con apuesta | Pts/tramo sugeridos | Pts/tramo con apuesta | Pts/tramo reales | % a juntar |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        var warnings = new List<string>();
        for (int round = 1; round <= Rounds; round++)
        {
            float speed = balance.GetBaseSpeed(round);
            float meters = speed * balance.RoundDuration;
            float tramos = Mathf.Max(0.01f, (meters - balance.SafeStartSegments * balance.SegmentLength) / balance.SegmentLength);
            int goal = balance.GetGoal(round);
            bool bet = round >= balance.FirstBetRound;
            int betGoal = balance.GetBetGoal(goal);
            float suggested = goal / (tramos * balance.TargetCollectRatio);
            float suggestedBet = betGoal / (tramos * balance.TargetCollectRatio);
            float? real = ExpectedStarValuePerSegment(segments, round, balance);

            string realText = real.HasValue ? $"{real.Value:0.0}" : "—";
            string percent = "—";
            if (real.HasValue && real.Value > 0f)
            {
                float ratio = goal / (tramos * real.Value);
                percent = $"{ratio:P0}";
                if (ratio > TooHard) warnings.Add($"Ronda {round}: hay que juntar {ratio:P0} de las estrellas (muy difícil). Agregar estrellas o bajar la meta.");
                else if (ratio < TooEasy) warnings.Add($"Ronda {round}: alcanza con {ratio:P0} de las estrellas (muy fácil).");
            }
            else if (real.HasValue)
            {
                warnings.Add($"Ronda {round}: los tramos disponibles no tienen estrellas.");
            }

            sb.AppendLine($"| {round} | {speed:0.0} | {meters:0} | {tramos:0.0} | {goal} | {(bet ? betGoal.ToString() : "—")} | " +
                          $"{suggested:0.0} | {(bet ? suggestedBet.ToString("0.0") : "—")} | {realText} | {percent} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Cómo leerlo:** «Pts/tramo sugeridos» es cuántos puntos de estrella (normal = " +
                      $"{balance.NormalStarValue}, dorada = {balance.GoldenStarValue}) debería tener en promedio cada tramo de esa ronda. " +
                      "«% a juntar» usa los tramos reales: cerca del objetivo está bien. «Todo vale doble» duplica las estrellas pero la pista va más rápido.");
        sb.AppendLine();
        if (warnings.Count > 0)
        {
            sb.AppendLine("## Avisos");
            sb.AppendLine();
            foreach (string warning in warnings) sb.AppendLine("- " + warning);
        }
        else if (segments.Count == 0)
        {
            sb.AppendLine("Todavía no hay tramos: usa la columna «Pts/tramo sugeridos» como guía para armarlos.");
        }

        string text = sb.ToString();
        string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ReportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, text, new UTF8Encoding(false));
        return text;
    }

    /// <summary>Puntos de estrella promedio de un tramo en esa ronda, con los pesos de dificultad del generador.</summary>
    public static float? ExpectedStarValuePerSegment(List<TrackSegment> segments, int round, BalanceConfig balance)
    {
        var allowed = new List<TrackSegment>();
        foreach (TrackSegment segment in segments)
        {
            if (TrackGenerator.IsSegmentAllowed(segment, round, null, balance)) allowed.Add(segment);
        }
        if (allowed.Count == 0) allowed.AddRange(segments); // igual que el respaldo del generador
        if (allowed.Count == 0) return null;

        DifficultyStage stage = balance.GetDifficultyStage(round);
        float totalWeight = 0f;
        float weighted = 0f;
        float plainSum = 0f;
        foreach (SegmentDifficulty difficulty in System.Enum.GetValues(typeof(SegmentDifficulty)))
        {
            int count = 0;
            float sum = 0f;
            foreach (TrackSegment segment in allowed)
            {
                if (segment.Difficulty != difficulty) continue;
                count++;
                sum += segment.GetStarValue(balance);
            }
            plainSum += sum;
            if (count == 0) continue;
            float weight = stage.GetWeight(difficulty);
            totalWeight += weight;
            weighted += weight * sum / count;
        }
        return totalWeight > 0f ? weighted / totalWeight : plainSum / allowed.Count;
    }
}
