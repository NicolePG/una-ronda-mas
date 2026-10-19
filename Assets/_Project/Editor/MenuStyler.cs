using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Estilo de estudio de TV para los menús: fondo dibujado por código (reflectores, foquitos, tarima y agua),
/// paneles y botones redondeados con borde dorado. Se aplica al menú principal y a los prefabs de UI
/// editándolos en su lugar (no se rompen referencias).
/// </summary>
public static class MenuStyler
{
    private const string Folder = ProjectPaths.Art + "/UI";
    private const string BackgroundPath = Folder + "/Bg_Studio.png";
    private const string PanelPath = Folder + "/Panel_Gold.png";
    private const string CardPath = Folder + "/Panel_Card.png";
    private const string ButtonPath = Folder + "/Button_Gold.png";
    private const string MenuScenePath = ProjectPaths.Scenes + "/MainMenu.unity";
    private const string BackgroundName = "StudioBackground";
    private const int BgWidth = 1920;
    private const int BgHeight = 1080;

    private static readonly string[] UiPrefabs =
    {
        "Assets/_Project/Prefabs/UI/Button_Base.prefab",
        "Assets/_Project/Prefabs/UI/HostBanner_Canvas.prefab",
        "Assets/_Project/Prefabs/UI/GameOver_Canvas.prefab",
        "Assets/_Project/Prefabs/UI/PauseMenu_Canvas.prefab",
        "Assets/_Project/Prefabs/UI/PrizeScreen_Canvas.prefab",
        "Assets/_Project/Prefabs/UI/HostLines_Canvas.prefab",
    };

    private static readonly HashSet<string> PanelNames = new HashSet<string>
    {
        "MenuPanel", "OptionsPanel", "HowToPlayPanel", "CreditsPanel",
        "AnnouncementPanel", "PausePanel", "PrizePanel", "Card",
    };

    private static readonly HashSet<string> CardNames = new HashSet<string> { "Card_1", "Card_2", "Card_3", "SpeechBubble" };

    private static readonly Color Gold = Hex("#F7B11D");
    private static readonly Color Lavender = Hex("#B79CFF");

    [MenuItem("Una Ronda Más/UI/Estilo del estudio (fondo, paneles y botones)")]
    public static void Apply()
    {
        ProjectPaths.EnsureFolder(Folder);
        Sprite background = SaveSprite(BackgroundPath, DrawBackground(), Vector4.zero);
        Sprite panel = SaveSprite(PanelPath, DrawRounded(160, 160, 36f, 5f, Gold, Hex("#33236A"), Hex("#1C1240"), 0f), new Vector4(48, 48, 48, 48));
        Sprite card = SaveSprite(CardPath, DrawRounded(160, 160, 28f, 3f, Lavender, Hex("#43307E"), Hex("#2A1D57"), 0f), new Vector4(40, 40, 40, 40));
        Sprite button = SaveSprite(ButtonPath, DrawRounded(128, 64, 22f, 3f, Gold, Hex("#7653E0"), Hex("#4A2F9E"), 0.18f), new Vector4(28, 28, 28, 28));

        foreach (string path in UiPrefabs)
        {
            if (!File.Exists(path)) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Style(root, panel, card, button);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (File.Exists(MenuScenePath))
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Style(root, panel, card, button);
                Canvas canvas = root.GetComponent<Canvas>();
                if (canvas != null && canvas.isRootCanvas && root.name.Contains("Menu")) AddBackground(root.transform, background);
            }
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Estilo del estudio aplicado: fondo del menú, paneles y botones.");
    }

    private static void Style(GameObject root, Sprite panel, Sprite card, Sprite button)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            Button buttonComponent = image.GetComponent<Button>();
            if (buttonComponent != null)
            {
                SetSliced(image, button, Color.white);
                // El tinte oscuro anterior apagaba el botón: ahora normal = sin tinte, encima = dorado claro.
                ColorBlock colors = buttonComponent.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.92f, 0.7f);
                colors.selectedColor = new Color(1f, 0.92f, 0.7f);
                colors.pressedColor = new Color(0.8f, 0.75f, 0.9f);
                colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                colors.colorMultiplier = 1f;
                buttonComponent.colors = colors;
                EditorUtility.SetDirty(buttonComponent);
            }
            else if (PanelNames.Contains(image.name))
            {
                SetSliced(image, panel, new Color(1f, 1f, 1f, image.name == "MenuPanel" ? 0.9f : 0.97f));
            }
            else if (CardNames.Contains(image.name))
            {
                SetSliced(image, card, Color.white);
            }
        }
    }

    private static void SetSliced(Image image, Sprite sprite, Color color)
    {
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.fillCenter = true;
        image.color = color;
        EditorUtility.SetDirty(image);
    }

    private static void AddBackground(Transform canvas, Sprite sprite)
    {
        // El menú ya tiene un fondo de pantalla completa: se le pone la imagen del estudio.
        Transform current = canvas.Find("Background");
        Image currentImage = current != null ? current.GetComponent<Image>() : null;
        if (currentImage != null)
        {
            currentImage.sprite = sprite;
            currentImage.type = Image.Type.Simple;
            currentImage.preserveAspect = false;
            currentImage.color = Color.white;
            EditorUtility.SetDirty(currentImage);
            Transform extra = canvas.Find(BackgroundName);
            if (extra != null) Object.DestroyImmediate(extra.gameObject);
            return;
        }

        Transform existing = canvas.Find(BackgroundName);
        GameObject go = existing != null ? existing.gameObject
            : new GameObject(BackgroundName, typeof(RectTransform), typeof(Image));
        var oldFitter = go.GetComponent<AspectRatioFitter>();
        if (oldFitter != null) Object.DestroyImmediate(oldFitter);
        go.transform.SetParent(canvas, false);
        go.transform.SetAsFirstSibling();
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.color = Color.white;
        image.preserveAspect = false;
        EditorUtility.SetDirty(go);
    }

    // ---------- Dibujo ----------

    private static Texture2D DrawRounded(int width, int height, float radius, float border, Color borderColor,
        Color top, Color bottom, float gloss)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        var half = new Vector2(width * 0.5f, height * 0.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float d = RoundBox(p, half - Vector2.one, radius);
                float t = (float)y / (height - 1);
                Color fill = Color.Lerp(bottom, top, t);
                if (gloss > 0f && t > 0.55f) fill = Color.Lerp(fill, Color.white, gloss * (t - 0.55f) / 0.45f);
                float inside = Mathf.Clamp01(0.5f - d);
                float borderMix = Mathf.Clamp01(0.5f - (-d - border));   // 1 en el borde, 0 adentro
                Color c = Color.Lerp(fill, borderColor, borderMix);
                c.a = inside;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawBackground()
    {
        int w = BgWidth, h = BgHeight;
        var pixels = new Color[w * h];
        Color top = Hex("#120A2A"), bottom = Hex("#3A1F6E");
        Color[] beamColors = { Hex("#F7B11D"), Hex("#FF5C8A"), Hex("#4FA3E0"), Hex("#F7B11D") };
        float[] beamX = { 0.12f, 0.36f, 0.64f, 0.88f };
        float[] beamTarget = { 0.42f, 0.47f, 0.53f, 0.58f };
        Color water = Hex("#4FA3E0");

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                Color c = Color.Lerp(bottom, top, v);

                // Brillo morado en el centro (escenario)
                float glow = Mathf.Exp(-(Sq((u - 0.5f) * 1.6f) + Sq((v - 0.25f) * 2.4f)) * 3f);
                c += Hex("#7A3FC0") * glow * 0.45f;

                // Reflectores desde arriba
                for (int i = 0; i < beamX.Length; i++)
                {
                    var origin = new Vector2(beamX[i], 1.08f);
                    var target = new Vector2(beamTarget[i], 0.12f);
                    Vector2 dir = (target - origin).normalized;
                    var rel = new Vector2(u, v) - origin;
                    float along = Vector2.Dot(rel, dir);
                    if (along <= 0f) continue;
                    float across = Mathf.Abs(rel.x * dir.y - rel.y * dir.x);
                    float widthAt = 0.015f + along * 0.12f;
                    float beam = Mathf.Clamp01(1f - across / widthAt);
                    c += beamColors[i] * beam * beam * 0.22f * Mathf.Clamp01(1.2f - along);
                }

                // Tarima (elipse) y agua abajo
                float stage = Sq((u - 0.5f) / 0.42f) + Sq((v - 0.13f) / 0.09f);
                if (stage < 1f) c = Color.Lerp(c, Hex("#2A1D57"), 0.85f);
                float rim = Mathf.Abs(Mathf.Sqrt(stage) - 1f);
                c += Gold * Mathf.Clamp01(1f - rim / 0.035f) * 0.8f;
                float wave = 0.055f + 0.01f * Mathf.Sin(u * 40f);
                if (v < wave) c = Color.Lerp(c, water, 0.75f);

                c.a = 1f;
                pixels[y * w + x] = c;
            }
        }

        // Foquitos de marquesina en arco alrededor del título
        for (int i = 0; i <= 26; i++)
        {
            float angle = Mathf.Lerp(10f, 170f, i / 26f) * Mathf.Deg2Rad;
            float bx = 0.5f + Mathf.Cos(angle) * 0.4f;
            float by = 0.6f + Mathf.Sin(angle) * 0.3f;
            Stamp(pixels, w, h, bx * w, by * h, 9f, i % 2 == 0 ? Gold : Hex("#FFF2C2"));
        }
        // Destellos
        var random = new System.Random(18);
        for (int i = 0; i < 90; i++)
        {
            float sx = (float)random.NextDouble() * w;
            float sy = h * (0.25f + 0.75f * (float)random.NextDouble());
            float size = 2f + (float)random.NextDouble() * 4f;
            Stamp(pixels, w, h, sx, sy, size, random.NextDouble() < 0.5 ? Color.white : Gold);
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Punto con brillo suave (foquito o destello).
    private static void Stamp(Color[] pixels, int w, int h, float cx, float cy, float radius, Color color)
    {
        int r = Mathf.CeilToInt(radius * 3f);
        for (int y = Mathf.Max(0, (int)cy - r); y < Mathf.Min(h, (int)cy + r); y++)
        {
            for (int x = Mathf.Max(0, (int)cx - r); x < Mathf.Min(w, (int)cx + r); x++)
            {
                float d = Mathf.Sqrt(Sq(x - cx) + Sq(y - cy));
                float core = Mathf.Clamp01(radius - d + 0.5f);
                float halo = Mathf.Exp(-Sq(d / (radius * 1.6f))) * 0.6f;
                Color c = pixels[y * w + x] + color * Mathf.Max(core, halo);
                c.a = 1f;
                pixels[y * w + x] = c;
            }
        }
    }

    private static Sprite SaveSprite(string path, Texture2D texture, Vector4 border)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = border;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static float RoundBox(Vector2 p, Vector2 half, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * r;
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static float Sq(float x) => x * x;

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
