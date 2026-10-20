using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajustes de importación de los recursos externos del estudio:
/// Art/HDRI (Poly Haven, CC0) como cubemap para los reflejos y Art/Particles (Kenney Particle Pack, CC0)
/// como texturas de partículas en fondo negro (se usan con un shader aditivo).
/// </summary>
public class StudioAssetImporter : AssetPostprocessor
{
    public const string HdriFolder = "Assets/_Project/Art/HDRI/";
    public const string ParticlesFolder = "Assets/_Project/Art/Particles/";

    public override uint GetVersion() => 2;

    private void OnPreprocessTexture()
    {
        var importer = (TextureImporter)assetImporter;
        if (assetPath.StartsWith(HdriFolder))
        {
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 512;   // solo reflejos: liviano para WebGL
            // Mipmaps borrosos para que los materiales menos brillantes reflejen suave.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            importer.SetTextureSettings(settings);
        }
        else if (assetPath.StartsWith(ParticlesFolder))
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
            importer.alphaIsTransparency = true;   // sin bordes oscuros
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 256;
        }
    }
}
