using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dibuja por código los íconos de los 17 premios (PNG en Art/Icons) y los asigna a los PrizeData.
/// Los envenenados usan el mismo ícono que el normal (se ven iguales a propósito).
/// </summary>
public static class PrizeIconCreator
{
    public const string Folder = ProjectPaths.Art + "/Icons";
    private const int Size = 256;
    private const float OutlinePixels = 8f;

    private static readonly Color OutlineColor = Hex("#1B1230");
    private static readonly Color White = Color.white;
    private static readonly Color Gold = Hex("#F7B11D");
    private static readonly Color Pink = Hex("#FF5C8A");
    private static readonly Color Orange = Hex("#FF8A3D");
    private static readonly Color Blue = Hex("#2A78D4");
    private static readonly Color LightBlue = Hex("#8CC4FF");
    private static readonly Color Green = Hex("#4CC38A");
    private static readonly Color Cream = Hex("#E9E6DF");
    private static readonly Color Night = Hex("#24193F");
    private static readonly Color Water = Hex("#4FA3E0");

    private delegate float Shape(Vector2 p);

    private readonly struct Layer
    {
        public readonly Shape Shape;
        public readonly Color Color;
        public Layer(Shape shape, Color color) { Shape = shape; Color = color; }
    }

    public static void CreateAll()
    {
        ProjectPaths.EnsureFolder(Folder);
        var icons = new Dictionary<PrizeType, Sprite>();
        foreach (PrizeType type in System.Enum.GetValues(typeof(PrizeType))) icons[type] = CreateIcon(type);

        int assigned = 0;
        foreach (PrizeData prize in ProjectPaths.LoadAll<PrizeData>(ProjectPaths.Prizes))
        {
            var so = new SerializedObject(prize);
            so.FindProperty("icon").objectReferenceValue = icons[prize.Type];
            so.ApplyModifiedPropertiesWithoutUndo();
            assigned++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Íconos de premios creados en {Folder} y asignados a {assigned} premios.");
    }

    private static Sprite CreateIcon(PrizeType type)
    {
        string path = $"{Folder}/Icon_{type}.png";
        List<Layer> layers = Layers(type);
        float pixel = 2f / Size;
        var pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                var p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f);
                float union = float.MaxValue;
                foreach (Layer layer in layers) union = Mathf.Min(union, layer.Shape(p));
                Color c = Over(Color.clear, OutlineColor, Coverage(union - OutlinePixels * pixel, pixel));
                foreach (Layer layer in layers) c = Over(c, layer.Color, Coverage(layer.Shape(p), pixel));
                pixels[y * Size + x] = c;
            }
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static List<Layer> Layers(PrizeType type)
    {
        switch (type)
        {
            case PrizeType.ExtraHeart:
                return new List<Layer>
                {
                    new Layer(p => Mathf.Min(Mathf.Min(Circle(p, new Vector2(-0.3f, 0.2f), 0.38f), Circle(p, new Vector2(0.3f, 0.2f), 0.38f)),
                        Polygon(p, new[] { new Vector2(-0.66f, 0.12f), new Vector2(0f, -0.74f), new Vector2(0.66f, 0.12f) })), Pink),
                    new Layer(p => Mathf.Min(Box(p, new Vector2(0f, 0.04f), new Vector2(0.07f, 0.26f)),
                        Box(p, new Vector2(0f, 0.04f), new Vector2(0.26f, 0.07f))), White),
                };
            case PrizeType.Magnet:
                return new List<Layer>
                {
                    new Layer(p => Mathf.Min(Mathf.Max(Ring(p, new Vector2(0f, 0.02f), 0.42f, 0.15f), p.y - 0.02f),
                        Mathf.Min(Box(p, new Vector2(-0.42f, 0.32f), new Vector2(0.15f, 0.3f)), Box(p, new Vector2(0.42f, 0.32f), new Vector2(0.15f, 0.3f)))), Orange),
                    new Layer(p => Mathf.Min(Box(p, new Vector2(-0.42f, 0.62f), new Vector2(0.15f, 0.1f)), Box(p, new Vector2(0.42f, 0.62f), new Vector2(0.15f, 0.1f))), White),
                };
            case PrizeType.Float:
                return new List<Layer>
                {
                    new Layer(p => Ring(p, Vector2.zero, 0.55f, 0.2f), Orange),
                    new Layer(p => Mathf.Max(Ring(p, Vector2.zero, 0.55f, 0.2f),
                        Mathf.Min(Box(Rotate(p, 45f), Vector2.zero, new Vector2(0.95f, 0.12f)), Box(Rotate(p, -45f), Vector2.zero, new Vector2(0.95f, 0.12f)))), White),
                };
            case PrizeType.Shield:
                Vector2[] outer = { new Vector2(-0.58f, 0.66f), new Vector2(0.58f, 0.66f), new Vector2(0.58f, 0.08f), new Vector2(0f, -0.78f), new Vector2(-0.58f, 0.08f) };
                Vector2[] inner = Scale(outer, 0.6f, new Vector2(0f, 0.04f));
                return new List<Layer>
                {
                    new Layer(p => Polygon(p, outer), Blue),
                    new Layer(p => Polygon(p, inner), LightBlue),
                    new Layer(p => Star(p, new Vector2(0f, 0.12f), 0.22f, 0.1f), Gold),
                };
            case PrizeType.Spring:
                Vector2[] coil = { new Vector2(-0.42f, -0.5f), new Vector2(0.42f, -0.32f), new Vector2(-0.42f, -0.14f),
                                   new Vector2(0.42f, 0.04f), new Vector2(-0.42f, 0.22f), new Vector2(0.42f, 0.4f) };
                return new List<Layer>
                {
                    new Layer(p => Polyline(p, coil, 0.08f), Green),
                    new Layer(p => Box(p, new Vector2(0f, -0.64f), new Vector2(0.58f, 0.09f)), Cream),
                    new Layer(p => Box(p, new Vector2(0f, 0.56f), new Vector2(0.58f, 0.09f)), Gold),
                };
            case PrizeType.Magnifier: // lupa con un signo de pregunta rojo
                return new List<Layer>
                {
                    new Layer(p => Segment(p, new Vector2(0.22f, -0.22f), new Vector2(0.66f, -0.66f), 0.11f), Night),
                    new Layer(p => Ring(p, new Vector2(-0.12f, 0.12f), 0.44f, 0.1f), Gold),
                    new Layer(p => Circle(p, new Vector2(-0.12f, 0.12f), 0.34f), LightBlue),
                    new Layer(p => Mathf.Min(Mathf.Max(Ring(p, new Vector2(-0.12f, 0.24f), 0.14f, 0.05f), -p.y + 0.16f),
                        Mathf.Min(Segment(p, new Vector2(-0.02f, 0.16f), new Vector2(-0.12f, 0.04f), 0.05f),
                            Circle(p, new Vector2(-0.12f, -0.08f), 0.055f))), Pink),
                };
            case PrizeType.Clover: // trébol de cuatro hojas con tallo
                return new List<Layer>
                {
                    new Layer(p => Segment(p, new Vector2(0.04f, -0.08f), new Vector2(0.3f, -0.72f), 0.07f), Green),
                    new Layer(p => Mathf.Min(Mathf.Min(Circle(p, new Vector2(-0.26f, 0.18f), 0.27f), Circle(p, new Vector2(0.26f, 0.18f), 0.27f)),
                        Mathf.Min(Circle(p, new Vector2(0f, 0.44f), 0.27f), Circle(p, new Vector2(0f, -0.08f), 0.27f))), Green),
                    new Layer(p => Star(p, new Vector2(0f, 0.18f), 0.2f, 0.09f), Gold),
                };
            case PrizeType.Tip: // moneda dorada con estrella
                return new List<Layer>
                {
                    new Layer(p => Circle(p, Vector2.zero, 0.64f), Gold),
                    new Layer(p => Ring(p, Vector2.zero, 0.5f, 0.04f), Cream),
                    new Layer(p => Star(p, Vector2.zero, 0.32f, 0.14f), White),
                };
            case PrizeType.Sneakers: // zapatilla con líneas de velocidad
                Vector2[] upper = { new Vector2(-0.5f, -0.22f), new Vector2(-0.46f, 0.26f), new Vector2(-0.08f, 0.3f),
                                    new Vector2(0.12f, 0.04f), new Vector2(0.64f, -0.06f), new Vector2(0.68f, -0.22f) };
                return new List<Layer>
                {
                    new Layer(p => Polygon(p, upper), Blue),
                    new Layer(p => Box(p, new Vector2(0.09f, -0.32f), new Vector2(0.62f, 0.1f)), White),
                    new Layer(p => Mathf.Min(Segment(p, new Vector2(-0.92f, 0.12f), new Vector2(-0.66f, 0.12f), 0.05f),
                        Segment(p, new Vector2(-0.92f, -0.1f), new Vector2(-0.7f, -0.1f), 0.05f)), Cream),
                };
            case PrizeType.Whistle: // silbato con ondas
                return new List<Layer>
                {
                    new Layer(p => Mathf.Min(Circle(p, new Vector2(0.14f, -0.14f), 0.42f), Box(p, new Vector2(-0.38f, 0.1f), new Vector2(0.3f, 0.13f))), Cream),
                    new Layer(p => Circle(p, new Vector2(0.14f, -0.14f), 0.13f), Night),
                    new Layer(p => Mathf.Min(Segment(p, new Vector2(0.42f, 0.42f), new Vector2(0.6f, 0.62f), 0.05f),
                        Segment(p, new Vector2(0.62f, 0.26f), new Vector2(0.86f, 0.36f), 0.05f)), Orange),
                };
            case PrizeType.Safe: // caja fuerte con perilla
                return new List<Layer>
                {
                    new Layer(p => Box(p, Vector2.zero, new Vector2(0.62f, 0.62f)), Blue),
                    new Layer(p => Box(p, Vector2.zero, new Vector2(0.48f, 0.48f)), LightBlue),
                    new Layer(p => Ring(p, Vector2.zero, 0.22f, 0.06f), Gold),
                    new Layer(p => Segment(p, Vector2.zero, new Vector2(0.12f, 0.12f), 0.05f), Night),
                };
            case PrizeType.BetInsurance: // paraguas
                return new List<Layer>
                {
                    new Layer(p => Mathf.Min(Segment(p, new Vector2(0f, 0.05f), new Vector2(0f, -0.55f), 0.06f),
                        Mathf.Max(Ring(p, new Vector2(0.14f, -0.55f), 0.14f, 0.06f), p.y + 0.55f)), Cream),
                    new Layer(p => Mathf.Max(Circle(p, new Vector2(0f, 0.05f), 0.66f), 0.05f - p.y), Pink),
                    new Layer(p => Star(p, new Vector2(0f, 0.32f), 0.18f, 0.08f), Gold),
                };
            case PrizeType.SuperMagnet: // imán con estrella dorada
                return new List<Layer>
                {
                    new Layer(p => Mathf.Min(Mathf.Max(Ring(p, new Vector2(0f, -0.08f), 0.42f, 0.15f), p.y + 0.08f),
                        Mathf.Min(Box(p, new Vector2(-0.42f, 0.22f), new Vector2(0.15f, 0.3f)), Box(p, new Vector2(0.42f, 0.22f), new Vector2(0.15f, 0.3f)))), Pink),
                    new Layer(p => Mathf.Min(Box(p, new Vector2(-0.42f, 0.52f), new Vector2(0.15f, 0.1f)), Box(p, new Vector2(0.42f, 0.52f), new Vector2(0.15f, 0.1f))), White),
                    new Layer(p => Star(p, new Vector2(0f, 0.18f), 0.26f, 0.11f), Gold),
                };
            case PrizeType.SecondChance: // corazón con flecha que da la vuelta
                return new List<Layer>
                {
                    new Layer(p => Mathf.Max(Ring(p, Vector2.zero, 0.8f, 0.06f), Mathf.Min(p.x, p.y)), LightBlue),
                    new Layer(p => Polygon(p, new[] { new Vector2(-0.04f, 0.98f), new Vector2(0.24f, 0.8f), new Vector2(-0.04f, 0.62f) }), LightBlue),
                    new Layer(p => Mathf.Min(Mathf.Min(Circle(p, new Vector2(-0.2f, 0.1f), 0.25f), Circle(p, new Vector2(0.2f, 0.1f), 0.25f)),
                        Polygon(p, new[] { new Vector2(-0.44f, 0.04f), new Vector2(0f, -0.5f), new Vector2(0.44f, 0.04f) })), Pink),
                };
            case PrizeType.DetectiveBadge: // placa en forma de estrella
                return new List<Layer>
                {
                    new Layer(p => Star(p, Vector2.zero, 0.74f, 0.4f), Gold),
                    new Layer(p => Circle(p, Vector2.zero, 0.24f), Blue),
                    new Layer(p => Star(p, Vector2.zero, 0.14f, 0.06f), White),
                };
            case PrizeType.RiggedContract: // contrato con flecha roja hacia abajo
                return new List<Layer>
                {
                    new Layer(p => Box(p, new Vector2(-0.12f, 0.04f), new Vector2(0.46f, 0.66f)), Cream),
                    new Layer(p => Mathf.Min(Mathf.Min(Box(p, new Vector2(-0.16f, 0.4f), new Vector2(0.28f, 0.035f)),
                        Box(p, new Vector2(-0.16f, 0.2f), new Vector2(0.28f, 0.035f))), Box(p, new Vector2(-0.22f, 0f), new Vector2(0.22f, 0.035f))), Night),
                    new Layer(p => Mathf.Min(Segment(p, new Vector2(0.44f, 0.12f), new Vector2(0.44f, -0.36f), 0.08f),
                        Polygon(p, new[] { new Vector2(0.2f, -0.3f), new Vector2(0.68f, -0.3f), new Vector2(0.44f, -0.72f) })), Pink),
                };
            default: // SlowMotion: reloj
                return new List<Layer>
                {
                    new Layer(p => Circle(p, Vector2.zero, 0.66f), Water),
                    new Layer(p => Circle(p, Vector2.zero, 0.54f), Cream),
                    new Layer(p => Mathf.Min(Segment(p, Vector2.zero, new Vector2(0f, 0.38f), 0.06f), Segment(p, Vector2.zero, new Vector2(0.28f, 0f), 0.06f)), Night),
                    new Layer(p => Circle(p, Vector2.zero, 0.09f), Night),
                };
        }
    }

    // ---------- Formas (distancia con signo; negativa = adentro) ----------

    private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

    private static float Ring(Vector2 p, Vector2 c, float r, float halfWidth) => Mathf.Abs((p - c).magnitude - r) - halfWidth;

    private static float Box(Vector2 p, Vector2 c, Vector2 half)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half;
        return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
    }

    private static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }

    private static float Polyline(Vector2 p, Vector2[] points, float r)
    {
        float d = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++) d = Mathf.Min(d, Segment(p, points[i], points[i + 1], r));
        return d;
    }

    private static float Polygon(Vector2 p, Vector2[] v)
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

    private static float Star(Vector2 p, Vector2 c, float outer, float inner)
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

    private static Vector2 Rotate(Vector2 p, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
    }

    private static Vector2[] Scale(Vector2[] points, float factor, Vector2 offset)
    {
        var result = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) result[i] = points[i] * factor + offset;
        return result;
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

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
