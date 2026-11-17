using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ayudas y ánimo en pantalla (arriba al centro, debajo del HUD):
/// - Al empezar cada partida, los controles durante unos segundos; en la primera ronda con trampas, cómo acusar a Tito.
/// - Mensajes de ánimo cuando todo va bien: mitad de la meta, «¡ya casi!», buen ritmo, sin chocar, reflejos,
///   «¡arriba!» después de un golpe y cuando llega más público. Uno a la vez y con pausa entre ellos.
/// - Avisos urgentes de las zonas oscuras (DarkZones): «¡Zona oscura adelante!», «¡Linterna!», «¡A ciegas!».
/// Arma su propia interfaz por código (no necesita prefab). Tiempo sin escala para las animaciones.
/// </summary>
[DisallowMultipleComponent]
public class CoachMessages : MonoBehaviour
{
    [Header("Partes (las pone la herramienta que arma Game.unity)")]
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private Material titleMaterial;
    [Tooltip("Fondo del cartel de controles (vidrio). Si falta, un color plano.")]
    [SerializeField] private Sprite panelSprite;
    [Tooltip("Fondo de cada tecla. Si falta, un color plano.")]
    [SerializeField] private Sprite keySprite;

    [Header("Controles")]
    [Tooltip("Segundos que se ven los controles al empezar la partida.")]
    [SerializeField, Min(1f)] private float controlsTime = 9f;

    [Header("Ánimo")]
    [Tooltip("Segundos mínimos entre un mensaje y el siguiente.")]
    [SerializeField, Min(0.5f)] private float minGap = 3.5f;
    [Tooltip("Segundos mínimos antes de un aviso urgente (zona oscura, linterna).")]
    [SerializeField, Min(0.2f)] private float urgentGap = 0.8f;
    [SerializeField, Min(0.5f)] private float toastTime = 2.2f;
    [Tooltip("En qué parte de la ronda (0 a 1) se revisa si va al ritmo de la meta.")]
    [SerializeField, Range(0.1f, 0.9f)] private float paceCheckAt = 0.45f;
    [Tooltip("Segundos sin chocar para el mensaje «¡Sin un rasguño!».")]
    [SerializeField, Min(3f)] private float cleanRunSeconds = 15f;
    [Tooltip("Esquives al límite seguidos para «¡Qué reflejos!» y en cuántos segundos.")]
    [SerializeField, Min(2)] private int nearMissCombo = 3;
    [SerializeField, Min(1f)] private float nearMissWindow = 6f;

    private static readonly Color Gold = new Color32(247, 177, 29, 255);
    private static readonly string[] GoodTitles = { "¡TODO VA BIEN!", "¡EXCELENTE RITMO!", "¡ASÍ SE JUEGA!", "¡SIGUE ASÍ!" };
    private static readonly string[] HalfSubs = { "¡Vas muy bien!", "El público está contigo", "¡Qué buen ritmo!" };

    private struct Message
    {
        public string Title, Sub;
        public float Duration, QueuedAt;
        public bool Urgent;   // aviso de juego (zona oscura): no espera la pausa entre mensajes
    }

    private readonly List<Message> queue = new List<Message>();
    private readonly List<float> nearMisses = new List<float>();

    private CanvasGroup controlsGroup;
    private RectTransform toast;
    private CanvasGroup toastGroup;
    private TMP_Text toastTitle, toastSub;

    private float controlsShownAt = -100f;
    private float toastShownAt = -100f;
    private float toastDuration;
    private float lastToastAt = -100f;
    private bool controlsShown, trickTipShown;

    // Por ronda
    private bool shownHalf, shownAlmost, shownPace, shownClean, shownComeback;
    private float lastHitElapsed;
    private float comebackAt = -1f;

    private void Awake() => BuildUi();

    private void OnEnable()
    {
        GameEvents.PhaseChanged += HandlePhaseChanged;
        GameEvents.StarsChanged += HandleStarsChanged;
        GameEvents.ContestantHit += HandleHit;
        Obstacle.NearMiss += HandleNearMiss;
        AudienceCrowd.CrowdGrew += HandleCrowdGrew;
        DarkZones.ZoneWarning += HandleZoneWarning;
        DarkZones.FlashlightCollected += HandleFlashlight;
        DarkZones.ZoneEntered += HandleZoneEntered;
    }

    private void OnDisable()
    {
        GameEvents.PhaseChanged -= HandlePhaseChanged;
        GameEvents.StarsChanged -= HandleStarsChanged;
        GameEvents.ContestantHit -= HandleHit;
        Obstacle.NearMiss -= HandleNearMiss;
        AudienceCrowd.CrowdGrew -= HandleCrowdGrew;
        DarkZones.ZoneWarning -= HandleZoneWarning;
        DarkZones.FlashlightCollected -= HandleFlashlight;
        DarkZones.ZoneEntered -= HandleZoneEntered;
    }

    // ---------- Eventos ----------

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase != RoundPhase.Running)
        {
            queue.Clear();
            return;
        }
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        shownHalf = shownAlmost = shownPace = shownClean = shownComeback = false;
        lastHitElapsed = 0f;
        comebackAt = -1f;
        nearMisses.Clear();

        if (!controlsShown)
        {
            controlsShown = true;
            controlsShownAt = Time.unscaledTime + 1f; // después del «¡YA!»
        }
        if (!trickTipShown && gm.CurrentRound >= gm.Balance.FirstTrickRound)
        {
            trickTipShown = true;
            Enqueue("¡DESDE AHORA TITO HACE TRAMPA!", "Si ves algo raro (estrellas, meta o controles), acúsalo con T", 3.5f);
        }
    }

    private void HandleStarsChanged(int stars, int goal)
    {
        if (!IsRunning(out _) || goal <= 0) return;
        float ratio = (float)stars / goal;
        if (ratio >= 1f) return; // «¡META!» lo muestra RoundAnnouncer
        if (!shownAlmost && ratio >= 0.8f)
        {
            shownAlmost = shownHalf = true;
            Enqueue("¡YA CASI!", $"Te faltan {goal - stars} estrellas");
        }
        else if (!shownHalf && ratio >= 0.5f)
        {
            shownHalf = true;
            Enqueue("¡MITAD DE LA META!", Pick(HalfSubs));
        }
    }

    private void HandleHit(HitCause cause)
    {
        if (!IsRunning(out GameManager gm)) return;
        lastHitElapsed = gm.RoundElapsed;
        if (!shownComeback) comebackAt = Time.unscaledTime + 1.3f;
    }

    private void HandleNearMiss(Obstacle obstacle)
    {
        float now = Time.unscaledTime;
        nearMisses.Add(now);
        nearMisses.RemoveAll(t => now - t > nearMissWindow);
        if (nearMisses.Count < nearMissCombo) return;
        nearMisses.Clear();
        Enqueue("¡QUÉ REFLEJOS!", "Esquivaste por un pelo");
    }

    private void HandleCrowdGrew(int added, int visible, int total) =>
        Enqueue("¡LLEGA MÁS PÚBLICO!", $"+{added} espectadores · {visible} de {total}");

    // Zonas oscuras: van primero en la cola porque hay que reaccionar ya.
    private void HandleZoneWarning() =>
        Enqueue("¡ZONA OSCURA ADELANTE!", "Agarra la linterna dorada antes de entrar", 2.6f, true);

    private void HandleFlashlight() =>
        Enqueue("¡LINTERNA!", "Ahora vas a ver en la oscuridad", 1.8f, true);

    private void HandleZoneEntered(bool withLight)
    {
        if (!withLight) Enqueue("¡A CIEGAS!", "Sin linterna: sigue las estrellas que brillan", 2.2f, true);
    }

    // ---------- Cada frame ----------

    private void Update()
    {
        float now = Time.unscaledTime;
        CheckRound();
        AnimateControls(now);

        if (!ControlsVisible(now) && !RoundAnnouncer.IsBusy && queue.Count > 0
            && (now - lastToastAt >= minGap || (queue[0].Urgent && now - lastToastAt >= urgentGap)))
        {
            Message message = queue[0];
            queue.RemoveAt(0);
            if (now - message.QueuedAt < 6f) ShowToast(message, now); // los muy viejos ya no vienen al caso
        }
        AnimateToast(now);
    }

    private void CheckRound()
    {
        if (!IsRunning(out GameManager gm)) return;
        float duration = gm.Balance.RoundDuration;
        float elapsed = gm.RoundElapsed;

        if (!shownPace && elapsed >= duration * paceCheckAt && gm.CurrentGoal > 0)
        {
            shownPace = true;
            float progress = (float)gm.RoundStars / gm.CurrentGoal;
            int missing = gm.CurrentGoal - gm.RoundStars;
            if (progress >= 1f) { }
            else if (progress >= elapsed / duration) Enqueue(Pick(GoodTitles), "Vas al ritmo de la meta");
            else Enqueue("¡TÚ PUEDES!", $"Faltan {missing} estrellas: ¡busca las doradas!");
        }
        if (!shownClean && elapsed - lastHitElapsed >= cleanRunSeconds)
        {
            shownClean = true;
            Enqueue("¡SIN UN RASGUÑO!", $"{cleanRunSeconds:0} segundos sin chocar");
        }
        if (comebackAt > 0f && Time.unscaledTime >= comebackAt)
        {
            comebackAt = -1f;
            shownComeback = true;
            if (gm.Lives > 0) Enqueue("¡ARRIBA!", gm.Lives == 1 ? "Te queda 1 vida: ¡con cuidado!" : $"Todavía tienes {gm.Lives} vidas");
        }
    }

    private static bool IsRunning(out GameManager gm)
    {
        gm = GameManager.Instance;
        return gm != null && gm.Phase == RoundPhase.Running && !gm.IsPaused;
    }

    private void Enqueue(string title, string sub, float duration = 0f, bool urgent = false)
    {
        if (queue.Count >= 3)
        {
            int drop = queue.FindIndex(m => !m.Urgent); // primero se descarta el ánimo más viejo
            queue.RemoveAt(drop >= 0 ? drop : 0);
        }
        var message = new Message
        {
            Title = title, Sub = sub, Duration = duration > 0f ? duration : toastTime,
            QueuedAt = Time.unscaledTime, Urgent = urgent,
        };
        // Los urgentes se adelantan a los mensajes de ánimo.
        int index = urgent ? queue.FindIndex(m => !m.Urgent) : -1;
        if (index >= 0) queue.Insert(index, message);
        else queue.Add(message);
    }

    private static string Pick(string[] options) => options[Random.Range(0, options.Length)];

    // ---------- Animación ----------

    // También cuenta el segundo de espera antes de aparecer (así no se cruza con un mensaje).
    private bool ControlsVisible(float now) => now - controlsShownAt < controlsTime;

    private void AnimateControls(float now)
    {
        if (controlsGroup == null) return;
        float t = now - controlsShownAt;
        float alpha = t < 0f || t > controlsTime ? 0f : Mathf.Min(Mathf.Clamp01(t / 0.3f), Mathf.Clamp01((controlsTime - t) / 0.6f));
        controlsGroup.alpha = alpha;
        if (controlsGroup.gameObject.activeSelf != alpha > 0f) controlsGroup.gameObject.SetActive(alpha > 0f);
    }

    private void ShowToast(Message message, float now)
    {
        toastTitle.text = message.Title;
        toastSub.text = message.Sub;
        toastShownAt = now;
        lastToastAt = now;
        toastDuration = message.Duration;
        toast.gameObject.SetActive(true);
    }

    private void AnimateToast(float now)
    {
        if (toast == null || !toast.gameObject.activeSelf) return;
        float t = now - toastShownAt;
        if (t > toastDuration)
        {
            toast.gameObject.SetActive(false);
            return;
        }
        // Golpe de entrada y se desvanece al final.
        toast.localScale = Vector3.one * (t < 0.18f ? Mathf.Lerp(1.35f, 1f, t / 0.18f) : 1f);
        toastGroup.alpha = Mathf.Min(Mathf.Clamp01(t / 0.12f), Mathf.Clamp01((toastDuration - t) / 0.4f));
    }

    // ---------- Interfaz (por código) ----------

    private void BuildUi()
    {
        var canvasGo = new GameObject("CoachCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12; // encima del HUD, debajo de los carteles de Tito y las pantallas
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        BuildControls(root);

        toast = NewRect(root, "Toast", new Vector2(0f, -150f), new Vector2(900f, 110f));
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastTitle = NewText(toast, "Title", new Vector2(0f, -8f), new Vector2(900f, 60f), 46f, Gold, true);
        toastSub = NewText(toast, "Sub", new Vector2(0f, -66f), new Vector2(900f, 34f), 24f, Color.white, false);
        toast.gameObject.SetActive(false);
    }

    private void BuildControls(Transform root)
    {
        RectTransform card = NewRect(root, "Controls", new Vector2(0f, -140f), new Vector2(880f, 128f));
        controlsGroup = card.gameObject.AddComponent<CanvasGroup>();
        Image background = card.gameObject.AddComponent<Image>();
        background.raycastTarget = false;
        if (panelSprite != null)
        {
            background.sprite = panelSprite;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1.5f;
        }
        else background.color = new Color(0.08f, 0.05f, 0.18f, 0.88f);

        NewText(card, "Header", new Vector2(0f, -10f), new Vector2(880f, 26f), 18f, Gold, true).text = "CONTROLES";
        (string key, string action)[] items =
        {
            ("A / D", "carril (o flechas)"),
            ("ESPACIO / W", "saltar"),
            ("S", "deslizarse (o flecha abajo)"),
            ("ESC", "pausa"),
        };
        for (int i = 0; i < items.Length; i++)
        {
            float x = -315f + i * 210f;
            RectTransform chip = NewRect(card, "Key", new Vector2(x, -44f), new Vector2(170f, 40f));
            Image chipImage = chip.gameObject.AddComponent<Image>();
            chipImage.raycastTarget = false;
            if (keySprite != null)
            {
                chipImage.sprite = keySprite;
                chipImage.type = Image.Type.Sliced;
                chipImage.pixelsPerUnitMultiplier = 2f;
            }
            else chipImage.color = new Color(0.42f, 0.28f, 0.85f);
            TMP_Text key = NewText(chip, "Label", new Vector2(0f, -4f), new Vector2(170f, 32f), 22f, Color.white, true);
            key.text = items[i].key;
            NewText(card, "Action", new Vector2(x, -90f), new Vector2(205f, 28f), 18f, new Color(1f, 1f, 1f, 0.9f), false).text = items[i].action;
        }
        card.gameObject.SetActive(false);
    }

    private static RectTransform NewRect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private TMP_Text NewText(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, Color color, bool title)
    {
        RectTransform rect = NewRect(parent, name, position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = title && titleFont != null ? titleFont : bodyFont;
        if (font != null) text.font = font;
        if (title && titleMaterial != null && font == titleFont) text.fontSharedMaterial = titleMaterial;
        text.fontSize = fontSize;
        text.fontStyle = title ? FontStyles.Bold : FontStyles.Normal;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la escena.</summary>
    public void EditorSetup(TMP_FontAsset bodyFont, TMP_FontAsset titleFont, Material titleMaterial, Sprite panelSprite, Sprite keySprite)
    {
        this.bodyFont = bodyFont;
        this.titleFont = titleFont;
        this.titleMaterial = titleMaterial;
        this.panelSprite = panelSprite;
        this.keySprite = keySprite;
    }
#endif
}
