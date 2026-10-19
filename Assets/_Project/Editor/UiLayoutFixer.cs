using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arreglos de la UI sin regenerar los prefabs (se editan en su lugar para no romper referencias de Game.unity):
/// botones de la apuesta y de Eliminado dentro de su panel, y la regla del HUD sin tapar la pantalla de Tito.
/// </summary>
public static class UiLayoutFixer
{
    private const string BannerPath = "Assets/_Project/Prefabs/UI/HostBanner_Canvas.prefab";
    private const string GameOverPath = "Assets/_Project/Prefabs/UI/GameOver_Canvas.prefab";
    private const string HudPath = "Assets/_Project/Prefabs/UI/HUD_Canvas.prefab";
    private const string PrizePath = "Assets/_Project/Prefabs/UI/PrizeScreen_Canvas.prefab";
    private const float IconSize = 96f;

    [MenuItem("Una Ronda Más/UI/Arreglar botones y HUD (sin regenerar)")]
    public static void FixAll()
    {
        Edit(BannerPath, root =>
        {
            Place(root, "AcceptBetButton", new Vector2(-220f, 72f), new Vector2(380f, 82f), "¡Acepto! (1)");
            Place(root, "DeclineBetButton", new Vector2(220f, 72f), new Vector2(380f, 82f), "No, gracias (2)");
        });
        Edit(GameOverPath, root =>
        {
            Place(root, "RestartButton", new Vector2(-180f, 80f), new Vector2(420f, 90f), "¿Una ronda más? (Enter)");
            Place(root, "MenuButton", new Vector2(250f, 80f), new Vector2(240f, 90f), "Menú (M)");
        });
        Edit(HudPath, root =>
        {
            RectTransform rule = Find(root, "Rule");
            if (rule == null) return;
            var anchor = new Vector2(0.03f, 0.81f);
            rule.anchorMin = anchor;
            rule.anchorMax = anchor;
            rule.pivot = anchor;
            rule.anchoredPosition = new Vector2(0f, -12f);
            rule.sizeDelta = new Vector2(520f, 48f);
            var text = rule.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.fontSize = 26f;
                text.alignment = TextAlignmentOptions.TopLeft;
            }
        });
        Edit(PrizePath, FixPrizeCards);
        AssetDatabase.SaveAssets();
        Debug.Log("UI arreglada: botones de apuesta y Eliminado dentro del panel; regla del HUD a la izquierda.");
    }

    /// <summary>Para la terminal: arreglos de UI, íconos de premios y audio optimizado.</summary>
    public static void ApplyAllFromCommandLine()
    {
        FixAll();
        PrizeIconCreator.CreateAll();
        AudioImportOptimizer.Optimize();
        AssetDatabase.SaveAssets();
    }

    // Tarjeta de 350 × 360: número arriba a la izquierda, ícono, nombre, descripción, pista y botón.
    private static void FixPrizeCards(GameObject root)
    {
        var icons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            RectTransform card = Find(root, $"Card_{i + 1}");
            if (card == null)
            {
                Debug.LogError($"No encontré Card_{i + 1} en la pantalla de premios.");
                continue;
            }
            Vector2 cardPosition = card.anchoredPosition;
            card.anchorMin = card.anchorMax = new Vector2(card.anchorMin.x, 0.5f);
            card.anchoredPosition = cardPosition;

            Transform existing = card.Find("Icon");
            Image icon = existing != null ? existing.GetComponent<Image>() : null;
            if (icon == null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(card, false);
                icon = go.GetComponent<Image>();
            }
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Layout(icon.rectTransform, new Vector2(0.5f, 0.778f), new Vector2(IconSize, IconSize));
            icons[i] = icon;

            Layout(Find(card.gameObject, "Number"), new Vector2(0.09f, 0.93f), new Vector2(40f, 40f));
            Layout(Find(card.gameObject, "PrizeName"), new Vector2(0.5f, 0.572f), new Vector2(310f, 44f), 30f);
            Layout(Find(card.gameObject, "Description"), new Vector2(0.5f, 0.4f), new Vector2(310f, 80f), 21f);
            RectTransform hint = Find(card.gameObject, "PoisonHint");
            Layout(hint, new Vector2(0.5f, 0.244f), new Vector2(320f, 28f), 18f);
            if (hint != null)
            {
                var text = hint.GetComponent<TMP_Text>();
                text.text = "¿brilla raro?";
                text.color = new Color32(200, 255, 180, 200);
            }
        }

        var screen = root.GetComponentInChildren<PrizeScreen>(true);
        if (screen == null) return;
        var so = new SerializedObject(screen);
        SerializedProperty array = so.FindProperty("prizeIcons");
        array.arraySize = icons.Length;
        for (int i = 0; i < icons.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Layout(RectTransform rect, Vector2 anchor, Vector2 size, float fontSize = 0f)
    {
        if (rect == null) return;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        var text = rect.GetComponent<TMP_Text>();
        if (text != null && fontSize > 0f) text.fontSize = fontSize;
    }

    private static void Edit(string path, System.Action<GameObject> change)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            change(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Place(GameObject root, string name, Vector2 position, Vector2 size, string label)
    {
        RectTransform rect = Find(root, name);
        if (rect == null)
        {
            Debug.LogError($"No encontré «{name}» en {root.name}.");
            return;
        }
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = rect.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
    }

    private static RectTransform Find(GameObject root, string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name) return rect;
        }
        return null;
    }
}
