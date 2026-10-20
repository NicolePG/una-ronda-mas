using System;
using UnityEngine;

/// <summary>
/// Racha de estrellas: cada estrella seguida sin chocar ni caer al agua suma a la racha; cada
/// BalanceConfig.StreakMilestone estrellas da estrellas de premio (con GameManager.AddBonusStars).
/// Chocar o caer corta la racha. Los efectos (textos, público, sonido) escuchan sus eventos.
/// </summary>
[DisallowMultipleComponent]
public class StarStreak : MonoBehaviour
{
    /// <summary>Cambió la racha (0 = se cortó).</summary>
    public static event Action<int> StreakChanged;

    /// <summary>Llegó a un hito: racha actual y estrellas de premio que ganó.</summary>
    public static event Action<int, int> MilestoneReached;

    [Tooltip("Concursante (si se deja vacío, se usa el de la escena).")]
    [SerializeField] private ContestantController contestant;

    public int Current { get; private set; }

    private void OnEnable()
    {
        if (contestant == null) contestant = ContestantController.Current;
        if (contestant != null) contestant.StarCollected += HandleStar;
        GameEvents.ContestantHit += HandleHit;
        GameEvents.RoundStarted += HandleRoundStarted;
    }

    private void OnDisable()
    {
        if (contestant != null) contestant.StarCollected -= HandleStar;
        GameEvents.ContestantHit -= HandleHit;
        GameEvents.RoundStarted -= HandleRoundStarted;
    }

    private void Start()
    {
        // Por si el concursante se activó después que este componente.
        if (contestant == null && ContestantController.Current != null)
        {
            contestant = ContestantController.Current;
            contestant.StarCollected += HandleStar;
        }
    }

    private void HandleStar(Star star)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != RoundPhase.Running) return;

        Current++;
        StreakChanged?.Invoke(Current);

        BalanceConfig b = gm.Balance;
        if (Current % b.StreakMilestone != 0) return;
        int bonus = Mathf.Min(b.StreakMaxBonus, b.StreakBonusPerMilestone * (Current / b.StreakMilestone));
        if (bonus > 0) gm.AddBonusStars(bonus);
        MilestoneReached?.Invoke(Current, bonus);
    }

    private void HandleHit(HitCause cause) => ResetStreak();

    private void HandleRoundStarted(int round, int goal, float speed) => ResetStreak();

    private void ResetStreak()
    {
        if (Current == 0) return;
        Current = 0;
        StreakChanged?.Invoke(0);
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(ContestantController contestant) => this.contestant = contestant;
#endif
}
