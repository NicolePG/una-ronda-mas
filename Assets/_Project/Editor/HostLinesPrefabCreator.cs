using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Crea el globo de diálogo que muestra frases de Tito durante la partida.</summary>
public static class HostLinesPrefabCreator
{
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string CsvPath = "Assets/_Project/Resources/HostLines.csv";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/HostLines_Canvas.prefab";

    [MenuItem("Una Ronda Más/UI/Crear prefab frases de Tito")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.Log($"El prefab ya existe: {PrefabPath}");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
        if (font == null || csv == null)
        {
            Debug.LogError("Falta Font_UI_Dynamic o Resources/HostLines.csv. Revisa que ambos existan en el proyecto.");
            return;
        }

        var root = new GameObject("HostLines_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HostLines));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bubbleObject = new GameObject("SpeechBubble",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bubbleObject.transform.SetParent(root.transform, false);

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.5f, 0.14f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.14f);
        bubbleRect.pivot = new Vector2(0.5f, 0.5f);
        bubbleRect.anchoredPosition = Vector2.zero;
        bubbleRect.sizeDelta = new Vector2(900f, 92f);

        Image bubble = bubbleObject.GetComponent<Image>();
        bubble.color = new Color32(36, 25, 63, 235);

        var textObject = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(bubbleObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 8f);
        textRect.offsetMax = new Vector2(-24f, -8f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = string.Empty;
        text.fontSize = 28f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        SerializedObject serializedLines = new SerializedObject(root.GetComponent<HostLines>());
        SerializedProperty bubbleProperty = serializedLines.FindProperty("speechBubble");
        SerializedProperty textProperty = serializedLines.FindProperty("lineText");
        if (bubbleProperty == null || textProperty == null)
        {
            Debug.LogError("No existe el campo del globo o el texto en HostLines.");
            Object.DestroyImmediate(root);
            return;
        }
        bubbleProperty.objectReferenceValue = bubbleObject;
        textProperty.objectReferenceValue = text;
        serializedLines.ApplyModifiedPropertiesWithoutUndo();

        bubbleObject.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null) Debug.LogError($"No se pudo crear el prefab: {PrefabPath}");
        else Debug.Log($"Prefab de frases de Tito creado: {PrefabPath}");
    }
}
