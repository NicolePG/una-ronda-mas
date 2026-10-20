using UnityEngine;

/// <summary>
/// Estela del concursante: una cinta de luz que sale de sus pies y se queda atrás con la pista (el concursante
/// no avanza, así que los puntos viajan hacia −Z a la velocidad de la ronda). Dibuja las curvas de los cambios
/// de carril y los arcos de los saltos. También levanta polvo de agua al correr.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[DisallowMultipleComponent]
public class SpeedRibbon : MonoBehaviour
{
    [Tooltip("Segundos que dura cada punto de la estela.")]
    [SerializeField, Min(0.05f)] private float lifetime = 0.5f;
    [Tooltip("Puntos por segundo.")]
    [SerializeField, Min(10f)] private float sampleRate = 60f;
    [Tooltip("Gotas que levanta al correr (opcional).")]
    [SerializeField] private ParticleSystem runSpray;
    [Tooltip("Gotas por segundo por cada m/s de la pista.")]
    [SerializeField, Min(0f)] private float sprayPerSpeed = 2.5f;

    private LineRenderer line;
    private Vector3[] points;
    private float[] ages;
    private int count;
    private float sampleTimer;
    private ContestantController contestant;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        int capacity = Mathf.CeilToInt(lifetime * sampleRate) + 2;
        points = new Vector3[capacity];
        ages = new float[capacity];
        contestant = GetComponentInParent<ContestantController>();
    }

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        bool running = gm != null && gm.Phase == RoundPhase.Running && !gm.IsPaused;
        bool visible = running && contestant != null && contestant.State != ContestantState.Falling;
        float dt = Time.deltaTime;
        float speed = gm != null ? gm.CurrentSpeed : 0f;

        // Envejece y mueve los puntos con la pista.
        int kept = 0;
        for (int i = 0; i < count; i++)
        {
            ages[i] += dt;
            if (ages[i] >= lifetime) continue;
            points[kept] = points[i] + Vector3.back * speed * dt;
            ages[kept] = ages[i];
            kept++;
        }
        count = kept;

        // Punto nuevo en los pies (el más nuevo va primero para que el ancho se afine hacia atrás).
        sampleTimer += dt;
        if (visible && sampleTimer >= 1f / sampleRate && count < points.Length)
        {
            sampleTimer = 0f;
            for (int i = count; i > 0; i--)
            {
                points[i] = points[i - 1];
                ages[i] = ages[i - 1];
            }
            points[0] = transform.position;
            ages[0] = 0f;
            count++;
        }
        if (count > 0 && visible) points[0] = transform.position;

        line.positionCount = count;
        for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
        line.enabled = count > 1;

        if (runSpray != null)
        {
            var emission = runSpray.emission;
            bool grounded = visible && contestant.IsGrounded;
            emission.rateOverTime = grounded ? speed * sprayPerSpeed : 0f;
            var velocity = runSpray.velocityOverLifetime;
            velocity.z = new ParticleSystem.MinMaxCurve(-speed);
        }
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean el prefab.</summary>
    public void EditorSetup(ParticleSystem runSpray) => this.runSpray = runSpray;
#endif
}
