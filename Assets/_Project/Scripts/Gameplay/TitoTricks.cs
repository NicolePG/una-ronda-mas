using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrickType { StealStars, RigGoal, CrossedControls }

/// <summary>
/// Trampas en vivo de Tito: a mitad de la carrera se roba estrellas, infla la meta o cruza los controles, sin avisar.
/// Cada trampa abre una ventana de TrickWindow segundos para acusarlo con «¡Trampa!» (T): si lo pillan, la trampa
/// se deshace y paga premio; si no, queda hecha. Desde BluffFromRound también hace faroles: guiña y distrae igual,
/// pero no cambia nada (acusar un farol es falsa alarma). Va en GameSystems; RuleManager resuelve la acusación.
/// Números en BalanceConfig («Trampas en vivo de Tito»).
/// </summary>
[DisallowMultipleComponent]
public class TitoTricks : MonoBehaviour
{
    /// <summary>Empezó una trampa: tipo, ¿es farol?, estrellas que cambió (robadas o meta extra; 0 si no aplica).</summary>
    public static event Action<TrickType, bool, int> TrickStarted;
    /// <summary>Terminó la ventana: tipo, ¿era farol?, ¿lo descubrieron?</summary>
    public static event Action<TrickType, bool, bool> TrickEnded;

    private struct Plan
    {
        public float At;
        public TrickType Type;
        public bool Bluff;
    }

    private readonly List<Plan> plans = new List<Plan>();
    private readonly List<TrickType> validTypes = new List<TrickType>();
    private int nextPlan;
    private int changedAmount;

    /// <summary>Hay una ventana abierta (trampa real o farol): Tito está «sospechoso».</summary>
    public bool IsActive { get; private set; }
    public bool IsBluff { get; private set; }
    public TrickType ActiveType { get; private set; }
    public float WindowLeft { get; private set; }

    /// <summary>Controles cruzados en este momento (ContestantController lo lee como la regla Espejo).</summary>
    public bool CrossedControls => IsActive && !IsBluff && ActiveType == TrickType.CrossedControls;

    private void OnEnable()
    {
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        Cancel();
    }

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        plans.Clear();
        nextPlan = 0;
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        BalanceConfig b = gm.Balance;
        int count = b.GetTrickCount(round);
        if (count <= 0) return;

        // Cada trampa en su tramo de tiempo, sin pisarse con la ventana de la anterior.
        float slot = (b.TrickTimeEnd - b.TrickTimeStart) / count;
        float bluffChance = b.GetBluffChance(round);
        for (int i = 0; i < count; i++)
        {
            float jitter = UnityEngine.Random.Range(0f, Mathf.Max(0f, slot - b.TrickWindow));
            plans.Add(new Plan
            {
                At = b.TrickTimeStart + slot * i + jitter,
                Type = RandomType(round, b),
                Bluff = UnityEngine.Random.value < bluffChance,
            });
        }
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Running) return;
        plans.Clear();
        Cancel();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != RoundPhase.Running || gm.IsPaused) return;

#if UNITY_EDITOR
        // Para probar en el Editor: F8 lanza una trampa ya (Shift + F8, un farol).
        if (Input.GetKeyDown(KeyCode.F8) && !IsActive)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Begin(new Plan { Type = RandomType(gm.CurrentRound, gm.Balance), Bluff = shift }, gm);
        }
#endif
        if (IsActive)
        {
            WindowLeft -= Time.deltaTime;
            if (WindowLeft <= 0f) End(false);
            return;
        }

        if (nextPlan < plans.Count && gm.RoundElapsed >= plans[nextPlan].At) Begin(plans[nextPlan++], gm);
    }

    /// <summary>Lo llama RuleManager al acusar. True si había una trampa real (se deshace); un farol se cierra.</summary>
    public bool TryCatch()
    {
        if (!IsActive) return false;
        bool real = !IsBluff;
        if (real) Undo();
        End(real);
        return real;
    }

    private void Begin(Plan plan, GameManager gm)
    {
        BalanceConfig b = gm.Balance;
        TrickType type = PickValidType(plan.Type, gm);
        bool bluff = plan.Bluff;
        changedAmount = 0;

        // Caja fuerte: Tito se lleva (o infla) solo una parte.
        float penalty = gm.Modifiers.TrickPenaltyMultiplier;
        if (!bluff && type == TrickType.StealStars)
        {
            int steal = Mathf.Max(b.StealMin, Mathf.CeilToInt(gm.RoundStars * b.StealFraction));
            changedAmount = gm.StealStars(Mathf.Max(1, Mathf.RoundToInt(steal * penalty)));
            if (changedAmount == 0)
            {
                // No había qué robar: infla la meta si ya puede; si no, esta vez no hace nada.
                if (gm.CurrentRound < b.GoalRigFromRound) return;
                type = TrickType.RigGoal;
            }
        }
        if (!bluff && type == TrickType.RigGoal)
        {
            int rig = Mathf.Max(b.GoalRigMin, Mathf.CeilToInt(gm.CurrentGoal * b.GoalRigFraction));
            changedAmount = Mathf.Max(1, Mathf.RoundToInt(rig * penalty));
            gm.RigGoal(changedAmount);
        }

        IsActive = true;
        IsBluff = bluff;
        ActiveType = type;
        WindowLeft = b.TrickWindow;
        TrickStarted?.Invoke(type, bluff, changedAmount);
    }

    private void Undo()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (ActiveType == TrickType.StealStars) gm.ReturnStars(changedAmount);
        else if (ActiveType == TrickType.RigGoal) gm.UnrigGoal(changedAmount);
        changedAmount = 0;
    }

    private void End(bool caught)
    {
        if (!IsActive) return;
        IsActive = false;
        TrickEnded?.Invoke(ActiveType, IsBluff, caught);
    }

    // Fin de ronda o pausa larga: se cierra sin avisar (lo hecho queda hecho).
    private void Cancel()
    {
        IsActive = false;
        WindowLeft = 0f;
    }

    private TrickType RandomType(int round, BalanceConfig b)
    {
        validTypes.Clear();
        validTypes.Add(TrickType.StealStars);
        if (round >= b.GoalRigFromRound) validTypes.Add(TrickType.RigGoal);
        if (round >= b.CrossedFromRound) validTypes.Add(TrickType.CrossedControls);
        return validTypes[UnityEngine.Random.Range(0, validTypes.Count)];
    }

    // Cruzar los controles con la regla Espejo los deja como estaban: se cambia de trampa.
    private static TrickType PickValidType(TrickType wanted, GameManager gm)
    {
        bool mirrorRule = gm.Rules != null && gm.Rules.AppliedRule != null && gm.Rules.AppliedRule.Type == RuleType.Mirror;
        if (wanted != TrickType.CrossedControls || !mirrorRule) return wanted;
        return gm.CurrentRound >= gm.Balance.GoalRigFromRound ? TrickType.RigGoal : TrickType.StealStars;
    }
}
