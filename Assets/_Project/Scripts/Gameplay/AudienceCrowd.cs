using UnityEngine;

/// <summary>
/// Público de las gradas (personajes de Kenney): aplaude en las rachas, salta al ganar la ronda,
/// hace «¡uy!» con las caídas y saluda cuando Tito anuncia una regla. Cada persona reacciona con un
/// pequeño retraso al azar para que no se muevan todos iguales. Entre reacción y reacción, algunos
/// saludan o aplauden solos para que el estudio se vea vivo (también en el menú).
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
    [Tooltip("Cada cuántos segundos algunos del público saludan o aplauden solos (0 = nunca).")]
    [SerializeField, Min(0f)] private float ambientInterval = 2.5f;
    [Tooltip("Qué parte del público se mueve sola en cada ocasión (0 a 1).")]
    [SerializeField, Range(0f, 1f)] private float ambientShare = 0.12f;

    private float[] startAt;
    private float[] returnAt;
    private string[] pending;
    private float nextAmbientAt;

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
        SurpriseEvents.SurpriseStarted += HandleSurprise;
    }

    private void OnDisable()
    {
        StarStreak.MilestoneReached -= HandleMilestone;
        GameEvents.RoundWon -= HandleRoundWon;
        GameEvents.ContestantHit -= HandleHit;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.RuleAnnounced -= HandleRuleAnnounced;
        SurpriseEvents.SurpriseStarted -= HandleSurprise;
    }

    private void HandleMilestone(int streak, int bonus) => React(CheerState, streakShare);
    private void HandleRoundWon(int round, int prizes) => React(JumpState, 1f);
    private void HandleHit(HitCause cause) => React(OohState, 0.7f);
    private void HandleGameOver(GameStats stats) => React(OohState, 1f);
    private void HandleSurprise(SurpriseType type, float duration) =>
        React(type == SurpriseType.Blackout ? OohState : CheerState, 0.6f);

    private void HandleRuleAnnounced(RuleData rule, bool isLie)
    {
        if (rule != null) React(WaveState, 0.4f);
    }

    private void React(string state, float share, bool onlyIdle = false)
    {
        float now = Time.time;
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null || Random.value > share) continue;
            if (onlyIdle && (startAt[i] >= 0f || returnAt[i] >= 0f)) continue;
            pending[i] = state;
            startAt[i] = now + Random.Range(0f, maxDelay);
        }
    }

    private void Update()
    {
        float now = Time.time;
        if (ambientInterval > 0f && now >= nextAmbientAt)
        {
            nextAmbientAt = now + ambientInterval * Random.Range(0.7f, 1.3f);
            React(Random.value < 0.6f ? WaveState : CheerState, ambientShare, true);
        }
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
