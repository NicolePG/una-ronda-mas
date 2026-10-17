using UnityEngine;

/// <summary>
/// Genera la malla de una estrella de 5 puntas extruida, de frente a la cámara (−Z).
/// Las caras tienen normales planas para que se vea facetada.
/// </summary>
public static class StarMeshBuilder
{
    public const int Points = 5;
    public const float DefaultOuterRadius = 0.5f;
    public const float DefaultInnerRadius = 0.22f;
    public const float DefaultDepth = 0.16f;

    private static Mesh shared;

    /// <summary>Malla compartida con las medidas por defecto (se crea una sola vez).</summary>
    public static Mesh SharedMesh
    {
        get
        {
            if (shared == null) shared = Build();
            return shared;
        }
    }

    public static Mesh Build(float outerRadius = DefaultOuterRadius, float innerRadius = DefaultInnerRadius,
        float depth = DefaultDepth)
    {
        int rimCount = Points * 2;
        var rim = new Vector2[rimCount];
        for (int i = 0; i < rimCount; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI / Points; // empieza arriba, sentido antihorario
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            rim[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        float front = -depth * 0.5f;
        float back = depth * 0.5f;
        int capVertices = rimCount + 1;
        var vertices = new Vector3[capVertices * 2 + rimCount * 4];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[rimCount * 3 * 2 + rimCount * 6];
        int t = 0;

        // Cara de adelante (mira a −Z) y de atrás (mira a +Z).
        for (int face = 0; face < 2; face++)
        {
            int offset = face * capVertices;
            float z = face == 0 ? front : back;
            Vector3 normal = face == 0 ? Vector3.back : Vector3.forward;
            vertices[offset] = new Vector3(0f, 0f, z);
            normals[offset] = normal;
            uvs[offset] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < rimCount; i++)
            {
                vertices[offset + 1 + i] = new Vector3(rim[i].x, rim[i].y, z);
                normals[offset + 1 + i] = normal;
                uvs[offset + 1 + i] = rim[i] / (outerRadius * 2f) + new Vector2(0.5f, 0.5f);
            }
            for (int i = 0; i < rimCount; i++)
            {
                int a = offset + 1 + i;
                int b = offset + 1 + (i + 1) % rimCount;
                triangles[t++] = offset;
                // Unity dibuja los triángulos en sentido horario visto desde la cámara.
                if (face == 0)
                {
                    triangles[t++] = b;
                    triangles[t++] = a;
                }
                else
                {
                    triangles[t++] = a;
                    triangles[t++] = b;
                }
            }
        }

        // Costados: un quad por borde, con normal hacia afuera.
        int side = capVertices * 2;
        for (int i = 0; i < rimCount; i++)
        {
            Vector2 p0 = rim[i];
            Vector2 p1 = rim[(i + 1) % rimCount];
            Vector2 edge = p1 - p0;
            Vector3 normal = new Vector3(edge.y, -edge.x, 0f).normalized;
            int v = side + i * 4;
            vertices[v] = new Vector3(p0.x, p0.y, front);
            vertices[v + 1] = new Vector3(p1.x, p1.y, front);
            vertices[v + 2] = new Vector3(p0.x, p0.y, back);
            vertices[v + 3] = new Vector3(p1.x, p1.y, back);
            for (int k = 0; k < 4; k++) normals[v + k] = normal;
            uvs[v] = new Vector2(0f, 0f);
            uvs[v + 1] = new Vector2(1f, 0f);
            uvs[v + 2] = new Vector2(0f, 1f);
            uvs[v + 3] = new Vector2(1f, 1f);

            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            triangles[t++] = v + 1;
            triangles[t++] = v + 3;
            triangles[t++] = v + 2;
        }

        var mesh = new Mesh { name = "StarMesh" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
