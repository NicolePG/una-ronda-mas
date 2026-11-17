using TMPro;
using UnityEngine;

/// <summary>
/// Cinta de noticias de la transmisión (abajo del menú): frases del programa y los récords del jugador pasando
/// de derecha a izquierda sin cortes. Usa dos textos iguales que se turnan dentro de una máscara. Tiempo sin escala.
/// </summary>
[DisallowMultipleComponent]
public class TickerTape : MonoBehaviour
{
    [Tooltip("Los dos textos iguales que se turnan (dentro de una RectMask2D).")]
    [SerializeField] private TMP_Text first;
    [SerializeField] private TMP_Text second;
    [Tooltip("Píxeles por segundo.")]
    [SerializeField, Min(1f)] private float speed = 90f;
    [Tooltip("Espacio entre una vuelta y la siguiente (px).")]
    [SerializeField, Min(0f)] private float gap = 80f;

    private const string Separator = "   ·   ";
    private float width;

    private void OnEnable()
    {
        if (first == null || second == null) return;
        string text = BuildText();
        first.text = text;
        second.text = text;
        first.ForceMeshUpdate();
        width = first.preferredWidth + gap;
        first.rectTransform.sizeDelta = new Vector2(first.preferredWidth, first.rectTransform.sizeDelta.y);
        second.rectTransform.sizeDelta = first.rectTransform.sizeDelta;
        first.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        second.rectTransform.anchoredPosition = new Vector2(width, 0f);
    }

    private void Update()
    {
        if (first == null || second == null || width <= 0f) return;
        float step = speed * Time.unscaledDeltaTime;
        Move(first.rectTransform, step);
        Move(second.rectTransform, step);
    }

    // El que sale por la izquierda pasa detrás del otro.
    private void Move(RectTransform text, float step)
    {
        Vector2 p = text.anchoredPosition;
        p.x -= step;
        if (p.x <= -width) p.x += width * 2f;
        text.anchoredPosition = p;
    }

    private static string BuildText()
    {
        string record = SaveManager.BestRound > 0
            ? $"RÉCORD: RONDA {SaveManager.BestRound} CON {SaveManager.BestStars} ESTRELLAS"
            : "TODAVÍA NO HAY RÉCORD: ¡SÉ EL PRIMERO!";
        string[] lines =
        {
            "EN VIVO DESDE EL ESTUDIO 7",
            record,
            "TITO TRAMPA PROMETE NO HACER TRAMPA (MIENTE)",
            "SI LO VES HACIENDO TRAMPA, ¡PRESIONA T!",
            "EL PÚBLICO CRECE CON CADA RONDA",
            $"PARTIDAS JUGADAS: {SaveManager.GamesPlayed}",
            "CADA PREMIO SALE UNA SOLA VEZ: ELIGE BIEN",
            "¿UNA RONDA MÁS?",
        };
        return string.Join(Separator, lines) + Separator;
    }

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que arman la interfaz.</summary>
    public void EditorSetup(TMP_Text first, TMP_Text second)
    {
        this.first = first;
        this.second = second;
    }
#endif
}
