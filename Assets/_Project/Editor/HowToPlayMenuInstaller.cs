using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Añade el acceso y el panel de instrucciones a MainMenu sin reconstruir la escena.</summary>
public static class HowToPlayMenuInstaller
{
    private const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";

    [MenuItem("Una Ronda Más/UI/Agregar Cómo jugar a MainMenu")]
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

        Transform canvas = menu.transform;
        Transform existingPanel = canvas.Find("HowToPlayPanel");
        if (existingPanel != null)
        {
            Debug.LogWarning("MainMenu ya tiene HowToPlayPanel; no se duplicó la interfaz.");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (font == null || buttonPrefab == null)
        {
            Debug.LogError("Falta Font_UI_Dynamic o Button_Base. Créelos primero desde Una Ronda Más → UI.");
            return;
        }

        Transform menuPanel = canvas.Find("MenuPanel");
        Transform playButtonTransform = menuPanel != null ? menuPanel.Find("PlayButton") : null;
        Button playButton = playButtonTransform != null ? playButtonTransform.GetComponent<Button>() : null;
        if (menuPanel == null || playButton == null)
        {
            Debug.LogError("No se encontró MenuPanel o PlayButton en la escena MainMenu.", menu);
            return;
        }

        RectTransform playRect = playButton.GetComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0.38f);
        playRect.anchorMax = new Vector2(0.5f, 0.38f);

        GameObject howToPlayButtonObject = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, menuPanel);
        howToPlayButtonObject.name = "HowToPlayButton";
        RectTransform howToPlayButtonRect = howToPlayButtonObject.GetComponent<RectTransform>();
        howToPlayButtonRect.anchorMin = new Vector2(0.5f, 0.16f);
        howToPlayButtonRect.anchorMax = new Vector2(0.5f, 0.16f);
        howToPlayButtonRect.pivot = new Vector2(0.5f, 0.5f);
        howToPlayButtonRect.anchoredPosition = Vector2.zero;
        howToPlayButtonRect.sizeDelta = new Vector2(440f, 76f);

        TMP_Text howToPlayButtonText = howToPlayButtonObject.GetComponentInChildren<TMP_Text>();
        if (howToPlayButtonText == null)
        {
            Debug.LogError("Button_Base no contiene texto TMP.", howToPlayButtonObject);
            Object.DestroyImmediate(howToPlayButtonObject);
            return;
        }
        howToPlayButtonText.text = "Cómo jugar";

        GameObject panelObject = CreatePanel(canvas, font, buttonPrefab);
        Button closeButton = panelObject.transform.Find("Card/CloseButton").GetComponent<Button>();

        SerializedObject serializedMenu = new SerializedObject(menu);
        SetReference(serializedMenu, "howToPlayButton", howToPlayButtonObject.GetComponent<Button>());
        SetReference(serializedMenu, "howToPlayPanel", panelObject);
        SetReference(serializedMenu, "closeHowToPlayButton", closeButton);
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Panel Cómo jugar agregado al menú sin reemplazar la escena.");
    }

    private static GameObject CreatePanel(Transform parent, TMP_FontAsset font, GameObject buttonPrefab)
    {
        var panelObject = new GameObject("HowToPlayPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        panelObject.transform.SetAsLastSibling();

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        Image card = CreateImage(panelObject.transform, "Card",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1000f, 620f), new Color32(36, 25, 63, 255));

        CreateLabel(card.transform, "Title", font, "CÓMO JUGAR",
            new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(900f, 76f),
            48f, TextAlignmentOptions.Center, new Color32(247, 177, 29, 255));

        CreateLaneIllustration(card.transform);
        string[] instructions =
        {
            "A / D o ← / →: cambia de carril.  Espacio / W: salta.  S / ↓: deslízate.",
            "Junta estrellas y alcanza la meta antes de que termine la ronda de 40 segundos.",
            "T: acusa a Tito si sospechas que mintió. Si fallas, pierdes una vida.",
            "Esc: pausa. Tras ganar, elige un premio con clic o con las teclas 1, 2 y 3."
        };
        for (int i = 0; i < instructions.Length; i++)
        {
            CreateLabel(card.transform, $"Instruction_{i + 1}", font, instructions[i],
                new Vector2(0.5f, 0.74f - i * 0.16f), new Vector2(145f, 0f),
                new Vector2(620f, 78f), 23f, TextAlignmentOptions.Center, Color.white);
        }

        GameObject closeButtonObject = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, card.transform);
        closeButtonObject.name = "CloseButton";
        RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(0f, 62f);
        closeRect.sizeDelta = new Vector2(360f, 72f);
        TMP_Text closeText = closeButtonObject.GetComponentInChildren<TMP_Text>();
        if (closeText == null) Debug.LogError("Button_Base no contiene texto TMP.", closeButtonObject);
        else closeText.text = "Volver";

        panelObject.SetActive(false);
        return panelObject;
    }

    private static void CreateLaneIllustration(Transform parent)
    {
        Image diagram = CreateImage(parent, "ThreeLaneIllustration",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(155f, 12f), new Vector2(250f, 310f), new Color32(28, 20, 52, 255));
        RectTransform diagramRect = diagram.rectTransform;
        diagramRect.anchorMin = new Vector2(0f, 0.5f);
        diagramRect.anchorMax = new Vector2(0f, 0.5f);

        Color laneColor = new Color32(233, 230, 223, 255);
        for (int i = 0; i < 3; i++)
        {
            CreateImage(diagram.transform, $"Lane_{i + 1}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-72f + i * 72f, 0f), new Vector2(62f, 260f), laneColor);
        }

        CreateImage(diagram.transform, "ContestantMarker",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-72f, -78f), new Vector2(36f, 56f), new Color32(42, 120, 212, 255));
        CreateImage(diagram.transform, "StarMarker",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(72f, 72f), new Vector2(26f, 26f), new Color32(247, 177, 29, 255));
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
        imageObject.GetComponent<Image>().color = color;
        return imageObject.GetComponent<Image>();
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
