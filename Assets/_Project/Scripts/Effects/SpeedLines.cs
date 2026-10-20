using UnityEngine;

/// <summary>
/// Líneas de velocidad a los costados de la pista: salen más y más rápido cuanto más rápida es la ronda.
/// Solo durante la carrera.
/// </summary>
[DisallowMultipleComponent]
public class SpeedLines : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] emitters = new ParticleSystem[0];
    [Tooltip("Líneas por segundo por cada m/s de la pista.")]
    [SerializeField, Min(0f)] private float ratePerSpeed = 1.6f;
    [Tooltip("Velocidad de la pista a partir de la cual empiezan a verse (m/s).")]
    [SerializeField, Min(0f)] private float minSpeed = 9f;
    [Tooltip("Las líneas van así de rápido respecto a la pista.")]
    [SerializeField, Min(0.1f)] private float speedFactor = 1.6f;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        float speed = gm != null ? gm.CurrentSpeed : 0f;
        float rate = speed > minSpeed ? (speed - minSpeed * 0.5f) * ratePerSpeed : 0f;
        foreach (ParticleSystem ps in emitters)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var main = ps.main;
            main.startSpeed = speed * speedFactor;
        }
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(ParticleSystem[] emitters) => this.emitters = emitters;
#endif
}
