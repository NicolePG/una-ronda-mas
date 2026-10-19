using UnityEditor;
using UnityEngine;

/// <summary>Materiales extra de la escena Game (Otoniel). No pisa los que ya existen.</summary>
public static class GameMaterials
{
    public const string NoFogShader = "Una Ronda Mas/Standard sin niebla";
    public const string NoFogFolder = ProjectPaths.Materials + "/NoFog";

    /// <summary>Material Standard opaco con un color de la paleta.</summary>
    public static Material GetOrCreate(string name, string hex, float emission = 0f, float smoothness = 0.3f)
    {
        string path = $"{ProjectPaths.Materials}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        ProjectPaths.EnsureFolder(ProjectPaths.Materials);
        ColorUtility.TryParseHtmlString(hex, out Color color);
        material = new Material(Shader.Find("Standard")) { name = name, color = color };
        material.SetFloat("_Glossiness", smoothness);
        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>Material sin niebla (para lo que está al fondo, como la pantalla de Tito).</summary>
    public static Material GetOrCreateNoFog(string name, Color color, float emission)
    {
        Shader shader = Shader.Find(NoFogShader);
        if (shader == null)
        {
            Debug.LogError($"No encuentro el shader «{NoFogShader}» (Art/Shaders/StandardNoFog.shader).");
            return null;
        }
        ProjectPaths.EnsureFolder(NoFogFolder);
        string path = $"{NoFogFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.color = color;
        material.SetColor("_EmissionColor", color * emission);
        EditorUtility.SetDirty(material);
        return material;
    }

    public static Material GetOrCreateNoFog(string name, string hex, float emission)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return GetOrCreateNoFog(name, color, emission);
    }

    /// <summary>Copia sin niebla de un material existente (mismo color y textura).</summary>
    public static Material NoFogCopyOf(Material source, float emission)
    {
        if (source == null) return null;
        Color color = source.HasProperty("_Color") ? source.color : Color.white;
        Material copy = GetOrCreateNoFog(source.name + "_SinNiebla", color, emission);
        if (copy != null && source.HasProperty("_MainTex")) copy.mainTexture = source.mainTexture;
        return copy;
    }
}
