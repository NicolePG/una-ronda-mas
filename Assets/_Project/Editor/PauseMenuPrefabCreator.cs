using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Crea el prefab de pausa con controles conectados al GameManager.</summary>
public static class PauseMenuPrefabCreator
{
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/PauseMenu_Canvas.prefab";

    [MenuItem("Una Ronda Más/UI/Crear prefab menú de pausa")]
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

        var root = new GameObject("PauseMenu_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PauseMenu));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = CreateImage(root.transform, "Overlay", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
        overlay.rectTransform.offsetMin = Vector2.zero;
        overlay.rectTransform.offsetMax = Vector2.zero;

        Image panel = CreateImage(root.transform, "PausePanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(780f, 570f), new Color32(36, 25, 63, 250));

        CreateLabel(panel.transform, "Title", font, "PAUSA",
            new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(700f, 90f),
            58f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));

        GameObject resumeObject = CreateButton(buttonTemplate, panel.transform,
            "ResumeButton", "Continuar", new Vector2(0f, -190f), new Vector2(500f, 78f));
        GameObject restartObject = CreateButton(buttonTemplate, panel.transform,
            "RestartButton", "Reiniciar", new Vector2(0f, -292f), new Vector2(500f, 78f));
        GameObject menuObject = CreateButton(buttonTemplate, panel.transform,
            "MenuButton", "Menú principal", new Vector2(0f, -394f), new Vector2(500f, 78f));

        PauseMenu pauseMenu = root.GetComponent<PauseMenu>();
        SerializedObject serializedPause = new SerializedObject(pauseMenu);
        SetReference(serializedPause, "panel", panel.gameObject);
        SetReference(serializedPause, "resumeButton", resumeObject.GetComponent<Button>());
        SetReference(serializedPause, "restartButton", restartObject.GetComponent<Button>());
        SetReference(serializedPause, "menuButton", menuObject.GetComponent<Button>());
        serializedPause.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null) Debug.LogError($"No se pudo crear el prefab: {PrefabPath}");
        else Debug.Log($"Prefab del menú de pausa creado: {PrefabPath}");
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
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
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
            Debug.LogError($"No existe el campo «{propertyName}» en PauseMenu.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
