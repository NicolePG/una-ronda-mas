using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea Balance.asset, los 21 premios (17 normales y 4 envenenados) y las 5 reglas. No pisa los datos que ya existen, salvo dos cosas que manda
/// el código: la rareza, el nivel máximo y el texto de los premios, y el balance cuando su versión es más vieja
/// que BalanceConfig.CurrentDataVersion (vuelve a los valores por defecto nuevos).
/// </summary>
public static class DataAssetCreator
{
    public static void CreateAll()
    {
        ProjectPaths.EnsureFolder(ProjectPaths.Prizes);
        ProjectPaths.EnsureFolder(ProjectPaths.Rules);

        UpgradeBalance(CreateIfMissing<BalanceConfig>(ProjectPaths.Balance, _ => { }));

        // Cada premio se elige una sola vez por partida (nivel máximo 1); el corazón se repite.
        // Comunes (desde la ronda 1)
        Prize("Prize_ExtraHeart", PrizeType.ExtraHeart, false, PrizeRarity.Common, 9, "Corazón extra", "+1 vida (máximo 5).", "");
        Prize("Prize_Shield", PrizeType.Shield, false, PrizeRarity.Common, 1, "Escudo", "Absorbe 1 choque por ronda.", "");
        Prize("Prize_Float", PrizeType.Float, false, PrizeRarity.Common, 1, "Flotador", "La primera caída al agua de cada ronda no quita vida.", "");
        Prize("Prize_Tip", PrizeType.Tip, false, PrizeRarity.Common, 1, "Propina", "Empiezas cada ronda con 5 estrellas.", "");
        Prize("Prize_Sneakers", PrizeType.Sneakers, false, PrizeRarity.Common, 1, "Zapatillas", "Cambias de carril 30% más rápido.", "");
        // Raros (desde la ronda 2)
        Prize("Prize_Magnet", PrizeType.Magnet, false, PrizeRarity.Rare, 1, "Imán", "Atrae las estrellas de los carriles vecinos.", "");
        Prize("Prize_Spring", PrizeType.Spring, false, PrizeRarity.Rare, 1, "Resorte", "Salto más alto y más largo.", "");
        Prize("Prize_SlowMotion", PrizeType.SlowMotion, false, PrizeRarity.Rare, 1, "Cámara lenta",
            "La pista va 10% más lenta: más tiempo para esquivar.", "");
        Prize("Prize_Magnifier", PrizeType.Magnifier, false, PrizeRarity.Rare, 1, "Lupa",
            "Te dice qué trampa hizo Tito, cuándo es un farol y si la regla es falsa.", "");
        Prize("Prize_Whistle", PrizeType.Whistle, false, PrizeRarity.Rare, 1, "Silbato",
            "La primera falsa alarma de cada ronda no te quita vida.", "");
        Prize("Prize_Safe", PrizeType.Safe, false, PrizeRarity.Rare, 1, "Caja fuerte",
            "Tito solo puede robarte (o inflarte la meta) la mitad.", "");
        Prize("Prize_BetInsurance", PrizeType.BetInsurance, false, PrizeRarity.Rare, 1, "Seguro de apuesta",
            "Si fallas la apuesta pero llegas a la meta normal, igual pasas.", "");
        // Épicos (desde la ronda 5)
        Prize("Prize_Clover", PrizeType.Clover, false, PrizeRarity.Epic, 1, "Trébol", "Cada estrella dorada vale 3 más.", "");
        Prize("Prize_SuperMagnet", PrizeType.SuperMagnet, false, PrizeRarity.Epic, 1, "Súper imán",
            "Tu imán atrae de los 3 carriles y desde más lejos.", "");
        Prize("Prize_SecondChance", PrizeType.SecondChance, false, PrizeRarity.Epic, 1, "Segunda oportunidad",
            "Una vez por partida, al perder la última vida vuelves con 1.", "");
        Prize("Prize_DetectiveBadge", PrizeType.DetectiveBadge, false, PrizeRarity.Epic, 1, "Placa de detective",
            "Pillar a Tito te da el doble de estrellas.", "");
        Prize("Prize_RiggedContract", PrizeType.RiggedContract, false, PrizeRarity.Epic, 1, "Contrato amañado",
            "La meta de cada ronda baja 10%.", "");

        // Envenenados: mismo nombre y descripción que el normal; la sorpresa se ve al elegirlo.
        Prize("Poison_ExtraHeart", PrizeType.ExtraHeart, true, PrizeRarity.Common, 9, "Corazón extra", "+1 vida (máximo 5).",
            "¡Regalo de Tito! Tienes la vida… pero la meta de la próxima ronda sube 5 estrellas.");
        Prize("Poison_Spring", PrizeType.Spring, true, PrizeRarity.Rare, 1, "Resorte", "Salto más alto y más largo.",
            "¡Regalo de Tito! Cada salto es doble y cuesta caer donde quieres.");
        Prize("Poison_SlowMotion", PrizeType.SlowMotion, true, PrizeRarity.Rare, 1, "Cámara lenta",
            "La pista va 10% más lenta: más tiempo para esquivar.",
            "¡Regalo de Tito! La cámara lenta era para tus pies: cambiar de carril ahora es lento.");
        Prize("Poison_Shield", PrizeType.Shield, true, PrizeRarity.Common, 1, "Escudo", "Absorbe 1 choque por ronda.",
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

    // Si el asset es de una versión vieja, todos sus números vuelven a los valores por defecto del código.
    private static void UpgradeBalance(BalanceConfig balance)
    {
        if (balance == null || balance.DataVersion >= BalanceConfig.CurrentDataVersion) return;

        BalanceConfig defaults = ScriptableObject.CreateInstance<BalanceConfig>();
        var target = new SerializedObject(balance);
        SerializedProperty property = new SerializedObject(defaults).GetIterator();
        for (bool enter = true; property.NextVisible(enter); enter = false)
        {
            if (property.name != "m_Script") target.CopyFromSerializedProperty(property);
        }
        target.FindProperty("dataVersion").intValue = BalanceConfig.CurrentDataVersion;
        target.ApplyModifiedPropertiesWithoutUndo();
        Object.DestroyImmediate(defaults);
        EditorUtility.SetDirty(balance);
        Debug.Log($"Balance.asset actualizado a la versión {BalanceConfig.CurrentDataVersion}: más dificultad, trampas de Tito desde la ronda 4 y zonas oscuras.");
    }

    private static void Prize(string file, PrizeType type, bool poisoned, PrizeRarity rarity, int maxLevel,
        string name, string description, string reveal)
    {
        PrizeData prize = CreateIfMissing<PrizeData>($"{ProjectPaths.Prizes}/{file}.asset",
            p => p.EditorSetup(type, poisoned, name, description, reveal));
        var so = new SerializedObject(prize);
        so.FindProperty("rarity").enumValueIndex = (int)rarity;
        so.FindProperty("maxLevel").intValue = maxLevel;
        so.FindProperty("description").stringValue = description;
        so.ApplyModifiedPropertiesWithoutUndo();
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
