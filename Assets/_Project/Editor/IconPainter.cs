using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dibuja íconos y paneles por código (formas con distancia con signo y borde suave), con el mismo estilo
/// que los íconos de premios: relleno plano y contorno oscuro. Lo usan las herramientas del HUD.
/// </summary>
public static class IconPainter
{
    public delegate float Shape(Vector2 p);

    public readonly struct Layer
    {
        public readonly Shape Shape;
        public readonly Color Color;
        public Layer(Shape shape, Color color) { Shape = shape; Color = color; }
    }

    public static readonly Color OutlineColor = Hex("#1B1230");

    /// <summary>Ícono cuadrado: p va de −1 a 1 en ambos ejes (y hacia arriba).</summary>
    public static Texture2D Icon(int size, float outlinePixels, params Layer[] layers)
    {
        float pixel = 2f / size;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float union = float.MaxValue;
                foreach (Layer layer in layers) union = Mathf.Min(union, layer.Shape(p));
                Color c = Over(Color.clear, OutlineColor, Coverage(union - outlinePixels * pixel, pixel));
                foreach (Layer layer in layers) c = Over(c, layer.Color, Coverage(layer.Shape(p), pixel));
                pixels[y * size + x] = c;
            }
        }
        return ToTexture(size, size, pixels);
    }

    /// <summary>Panel redondeado para usar con 9-slice; la sombra (opcional) queda abajo.</summary>
    public static Texture2D Panel(int width, int height, float radius, float border, Color fill, Color borderColor,
        float shadowPixels = 0f, Color shadow = default)
    {
        var pixels = new Color[width * height];
        var half = new Vector2(width * 0.5f, (height - shadowPixels) * 0.5f);
        var center = new Vector2(width * 0.5f, shadowPixels + half.y);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                Color c = Color.clear;
                if (shadowPixels > 0f)
                    c = Over(c, shadow, Coverage(RoundBox(p - center + Vector2.up * shadowPixels, half, radius), 1f));
                // El borde es solo el anillo entre el contorno y el relleno (así un relleno transparente queda transparente).
                float d = RoundBox(p - center, half, radius);
                float outer = Coverage(d, 1f), inner = Coverage(d + border, 1f);
                c = Over(c, borderColor, Mathf.Clamp01(outer - inner));
                c = Over(c, fill, inner);
                pixels[y * width + x] = c;
            }
        }
        return ToTexture(width, height, pixels);
    }

    /// <summary>Guarda la textura como PNG y la importa como Sprite (border > 0 para 9-slice).</summary>
    public static Sprite Save(string path, Texture2D texture, Vector4 border)
    {
        ProjectPaths.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Formas (distancia con signo; negativa = adentro) ----------

    public static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

    public static float Ring(Vector2 p, Vector2 c, float r, float halfWidth) => Mathf.Abs((p - c).magnitude - r) - halfWidth;

    public static float Box(Vector2 p, Vector2 c, Vector2 half)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half;
        return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
    }

    public static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }

    public static float Polygon(Vector2 p, Vector2[] v)
    {
        float d = Vector2.Dot(p - v[0], p - v[0]);
        float s = 1f;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
        {
            Vector2 e = v[j] - v[i], w = p - v[i];
            Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = Mathf.Min(d, Vector2.Dot(b, b));
            bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
        }
        return s * Mathf.Sqrt(d);
    }

    public static float Star(Vector2 p, Vector2 c, float outer, float inner)
    {
        var points = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? outer : inner;
            points[i] = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
        return Polygon(p, points);
    }

    public static float Heart(Vector2 p, Vector2 c, float size)
    {
        Vector2 q = (p - c) / size;
        float lobes = Mathf.Min(Circle(q, new Vector2(-0.36f, 0.22f), 0.42f), Circle(q, new Vector2(0.36f, 0.22f), 0.42f));
        float tip = Polygon(q, new[] { new Vector2(-0.74f, 0.1f), new Vector2(0f, -0.82f), new Vector2(0.74f, 0.1f) });
        return Mathf.Min(lobes, tip) * size;
    }

    /// <summary>Flecha de a hacia b (cuerpo de grosor r y punta triangular).</summary>
    public static float Arrow(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
        float headLength = r * 3.2f;
        float shaft = Segment(p, a, b - dir * headLength * 0.8f, r);
        float head = Polygon(p, new[] { b, b - dir * headLength + side * r * 2.6f, b - dir * headLength - side * r * 2.6f });
        return Mathf.Min(shaft, head);
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private static float RoundBox(Vector2 p, Vector2 half, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(r, r);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static float Coverage(float distance, float pixel) => Mathf.Clamp01(0.5f - distance / pixel);

    private static Color Over(Color dst, Color src, float coverage)
    {
        float a = src.a * coverage;
        float outA = a + dst.a * (1f - a);
        if (outA <= 0f) return Color.clear;
        Color rgb = (src * a + dst * dst.a * (1f - a)) / outA;
        rgb.a = outA;
        return rgb;
    }

    private static Texture2D ToTexture(int width, int height, Color[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
