using UnityEngine;

/// <summary>
/// Pasa el estado del concursante al Animator del modelo (personaje de Kenney).
/// El Animator Controller tiene un estado por animación con los nombres de abajo (lo arma el menú de Otoniel).
/// </summary>
[DisallowMultipleComponent]
public class ContestantAnimator : MonoBehaviour
{
    public const string IdleState = "Idle";
    public const string RunState = "Run";
    public const string JumpState = "Jump";
    public const string SlideState = "Slide";
    public const string FallState = "Fall";
    public const string HitState = "Hit";
    public const string WinState = "Win";
    public const string LoseState = "Lose";

    [SerializeField] private ContestantController contestant;
    [SerializeField] private Animator animator;
    [Tooltip("Segundos de mezcla entre animaciones.")]
    [SerializeField, Min(0f)] private float crossFade = 0.08f;
    [Tooltip("Velocidad de la pista (m/s) a la que la animación de correr va a 1x.")]
    [SerializeField, Min(0.1f)] private float runSpeedReference = 8f;
    [SerializeField, Min(0.1f)] private float minRunAnimSpeed = 0.9f;
    [SerializeField, Min(0.1f)] private float maxRunAnimSpeed = 1.6f;

    private ContestantState lastState = (ContestantState)(-1);
    private float jumpClipLength = 0.5f;

    private void Awake()
    {
        if (contestant == null) contestant = GetComponent<ContestantController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("ContestantAnimator: falta el Animator con su controller.", this);
            enabled = false;
            return;
        }
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "jump") jumpClipLength = clip.length;
        }
    }

    private void Update()
    {
        if (contestant == null) return;
        ContestantState state = contestant.State;
        if (state != lastState)
        {
            animator.CrossFadeInFixedTime(StateName(state), crossFade);
            lastState = state;
        }

        GameManager gm = GameManager.Instance;
        animator.speed = state switch
        {
            ContestantState.Running when gm != null =>
                Mathf.Clamp(gm.CurrentSpeed / runSpeedReference, minRunAnimSpeed, maxRunAnimSpeed),
            ContestantState.Jumping => jumpClipLength / Mathf.Max(contestant.CurrentJumpDuration, 0.05f),
            _ => 1f,
        };
    }

    private static string StateName(ContestantState state) => state switch
    {
        ContestantState.Running => RunState,
        ContestantState.Jumping => JumpState,
        ContestantState.Sliding => SlideState,
        ContestantState.Falling => FallState,
        ContestantState.Hit => HitState,
        ContestantState.Celebrating => WinState,
        ContestantState.Eliminated => LoseState,
        _ => IdleState,
    };

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean el prefab.</summary>
    public void EditorSetup(ContestantController contestant, Animator animator)
    {
        this.contestant = contestant;
        this.animator = animator;
    }
#endif
}
