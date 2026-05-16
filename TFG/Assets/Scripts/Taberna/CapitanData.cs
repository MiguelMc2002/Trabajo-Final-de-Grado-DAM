using UnityEngine;

/// <summary>
/// Datos de un capitán contratado o disponible para contratar en la taberna.
/// Clase pura C# — no hereda de MonoBehaviour.
/// </summary>
public class CapitanData
{
    /// <summary>Identificador único del capitán dentro de la sesión.</summary>
    public int IdCapitan { get; }

    /// <summary>Nombre propio del capitán.</summary>
    public string Nombre { get; }

    /// <summary>
    /// Habilidad de navegación del capitán, entre 0.0 y 5.0.
    /// Influye en la velocidad efectiva de la flota y la evasión de tormentas.
    /// </summary>
    public float HabilidadNavegacion { get; }

    /// <summary>
    /// Habilidad de combate del capitán, entre 0.0 y 5.0.
    /// Se usa como <c>HabilidadCapitan</c> en <see cref="FlotaRuntimeData"/> al resolver combates.
    /// </summary>
    public float HabilidadCombate { get; }

    /// <summary>
    /// Identificador del barco al que está asignado este capitán.
    /// Vale <c>-1</c> si el capitán aún no ha sido embarcado.
    /// </summary>
    public int IdBarcoAsignado { get; set; }

    /// <summary>
    /// Inicializa un capitán con el identificador y nombre indicados.
    /// Las habilidades se generan aleatoriamente en el rango [0, 5] con un decimal.
    /// </summary>
    /// <param name="idCapitan">Identificador único del capitán.</param>
    /// <param name="nombre">Nombre propio del capitán.</param>
    public CapitanData(int idCapitan, string nombre)
    {
        IdCapitan           = idCapitan;
        Nombre              = nombre;
        HabilidadNavegacion = Mathf.Round(Random.Range(0f, 5f) * 10f) / 10f;
        HabilidadCombate    = Mathf.Round(Random.Range(0f, 5f) * 10f) / 10f;
        IdBarcoAsignado     = -1;
    }

    /// <summary>
    /// Inicializa un capitán con habilidades explícitas, sin aleatoriedad.
    /// Usar al restaurar capitanes desde la base de datos.
    /// </summary>
    /// <param name="idCapitan">Identificador único del capitán.</param>
    /// <param name="nombre">Nombre propio del capitán.</param>
    /// <param name="habilidadNavegacion">Habilidad de navegación guardada.</param>
    /// <param name="habilidadCombate">Habilidad de combate guardada.</param>
    public CapitanData(int idCapitan, string nombre, float habilidadNavegacion, float habilidadCombate)
    {
        IdCapitan           = idCapitan;
        Nombre              = nombre;
        HabilidadNavegacion = habilidadNavegacion;
        HabilidadCombate    = habilidadCombate;
        IdBarcoAsignado     = -1;
    }
}
