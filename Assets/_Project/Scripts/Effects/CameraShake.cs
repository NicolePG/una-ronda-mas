using UnityEngine;

/// <summary>Sacude la cámara cuando el concursante choca o cae al agua.</summary>
[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    [Tooltip("Fuerza de la sacudida al chocar con un obstáculo (m).")]
    [SerializeField, Min(0f)] private float hitAmplitude = 0.25f;
    [Tooltip("Fuerza de la sacudida al caer al agua (m).")]
    [SerializeField, Min(0f)] private float waterAmplitude = 0.12f;
    [Tooltip("Segundos que dura.")]
    [SerializeField, Min(0.01f)] private float duration = 0.3f;

    private Vector3 home;
    private float amplitude;
    private float timeLeft;

    private void Awake() => home = transform.localPosition;

    private void OnEnable() => GameEvents.ContestantHit += HandleHit;

    private void OnDisable()
    {
        GameEvents.ContestantHit -= HandleHit;
        transform.localPosition = home;
    }

    private void HandleHit(HitCause cause)
    {
        amplitude = cause == HitCause.Water ? waterAmplitude : hitAmplitude;
        timeLeft = duration;
    }

    private void LateUpdate()
    {
        if (timeLeft <= 0f) return;
        timeLeft -= Time.unscaledDeltaTime;
        float strength = Mathf.Max(0f, timeLeft / duration) * amplitude;
        transform.localPosition = timeLeft > 0f ? home + Random.insideUnitSphere * strength : home;
    }
}
