using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Aplica el hover al botón base y añade transiciones a MainMenu.</summary>
public static class UiPolishInstaller
{
    private const string ButtonPrefabPath = "Assets/_Project/Prefabs/UI/Button_Base.prefab";
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

    [MenuItem("Una Ronda Más/UI/Aplicar pulido de botones y transiciones")]
    public static void Install()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!InstallButtonHover()) return;
        if (!InstallMainMenuTransition()) return;
        AssetDatabase.SaveAssets();
        Debug.Log("Hover instalado en Button_Base y transición configurada en MainMenu.");
    }

    private static bool InstallButtonHover()
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(ButtonPrefabPath);
        if (prefabContents == null)
        {
            Debug.LogError($"No se pudo abrir el prefab base: {ButtonPrefabPath}");
            return false;
        }

        if (prefabContents.GetComponent<Selectable>() == null)
        {
            Debug.LogError("Button_Base necesita un componente Selectable o Button en la raíz.");
            PrefabUtility.UnloadPrefabContents(prefabContents);
            return false;
        }

        if (prefabContents.GetComponent<ButtonHoverEffect>() == null)
            prefabContents.AddComponent<ButtonHoverEffect>();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(prefabContents, ButtonPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabContents);
        if (saved == null)
        {
            Debug.LogError($"No se pudo guardar Button_Base: {ButtonPrefabPath}");
            return false;
        }
        return true;
    }

    private static bool InstallMainMenuTransition()
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath);
        MainMenuUI menu = Object.FindObjectOfType<MainMenuUI>();
        if (menu == null)
        {
            Debug.LogError("No se encontró MainMenuUI en MainMenu.unity.");
            return false;
        }

        SerializedObject serializedMenu = new SerializedObject(menu);
        SerializedProperty overlayProperty = serializedMenu.FindProperty("transitionOverlay");
        if (overlayProperty == null)
        {
            Debug.LogError("No se encontró el campo transitionOverlay en MainMenuUI.");
            return false;
        }

        CanvasGroup transitionOverlay;
        if (overlayProperty.objectReferenceValue != null)
        {
            transitionOverlay = (CanvasGroup)overlayProperty.objectReferenceValue;
        }
        else
        {
            Transform existing = menu.transform.Find("TransitionOverlay");
            if (existing != null) transitionOverlay = existing.GetComponent<CanvasGroup>();
            else
            {
                var overlayObject = new GameObject("TransitionOverlay",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                overlayObject.transform.SetParent(menu.transform, false);
                overlayObject.transform.SetAsLastSibling();

                RectTransform rect = overlayObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = overlayObject.GetComponent<Image>();
                image.color = Color.black;
                image.raycastTarget = true;
                transitionOverlay = overlayObject.GetComponent<CanvasGroup>();
                transitionOverlay.alpha = 1f;
                transitionOverlay.blocksRaycasts = true;
                transitionOverlay.interactable = false;
            }
        }

        if (transitionOverlay == null)
        {
            Debug.LogError("TransitionOverlay existe pero no tiene CanvasGroup.", menu);
            return false;
        }

        overlayProperty.objectReferenceValue = transitionOverlay;
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        return EditorSceneManager.SaveScene(scene);
    }
}
