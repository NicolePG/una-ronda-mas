using System;
using UnityEngine;

/// <summary>Pesos de dificultad de tramos a partir de una ronda.</summary>
[Serializable]
public struct DifficultyStage
{
    [Tooltip("Ronda desde la que se usan estos pesos.")]
    [SerializeField, Min(1)] private int fromRound;
    [SerializeField, Min(0f)] private float easy;
    [SerializeField, Min(0f)] private float mid;
    [SerializeField, Min(0f)] private float hard;

    public DifficultyStage(int fromRound, float easy, float mid, float hard)
    {
        this.fromRound = fromRound;
        this.easy = easy;
        this.mid = mid;
        this.hard = hard;
    }

    public int FromRound => fromRound;

    public float GetWeight(SegmentDifficulty difficulty) => difficulty switch
    {
        SegmentDifficulty.Easy => easy,
        SegmentDifficulty.Mid => mid,
        _ => hard,
    };
}

/// <summary>
/// Todos los números de diseño del juego. Se edita en Data/Balance.asset.
/// Los valores por defecto salen de docs/GDD.md.
/// </summary>
[CreateAssetMenu(fileName = "Balance", menuName = "Una Ronda Más/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    [Header("Ronda")]
    [Tooltip("Duración de cada ronda en segundos. El temporizador manda.")]
    [SerializeField, Min(1f)] private float roundDuration = 40f;
    [Tooltip("Segundos del anuncio de Tito cuando NO hay apuesta; después la ronda empieza sola. 0 = esperar a la UI.")]
    [SerializeField, Min(0f)] private float announcementDuration = 3f;
    [Tooltip("Espera tras elegir el último premio antes del siguiente anuncio.")]
    [SerializeField, Min(0f)] private float afterPrizeDelay = 1f;
    [Tooltip("Espera extra si se eligió un premio envenenado, para mostrar la sorpresa.")]
    [SerializeField, Min(0f)] private float poisonRevealDelay = 2f;

    [Header("Meta de estrellas")]
    [SerializeField, Min(1)] private int baseGoal = 10;
    [Tooltip("Cuánto sube la meta en cada ronda.")]
    [SerializeField, Min(0)] private int goalPerRound = 5;

    [Header("Velocidad de la pista (m/s)")]
    [SerializeField, Min(0.1f)] private float baseSpeed = 8f;
    [Tooltip("Multiplicador por ronda: 1,08 = 8% más rápido cada ronda.")]
    [SerializeField, Min(1f)] private float speedGrowthPerRound = 1.08f;
    [Tooltip("Velocidad máxima (se alcanza en la ronda 10).")]
    [SerializeField, Min(0.1f)] private float maxSpeed = 16f;

    [Header("Vidas")]
    [SerializeField, Min(1)] private int startLives = 3;
    [SerializeField, Min(1)] private int maxLives = 5;

    [Header("Concursante (Otoniel)")]
    [Tooltip("Distancia entre carriles: x = −2, 0, 2.")]
    [SerializeField, Min(0.5f)] private float laneWidth = 2f;
    [Tooltip("Segundos que tarda el cambio de carril.")]
    [SerializeField, Min(0.01f)] private float laneChangeDuration = 0.15f;
    [Tooltip("Altura máxima del salto (m).")]
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.2f;
    [Tooltip("Segundos en el aire por salto.")]
    [SerializeField, Min(0.1f)] private float jumpDuration = 0.6f;
    [Tooltip("Segundos que dura el deslizamiento.")]
    [SerializeField, Min(0.1f)] private float slideDuration = 0.6f;
    [Tooltip("Altura de la cápsula mientras se desliza (m).")]
    [SerializeField, Min(0.1f)] private float slideHeight = 0.7f;
    [Tooltip("Segundos sin recibir daño después de un golpe.")]
    [SerializeField, Min(0f)] private float invulnerabilityTime = 1f;

    [Header("Estrellas")]
    [SerializeField, Min(0)] private int normalStarValue = 1;
    [SerializeField, Min(0)] private int goldenStarValue = 3;
    [Tooltip("Imán: distancia hacia adelante (m) desde la que atrae estrellas.")]
    [SerializeField, Min(0f)] private float magnetRange = 12f;
    [Tooltip("Imán: distancia lateral máxima (m). 2,5 alcanza los carriles vecinos.")]
    [SerializeField, Min(0f)] private float magnetLateralReach = 2.5f;

    [Header("Reglas de Tito")]
    [SerializeField, Min(1)] private int firstRuleRound = 3;
    [SerializeField, Min(1)] private int firstLieRound = 4;
    [Tooltip("Probabilidad de que Tito mienta desde firstLieRound.")]
    [SerializeField, Range(0f, 1f)] private float lieChance = 0.3f;
    [Tooltip("Todo vale doble: multiplicador del valor de las estrellas.")]
    [SerializeField, Min(1)] private int doubleValueStarMultiplier = 2;
    [Tooltip("Todo vale doble: multiplicador de la velocidad de la pista.")]
    [SerializeField, Min(0.1f)] private float doubleValueSpeedMultiplier = 1.3f;
    [Tooltip("Cuando Tito miente: probabilidad de que cada HologramSlot de un tramo tenga holograma.")]
    [SerializeField, Range(0f, 1f)] private float hologramChancePerSlot = 0.5f;

    [Header("Botón ¡Trampa!")]
    [Tooltip("Estrellas que gana si acierta.")]
    [SerializeField, Min(0)] private int cheatRewardStars = 5;
    [Tooltip("Vidas que pierde si se equivoca.")]
    [SerializeField, Min(0)] private int cheatPenaltyLives = 1;

    [Header("Apuesta")]
    [SerializeField, Min(1)] private int firstBetRound = 3;
    [Tooltip("La meta se multiplica por esto si acepta (se redondea hacia arriba).")]
    [SerializeField, Min(1f)] private float betGoalMultiplier = 1.5f;

    [Header("Premios")]
    [Tooltip("Cuántas tarjetas se muestran.")]
    [SerializeField, Min(1)] private int prizeChoices = 3;
    [SerializeField, Min(0)] private int prizesPerWin = 1;
    [SerializeField, Min(0)] private int prizesPerBetWin = 2;
    [SerializeField, Min(1)] private int firstPoisonRound = 4;
    [Tooltip("Probabilidad de que una de las tarjetas sea un premio envenenado.")]
    [SerializeField, Range(0f, 1f)] private float poisonChance = 0.5f;
    [Tooltip("Resorte: altura de salto extra por cada resorte (0,4 = +40%).")]
    [SerializeField, Min(0f)] private float springJumpHeightBonus = 0.4f;
    [Tooltip("Resorte: duración de salto extra por cada resorte (0,25 = +25%).")]
    [SerializeField, Min(0f)] private float springJumpDurationBonus = 0.25f;
    [Tooltip("Cámara lenta: multiplicador de la velocidad de la pista por cada una.")]
    [SerializeField, Range(0.1f, 1f)] private float slowMotionSpeedMultiplier = 0.9f;
    [Tooltip("Corazón envenenado: estrellas que sube la meta de la siguiente ronda.")]
    [SerializeField, Min(0)] private int poisonHeartGoalPenalty = 5;
    [Tooltip("Resorte envenenado: multiplica altura y duración de cada salto.")]
    [SerializeField, Min(1f)] private float poisonSpringJumpMultiplier = 2f;
    [Tooltip("Cámara lenta envenenada: multiplica lo que tarda el cambio de carril.")]
    [SerializeField, Min(1f)] private float poisonSlowLaneChangeMultiplier = 2f;
    [Tooltip("Escudo envenenado: solo protege durante estos primeros segundos de la ronda.")]
    [SerializeField, Min(0f)] private float poisonShieldDuration = 10f;

    [Header("Pista")]
    [SerializeField, Min(1f)] private float segmentLength = 20f;
    [Tooltip("Hasta qué distancia adelante (m) se crean tramos.")]
    [SerializeField, Min(1f)] private float spawnAheadDistance = 100f;
    [Tooltip("Un tramo se borra cuando su final queda detrás de esta z.")]
    [SerializeField] private float destroyBehindZ = -20f;
    [Tooltip("Tramos vacíos al empezar cada ronda.")]
    [SerializeField, Min(0)] private int safeStartSegments = 1;
    [Tooltip("Metros al inicio y al final de cada tramo que deben quedar sin obstáculos (lo revisa «Validar tramos»).")]
    [SerializeField, Min(0f)] private float segmentEdgeMargin = 3f;
    [Tooltip("Ronda desde la que aparecen tramos con barra alta.")]
    [SerializeField, Min(1)] private int highBarFromRound = 2;
    [Tooltip("Ronda desde la que aparecen tramos con martillo.")]
    [SerializeField, Min(1)] private int hammerFromRound = 3;
    [Tooltip("Ronda desde la que aparecen tramos con hueco al agua.")]
    [SerializeField, Min(1)] private int waterGapFromRound = 4;
    [Tooltip("Qué parte de las estrellas de una ronda debería hacer falta juntar para llegar a la meta (lo usa «Informe de balance»).")]
    [SerializeField, Range(0.1f, 1f)] private float targetCollectRatio = 0.5f;
    [Tooltip("Pesos de dificultad según la ronda. Se usa la última fila cuya ronda ya llegó.")]
    [SerializeField] private DifficultyStage[] difficultyStages =
    {
        new DifficultyStage(1, 1f, 0f, 0f),
        new DifficultyStage(5, 0.3f, 0.7f, 0f),
        new DifficultyStage(6, 0.1f, 0.9f, 0f),
        new DifficultyStage(7, 0f, 0.5f, 0.5f),
        new DifficultyStage(8, 0f, 0.2f, 0.8f),
    };

    // Ronda
    public float RoundDuration => roundDuration;
    public float AnnouncementDuration => announcementDuration;
    public float AfterPrizeDelay => afterPrizeDelay;
    public float PoisonRevealDelay => poisonRevealDelay;

    // Vidas
    public int StartLives => startLives;
    public int MaxLives => maxLives;

    // Concursante
    public float LaneWidth => laneWidth;
    public float LaneChangeDuration => laneChangeDuration;
    public float JumpHeight => jumpHeight;
    public float JumpDuration => jumpDuration;
    public float SlideDuration => slideDuration;
    public float SlideHeight => slideHeight;
    public float InvulnerabilityTime => invulnerabilityTime;

    // Estrellas
    public int NormalStarValue => normalStarValue;
    public int GoldenStarValue => goldenStarValue;
    public float MagnetRange => magnetRange;
    public float MagnetLateralReach => magnetLateralReach;

    // Reglas
    public int FirstRuleRound => firstRuleRound;
    public int FirstLieRound => firstLieRound;
    public float LieChance => lieChance;
    public int DoubleValueStarMultiplier => doubleValueStarMultiplier;
    public float DoubleValueSpeedMultiplier => doubleValueSpeedMultiplier;
    public float HologramChancePerSlot => hologramChancePerSlot;
    public int CheatRewardStars => cheatRewardStars;
    public int CheatPenaltyLives => cheatPenaltyLives;

    // Apuesta
    public int FirstBetRound => firstBetRound;
    public float BetGoalMultiplier => betGoalMultiplier;

    // Premios
    public int PrizeChoices => prizeChoices;
    public int PrizesPerWin => prizesPerWin;
    public int PrizesPerBetWin => prizesPerBetWin;
    public int FirstPoisonRound => firstPoisonRound;
    public float PoisonChance => poisonChance;
    public float SpringJumpHeightBonus => springJumpHeightBonus;
    public float SpringJumpDurationBonus => springJumpDurationBonus;
    public float SlowMotionSpeedMultiplier => slowMotionSpeedMultiplier;
    public int PoisonHeartGoalPenalty => poisonHeartGoalPenalty;
    public float PoisonSpringJumpMultiplier => poisonSpringJumpMultiplier;
    public float PoisonSlowLaneChangeMultiplier => poisonSlowLaneChangeMultiplier;
    public float PoisonShieldDuration => poisonShieldDuration;

    // Pista
    public float SegmentLength => segmentLength;
    public float SpawnAheadDistance => spawnAheadDistance;
    public float DestroyBehindZ => destroyBehindZ;
    public int SafeStartSegments => safeStartSegments;
    public float SegmentEdgeMargin => segmentEdgeMargin;
    public int HighBarFromRound => highBarFromRound;
    public int HammerFromRound => hammerFromRound;
    public int WaterGapFromRound => waterGapFromRound;
    public float TargetCollectRatio => targetCollectRatio;

    /// <summary>Meta de estrellas de la ronda (sin apuesta ni penalización).</summary>
    public int GetGoal(int round) => baseGoal + goalPerRound * Mathf.Max(0, round - 1);

    /// <summary>Meta con la apuesta aceptada.</summary>
    public int GetBetGoal(int goal) => Mathf.CeilToInt(goal * betGoalMultiplier);

    /// <summary>Velocidad de la pista de la ronda, sin reglas ni premios.</summary>
    public float GetBaseSpeed(int round) =>
        Mathf.Min(baseSpeed * Mathf.Pow(speedGrowthPerRound, Mathf.Max(0, round - 1)), maxSpeed);

    /// <summary>Pesos de dificultad que tocan en esta ronda.</summary>
    public DifficultyStage GetDifficultyStage(int round)
    {
        var result = new DifficultyStage(1, 1f, 0f, 0f);
        int best = int.MinValue;
        if (difficultyStages == null) return result;
        foreach (DifficultyStage stage in difficultyStages)
        {
            if (stage.FromRound <= round && stage.FromRound >= best)
            {
                best = stage.FromRound;
                result = stage;
            }
        }
        return result;
    }

    private void OnValidate()
    {
        maxLives = Mathf.Max(maxLives, startLives);
        maxSpeed = Mathf.Max(maxSpeed, baseSpeed);
    }
}
