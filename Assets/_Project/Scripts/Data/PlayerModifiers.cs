using System.Text;
using UnityEngine;

/// <summary>
/// Premios elegidos en la partida y sus efectos. Lo crea GameManager.
/// Cada premio se elige una vez (el corazón se puede repetir, pero no en dos premiaciones seguidas).
/// Otoniel lee los multiplicadores desde GameManager.Instance.Modifiers.
/// </summary>
public class PlayerModifiers
{
    private static readonly int TypeCount = System.Enum.GetValues(typeof(PrizeType)).Length;

    private readonly BalanceConfig balance;
    // Veces que se eligió cada tipo (normal o envenenado) y en qué premiación fue la última vez.
    private readonly int[] levels = new int[TypeCount];
    private readonly int[] lastPickScreen = new int[TypeCount];
    private int prizeScreen;

    public PlayerModifiers(BalanceConfig balance)
    {
        this.balance = balance;
        Reset();
    }

    // Cantidad de cada premio que tiene versión envenenada (cuentan por separado)
    public int MagnetCount => levels[(int)PrizeType.Magnet];
    public int FloatCount { get; private set; }
    public int ShieldCount { get; private set; }
    public int SpringCount { get; private set; }
    public int SlowMotionCount { get; private set; }
    public int PoisonSpringCount { get; private set; }
    public int PoisonSlowMotionCount { get; private set; }
    public int PoisonShieldCount { get; private set; }
    public int MagnifierCount => levels[(int)PrizeType.Magnifier];

    /// <summary>Estrellas que se suman a la meta de la próxima ronda (corazón envenenado).</summary>
    public int PendingGoalPenalty { get; private set; }

    // Usos que quedan en la ronda actual (o en la partida, la Segunda oportunidad)
    public int ShieldsLeft { get; private set; }
    public int PoisonShieldsLeft { get; private set; }
    public int FloatsLeft { get; private set; }
    public int WhistlesLeft { get; private set; }
    public int SecondChancesLeft { get; private set; }

    public int TotalPrizes { get; private set; }

    /// <summary>¿Ya tiene este premio? (normal o envenenado).</summary>
    public bool Has(PrizeType type) => levels[(int)type] > 0;

    /// <summary>Cuántas veces se eligió este tipo de premio en la partida.</summary>
    public int GetLevel(PrizeType type) => levels[(int)type];

    /// <summary>¿Se eligió en la premiación anterior? (para no repetir el corazón seguido).</summary>
    public bool WasPickedLastScreen(PrizeType type) =>
        levels[(int)type] > 0 && lastPickScreen[(int)type] == prizeScreen - 1;

    // Para el concursante
    public bool HasMagnet => Has(PrizeType.Magnet) || Has(PrizeType.SuperMagnet);
    public bool HasPoisonSpring => PoisonSpringCount > 0;

    public float JumpHeightMultiplier =>
        (1f + balance.SpringJumpHeightBonus * SpringCount) * (HasPoisonSpring ? balance.PoisonSpringJumpMultiplier : 1f);

    public float JumpDurationMultiplier =>
        (1f + balance.SpringJumpDurationBonus * SpringCount) * (HasPoisonSpring ? balance.PoisonSpringJumpMultiplier : 1f);

    public float LaneChangeDurationMultiplier =>
        Mathf.Pow(balance.PoisonSlowLaneChangeMultiplier, PoisonSlowMotionCount)
        * (Has(PrizeType.Sneakers) ? balance.SneakersLaneChangeMultiplier : 1f);

    /// <summary>Imán: metros hacia adelante desde los que atrae (más con el Súper imán).</summary>
    public float MagnetRange => Has(PrizeType.SuperMagnet) ? balance.SuperMagnetRange : balance.MagnetRange;

    /// <summary>Imán: alcance hacia los costados (el Súper imán llega a los 3 carriles).</summary>
    public float MagnetLateralReach => Has(PrizeType.SuperMagnet) ? balance.SuperMagnetLateralReach : balance.MagnetLateralReach;

    /// <summary>Trébol: puntos extra de cada estrella dorada.</summary>
    public int GoldenStarBonus => Has(PrizeType.Clover) ? balance.CloverGoldenBonus : 0;

    // Para la pista y la partida
    public float TrackSpeedMultiplier => Mathf.Pow(balance.SlowMotionSpeedMultiplier, SlowMotionCount);

    /// <summary>Propina: estrellas con las que empieza cada ronda.</summary>
    public int RoundStartStars => Has(PrizeType.Tip) ? balance.TipStars : 0;

    /// <summary>Contrato amañado: multiplica la meta de cada ronda.</summary>
    public float GoalMultiplier => Has(PrizeType.RiggedContract) ? balance.RiggedContractGoalMultiplier : 1f;

    /// <summary>Caja fuerte: parte de lo que Tito roba o infla.</summary>
    public float TrickPenaltyMultiplier => Has(PrizeType.Safe) ? balance.SafeTrickMultiplier : 1f;

    /// <summary>Placa de detective: multiplica las estrellas por pillar a Tito.</summary>
    public float CheatRewardMultiplier => Has(PrizeType.DetectiveBadge) ? balance.DetectiveBadgeMultiplier : 1f;

    /// <summary>Seguro de apuesta: si falla la meta de la apuesta pero llega a la normal, igual gana.</summary>
    public bool HasBetInsurance => Has(PrizeType.BetInsurance);

    public void Reset()
    {
        FloatCount = ShieldCount = SpringCount = SlowMotionCount = 0;
        PoisonSpringCount = PoisonSlowMotionCount = PoisonShieldCount = 0;
        PendingGoalPenalty = 0;
        ShieldsLeft = PoisonShieldsLeft = FloatsLeft = WhistlesLeft = SecondChancesLeft = 0;
        TotalPrizes = 0;
        prizeScreen = 0;
        System.Array.Clear(levels, 0, levels.Length);
        System.Array.Clear(lastPickScreen, 0, lastPickScreen.Length);
    }

    /// <summary>Empieza una premiación (GameManager la llama antes de armar las tarjetas).</summary>
    public void BeginPrizeScreen() => prizeScreen++;

    /// <summary>Suma un premio. La vida del corazón la suma GameManager.</summary>
    public void AddPrize(PrizeType type, bool poisoned)
    {
        TotalPrizes++;
        levels[(int)type]++;
        lastPickScreen[(int)type] = prizeScreen;
        switch (type)
        {
            case PrizeType.ExtraHeart:
                if (poisoned) PendingGoalPenalty += balance.PoisonHeartGoalPenalty;
                break;
            case PrizeType.Float:
                FloatCount++;
                break;
            case PrizeType.Shield:
                if (poisoned) PoisonShieldCount++; else ShieldCount++;
                break;
            case PrizeType.Spring:
                if (poisoned) PoisonSpringCount++; else SpringCount++;
                break;
            case PrizeType.SlowMotion:
                if (poisoned) PoisonSlowMotionCount++; else SlowMotionCount++;
                break;
            case PrizeType.SecondChance:
                SecondChancesLeft++;
                break;
        }
    }

    /// <summary>Recarga escudos, flotadores y silbato al empezar cada ronda.</summary>
    public void BeginRound()
    {
        ShieldsLeft = ShieldCount;
        PoisonShieldsLeft = PoisonShieldCount;
        FloatsLeft = FloatCount;
        WhistlesLeft = Has(PrizeType.Whistle) ? 1 : 0;
    }

    /// <summary>Devuelve la penalización de meta pendiente y la borra.</summary>
    public int ConsumeGoalPenalty()
    {
        int penalty = PendingGoalPenalty;
        PendingGoalPenalty = 0;
        return penalty;
    }

    /// <summary>True si un premio absorbió el golpe (no se pierde vida).</summary>
    public bool TryAbsorbHit(HitCause cause, float roundElapsed)
    {
        if (cause == HitCause.Water)
        {
            if (FloatsLeft <= 0) return false;
            FloatsLeft--;
            return true;
        }

        if (ShieldsLeft > 0)
        {
            ShieldsLeft--;
            return true;
        }
        if (PoisonShieldsLeft > 0 && roundElapsed < balance.PoisonShieldDuration)
        {
            PoisonShieldsLeft--;
            return true;
        }
        return false;
    }

    /// <summary>Silbato: la primera falsa alarma de la ronda no quita vida.</summary>
    public bool TryUseWhistle()
    {
        if (WhistlesLeft <= 0) return false;
        WhistlesLeft--;
        return true;
    }

    /// <summary>Segunda oportunidad: una vez por partida, al perder la última vida vuelve con 1.</summary>
    public bool TryUseSecondChance()
    {
        if (SecondChancesLeft <= 0) return false;
        SecondChancesLeft--;
        return true;
    }

    /// <summary>Resumen para el panel de depuración.</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < TypeCount; i++)
        {
            if (levels[i] > 0) sb.Append(levels[i] > 1 ? $"{(PrizeType)i} x{levels[i]}, " : $"{(PrizeType)i}, ");
        }
        if (PendingGoalPenalty > 0) sb.Append($"meta +{PendingGoalPenalty} próxima ronda");
        return sb.Length == 0 ? "ninguno" : sb.ToString().TrimEnd(',', ' ');
    }
}
