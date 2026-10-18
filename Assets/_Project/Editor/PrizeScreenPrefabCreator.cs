using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Crea el prefab de selección de premios con tres tarjetas interactivas.</summary>
public static class PrizeScreenPrefabCreator
{
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/PrizeScreen_Canvas.prefab";

    [MenuItem("Una Ronda Más/UI/Crear prefab pantalla de Premios")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.Log($"El prefab ya existe: {PrefabPath}");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        GameObject buttonTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (font == null || buttonTemplate == null)
        {
            Debug.LogError("Falta Font_UI_Dynamic o Button_Base. Créelos primero desde Una Ronda Más → UI.");
            return;
        }

        var root = new GameObject("PrizeScreen_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PrizeScreen));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = CreateImage(root.transform, "Overlay", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
        overlay.rectTransform.offsetMin = Vector2.zero;
        overlay.rectTransform.offsetMax = Vector2.zero;

        Image panel = CreateImage(root.transform, "PrizePanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1180f, 660f), new Color32(36, 25, 63, 250));

        CreateLabel(panel.transform, "Title", font, "¡ELIGE TU PREMIO!",
            new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1080f, 76f),
            48f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));
        TMP_Text picksText = CreateLabel(panel.transform, "PicksRemaining",
            font, "Elige tu premio", new Vector2(0.5f, 1f),
            new Vector2(0f, -106f), new Vector2(1000f, 52f),
            30f, TextAlignmentOptions.Center, Color.white);

        var buttons = new Button[3];
        var names = new TMP_Text[3];
        var descriptions = new TMP_Text[3];
        var icons = new Image[3];
        var indicators = new GameObject[3];
        float[] cardCenters = { -390f, 0f, 390f };

        for (int i = 0; i < 3; i++)
        {
            Image card = CreateImage(panel.transform, $"Card_{i + 1}",
                new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f),
                new Vector2(cardCenters[i], 0f), new Vector2(350f, 360f),
                new Color32(49, 38, 78, 255));

            TMP_Text number = CreateLabel(card.transform, "Number", font, $"{i + 1}",
                new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(300f, 44f),
                24f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));

            names[i] = CreateLabel(card.transform, "PrizeName", font, "Premio",
                new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(310f, 70f),
                32f, TextAlignmentOptions.Center, Color.white);

            descriptions[i] = CreateLabel(card.transform, "Description", font, string.Empty,
                new Vector2(0.5f, 0.49f), Vector2.zero, new Vector2(310f, 116f),
                23f, TextAlignmentOptions.Center, Color.white);
            descriptions[i].enableWordWrapping = true;

            TMP_Text poisonHint = CreateLabel(card.transform, "PoisonHint", font,
                "✦ REGALO SOSPECHOSO ✦", new Vector2(0.5f, 0.22f),
                Vector2.zero, new Vector2(320f, 48f), 20f, TextAlignmentOptions.Center,
                new Color32(247, 177, 29, 255));
            indicators[i] = poisonHint.gameObject;

            GameObject buttonObject = CreateButton(buttonTemplate, card.transform,
                $"ChooseButton_{i + 1}", "Elegir", new Vector2(0f, 42f),
                new Vector2(270f, 60f));
            buttons[i] = buttonObject.GetComponent<Button>();
        }

        TMP_Text reveal = CreateLabel(panel.transform, "PoisonReveal", font,
            string.Empty, new Vector2(0.5f, 0.08f), Vector2.zero,
            new Vector2(1080f, 70f), 24f, TextAlignmentOptions.Center,
            new Color32(255, 190, 130, 255));
        reveal.enableWordWrapping = true;
        reveal.gameObject.SetActive(false);

        PrizeScreen screen = root.GetComponent<PrizeScreen>();
        SerializedObject serializedScreen = new SerializedObject(screen);
        SetReference(serializedScreen, "panel", panel.gameObject);
        SetReference(serializedScreen, "picksRemainingText", picksText);
        SetReference(serializedScreen, "revealText", reveal);
        SetArray(serializedScreen, "prizeButtons", buttons);
        SetArray(serializedScreen, "prizeNames", names);
        SetArray(serializedScreen, "prizeDescriptions", descriptions);
        SetArray(serializedScreen, "prizeIcons", icons);
        SetArray(serializedScreen, "poisonIndicators", indicators);
        serializedScreen.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null) Debug.LogError($"No se pudo crear el prefab: {PrefabPath}");
        else Debug.Log($"Prefab de selección de premios creado: {PrefabPath}");
    }

    private static Image CreateImage(Transform parent, string objectName, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
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
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = initialText;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateButton(GameObject template, Transform parent,
        string objectName, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = Object.Instantiate(template, parent);
        buttonObject.name = objectName;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text buttonLabel = buttonObject.GetComponentInChildren<TMP_Text>();
        if (buttonLabel == null)
            Debug.LogError($"Button_Base no tiene un texto TMP: {objectName}");
        else
        {
            buttonLabel.text = label;
            buttonLabel.enableAutoSizing = true;
        }
        return buttonObject;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No existe el campo «{propertyName}» en PrizeScreen.");
            return;
        }
        property.objectReferenceValue = value;
    }

    private static void SetArray<T>(SerializedObject serializedObject, string propertyName, T[] values)
        where T : Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No existe la lista «{propertyName}» en PrizeScreen.");
            return;
        }
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
