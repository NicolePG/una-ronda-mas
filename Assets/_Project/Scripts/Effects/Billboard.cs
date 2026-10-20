using UnityEngine;

/// <summary>Mira siempre a la cámara (halos de las estrellas, textos flotantes).</summary>
[DisallowMultipleComponent]
public class Billboard : MonoBehaviour
{
    private static Camera cachedCamera;

    private void LateUpdate()
    {
        if (cachedCamera == null || !cachedCamera.isActiveAndEnabled) cachedCamera = Camera.main;
        if (cachedCamera != null) transform.rotation = cachedCamera.transform.rotation;
    }
}
