using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajusta la importación del audio para que la versión web cargue rápido:
/// la música se carga en segundo plano cuando suena (no al abrir la escena) y todo se comprime.
/// </summary>
public static class AudioImportOptimizer
{
    private const float MusicQuality = 0.4f;
    private const float EffectQuality = 0.6f;
    private const long LargeClipBytes = 300_000;   // voces y efectos largos

    [MenuItem("Una Ronda Más/Build/Optimizar audio para WebGL")]
    public static void Optimize()
    {
        int count = 0;
        long before = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { ProjectPaths.Root + "/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;

            long bytes = new FileInfo(path).Length;
            before += bytes;
            bool music = path.Contains("/Music/");
            bool large = music || bytes > LargeClipBytes;

            importer.forceToMono = !music;
            importer.loadInBackground = large;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = large ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? MusicQuality : EffectQuality;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            settings.preloadAudioData = !music;   // la música no frena la carga de la escena
            importer.defaultSampleSettings = settings;

            // En WebGL Unity usa AAC.
            AudioImporterSampleSettings web = settings;
            web.compressionFormat = AudioCompressionFormat.AAC;
            importer.SetOverrideSampleSettings("WebGL", web);

            importer.SaveAndReimport();
            count++;
        }
        Debug.Log($"Audio optimizado: {count} clips ({before / (1024f * 1024f):0.0} MB de archivos originales). " +
                  "Música en segundo plano y comprimida; efectos en mono.");
    }
}
