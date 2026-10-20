using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Crea los personajes con los modelos de Kenney Mini Characters (CC0, Art/Characters/KenneyMini):
/// el concursante (Prefabs/Gameplay/Contestant) y Tito en persona (Prefabs/Gameplay/TitoHost), con sus Animator Controllers.
/// Para cambiar de personaje, cambiar ContestantModel o HostModel y volver a ejecutar el menú.
/// </summary>
public static class CharacterPrefabCreator
{
    public const string ContestantModel = "character-female-c";   // camiseta azul (el color del jugador)
    public const string HostModel = "character-male-d";           // traje negro y corbata roja
    public const string AnimatorFolder = ProjectPaths.Art + "/Characters/Animators";
    public const string ContestantPath = ProjectPaths.PrefabsGameplay + "/Contestant.prefab";
    public const string HostPath = ProjectPaths.PrefabsGameplay + "/TitoHost.prefab";

    private const float ContestantHeight = 1.6f;
    private const float ContestantRadius = 0.35f;
    private const float HostHeight = 2f;

    // Estado del Animator → clip del modelo de Kenney.
    private static readonly (string state, string clip)[] ContestantStates =
    {
        (ContestantAnimator.IdleState, "idle"),
        (ContestantAnimator.RunState, "sprint"),
        (ContestantAnimator.JumpState, "jump"),
        (ContestantAnimator.SlideState, "crouch"),
        (ContestantAnimator.FallState, "fall"),
        (ContestantAnimator.HitState, "emote-no"),
        (ContestantAnimator.WinState, "emote-yes"),
        (ContestantAnimator.LoseState, "die"),
    };

    private static readonly (string state, string clip)[] HostStates =
    {
        (HostPresenter.IdleState, "idle"),
        (HostPresenter.LaughState, "emote-yes"),
        (HostPresenter.PointState, "interact-right"),
        (HostPresenter.ShockState, "emote-no"),
    };

    [MenuItem("Una Ronda Más/Prefabs/Crear personajes (Otoniel)")]
    public static void CreateAllMenu() =>
        CreateAll(AssetDatabase.LoadAssetAtPath<ParticleSystem>(ObstaclePrefabCreator.SplashPath));

    public static void CreateAll(ParticleSystem splash)
    {
        ProjectPaths.EnsureFolder(ProjectPaths.PrefabsGameplay);
        CreateContestant(splash);
        CreateHost();
        AssetDatabase.SaveAssets();
    }

    public static string ModelPath(string model) => KenneyCharacterImporter.Folder + model + ".fbx";

    private static void CreateContestant(ParticleSystem splash)
    {
        AnimatorController controller = CreateController("Contestant", ModelPath(ContestantModel), ContestantStates);
        var root = new GameObject("Contestant") { tag = "Player" };
        var body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.height = ContestantHeight;
        capsule.radius = ContestantRadius;
        capsule.center = new Vector3(0f, ContestantHeight * 0.5f, 0f);

        GameObject model = CreateModel(ContestantModel, root.transform, ContestantHeight, controller, out Animator animator);
        if (model == null)
        {
            Object.DestroyImmediate(root);
            return;
        }
        var contestant = root.AddComponent<ContestantController>();
        contestant.EditorSetup(model.transform, splash);
        root.AddComponent<ContestantAnimator>().EditorSetup(contestant, animator);

        // Reflector para la regla Apagón (RuleEffects lo prende).
        var spotGo = new GameObject("BlackoutSpot");
        spotGo.transform.SetParent(root.transform, false);
        spotGo.transform.localPosition = new Vector3(0f, 2.6f, -0.3f);
        spotGo.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        var spot = spotGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 30f;
        spot.spotAngle = 55f;
        spot.intensity = 4f;
        spot.color = new Color(1f, 0.95f, 0.85f);
        spot.enabled = false;

        AddSpeedRibbon(root.transform);
        ObstaclePrefabCreator.Save(root, ContestantPath);
    }

    // Estela de luz en los pies y gotas al correr (SpeedRibbon).
    private static void AddSpeedRibbon(Transform root)
    {
        var go = new GameObject("SpeedRibbon", typeof(LineRenderer));
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var line = go.GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 0;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 0.22f));
        // Con transparencia normal (no aditiva): sobre la pista blanca una luz aditiva no se vería.
        ColorUtility.TryParseHtmlString("#4FA3E0", out Color water);
        ColorUtility.TryParseHtmlString("#2A78D4", out Color player);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(water, 0f), new GradientColorKey(player, 0.35f), new GradientColorKey(player, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
        line.colorGradient = gradient;
        Material ribbon = ObstaclePrefabCreator.ParticleMaterial("Mat_FX_Ribbon", RibbonTexture(), false);
        // Va pegada al piso: sin esto las «partículas suaves» del shader la desvanecen casi por completo.
        if (ribbon != null) ribbon.SetFloat("_InvFade", 10f);
        line.sharedMaterial = ribbon;

        // Gotas: salen hacia arriba y se van hacia atrás con la pista.
        var sprayGo = new GameObject("RunSpray");
        sprayGo.transform.SetParent(root, false);
        sprayGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        var spray = sprayGo.AddComponent<ParticleSystem>();
        spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = spray.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.7f), new Color(water.r, water.g, water.b, 0.7f));
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        var emission = spray.emission;
        emission.rateOverTime = 0f;
        var shape = spray.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.2f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var velocity = spray.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
        Material drop = ObstaclePrefabCreator.ParticleMaterial("Mat_FX_Drop", "circle_05.png", false);
        if (drop != null) sprayGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = drop;

        go.AddComponent<SpeedRibbon>().EditorSetup(spray);
    }

    // Franja suave: opaca en el centro y transparente en los bordes (a lo ancho de la cinta).
    private static Texture2D RibbonTexture()
    {
        string path = ProjectPaths.Art + "/Textures/Tex_Ribbon.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        return IconPainter.SaveTexture(path, IconPainter.Paint(8, 64, (u, v) =>
        {
            float edge = 1f - Mathf.Abs(v - 0.5f) * 2f;
            return new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, edge));
        }), false, false);
    }

    private static void CreateHost()
    {
        AnimatorController controller = CreateController("TitoHost", ModelPath(HostModel), HostStates);
        var root = new GameObject("TitoHost");
        GameObject model = CreateModel(HostModel, root.transform, HostHeight, controller, out Animator animator);
        if (model == null)
        {
            Object.DestroyImmediate(root);
            return;
        }
        root.AddComponent<HostPresenter>().EditorSetup(animator);
        ObstaclePrefabCreator.Save(root, HostPath);
    }

    /// <summary>Instancia el modelo, lo escala a la altura pedida y lo apoya en y = 0 mirando a +Z.</summary>
    private static GameObject CreateModel(string model, Transform parent, float height,
        RuntimeAnimatorController controller, out Animator animator)
    {
        animator = null;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(model));
        if (asset == null)
        {
            Debug.LogError($"No encuentro el modelo {ModelPath(model)}.");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.name = "Model";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        Bounds bounds = PreviewRenderer.GetBounds(go);
        float scale = height / Mathf.Max(bounds.size.y, 0.01f);
        go.transform.localScale = Vector3.one * scale;
        go.transform.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);

        animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return go;
    }

    private static AnimatorController CreateController(string name, string modelPath, (string state, string clip)[] states)
    {
        ProjectPaths.EnsureFolder(AnimatorFolder);
        string path = $"{AnimatorFolder}/{name}.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        var clips = new Dictionary<string, AnimationClip>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) clips[clip.name] = clip;
        }

        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = machine.AddState(states[i].state, new Vector3(300f, 60f * i, 0f));
            if (clips.TryGetValue(states[i].clip, out AnimationClip clip)) state.motion = clip;
            else Debug.LogWarning($"{name}: el modelo no tiene la animación «{states[i].clip}».");
            if (i == 0) machine.defaultState = state;
        }
        return controller;
    }
}
