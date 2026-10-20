using UnityEngine;

/// <summary>
/// Escenario 3D detrás del menú principal: Tito y la concursante saludan, aplauden o saltan por turnos y
/// los cañones tiran confeti cada tanto. Usa tiempo sin escala (no depende de la pausa).
/// </summary>
[DisallowMultipleComponent]
public class MenuStage : MonoBehaviour
{
    [Tooltip("Personajes principales (Animator con los estados del público: Idle, Cheer, Wave, Jump).")]
    [SerializeField] private Animator[] performers = new Animator[0];
    [SerializeField] private ParticleSystem[] confettiCannons = new ParticleSystem[0];
    [Tooltip("Segundos entre gestos de los personajes.")]
    [SerializeField, Min(0.5f)] private float emoteInterval = 3.2f;
    [Tooltip("Cada cuántos gestos se dispara el confeti.")]
    [SerializeField, Min(1)] private int confettiEvery = 3;
    [Tooltip("Segundos que dura cada gesto antes de volver a quieto.")]
    [SerializeField, Min(0.2f)] private float emoteTime = 1.6f;

    private static readonly string[] Emotes = { AudienceCrowd.WaveState, AudienceCrowd.CheerState, AudienceCrowd.JumpState };

    private float nextEmoteAt;
    private float backToIdleAt = -1f;
    private int beat;
    private int performer;

    private void Start()
    {
        nextEmoteAt = Time.unscaledTime + 1f;
        foreach (Animator animator in performers)
        {
            if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        if (backToIdleAt >= 0f && now >= backToIdleAt)
        {
            backToIdleAt = -1f;
            foreach (Animator animator in performers)
            {
                if (animator != null) animator.CrossFadeInFixedTime(AudienceCrowd.IdleState, 0.15f);
            }
        }
        if (now < nextEmoteAt) return;

        nextEmoteAt = now + emoteInterval;
        beat++;
        if (performers.Length > 0)
        {
            // Por turnos y, a veces, todos juntos con el confeti.
            bool together = beat % confettiEvery == 0;
            performer = (performer + 1) % performers.Length;
            string emote = together ? AudienceCrowd.JumpState : Emotes[Random.Range(0, Emotes.Length)];
            for (int i = 0; i < performers.Length; i++)
            {
                if (performers[i] != null && (together || i == performer)) performers[i].CrossFadeInFixedTime(emote, 0.12f);
            }
            backToIdleAt = now + emoteTime;
        }
        if (beat % confettiEvery == 0)
        {
            foreach (ParticleSystem cannon in confettiCannons)
            {
                if (cannon != null) cannon.Play(true);
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman el menú.</summary>
    public void EditorSetup(Animator[] performers, ParticleSystem[] confettiCannons)
    {
        this.performers = performers;
        this.confettiCannons = confettiCannons;
    }
#endif
}
