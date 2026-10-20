using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal con el estudio de TV en 3D detrás (Otoniel, sobre la escena de Tania): Tito en su tarima y la
/// concursante saludando, público, cartel, haces de luz y confeti, con la cámara meciéndose. La UI pasa a una
/// columna a la izquierda con el logo animado. Modifica MainMenu.unity en su lugar: no rompe las referencias de
/// MainMenuUI y se puede repetir.
/// </summary>
public static class MainMenuStudioCreator
{
    private const string ScenePath = ProjectPaths.Scenes + "/MainMenu.unity";
    private const string StageRootName = "MenuStage3D";
    private const string FontPath = ProjectPaths.Root + "/Fonts/Font_UI_Dynamic.asset";
    private const string SideFadePath = ProjectPaths.Art + "/UI/Menu_SideFade.png";
    private const string PrimaryButtonPath = ProjectPaths.Art + "/UI/Button_Primary.png";
    private const string CardPath = ProjectPaths.Art + "/UI/Panel_Card.png";

    private static readonly Vector3 CameraPosition = new Vector3(-2.2f, 2.3f, -3.8f);
    private static readonly Vector3 CameraTarget = new Vector3(0.7f, 1.2f, 7.8f);
    private static readonly Vector3 ContestantPosition = new Vector3(1.4f, 0f, 1.6f);
    private static readonly Vector3 PodiumPosition = new Vector3(5.6f, 0f, 5.6f);
    private const float PodiumTop = 0.35f;
    private const float PodiumHeight = 0.9f;
    private const float WaterHeight = -0.6f;
    private const float PoolFloorHeight = -2.5f;

    private static readonly Color Studio = IconPainter.Hex("#24193F");
    private static readonly Color Gold = IconPainter.Hex("#F7B11D");
    private static readonly Color StarLight = IconPainter.Hex("#FBD97A");
    private static readonly Color DeepPurple = IconPainter.Hex("#2A1650");

    [MenuItem("Una Ronda Más/UI/Menú con el estudio 3D (Otoniel)")]
    public static void CreateMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Create();
    }

    public static void Create()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == StageRootName || root.name == "Studio" || root.name == "PostProcessVolume")
                Object.DestroyImmediate(root);
        }
        Build3D();
        var menu = Object.FindObjectOfType<MainMenuUI>();
        if (menu != null) RestyleUi(menu.transform);
        else Debug.LogError("MainMenu.unity no tiene MainMenuUI: solo se armó el fondo 3D.");
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Menú con el estudio 3D listo (MainMenu.unity).");
    }

    // ---------- Fondo 3D ----------

    private static void Build3D()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.5f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Studio;
        RenderSettings.fogStartDistance = 50f;
        RenderSettings.fogEndDistance = 115f;

        Transform root = new GameObject(StageRootName).transform;

        var lightGo = new GameObject("Directional Light");
        lightGo.transform.SetParent(root, false);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var sun = lightGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.8f;
        sun.color = new Color(1f, 0.96f, 0.9f);
        sun.shadows = LightShadows.Soft;

        // Cámara (sin AudioListener: lo pone el AudioManager).
        var cameraGo = new GameObject("MenuCamera") { tag = "MainCamera" };
        cameraGo.transform.SetParent(root, false);
        cameraGo.transform.position = CameraPosition;
        cameraGo.transform.LookAt(CameraTarget);
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Studio;
        camera.farClipPlane = 200f;
        cameraGo.AddComponent<MenuCameraDrift>();
        GameFeelCreator.SetupPostProcessing(camera);
        GameFeelCreator.SetupReflections();

        CreatePool(root);
        PlaceSegment(root, SegmentCreator.SafePath, -4f);
        PlaceSegment(root, ProjectPaths.PrefabsSegments + "/Segment_Easy_01_Libre.prefab", 16f);
        PlaceSegment(root, ProjectPaths.PrefabsSegments + "/Segment_Mid_03_Martillo.prefab", 36f);
        PlaceSegment(root, ProjectPaths.PrefabsSegments + "/Segment_Easy_02_Zigzag.prefab", 56f);

        // La concursante y Tito en su tarima, mirando a la cámara.
        Animator contestant = StudioSetCreator.Character(root, CharacterPrefabCreator.ContestantModel, ContestantPosition,
            YawTowardCamera(ContestantPosition), 1.6f);
        Material podium = GameMaterials.GetOrCreate("Mat_Podium", "#33255A");
        Transform stand = new GameObject("TitoPodium").transform;
        stand.SetParent(root, false);
        stand.position = PodiumPosition;
        ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, stand, "Base", new Vector3(0f, PodiumTop - PodiumHeight * 0.5f, 0f),
            new Vector3(2.6f, PodiumHeight * 0.5f, 2.6f), podium);
        ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, stand, "GoldRing", new Vector3(0f, PodiumTop - 0.02f, 0f),
            new Vector3(2.7f, 0.03f, 2.7f), MaterialCreator.Load("Mat_Gold"));
        Vector3 titoPosition = PodiumPosition + Vector3.up * PodiumTop;
        Animator tito = StudioSetCreator.Character(root, CharacterPrefabCreator.HostModel, titoPosition,
            YawTowardCamera(titoPosition), 2f);

        Spot(root, "SpotContestant", ContestantPosition + new Vector3(-1.5f, 6f, -2f), ContestantPosition + Vector3.up, new Color(1f, 0.92f, 0.8f));
        Spot(root, "SpotTito", titoPosition + new Vector3(-1f, 6f, -2.5f), titoPosition + Vector3.up, Gold);

        ParticleSystem[] cannons = StudioSetCreator.Build();
        root.gameObject.AddComponent<MenuStage>().EditorSetup(new[] { contestant, tito }, cannons);
    }

    private static float YawTowardCamera(Vector3 from)
    {
        Vector3 look = CameraPosition - from;
        look.y = 0f;
        return Quaternion.LookRotation(look).eulerAngles.y;
    }

    private static void CreatePool(Transform root)
    {
        const float width = 20f, length = 160f, centerZ = 50f;
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.SetParent(root, false);
        water.transform.position = new Vector3(0f, WaterHeight, centerZ);
        water.transform.localScale = new Vector3(width / 10f, 1f, length / 10f);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        var renderer = water.GetComponent<Renderer>();
        renderer.sharedMaterial = StudioSetCreator.WaterMaterial();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        water.AddComponent<WaterSurface>();

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "PoolFloor";
        floor.transform.SetParent(root, false);
        floor.transform.position = new Vector3(0f, PoolFloorHeight, centerZ);
        floor.transform.localScale = new Vector3(width / 10f, 1f, length / 10f);
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.GetComponent<Renderer>().sharedMaterial = StudioSetCreator.PoolFloorMaterial();
    }

    private static void PlaceSegment(Transform root, string path, float z)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        go.transform.position = new Vector3(0f, 0f, z);
    }

    private static void Spot(Transform root, string name, Vector3 position, Vector3 target, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = position;
        go.transform.LookAt(target);
        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = 1.4f;
        light.range = 14f;
        light.spotAngle = 38f;
    }

    // ---------- UI: columna a la izquierda ----------

    private static void RestyleUi(Transform canvas)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Material outline = font != null ? StudioSetCreator.OutlineFontMaterial(font) : null;

        // El fondo dibujado se reemplaza por el estudio 3D.
        foreach (Transform child in canvas)
        {
            if (child.name == "Background" || child.name == "StudioBackground") child.gameObject.SetActive(false);
        }

        RectTransform panel = canvas.Find("MenuPanel") as RectTransform;
        if (panel == null)
        {
            Debug.LogError("MainMenu: no encuentro MenuPanel.");
            return;
        }
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(640f, 0f);
        var panelImage = panel.GetComponent<Image>();
        panelImage.sprite = SideFadeSprite();
        panelImage.type = Image.Type.Simple;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;

        // Logo en dos líneas, inclinado y latiendo.
        TMP_Text title = Find<TMP_Text>(panel, "Title");
        if (title != null)
        {
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(340f, -140f), new Vector2(580f, 190f));
            title.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            title.text = "¡UNA RONDA\nMÁS!";
            title.enableAutoSizing = false;
            title.enableWordWrapping = false;
            title.fontSize = 78f;
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = -18f;
            title.alignment = TextAlignmentOptions.Left;
            title.color = Gold;
            if (outline != null) title.fontSharedMaterial = outline;
            if (title.GetComponent<TitlePulse>() == null) title.gameObject.AddComponent<TitlePulse>();
        }

        TMP_Text tagline = Find<TMP_Text>(panel, "Tagline");
        if (tagline == null && font != null) tagline = NewText(panel, "Tagline", font);
        if (tagline != null)
        {
            Place(tagline.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(66f, -240f), new Vector2(520f, 60f));
            tagline.text = "El concurso donde las reglas cambian… ¡y Tito miente!";
            tagline.fontSize = 22f;
            tagline.enableAutoSizing = false;
            tagline.enableWordWrapping = true;
            tagline.alignment = TextAlignmentOptions.TopLeft;
            tagline.color = new Color(1f, 1f, 1f, 0.85f);
        }

        Sprite primary = PrimaryButtonSprite();
        StyleButton(panel, "PlayButton", new Vector2(60f, -312f), new Vector2(430f, 90f), 40f, primary, DeepPurple);
        StyleButton(panel, "HowToPlayButton", new Vector2(60f, -424f), new Vector2(360f, 60f), 28f, null, Color.white);
        StyleButton(panel, "OptionsButton", new Vector2(60f, -496f), new Vector2(360f, 60f), 28f, null, Color.white);
        StyleButton(panel, "CreditsButton", new Vector2(60f, -568f), new Vector2(360f, 60f), 28f, null, Color.white);

        TMP_Text status = Find<TMP_Text>(panel, "Status");
        if (status != null)
        {
            Place(status.rectTransform, Vector2.zero, Vector2.zero, new Vector2(64f, 22f), new Vector2(540f, 40f));
            status.alignment = TextAlignmentOptions.BottomLeft;
            status.fontSizeMax = 20f;
        }

        int afterPanel = panel.GetSiblingIndex() + 1;

        // Récords arriba a la derecha, en una tarjeta.
        TMP_Text records = Find<TMP_Text>(canvas, "Records");
        if (records != null)
        {
            RectTransform chip = canvas.Find("RecordsChip") as RectTransform;
            if (chip == null)
            {
                chip = new GameObject("RecordsChip", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                chip.SetParent(canvas, false);
            }
            chip.SetSiblingIndex(afterPanel);
            Place(chip, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(360f, 86f));
            var chipImage = chip.GetComponent<Image>();
            chipImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardPath);
            chipImage.type = Image.Type.Sliced;
            chipImage.color = new Color(1f, 1f, 1f, 0.92f);
            chipImage.raycastTarget = false;
            records.rectTransform.SetParent(chip, false);
            records.rectTransform.anchorMin = Vector2.zero;
            records.rectTransform.anchorMax = Vector2.one;
            records.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            records.rectTransform.anchoredPosition = Vector2.zero;
            records.rectTransform.sizeDelta = new Vector2(-36f, -18f);
            records.alignment = TextAlignmentOptions.Center;
            records.fontSizeMax = 24f;
            records.color = StarLight;
        }

        // Pie con el nombre de la jam.
        TMP_Text jam = Find<TMP_Text>(canvas, "JamLabel");
        if (jam == null && font != null)
        {
            jam = NewText(canvas, "JamLabel", font);
            jam.transform.SetSiblingIndex(afterPanel);
        }
        if (jam != null)
        {
            Place(jam.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 16f), new Vector2(460f, 28f));
            jam.text = "GameJamPlus 26/27 · One More Time";
            jam.fontSize = 16f;
            jam.alignment = TextAlignmentOptions.BottomRight;
            jam.color = new Color(1f, 1f, 1f, 0.55f);
        }
    }

    private static void StyleButton(Transform panel, string name, Vector2 position, Vector2 size, float fontSize, Sprite sprite, Color labelColor)
    {
        var rect = panel.Find(name) as RectTransform;
        if (rect == null) return;
        Place(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        var image = rect.GetComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontStyle = sprite != null ? FontStyles.Bold : FontStyles.Normal;
            label.color = labelColor;
        }
        // Los botones son instancias de Button_Base: sin esto los cambios no se guardan en la escena.
        if (!PrefabUtility.IsPartOfPrefabInstance(rect)) return;
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        if (image != null) PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        if (label != null) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static T Find<T>(Transform parent, string name) where T : Component
    {
        foreach (T component in parent.GetComponentsInChildren<T>(true))
        {
            if (component.name == name) return component;
        }
        return null;
    }

    private static TMP_Text NewText(Transform parent, string name, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.raycastTarget = false;
        return text;
    }

    // Franja oscura a la izquierda que se desvanece hacia el estudio.
    private static Sprite SideFadeSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SideFadePath);
        if (existing != null) return existing;
        Color dark = IconPainter.Hex("#140C2E");
        return IconPainter.Save(SideFadePath, IconPainter.Paint(256, 4, (u, v) =>
        {
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.45f) / 0.55f));
            return new Color(dark.r, dark.g, dark.b, Mathf.Lerp(0.88f, 0f, fade));
        }), Vector4.zero);
    }

    // Botón principal dorado (los demás quedan con el estilo morado de Tania).
    private static Sprite PrimaryButtonSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PrimaryButtonPath);
        if (existing != null) return existing;
        return IconPainter.Save(PrimaryButtonPath,
            IconPainter.Panel(160, 80, 26f, 4f, Gold, IconPainter.Hex("#FFE7A3"), 6f, new Color(0.1f, 0.05f, 0.2f, 0.6f)),
            new Vector4(32f, 36f, 32f, 32f));
    }
}
