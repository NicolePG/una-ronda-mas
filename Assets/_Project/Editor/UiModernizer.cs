using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Interfaz moderna estilo transmisión de TV (Otoniel, sobre las pantallas de Tania): paneles de vidrio oscuro con
/// borde de neón dorado a rosa, botones píldora con halo al pasar el cursor, el principal dorado con destello,
/// títulos dorados con contorno, entradas animadas en cascada, tarjetas de premio con brillo de rareza,
/// cartel «● EN VIVO» y cinta de noticias en el menú. Dibuja su kit en Art/UI/Modern y edita en su lugar los
/// prefabs de UI y MainMenu.unity (no rompe referencias). Si hay una fuente .ttf/.otf en Fonts, la usa para títulos.
/// Se puede repetir. Lo llama «Crear todo» al final.
/// </summary>
public static class UiModernizer
{
    public const string Folder = ProjectPaths.Art + "/UI/Modern";
    private const string MenuScenePath = ProjectPaths.Scenes + "/MainMenu.unity";
    private const string FontsFolder = ProjectPaths.Root + "/Fonts";
    private const string BaseFontPath = FontsFolder + "/Font_UI_Dynamic.asset";
    private const string DisplayFontPath = FontsFolder + "/Font_Display.asset";
    private const string ButtonBasePath = ProjectPaths.Root + "/Prefabs/UI/Button_Base.prefab";
    private const float HaloPixels = 20f;

    private static readonly string[] ScreenPrefabs =
    {
        ProjectPaths.Root + "/Prefabs/UI/HostBanner_Canvas.prefab",
        ProjectPaths.Root + "/Prefabs/UI/PrizeScreen_Canvas.prefab",
        ProjectPaths.Root + "/Prefabs/UI/PauseMenu_Canvas.prefab",
        ProjectPaths.Root + "/Prefabs/UI/GameOver_Canvas.prefab",
        ProjectPaths.Root + "/Prefabs/UI/HostLines_Canvas.prefab",
    };

    // Botones que llevan al juego: dorados, con destello y halo que late.
    private static readonly HashSet<string> PrimaryButtons = new HashSet<string>
        { "PlayButton", "RestartButton", "ResumeButton", "AcceptBetButton" };

    private static readonly HashSet<string> GlassPanels = new HashSet<string>
        { "AnnouncementPanel", "PausePanel", "PrizePanel", "OptionsPanel", "HowToPlayPanel", "CreditsPanel" };

    private static readonly Color Gold = IconPainter.Hex("#F7B11D");
    private static readonly Color Pink = IconPainter.Hex("#FF5C8A");
    private static readonly Color Lavender = IconPainter.Hex("#B79CFF");
    private static readonly Color Ink = IconPainter.Hex("#2A1650");
    private static readonly Color DangerTitle = IconPainter.Hex("#FF4D6A");

    private struct Kit
    {
        public Sprite Panel, Card, Pill, PillPrimary, Halo, Shine, Vignette, LiveTag, Dot;
        public TMP_FontAsset TitleFont;
        public Material TitleMaterial;
    }

    public static void Apply()
    {
        Kit kit = CreateKit();
        StylePrefab(ButtonBasePath, kit);           // primero el botón base: los demás lo heredan
        foreach (string path in ScreenPrefabs) StylePrefab(path, kit);
        StyleMenu(kit);
        AssetDatabase.SaveAssets();
        Debug.Log($"Interfaz moderna aplicada ({(kit.TitleFont != null ? kit.TitleFont.name : "sin fuente")}).");
    }

    // ---------- Pantallas ----------

    private static void StylePrefab(string path, Kit kit)
    {
        if (!File.Exists(path)) return;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            StyleTree(root, kit);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void StyleTree(GameObject root, Kit kit)
    {
        int card = 0;
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.name;
            if (image.GetComponent<Button>() != null) StyleButton(image, kit);
            else if (GlassPanels.Contains(name) || (name == "Card" && image.transform.parent == root.transform))
            {
                StylePanel(image, kit.Panel, 1.5f, true, new Vector2(0f, -50f));
                // El anuncio de Tito lleva el cartel «EN VIVO» montado sobre el borde, como una pestaña.
                if (name == "AnnouncementPanel")
                    BuildLiveBadge(image.transform, kit, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(36f, 0f));
            }
            else if (name.StartsWith("Card_"))
                StylePrizeCard(image, kit, card++);
            else if (name == "SpeechBubble")
                StylePanel(image, kit.Card, 1.5f, true, new Vector2(0f, -24f), 0.22f);
            else if (name == "Overlay")
                SetSprite(image, kit.Vignette, Image.Type.Simple, Color.white, 1f);
            else if (name == "RecordsChip")
                SetSprite(image, kit.Card, Image.Type.Sliced, new Color(1f, 1f, 1f, 0.95f), 1.5f);
            else if (name == "Fill" && image.GetComponentInParent<Slider>(true) != null)
                SetColor(image, Gold);
        }

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.GetComponentInParent<Button>(true) != null) continue;
            bool isTitle = text.name == "Title" || text.name.EndsWith("Title");
            if (!isTitle) continue;
            bool danger = root.name.StartsWith("GameOver");
            StyleTitle(text, kit, danger ? DangerTitle : Gold);
            if (danger && text.GetComponent<TitlePulse>() == null) text.gameObject.AddComponent<TitlePulse>();
        }
    }

    private static void StyleButton(Image image, Kit kit)
    {
        bool primary = PrimaryButtons.Contains(image.name);
        SetSprite(image, primary ? kit.PillPrimary : kit.Pill, Image.Type.Sliced, Color.white, 2f);

        var button = image.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 0.86f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.82f, 0.78f, 0.9f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        Record(button);

        TMP_Text label = image.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = primary ? Ink : Color.white;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 2f;
            if (kit.TitleFont != null) label.font = kit.TitleFont;
            Record(label);
        }

        // Halo de neón alrededor (encima del botón pero vacío por dentro: no tapa el texto).
        Image halo = Child(image.transform, "Glow", 0);
        Stretch(halo.rectTransform, -HaloPixels);
        SetSprite(halo, kit.Halo, Image.Type.Sliced, primary ? Gold : Lavender, 1f);
        halo.raycastTarget = false;
        Color clear = halo.color;
        clear.a = 0f;
        halo.color = clear;
        ButtonGlow glow = GetOrAdd<ButtonGlow>(image.gameObject);
        glow.EditorSetup(halo, primary ? 0.45f : 0f);
        Record(glow);

        // Destello que cruza el botón, recortado por una máscara del tamaño del botón (debajo del texto).
        // Está en todos los botones (viene de Button_Base) pero solo corre en los principales.
        Image maskImage = Child(image.transform, "ShineMask", 1);
        Stretch(maskImage.rectTransform, 0f);
        maskImage.color = Color.clear;
        maskImage.raycastTarget = false;
        Record(maskImage);
        GetOrAdd<RectMask2D>(maskImage.gameObject);
        Image shine = Child(maskImage.transform, "Shine", 0);
        shine.sprite = kit.Shine;
        shine.raycastTarget = false;
        shine.color = new Color(1f, 1f, 1f, 0.7f);
        RectTransform shineRect = shine.rectTransform;
        shineRect.anchorMin = shineRect.anchorMax = new Vector2(0.5f, 0.5f);
        shineRect.sizeDelta = new Vector2(90f, 140f);
        shineRect.anchoredPosition = Vector2.zero;
        Record(shine);
        Record(shineRect);
        if (shine.gameObject.activeSelf) shine.gameObject.SetActive(false);
        UiShine sweep = GetOrAdd<UiShine>(maskImage.gameObject);
        sweep.EditorSetup(shineRect);
        sweep.enabled = primary;
        Record(sweep);
    }

    private static void StylePanel(Image image, Sprite sprite, float pixelsPerUnit, bool animate, Vector2 from, float duration = 0.35f)
    {
        SetSprite(image, sprite, Image.Type.Sliced, Color.white, pixelsPerUnit);
        if (!animate) return;
        UiEntrance entrance = GetOrAdd<UiEntrance>(image.gameObject);
        entrance.EditorSetup(0f, from, true, duration);
        Record(entrance);
    }

    // Tarjeta de premio: vidrio, halo con el color de la rareza (lo pinta PrizeScreen) y entrada en cascada.
    private static void StylePrizeCard(Image image, Kit kit, int index)
    {
        SetSprite(image, kit.Card, Image.Type.Sliced, Color.white, 1.5f);
        Image glow = Child(image.transform, "RarityGlow", 0);
        Stretch(glow.rectTransform, -HaloPixels);
        SetSprite(glow, kit.Halo, Image.Type.Sliced, new Color(1f, 1f, 1f, 0f), 1f);
        glow.raycastTarget = false;
        UiEntrance entrance = GetOrAdd<UiEntrance>(image.gameObject);
        entrance.EditorSetup(0.12f + index * 0.09f, new Vector2(0f, -70f), true, 0.4f);
        Record(entrance);
    }

    private static void StyleTitle(TMP_Text text, Kit kit, Color color)
    {
        if (kit.TitleFont != null) text.font = kit.TitleFont;
        if (kit.TitleMaterial != null) text.fontSharedMaterial = kit.TitleMaterial;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 3f;
        Record(text);
    }

    // ---------- Menú principal ----------

    private static void StyleMenu(Kit kit)
    {
        if (!File.Exists(MenuScenePath)) return;
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        MainMenuUI menu = Object.FindObjectOfType<MainMenuUI>();
        if (menu == null)
        {
            Debug.LogWarning("Interfaz moderna: MainMenu.unity no tiene MainMenuUI.");
            return;
        }

        Transform canvas = menu.transform;
        StyleTree(canvas.gameObject, kit);

        // El logo usa la fuente de títulos si hay una (tamaño y posición los pone el estudio 3D).
        Transform panel = canvas.Find("MenuPanel");
        if (panel != null)
        {
            Transform logo = panel.Find("Title");
            if (logo != null && kit.TitleFont != null)
            {
                var text = logo.GetComponent<TMP_Text>();
                text.font = kit.TitleFont;
                if (kit.TitleMaterial != null) text.fontSharedMaterial = kit.TitleMaterial;
            }
            // Los botones del menú entran en cascada desde la izquierda.
            string[] order = { "PlayButton", "HowToPlayButton", "OptionsButton", "CreditsButton" };
            for (int i = 0; i < order.Length; i++)
            {
                Transform button = panel.Find(order[i]);
                if (button == null) continue;
                UiEntrance entrance = GetOrAdd<UiEntrance>(button.gameObject);
                entrance.EditorSetup(0.25f + i * 0.08f, new Vector2(-80f, 0f), false, 0.4f);
                Record(entrance);
            }
            MoveUp(panel.Find("Status"), 60f);
        }
        MoveUp(canvas.Find("JamLabel"), 54f);

        // «EN VIVO» arriba a la derecha, al lado de los récords.
        BuildLiveBadge(canvas, kit, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-404f, -46f));
        BuildTicker(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Cartel «● EN VIVO» con el punto que titila.
    private static void BuildLiveBadge(Transform parent, Kit kit, Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        Image badge = Child(parent, "LiveBadge", -1);
        Place(badge.rectTransform, anchor, pivot, position, new Vector2(170f, 44f));
        SetSprite(badge, kit.LiveTag, Image.Type.Simple, Color.white, 1f);
        badge.raycastTarget = false;

        Image dot = Child(badge.transform, "Dot", -1);
        Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(16f, 16f));
        dot.sprite = kit.Dot;
        dot.color = Color.white;
        dot.raycastTarget = false;

        TMP_Text label = ChildText(badge.transform, "Label", BaseFont());
        Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(110f, 36f));
        label.text = "EN VIVO";
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 4f;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
        if (kit.TitleFont != null) label.font = kit.TitleFont;

        GetOrAdd<LiveBadge>(badge.gameObject).EditorSetup(dot);
        GetOrAdd<UiEntrance>(badge.gameObject).EditorSetup(0.5f, new Vector2(0f, 30f), false, 0.4f);
    }

    // Cinta de noticias abajo, de lado a lado.
    private static void BuildTicker(Transform canvas)
    {
        Image strip = Child(canvas, "Ticker", -1);
        Place(strip.rectTransform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 44f));
        strip.rectTransform.anchorMax = new Vector2(1f, 0f);
        strip.sprite = null;
        strip.color = new Color(0.05f, 0.03f, 0.12f, 0.9f);
        strip.raycastTarget = false;

        Image line = Child(strip.transform, "TopLine", -1);
        Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f));
        line.rectTransform.anchorMax = Vector2.one;
        line.color = Gold;
        line.raycastTarget = false;

        Image tag = Child(strip.transform, "Tag", -1);
        Place(tag.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(150f, 44f));
        tag.color = IconPainter.Hex("#D13B3B");
        tag.raycastTarget = false;
        TMP_Text tagText = ChildText(tag.transform, "Label", BaseFont());
        Stretch(tagText.rectTransform, 0f);
        tagText.text = "ÚLTIMO MOMENTO";
        tagText.fontSize = 16f;
        tagText.fontStyle = FontStyles.Bold;
        tagText.alignment = TextAlignmentOptions.Center;
        tagText.color = Color.white;

        Image mask = Child(strip.transform, "Mask", -1);
        Place(mask.rectTransform, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        mask.rectTransform.anchorMax = Vector2.one;
        mask.rectTransform.offsetMin = new Vector2(162f, 0f);
        mask.rectTransform.offsetMax = Vector2.zero;
        mask.color = Color.clear;
        mask.raycastTarget = false;
        GetOrAdd<RectMask2D>(mask.gameObject);

        TMP_Text first = ChildText(mask.transform, "TextA", BaseFont());
        TMP_Text second = ChildText(mask.transform, "TextB", BaseFont());
        foreach (TMP_Text text in new[] { first, second })
        {
            Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(2000f, 40f));
            text.fontSize = 20f;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Left;
            text.color = IconPainter.Hex("#FBD97A");
        }
        GetOrAdd<TickerTape>(strip.gameObject).EditorSetup(first, second);
    }

    // ---------- Kit (dibujado por código) ----------

    private static Kit CreateKit()
    {
        var kit = new Kit
        {
            Panel = IconPainter.Save($"{Folder}/Glass_Panel.png",
                RoundedGradient(256, 256, 40f, 3f, Hex("#2E1B66", 0.93f), Hex("#120A2A", 0.95f), Gold, Pink, 0.06f), Border(56)),
            Card = IconPainter.Save($"{Folder}/Glass_Card.png",
                RoundedGradient(192, 192, 30f, 2f, Hex("#2A1A5C", 0.95f), Hex("#160C36", 0.96f), Lavender, Hex("#6B4FD0", 1f), 0.05f), Border(44)),
            Pill = IconPainter.Save($"{Folder}/Pill_Button.png",
                RoundedGradient(192, 96, 40f, 2f, Hex("#7E5BF0", 1f), Hex("#432996", 1f), Hex("#D9C9FF", 0.9f), Hex("#8C6CF5", 0.9f), 0.22f),
                new Vector4(46f, 40f, 46f, 40f)),
            PillPrimary = IconPainter.Save($"{Folder}/Pill_Primary.png",
                RoundedGradient(192, 96, 40f, 2f, Hex("#FFDA6B", 1f), Hex("#F0920F", 1f), Hex("#FFF4C6", 1f), Hex("#C66F00", 1f), 0.3f),
                new Vector4(46f, 40f, 46f, 40f)),
            Halo = IconPainter.Save($"{Folder}/Halo.png", Halo(128, 96, 20f, HaloPixels), Border(42)),
            Shine = IconPainter.Save($"{Folder}/Shine.png", Shine(64, 128), Vector4.zero),
            Vignette = IconPainter.Save($"{Folder}/Vignette.png", Vignette(256), Vector4.zero),
            LiveTag = IconPainter.Save($"{Folder}/Live_Tag.png", LiveTag(170, 44), Vector4.zero),
            Dot = IconPainter.Save($"{Folder}/Dot.png", Dot(32, IconPainter.Hex("#FF3B4E")), Vector4.zero),
        };
        kit.TitleFont = DisplayFont();
        kit.TitleMaterial = kit.TitleFont != null ? OutlineMaterial(kit.TitleFont) : null;
        return kit;
    }

    private static Vector4 Border(float px) => new Vector4(px, px, px, px);

    // Rectángulo redondeado con relleno degradado (arriba → abajo), borde degradado y un brillo arriba.
    private static Texture2D RoundedGradient(int width, int height, float radius, float border,
        Color fillTop, Color fillBottom, Color borderTop, Color borderBottom, float gloss)
    {
        var half = new Vector2(width * 0.5f, height * 0.5f);
        return IconPainter.Paint(width, height, (u, v) =>
        {
            var p = new Vector2(u * width, v * height) - half;
            float d = RoundBox(p, half - Vector2.one, radius);
            Color fill = Color.Lerp(fillBottom, fillTop, v);
            if (v > 0.55f) fill = Color.Lerp(fill, new Color(1f, 1f, 1f, fill.a), gloss * (v - 0.55f) / 0.45f);
            Color edge = Color.Lerp(borderBottom, borderTop, v);
            float borderMix = Mathf.Clamp01(0.5f - (-d - border));
            Color c = Color.Lerp(fill, edge, borderMix);
            c.a = Mathf.Lerp(fill.a, edge.a, borderMix) * Mathf.Clamp01(0.5f - d);
            return c;
        });
    }

    // Brillo que sale hacia afuera del borde (transparente por dentro), blanco para teñirlo.
    private static Texture2D Halo(int width, int height, float radius, float glow)
    {
        var half = new Vector2(width * 0.5f, height * 0.5f);
        return IconPainter.Paint(width, height, (u, v) =>
        {
            var p = new Vector2(u * width, v * height) - half;
            float d = RoundBox(p, half - Vector2.one * glow, radius);
            float a = d <= 0f ? Mathf.Clamp01(1f + d) * 0.9f : Mathf.Pow(Mathf.Clamp01(1f - d / glow), 2.2f);
            if (d < -1f) a = 0f;
            return new Color(1f, 1f, 1f, a);
        });
    }

    private static Texture2D Shine(int width, int height) => IconPainter.Paint(width, height, (u, v) =>
    {
        float band = (u - 0.5f) + (v - 0.5f) * 0.45f;
        return new Color(1f, 1f, 1f, Mathf.Exp(-band * band / 0.018f) * 0.85f);
    });

    private static Texture2D Vignette(int size)
    {
        Color night = IconPainter.Hex("#0B0620");
        return IconPainter.Paint(size, size, (u, v) =>
        {
            float d = new Vector2(u - 0.5f, (v - 0.5f) * 1.15f).magnitude / 0.72f;
            return new Color(night.r, night.g, night.b, Mathf.Lerp(0.55f, 0.92f, Mathf.SmoothStep(0.15f, 1f, d)));
        });
    }

    // Etiqueta inclinada roja (como los cartelitos de las noticias).
    private static Texture2D LiveTag(int width, int height)
    {
        Color top = IconPainter.Hex("#FF4D63"), bottom = IconPainter.Hex("#C21E3A");
        var half = new Vector2(width * 0.5f, height * 0.5f);
        return IconPainter.Paint(width, height, (u, v) =>
        {
            var p = new Vector2(u * width, v * height) - half;
            p.x += p.y * 0.35f;
            float d = RoundBox(p, half - new Vector2(10f, 1f), 6f);
            Color c = Color.Lerp(bottom, top, v);
            c.a = Mathf.Clamp01(0.5f - d);
            return c;
        });
    }

    private static Texture2D Dot(int size, Color color) => IconPainter.Paint(size, size, (u, v) =>
    {
        float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * size - size * 0.42f;
        Color c = color;
        c.a = Mathf.Clamp01(0.5f - d);
        return c;
    });

    private static float RoundBox(Vector2 p, Vector2 half, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * r;
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static Color Hex(string hex, float alpha)
    {
        Color c = IconPainter.Hex(hex);
        c.a = alpha;
        return c;
    }

    // ---------- Fuentes ----------

    private static TMP_FontAsset BaseFont() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BaseFontPath);

    /// <summary>Fuente de títulos: la primera .ttf/.otf de Fonts (si hay) como fuente dinámica; si no, la de siempre.</summary>
    public static TMP_FontAsset DisplayFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        if (existing != null) return existing;
        foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { FontsFolder }))
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
            if (source == null) continue;
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null) continue;
            font.name = "Font_Display";
            AssetDatabase.CreateAsset(font, DisplayFontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures)
            {
                if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, font);
            }
            // Si falta un carácter (¡, ñ…), se busca en la fuente de siempre.
            TMP_FontAsset fallback = BaseFont();
            if (fallback != null) font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(font);
            return font;
        }
        return BaseFont();
    }

    public static Material OutlineMaterial(TMP_FontAsset font)
    {
        if (font.name == "Font_UI_Dynamic") return StudioSetCreator.OutlineFontMaterial(font);
        string path = $"{FontsFolder}/{font.name}_Outline.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(font.material) { name = $"{font.name}_Outline" };
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, IconPainter.Hex("#1A1230"));
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // ---------- Utilidades ----------

    private static void SetSprite(Image image, Sprite sprite, Image.Type type, Color color, float pixelsPerUnit)
    {
        image.sprite = sprite;
        image.type = type;
        image.fillCenter = true;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = pixelsPerUnit;
        image.color = color;
        Record(image);
    }

    private static void SetColor(Image image, Color color)
    {
        image.color = color;
        Record(image);
    }

    // Hijo con Image (lo crea si falta). siblingIndex −1 = al final. Dentro de instancias de prefab no se reordena
    // (Unity no lo permite): por eso los hijos de los botones se crean en Button_Base y las instancias los heredan.
    private static Image Child(Transform parent, string name, int siblingIndex)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
            if (siblingIndex >= 0 && existing.GetSiblingIndex() != siblingIndex && !PrefabUtility.IsPartOfPrefabInstance(existing))
                existing.SetSiblingIndex(siblingIndex);
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            if (siblingIndex >= 0 && !PrefabUtility.IsPartOfPrefabInstance(parent)) go.transform.SetSiblingIndex(siblingIndex);
        }
        Image image = go.GetComponent<Image>();
        return image != null ? image : go.AddComponent<Image>();
    }

    private static TMP_Text ChildText(Transform parent, string name, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (existing == null) go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>();
        if (font != null) text.font = font;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void MoveUp(Transform target, float y)
    {
        var rect = target as RectTransform;
        if (rect == null) return;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        Record(rect);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    // Los botones son instancias de Button_Base: sin esto los cambios no quedan guardados.
    private static void Record(Object target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
