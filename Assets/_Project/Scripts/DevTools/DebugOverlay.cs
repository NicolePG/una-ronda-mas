using System.Text;
using UnityEngine;

/// <summary>
/// Panel de depuración (solo en el Editor y en builds de desarrollo): muestra el estado de la partida
/// y trae atajos para probar sin la UI. Desactiva «handleKeys» cuando la UI de Tania use las teclas 1-2-3.
/// </summary>
public class DebugOverlay : MonoBehaviour
{
    private const int CheatStars = 5;
    private const float EndRoundSeconds = 0.5f;
    private const float PanelWidth = 380f;
    private const float PanelMargin = 10f;
    private const int FontSize = 14;

    [Header("Panel")]
    [SerializeField] private bool showPanel = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("Atajos")]
    [Tooltip("Anuncio: 1 acepta la apuesta, 2/Enter empieza sin apuesta. Premios: 1-2-3. Eliminado: R. Ronda: F2 +5 estrellas, F3 termina la ronda, F4 choque, F5 agua, F6 siguiente ronda.")]
    [SerializeField] private bool handleKeys = true;

    private GUIStyle style;
    private readonly StringBuilder text = new StringBuilder();

    private void Awake()
    {
        if (!Debug.isDebugBuild) enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) showPanel = !showPanel;
        GameManager gm = GameManager.Instance;
        if (!handleKeys || gm == null) return;

        switch (gm.Phase)
        {
            case RoundPhase.Announcement:
                if (Pressed(0)) gm.ConfirmAnnouncement(true);
                else if (Pressed(1) || Input.GetKeyDown(KeyCode.Return)) gm.ConfirmAnnouncement(false);
                break;
            case RoundPhase.PrizeSelection:
                for (int i = 0; i < gm.PrizeOffers.Count; i++)
                {
                    if (Pressed(i)) gm.ChoosePrize(i);
                }
                break;
            case RoundPhase.GameOver:
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)) gm.RestartGame();
                break;
            case RoundPhase.Running:
                if (Input.GetKeyDown(KeyCode.F4)) GameEvents.RaiseContestantHit(HitCause.Obstacle);
                if (Input.GetKeyDown(KeyCode.F5)) GameEvents.RaiseContestantHit(HitCause.Water);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Input.GetKeyDown(KeyCode.F2)) gm.DebugAddStars(CheatStars);
                if (Input.GetKeyDown(KeyCode.F3)) gm.DebugSetTimeRemaining(EndRoundSeconds);
#endif
                break;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F6) && gm.Phase != RoundPhase.GameOver) gm.DebugStartRound(gm.CurrentRound + 1);
#endif
    }

    private static bool Pressed(int index)
    {
        return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index))
            || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + index));
    }

    private void OnGUI()
    {
        if (!showPanel) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = FontSize,
                richText = true,
                wordWrap = true,
            };
            style.normal.textColor = Color.white;
        }

        GameManager gm = GameManager.Instance;
        string content = gm == null ? "No hay GameManager en la escena." : BuildText(gm);
        float height = style.CalcHeight(new GUIContent(content), PanelWidth);
        GUI.Box(new Rect(PanelMargin, PanelMargin, PanelWidth, height), content, style);
    }

    private string BuildText(GameManager gm)
    {
        RuleManager rules = gm.Rules;
        TrackGenerator track = gm.Track;
        text.Clear();
        text.AppendLine($"<b>DEBUG</b> (F1 oculta)   Fase: <b>{gm.Phase}</b>{(gm.IsPaused ? "  [PAUSA]" : "")}");
        text.AppendLine($"Ronda {gm.CurrentRound}   Estrellas {gm.RoundStars}/{gm.CurrentGoal}   Total {gm.TotalStars}   Vidas {gm.Lives}");
        text.AppendLine($"Tiempo {gm.TimeRemaining:0.0} s   Velocidad {gm.RoundSpeed:0.0} m/s");

        string announced = rules.AnnouncedRule != null ? rules.AnnouncedRule.DisplayName : "—";
        string applied = rules.AppliedRule != null ? rules.AppliedRule.DisplayName : "—";
        text.AppendLine($"Regla dicha: {announced}   real: {applied}{(rules.IsLie ? "  <color=red>MIENTE</color>" : "")}");
        if (gm.IsBetOffered)
            text.AppendLine($"Apuesta: meta {gm.BaseGoal} → {gm.BetGoal}   {(gm.Phase == RoundPhase.Announcement ? "(1 acepta / 2 rechaza)" : gm.BetAccepted ? "ACEPTADA" : "rechazada")}");
        text.AppendLine($"¡Trampa! (T): {(rules.CanCallCheat ? "disponible" : rules.CheatUsed ? "usada" : "no")}");
        text.AppendLine($"Premios: {gm.Modifiers.Describe()}");
        text.AppendLine($"Escudos {gm.Modifiers.ShieldsLeft}+{gm.Modifiers.PoisonShieldsLeft}   Flotadores {gm.Modifiers.FloatsLeft}");
        if (track != null)
            text.AppendLine($"Pista: {track.Travelled:0}/{track.RoundDistance:0} m   arco z={track.FinishArchZ:0.0}   estrellas creadas {track.SpawnedStarValue}");

        if (gm.Phase == RoundPhase.PrizeSelection)
        {
            text.AppendLine($"<b>Elige {gm.PrizePicksRemaining}:</b>");
            for (int i = 0; i < gm.PrizeOffers.Count; i++)
            {
                PrizeData prize = gm.PrizeOffers[i];
                string poison = prize.IsPoisoned ? " <color=red>(envenenado)</color>" : "";
                string taken = gm.IsPrizeTaken(i) ? " (elegido)" : "";
                text.AppendLine($"  [{i + 1}] {prize.DisplayName}{poison}{taken}");
            }
        }
        if (gm.Phase == RoundPhase.GameOver && gm.LastStats != null)
        {
            GameStats s = gm.LastStats;
            text.AppendLine($"<b>ELIMINADO</b> ronda {s.RoundReached}, {s.TotalStars} estrellas. Récord: ronda {s.BestRound}, {s.BestStars} estrellas. Partidas: {s.GamesPlayed}. (R = una ronda más)");
        }
        if (handleKeys) text.Append("F2 +5 estrellas  F3 fin ronda  F4 choque  F5 agua  F6 sig. ronda  Esc pausa");
        return text.ToString();
    }
}
