using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Crea los recursos base de tipografía y botones de la interfaz.</summary>
public static class UiAssetCreator
{
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    private const string FontAssetPath = "Assets/_Project/Fonts/Font_UI_Dynamic.asset";
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";

    [MenuItem("Una Ronda Más/UI/Crear fuente y botón base")]
    public static void CreateUiAssets()
    {
        ProjectPaths.EnsureFolder("Assets/_Project/Fonts");
        ProjectPaths.EnsureFolder("Assets/_Project/Prefabs/UI");

        TMP_FontAsset font = GetOrCreateFont();
        if (font == null) return;

        CreateButtonPrefab(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static TMP_FontAsset GetOrCreateFont()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"No se encontró la fuente TMP Essentials: {SourceFontPath}");
            return null;
        }

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (font == null)
        {
            Debug.LogError($"No se pudo crear la fuente dinámica desde {SourceFontPath}.");
            return null;
        }

        font.name = "Font_UI_Dynamic";
        AssetDatabase.CreateAsset(font, FontAssetPath);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (Texture2D atlas in font.atlasTextures)
        {
            if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, font);
        }
        EditorUtility.SetDirty(font);
        return font;
    }

    private static void CreateButtonPrefab(TMP_FontAsset font)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath) != null)
        {
            Debug.Log($"El prefab ya existe: {ButtonPrefabPath}");
            return;
        }

        var root = new GameObject("Button_Base", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(320f, 72f);

        Image background = root.GetComponent<Image>();
        background.color = new Color32(36, 25, 63, 255);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = new Color32(36, 25, 63, 255),
            highlightedColor = new Color32(72, 54, 112, 255),
            pressedColor = new Color32(25, 18, 44, 255),
            selectedColor = new Color32(72, 54, 112, 255),
            disabledColor = new Color32(128, 128, 128, 128),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 8f);
        labelRect.offsetMax = new Vector2(-16f, -8f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = "Botón";
        label.fontSize = 32f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 32f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
        Object.DestroyImmediate(root);
        if (savedPrefab == null) Debug.LogError($"No se pudo crear el prefab: {ButtonPrefabPath}");
    }
}
