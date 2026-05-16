using System.Collections.Generic;

/// <summary>
/// Agrupa varios barcos del jugador en una formación navegable.
/// El barco líder es el primero en incorporarse y no puede abandonar el convoy.
/// Clase pura C# — no hereda de MonoBehaviour.
/// </summary>
public class ConvoyData
{
    private readonly List<BarcoJugador> _miembros = new List<BarcoJugador>();

    /// <summary>Nombre del convoy, igual al nombre del barco líder.</summary>
    public string NombreConvoy { get; }

    /// <summary>Barco que lidera el convoy. No puede ser eliminado de la formación.</summary>
    public BarcoJugador BarcoLider { get; }

    /// <summary>Lista de barcos miembros del convoy, incluyendo al líder. Solo lectura desde fuera.</summary>
    public IReadOnlyList<BarcoJugador> Miembros => _miembros;

    /// <summary>
    /// Si <c>true</c>, el convoy actúa como pirata en el mapamundi:
    /// los comerciantes le huyen y puede atacarlos.
    /// </summary>
    public bool ModoPirata { get; set; }

    // ─── Propiedades calculadas ───────────────────────────────────────────────

    /// <summary>Suma de la tripulación de todos los barcos miembros.</summary>
    public int TripulacionTotal
    {
        get
        {
            int total = 0;
            foreach (BarcoJugador b in _miembros) total += b.Tripulacion;
            return total;
        }
    }

    /// <summary>
    /// Velocidad del convoy: el barco más lento determina la velocidad de toda la formación.
    /// </summary>
    public float VelocidadConvoy
    {
        get
        {
            if (_miembros.Count == 0) return 0f;
            float min = float.MaxValue;
            foreach (BarcoJugador b in _miembros)
                if (b.VelocidadTotal < min) min = b.VelocidadTotal;
            return min;
        }
    }

    /// <summary>Suma de la fuerza de combate de todos los barcos miembros.</summary>
    public int FuerzaCombateTotal
    {
        get
        {
            int total = 0;
            foreach (BarcoJugador b in _miembros) total += b.FuerzaCombateTotal;
            return total;
        }
    }

    /// <summary>Suma de la capacidad de carga máxima de todos los barcos miembros.</summary>
    public int CargaMaximaTotal
    {
        get
        {
            int total = 0;
            foreach (BarcoJugador b in _miembros) total += b.CargaMaximaTotal;
            return total;
        }
    }

    // ─── Constructor ──────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un convoy con el barco indicado como líder.
    /// El líder se añade automáticamente a la lista de miembros.
    /// </summary>
    /// <param name="barcoLider">Barco que encabeza la formación.</param>
    public ConvoyData(BarcoJugador barcoLider)
    {
        BarcoLider   = barcoLider;
        NombreConvoy = barcoLider.Nombre;
        _miembros.Add(barcoLider);
    }

    // ─── Métodos públicos ─────────────────────────────────────────────────────

    /// <summary>
    /// Añade un barco al convoy. Falla si el barco ya es miembro, si es el líder
    /// o si ya hay 5 miembros (límite de la formación).
    /// </summary>
    /// <param name="barco">Barco a incorporar.</param>
    /// <returns><c>true</c> si se incorporó correctamente; <c>false</c> en caso contrario.</returns>
    public bool AñadirMiembro(BarcoJugador barco)
    {
        if (barco == null || _miembros.Count >= 5) return false;
        if (_miembros.Contains(barco)) return false;
        _miembros.Add(barco);
        return true;
    }

    /// <summary>
    /// Elimina un barco del convoy. El barco líder no puede abandonar la formación.
    /// </summary>
    /// <param name="barco">Barco a eliminar.</param>
    /// <returns><c>true</c> si se eliminó correctamente; <c>false</c> si era el líder o no estaba.</returns>
    public bool EliminarMiembro(BarcoJugador barco)
    {
        if (barco == null || barco == BarcoLider) return false;
        return _miembros.Remove(barco);
    }

    /// <summary>
    /// Convierte el convoy en un <see cref="FlotaRuntimeData"/> para que
    /// <see cref="CombateNavalResolver"/> pueda procesarlo igual que cualquier otra flota.
    /// La habilidad del capitán se obtiene del capitán asignado al barco líder.
    /// </summary>
    /// <returns>Instancia de <see cref="FlotaRuntimeData"/> con los stats agregados del convoy.</returns>
    public FlotaRuntimeData ComoFlotaRuntime()
    {
        float habilidadCapitan = TabernaManager.Instance
            ?.GetCapitanDeBarco(BarcoLider.IdBarco)
            ?.HabilidadCombate ?? 1f;

        var runtime = new FlotaRuntimeData(-1, NombreConvoy, esPirata: ModoPirata);
        runtime.VidaActual        = _miembros.Count * 100;
        runtime.FuerzaCanhones    = FuerzaCombateTotal;
        runtime.VelocidadFlota    = VelocidadConvoy;
        runtime.NumBarcos         = _miembros.Count;
        runtime.Tripulacion       = TripulacionTotal;
        runtime.HabilidadCapitan  = habilidadCapitan;
        return runtime;
    }
}
