using UnityEngine;

public enum PrizeType { ExtraHeart, Magnet, Float, Shield, Spring, SlowMotion }

/// <summary>
/// Un premio que se elige al ganar una ronda. Los envenenados son assets aparte
/// con isPoisoned = true y el mismo nombre que el normal (se revelan al elegirlos).
/// </summary>
[CreateAssetMenu(fileName = "Prize", menuName = "Una Ronda Más/Premio")]
public class PrizeData : ScriptableObject
{
    [Header("Efecto")]
    [SerializeField] private PrizeType type;
    [Tooltip("Regalo de Tito: se ve igual que el normal pero tiene un efecto oculto.")]
    [SerializeField] private bool isPoisoned;

    [Header("Texto e ícono")]
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [Tooltip("Lo que se muestra al elegirlo (solo si es envenenado).")]
    [TextArea(2, 4)]
    [SerializeField] private string poisonReveal;
    [SerializeField] private Sprite icon;

    public PrizeType Type => type;
    public bool IsPoisoned => isPoisoned;
    public string DisplayName => displayName;
    public string Description => description;
    public string PoisonReveal => poisonReveal;
    public Sprite Icon => icon;

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los assets.</summary>
    public void EditorSetup(PrizeType type, bool isPoisoned, string displayName, string description, string poisonReveal)
    {
        this.type = type;
        this.isPoisoned = isPoisoned;
        this.displayName = displayName;
        this.description = description;
        this.poisonReveal = poisonReveal;
    }
#endif
}
