using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elige la regla de cada ronda, decide si Tito miente y resuelve el botón «¡Trampa!» (tecla T).
/// Se puede acusar cuantas veces se quiera: acierta si hay una trampa en vivo abierta (TitoTricks) o si la regla
/// anunciada es mentira; paga más en cada ronda y con la racha de detective. Una falsa alarma quita una vida,
/// corta la racha y bloquea el botón unos segundos. Descubrir la mentira cancela la regla real.
/// Va en el mismo GameObject que GameManager, que lo llama en cada ronda.
/// </summary>
[DisallowMultipleComponent]
public class RuleManager : MonoBehaviour
{
    [Header("Reglas")]
    [Tooltip("Las reglas posibles (Data/Rules/*.asset).")]
    [SerializeField] private RuleData[] rules;

    [Header("Trampas en vivo")]
    [Tooltip("Si se deja vacío, se busca en este mismo GameObject.")]
    [SerializeField] private TitoTricks tricks;

    [Header("Entrada")]
    [SerializeField] private KeyCode cheatKey = KeyCode.T;

    private readonly List<RuleData> candidates = new List<RuleData>();
    private RuleData lastAnnounced;
    private float cooldownUntil;

    /// <summary>Lo que Tito dice (null en rondas sin regla). Es lo que debe mostrar la UI.</summary>
    public RuleData AnnouncedRule { get; private set; }

    /// <summary>Lo que realmente pasa (null en rondas sin regla o si ya se descubrió la mentira).</summary>
    public RuleData AppliedRule { get; private set; }

    public bool IsLie { get; private set; }

    /// <summary>El jugador ya descubrió la mentira de esta ronda.</summary>
    public bool LieExposed { get; private set; }

    /// <summary>Se acusó al menos una vez en esta ronda.</summary>
    public bool CheatUsed { get; private set; }

    /// <summary>Aciertos seguidos sin falsa alarma en la partida.</summary>
    public int DetectiveStreak { get; private set; }

    /// <summary>Estrellas que dio el último acierto (0 si fue falsa alarma).</summary>
    public int LastCheatReward { get; private set; }

    /// <summary>El último acierto fue descubrir la regla falsa (no una trampa en vivo).</summary>
    public bool LastCatchWasLie { get; private set; }

    /// <summary>La última falsa alarma no quitó vida (premio Silbato).</summary>
    public bool LastFalseAlarmForgiven { get; private set; }

    public TitoTricks Tricks => tricks;
    public IReadOnlyList<RuleData> Rules => rules;

    /// <summary>Segundos que faltan para poder acusar de nuevo tras una falsa alarma.</summary>
    public float CooldownLeft => Mathf.Max(0f, cooldownUntil - Time.time);

    /// <summary>¿Hay algo que Tito pueda estar tramando esta ronda? (el botón se muestra).</summary>
    public bool CheatsPossible
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && (AnnouncedRule != null || gm.CurrentRound >= gm.Balance.FirstTrickRound);
        }
    }

    /// <summary>True si ahora se puede acusar a Tito (para el botón del HUD).</summary>
    public bool CanCallCheat
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.Phase == RoundPhase.Running && !gm.IsPaused && CheatsPossible && CooldownLeft <= 0f;
        }
    }

    /// <summary>Hay una mentira de regla que todavía nadie descubrió.</summary>
    public bool HasHiddenLie => IsLie && !LieExposed;

    public bool IsApplied(RuleType type)
    {
        if (AppliedRule != null && AppliedRule.Type == type) return true;
        // Los controles cruzados de las trampas en vivo funcionan como el Espejo.
        return type == RuleType.Mirror && tricks != null && tricks.CrossedControls;
    }

    private void Awake()
    {
        if (tricks == null) tricks = GetComponent<TitoTricks>();
    }

    /// <summary>Elige la regla anunciada y la aplicada para la ronda.</summary>
    public void PrepareRound(int round, BalanceConfig balance)
    {
        CheatUsed = false;
        LieExposed = false;
        AnnouncedRule = null;
        AppliedRule = null;
        IsLie = false;
        if (balance == null || round < balance.FirstRuleRound) return;

        // No repetir la regla anunciada de la ronda anterior.
        AnnouncedRule = PickRandom(lastAnnounced);
        if (AnnouncedRule == null) return;
        lastAnnounced = AnnouncedRule;

        bool canLie = CountTypes() > 1;
        IsLie = canLie && Random.value < balance.GetLieChance(round);
        AppliedRule = IsLie ? PickRandom(AnnouncedRule) : AnnouncedRule;
        if (AppliedRule == null || AppliedRule.Type == AnnouncedRule.Type)
        {
            AppliedRule = AnnouncedRule;
            IsLie = false;
        }
    }

    public void AnnounceRule() => GameEvents.RaiseRuleAnnounced(AnnouncedRule, IsLie);

    public void ApplyRule() => GameEvents.RaiseRuleApplied(AppliedRule);

    private void Update()
    {
        if (Input.GetKeyDown(cheatKey)) CallCheat();
    }

    /// <summary>Acusa a Tito. Se puede llamar desde un botón de la UI. Devuelve true si acertó.</summary>
    public bool CallCheat()
    {
        if (!CanCallCheat) return false;
        GameManager gm = GameManager.Instance;
        BalanceConfig b = gm.Balance;
        CheatUsed = true;

        bool caughtTrick = tricks != null && tricks.TryCatch();
        LastCatchWasLie = !caughtTrick && HasHiddenLie;
        bool correct = caughtTrick || LastCatchWasLie;
        if (LastCatchWasLie) ExposeLie(gm);

        LastFalseAlarmForgiven = false;
        if (correct)
        {
            DetectiveStreak++;
            float streak = Mathf.Min(b.DetectiveMaxMultiplier, 1f + b.DetectiveStreakBonus * (DetectiveStreak - 1));
            LastCheatReward = Mathf.RoundToInt(b.GetCheatReward(gm.CurrentRound) * streak * gm.Modifiers.CheatRewardMultiplier);
        }
        else
        {
            DetectiveStreak = 0;
            LastCheatReward = 0;
            cooldownUntil = Time.time + b.FalseAlarmCooldown;
            LastFalseAlarmForgiven = gm.Modifiers.TryUseWhistle();
        }

        GameEvents.RaiseCheatCalled(correct);
        if (correct) gm.AddBonusStars(LastCheatReward);
        else if (LastFalseAlarmForgiven) GameEvents.RaisePrizeTriggered(PrizeType.Whistle);
        else gm.LoseLife(b.CheatPenaltyLives);
        return correct;
    }

    // La regla real se cancela: se prenden las luces, vuelven los controles y se van los hologramas.
    private void ExposeLie(GameManager gm)
    {
        LieExposed = true;
        AppliedRule = null;
        if (gm.Track != null) gm.Track.RemoveHolograms();
        GameEvents.RaiseRuleApplied(null);
    }

    private RuleData PickRandom(RuleData exclude)
    {
        candidates.Clear();
        if (rules == null) return null;
        foreach (RuleData rule in rules)
        {
            if (rule != null && (exclude == null || rule.Type != exclude.Type)) candidates.Add(rule);
        }
        if (candidates.Count == 0)
        {
            // Solo hay una regla: se repite.
            foreach (RuleData rule in rules)
            {
                if (rule != null) candidates.Add(rule);
            }
        }
        return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
    }

    private int CountTypes()
    {
        int mask = 0;
        if (rules != null)
        {
            foreach (RuleData rule in rules)
            {
                if (rule != null) mask |= 1 << (int)rule.Type;
            }
        }
        int count = 0;
        for (; mask != 0; mask >>= 1) count += mask & 1;
        return count;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Fuerza la regla de la ronda actual (pruebas).</summary>
    public void DebugForceRule(RuleData announced, RuleData applied)
    {
        AnnouncedRule = announced;
        AppliedRule = applied;
        IsLie = announced != null && applied != null && announced.Type != applied.Type;
        LieExposed = false;
    }
#endif
}
