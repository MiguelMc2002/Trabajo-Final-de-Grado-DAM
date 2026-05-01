/// <summary>
/// Metadatos de un slot de guardado. Representa lo que se muestra en la pantalla
/// de selección de partida: si el slot tiene datos, cuándo se guardaron y cuánto
/// tiempo de juego acumula. No es un MonoBehaviour; se instancia desde
/// <see cref="PantallaSlotsUI"/> al escanear los archivos de partida.
/// </summary>
public class SlotData
{
    /// <summary>
    /// Número de slot (1 a 5). Determina el nombre del archivo en disco:
    /// <c>slot_N.db</c> dentro de <c>Application.persistentDataPath</c>.
    /// </summary>
    public int NumeroSlot { get; set; }

    /// <summary>
    /// Indica si el archivo <c>slot_N.db</c> existe en disco y contiene una partida guardada.
    /// Un slot vacío muestra solo el botón Guardar; uno ocupado muestra los tres botones.
    /// </summary>
    public bool EstaOcupado { get; set; }

    /// <summary>
    /// Nombre visible del slot en la interfaz.
    /// Por defecto se asigna <c>"Partida N"</c> donde N es <see cref="NumeroSlot"/>.
    /// </summary>
    public string NombrePartida { get; set; }

    /// <summary>
    /// Fecha y hora del sistema en que se realizó el último guardado, formateada como
    /// <c>"dd/MM/yyyy HH:mm"</c>. Vacío si el slot no está ocupado.
    /// </summary>
    public string FechaGuardado { get; set; }

    /// <summary>
    /// Número de días de juego transcurridos en la partida guardada, leído desde
    /// la tabla <c>estadoJuego</c> via <see cref="EstadoJuegoDAO"/>. Cero si el slot
    /// no está ocupado.
    /// </summary>
    public int DiasJugados { get; set; }
}
