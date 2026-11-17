using UnityEngine;

/// <summary>
/// Público de las gradas (personajes de Kenney): aplaude en las rachas, salta al ganar la ronda,
/// hace «¡uy!» con las caídas y saluda cuando Tito anuncia una regla. Cada persona reacciona con un
/// pequeño retraso al azar para que no se muevan todos iguales. Entre reacción y reacción, algunos
/// saludan o aplauden solos para que el estudio se vea vivo (también en el menú).
/// En el juego las gradas se van llenando: en la ronda 1 hay poca gente y cada ronda llegan más (saltando de
/// alegría) hasta llenar el estudio (BalanceConfig, «Público»). En el menú está lleno.
/// </summary>
[DisallowMultipleComponent]
public class AudienceCrowd : MonoBehaviour
{
    private const float ArrivalTime = 0.45f;     // segundos que tarda en «aparecer» cada persona nueva
    private const float ArrivalSpread = 1.2f;    // los nuevos llegan escalonados en este tiempo
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

    // Gradas que se llenan
    private int[] arrivalOrder;
    private Vector3[] homeScale;
    private float[] arriveAt;
    private int visibleCount = -1;

    private void Awake()
    {
        startAt = new float[members.Length];
        returnAt = new float[members.Length];
        pending = new string[members.Length];
        homeScale = new Vector3[members.Length];
        arriveAt = new float[members.Length];
        for (int i = 0; i < members.Length; i++)
        {
            returnAt[i] = -1f;
            startAt[i] = -1f;
            arriveAt[i] = -1f;
            if (members[i] == null) continue;
            homeScale[i] = members[i].transform.localScale;
            members[i].cullingMode = AnimatorCullingMode.CullUpdateTransforms; // fuera de cámara no se anima (WebGL)
            members[i].Play(IdleState, 0, Random.value);   // cada uno en un punto distinto de la animación
            members[i].speed = Random.Range(0.85f, 1.15f);
        }

        // Orden de llegada fijo: la misma gente se queda de una ronda a otra.
        arrivalOrder = new int[members.Length];
        for (int i = 0; i < arrivalOrder.Length; i++) arrivalOrder[i] = i;
        var random = new System.Random(7);
        for (int i = arrivalOrder.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (arrivalOrder[i], arrivalOrder[j]) = (arrivalOrder[j], arrivalOrder[i]);
        }
    }

    private void Start()
    {
        if (visibleCount >= 0) return; // GameManager ya avisó la ronda
        GameManager gm = GameManager.Instance;
        SetAttendance(gm != null ? gm.Balance.GetCrowdShare(Mathf.Max(1, gm.CurrentRound)) : 1f, false);
    }

    private void OnEnable()
    {
        GameEvents.PhaseChanged += HandlePhaseChanged;
        StarStreak.MilestoneReached += HandleMilestone;
        GameEvents.RoundWon += HandleRoundWon;
        GameEvents.ContestantHit += HandleHit;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.RuleAnnounced += HandleRuleAnnounced;
        GameEvents.CheatCalled += HandleCheatCalled;
        SurpriseEvents.SurpriseStarted += HandleSurprise;
        TitoTricks.TrickStarted += HandleTrickStarted;
    }

    private void OnDisable()
    {
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        StarStreak.MilestoneReached -= HandleMilestone;
        GameEvents.RoundWon -= HandleRoundWon;
        GameEvents.ContestantHit -= HandleHit;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.RuleAnnounced -= HandleRuleAnnounced;
        GameEvents.CheatCalled -= HandleCheatCalled;
        SurpriseEvents.SurpriseStarted -= HandleSurprise;
        TitoTricks.TrickStarted -= HandleTrickStarted;
    }

    private void HandleMilestone(int streak, int bonus) => React(CheerState, streakShare);
    private void HandleRoundWon(int round, int prizes) => React(JumpState, 1f);
    private void HandleHit(HitCause cause) => React(OohState, 0.7f);
    private void HandleGameOver(GameStats stats) => React(OohState, 1f);
    private void HandleSurprise(SurpriseType type, float duration) =>
        React(type == SurpriseType.Blackout ? OohState : CheerState, 0.6f);
    // Un murmullo cuando Tito trama algo (también en los faroles) y fiesta si lo pillan.
    private void HandleTrickStarted(TrickType type, bool bluff, int amount) => React(OohState, 0.35f);
    private void HandleCheatCalled(bool correct) => React(correct ? CheerState : OohState, correct ? 0.9f : 0.5f);

    private void HandleRuleAnnounced(RuleData rule, bool isLie)
    {
        if (rule != null) React(WaveState, 0.4f);
    }

    // Cada anuncio de ronda llega más gente (la primera vez, sin animación).
    private void HandlePhaseChanged(RoundPhase phase)
    {
        GameManager gm = GameManager.Instance;
        if (phase != RoundPhase.Announcement || gm == null) return;
        SetAttendance(gm.Balance.GetCrowdShare(gm.CurrentRound), visibleCount >= 0);
    }

    private void SetAttendance(float share, bool animate)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(share * members.Length), 1, members.Length);
        float now = Time.time;
        for (int k = 0; k < arrivalOrder.Length; k++)
        {
            int i = arrivalOrder[k];
            Animator member = members[i];
            if (member == null) continue;
            bool visible = k < count;
            if (member.gameObject.activeSelf == visible) continue;
            member.gameObject.SetActive(visible);
            startAt[i] = returnAt[i] = arriveAt[i] = -1f;
            if (!visible) continue;
            member.Play(IdleState, 0, Random.value);
            if (!animate)
            {
                member.transform.localScale = homeScale[i];
                continue;
            }
            // Entra saltando: aparece desde chiquito y festeja.
            arriveAt[i] = now + Random.Range(0f, ArrivalSpread);
            member.transform.localScale = Vector3.zero;
            pending[i] = Random.value < 0.5f ? JumpState : CheerState;
            startAt[i] = arriveAt[i];
        }
        visibleCount = count;
    }

    private void React(string state, float share, bool onlyIdle = false)
    {
        float now = Time.time;
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null || !members[i].gameObject.activeInHierarchy || Random.value > share) continue;
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
            if (member == null || !member.gameObject.activeInHierarchy) continue;
            if (arriveAt[i] >= 0f) UpdateArrival(i, member, now);
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

    // Crece desde 0 con un pequeño rebote.
    private void UpdateArrival(int i, Animator member, float now)
    {
        float t = (now - arriveAt[i]) / ArrivalTime;
        if (t < 0f) return;
        if (t >= 1f)
        {
            member.transform.localScale = homeScale[i];
            arriveAt[i] = -1f;
            return;
        }
        float back = 1f + 2.4f * Mathf.Pow(t - 1f, 3f) + 1.4f * Mathf.Pow(t - 1f, 2f);
        member.transform.localScale = homeScale[i] * back;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman el estudio.</summary>
    public void EditorSetup(Animator[] members) => this.members = members;
#endif
}
