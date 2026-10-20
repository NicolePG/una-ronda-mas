using UnityEngine;

/// <summary>
/// Olas del agua: desplaza dos mapas de normales en direcciones distintas para que la superficie se mueva y brille.
/// Usa una copia del material solo en Play (no cambia el asset).
/// </summary>
[RequireComponent(typeof(Renderer))]
[DisallowMultipleComponent]
public class WaterSurface : MonoBehaviour
{
    private static readonly int BumpId = Shader.PropertyToID("_BumpMap");
    private static readonly int DetailBumpId = Shader.PropertyToID("_DetailNormalMap");

    [Tooltip("Velocidad de la primera capa de olas (UV por segundo).")]
    [SerializeField] private Vector2 speedA = new Vector2(0.015f, 0.04f);
    [Tooltip("Velocidad de la segunda capa de olas.")]
    [SerializeField] private Vector2 speedB = new Vector2(-0.025f, 0.03f);

    private Material material;

    private void Awake() => material = GetComponent<Renderer>().material;

    private void Update()
    {
        if (material == null) return;
        material.SetTextureOffset(BumpId, speedA * Time.time);
        material.SetTextureOffset(DetailBumpId, speedB * Time.time);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
