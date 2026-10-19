using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea los obstáculos inflables (Prefabs/Obstacles), el holograma, el arco de meta y las partículas (Prefabs/Effects).
/// Todo con primitivas de Unity y los materiales de la paleta. Se puede repetir: vuelve a generar los prefabs.
/// </summary>
public static class ObstaclePrefabCreator
{
    public const string BlockPath = ProjectPaths.PrefabsObstacles + "/Obstacle_Block.prefab";
    public const string HologramPath = ProjectPaths.PrefabsObstacles + "/Obstacle_Block_Holo.prefab";
    public const string LowBarPath = ProjectPaths.PrefabsObstacles + "/Obstacle_LowBar.prefab";
    public const string HighBarPath = ProjectPaths.PrefabsObstacles + "/Obstacle_HighBar.prefab";
    public const string HammerPath = ProjectPaths.PrefabsObstacles + "/Obstacle_Hammer.prefab";
    public const string EffectsFolder = ProjectPaths.Root + "/Prefabs/Effects";
    public const string SplashPath = EffectsFolder + "/FX_Splash.prefab";
    public const string ConfettiPath = EffectsFolder + "/FX_Confetti.prefab";
    public const string SparklePath = EffectsFolder + "/FX_StarSparkle.prefab";
    private const string FontPath = ProjectPaths.Root + "/Fonts/Font_UI_Dynamic.asset";

    // Medidas (m). 1 unidad = 1 metro; un carril mide 2 m.
    private const float BlockSize = 1.7f;
    private const float BarLength = 1.9f;
    private const float LowBarHeight = 0.5f;
    private const float HighBarHeight = 1.2f;
    private const float BarDiameter = 0.2f;
    private const float PostDiameter = 0.14f;
    private const float LowBarColliderTop = 0.65f;      // se pasa saltando
    private const float HighBarColliderBottom = 1.0f;   // se pasa deslizándose (cápsula de 0,7 m)
    private const float HighBarColliderTop = 2.4f;
    private const float BarColliderDepth = 0.4f;
    private const float HammerPivotHeight = 4.5f;
    private const float HammerArmLength = 3.3f;
    private const float HammerFrameHalfWidth = 3.9f;
    private const float ArchHalfWidth = 3.8f;
    private const float ArchPostHeight = 4.2f;
    private const float ArchRise = 1.4f;
    private const float ArchTube = 0.7f;
    private const int ArchSegments = 10;

    [MenuItem("Una Ronda Más/Prefabs/Crear obstáculos, arco y efectos (Otoniel)")]
    public static void CreateAll()
    {
        ProjectPaths.EnsureFolder(ProjectPaths.PrefabsObstacles);
        ProjectPaths.EnsureFolder(EffectsFolder);
        CreateEffects();
        CreateBlock(BlockPath, false);
        CreateBlock(HologramPath, true);
        CreateBar(LowBarPath, LowBarHeight, 0f, LowBarColliderTop);
        CreateBar(HighBarPath, HighBarHeight, HighBarColliderBottom, HighBarColliderTop);
        CreateHammer();
        CreateFinishArch();
        AssignStarSparkle();
        AssetDatabase.SaveAssets();
    }

    // ---------- Obstáculos ----------

    private static void CreateBlock(string path, bool fake)
    {
        var root = new GameObject(Path.GetFileNameWithoutExtension(path));
        Transform visual = NewChild(root.transform, "Visual");
        Material material = MaterialCreator.Load(fake ? "Mat_Hologram" : "Mat_Red");
        Part(PrimitiveType.Cube, visual, "Body", new Vector3(0f, BlockSize * 0.5f, 0f), Vector3.one * BlockSize, material);
        // Cojín de arriba para que parezca inflable.
        Part(PrimitiveType.Sphere, visual, "Cushion", new Vector3(0f, BlockSize, 0f),
            new Vector3(BlockSize * 0.96f, 0.32f, BlockSize * 0.96f), material);

        root.AddComponent<Obstacle>().EditorSetup(fake, visual);
        if (fake)
        {
            root.AddComponent<Hologram>().EditorSetup(visual);
        }
        else
        {
            AddLabel(visual, "!", new Vector3(0f, BlockSize * 0.5f, -BlockSize * 0.5f - 0.01f), Color.white);
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, BlockSize * 0.5f, 0f);
            box.size = Vector3.one * (BlockSize - 0.1f);
        }
        Save(root, path);
    }

    private static void CreateBar(string path, float barHeight, float colliderBottom, float colliderTop)
    {
        var root = new GameObject(Path.GetFileNameWithoutExtension(path));
        Transform visual = NewChild(root.transform, "Visual");
        Material red = MaterialCreator.Load("Mat_Red");
        Material grey = MaterialCreator.Load("Mat_Grey");
        Material stripe = GameMaterials.GetOrCreate("Mat_Stripe", "#F2EFE8");
        float half = BarLength * 0.5f;
        float postHeight = barHeight + BarDiameter;

        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cylinder, visual, "Post", new Vector3(side * half, postHeight * 0.5f, 0f),
                new Vector3(PostDiameter, postHeight * 0.5f, PostDiameter), grey);
        }
        Quaternion alongX = Quaternion.Euler(0f, 0f, 90f);
        Part(PrimitiveType.Cylinder, visual, "Bar", new Vector3(0f, barHeight, 0f),
            new Vector3(BarDiameter, half, BarDiameter), red, alongX);
        foreach (float x in new[] { -0.55f, 0f, 0.55f })
        {
            Part(PrimitiveType.Cylinder, visual, "Stripe", new Vector3(x, barHeight, 0f),
                new Vector3(BarDiameter * 1.08f, 0.08f, BarDiameter * 1.08f), stripe, alongX);
        }

        root.AddComponent<Obstacle>().EditorSetup(false, visual);
        var box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, (colliderBottom + colliderTop) * 0.5f, 0f);
        box.size = new Vector3(BarLength, colliderTop - colliderBottom, BarColliderDepth);
        Save(root, path);
    }

    private static void CreateHammer()
    {
        var root = new GameObject("Obstacle_Hammer");
        Transform frame = NewChild(root.transform, "Frame");
        Material red = MaterialCreator.Load("Mat_Red");
        Material grey = MaterialCreator.Load("Mat_Grey");
        float frameHeight = HammerPivotHeight + 0.3f;
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cylinder, frame, "Post", new Vector3(side * HammerFrameHalfWidth, frameHeight * 0.5f, 0f),
                new Vector3(0.25f, frameHeight * 0.5f, 0.25f), grey);
        }
        Part(PrimitiveType.Cube, frame, "Beam", new Vector3(0f, frameHeight, 0f),
            new Vector3(HammerFrameHalfWidth * 2f + 0.3f, 0.3f, 0.35f), grey);

        Transform pivot = NewChild(root.transform, "Pivot");
        pivot.localPosition = new Vector3(0f, HammerPivotHeight, 0f);
        Part(PrimitiveType.Cylinder, pivot, "Arm", new Vector3(0f, -HammerArmLength * 0.5f, 0f),
            new Vector3(0.12f, HammerArmLength * 0.5f, 0.12f), grey);
        Part(PrimitiveType.Cylinder, pivot, "Head", new Vector3(0f, -HammerArmLength, 0f),
            new Vector3(1.1f, 0.65f, 1.1f), red, Quaternion.Euler(90f, 0f, 0f));
        Transform trigger = NewChild(pivot, "HeadTrigger");
        trigger.localPosition = new Vector3(0f, -HammerArmLength, 0f);
        var box = trigger.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1f, 1f, 1.2f);

        root.AddComponent<Obstacle>().EditorSetup(false, null);
        root.AddComponent<HammerSwing>().EditorSetup(pivot);
        Save(root, HammerPath);
    }

    // ---------- Arco de meta ----------

    private static void CreateFinishArch()
    {
        var root = new GameObject("FinishArch");
        Transform visual = NewChild(root.transform, "Visual");
        Material gold = MaterialCreator.Load("Mat_Gold");
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cylinder, visual, "Post", new Vector3(side * ArchHalfWidth, ArchPostHeight * 0.5f, 0f),
                new Vector3(ArchTube, ArchPostHeight * 0.5f, ArchTube), gold);
        }
        // Curva de arriba con tubos cortos y esferas en las uniones.
        Vector3 previous = ArchPoint(0);
        for (int i = 1; i <= ArchSegments; i++)
        {
            Vector3 point = ArchPoint(i);
            Vector3 direction = point - previous;
            Part(PrimitiveType.Cylinder, visual, "Arc", (point + previous) * 0.5f,
                new Vector3(ArchTube, direction.magnitude * 0.5f + 0.05f, ArchTube), gold,
                Quaternion.FromToRotation(Vector3.up, direction));
            Part(PrimitiveType.Sphere, visual, "Joint", point, Vector3.one * ArchTube, gold);
            previous = point;
        }

        var confettiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConfettiPath);
        ParticleSystem confetti = null;
        if (confettiPrefab != null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(confettiPrefab, root.transform);
            go.transform.localPosition = new Vector3(0f, ArchPostHeight + ArchRise, 0f);
            confetti = go.GetComponent<ParticleSystem>();
        }
        root.AddComponent<FinishArch>().EditorSetup(confetti, visual);
        Save(root, GameSystemsPrefabCreator.FinishArchPath);
    }

    private static Vector3 ArchPoint(int i)
    {
        float angle = Mathf.PI * i / ArchSegments;
        return new Vector3(ArchHalfWidth * Mathf.Cos(angle), ArchPostHeight + ArchRise * Mathf.Sin(angle), 0f);
    }

    // ---------- Partículas ----------

    private static void CreateEffects()
    {
        ColorUtility.TryParseHtmlString("#4FA3E0", out Color water);
        ColorUtility.TryParseHtmlString("#F7B11D", out Color gold);
        ColorUtility.TryParseHtmlString("#FBD97A", out Color starLight);
        ColorUtility.TryParseHtmlString("#2A78D4", out Color blue);

        // Salpicadura al caer al agua.
        ParticleSystem splash = NewParticles("FX_Splash", true, true);
        var main = splash.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.gravityModifier = 1.5f;
        main.startColor = new ParticleSystem.MinMaxGradient(water, Color.white);
        SetBurst(splash, 45);
        SetCone(splash, 25f, 0.45f);
        SaveEffect(splash, SplashPath);

        // Confeti del arco de meta.
        ParticleSystem confetti = NewParticles("FX_Confetti", false, false);
        main = confetti.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.7f;
        var colors = new Gradient();
        colors.SetKeys(
            new[] { new GradientColorKey(gold, 0f), new GradientColorKey(blue, 0.35f), new GradientColorKey(Color.white, 0.7f), new GradientColorKey(starLight, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
        SetBurst(confetti, 140);
        SetCone(confetti, 35f, 1.5f);
        SaveEffect(confetti, ConfettiPath);

        // Destello al tomar una estrella.
        ParticleSystem sparkle = NewParticles("FX_StarSparkle", true, true);
        main = sparkle.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(gold, starLight);
        SetBurst(sparkle, 16);
        var shape = sparkle.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        SaveEffect(sparkle, SparklePath);
    }

    private static ParticleSystem NewParticles(string name, bool playOnAwake, bool destroyWhenDone)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = playOnAwake;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.stopAction = destroyWhenDone ? ParticleSystemStopAction.Destroy : ParticleSystemStopAction.None;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial =
            AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        return ps;
    }

    private static void SetBurst(ParticleSystem ps, short count)
    {
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
    }

    private static void SetCone(ParticleSystem ps, float angle, float radius)
    {
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = radius;
        shape.rotation = new Vector3(-90f, 0f, 0f); // hacia arriba, sin importar la rotación del objeto
    }

    private static void SaveEffect(ParticleSystem ps, string path) => Save(ps.gameObject, path);

    private static void AssignStarSparkle()
    {
        var sparkle = AssetDatabase.LoadAssetAtPath<GameObject>(SparklePath);
        foreach (string path in new[] { StarPrefabCreator.NormalPath, StarPrefabCreator.GoldenPath })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Star star = root.GetComponent<Star>();
                if (star == null) continue;
                var so = new SerializedObject(star);
                ProjectPaths.SetObject(so, "collectEffectPrefab", sparkle);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    // ---------- Utilidades (también las usan los otros creadores de Otoniel) ----------

    /// <summary>Primitiva solo visual (sin collider) con un material.</summary>
    public static GameObject Part(PrimitiveType type, Transform parent, string name, Vector3 localPosition,
        Vector3 localScale, Material material, Quaternion? localRotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation ?? Quaternion.identity;
        go.transform.localScale = localScale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    public static Transform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>Texto 3D (TextMeshPro) mirando hacia la cámara (−Z).</summary>
    public static void AddLabel(Transform parent, string text, Vector3 localPosition, Color color, float fontSize = 12f)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) return;
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 2f);
    }

    public static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
