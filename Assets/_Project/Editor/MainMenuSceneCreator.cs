using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Crea MainMenu.unity con el botón Jugar y los récords.</summary>
public static class MainMenuSceneCreator
{
    private const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";

    [MenuItem("Una Ronda Más/UI/Crear escena MainMenu")]
    public static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (font == null || buttonPrefab == null)
        {
            Debug.LogError("Falta Font_UI_Dynamic o Button_Base. Créelos primero desde Una Ronda Más → UI.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuUI));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateImage(canvasObject.transform, "Background",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(36, 25, 63, 255));

        Image panel = CreateImage(canvasObject.transform, "MenuPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(900f, 600f), new Color32(28, 20, 52, 245));

        CreateLabel(panel.transform, "Title", font, "¡UNA RONDA MÁS!",
            new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(820f, 100f),
            64f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));

        TMP_Text records = CreateLabel(panel.transform, "Records", font,
            "RÉCORD DE RONDA: 0\nRÉCORD DE ESTRELLAS: 0",
            new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(800f, 130f),
            32f, TextAlignmentOptions.Center, Color.white);

        GameObject buttonObject = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, panel.transform);
        buttonObject.name = "PlayButton";
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.28f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.28f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(500f, 100f);
        Button button = buttonObject.GetComponent<Button>();

        TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
        if (buttonText == null)
        {
            Debug.LogError("Button_Base no contiene un componente TMP_Text.", buttonObject);
            Object.DestroyImmediate(canvasObject);
            return;
        }
        buttonText.text = "Jugar";
        buttonText.fontSize = 42f;

        TMP_Text status = CreateLabel(panel.transform, "Status", font, string.Empty,
            new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(820f, 70f),
            24f, TextAlignmentOptions.Center, new Color32(255, 190, 130, 255));
        status.enableAutoSizing = true;

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetAsLastSibling();

        SerializedObject serializedMenu = new SerializedObject(canvasObject.GetComponent<MainMenuUI>());
        SetReference(serializedMenu, "playButtonText", buttonText);
        SetReference(serializedMenu, "recordText", records);
        SetReference(serializedMenu, "statusText", status);
        SetReference(serializedMenu, "playButton", button);
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        WebGLBuildTools.UpdateBuildScenes();
        AssetDatabase.SaveAssets();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            Debug.LogError($"No se pudo guardar la escena del menú: {ScenePath}");
        else if (!System.IO.File.Exists("Assets/_Project/Scenes/Game.unity"))
            Debug.LogWarning("MainMenu está creado. Jugar se habilitará cuando Game.unity esté disponible y agregada a Build Settings.");
        else
            Debug.Log("Escena MainMenu creada y Build Settings actualizadas.");
    }

    private static Image CreateImage(Transform parent, string objectName,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
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
        text.fontSizeMin = 18f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No existe el campo «{propertyName}» en MainMenuUI.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
