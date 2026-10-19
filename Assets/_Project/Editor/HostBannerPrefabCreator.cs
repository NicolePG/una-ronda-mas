using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Crea el prefab del anuncio de Tito con controles de apuesta conectados.</summary>
public static class HostBannerPrefabCreator
{
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/HostBanner_Canvas.prefab";

    [MenuItem("Una Ronda Más/UI/Crear prefab anuncio de Tito")]
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

        var root = new GameObject("HostBanner_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HostBanner));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = CreateImage(root.transform, "Overlay", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
        overlay.rectTransform.offsetMin = Vector2.zero;
        overlay.rectTransform.offsetMax = Vector2.zero;

        Image card = CreateImage(root.transform, "AnnouncementPanel",
            new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f),
            Vector2.zero, new Vector2(980f, 500f), new Color32(36, 25, 63, 250));

        TMP_Text title = CreateLabel(card.transform, "RoundTitle", font, "¡RONDA 1!  META: 10",
            new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(900f, 82f),
            46f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));
        TMP_Text rule = CreateLabel(card.transform, "Rule", font, "Sin regla especial esta ronda.",
            new Vector2(0.5f, 0.67f), Vector2.zero, new Vector2(860f, 150f),
            34f, TextAlignmentOptions.Center, Color.white);
        rule.enableWordWrapping = true;
        TMP_Text prompt = CreateLabel(card.transform, "BetPrompt", font, "¡Prepárate para correr!",
            new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(860f, 80f),
            28f, TextAlignmentOptions.Center, Color.white);

        GameObject acceptObject = CreateButton(buttonTemplate, card.transform, "AcceptBetButton",
            "¡Acepto! (1)", new Vector2(-220f, 72f), new Vector2(380f, 82f));
        GameObject declineObject = CreateButton(buttonTemplate, card.transform, "DeclineBetButton",
            "No, gracias (2)", new Vector2(220f, 72f), new Vector2(380f, 82f));

        HostBanner banner = root.GetComponent<HostBanner>();
        SerializedObject serializedBanner = new SerializedObject(banner);
        SetReference(serializedBanner, "panel", card.gameObject);
        SetReference(serializedBanner, "titleText", title);
        SetReference(serializedBanner, "ruleText", rule);
        SetReference(serializedBanner, "promptText", prompt);
        SetReference(serializedBanner, "acceptBetButton", acceptObject.GetComponent<Button>());
        SetReference(serializedBanner, "acceptBetText", acceptObject.GetComponentInChildren<TMP_Text>());
        SetReference(serializedBanner, "declineBetButton", declineObject.GetComponent<Button>());
        SetReference(serializedBanner, "declineBetText", declineObject.GetComponentInChildren<TMP_Text>());
        serializedBanner.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null) Debug.LogError($"No se pudo crear el prefab: {PrefabPath}");
        else Debug.Log($"Prefab del anuncio de Tito creado: {PrefabPath}");
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
        {
            Debug.LogError($"Button_Base no tiene un texto TMP: {objectName}");
            return buttonObject;
        }

        buttonLabel.text = label;
        buttonLabel.enableAutoSizing = true;
        return buttonObject;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No existe el campo «{propertyName}» en HostBanner.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
