using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea los 12 tramos de 20 m del MVP (4 fáciles, 4 medios, 4 difíciles) y el tramo vacío en Prefabs/Segments.
/// Pivote al inicio del tramo, obstáculos solo entre z = 3 y z = 17, y siempre hay un camino posible.
/// Carriles: −1 = izquierda (x = −2), 0 = centro, 1 = derecha (x = 2).
/// </summary>
public static class SegmentCreator
{
    public const string SafePath = ProjectPaths.PrefabsSegments + "/Segment_Safe.prefab";

    private const float FloorThickness = 0.2f;
    private const float SideMargin = 0.4f;
    private const float RailDiameter = 0.45f;
    private const float StarHeight = 1f;
    private const float JumpStarHeight = 2.4f;   // solo se alcanza saltando
    private const float SlideStarHeight = 0.45f; // debajo de la barra alta: se toma deslizándose
    private const float DashLength = 1f;
    private const float DashGap = 1f;
    private const float WaterZoneDepth = 0.9f;
    private const float WaterZoneTop = 0.1f;     // un poco sobre el piso: solo lo toca quien está en el suelo

    private const int L = -1, C = 0, R = 1;

    private sealed class SegmentBuilder
    {
        public readonly Transform Root;
        public readonly List<Vector3Int> Gaps = new List<Vector3Int>(); // x = carril, y/z = inicio/fin ×10
        public readonly List<Transform> HologramSlots = new List<Transform>();
        public bool HasHighBar, HasHammer, HasWaterGap, RequiresJump, RequiresSlide;
        private readonly float laneWidth;

        public SegmentBuilder(Transform root, float laneWidth)
        {
            Root = root;
            this.laneWidth = laneWidth;
        }

        private float X(int lane) => lane * laneWidth;

        public void Block(int lane, float z) => Add(ObstaclePrefabCreator.BlockPath, new Vector3(X(lane), 0f, z));
        public void LowBar(int lane, float z) => Add(ObstaclePrefabCreator.LowBarPath, new Vector3(X(lane), 0f, z));

        public void HighBar(int lane, float z)
        {
            HasHighBar = true;
            Add(ObstaclePrefabCreator.HighBarPath, new Vector3(X(lane), 0f, z));
        }

        public void Hammer(float z)
        {
            HasHammer = true;
            Add(ObstaclePrefabCreator.HammerPath, new Vector3(0f, 0f, z));
        }

        /// <summary>Hueco al agua en un carril entre start y end (m).</summary>
        public void Gap(int lane, float start, float end)
        {
            HasWaterGap = true;
            Gaps.Add(new Vector3Int(lane, Mathf.RoundToInt(start * 10f), Mathf.RoundToInt(end * 10f)));
        }

        public void Star(int lane, float z, float y = StarHeight) =>
            Add(StarPrefabCreator.NormalPath, new Vector3(X(lane), y, z));

        public void Golden(int lane, float z, float y = StarHeight) =>
            Add(StarPrefabCreator.GoldenPath, new Vector3(X(lane), y, z));

        /// <summary>Punto donde puede salir un holograma cuando Tito miente.</summary>
        public void HoloSlot(int lane, float z)
        {
            Transform slot = ObstaclePrefabCreator.NewChild(Root, "HologramSlot");
            slot.localPosition = new Vector3(X(lane), 0f, z);
            HologramSlots.Add(slot);
        }

        private void Add(string prefabPath, Vector3 localPosition)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Falta el prefab {prefabPath}.");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Root);
            go.transform.localPosition = localPosition;
        }
    }

    private sealed class Spec
    {
        public string Name;
        public SegmentDifficulty Difficulty;
        public Action<SegmentBuilder> Build;
    }

    private static readonly Spec[] Specs =
    {
        // ---------- Fáciles (desde la ronda 1: bloques y barras bajas) ----------
        new Spec { Name = "Easy_01_Libre", Difficulty = SegmentDifficulty.Easy, Build = s =>
        {
            foreach (float z in new[] { 4f, 7f, 10f, 13f, 16f }) s.Star(C, z);
            s.Star(R, 8f); s.Star(R, 11f);
            s.Golden(L, 15f);
            s.HoloSlot(L, 8f); s.HoloSlot(R, 14f);
        }},
        new Spec { Name = "Easy_02_Zigzag", Difficulty = SegmentDifficulty.Easy, Build = s =>
        {
            s.Block(L, 6f); s.Block(R, 6f);
            s.Block(C, 13f); s.Block(R, 13f);
            s.Star(C, 3.5f); s.Golden(C, 6f);
            foreach (float z in new[] { 10f, 13f, 16f }) s.Star(L, z);
            s.HoloSlot(C, 9.5f); s.HoloSlot(R, 16f);
        }},
        new Spec { Name = "Easy_03_BarraBaja", Difficulty = SegmentDifficulty.Easy, Build = s =>
        {
            s.LowBar(L, 10f); s.LowBar(C, 10f); s.LowBar(R, 10f);
            s.RequiresJump = true;
            s.Star(C, 5f); s.Star(C, 7f);
            s.Golden(C, 10f, JumpStarHeight);
            s.Star(C, 14f); s.Star(C, 16f);
            s.HoloSlot(L, 15f);
        }},
        new Spec { Name = "Easy_04_BloquesYBarra", Difficulty = SegmentDifficulty.Easy, Build = s =>
        {
            s.Block(L, 5f); s.Block(L, 14f);
            s.LowBar(C, 10f);
            foreach (float z in new[] { 6f, 9f, 12f, 15f }) s.Star(R, z);
            s.Golden(C, 13f);
            s.HoloSlot(C, 5f);
        }},

        // ---------- Medios (barra alta desde la ronda 2, martillo desde la 3, hueco desde la 4) ----------
        new Spec { Name = "Mid_01_Barras", Difficulty = SegmentDifficulty.Mid, Build = s =>
        {
            s.LowBar(L, 5f); s.LowBar(C, 5f); s.LowBar(R, 5f);
            s.HighBar(L, 13f); s.HighBar(C, 13f); s.HighBar(R, 13f);
            s.RequiresJump = true; s.RequiresSlide = true;
            s.Star(C, 9f); s.Star(C, 11f);
            s.Golden(C, 13f, SlideStarHeight);
            s.Star(C, 16f);
            s.HoloSlot(R, 9.5f);
        }},
        new Spec { Name = "Mid_02_BarraAlta", Difficulty = SegmentDifficulty.Mid, Build = s =>
        {
            s.HighBar(L, 8f); s.HighBar(C, 8f);
            s.Block(C, 14f);
            foreach (float z in new[] { 5f, 8f, 11f }) s.Star(R, z);
            s.Golden(L, 8f, SlideStarHeight);
            s.Star(L, 14f); s.Star(L, 16f);
            s.HoloSlot(R, 15f);
        }},
        new Spec { Name = "Mid_03_Martillo", Difficulty = SegmentDifficulty.Mid, Build = s =>
        {
            s.Hammer(10f);
            foreach (float z in new[] { 4f, 6f, 14f, 16f }) s.Star(C, z);
            s.Golden(C, 10f);
            s.Star(L, 10f); s.Star(R, 10f);
            s.HoloSlot(L, 5f); s.HoloSlot(R, 15f);
        }},
        new Spec { Name = "Mid_04_HuecoCentro", Difficulty = SegmentDifficulty.Mid, Build = s =>
        {
            s.Gap(C, 8f, 11f);
            s.Block(L, 14f);
            foreach (float z in new[] { 5f, 8f, 11f, 14f }) s.Star(R, z);
            s.Golden(C, 9.5f, JumpStarHeight);
            s.HoloSlot(L, 6f);
        }},

        // ---------- Difíciles (desde la ronda 7) ----------
        new Spec { Name = "Hard_01_MartilloYHueco", Difficulty = SegmentDifficulty.Hard, Build = s =>
        {
            s.Gap(L, 4f, 7f); s.Gap(R, 4f, 7f);
            s.Hammer(11f);
            s.Block(L, 15f);
            s.Star(C, 4f); s.Star(C, 6f); s.Star(C, 8f);
            s.Golden(R, 15f);
            s.HoloSlot(C, 16f);
        }},
        new Spec { Name = "Hard_02_HuecoTotal", Difficulty = SegmentDifficulty.Hard, Build = s =>
        {
            s.Gap(L, 9f, 11.5f); s.Gap(C, 9f, 11.5f); s.Gap(R, 9f, 11.5f);
            s.RequiresJump = true;
            s.HighBar(L, 15f); s.HighBar(R, 15f);
            s.Star(C, 5f); s.Star(C, 7f);
            s.Golden(C, 10.25f, JumpStarHeight);
            s.Star(C, 15f);
            s.HoloSlot(L, 5f);
        }},
        new Spec { Name = "Hard_03_DobleBarra", Difficulty = SegmentDifficulty.Hard, Build = s =>
        {
            s.LowBar(L, 5f); s.LowBar(C, 5f); s.LowBar(R, 5f);
            s.HighBar(L, 13f); s.HighBar(C, 13f); s.HighBar(R, 13f);
            s.RequiresJump = true; s.RequiresSlide = true;
            s.Star(L, 9f); s.Star(R, 9f);
            s.Golden(C, 13f, SlideStarHeight);
            s.Star(L, 16f); s.Star(R, 16f);
        }},
        new Spec { Name = "Hard_04_Slalom", Difficulty = SegmentDifficulty.Hard, Build = s =>
        {
            s.Block(L, 4f); s.Block(C, 4f);
            s.Block(L, 10f); s.Block(R, 10f);
            s.Block(C, 16f); s.Block(R, 16f);
            s.Star(R, 4f); s.Golden(C, 10f); s.Star(L, 16f);
            s.Star(R, 7f); s.Star(L, 13f);
            s.HoloSlot(C, 13f);
        }},
    };

    [MenuItem("Una Ronda Más/Prefabs/Crear tramos (Otoniel)")]
    public static void CreateAllMenu()
    {
        CreateAll(AssetDatabase.LoadAssetAtPath<BalanceConfig>(ProjectPaths.Balance));
        GameSystemsPrefabCreator.CreateOrUpdate();
    }

    public static void CreateAll(BalanceConfig balance)
    {
        if (balance == null)
        {
            Debug.LogError("Falta Balance.asset (menú Una Ronda Más/Datos).");
            return;
        }
        ProjectPaths.EnsureFolder(ProjectPaths.PrefabsSegments);

        var safe = new SegmentBuilder(CreateRoot("Segment_Safe").transform, balance.LaneWidth);
        Finish(safe, balance, SegmentDifficulty.Easy, SafePath);

        foreach (Spec spec in Specs)
        {
            var builder = new SegmentBuilder(CreateRoot("Segment_" + spec.Name).transform, balance.LaneWidth);
            spec.Build(builder);
            Finish(builder, balance, spec.Difficulty, $"{ProjectPaths.PrefabsSegments}/Segment_{spec.Name}.prefab");
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Tramos creados: {Specs.Length} + Segment_Safe en {ProjectPaths.PrefabsSegments}.");
    }

    private static GameObject CreateRoot(string name)
    {
        var root = new GameObject(name);
        root.AddComponent<TrackSegment>();
        Rigidbody body = root.GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        return root;
    }

    private static void Finish(SegmentBuilder s, BalanceConfig balance, SegmentDifficulty difficulty, string path)
    {
        BuildFloor(s, balance);
        s.Root.GetComponent<TrackSegment>().DebugSetup(difficulty, s.HasHighBar, s.HasHammer, s.HasWaterGap,
            s.RequiresJump, s.RequiresSlide, s.HologramSlots.ToArray());
        ObstaclePrefabCreator.Save(s.Root.gameObject, path);
    }

    // ---------- Piso inflable con huecos ----------

    private static void BuildFloor(SegmentBuilder s, BalanceConfig balance)
    {
        float length = balance.SegmentLength;
        float laneWidth = balance.LaneWidth;
        Material track = MaterialCreator.Load("Mat_Track");
        Material rail = GameMaterials.GetOrCreate("Mat_Rail", "#D6CFC2");
        Material line = GameMaterials.GetOrCreate("Mat_LaneLine", "#B9B2A5");
        Material red = MaterialCreator.Load("Mat_Red");
        Transform floor = ObstaclePrefabCreator.NewChild(s.Root, "Floor");

        // Un listón por carril, cortado donde hay huecos.
        for (int lane = -1; lane <= 1; lane++)
        {
            float x = lane * laneWidth;
            foreach (Vector2 piece in SolidPieces(s, lane, length))
                FloorPiece(floor, x, laneWidth, piece.x, piece.y, track);
        }
        float edgeX = laneWidth * 1.5f + SideMargin * 0.5f;
        FloorPiece(floor, -edgeX, SideMargin, 0f, length, track);
        FloorPiece(floor, edgeX, SideMargin, 0f, length, track);

        // Bordes inflables a los costados.
        float railX = laneWidth * 1.5f + SideMargin;
        foreach (float side in new[] { -1f, 1f })
        {
            ObstaclePrefabCreator.Part(PrimitiveType.Cylinder, floor, "Rail",
                new Vector3(side * railX, RailDiameter * 0.4f, length * 0.5f),
                new Vector3(RailDiameter, length * 0.5f, RailDiameter), rail, Quaternion.Euler(90f, 0f, 0f));
        }

        // Líneas punteadas entre carriles (no se dibujan sobre los huecos).
        foreach (float side in new[] { -0.5f, 0.5f })
        {
            int left = side < 0f ? -1 : 0, right = left + 1;
            for (float z = DashGap * 0.5f; z + DashLength <= length; z += DashLength + DashGap)
            {
                float mid = z + DashLength * 0.5f;
                if (InGap(s, left, mid) || InGap(s, right, mid)) continue;
                ObstaclePrefabCreator.Part(PrimitiveType.Cube, floor, "LaneDash",
                    new Vector3(side * laneWidth, 0.005f, mid), new Vector3(0.08f, 0.01f, DashLength), line);
            }
        }

        // Huecos: agua debajo (trigger) y borde rojo de aviso.
        foreach (Vector3Int gap in s.Gaps)
        {
            float x = gap.x * laneWidth, start = gap.y / 10f, end = gap.z / 10f;
            var zoneGo = new GameObject("WaterZone");
            zoneGo.transform.SetParent(s.Root, false);
            zoneGo.transform.localPosition = new Vector3(x, WaterZoneTop - WaterZoneDepth * 0.5f, (start + end) * 0.5f);
            var box = zoneGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(laneWidth * 0.9f, WaterZoneDepth, end - start - 0.3f);
            zoneGo.AddComponent<WaterZone>();
            foreach (float z in new[] { start, end })
            {
                ObstaclePrefabCreator.Part(PrimitiveType.Cube, floor, "GapEdge",
                    new Vector3(x, -FloorThickness * 0.5f + 0.03f, z), new Vector3(laneWidth, FloorThickness + 0.06f, 0.14f), red);
            }
        }
    }

    private static void FloorPiece(Transform parent, float x, float width, float start, float end, Material material)
    {
        if (end - start <= 0.01f) return;
        ObstaclePrefabCreator.Part(PrimitiveType.Cube, parent, "FloorPiece",
            new Vector3(x, -FloorThickness * 0.5f, (start + end) * 0.5f),
            new Vector3(width, FloorThickness, end - start), material);
    }

    private static IEnumerable<Vector2> SolidPieces(SegmentBuilder s, int lane, float length)
    {
        var gaps = new List<Vector2>();
        foreach (Vector3Int gap in s.Gaps)
        {
            if (gap.x == lane) gaps.Add(new Vector2(gap.y / 10f, gap.z / 10f));
        }
        gaps.Sort((a, b) => a.x.CompareTo(b.x));
        float cursor = 0f;
        foreach (Vector2 gap in gaps)
        {
            yield return new Vector2(cursor, gap.x);
            cursor = gap.y;
        }
        yield return new Vector2(cursor, length);
    }

    private static bool InGap(SegmentBuilder s, int lane, float z)
    {
        foreach (Vector3Int gap in s.Gaps)
        {
            if (gap.x == lane && z >= gap.y / 10f - 0.1f && z <= gap.z / 10f + 0.1f) return true;
        }
        return false;
    }
}
