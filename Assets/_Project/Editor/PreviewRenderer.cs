using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Capturas en PNG sin entrar en Play: los personajes de Kenney (para elegir) y la vista de Game.unity.
/// Las imágenes quedan en Logs/Previews (fuera de Assets, no se suben).
/// </summary>
public static class PreviewRenderer
{
    public const string OutputFolder = "Logs/Previews";
    private const int ThumbSize = 256;

    [MenuItem("Una Ronda Más/Pruebas/Capturar personajes de Kenney")]
    public static void RenderKenneyCharacters()
    {
        Directory.CreateDirectory(OutputFolder);
        var report = new StringBuilder("Personajes de Kenney:\n");
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { KenneyCharacterImporter.Folder.TrimEnd('/') });
        var sheet = new Texture2D(ThumbSize * 2 * 6, ThumbSize * 2, TextureFormat.RGB24, false);
        int index = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject go = Object.Instantiate(model);
            go.transform.position = Vector3.zero;
            Bounds bounds = GetBounds(go);
            report.Append($"- {model.name}: alto {bounds.size.y:0.00} m, ancho {bounds.size.x:0.00}, centro {bounds.center}. Clips: ");
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    report.Append($"{clip.name} ({clip.length:0.00}s{(clip.isLooping ? ", bucle" : "")}) ");
            }
            report.AppendLine();

            // Dos vistas: desde −Z (detrás si mira a +Z) y desde +Z.
            for (int view = 0; view < 2; view++)
            {
                float side = view == 0 ? -1f : 1f;
                Vector3 target = bounds.center;
                Vector3 eye = target + new Vector3(0.35f * side, 0.15f, 2.6f * side) * Mathf.Max(bounds.size.y, 0.5f);
                Texture2D shot = RenderView(eye, target, ThumbSize, ThumbSize, new Color(0.14f, 0.1f, 0.25f), 30f);
                int col = index % 6, row = index / 6;
                sheet.SetPixels((col * 2 + view) * ThumbSize, (1 - row) * ThumbSize, ThumbSize, ThumbSize, shot.GetPixels());
                Object.DestroyImmediate(shot);
            }
            Object.DestroyImmediate(go);
            index++;
        }

        sheet.Apply();
        File.WriteAllBytes(Path.Combine(OutputFolder, "kenney_characters.png"), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        report.Append("Hoja: Logs/Previews/kenney_characters.png (por personaje: izquierda vista desde −Z, derecha desde +Z; orden alfabético).");
        Debug.Log(report.ToString());
    }

    /// <summary>Renderiza la escena actual desde un punto a una textura (la cámara se crea y se borra).</summary>
    public static Texture2D RenderView(Vector3 eye, Vector3 target, int width, int height, Color background, float fov)
    {
        var camGo = new GameObject("PreviewCamera");
        var camera = camGo.AddComponent<Camera>();
        camera.transform.position = eye;
        camera.transform.LookAt(target);
        camera.fieldOfView = fov;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        var lightGo = new GameObject("PreviewLight");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        AmbientMode oldMode = RenderSettings.ambientMode;
        Color oldAmbient = RenderSettings.ambientLight;
        bool oldFog = RenderSettings.fog;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);
        RenderSettings.fog = false;

        Texture2D result = Capture(camera, width, height);

        RenderSettings.ambientMode = oldMode;
        RenderSettings.ambientLight = oldAmbient;
        RenderSettings.fog = oldFog;
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(lightGo);
        return result;
    }

    /// <summary>Renderiza una cámara existente a una textura.</summary>
    public static Texture2D Capture(Camera camera, int width, int height)
    {
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        rt.Release();
        Object.DestroyImmediate(rt);
        return texture;
    }

    public static Bounds GetBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }
}
