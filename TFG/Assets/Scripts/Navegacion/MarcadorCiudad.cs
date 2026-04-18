using TMPro;
using UnityEngine;

/// <summary>
/// Componente que se adjunta al sprite de cada ciudad en el mapamundi.
/// Gestiona el feedback visual al pasar el cursor por encima y delega el viaje
/// al <see cref="MapamundiController"/> cuando el jugador hace clic sobre el marcador.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MarcadorCiudad : MonoBehaviour
{
    // ─── Datos de ciudad ─────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject con la configuración del puerto representado por este marcador.
    /// Asignar desde el Inspector.
    /// </summary>
    public CiudadData DatosCiudad;

    /// <summary>
    /// Etiqueta opcional que muestra el nombre de la ciudad sobre el marcador en el mapa.
    /// Si es <c>null</c>, no se mostrará texto.
    /// </summary>
    public TextMeshPro TextoNombre;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private MapamundiController _controlador;

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Enlaza el marcador con el controlador del mapa y, si hay etiqueta de texto asignada,
    /// escribe el nombre de la ciudad. Llamado por <see cref="MapamundiController"/> en su <c>Start</c>.
    /// </summary>
    /// <param name="controlador">Controlador del mapamundi al que pertenece este marcador.</param>
    public void Inicializar(MapamundiController controlador)
    {
        _controlador = controlador;

        if (TextoNombre != null && DatosCiudad != null)
            TextoNombre.text = DatosCiudad.NombreCiudad;
    }

    // ─── Interacción con el ratón ─────────────────────────────────────────────

    /// <summary>
    /// Inicia el viaje hacia esta ciudad cuando el jugador hace clic sobre el marcador.
    /// </summary>
    private void OnMouseDown()
    {
        Debug.Log($"[MarcadorCiudad] Click detectado en {DatosCiudad?.NombreCiudad ?? "ciudad sin datos"}");
        if (_controlador == null || DatosCiudad == null) return;
        _controlador.ViajarACiudad(DatosCiudad);
    }

    /// <summary>
    /// Amplía el marcador al 120 % cuando el cursor entra sobre él para indicar
    /// que es interactuable.
    /// </summary>
    private void OnMouseEnter()
    {
        transform.localScale = Vector3.one * 1.2f;
    }

    /// <summary>
    /// Restaura el tamaño original del marcador cuando el cursor lo abandona.
    /// </summary>
    private void OnMouseExit()
    {
        transform.localScale = Vector3.one;
    }
}
