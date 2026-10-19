using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Crea el prefab de la pantalla Eliminado con sus controles conectados.</summary>
public static class GameOverPrefabCreator
{
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/GameOver_Canvas.prefab";

    [MenuItem("Una Ronda Más/UI/Crear prefab pantalla Eliminado")]
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
            Debug.LogError("Falta la fuente dinámica o Button_Base. Ejecuta primero «Una Ronda Más/UI/Crear fuente y botón base».");
            return;
        }

        var root = new GameObject("GameOver_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameOverScreen));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = CreateImage(root.transform, "Overlay", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1280f, 720f), new Color(0f, 0f, 0f, 0.76f));
        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image card = CreateImage(root.transform, "Card", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 600f), new Color32(36, 25, 63, 250));

        TMP_Text title = CreateLabel(card.transform, "Title", font, "ELIMINADO",
            new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(820f, 90f),
            58f, TextAlignmentOptions.Top, new Color32(247, 177, 29, 255));
        TMP_Text stats = CreateLabel(card.transform, "Stats", font, string.Empty,
            new Vector2(0.5f, 0.61f), Vector2.zero, new Vector2(800f, 250f),
            32f, TextAlignmentOptions.Top, Color.white);
        stats.alignment = TextAlignmentOptions.Center;

        GameObject restartObject = CreateButton(buttonTemplate, card.transform,
            "RestartButton", "¿Una ronda más? (Enter)", new Vector2(-180f, 80f), new Vector2(420f, 90f));
        GameObject menuObject = CreateButton(buttonTemplate, card.transform,
            "MenuButton", "Menú (M)", new Vector2(250f, 80f), new Vector2(240f, 90f));

        GameOverScreen screen = root.GetComponent<GameOverScreen>();
        SerializedObject serializedScreen = new SerializedObject(screen);
        SetReference(serializedScreen, "panel", card.gameObject);
        SetReference(serializedScreen, "statsText", stats);
        SetReference(serializedScreen, "restartButton", restartObject.GetComponent<Button>());
        SetReference(serializedScreen, "menuButton", menuObject.GetComponent<Button>());
        serializedScreen.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null) Debug.LogError($"No se pudo crear el prefab: {PrefabPath}");
        else Debug.Log($"Prefab de pantalla Eliminado creado: {PrefabPath}");
    }

    private static Image CreateImage(Transform parent, string objectName, Vector2 anchor,
        Vector2 position, Vector2 size, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
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
        text.fontSizeMin = 20f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateButton(GameObject template, Transform parent, string objectName,
        string label, Vector2 position, Vector2 size)
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
            Debug.LogError($"El prefab Button_Base no tiene un texto TMP: {objectName}");
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
            Debug.LogError($"No existe el campo «{propertyName}» en {serializedObject.targetObject.GetType().Name}.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
