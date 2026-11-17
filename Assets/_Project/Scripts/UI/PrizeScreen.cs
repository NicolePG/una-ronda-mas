using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Muestra las ofertas de premio y registra las elecciones del jugador.</summary>
public class PrizeScreen : MonoBehaviour
{
    private const int OfferCount = 3;

    [Header("Panel")]
    [Tooltip("Panel visible durante la selección de premios.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Indica cuántos premios quedan por elegir.")]
    [SerializeField] private TMP_Text picksRemainingText;
    [Tooltip("Muestra el efecto revelado al elegir un premio envenenado.")]
    [SerializeField] private TMP_Text revealText;

    [Header("Tarjetas de premios")]
    [Tooltip("Botones de las tarjetas en orden; corresponden a las teclas 1, 2 y 3.")]
    [SerializeField] private Button[] prizeButtons = new Button[OfferCount];
    [Tooltip("Nombres de las tarjetas en el mismo orden que sus botones.")]
    [SerializeField] private TMP_Text[] prizeNames = new TMP_Text[OfferCount];
    [Tooltip("Descripciones de las tarjetas en el mismo orden que sus botones.")]
    [SerializeField] private TMP_Text[] prizeDescriptions = new TMP_Text[OfferCount];
    [Tooltip("Imágenes de los premios; pueden dejarse vacías si no hay íconos.")]
    [SerializeField] private Image[] prizeIcons = new Image[OfferCount];
    [Tooltip("Indicadores visuales de posible regalo envenenado.")]
    [SerializeField] private GameObject[] poisonIndicators = new GameObject[OfferCount];

    private static readonly Color PoisonTint = new Color(0.78f, 1f, 0.7f);

    private readonly UnityEngine.Events.UnityAction[] chooseActions =
        new UnityEngine.Events.UnityAction[OfferCount];

    // Halo de cada tarjeta con el color de su rareza (lo agrega la interfaz moderna; puede faltar).
    private readonly Image[] rarityGlows = new Image[OfferCount];
    private readonly PrizeRarity[] shownRarity = new PrizeRarity[OfferCount];

    private void Awake()
    {
        if (panel == null || picksRemainingText == null || revealText == null
            || !HasThreeItems(prizeButtons) || !HasThreeItems(prizeNames)
            || !HasThreeItems(prizeDescriptions) || !HasThreeSlots(prizeIcons)
            || !HasThreeItems(poisonIndicators))
        {
            Debug.LogError("PrizeScreen: asigna panel, textos y tres elementos en cada lista de tarjetas.", this);
            enabled = false;
        }
        else
        {
            revealText.gameObject.SetActive(false);
            for (int i = 0; i < OfferCount; i++)
            {
                Transform card = prizeNames[i].transform.parent;
                Transform glow = card != null ? card.Find("RarityGlow") : null;
                rarityGlows[i] = glow != null ? glow.GetComponent<Image>() : null;
            }
        }
    }

    private void OnEnable()
    {
        GameEvents.PhaseChanged += HandlePhaseChanged;
        GameEvents.RoundWon += HandleRoundWon;
        GameEvents.PrizeChosen += HandlePrizeChosen;

        for (int i = 0; i < OfferCount; i++)
        {
            int index = i;
            chooseActions[index] = () => ChoosePrize(index);
            if (prizeButtons != null && index < prizeButtons.Length && prizeButtons[index] != null)
                prizeButtons[index].onClick.AddListener(chooseActions[index]);
        }

        RefreshFromGameManager();
    }

    private void OnDisable()
    {
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        GameEvents.RoundWon -= HandleRoundWon;
        GameEvents.PrizeChosen -= HandlePrizeChosen;

        for (int i = 0; i < OfferCount; i++)
        {
            if (prizeButtons != null && i < prizeButtons.Length && prizeButtons[i] != null
                && chooseActions[i] != null)
            {
                prizeButtons[i].onClick.RemoveListener(chooseActions[i]);
            }
        }
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        PulseRarityGlows();
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) ChoosePrize(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) ChoosePrize(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) ChoosePrize(2);
    }

    private static bool HasThreeItems<T>(T[] items) where T : Object =>
        items != null && items.Length == OfferCount && System.Array.TrueForAll(items, item => item != null);

    private static bool HasThreeSlots<T>(T[] items) where T : Object =>
        items != null && items.Length == OfferCount;
    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.PrizeSelection)
        {
            RefreshFromGameManager();
            return;
        }

        if (panel != null) panel.SetActive(false);
    }

    private void HandleRoundWon(int round, int prizesToChoose)
    {
        if (revealText != null)
        {
            revealText.text = string.Empty;
            revealText.gameObject.SetActive(false);
        }
        RefreshFromGameManager();
    }

    private void HandlePrizeChosen(PrizeData prize)
    {
        if (prize == null) return;
        if (prize.IsPoisoned && revealText != null)
        {
            revealText.text = prize.PoisonReveal;
            revealText.gameObject.SetActive(true);
        }
        RefreshFromGameManager();
    }

    private void RefreshFromGameManager()
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.Phase != RoundPhase.PrizeSelection)
        {
            if (panel != null) panel.SetActive(false);
            return;
        }

        if (panel != null) panel.SetActive(true);
        // Siempre se elige uno; después de elegir, las otras tarjetas quedan bloqueadas hasta la siguiente ronda.
        bool canPick = game.PrizePicksRemaining > 0;
        if (picksRemainingText != null)
        {
            picksRemainingText.text = !canPick ? "¡Buena elección! Ya viene la siguiente ronda…"
                : game.BetAccepted ? "¡Ganaste la apuesta! Premios raros o mejores · elige 1"
                : "Elige 1 · se queda contigo toda la partida";
        }

        for (int i = 0; i < OfferCount; i++)
        {
            bool hasOffer = i < game.PrizeOffers.Count;
            PrizeData prize = hasOffer ? game.PrizeOffers[i] : null;
            bool taken = hasOffer && game.IsPrizeTaken(i);

            if (prizeButtons[i] != null) prizeButtons[i].interactable = hasOffer && !taken && canPick;
            if (rarityGlows[i] != null)
            {
                rarityGlows[i].gameObject.SetActive(hasOffer && !taken && canPick);
                if (hasOffer) shownRarity[i] = prize.Rarity;
            }
            if (!hasOffer)
            {
                if (prizeNames[i] != null) prizeNames[i].text = "—";
                if (prizeDescriptions[i] != null) prizeDescriptions[i].text = string.Empty;
                if (prizeIcons[i] != null) prizeIcons[i].gameObject.SetActive(false);
                if (poisonIndicators[i] != null) poisonIndicators[i].SetActive(false);
                continue;
            }

            if (prizeNames[i] != null)
                prizeNames[i].text = taken ? $"{prize.DisplayName} <color=#F7B11D>(elegido)</color>" : prize.DisplayName;

            if (prizeDescriptions[i] != null)
                prizeDescriptions[i].text = taken && prize.IsPoisoned
                    ? prize.PoisonReveal
                    : $"<color={RarityColor(prize.Rarity)}><b>{RarityLabel(prize.Rarity)}</b></color>\n{prize.Description}";

            if (prizeIcons[i] != null)
            {
                prizeIcons[i].sprite = prize.Icon;
                prizeIcons[i].color = prize.IsPoisoned ? PoisonTint : Color.white;
                prizeIcons[i].gameObject.SetActive(prize.Icon != null);
            }

            if (poisonIndicators[i] != null)
                poisonIndicators[i].SetActive(prize.IsPoisoned && !taken);
        }
    }

    // Las raras y épicas laten; la común brilla quieta y suave.
    private void PulseRarityGlows()
    {
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
        for (int i = 0; i < OfferCount; i++)
        {
            Image glow = rarityGlows[i];
            if (glow == null || !glow.gameObject.activeSelf) continue;
            ColorUtility.TryParseHtmlString(RarityColor(shownRarity[i]), out Color c);
            c.a = shownRarity[i] == PrizeRarity.Common ? 0.35f : Mathf.Lerp(0.45f, 1f, wave);
            glow.color = c;
        }
    }

    private static string RarityLabel(PrizeRarity rarity) => rarity switch
    {
        PrizeRarity.Epic => "ÉPICO",
        PrizeRarity.Rare => "RARO",
        _ => "COMÚN",
    };

    private static string RarityColor(PrizeRarity rarity) => rarity switch
    {
        PrizeRarity.Epic => "#F7B11D",
        PrizeRarity.Rare => "#7FC8FF",
        _ => "#CFC6EA",
    };

    private void ChoosePrize(int index)
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("No se puede elegir un premio: no hay GameManager activo.", this);
            return;
        }

        if (!game.ChoosePrize(index)) RefreshFromGameManager();
    }
}
