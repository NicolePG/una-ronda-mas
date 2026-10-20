using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

/// <summary>
/// Pulido visual y emoción de Game.unity (Otoniel): post-procesado (bloom, color y viñeta), reflejos con el
/// HDRI del estudio (Poly Haven, CC0), líneas de velocidad, textos flotantes, racha de estrellas y el lienzo de
/// RoundDrama. Lo llama GameSceneCreator al armar la escena.
/// </summary>
public static class GameFeelCreator
{
    private const string ProfilePath = ProjectPaths.Data + "/PostProcess_Game.asset";
    private const string ResourcesPath = "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";
    private const string HdriPath = StudioAssetImporter.HdriFolder + "tv_studio_1k.hdr";
    private const string VignettePath = ProjectPaths.Art + "/UI/Drama_Vignette.png";
    private const string FontPath = ProjectPaths.Root + "/Fonts/Font_UI_Dynamic.asset";

    private static readonly Color Gold = IconPainter.Hex("#F7B11D");
    private static readonly Color Red = IconPainter.Hex("#D13B3B");
    private static readonly Color Night = IconPainter.Hex("#24193F");

    /// <summary>Agrega todo a la escena abierta.</summary>
    public static void Build(Camera camera, Transform cameraRig, ContestantController contestant, ParticleSystem[] cannons)
    {
        SetupPostProcessing(camera);
        SetupReflections();
        CreateSpeedLines(cameraRig);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning($"Falta la fuente {FontPath}: sin textos flotantes ni cartel de racha.");
        else
        {
            CreateFloatingTexts(font);
            CreateDramaCanvas(font, cannons);
        }
        var streak = new GameObject("StarStreak").AddComponent<StarStreak>();
        if (contestant != null) streak.EditorSetup(contestant);
    }

    // ---------- Post-procesado ----------

    private static void SetupPostProcessing(Camera camera)
    {
        var resources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(ResourcesPath);
        if (resources == null)
        {
            Debug.LogWarning("No encuentro PostProcessResources (paquete com.unity.postprocessing): sin post-procesado.");
            return;
        }
        camera.allowHDR = true;
        var layer = camera.gameObject.AddComponent<PostProcessLayer>();
        layer.Init(resources);
        layer.volumeTrigger = camera.transform;
        layer.volumeLayer = 1 << camera.gameObject.layer;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        layer.fastApproximateAntialiasing.fastMode = true;   // liviano para WebGL

        var volumeGo = new GameObject("PostProcessVolume") { layer = camera.gameObject.layer };
        var volume = volumeGo.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = Profile();
    }

    private static PostProcessProfile Profile()
    {
        AssetDatabase.DeleteAsset(ProfilePath);
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        var bloom = profile.AddSettings<Bloom>();
        bloom.intensity.Override(1.6f);
        bloom.threshold.Override(1.05f);
        bloom.softKnee.Override(0.6f);
        bloom.diffusion.Override(7f);
        bloom.fastMode.Override(true);

        // LDR: funciona en WebGL (el modo HDR necesita compute shaders).
        var grading = profile.AddSettings<ColorGrading>();
        grading.gradingMode.Override(GradingMode.LowDefinitionRange);
        grading.saturation.Override(14f);
        grading.contrast.Override(12f);
        grading.temperature.Override(4f);

        var vignette = profile.AddSettings<Vignette>();
        vignette.intensity.Override(0.32f);
        vignette.smoothness.Override(0.42f);
        vignette.color.Override(new Color(0.07f, 0.03f, 0.14f));

        foreach (PostProcessEffectSettings settings in profile.settings) AssetDatabase.AddObjectToAsset(settings, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // ---------- Reflejos con el HDRI del estudio ----------

    private static void SetupReflections()
    {
        AssetDatabase.ImportAsset(HdriPath, ImportAssetOptions.ForceUpdate);
        var cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(HdriPath);
        if (cubemap == null)
        {
            Debug.LogWarning($"No encuentro el HDRI {HdriPath}: reflejos por defecto.");
            return;
        }
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = cubemap;
        RenderSettings.reflectionIntensity = 0.45f;
    }

    // ---------- Líneas de velocidad ----------

    private static void CreateSpeedLines(Transform rig)
    {
        Material material = ObstaclePrefabCreator.ParticleMaterial("Mat_FX_SpeedLine", "trace_02.png", true);
        var root = new GameObject("SpeedLines").transform;
        root.SetParent(rig, false);
        // Hijo del soporte de la cámara pero en coordenadas del mundo (las líneas viajan hacia −Z).
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.Inverse(rig.rotation);
        var emitters = new ParticleSystem[2];
        for (int i = 0; i < emitters.Length; i++)
        {
            float side = i == 0 ? -1f : 1f;
            var go = new GameObject(side < 0f ? "Left" : "Right");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(side * 6.5f, -1.5f, 34f);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // emite hacia −Z
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 1.3f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.35f), new Color(0.75f, 0.85f, 1f, 0.6f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5f, 5f, 2f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 2f;
            if (material != null) renderer.sharedMaterial = material;
            emitters[i] = ps;
        }
        root.gameObject.AddComponent<SpeedLines>().EditorSetup(emitters);
    }

    // ---------- Textos flotantes ----------

    private static void CreateFloatingTexts(TMP_FontAsset font)
    {
        var root = new GameObject("FloatingTexts");
        var templateGo = new GameObject("Template");
        templateGo.transform.SetParent(root.transform, false);
        var text = templateGo.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSharedMaterial = StudioSetCreator.OutlineFontMaterial(font);
        text.fontSize = 7f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
        text.text = "+1";
        templateGo.SetActive(false);
        root.AddComponent<FloatingTexts>().EditorSetup(text);
    }

    // ---------- Lienzo de emoción (RoundDrama) ----------

    private static void CreateDramaCanvas(TMP_FontAsset font, ParticleSystem[] cannons)
    {
        var root = new GameObject("RoundDrama_Canvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;   // sobre el HUD y debajo de los anuncios de Tito
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        Image flash = FullScreen(root.transform, "HitFlash", null, Red);
        Image vignette = FullScreen(root.transform, "DangerVignette", VignetteSprite(), Red);

        // Cuenta regresiva debajo del reloj del HUD (no tapa la pista donde aparecen los obstáculos).
        TMP_Text countdown = Text(root.transform, "Countdown", font, "5", 120f, Red);
        RectTransform countdownRect = countdown.rectTransform;
        countdownRect.anchorMin = countdownRect.anchorMax = new Vector2(1f, 1f);
        countdownRect.anchoredPosition = new Vector2(-119f, -215f);
        countdownRect.sizeDelta = new Vector2(190f, 150f);

        var banner = new GameObject("StreakBanner", typeof(RectTransform)).GetComponent<RectTransform>();
        banner.SetParent(root.transform, false);
        banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 0.86f);
        banner.sizeDelta = new Vector2(700f, 140f);
        TMP_Text title = Text(banner, "Title", font, "¡RACHA ×5!", 72f, Gold);
        title.rectTransform.anchoredPosition = new Vector2(0f, 22f);
        title.rectTransform.sizeDelta = new Vector2(700f, 90f);
        TMP_Text bonus = Text(banner, "Bonus", font, "+1 estrella", 34f, Color.white);
        bonus.rectTransform.anchoredPosition = new Vector2(0f, -42f);
        bonus.rectTransform.sizeDelta = new Vector2(700f, 44f);

        // Contador chico de la racha, debajo del panel de estrellas del HUD.
        var chip = new GameObject("StreakChip", typeof(RectTransform)).GetComponent<RectTransform>();
        chip.SetParent(root.transform, false);
        chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(0f, 1f);
        chip.anchoredPosition = new Vector2(24f, -184f);
        chip.sizeDelta = new Vector2(180f, 40f);
        var chipBackground = chip.gameObject.AddComponent<Image>();
        chipBackground.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectPaths.Art + "/UI/Hud_RuleChip.png");
        chipBackground.type = Image.Type.Sliced;
        chipBackground.color = chipBackground.sprite != null ? Color.white : new Color(Night.r, Night.g, Night.b, 0.85f);
        chipBackground.raycastTarget = false;
        var chipGroup = chip.gameObject.AddComponent<CanvasGroup>();
        chipGroup.blocksRaycasts = false;
        chipGroup.interactable = false;
        TMP_Text chipText = Text(chip, "Text", font, "RACHA ×3", 24f, Gold);
        chipText.rectTransform.anchorMin = Vector2.zero;
        chipText.rectTransform.anchorMax = Vector2.one;
        chipText.rectTransform.sizeDelta = Vector2.zero;

        root.AddComponent<RoundDrama>().EditorSetup(flash, vignette, countdown, banner, title, bonus, chipGroup, chipText, cannons);
    }

    private static Image FullScreen(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        color.a = 0f;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, string value, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSharedMaterial = StudioSetCreator.OutlineFontMaterial(font);
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    // Borde que se oscurece hacia las esquinas (centro transparente).
    private static Sprite VignetteSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
        if (existing != null) return existing;
        Texture2D texture = IconPainter.Paint(256, 256, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
            float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
            float a = Mathf.Clamp01((d - 0.7f) / 0.55f);
            return new Color(1f, 1f, 1f, a * a);
        });
        return IconPainter.Save(VignettePath, texture, Vector4.zero);
    }
}
