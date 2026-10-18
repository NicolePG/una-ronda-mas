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

    private readonly UnityEngine.Events.UnityAction[] chooseActions =
        new UnityEngine.Events.UnityAction[OfferCount];

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
        if (picksRemainingText != null)
        {
            picksRemainingText.text = game.PrizePicksRemaining == 1
                ? "Elige tu premio"
                : $"Elige {game.PrizePicksRemaining} premios";
        }

        for (int i = 0; i < OfferCount; i++)
        {
            bool hasOffer = i < game.PrizeOffers.Count;
            PrizeData prize = hasOffer ? game.PrizeOffers[i] : null;
            bool taken = hasOffer && game.IsPrizeTaken(i);

            if (prizeButtons[i] != null) prizeButtons[i].interactable = hasOffer && !taken;
            if (!hasOffer)
            {
                if (prizeNames[i] != null) prizeNames[i].text = "—";
                if (prizeDescriptions[i] != null) prizeDescriptions[i].text = string.Empty;
                if (prizeIcons[i] != null) prizeIcons[i].gameObject.SetActive(false);
                if (poisonIndicators[i] != null) poisonIndicators[i].SetActive(false);
                continue;
            }

            if (prizeNames[i] != null)
                prizeNames[i].text = taken ? $"{prize.DisplayName} ✓" : prize.DisplayName;

            if (prizeDescriptions[i] != null)
                prizeDescriptions[i].text = taken && prize.IsPoisoned
                    ? prize.PoisonReveal
                    : prize.Description;

            if (prizeIcons[i] != null)
            {
                prizeIcons[i].sprite = prize.Icon;
                prizeIcons[i].gameObject.SetActive(prize.Icon != null);
            }

            if (poisonIndicators[i] != null)
                poisonIndicators[i].SetActive(prize.IsPoisoned && !taken);
        }
    }

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
