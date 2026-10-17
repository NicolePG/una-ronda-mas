using System.Text;
using UnityEngine;

/// <summary>
/// Premios acumulados en la partida y sus efectos. Lo crea GameManager.
/// Otoniel lee los multiplicadores desde GameManager.Instance.Modifiers.
/// </summary>
public class PlayerModifiers
{
    private readonly BalanceConfig balance;

    public PlayerModifiers(BalanceConfig balance)
    {
        this.balance = balance;
    }

    // Cantidad de cada premio
    public int MagnetCount { get; private set; }
    public int FloatCount { get; private set; }
    public int ShieldCount { get; private set; }
    public int SpringCount { get; private set; }
    public int SlowMotionCount { get; private set; }
    public int PoisonSpringCount { get; private set; }
    public int PoisonSlowMotionCount { get; private set; }
    public int PoisonShieldCount { get; private set; }

    /// <summary>Estrellas que se suman a la meta de la próxima ronda (corazón envenenado).</summary>
    public int PendingGoalPenalty { get; private set; }

    // Usos que quedan en la ronda actual
    public int ShieldsLeft { get; private set; }
    public int PoisonShieldsLeft { get; private set; }
    public int FloatsLeft { get; private set; }

    public int TotalPrizes { get; private set; }

    // Para el concursante
    public bool HasMagnet => MagnetCount > 0;
    public bool HasPoisonSpring => PoisonSpringCount > 0;

    public float JumpHeightMultiplier =>
        (1f + balance.SpringJumpHeightBonus * SpringCount) * (HasPoisonSpring ? balance.PoisonSpringJumpMultiplier : 1f);

    public float JumpDurationMultiplier =>
        (1f + balance.SpringJumpDurationBonus * SpringCount) * (HasPoisonSpring ? balance.PoisonSpringJumpMultiplier : 1f);

    public float LaneChangeDurationMultiplier =>
        Mathf.Pow(balance.PoisonSlowLaneChangeMultiplier, PoisonSlowMotionCount);

    // Para la pista
    public float TrackSpeedMultiplier => Mathf.Pow(balance.SlowMotionSpeedMultiplier, SlowMotionCount);

    public void Reset()
    {
        MagnetCount = FloatCount = ShieldCount = SpringCount = SlowMotionCount = 0;
        PoisonSpringCount = PoisonSlowMotionCount = PoisonShieldCount = 0;
        PendingGoalPenalty = 0;
        ShieldsLeft = PoisonShieldsLeft = FloatsLeft = 0;
        TotalPrizes = 0;
    }

    /// <summary>Suma un premio. La vida del corazón la suma GameManager.</summary>
    public void AddPrize(PrizeType type, bool poisoned)
    {
        TotalPrizes++;
        switch (type)
        {
            case PrizeType.ExtraHeart:
                if (poisoned) PendingGoalPenalty += balance.PoisonHeartGoalPenalty;
                break;
            case PrizeType.Magnet:
                MagnetCount++;
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
        }
    }

    /// <summary>Recarga escudos y flotadores al empezar cada ronda.</summary>
    public void BeginRound()
    {
        ShieldsLeft = ShieldCount;
        PoisonShieldsLeft = PoisonShieldCount;
        FloatsLeft = FloatCount;
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

    /// <summary>Resumen para el panel de depuración.</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        Append(sb, "Imán", MagnetCount);
        Append(sb, "Flotador", FloatCount);
        Append(sb, "Escudo", ShieldCount);
        Append(sb, "Resorte", SpringCount);
        Append(sb, "Cámara lenta", SlowMotionCount);
        Append(sb, "Resorte (env.)", PoisonSpringCount);
        Append(sb, "Cámara lenta (env.)", PoisonSlowMotionCount);
        Append(sb, "Escudo (env.)", PoisonShieldCount);
        if (PendingGoalPenalty > 0) sb.Append($"meta +{PendingGoalPenalty} próxima ronda");
        return sb.Length == 0 ? "ninguno" : sb.ToString().TrimEnd(',', ' ');
    }

    private static void Append(StringBuilder sb, string label, int count)
    {
        if (count > 0) sb.Append($"{label} x{count}, ");
    }
}
