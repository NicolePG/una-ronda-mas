using UnityEngine;

/// <summary>
/// Tito Trampa en persona: modelo 3D en su tarima al lado de la pista (la cara de la pantalla gigante es HostFace, de Tania).
/// Reacciona a la partida: señala al anunciar la regla, se ríe con cada caída y se asusta si lo descubren.
/// </summary>
[DisallowMultipleComponent]
public class HostPresenter : MonoBehaviour
{
    public const string IdleState = "Idle";
    public const string LaughState = "Laugh";
    public const string PointState = "Point";
    public const string ShockState = "Shock";

    [SerializeField] private Animator animator;
    [Tooltip("Segundos de mezcla entre animaciones.")]
    [SerializeField, Min(0f)] private float crossFade = 0.1f;
    [Tooltip("Veces que repite la risa.")]
    [SerializeField, Min(1)] private int laughRepeats = 2;

    private string currentState;
    private int repeatsLeft;
    private float nextRepeatAt = -1f;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("HostPresenter: falta el Animator con su controller.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        GameEvents.RuleAnnounced += HandleRuleAnnounced;
        GameEvents.ContestantHit += HandleContestantHit;
        GameEvents.CheatCalled += HandleCheatCalled;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.RoundWon += HandleRoundWon;
    }

    private void OnDisable()
    {
        GameEvents.RuleAnnounced -= HandleRuleAnnounced;
        GameEvents.ContestantHit -= HandleContestantHit;
        GameEvents.CheatCalled -= HandleCheatCalled;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.RoundWon -= HandleRoundWon;
    }

    private void HandleRuleAnnounced(RuleData rule, bool isLie) => Play(PointState, 1);
    private void HandleContestantHit(HitCause cause) => Play(LaughState, laughRepeats);
    private void HandleCheatCalled(bool correct) => Play(correct ? ShockState : LaughState, correct ? 1 : laughRepeats);
    private void HandleGameOver(GameStats stats) => Play(LaughState, laughRepeats + 1);
    private void HandleRoundWon(int round, int prizes) => Play(PointState, 1);

    private void Play(string state, int repeats)
    {
        if (!enabled) return;
        animator.CrossFadeInFixedTime(state, crossFade);
        currentState = state;
        repeatsLeft = repeats;
        nextRepeatAt = Time.time + ClipLength(state);
    }

    private void Update()
    {
        if (nextRepeatAt < 0f || Time.time < nextRepeatAt) return;
        repeatsLeft--;
        if (repeatsLeft > 0)
        {
            // Las animaciones de un solo tiro se repiten empezando el estado de nuevo.
            animator.Play(currentState, 0, 0f);
            nextRepeatAt = Time.time + ClipLength(currentState);
            return;
        }
        nextRepeatAt = -1f;
        animator.CrossFadeInFixedTime(IdleState, crossFade);
    }

    private float ClipLength(string state)
    {
        string clipName = state switch
        {
            LaughState => "emote-yes",
            PointState => "interact-right",
            ShockState => "emote-no",
            _ => "idle",
        };
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip.length;
        }
        return 1f;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean el prefab.</summary>
    public void EditorSetup(Animator animator) => this.animator = animator;
#endif
}
