using UnityEngine;

public enum RuleType { DoubleValue, Blackout, Mirror, Giant, NoJump }

/// <summary>Una regla especial de Tito (desde la ronda 3).</summary>
[CreateAssetMenu(fileName = "Rule", menuName = "Una Ronda Más/Regla")]
public class RuleData : ScriptableObject
{
    [SerializeField] private RuleType type;
    [Tooltip("Título que anuncia Tito, por ejemplo «¡Todo vale doble!».")]
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    public RuleType Type => type;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

#if UNITY_EDITOR
    /// <summary>Solo para las herramientas de Editor que crean los assets.</summary>
    public void EditorSetup(RuleType type, string displayName, string description)
    {
        this.type = type;
        this.displayName = displayName;
        this.description = description;
    }
#endif
}
