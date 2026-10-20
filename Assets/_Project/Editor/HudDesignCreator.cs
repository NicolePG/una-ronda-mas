using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arma Prefabs/UI/HUD_Canvas.prefab con el diseño del lienzo: panel de estrellas y puntaje con corazones y premios
/// (arriba a la izquierda), tiempo y ronda (arriba a la derecha), cartel de la regla (abajo a la izquierda)
/// y botón «¡Trampa!» (abajo a la derecha), los dos encima de la burbuja de frases de Tito. Dibuja sus sprites en Art/UI y Art/Icons y asigna los íconos de las reglas.
/// Se puede repetir: vuelve a generar el prefab (Game.unity lo toma solo).
/// </summary>
public static class HudDesignCreator
{
    public const string PrefabPath = ProjectPaths.Root + "/Prefabs/UI/HUD_Canvas.prefab";
    private const string FontPath = ProjectPaths.Root + "/Fonts/Font_UI_Dynamic.asset";
    private const string UiFolder = ProjectPaths.Art + "/UI";
    private const string IconFolder = ProjectPaths.Art + "/Icons";
    private const int IconSize = 128;
    private const float IconOutline = 7f;
    private const float AboveSpeechBubble = 160f;   // la burbuja de frases de Tito ocupa la franja de abajo

    private static readonly Color Ink = IconPainter.Hex("#1A1230");
    private static readonly Color Lavender = IconPainter.Hex("#B79CFF");
    private static readonly Color Gold = IconPainter.Hex("#F7B11D");
    private static readonly Color StarLight = IconPainter.Hex("#FBD97A");
    private static readonly Color Red = IconPainter.Hex("#D13B3B");
    private static readonly Color DarkRed = IconPainter.Hex("#8C2222");
    private static readonly Color Muted = IconPainter.Hex("#CFC6EA");
    private static readonly Color Water = IconPainter.Hex("#4FA3E0");
    private static readonly Color Cream = IconPainter.Hex("#E9E6DF");
    private static readonly Color Grey = IconPainter.Hex("#8C8C99");

    private struct Sprites
    {
        public Sprite Panel, RuleChip, Cheat, Key, Star, Heart;
    }

    [MenuItem("Una Ronda Más/UI/HUD con el diseño del lienzo")]
    public static void Create()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError($"Falta la fuente {FontPath} (menú de Tania «Crear fuente y botón base»).");
            return;
        }
        var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance);
        int maxLives = balance != null ? balance.MaxLives : 5;

        Sprites sprites = CreateSprites();
        CreateRuleIcons();
        BuildPrefab(font, sprites, maxLives);
        AssetDatabase.SaveAssets();
        Debug.Log($"HUD con el diseño del lienzo: {PrefabPath}.");
    }

    // ---------- Sprites ----------

    private static Sprites CreateSprites()
    {
        Color panelFill = Ink;
        panelFill.a = 0.82f;
        Color panelBorder = Lavender;
        panelBorder.a = 0.55f;
        Color chipFill = Ink;
        chipFill.a = 0.9f;
        return new Sprites
        {
            Panel = IconPainter.Save($"{UiFolder}/Hud_Panel.png",
                IconPainter.Panel(128, 128, 26f, 2f, panelFill, panelBorder), new Vector4(32, 32, 32, 32)),
            RuleChip = IconPainter.Save($"{UiFolder}/Hud_RuleChip.png",
                IconPainter.Panel(128, 128, 24f, 4f, chipFill, Gold), new Vector4(32, 32, 32, 32)),
            Cheat = IconPainter.Save($"{UiFolder}/Hud_Cheat.png",
                IconPainter.Panel(128, 128, 24f, 0f, Red, Red, 8f, DarkRed), new Vector4(36, 36, 36, 36)),
            Key = IconPainter.Save($"{UiFolder}/Hud_Key.png",
                IconPainter.Panel(64, 64, 12f, 4f, Color.clear, Color.white), new Vector4(18, 18, 18, 18)),
            Star = IconPainter.Save($"{IconFolder}/Icon_Star.png", IconPainter.Icon(IconSize, IconOutline,
                new IconPainter.Layer(p => IconPainter.Star(p, new Vector2(0f, -0.04f), 0.86f, 0.4f), Gold),
                new IconPainter.Layer(p => IconPainter.Star(p, new Vector2(-0.08f, 0.02f), 0.4f, 0.18f), StarLight)), Vector4.zero),
            Heart = IconPainter.Save($"{IconFolder}/Icon_Heart.png", IconPainter.Icon(IconSize, IconOutline,
                new IconPainter.Layer(p => IconPainter.Heart(p, Vector2.zero, 0.92f), Red),
                new IconPainter.Layer(p => IconPainter.Circle(p, new Vector2(-0.34f, 0.3f), 0.14f), new Color(1f, 1f, 1f, 0.55f))), Vector4.zero),
        };
    }

    // Íconos de las 5 reglas (los usa el cartel del HUD; RuleData.Icon).
    private static void CreateRuleIcons()
    {
        var icons = new Dictionary<RuleType, Sprite>
        {
            [RuleType.DoubleValue] = RuleIcon(RuleType.DoubleValue,
                new IconPainter.Layer(p => IconPainter.Star(p, new Vector2(-0.26f, -0.16f), 0.56f, 0.25f), StarLight),
                new IconPainter.Layer(p => IconPainter.Star(p, new Vector2(0.26f, 0.18f), 0.56f, 0.25f), Gold)),
            [RuleType.Blackout] = RuleIcon(RuleType.Blackout,
                new IconPainter.Layer(p => IconPainter.Circle(p, new Vector2(0f, 0.2f), 0.48f), Muted),
                new IconPainter.Layer(p => IconPainter.Box(p, new Vector2(0f, -0.48f), new Vector2(0.22f, 0.16f)), Grey),
                new IconPainter.Layer(p => IconPainter.Segment(p, new Vector2(-0.68f, -0.68f), new Vector2(0.68f, 0.68f), 0.1f), Red)),
            [RuleType.Mirror] = RuleIcon(RuleType.Mirror,
                new IconPainter.Layer(p => IconPainter.Arrow(p, new Vector2(-0.72f, 0.3f), new Vector2(0.76f, 0.3f), 0.1f), Cream),
                new IconPainter.Layer(p => IconPainter.Arrow(p, new Vector2(0.72f, -0.3f), new Vector2(-0.76f, -0.3f), 0.1f), Water)),
            [RuleType.Giant] = RuleIcon(RuleType.Giant,
                new IconPainter.Layer(p => Mathf.Min(IconPainter.Arrow(p, Vector2.zero, new Vector2(0.72f, 0.72f), 0.1f),
                    IconPainter.Arrow(p, Vector2.zero, new Vector2(-0.72f, -0.72f), 0.1f)), Gold)),
            [RuleType.NoJump] = RuleIcon(RuleType.NoJump,
                new IconPainter.Layer(p => IconPainter.Arrow(p, new Vector2(0f, -0.56f), new Vector2(0f, 0.6f), 0.1f), Cream),
                new IconPainter.Layer(p => Mathf.Min(IconPainter.Ring(p, Vector2.zero, 0.74f, 0.09f),
                    IconPainter.Segment(p, new Vector2(-0.52f, 0.52f), new Vector2(0.52f, -0.52f), 0.09f)), Red)),
        };

        foreach (RuleData rule in ProjectPaths.LoadAll<RuleData>(ProjectPaths.Rules))
        {
            if (!icons.TryGetValue(rule.Type, out Sprite icon)) continue;
            var so = new SerializedObject(rule);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Sprite RuleIcon(RuleType type, params IconPainter.Layer[] layers) =>
        IconPainter.Save($"{IconFolder}/Rule_{type}.png", IconPainter.Icon(IconSize, IconOutline, layers), Vector4.zero);

    // ---------- Prefab ----------

    private static void BuildPrefab(TMP_FontAsset font, Sprites s, int maxLives)
    {
        var root = new GameObject("HUD_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HUD));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        Vector2 topLeft = new Vector2(0f, 1f), topRight = new Vector2(1f, 1f);
        Vector2 bottomLeft = Vector2.zero, bottomRight = new Vector2(1f, 0f);
        Vector2 left = new Vector2(0f, 0.5f);

        // Estrellas, puntaje, vidas y premios.
        RectTransform stats = Panel(root.transform, "StatsPanel", s.Panel, topLeft, new Vector2(24f, -20f), new Vector2(300f, 152f));
        RectTransform starsGroup = Rect(stats, "StarsGroup", topLeft, new Vector2(16f, -12f), new Vector2(268f, 56f));
        Img(starsGroup, "StarIcon", s.Star, left, Vector2.zero, new Vector2(46f, 46f));
        TMP_Text stars = Text(starsGroup, "StarsText", font, "0 / 20", left, new Vector2(56f, 2f), new Vector2(210f, 56f),
            44f, TextAlignmentOptions.Left, Color.white, true);
        TMP_Text score = Text(stats, "ScoreText", font, "PUNTAJE 0", topLeft, new Vector2(72f, -64f), new Vector2(210f, 22f),
            17f, TextAlignmentOptions.Left, Muted, false);
        RectTransform heartsRow = Row(stats, "HeartsRow", new Vector2(16f, -92f), new Vector2(268f, 30f), 6f);
        var hearts = new Image[maxLives];
        for (int i = 0; i < maxLives; i++) hearts[i] = Img(heartsRow, $"Heart_{i + 1}", s.Heart, left, Vector2.zero, new Vector2(30f, 30f));
        RectTransform prizeRow = Row(stats, "PrizeRow", new Vector2(16f, -124f), new Vector2(268f, 24f), 5f);
        Image prizeTemplate = Img(prizeRow, "PrizeIconTemplate", null, left, Vector2.zero, new Vector2(24f, 24f));
        prizeTemplate.gameObject.SetActive(false);

        // Tiempo y ronda.
        RectTransform timePanel = Panel(root.transform, "TimePanel", s.Panel, topRight, new Vector2(-24f, -20f), new Vector2(190f, 120f));
        TMP_Text time = Text(timePanel, "TimeText", font, "0:40", topRight, new Vector2(-16f, -8f), new Vector2(170f, 58f),
            52f, TextAlignmentOptions.Right, Color.white, true);
        TMP_Text round = Text(timePanel, "RoundText", font, "Ronda 1", topRight, new Vector2(-16f, -66f), new Vector2(170f, 24f),
            20f, TextAlignmentOptions.Right, Muted, false);
        Text(timePanel, "PauseHint", font, "Esc: pausa", topRight, new Vector2(-16f, -92f), new Vector2(170f, 20f),
            14f, TextAlignmentOptions.Right, Muted, false);

        // Cartel de la regla activa.
        RectTransform chip = Panel(root.transform, "RuleChip", s.RuleChip, bottomLeft, new Vector2(24f, AboveSpeechBubble), new Vector2(360f, 74f));
        Image ruleIcon = Img(chip, "RuleIcon", null, left, new Vector2(12f, 0f), new Vector2(52f, 52f));
        Text(chip, "RuleLabel", font, "REGLA", topLeft, new Vector2(76f, -10f), new Vector2(270f, 18f),
            14f, TextAlignmentOptions.Left, Gold, true);
        TMP_Text ruleText = Text(chip, "RuleText", font, "Todo vale doble", topLeft, new Vector2(76f, -28f), new Vector2(272f, 36f),
            26f, TextAlignmentOptions.Left, Color.white, false);
        ruleText.overflowMode = TextOverflowModes.Ellipsis;
        chip.gameObject.SetActive(false);

        // Botón «¡Trampa!».
        RectTransform cheat = Panel(root.transform, "CheatButton", s.Cheat, bottomRight, new Vector2(-24f, AboveSpeechBubble), new Vector2(252f, 80f));
        Image cheatImage = cheat.GetComponent<Image>();
        cheatImage.raycastTarget = true;
        Button button = cheat.gameObject.AddComponent<Button>();
        button.targetGraphic = cheatImage;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.7f, 0.7f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        RectTransform key = Panel(cheat, "KeyBadge", s.Key, left, new Vector2(14f, 3f), new Vector2(46f, 46f));
        Text(key, "KeyText", font, "T", Vector2.one * 0.5f, Vector2.zero, new Vector2(46f, 46f),
            26f, TextAlignmentOptions.Center, Color.white, true);
        Text(cheat, "CheatTitle", font, "¡Trampa!", topLeft, new Vector2(72f, -10f), new Vector2(170f, 36f),
            30f, TextAlignmentOptions.Left, Color.white, true);
        Text(cheat, "CheatSub", font, "disponible", topLeft, new Vector2(72f, -46f), new Vector2(170f, 20f),
            14f, TextAlignmentOptions.Left, Color.white, false);
        cheat.gameObject.SetActive(false);

        root.GetComponent<HUD>().EditorSetup(stars, score, starsGroup, hearts, heartsRow, prizeRow, prizeTemplate,
            time, round, chip.gameObject, ruleIcon, ruleText, button);

        ProjectPaths.EnsureFolder(ProjectPaths.Root + "/Prefabs/UI");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    // ---------- Utilidades de UI ----------

    // Rect anclado en un borde o esquina: el pivote coincide con el ancla.
    private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Panel(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(parent, name, anchor, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return rect;
    }

    private static Image Img(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(parent, name, anchor, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Row(Transform parent, string name, Vector2 position, Vector2 size, float spacing)
    {
        RectTransform rect = Rect(parent, name, new Vector2(0f, 1f), position, size);
        var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return rect;
    }

    private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, string value, Vector2 anchor,
        Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color, bool bold)
    {
        RectTransform rect = Rect(parent, name, anchor, position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }
}
