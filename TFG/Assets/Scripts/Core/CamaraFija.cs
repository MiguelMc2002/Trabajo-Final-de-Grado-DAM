using UnityEngine;

/// <summary>
/// Fija el tamaño ortográfico de la cámara para impedir zoom accidental.
/// </summary>
public class CamaraFija : MonoBehaviour
{
    [SerializeField] float _size = 0.71f;

    Camera _cam;

    /// <summary>Obtiene el componente Camera y aplica el tamaño inicial.</summary>
    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographicSize = _size;
    }

    /// <summary>Reaplica el tamaño cada frame para evitar cualquier modificación externa.</summary>
    void Update()
    {
        _cam.orthographicSize = _size;
    }
}
