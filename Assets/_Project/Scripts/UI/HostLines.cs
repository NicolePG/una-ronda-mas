using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Elige frases de Tito por momento y las muestra en un globo de diálogo.</summary>
public class HostLines : MonoBehaviour
{
    private const string ResourceName = "HostLines";

    [Header("Presentación")]
    [Tooltip("Fondo del globo que contiene la frase.")]
    [SerializeField] private GameObject speechBubble;
    [Tooltip("Texto TMP donde aparecen las frases de Tito.")]
    [SerializeField] private TMP_Text lineText;
    [Tooltip("Cuánto tiempo permanece visible cada frase, en segundos.")]
    [SerializeField, Min(0.1f)] private float displayDuration = 3f;

    private readonly Dictionary<string, List<string>> linesByMoment =
        new Dictionary<string, List<string>>();
    private readonly Dictionary<string, int> lastLineByMoment =
        new Dictionary<string, int>();
    private float remainingDisplayTime;
    private bool isLoaded;

    private void Awake()
    {
        if (speechBubble == null || lineText == null)
        {
            Debug.LogError("HostLines: asigna el globo y su texto TMP.", this);
            enabled = false;
            return;
        }

        LoadLines();
        lineText.text = string.Empty;
        speechBubble.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.RuleAnnounced += HandleRuleAnnounced;
        GameEvents.RoundStarted += HandleRoundStarted;
        GameEvents.ContestantHit += HandleContestantHit;
        GameEvents.CheatCalled += HandleCheatCalled;
        GameEvents.RoundWon += HandleRoundWon;
        GameEvents.GameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.RuleAnnounced -= HandleRuleAnnounced;
        GameEvents.RoundStarted -= HandleRoundStarted;
        GameEvents.ContestantHit -= HandleContestantHit;
        GameEvents.CheatCalled -= HandleCheatCalled;
        GameEvents.RoundWon -= HandleRoundWon;
        GameEvents.GameOver -= HandleGameOver;
    }

    private void Update()
    {
        if (remainingDisplayTime <= 0f || speechBubble == null) return;
        remainingDisplayTime -= Time.unscaledDeltaTime;
        if (remainingDisplayTime <= 0f) speechBubble.SetActive(false);
    }

    public void ShowMoment(string moment)
    {
        if (lineText == null || speechBubble == null || !isLoaded) return;
        if (!linesByMoment.TryGetValue(moment, out List<string> momentLines) || momentLines.Count == 0)
        {
            Debug.LogError($"HostLines no tiene frases para el momento «{moment}».", this);
            return;
        }

        int previousIndex = lastLineByMoment.TryGetValue(moment, out int previous) ? previous : -1;
        int index = Random.Range(0, momentLines.Count);
        if (momentLines.Count > 1 && index == previousIndex)
            index = (index + Random.Range(1, momentLines.Count)) % momentLines.Count;

        lastLineByMoment[moment] = index;
        lineText.text = momentLines[index];
        speechBubble.SetActive(true);
        remainingDisplayTime = displayDuration;
    }

    private void LoadLines()
    {
        TextAsset csv = Resources.Load<TextAsset>(ResourceName);
        if (csv == null)
        {
            Debug.LogError($"No se encontró Resources/{ResourceName}.csv.", this);
            return;
        }

        string[] rows = csv.text.Split('\n');
        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            string row = rows[rowIndex].TrimEnd('\r').Trim();
            if (string.IsNullOrEmpty(row) || rowIndex == 0 && row == "moment,line") continue;

            if (!TryParseCsvRow(row, out List<string> columns) || columns.Count != 2)
            {
                Debug.LogError($"Fila CSV inválida en Resources/{ResourceName}.csv, línea {rowIndex + 1}.", this);
                continue;
            }

            string moment = columns[0].Trim();
            string line = columns[1].Trim();
            if (string.IsNullOrEmpty(moment) || string.IsNullOrEmpty(line))
            {
                Debug.LogError($"Momento o frase vacíos en Resources/{ResourceName}.csv, línea {rowIndex + 1}.", this);
                continue;
            }

            if (!linesByMoment.TryGetValue(moment, out List<string> momentLines))
            {
                momentLines = new List<string>();
                linesByMoment.Add(moment, momentLines);
            }
            momentLines.Add(line);
        }

        isLoaded = linesByMoment.Count > 0;
        if (!isLoaded) Debug.LogError($"No se cargaron frases desde Resources/{ResourceName}.csv.", this);
    }

    private static bool TryParseCsvRow(string row, out List<string> columns)
    {
        columns = new List<string>();
        var field = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < row.Length; i++)
        {
            char current = row[i];
            if (current == '"')
            {
                if (insideQuotes && i + 1 < row.Length && row[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (current == ',' && !insideQuotes)
            {
                columns.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(current);
            }
        }

        if (insideQuotes) return false;
        columns.Add(field.ToString());
        return true;
    }

    private void HandleRuleAnnounced(RuleData rule, bool isLie) => ShowMoment("rule");

    private void HandleRoundStarted(int round, int goal, float speed)
    {
        GameManager game = GameManager.Instance;
        if (game != null && game.IsBetOffered)
        {
            ShowMoment("bet");
            return;
        }
        ShowMoment("round_start");
    }

    private void HandleContestantHit(HitCause cause)
    {
        if (cause == HitCause.Water) ShowMoment("water_fall");
    }

    private void HandleCheatCalled(bool correct) => ShowMoment(correct ? "cheat_correct" : "cheat_failed");

    private void HandleRoundWon(int round, int prizesToChoose) => ShowMoment("victory");

    private void HandleGameOver(GameStats stats) => ShowMoment("elimination");
}
