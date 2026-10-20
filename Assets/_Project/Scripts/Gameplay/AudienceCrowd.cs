using UnityEngine;

/// <summary>
/// Público de las gradas (personajes de Kenney): aplaude en las rachas, salta al ganar la ronda,
/// hace «¡uy!» con las caídas y saluda cuando Tito anuncia una regla. Cada persona reacciona con un
/// pequeño retraso al azar para que no se muevan todos iguales.
/// </summary>
[DisallowMultipleComponent]
public class AudienceCrowd : MonoBehaviour
{
    public const string IdleState = "Idle";
    public const string CheerState = "Cheer";
    public const string OohState = "Ooh";
    public const string JumpState = "Jump";
    public const string WaveState = "Wave";

    [SerializeField] private Animator[] members = new Animator[0];
    [Tooltip("Retraso máximo al azar antes de reaccionar (s).")]
    [SerializeField, Min(0f)] private float maxDelay = 0.35f;
    [Tooltip("Qué parte del público aplaude en un hito de racha (0 a 1).")]
    [SerializeField, Range(0f, 1f)] private float streakShare = 0.5f;
    [Tooltip("Segundos que dura una reacción antes de volver a quedarse quieto.")]
    [SerializeField, Min(0.1f)] private float reactionTime = 1.4f;
    [SerializeField, Min(0f)] private float crossFade = 0.12f;

    private float[] startAt;
    private float[] returnAt;
    private string[] pending;

    private void Awake()
    {
        startAt = new float[members.Length];
        returnAt = new float[members.Length];
        pending = new string[members.Length];
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null) continue;
            members[i].Play(IdleState, 0, Random.value);   // cada uno en un punto distinto de la animación
            members[i].speed = Random.Range(0.85f, 1.15f);
            returnAt[i] = -1f;
            startAt[i] = -1f;
        }
    }

    private void OnEnable()
    {
        StarStreak.MilestoneReached += HandleMilestone;
        GameEvents.RoundWon += HandleRoundWon;
        GameEvents.ContestantHit += HandleHit;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.RuleAnnounced += HandleRuleAnnounced;
    }

    private void OnDisable()
    {
        StarStreak.MilestoneReached -= HandleMilestone;
        GameEvents.RoundWon -= HandleRoundWon;
        GameEvents.ContestantHit -= HandleHit;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.RuleAnnounced -= HandleRuleAnnounced;
    }

    private void HandleMilestone(int streak, int bonus) => React(CheerState, streakShare);
    private void HandleRoundWon(int round, int prizes) => React(JumpState, 1f);
    private void HandleHit(HitCause cause) => React(OohState, 0.7f);
    private void HandleGameOver(GameStats stats) => React(OohState, 1f);

    private void HandleRuleAnnounced(RuleData rule, bool isLie)
    {
        if (rule != null) React(WaveState, 0.4f);
    }

    private void React(string state, float share)
    {
        float now = Time.time;
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null || Random.value > share) continue;
            pending[i] = state;
            startAt[i] = now + Random.Range(0f, maxDelay);
        }
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = 0; i < members.Length; i++)
        {
            Animator member = members[i];
            if (member == null) continue;
            if (startAt[i] >= 0f && now >= startAt[i])
            {
                member.CrossFadeInFixedTime(pending[i], crossFade);
                startAt[i] = -1f;
                returnAt[i] = now + reactionTime;
            }
            else if (returnAt[i] >= 0f && now >= returnAt[i])
            {
                member.CrossFadeInFixedTime(IdleState, crossFade);
                returnAt[i] = -1f;
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman el estudio.</summary>
    public void EditorSetup(Animator[] members) => this.members = members;
#endif
}
