using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Crea los materiales de la paleta (Standard). No pisa los que ya existen.</summary>
public static class MaterialCreator
{
    private const float WaterAlpha = 0.6f;
    private const float HologramAlpha = 0.35f;
    private const float GoldEmission = 0.35f;
    private const float StarLightEmission = 0.25f;

    [MenuItem("Una Ronda Más/Materiales/Crear materiales de la paleta")]
    public static void CreateAll()
    {
        ProjectPaths.EnsureFolder(ProjectPaths.Materials);
        Create("Mat_Red", "#D13B3B");
        Create("Mat_Gold", "#F7B11D", emission: GoldEmission);
        Create("Mat_StarLight", "#FBD97A", emission: StarLightEmission);
        Create("Mat_Player", "#2A78D4");
        Create("Mat_Water", "#4FA3E0", alpha: WaterAlpha);
        Create("Mat_Hologram", "#D13B3B", alpha: HologramAlpha);
        Create("Mat_Track", "#E9E6DF");
        Create("Mat_Grey", "#8C8C99");
        AssetDatabase.SaveAssets();
    }

    public static Material Load(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{ProjectPaths.Materials}/{name}.mat");

    private static void Create(string name, string hex, float alpha = 1f, float emission = 0f)
    {
        string path = $"{ProjectPaths.Materials}/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;

        ColorUtility.TryParseHtmlString(hex, out Color color);
        color.a = alpha;
        var material = new Material(Shader.Find("Standard")) { name = name, color = color };
        if (alpha < 1f) SetFadeMode(material);
        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(material, path);
    }

    // Igual que elegir Rendering Mode = Fade en el Inspector del shader Standard.
    private static void SetFadeMode(Material material)
    {
        material.SetFloat("_Mode", 2f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }
}
