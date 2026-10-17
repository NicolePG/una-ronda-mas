using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Importa los TMP Essential Resources (Fase 0, paso 5) si todavía no están en el proyecto.</summary>
public static class TmpEssentialsImporter
{
    public const string TmpFolder = "Assets/TextMesh Pro";
    private const string PackagePath = "Packages/com.unity.textmeshpro";
    private const string EssentialsFile = "Package Resources/TMP Essential Resources.unitypackage";

    public static bool IsImported => AssetDatabase.IsValidFolder(TmpFolder + "/Resources");

    [MenuItem("Una Ronda Más/Importar TMP Essentials")]
    public static void ImportMenu()
    {
        if (IsImported) Debug.Log("TMP Essentials ya están importados.");
        else Import();
    }

    public static bool Import()
    {
        if (IsImported) return true;
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackagePath);
        if (info == null)
        {
            Debug.LogError("No está instalado el paquete TextMeshPro (Window → Package Manager).");
            return false;
        }
        string path = Path.Combine(info.resolvedPath, EssentialsFile);
        if (!File.Exists(path))
        {
            Debug.LogError("No se encontró " + path);
            return false;
        }
        // Se importa en segundo plano. En modo batch Unity se cierra antes: tools/preparar-todo.ps1 usa -importPackage.
        AssetDatabase.ImportPackage(path, false);
        Debug.Log("Importando TMP Essentials en " + TmpFolder + "...");
        return true;
    }
}
