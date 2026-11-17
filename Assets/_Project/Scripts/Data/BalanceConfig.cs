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
    /// <summary>Versión de los valores por defecto. «Crear todo» pone el asset al día si es más viejo.</summary>
    public const int CurrentDataVersion = 3;

    [Tooltip("Versión de los datos (la usa «Crear todo» para actualizar el balance).")]
    [SerializeField, HideInInspector] private int dataVersion;

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
    [Tooltip("Meta de la ronda 1. Con 34 hay que juntar ~1 de cada 4 estrellas; en la ronda 8 ya hacen falta ~6 de cada 10.")]
    [SerializeField, Min(1)] private int baseGoal = 34;
    [Tooltip("Cuánto sube la meta en cada ronda.")]
    [SerializeField, Min(0)] private int goalPerRound = 12;

    [Header("Velocidad de la pista (m/s)")]
    [SerializeField, Min(0.1f)] private float baseSpeed = 8.5f;
    [Tooltip("Multiplicador por ronda: 1,075 = 7,5% más rápido cada ronda.")]
    [SerializeField, Min(1f)] private float speedGrowthPerRound = 1.075f;
    [Tooltip("Velocidad máxima (se alcanza en la ronda 11).")]
    [SerializeField, Min(0.1f)] private float maxSpeed = 17f;

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
    [Tooltip("Súper imán: distancia hacia adelante (m).")]
    [SerializeField, Min(0f)] private float superMagnetRange = 20f;
    [Tooltip("Súper imán: distancia lateral (m). 4,5 alcanza los 3 carriles.")]
    [SerializeField, Min(0f)] private float superMagnetLateralReach = 4.5f;
    [Tooltip("Trébol: puntos extra de cada estrella dorada.")]
    [SerializeField, Min(0)] private int cloverGoldenBonus = 3;

    [Header("Reglas de Tito")]
    [SerializeField, Min(1)] private int firstRuleRound = 2;
    [SerializeField, Min(1)] private int firstLieRound = 3;
    [Tooltip("Probabilidad de que Tito mienta en firstLieRound.")]
    [SerializeField, Range(0f, 1f)] private float lieChance = 0.35f;
    [Tooltip("Cuánto sube la probabilidad de mentira en cada ronda.")]
    [SerializeField, Range(0f, 1f)] private float lieChancePerRound = 0.05f;
    [Tooltip("Probabilidad máxima de mentira.")]
    [SerializeField, Range(0f, 1f)] private float maxLieChance = 0.7f;
    [Tooltip("Todo vale doble: multiplicador del valor de las estrellas.")]
    [SerializeField, Min(1)] private int doubleValueStarMultiplier = 2;
    [Tooltip("Todo vale doble: multiplicador de la velocidad de la pista.")]
    [SerializeField, Min(0.1f)] private float doubleValueSpeedMultiplier = 1.3f;
    [Tooltip("Cuando Tito miente: probabilidad de que cada HologramSlot de un tramo tenga holograma.")]
    [SerializeField, Range(0f, 1f)] private float hologramChancePerSlot = 0.5f;

    [Header("Botón ¡Trampa!")]
    [Tooltip("Estrellas base que gana si acierta.")]
    [SerializeField, Min(0)] private int cheatRewardBase = 4;
    [Tooltip("Estrellas extra por número de ronda (ronda 5 = 4 + 5 × 2 = 14).")]
    [SerializeField, Min(0)] private int cheatRewardPerRound = 2;
    [Tooltip("Racha de detective: +50% de premio por cada acierto seguido sin falsa alarma.")]
    [SerializeField, Min(0f)] private float detectiveStreakBonus = 0.5f;
    [Tooltip("Tope del multiplicador de la racha de detective.")]
    [SerializeField, Min(1f)] private float detectiveMaxMultiplier = 3f;
    [Tooltip("Vidas que pierde si se equivoca (falsa alarma).")]
    [SerializeField, Min(0)] private int cheatPenaltyLives = 1;
    [Tooltip("Segundos sin poder acusar después de una falsa alarma.")]
    [SerializeField, Min(0f)] private float falseAlarmCooldown = 2.5f;

    [Header("Trampas en vivo de Tito")]
    [Tooltip("Desde qué ronda Tito hace trampas a mitad de la carrera.")]
    [SerializeField, Min(1)] private int firstTrickRound = 2;
    [Tooltip("Cada cuántas rondas suma una trampa más por ronda.")]
    [SerializeField, Min(1)] private int roundsPerExtraTrick = 2;
    [SerializeField, Min(1)] private int maxTricksPerRound = 3;
    [Tooltip("Segundos que tiene el jugador para acusar una trampa antes de que quede hecha.")]
    [SerializeField, Min(1f)] private float trickWindow = 6f;
    [Tooltip("Las trampas pasan entre estos segundos de la ronda.")]
    [SerializeField, Min(0f)] private float trickTimeStart = 5f;
    [SerializeField, Min(0f)] private float trickTimeEnd = 33f;
    [Tooltip("Robo de estrellas: parte de las estrellas de la ronda que se lleva.")]
    [SerializeField, Range(0f, 1f)] private float stealFraction = 0.2f;
    [Tooltip("Robo de estrellas: mínimo que se lleva.")]
    [SerializeField, Min(1)] private int stealMin = 3;
    [Tooltip("Desde qué ronda puede inflar la meta.")]
    [SerializeField, Min(1)] private int goalRigFromRound = 3;
    [Tooltip("Meta inflada: cuánto sube la meta (0,12 = +12%).")]
    [SerializeField, Range(0f, 1f)] private float goalRigFraction = 0.12f;
    [Tooltip("Meta inflada: mínimo de estrellas que sube.")]
    [SerializeField, Min(1)] private int goalRigMin = 4;
    [Tooltip("Desde qué ronda puede cruzar los controles (izquierda y derecha al revés).")]
    [SerializeField, Min(1)] private int crossedFromRound = 5;
    [Tooltip("Desde qué ronda Tito hace faroles: parece que hace trampa pero no cambia nada.")]
    [SerializeField, Min(1)] private int bluffFromRound = 4;
    [Tooltip("Probabilidad de que una trampa sea un farol en bluffFromRound.")]
    [SerializeField, Range(0f, 1f)] private float bluffChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float bluffChancePerRound = 0.04f;
    [SerializeField, Range(0f, 1f)] private float maxBluffChance = 0.45f;

    [Header("Racha de estrellas (Otoniel)")]
    [Tooltip("Cada cuántas estrellas seguidas sin chocar hay premio.")]
    [SerializeField, Min(2)] private int streakMilestone = 5;
    [Tooltip("Estrellas de premio por hito: ×1 en el primero, ×2 en el segundo… (no se multiplican por reglas).")]
    [SerializeField, Min(0)] private int streakBonusPerMilestone = 1;
    [Tooltip("Tope de estrellas de premio por hito.")]
    [SerializeField, Min(0)] private int streakMaxBonus = 4;

    [Header("Emoción (Otoniel)")]
    [Tooltip("Segundos antes de pasar un obstáculo en los que esquivarlo cuenta como «¡Uff!».")]
    [SerializeField, Min(0f)] private float nearMissWindow = 0.4f;
    [Tooltip("Segundos (reales) que se congela el juego al chocar.")]
    [SerializeField, Min(0f)] private float hitStopDuration = 0.07f;
    [Tooltip("Velocidad del tiempo durante el congelado al chocar.")]
    [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0.05f;
    [Tooltip("Últimos segundos de la ronda en cámara lenta si ya llegó a la meta.")]
    [SerializeField, Min(0f)] private float finishSlowMoWindow = 0.8f;
    [Tooltip("Velocidad del tiempo en la cámara lenta del final.")]
    [SerializeField, Range(0.05f, 1f)] private float finishSlowMoTimeScale = 0.35f;
    [Tooltip("Desde cuántos segundos para el final se ve el borde rojo de peligro.")]
    [SerializeField, Min(0f)] private float dangerSeconds = 10f;
    [Tooltip("Desde cuántos segundos se muestra la cuenta regresiva grande.")]
    [SerializeField, Min(0)] private int countdownSeconds = 5;

    [Header("Obstáculos que se mueven (Otoniel)")]
    [Tooltip("Velocidad extra de la pelota rodante sobre la pista (m/s).")]
    [SerializeField, Min(0f)] private float rollingBallSpeed = 7f;
    [Tooltip("En qué parte de su recorrido (0 a 1) la pelota pasa junto al concursante; define cuándo arranca.")]
    [SerializeField, Range(0.1f, 1f)] private float rollingBallPassPoint = 0.7f;
    [Tooltip("Grados por segundo que gira el rodillo barredor.")]
    [SerializeField, Min(0f)] private float sweeperDegreesPerSecond = 90f;
    [Tooltip("Ronda desde la que aparecen tramos con pelota rodante.")]
    [SerializeField, Min(1)] private int rollingBallFromRound = 4;
    [Tooltip("Ronda desde la que aparecen tramos con rodillo barredor.")]
    [SerializeField, Min(1)] private int sweeperFromRound = 6;

    [Header("Público (Otoniel)")]
    [Tooltip("Qué parte de las gradas está llena en la ronda 1 (0 a 1).")]
    [SerializeField, Range(0f, 1f)] private float crowdStartShare = 0.35f;
    [Tooltip("Ronda en la que el estudio se llena del todo.")]
    [SerializeField, Min(1)] private int crowdFullRound = 8;

    [Header("Eventos sorpresa (Otoniel)")]
    [Tooltip("Desde qué ronda Tito puede lanzar un evento a mitad de ronda.")]
    [SerializeField, Min(1)] private int surpriseFromRound = 2;
    [Tooltip("Probabilidad de que haya un evento en una ronda (0 a 1).")]
    [SerializeField, Range(0f, 1f)] private float surpriseChance = 0.65f;
    [Tooltip("El evento empieza entre estos segundos de la ronda (y termina antes del final).")]
    [SerializeField, Min(0f)] private float surpriseWindowStart = 10f;
    [SerializeField, Min(0f)] private float surpriseWindowEnd = 24f;
    [Tooltip("Segundos de lluvia de estrellas.")]
    [SerializeField, Min(0.5f)] private float starRainDuration = 5f;
    [Tooltip("Segundos entre estrellas de la lluvia.")]
    [SerializeField, Min(0.05f)] private float starRainInterval = 0.3f;
    [Tooltip("Probabilidad de que una estrella de la lluvia sea dorada.")]
    [SerializeField, Range(0f, 1f)] private float starRainGoldenChance = 0.15f;
    [Tooltip("Segundos de estrellas dobles.")]
    [SerializeField, Min(0.5f)] private float doubleStarsDuration = 6f;
    [Tooltip("Segundos del apagón sorpresa.")]
    [SerializeField, Min(0.5f)] private float blackoutEventDuration = 3f;

    [Header("Apuesta")]
    [SerializeField, Min(1)] private int firstBetRound = 3;
    [Tooltip("La meta se multiplica por esto si acepta (se redondea hacia arriba).")]
    [SerializeField, Min(1f)] private float betGoalMultiplier = 1.5f;

    [Header("Premios")]
    [Tooltip("Cuántas tarjetas se muestran.")]
    [SerializeField, Min(1)] private int prizeChoices = 3;
    [Tooltip("Premios que se eligen al ganar (los premios se quedan toda la partida). La apuesta ganada no da más: da tarjetas raras o épicas.")]
    [SerializeField, Min(0)] private int prizesPerWin = 1;
    [SerializeField, Min(1)] private int firstPoisonRound = 4;
    [Tooltip("Probabilidad de que una de las tarjetas sea un premio envenenado.")]
    [SerializeField, Range(0f, 1f)] private float poisonChance = 0.5f;
    [Tooltip("Ronda desde la que pueden salir premios raros.")]
    [SerializeField, Min(1)] private int rareFromRound = 2;
    [Tooltip("Probabilidad de que cada tarjeta sea rara en rareFromRound.")]
    [SerializeField, Range(0f, 1f)] private float rareChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float rareChancePerRound = 0.06f;
    [SerializeField, Range(0f, 1f)] private float maxRareChance = 0.6f;
    [Tooltip("Ronda desde la que pueden salir premios épicos.")]
    [SerializeField, Min(1)] private int epicFromRound = 5;
    [Tooltip("Probabilidad de que cada tarjeta sea épica en epicFromRound.")]
    [SerializeField, Range(0f, 1f)] private float epicChance = 0.12f;
    [SerializeField, Range(0f, 1f)] private float epicChancePerRound = 0.04f;
    [SerializeField, Range(0f, 1f)] private float maxEpicChance = 0.35f;
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
    [Tooltip("Propina: estrellas con las que empieza cada ronda.")]
    [SerializeField, Min(0)] private int tipStars = 5;
    [Tooltip("Zapatillas: multiplica lo que tarda el cambio de carril (0,7 = 30% más rápido).")]
    [SerializeField, Range(0.1f, 1f)] private float sneakersLaneChangeMultiplier = 0.7f;
    [Tooltip("Caja fuerte: parte de lo que Tito roba o infla (0,5 = la mitad).")]
    [SerializeField, Range(0f, 1f)] private float safeTrickMultiplier = 0.5f;
    [Tooltip("Placa de detective: multiplica las estrellas por pillar a Tito.")]
    [SerializeField, Min(1f)] private float detectiveBadgeMultiplier = 2f;
    [Tooltip("Contrato amañado: multiplica la meta de cada ronda (0,9 = 10% menos).")]
    [SerializeField, Range(0.1f, 1f)] private float riggedContractGoalMultiplier = 0.9f;

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
    [SerializeField, Min(1)] private int waterGapFromRound = 3;
    [Tooltip("Qué parte de las estrellas de una ronda debería hacer falta juntar para llegar a la meta (lo usa «Informe de balance»).")]
    [SerializeField, Range(0.1f, 1f)] private float targetCollectRatio = 0.5f;
    [Tooltip("Pesos de dificultad según la ronda. Se usa la última fila cuya ronda ya llegó.")]
    [SerializeField] private DifficultyStage[] difficultyStages =
    {
        new DifficultyStage(1, 1f, 0f, 0f),
        new DifficultyStage(2, 0.6f, 0.4f, 0f),
        new DifficultyStage(3, 0.35f, 0.55f, 0.1f),
        new DifficultyStage(4, 0.15f, 0.6f, 0.25f),
        new DifficultyStage(5, 0f, 0.55f, 0.45f),
        new DifficultyStage(7, 0f, 0.3f, 0.7f),
        new DifficultyStage(9, 0f, 0.1f, 0.9f),
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
    public float SuperMagnetRange => superMagnetRange;
    public float SuperMagnetLateralReach => superMagnetLateralReach;
    public int CloverGoldenBonus => cloverGoldenBonus;

    // Reglas
    public int FirstRuleRound => firstRuleRound;
    public int FirstLieRound => firstLieRound;
    public int DoubleValueStarMultiplier => doubleValueStarMultiplier;
    public float DoubleValueSpeedMultiplier => doubleValueSpeedMultiplier;
    public float HologramChancePerSlot => hologramChancePerSlot;

    // ¡Trampa!
    public float DetectiveStreakBonus => detectiveStreakBonus;
    public float DetectiveMaxMultiplier => detectiveMaxMultiplier;
    public int CheatPenaltyLives => cheatPenaltyLives;
    public float FalseAlarmCooldown => falseAlarmCooldown;

    // Trampas en vivo
    public int FirstTrickRound => firstTrickRound;
    public float TrickWindow => trickWindow;
    public float TrickTimeStart => trickTimeStart;
    public float TrickTimeEnd => Mathf.Max(trickTimeStart, trickTimeEnd);
    public float StealFraction => stealFraction;
    public int StealMin => stealMin;
    public int GoalRigFromRound => goalRigFromRound;
    public float GoalRigFraction => goalRigFraction;
    public int GoalRigMin => goalRigMin;
    public int CrossedFromRound => crossedFromRound;
    public int BluffFromRound => bluffFromRound;

    // Racha
    public int StreakMilestone => streakMilestone;
    public int StreakBonusPerMilestone => streakBonusPerMilestone;
    public int StreakMaxBonus => streakMaxBonus;

    // Emoción
    public float NearMissWindow => nearMissWindow;
    public float HitStopDuration => hitStopDuration;
    public float HitStopTimeScale => hitStopTimeScale;
    public float FinishSlowMoWindow => finishSlowMoWindow;
    public float FinishSlowMoTimeScale => finishSlowMoTimeScale;
    public float DangerSeconds => dangerSeconds;
    public int CountdownSeconds => countdownSeconds;

    // Obstáculos que se mueven
    public float RollingBallSpeed => rollingBallSpeed;
    public float RollingBallPassPoint => rollingBallPassPoint;
    public float SweeperDegreesPerSecond => sweeperDegreesPerSecond;
    public int RollingBallFromRound => rollingBallFromRound;
    public int SweeperFromRound => sweeperFromRound;

    // Eventos sorpresa
    public int SurpriseFromRound => surpriseFromRound;
    public float SurpriseChance => surpriseChance;
    public float SurpriseWindowStart => surpriseWindowStart;
    public float SurpriseWindowEnd => Mathf.Max(surpriseWindowStart, surpriseWindowEnd);
    public float StarRainDuration => starRainDuration;
    public float StarRainInterval => starRainInterval;
    public float StarRainGoldenChance => starRainGoldenChance;
    public float DoubleStarsDuration => doubleStarsDuration;
    public float BlackoutEventDuration => blackoutEventDuration;

    // Apuesta
    public int FirstBetRound => firstBetRound;
    public float BetGoalMultiplier => betGoalMultiplier;

    // Premios
    public int PrizeChoices => prizeChoices;
    public int PrizesPerWin => prizesPerWin;
    public int FirstPoisonRound => firstPoisonRound;
    public float PoisonChance => poisonChance;
    public int RareFromRound => rareFromRound;
    public int EpicFromRound => epicFromRound;
    public float SpringJumpHeightBonus => springJumpHeightBonus;
    public float SpringJumpDurationBonus => springJumpDurationBonus;
    public float SlowMotionSpeedMultiplier => slowMotionSpeedMultiplier;
    public int PoisonHeartGoalPenalty => poisonHeartGoalPenalty;
    public float PoisonSpringJumpMultiplier => poisonSpringJumpMultiplier;
    public float PoisonSlowLaneChangeMultiplier => poisonSlowLaneChangeMultiplier;
    public float PoisonShieldDuration => poisonShieldDuration;
    public int TipStars => tipStars;
    public float SneakersLaneChangeMultiplier => sneakersLaneChangeMultiplier;
    public float SafeTrickMultiplier => safeTrickMultiplier;
    public float DetectiveBadgeMultiplier => detectiveBadgeMultiplier;
    public float RiggedContractGoalMultiplier => riggedContractGoalMultiplier;

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

    /// <summary>Qué parte de las gradas está llena en esta ronda (sube hasta 1 en crowdFullRound).</summary>
    public float GetCrowdShare(int round) =>
        crowdFullRound <= 1 ? 1f : Mathf.Lerp(crowdStartShare, 1f, Mathf.Clamp01((round - 1f) / (crowdFullRound - 1f)));

    /// <summary>Probabilidad de que Tito mienta con la regla en esta ronda.</summary>
    public float GetLieChance(int round) =>
        round < firstLieRound ? 0f : Mathf.Min(maxLieChance, lieChance + lieChancePerRound * (round - firstLieRound));

    /// <summary>Estrellas que da acertar «¡Trampa!» en esta ronda, sin racha ni Lupa.</summary>
    public int GetCheatReward(int round) => cheatRewardBase + cheatRewardPerRound * Mathf.Max(1, round);

    /// <summary>Cuántas trampas en vivo hace Tito en esta ronda (faroles incluidos).</summary>
    public int GetTrickCount(int round) =>
        round < firstTrickRound ? 0 : Mathf.Min(maxTricksPerRound, 1 + (round - firstTrickRound) / roundsPerExtraTrick);

    /// <summary>Probabilidad de que una trampa sea un farol en esta ronda.</summary>
    public float GetBluffChance(int round) =>
        round < bluffFromRound ? 0f : Mathf.Min(maxBluffChance, bluffChance + bluffChancePerRound * (round - bluffFromRound));

    /// <summary>Probabilidad de que una tarjeta de premio sea rara (sin contar las épicas).</summary>
    public float GetRareChance(int round) =>
        round < rareFromRound ? 0f : Mathf.Min(maxRareChance, rareChance + rareChancePerRound * (round - rareFromRound));

    /// <summary>Probabilidad de que una tarjeta de premio sea épica.</summary>
    public float GetEpicChance(int round) =>
        round < epicFromRound ? 0f : Mathf.Min(maxEpicChance, epicChance + epicChancePerRound * (round - epicFromRound));

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

    /// <summary>Versión de los datos del asset (ver CurrentDataVersion).</summary>
    public int DataVersion => dataVersion;
}
