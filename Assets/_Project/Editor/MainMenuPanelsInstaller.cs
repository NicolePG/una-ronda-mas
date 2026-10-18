using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Añade Opciones y Créditos al menú sin reconstruir la escena existente.</summary>
public static class MainMenuPanelsInstaller
{
    private const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";

    [MenuItem("Una Ronda Más/UI/Agregar Opciones y Créditos a MainMenu")]
    public static void Install()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);

        MainMenuUI menu = Object.FindObjectOfType<MainMenuUI>();
        if (menu == null)
        {
            Debug.LogError("No se encontró MainMenuUI en MainMenu.unity.");
            return;
        }

        if (menu.transform.Find("OptionsPanel") != null || menu.transform.Find("CreditsPanel") != null)
        {
            Debug.LogWarning("MainMenu ya contiene paneles de Opciones o Créditos; no se modificó para evitar duplicados.");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (font == null || buttonPrefab == null)
        {
            Debug.LogError("Falta Font_UI_Dynamic o Button_Base. Créelos primero desde Una Ronda Más → UI.");
            return;
        }

        Transform menuPanel = menu.transform.Find("MenuPanel");
        Transform howToPlayButtonTransform = menuPanel != null ? menuPanel.Find("HowToPlayButton") : null;
        Button howToPlayButton = howToPlayButtonTransform != null
            ? howToPlayButtonTransform.GetComponent<Button>()
            : null;
        Button playButton = menuPanel != null && menuPanel.Find("PlayButton") != null
            ? menuPanel.Find("PlayButton").GetComponent<Button>()
            : null;
        if (menuPanel == null || howToPlayButton == null || playButton == null)
        {
            Debug.LogError("Primero agrega Cómo jugar a MainMenu y verifica que exista PlayButton.");
            return;
        }

        SetButtonPosition(playButton, 0.46f);
        SetButtonPosition(howToPlayButton, 0.32f);

        Button optionsButton = CreateMenuButton(buttonPrefab, menuPanel, "OptionsButton",
            "Opciones", 0.18f, new Vector2(440f, 68f));
        Button creditsButton = CreateMenuButton(buttonPrefab, menuPanel, "CreditsButton",
            "Créditos", 0.06f, new Vector2(440f, 68f));
        if (optionsButton == null || creditsButton == null) return;

        GameObject optionsPanel = CreateOptionsPanel(menu.transform, font, buttonPrefab);
        GameObject creditsPanel = CreateCreditsPanel(menu.transform, font, buttonPrefab);

        SerializedObject serializedMenu = new SerializedObject(menu);
        SetReference(serializedMenu, "optionsButton", optionsButton);
        SetReference(serializedMenu, "optionsPanel", optionsPanel);
        SetReference(serializedMenu, "creditsButton", creditsButton);
        SetReference(serializedMenu, "creditsPanel", creditsPanel);
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Opciones y Créditos agregados a MainMenu.unity.");
    }

    private static Button CreateMenuButton(GameObject buttonPrefab, Transform parent,
        string objectName, string label, float anchorY, Vector2 size)
    {
        GameObject buttonObject = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent);
        buttonObject.name = objectName;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, anchorY);
        rect.anchorMax = new Vector2(0.5f, anchorY);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        TMP_Text text = buttonObject.GetComponentInChildren<TMP_Text>();
        if (text == null)
        {
            Debug.LogError($"Button_Base no tiene texto TMP: {objectName}");
            Object.DestroyImmediate(buttonObject);
            return null;
        }
        text.text = label;
        text.fontSize = 30f;
        return buttonObject.GetComponent<Button>();
    }

    private static void SetButtonPosition(Button button, float anchorY)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, anchorY);
        rect.anchorMax = new Vector2(0.5f, anchorY);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500f, 68f);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null) text.fontSize = 34f;
    }

    private static GameObject CreateOptionsPanel(Transform parent, TMP_FontAsset font, GameObject buttonPrefab)
    {
        var root = new GameObject("OptionsPanel", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(OptionsUI));
        root.transform.SetParent(parent, false);
        SetupFullscreenOverlay(root, new Color(0f, 0f, 0f, 0.9f));

        Image card = CreateImage(root.transform, "Card", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 600f),
            new Color32(36, 25, 63, 255));
        CreateLabel(card.transform, "Title", font, "OPCIONES",
            new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(820f, 76f),
            48f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));

        CreateLabel(card.transform, "MusicLabel", font, "Volumen de música",
            new Vector2(0.5f, 0.74f), new Vector2(-170f, 0f), new Vector2(420f, 54f),
            28f, TextAlignmentOptions.Left, Color.white);
        Slider musicSlider = CreateSlider(card.transform, "MusicSlider",
            new Vector2(0.5f, 0.66f), new Vector2(0f, 0f), new Vector2(600f, 28f));
        TMP_Text musicValue = CreateLabel(card.transform, "MusicValue", font, "80%",
            new Vector2(0.5f, 0.66f), new Vector2(350f, 0f), new Vector2(100f, 40f),
            24f, TextAlignmentOptions.Right, Color.white);

        CreateLabel(card.transform, "SfxLabel", font, "Volumen de efectos",
            new Vector2(0.5f, 0.52f), new Vector2(-170f, 0f), new Vector2(420f, 54f),
            28f, TextAlignmentOptions.Left, Color.white);
        Slider sfxSlider = CreateSlider(card.transform, "SfxSlider",
            new Vector2(0.5f, 0.44f), new Vector2(0f, 0f), new Vector2(600f, 28f));
        TMP_Text sfxValue = CreateLabel(card.transform, "SfxValue", font, "80%",
            new Vector2(0.5f, 0.44f), new Vector2(350f, 0f), new Vector2(100f, 40f),
            24f, TextAlignmentOptions.Right, Color.white);

        Toggle fullscreen = CreateFullscreenToggle(card.transform, font);
        Button closeButton = CreateMenuButton(buttonPrefab, card.transform, "CloseButton",
            "Volver", 0.1f, new Vector2(360f, 72f));

        OptionsUI options = root.GetComponent<OptionsUI>();
        SerializedObject serializedOptions = new SerializedObject(options);
        SetReference(serializedOptions, "musicSlider", musicSlider);
        SetReference(serializedOptions, "sfxSlider", sfxSlider);
        SetReference(serializedOptions, "fullscreenToggle", fullscreen);
        SetReference(serializedOptions, "musicValueText", musicValue);
        SetReference(serializedOptions, "sfxValueText", sfxValue);
        SetReference(serializedOptions, "closeButton", closeButton);
        serializedOptions.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        return root;
    }

    private static GameObject CreateCreditsPanel(Transform parent, TMP_FontAsset font, GameObject buttonPrefab)
    {
        var root = new GameObject("CreditsPanel", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(CreditsUI));
        root.transform.SetParent(parent, false);
        SetupFullscreenOverlay(root, new Color(0f, 0f, 0f, 0.9f));

        Image card = CreateImage(root.transform, "Card", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 600f),
            new Color32(36, 25, 63, 255));
        CreateLabel(card.transform, "Title", font, "CRÉDITOS",
            new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(840f, 74f),
            48f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));

        TMP_Text creditsText = CreateLabel(card.transform, "CreditsText", font, string.Empty,
            new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(820f, 350f),
            25f, TextAlignmentOptions.Center, Color.white);
        creditsText.enableWordWrapping = true;

        Button closeButton = CreateMenuButton(buttonPrefab, card.transform, "CloseButton",
            "Volver", 0.09f, new Vector2(360f, 72f));

        SerializedObject serializedCredits = new SerializedObject(root.GetComponent<CreditsUI>());
        SetReference(serializedCredits, "closeButton", closeButton);
        SetReference(serializedCredits, "creditsText", creditsText);
        serializedCredits.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        return root;
    }

    private static Slider CreateSlider(Transform parent, string objectName, Vector2 anchor,
        Vector2 position, Vector2 size)
    {
        var sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = anchor;
        sliderRect.anchorMax = anchor;
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = position;
        sliderRect.sizeDelta = size;

        Image background = CreateImage(sliderObject.transform, "Background",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color32(74, 65, 95, 255));
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        Image fill = CreateImage(sliderObject.transform, "Fill",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color32(247, 177, 29, 255));
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        handleObject.transform.SetParent(sliderObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 42f);
        handleObject.GetComponent<Image>().color = Color.white;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = SaveManager.DefaultVolume;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleObject.GetComponent<Image>();
        return slider;
    }

    private static Toggle CreateFullscreenToggle(Transform parent, TMP_FontAsset font)
    {
        var toggleObject = new GameObject("FullscreenToggle", typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0.29f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.29f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = Vector2.zero;
        toggleRect.sizeDelta = new Vector2(480f, 58f);

        Image box = CreateImage(toggleObject.transform, "Box",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16f, 0f), new Vector2(34f, 34f),
            new Color32(74, 65, 95, 255));
        Image check = CreateImage(box.transform, "Check",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color32(247, 177, 29, 255));
        check.rectTransform.offsetMin = new Vector2(5f, 5f);
        check.rectTransform.offsetMax = new Vector2(-5f, -5f);

        CreateLabel(toggleObject.transform, "Label", font, "Pantalla completa",
            new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(400f, 52f),
            26f, TextAlignmentOptions.Left, Color.white);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = Screen.fullScreen;
        return toggle;
    }

    private static void SetupFullscreenOverlay(GameObject root, Color color)
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = color;
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
            Debug.LogError($"No existe el campo «{propertyName}» en {serializedObject.targetObject.GetType().Name}.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
