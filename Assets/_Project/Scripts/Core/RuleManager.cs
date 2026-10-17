using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elige la regla de cada ronda, decide si Tito miente y resuelve el botón «¡Trampa!» (tecla T).
/// Va en el mismo GameObject que GameManager, que lo llama en cada ronda.
/// </summary>
[DisallowMultipleComponent]
public class RuleManager : MonoBehaviour
{
    [Header("Reglas")]
    [Tooltip("Las reglas posibles (Data/Rules/*.asset).")]
    [SerializeField] private RuleData[] rules;

    [Header("Entrada")]
    [SerializeField] private KeyCode cheatKey = KeyCode.T;

    private readonly List<RuleData> candidates = new List<RuleData>();
    private RuleData lastAnnounced;

    /// <summary>Lo que Tito dice (null en rondas sin regla). Es lo que debe mostrar la UI.</summary>
    public RuleData AnnouncedRule { get; private set; }

    /// <summary>Lo que realmente pasa (null en rondas sin regla). Lo usan los efectos y el concursante.</summary>
    public RuleData AppliedRule { get; private set; }

    public bool IsLie { get; private set; }
    public bool CheatUsed { get; private set; }
    public IReadOnlyList<RuleData> Rules => rules;

    /// <summary>True si ahora se puede acusar a Tito (para el aviso del HUD).</summary>
    public bool CanCallCheat
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.Phase == RoundPhase.Running && !gm.IsPaused
                && AnnouncedRule != null && !CheatUsed;
        }
    }

    public bool IsApplied(RuleType type) => AppliedRule != null && AppliedRule.Type == type;

    /// <summary>Elige la regla anunciada y la aplicada para la ronda.</summary>
    public void PrepareRound(int round, BalanceConfig balance)
    {
        CheatUsed = false;
        AnnouncedRule = null;
        AppliedRule = null;
        IsLie = false;
        if (balance == null || round < balance.FirstRuleRound) return;

        // No repetir la regla anunciada de la ronda anterior.
        AnnouncedRule = PickRandom(lastAnnounced);
        if (AnnouncedRule == null) return;
        lastAnnounced = AnnouncedRule;

        bool canLie = round >= balance.FirstLieRound && CountTypes() > 1;
        IsLie = canLie && Random.value < balance.LieChance;
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
        CheatUsed = true;
        bool correct = IsLie;
        GameEvents.RaiseCheatCalled(correct);
        if (correct) gm.AddBonusStars(gm.Balance.CheatRewardStars);
        else gm.LoseLife(gm.Balance.CheatPenaltyLives);
        return correct;
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
    }
#endif
}
