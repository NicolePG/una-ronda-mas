using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Escenografía del estudio de TV para Game.unity (Otoniel): piscina con azulejos y agua con olas, gradas con
/// público animado (Kenney, CC0), pared del fondo con el cartel y focos de marquesina, torres con haces de luz,
/// flotadores y cañones de confeti. Lo llama GameSceneCreator al armar la escena.
/// </summary>
public static class StudioSetCreator
{
    private const string MeshFolder = ProjectPaths.Art + "/Meshes";
    private const string TextureFolder = ProjectPaths.Art + "/Textures";
    private const string AudienceControllerPrefix = "Audience_";
    private const string FontPath = ProjectPaths.Root + "/Fonts/Font_UI_Dynamic.asset";
    public const string OutlineFontMaterialPath = ProjectPaths.Root + "/Fonts/Font_UI_Dynamic_Outline.mat";

    // Medidas (m).
    private const float PoolHalfWidth = 10f;       // el agua va de x = −10 a 10
    private const float DeckTop = 0.25f;
    private const float DeckWidth = 30f;
    private const float PoolStartZ = -30f;
    private const float PoolLength = 160f;
    private const float WaterHeight = -0.6f;
    private const float PoolFloorHeight = -2.5f;
    private const int BleacherRows = 3;
    private const float BleacherFirstX = 11.6f;
    private const float BleacherRowDepth = 1.5f;
    private const float BleacherStep = 0.6f;
    private const float BleacherStartZ = 0f;
    private const float BleacherLength = 56f;
    private const int SeatsPerRow = 7;
    private const float AudienceHeight = 1.35f;
    private const float BackWallZ = 122f;
    private const float SignY = 25f;
    private const float BulbSpacing = 1.4f;

    private static readonly Color Gold = IconPainter.Hex("#F7B11D");
    private static readonly Color Water = IconPainter.Hex("#4FA3E0");

    /// <summary>Arma el estudio en la escena abierta. Devuelve los cañones de confeti.</summary>
    public static ParticleSystem[] Build()
    {
        Transform root = new GameObject("Studio").transform;
        BuildPool(root);
        BuildBleachers(root);
        BuildBackWall(root);
        BuildLightTowers(root);
        BuildFloatingRings(root);
        return BuildConfettiCannons(root);
    }

    // ---------- Piscina ----------

    private static void BuildPool(Transform root)
    {
        Transform pool = Child(root, "Pool");
        AssetDatabase.DeleteAsset(CharacterPrefabCreator.AnimatorFolder + "/Audience.controller");   // versión vieja (un solo controlador)
        Material deck = GameMaterials.GetOrCreate("Mat_Deck", "#2C2050");
        Material wallTiles = TiledMaterial("Mat_PoolWallTiles", new Vector2(PoolLength / 2f, 1.4f));
        Material coping = MaterialCreator.Load("Mat_Track");
        float centerZ = PoolStartZ + PoolLength * 0.5f;

        foreach (float side in new[] { -1f, 1f })
        {
            ObstaclePrefabCreator.Part(PrimitiveType.Cube, pool, "Deck",
                new Vector3(side * (PoolHalfWidth + DeckWidth * 0.5f), DeckTop - 0.5f, centerZ),
                new Vector3(DeckWidth, 1f, PoolLength), deck);
            float wallHeight = DeckTop - PoolFloorHeight;
            ObstaclePrefabCreator.Part(PrimitiveType.Cube, pool, "PoolWall",
                new Vector3(side * PoolHalfWidth, PoolFloorHeight + wallHeight * 0.5f, centerZ),
                new Vector3(0.3f, wallHeight, PoolLength), wallTiles, Quaternion.identity);
            ObstaclePrefabCreator.Part(PrimitiveType.Cube, pool, "Coping",
                new Vector3(side * (PoolHalfWidth + 0.3f), DeckTop + 0.06f, centerZ),
                new Vector3(0.9f, 0.12f, PoolLength), coping);
        }
    }

    /// <summary>Material del fondo de la piscina (azulejos); lo usa GameSceneCreator.</summary>
    public static Material PoolFloorMaterial() => TiledMaterial("Mat_PoolFloorTiles", new Vector2(10f, 80f));

    /// <summary>Material del agua con olas (mapas de normales) y transparencia; lo usa GameSceneCreator.</summary>
    public static Material WaterMaterial()
    {
        string path = $"{ProjectPaths.Materials}/Mat_WaterSurface.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { name = "Mat_WaterSurface" };
            AssetDatabase.CreateAsset(material, path);
        }
        Texture2D normal = WaterNormalTexture();
        Color color = Water;
        color.a = 0.78f;
        material.color = color;
        material.SetFloat("_Glossiness", 0.84f);
        material.SetFloat("_Metallic", 0.05f);
        material.SetTexture("_BumpMap", normal);
        material.SetTextureScale("_BumpMap", new Vector2(8f, 64f));
        material.SetFloat("_BumpScale", 0.35f);
        material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_DetailNormalMap", normal);
        material.SetTextureScale("_DetailNormalMap", new Vector2(15f, 120f));
        material.SetFloat("_DetailNormalMapScale", 0.45f);
        material.EnableKeyword("_DETAIL_MULX2");
        // Modo Transparent del shader Standard: los brillos no se apagan con la transparencia.
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material TiledMaterial(string name, Vector2 tiling)
    {
        string path = $"{ProjectPaths.Materials}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.mainTexture = TileTexture();
        material.mainTextureScale = tiling;
        material.color = Color.white;
        material.SetFloat("_Glossiness", 0.55f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Azulejos de piscina: celeste con junta blanca y un poco de relieve.
    private static Texture2D TileTexture()
    {
        string path = $"{TextureFolder}/Tex_PoolTiles.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        Color tile = IconPainter.Hex("#8FD0F2"), grout = IconPainter.Hex("#EAF6FF"), dark = IconPainter.Hex("#5FB3E3");
        const int tiles = 4;
        const float groutWidth = 0.035f;
        return IconPainter.SaveTexture(path, IconPainter.Paint(256, 256, (u, v) =>
        {
            float fu = Mathf.Repeat(u * tiles, 1f), fv = Mathf.Repeat(v * tiles, 1f);
            float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
            if (edge < groutWidth) return grout;
            float bevel = Mathf.Clamp01((edge - groutWidth) / 0.08f);
            return Color.Lerp(dark, tile, 0.55f + 0.45f * bevel);
        }), false);
    }

    // Olas: suma de ondas con frecuencias enteras (se repite sin costuras) convertida a mapa de normales.
    private static Texture2D WaterNormalTexture()
    {
        string path = $"{TextureFolder}/Tex_WaterNormal.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        int[,] waves = { { 1, 2 }, { 3, -1 }, { -2, 3 }, { 4, 1 }, { 1, -5 }, { -5, 2 }, { 6, 3 }, { -3, -7 } };
        float[] amplitudes = { 1f, 0.7f, 0.6f, 0.45f, 0.35f, 0.3f, 0.2f, 0.15f };
        var random = new System.Random(7);
        var phases = new float[amplitudes.Length];
        for (int i = 0; i < phases.Length; i++) phases[i] = (float)random.NextDouble() * Mathf.PI * 2f;
        const float strength = 0.035f;
        return IconPainter.SaveTexture(path, IconPainter.Paint(256, 256, (u, v) =>
        {
            float du = 0f, dv = 0f;
            for (int i = 0; i < amplitudes.Length; i++)
            {
                float k = Mathf.PI * 2f;
                float arg = k * (waves[i, 0] * u + waves[i, 1] * v) + phases[i];
                float c = Mathf.Cos(arg) * amplitudes[i] * k;
                du += c * waves[i, 0];
                dv += c * waves[i, 1];
            }
            Vector3 n = new Vector3(-du * strength, -dv * strength, 1f).normalized;
            return new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
        }), true);
    }

    // ---------- Gradas y público ----------

    private static void BuildBleachers(Transform root)
    {
        Transform stands = Child(root, "Bleachers");
        Material seat = GameMaterials.GetOrCreate("Mat_Bleacher", "#3A2A70");
        Material trim = MaterialCreator.Load("Mat_Gold");
        List<GameObject> models = AudienceModels();
        var controllers = new Dictionary<GameObject, RuntimeAnimatorController>();
        var members = new List<Animator>();
        var random = new System.Random(11);

        foreach (float side in new[] { -1f, 1f })
        {
            for (int row = 0; row < BleacherRows; row++)
            {
                float x = side * (BleacherFirstX + row * BleacherRowDepth);
                float height = (row + 1) * BleacherStep;
                float top = DeckTop + height;
                ObstaclePrefabCreator.Part(PrimitiveType.Cube, stands, $"Row{row}",
                    new Vector3(x, DeckTop + height * 0.5f, BleacherStartZ + BleacherLength * 0.5f),
                    new Vector3(BleacherRowDepth, height, BleacherLength), seat);
                ObstaclePrefabCreator.Part(PrimitiveType.Cube, stands, "Trim",
                    new Vector3(x - side * BleacherRowDepth * 0.5f, top - 0.05f, BleacherStartZ + BleacherLength * 0.5f),
                    new Vector3(0.08f, 0.1f, BleacherLength), trim);

                for (int seatIndex = 0; seatIndex < SeatsPerRow; seatIndex++)
                {
                    float z = BleacherStartZ + 4f + seatIndex * 7.5f + (row % 2) * 3.5f;
                    GameObject model = models[random.Next(models.Count)];
                    if (!controllers.TryGetValue(model, out RuntimeAnimatorController controller))
                        controllers[model] = controller = AudienceController(model.name);
                    // Mira al centro de la pista y un poco hacia la cámara.
                    float yaw = side > 0f ? -115f : 115f;
                    Animator member = Person(stands, model, new Vector3(x, top, z), yaw, controller);
                    if (member != null) members.Add(member);
                }
            }
        }
        stands.gameObject.AddComponent<AudienceCrowd>().EditorSetup(members.ToArray());
    }

    private static Animator Person(Transform parent, GameObject model, Vector3 position, float yaw,
        RuntimeAnimatorController controller)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        Bounds bounds = PreviewRenderer.GetBounds(go);
        float scale = AudienceHeight / Mathf.Max(bounds.size.y, 0.01f);
        go.transform.localScale = Vector3.one * scale;
        go.transform.SetPositionAndRotation(position + Vector3.down * bounds.min.y * scale, Quaternion.Euler(0f, yaw, 0f));
        Animator animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        // Sin sombras: son muchos y en WebGL cuesta.
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return animator;
    }

    // Todos los personajes de Kenney menos el concursante y Tito.
    private static List<GameObject> AudienceModels()
    {
        var result = new List<GameObject>();
        foreach (GameObject model in ProjectPaths.LoadAll<GameObject>(KenneyCharacterImporter.Folder.TrimEnd('/')))
        {
            if (model.name == CharacterPrefabCreator.ContestantModel || model.name == CharacterPrefabCreator.HostModel) continue;
            if (model.name.StartsWith("character-")) result.Add(model);
        }
        return result;
    }

    // Un controlador por modelo con sus propios clips (los de un modelo no animan a los otros).
    private static RuntimeAnimatorController AudienceController(string model)
    {
        ProjectPaths.EnsureFolder(CharacterPrefabCreator.AnimatorFolder);
        string path = $"{CharacterPrefabCreator.AnimatorFolder}/{AudienceControllerPrefix}{model}.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        var clips = new Dictionary<string, AnimationClip>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(CharacterPrefabCreator.ModelPath(model)))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) clips[clip.name] = clip;
        }
        (string state, string clip)[] states =
        {
            (AudienceCrowd.IdleState, "idle"), (AudienceCrowd.CheerState, "emote-yes"), (AudienceCrowd.OohState, "emote-no"),
            (AudienceCrowd.JumpState, "jump"), (AudienceCrowd.WaveState, "interact-left"),
        };
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = machine.AddState(states[i].state, new Vector3(300f, 60f * i, 0f));
            if (clips.TryGetValue(states[i].clip, out AnimationClip clip)) state.motion = clip;
            if (i == 0) machine.defaultState = state;
        }
        return controller;
    }

    // ---------- Pared del fondo con cartel y marquesina ----------

    private static void BuildBackWall(Transform root)
    {
        Transform wall = Child(root, "BackWall");
        Material wallMaterial = GameMaterials.GetOrCreateNoFog("Mat_BackWall", "#1A1230", 0f);
        Material bulbMaterial = GameMaterials.GetOrCreateNoFog("Mat_MarqueeBulb", "#F7B11D", 1.5f);
        ObstaclePrefabCreator.Part(PrimitiveType.Cube, wall, "Wall", new Vector3(0f, 13f, BackWallZ), new Vector3(90f, 34f, 1f), wallMaterial);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null)
        {
            var signGo = new GameObject("Sign");
            signGo.transform.SetParent(wall, false);
            signGo.transform.position = new Vector3(0f, SignY, BackWallZ - 0.7f);
            var sign = signGo.AddComponent<TextMeshPro>();
            sign.font = font;
            sign.fontSharedMaterial = OutlineFontMaterial(font);
            sign.text = "¡UNA RONDA MÁS!";
            sign.fontSize = 48f;
            sign.fontStyle = FontStyles.Bold;
            sign.color = Gold;
            sign.alignment = TextAlignmentOptions.Center;
            sign.enableWordWrapping = false;
            sign.rectTransform.sizeDelta = new Vector2(60f, 8f);
        }

        // Focos alrededor del cartel, en orden para la ola.
        const float halfWidth = 26.5f, halfHeight = 3.8f;
        var bulbs = new List<Renderer>();
        var corners = new[]
        {
            new Vector2(-halfWidth, -halfHeight), new Vector2(halfWidth, -halfHeight),
            new Vector2(halfWidth, halfHeight), new Vector2(-halfWidth, halfHeight),
        };
        for (int c = 0; c < corners.Length; c++)
        {
            Vector2 a = corners[c], b = corners[(c + 1) % corners.Length];
            int count = Mathf.Max(1, Mathf.RoundToInt((b - a).magnitude / BulbSpacing));
            for (int i = 0; i < count; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)count);
                GameObject bulb = ObstaclePrefabCreator.Part(PrimitiveType.Sphere, wall, "Bulb",
                    new Vector3(p.x, SignY + p.y, BackWallZ - 0.6f), Vector3.one * 0.7f, bulbMaterial);
                bulbs.Add(bulb.GetComponent<Renderer>());
            }
        }
        wall.gameObject.AddComponent<MarqueeLights>().EditorSetup(bulbs.ToArray());
    }

    /// <summary>Material de la fuente con contorno oscuro (carteles y textos flotantes en 3D).</summary>
    public static Material OutlineFontMaterial(TMP_FontAsset font)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineFontMaterialPath);
        if (material != null) return material;
        material = new Material(font.material) { name = "Font_UI_Dynamic_Outline" };
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, IconPainter.Hex("#1A1230"));
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        AssetDatabase.CreateAsset(material, OutlineFontMaterialPath);
        return material;
    }

    // ---------- Torres con haces de luz ----------

    private static void BuildLightTowers(Transform root)
    {
        Transform towers = Child(root, "LightTowers");
        Material steel = MaterialCreator.Load("Mat_Grey");
        Material fixture = GameMaterials.GetOrCreate("Mat_Fixture", "#1A1A22");
        Mesh cone = ConeMesh();
        Material[] beams = { BeamMaterial("Mat_Beam_Gold", Gold), BeamMaterial("Mat_Beam_Blue", Water), BeamMaterial("Mat_Beam_White", Color.white) };
        float[] zs = { 16f, 40f, 72f };
        const float towerHeight = 11f, beamLength = 16f, beamRadius = 2.4f;
        int index = 0;

        foreach (float z in zs)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 basePosition = new Vector3(side * (PoolHalfWidth + 0.6f), DeckTop, z);
                ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, towers, "Tower",
                    basePosition + Vector3.up * towerHeight * 0.5f, new Vector3(0.35f, towerHeight * 0.5f, 0.35f), steel);
                ObstaclePrefabCreator.Part(PrimitiveType.Cube, towers, "Fixture",
                    basePosition + Vector3.up * towerHeight, new Vector3(0.9f, 0.6f, 0.9f), fixture);

                var pivot = new GameObject("Beam").transform;
                pivot.SetParent(towers, false);
                pivot.position = basePosition + Vector3.up * (towerHeight - 0.3f);
                // Hacia el agua de su lado: iluminan la piscina sin tapar la pantalla de Tito.
                pivot.LookAt(new Vector3(side * 5.5f, WaterHeight, z + 5f));
                var beam = new GameObject("Cone", typeof(MeshFilter), typeof(MeshRenderer));
                beam.transform.SetParent(pivot, false);
                beam.transform.localScale = new Vector3(beamRadius, beamRadius, beamLength);
                beam.GetComponent<MeshFilter>().sharedMesh = cone;
                var renderer = beam.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = beams[index % beams.Length];
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                pivot.gameObject.AddComponent<StudioLightBeam>().EditorSetup(index * 0.37f % 1f, 18f + 6f * (index % 2));
                index++;
            }
        }
    }

    private static Material BeamMaterial(string name, Color tint)
    {
        string path = $"{ProjectPaths.Materials}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.mainTexture = BeamTexture();
        tint.a = 0.12f;
        material.SetColor("_TintColor", tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Degradé del haz: fuerte cerca del foco y se apaga hacia el piso.
    private static Texture2D BeamTexture()
    {
        string path = $"{TextureFolder}/Tex_BeamGradient.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        return IconPainter.SaveTexture(path, IconPainter.Paint(8, 128, (u, v) =>
        {
            float fadeIn = Mathf.Clamp01(v / 0.06f);
            float alpha = fadeIn * Mathf.Pow(1f - v, 1.4f);
            return new Color(1f, 1f, 1f, alpha);
        }), false, false);
    }

    // Cono abierto: punta en (0, 0, 0), base de radio 1 en z = 1. v = 0 en la punta y 1 en la base.
    private static Mesh ConeMesh()
    {
        string path = $"{MeshFolder}/Mesh_BeamCone.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        const int segments = 24;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments, angle = u * Mathf.PI * 2f;
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(u, 0f));
            vertices.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 1f));
            uvs.Add(new Vector2(u, 1f));
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2, b = a + 1, c = a + 3;
            triangles.AddRange(new[] { a, c, b });
        }
        var mesh = new Mesh { name = "BeamCone" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        ProjectPaths.EnsureFolder(MeshFolder);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // ---------- Flotadores ----------

    private static void BuildFloatingRings(Transform root)
    {
        Transform rings = Child(root, "FloatingRings");
        Mesh torus = TorusMesh();
        Material white = GameMaterials.GetOrCreate("Mat_RingWhite", "#F2EFE8", 0f, 0.6f);
        Material orange = GameMaterials.GetOrCreate("Mat_RingOrange", "#FF8A3D", 0f, 0.6f);
        Vector3[] spots =
        {
            new Vector3(-5.2f, 0f, 9f), new Vector3(-8.4f, 0f, 21f), new Vector3(5.6f, 0f, 24f), new Vector3(-6.3f, 0f, 37f),
            new Vector3(8.2f, 0f, 44f), new Vector3(5.1f, 0f, 58f), new Vector3(-8.7f, 0f, 63f), new Vector3(7.4f, 0f, 79f),
            new Vector3(-5.5f, 0f, 88f), new Vector3(6.2f, 0f, 101f),
        };
        for (int i = 0; i < spots.Length; i++)
        {
            var ring = new GameObject("Ring", typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(rings, false);
            ring.transform.localPosition = spots[i] + Vector3.up * (WaterHeight + 0.08f);
            ring.transform.localScale = Vector3.one * 0.6f;
            ring.GetComponent<MeshFilter>().sharedMesh = torus;
            ring.GetComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? white : orange;
            ring.AddComponent<FloatingBob>();
        }
    }

    // Toro acostado (eje Y): radio mayor 1, radio del tubo 0,38.
    private static Mesh TorusMesh()
    {
        string path = $"{MeshFolder}/Mesh_Torus.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        const int major = 28, minor = 12;
        const float tube = 0.38f;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        for (int i = 0; i <= major; i++)
        {
            float a = i / (float)major * Mathf.PI * 2f;
            var center = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int j = 0; j <= minor; j++)
            {
                float b = j / (float)minor * Mathf.PI * 2f;
                Vector3 normal = center * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                vertices.Add(center + normal * tube);
                normals.Add(normal);
            }
        }
        for (int i = 0; i < major; i++)
        {
            for (int j = 0; j < minor; j++)
            {
                int a = i * (minor + 1) + j, b = a + minor + 1;
                triangles.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }
        }
        var mesh = new Mesh { name = "Torus" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        ProjectPaths.EnsureFolder(MeshFolder);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // ---------- Cañones de confeti ----------

    private static ParticleSystem[] BuildConfettiCannons(Transform root)
    {
        Transform cannons = Child(root, "ConfettiCannons");
        Material body = GameMaterials.GetOrCreate("Mat_Cannon", "#2C2050");
        Material ring = MaterialCreator.Load("Mat_Gold");
        var confettiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ObstaclePrefabCreator.ConfettiPath);
        var result = new List<ParticleSystem>();
        foreach (float side in new[] { -1f, 1f })
        {
            var cannon = new GameObject("Cannon").transform;
            cannon.SetParent(cannons, false);
            cannon.position = new Vector3(side * (PoolHalfWidth + 0.5f), DeckTop, 12f);
            cannon.rotation = Quaternion.Euler(0f, 0f, side * 28f);   // inclinado hacia la pista
            ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, cannon, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(0.6f, 0.6f, 0.6f), body);
            ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, cannon, "Ring", new Vector3(0f, 1.1f, 0f), new Vector3(0.68f, 0.05f, 0.68f), ring);
            if (confettiPrefab == null) continue;
            var fx = (GameObject)PrefabUtility.InstantiatePrefab(confettiPrefab, cannon);
            fx.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            fx.transform.localRotation = Quaternion.identity;
            result.Add(fx.GetComponent<ParticleSystem>());
        }
        return result.ToArray();
    }

    private static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }
}
