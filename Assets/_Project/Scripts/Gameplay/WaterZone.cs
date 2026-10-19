using UnityEngine;

/// <summary>
/// Hueco al agua. Trigger que cubre el pedazo de carril sin piso: si el concursante pisa ahí (sin saltar), cae al agua.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class WaterZone : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Awake() => GetComponent<Collider>().isTrigger = true;
}
