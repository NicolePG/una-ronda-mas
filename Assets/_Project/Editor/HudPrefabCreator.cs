using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Crea el prefab del HUD y conecta sus textos.</summary>
public static class HudPrefabCreator
{
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/HUD_Canvas.prefab";

    [MenuItem("Una Ronda Más/UI/Crear prefab HUD")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.Log($"El prefab ya existe: {PrefabPath}");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            Debug.LogError($"No se encontró la fuente dinámica. Primero ejecuta «Crear fuente y botón base». Ruta: {FontAssetPath}");
            return;
        }

        var root = new GameObject("HUD_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HUD));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        TMP_Text round = CreateLabel(root.transform, "Round", font, "RONDA: 1",
            new Vector2(0.03f, 0.96f), new Vector2(0f, -20f), new Vector2(360f, 54f),
            34f, TextAlignmentOptions.TopLeft, Color.white);
        TMP_Text stars = CreateLabel(root.transform, "Stars", font, "ESTRELLAS: 0 / 10",
            new Vector2(0.5f, 0.96f), new Vector2(0f, -20f), new Vector2(500f, 54f),
            34f, TextAlignmentOptions.Top, new Color32(247, 177, 29, 255));
        TMP_Text time = CreateLabel(root.transform, "Time", font, "TIEMPO: 00:40",
            new Vector2(0.97f, 0.96f), new Vector2(0f, -20f), new Vector2(360f, 54f),
            34f, TextAlignmentOptions.TopRight, Color.white);
        TMP_Text lives = CreateLabel(root.transform, "Lives", font, "VIDAS: 3",
            new Vector2(0.03f, 0.88f), new Vector2(0f, -12f), new Vector2(360f, 48f),
            28f, TextAlignmentOptions.TopLeft, Color.white);
        TMP_Text rule = CreateLabel(root.transform, "Rule", font, "REGLA: —",
            new Vector2(0.03f, 0.81f), new Vector2(0f, -12f), new Vector2(520f, 48f),
            26f, TextAlignmentOptions.TopLeft, Color.white);
        TMP_Text cheat = CreateLabel(root.transform, "CheatPrompt", font, "¡TRAMPA! (T)",
            new Vector2(0.5f, 0.08f), new Vector2(0f, 0f), new Vector2(440f, 66f),
            36f, TextAlignmentOptions.Bottom, new Color32(247, 177, 29, 255));

        SerializedObject serializedHud = new SerializedObject(root.GetComponent<HUD>());
        SetTextReference(serializedHud, "roundText", round);
        SetTextReference(serializedHud, "starsText", stars);
        SetTextReference(serializedHud, "timeText", time);
        SetTextReference(serializedHud, "livesText", lives);
        SetTextReference(serializedHud, "ruleText", rule);
        SetTextReference(serializedHud, "cheatPrompt", cheat);
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        cheat.gameObject.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null) Debug.LogError($"No se pudo crear el prefab: {PrefabPath}");
        else Debug.Log($"Prefab del HUD creado: {PrefabPath}");
    }

    private static TMP_Text CreateLabel(Transform parent, string objectName, TMP_FontAsset font,
        string initialText, Vector2 anchor, Vector2 position, Vector2 size, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, anchor.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = initialText;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetTextReference(SerializedObject serializedHud, string propertyName, TMP_Text text)
    {
        SerializedProperty property = serializedHud.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No existe el campo «{propertyName}» en HUD.");
            return;
        }
        property.objectReferenceValue = text;
    }
}
