using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajustes de importación de los personajes de Kenney (Art/Characters/KenneyMini, CC0):
/// rig Generic con avatar propio, clips en bucle para correr/estar quieto y textura de paleta sin filtrado.
/// </summary>
public class KenneyCharacterImporter : AssetPostprocessor
{
    public const string Folder = "Assets/_Project/Art/Characters/KenneyMini/";

    // Clips que se repiten mientras dura el estado.
    private static readonly string[] LoopingClips = { "idle", "walk", "sprint", "crouch", "fall", "static", "sit" };

    private bool IsKenneyAsset => assetPath.StartsWith(Folder);

    private void OnPreprocessModel()
    {
        if (!IsKenneyAsset) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
    }

    private void OnPreprocessAnimation()
    {
        if (!IsKenneyAsset) return;
        var importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loopTime = System.Array.IndexOf(LoopingClips, clip.name) >= 0;
        }
        importer.clipAnimations = clips;
    }

    private void OnPreprocessTexture()
    {
        if (!IsKenneyAsset) return;
        // colormap.png es una paleta: sin filtrado ni mipmaps para que los colores no se mezclen.
        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
