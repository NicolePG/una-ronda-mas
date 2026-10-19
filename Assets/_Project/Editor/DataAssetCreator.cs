using UnityEditor;
using UnityEngine;

/// <summary>Crea Balance.asset, los 10 premios y las 5 reglas. No pisa los que ya existen (para no perder el balance).</summary>
public static class DataAssetCreator
{
    [MenuItem("Una Ronda Más/Datos/Crear Balance, premios y reglas")]
    public static void CreateAll()
    {
        ProjectPaths.EnsureFolder(ProjectPaths.Prizes);
        ProjectPaths.EnsureFolder(ProjectPaths.Rules);

        CreateIfMissing<BalanceConfig>(ProjectPaths.Balance, _ => { });

        Prize("Prize_ExtraHeart", PrizeType.ExtraHeart, false, "Corazón extra", "+1 vida (máximo 5).", "");
        Prize("Prize_Magnet", PrizeType.Magnet, false, "Imán", "Atrae las estrellas de los carriles vecinos.", "");
        Prize("Prize_Float", PrizeType.Float, false, "Flotador", "La primera caída al agua de cada ronda no quita vida.", "");
        Prize("Prize_Shield", PrizeType.Shield, false, "Escudo", "Absorbe 1 choque por ronda.", "");
        Prize("Prize_Spring", PrizeType.Spring, false, "Resorte", "Salto más alto y más largo.", "");
        Prize("Prize_SlowMotion", PrizeType.SlowMotion, false, "Cámara lenta", "La pista va 10% más lenta: más tiempo para esquivar.", "");

        // Envenenados: mismo nombre y descripción que el normal; la sorpresa se ve al elegirlo.
        Prize("Poison_ExtraHeart", PrizeType.ExtraHeart, true, "Corazón extra", "+1 vida (máximo 5).",
            "¡Regalo de Tito! Tienes la vida… pero la meta de la próxima ronda sube 5 estrellas.");
        Prize("Poison_Spring", PrizeType.Spring, true, "Resorte", "Salto más alto y más largo.",
            "¡Regalo de Tito! Cada salto es doble y cuesta caer donde quieres.");
        Prize("Poison_SlowMotion", PrizeType.SlowMotion, true, "Cámara lenta", "La pista va 10% más lenta: más tiempo para esquivar.",
            "¡Regalo de Tito! La cámara lenta era para tus pies: cambiar de carril ahora es lento.");
        Prize("Poison_Shield", PrizeType.Shield, true, "Escudo", "Absorbe 1 choque por ronda.",
            "¡Regalo de Tito! El escudo solo dura los primeros 10 segundos de cada ronda.");

        Rule("Rule_DoubleValue", RuleType.DoubleValue, "¡Todo vale doble!",
            "Cada estrella vale el doble… pero la pista va 30% más rápido.");
        Rule("Rule_Blackout", RuleType.Blackout, "¡Apagón!",
            "Se apagan las luces: solo ves lo que ilumina tu reflector.");
        Rule("Rule_Mirror", RuleType.Mirror, "¡Espejo!",
            "Izquierda es derecha y derecha es izquierda.");
        Rule("Rule_Giant", RuleType.Giant, "¡Gigante!",
            "Eres el doble de grande… y no cabes bajo las barras altas.");
        Rule("Rule_NoJump", RuleType.NoJump, "¡Sin salto!",
            "Hoy nadie salta. Ni lo intentes.");

        AssetDatabase.SaveAssets();
        Debug.Log("Datos listos en " + ProjectPaths.Data);
    }

    /// <summary>
    /// Balance del domingo: la meta empieza en 20 y sube 8 por ronda (antes 10 y 5: sobraban estrellas)
    /// y la Cámara lenta explica para qué sirve. Cambia los assets que ya existen.
    /// </summary>
    [MenuItem("Una Ronda Más/Datos/Aplicar balance del domingo")]
    public static void ApplySundayBalance()
    {
        var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        if (balance != null)
        {
            var so = new SerializedObject(balance);
            so.FindProperty("baseGoal").intValue = SundayBaseGoal;
            so.FindProperty("goalPerRound").intValue = SundayGoalPerRound;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (PrizeData prize in ProjectPaths.LoadAll<PrizeData>(ProjectPaths.Prizes))
        {
            if (prize.Type != PrizeType.SlowMotion) continue;
            var so = new SerializedObject(prize);
            so.FindProperty("description").stringValue = SlowMotionText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
        BalanceReport.Generate();
        Debug.Log($"Balance del domingo aplicado: meta {SundayBaseGoal} + {SundayGoalPerRound} por ronda.");
    }

    private const int SundayBaseGoal = 20;
    private const int SundayGoalPerRound = 8;
    private const string SlowMotionText = "La pista va 10% más lenta: más tiempo para esquivar.";

    private static void Prize(string file, PrizeType type, bool poisoned, string name, string description, string reveal)
    {
        CreateIfMissing<PrizeData>($"{ProjectPaths.Prizes}/{file}.asset",
            p => p.EditorSetup(type, poisoned, name, description, reveal));
    }

    private static void Rule(string file, RuleType type, string name, string description)
    {
        CreateIfMissing<RuleData>($"{ProjectPaths.Rules}/{file}.asset", r => r.EditorSetup(type, name, description));
    }

    private static T CreateIfMissing<T>(string path, System.Action<T> setup) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        T asset = ScriptableObject.CreateInstance<T>();
        setup(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
