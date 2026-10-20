using UnityEngine;

/// <summary>
/// Focos de marquesina del estudio: se prenden en ola (persecución) y parpadean todos juntos al ganar la ronda.
/// Cambia el color de emisión de cada foco con MaterialPropertyBlock (sin copiar materiales).
/// </summary>
[DisallowMultipleComponent]
public class MarqueeLights : MonoBehaviour
{
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    [Tooltip("Focos en orden alrededor del cartel.")]
    [SerializeField] private Renderer[] bulbs = new Renderer[0];
    [SerializeField] private Color onColor = new Color(1f, 0.75f, 0.2f);
    [SerializeField] private Color offColor = new Color(0.25f, 0.15f, 0.04f);
    [Tooltip("Intensidad del brillo de un foco prendido (la toma el bloom).")]
    [SerializeField, Min(0f)] private float onIntensity = 2.2f;
    [Tooltip("Focos por segundo que avanza la ola.")]
    [SerializeField, Min(0.1f)] private float chaseSpeed = 10f;
    [Tooltip("Uno de cada tantos focos va prendido.")]
    [SerializeField, Min(2)] private int spacing = 3;
    [Tooltip("Segundos de parpadeo al ganar la ronda.")]
    [SerializeField, Min(0f)] private float celebrateTime = 3f;
    [Tooltip("Parpadeos por segundo al celebrar.")]
    [SerializeField, Min(0.1f)] private float celebrateBlink = 5f;

    private MaterialPropertyBlock block;
    private float celebrateUntil = -1f;

    private void Awake() => block = new MaterialPropertyBlock();

    private void OnEnable() => GameEvents.RoundWon += HandleRoundWon;

    private void OnDisable() => GameEvents.RoundWon -= HandleRoundWon;

    private void HandleRoundWon(int round, int prizes) => celebrateUntil = Time.unscaledTime + celebrateTime;

    private void Update()
    {
        bool celebrating = Time.unscaledTime < celebrateUntil;
        int head = Mathf.FloorToInt(Time.unscaledTime * chaseSpeed);
        bool blinkOn = Mathf.Repeat(Time.unscaledTime * celebrateBlink, 1f) < 0.5f;
        for (int i = 0; i < bulbs.Length; i++)
        {
            if (bulbs[i] == null) continue;
            bool on = celebrating ? blinkOn : (i + head) % spacing == 0;
            block.SetColor(EmissionId, on ? onColor * onIntensity : offColor);
            bulbs[i].SetPropertyBlock(block);
        }
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman el estudio.</summary>
    public void EditorSetup(Renderer[] bulbs) => this.bulbs = bulbs;
#endif
}
