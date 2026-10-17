using System;
using UnityEngine;

public enum HitCause { Obstacle, Water }
public enum RoundPhase { Announcement, Running, PrizeSelection, GameOver }

/// <summary>
/// Eventos globales del juego (contrato del equipo, ver CLAUDE.md).
/// Suscribirse en OnEnable y desuscribirse en OnDisable.
/// Si un listener lanza una excepción, se muestra en la Consola y los demás listeners igual reciben el evento.
/// </summary>
public static class GameEvents
{
    public static event Action<RoundPhase> PhaseChanged;
    public static event Action<int, int, float> RoundStarted;    // ronda, meta, velocidad (m/s)
    public static event Action<int, int> StarsChanged;           // estrellas de la ronda, meta
    public static event Action<int> LivesChanged;                // vidas actuales
    public static event Action<float> TimeChanged;               // segundos restantes
    public static event Action<HitCause> ContestantHit;
    public static event Action<int, int> RoundWon;               // ronda, cantidad de premios a elegir
    public static event Action<GameStats> GameOver;              // ronda alcanzada, estrellas totales, récords
    public static event Action<RuleData, bool> RuleAnnounced;    // regla anunciada (null si la ronda no tiene), ¿es mentira?
    public static event Action<RuleData> RuleApplied;            // regla que realmente se aplica (null si no hay)
    public static event Action<bool> BetResolved;                // true = aceptó la apuesta
    public static event Action<bool> CheatCalled;                // true = acertó
    public static event Action<PrizeData> PrizeChosen;
    public static event Action<bool> PauseToggled;

    public static void RaisePhaseChanged(RoundPhase phase) => Invoke(PhaseChanged, phase);
    public static void RaiseRoundStarted(int round, int goal, float speed) => Invoke(RoundStarted, round, goal, speed);
    public static void RaiseStarsChanged(int stars, int goal) => Invoke(StarsChanged, stars, goal);
    public static void RaiseLivesChanged(int lives) => Invoke(LivesChanged, lives);
    public static void RaiseTimeChanged(float secondsLeft) => Invoke(TimeChanged, secondsLeft);
    public static void RaiseContestantHit(HitCause cause) => Invoke(ContestantHit, cause);
    public static void RaiseRoundWon(int round, int prizesToChoose) => Invoke(RoundWon, round, prizesToChoose);
    public static void RaiseGameOver(GameStats stats) => Invoke(GameOver, stats);
    public static void RaiseRuleAnnounced(RuleData rule, bool isLie) => Invoke(RuleAnnounced, rule, isLie);
    public static void RaiseRuleApplied(RuleData rule) => Invoke(RuleApplied, rule);
    public static void RaiseBetResolved(bool accepted) => Invoke(BetResolved, accepted);
    public static void RaiseCheatCalled(bool correct) => Invoke(CheatCalled, correct);
    public static void RaisePrizeChosen(PrizeData prize) => Invoke(PrizeChosen, prize);
    public static void RaisePauseToggled(bool paused) => Invoke(PauseToggled, paused);

    // Cada listener se llama por separado para que un error de uno no corte a los demás.
    private static void Invoke<T>(Action<T> handlers, T arg)
    {
        if (handlers == null) return;
        foreach (Delegate d in handlers.GetInvocationList())
        {
            try { ((Action<T>)d)(arg); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private static void Invoke<T1, T2>(Action<T1, T2> handlers, T1 a, T2 b)
    {
        if (handlers == null) return;
        foreach (Delegate d in handlers.GetInvocationList())
        {
            try { ((Action<T1, T2>)d)(a, b); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private static void Invoke<T1, T2, T3>(Action<T1, T2, T3> handlers, T1 a, T2 b, T3 c)
    {
        if (handlers == null) return;
        foreach (Delegate d in handlers.GetInvocationList())
        {
            try { ((Action<T1, T2, T3>)d)(a, b, c); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
