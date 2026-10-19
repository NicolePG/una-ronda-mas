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

        ObstaclePrefabCreator.Save(root, ContestantPath);
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
