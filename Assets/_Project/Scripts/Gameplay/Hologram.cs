using UnityEngine;

/// <summary>
/// Parpadeo y temblor del holograma de Tito (obstáculo falso, Obstacle.isFake = true).
/// Usa un material en modo Fade (Mat_Hologram) y le cambia la transparencia.
/// </summary>
[DisallowMultipleComponent]
public class Hologram : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Tooltip("Parte visual que tiembla.")]
    [SerializeField] private Transform visual;
    [Tooltip("Velocidad del parpadeo.")]
    [SerializeField, Min(0f)] private float flickerSpeed = 9f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.15f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.5f;
    [Tooltip("Cuánto tiembla (m).")]
    [SerializeField, Min(0f)] private float jitter = 0.04f;

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private Color baseColor = Color.red;
    private Vector3 visualHome;
    private float seed;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        if (renderers.Length > 0 && renderers[0].sharedMaterial != null) baseColor = renderers[0].sharedMaterial.color;
        if (visual != null) visualHome = visual.localPosition;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        float t = Time.time * flickerSpeed + seed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PerlinNoise(t, seed));
        if (Mathf.PerlinNoise(seed, t * 0.5f) > 0.8f) alpha *= 0.3f; // apagones cortos
        Color color = baseColor;
        color.a = alpha;
        block.SetColor(ColorId, color);
        foreach (Renderer r in renderers) r.SetPropertyBlock(block);

        if (visual != null)
            visual.localPosition = visualHome + new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, 0f, 0f) * (jitter * 2f);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los prefabs.</summary>
    public void EditorSetup(Transform visual) => this.visual = visual;
#endif
}
