using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flota del jugador compuesta por hasta <see cref="MaxBarcos"/> barcos propios.
/// Agrega las estadísticas de todos los barcos y puede convertirse en
/// <see cref="FlotaRuntimeData"/> para que <see cref="CombateNavalResolver"/> la use.
/// Clase pura C# — no hereda de MonoBehaviour.
/// </summary>
public class FlotaJugador
{
    /// <summary>Número máximo de barcos que puede tener la flota del jugador.</summary>
    public const int MaxBarcos = 5;

    private readonly List<BarcoJugador> _barcos = new List<BarcoJugador>();

    /// <summary>Lista de barcos activos en la flota. Solo lectura desde fuera.</summary>
    public IReadOnlyList<BarcoJugador> Barcos => _barcos;

    /// <summary>
    /// Si <c>true</c>, el jugador actúa como pirata en el mapamundi:
    /// los comerciantes le huyen y puede atacarlos. Los puertos siguen siendo accesibles.
    /// </summary>
    public bool ModoPirata { get; set; }

    // ─── Propiedades agregadas ────────────────────────────────────────────────

    /// <summary>Suma de los puntos de vida totales de todos los barcos de la flota.</summary>
    public int VidaTotalFlota
    {
        get
        {
            int total = 0;
            foreach (BarcoJugador b in _barcos) total += b.VidaTotal;
            return total;
        }
    }

    /// <summary>
    /// Velocidad de la flota: el barco más lento determina la velocidad de toda la formación.
    /// Devuelve 0 si la flota está vacía.
    /// </summary>
    public float VelocidadFlota
    {
        get
        {
            if (_barcos.Count == 0) return 0f;
            float min = float.MaxValue;
            foreach (BarcoJugador b in _barcos)
                if (b.VelocidadTotal < min) min = b.VelocidadTotal;
            return min;
        }
    }

    /// <summary>Maniobrabilidad media de todos los barcos de la flota.</summary>
    public float ManiobrabilidadMedia
    {
        get
        {
            if (_barcos.Count == 0) return 0f;
            float suma = 0f;
            foreach (BarcoJugador b in _barcos) suma += b.ManiobrabilidadTotal;
            return suma / _barcos.Count;
        }
    }

    /// <summary>Suma de la capacidad de carga máxima de todos los barcos de la flota.</summary>
    public int CargaMaximaTotal
    {
        get
        {
            int total = 0;
            foreach (BarcoJugador b in _barcos) total += b.CargaMaximaTotal;
            return total;
        }
    }

    /// <summary>Suma de la fuerza de combate de todos los barcos de la flota.</summary>
    public int FuerzaCombateTotal
    {
        get
        {
            int total = 0;
            foreach (BarcoJugador b in _barcos) total += b.FuerzaCombateTotal;
            return total;
        }
    }

    /// <summary>Tripulación total: suma de la tripulación individual de cada barco.</summary>
    public int TripulacionTotal
    {
        get
        {
            int total = 0;
            foreach (BarcoJugador b in _barcos) total += b.Tripulacion;
            return total;
        }
    }

    // ─── Métodos públicos ─────────────────────────────────────────────────────

    /// <summary>
    /// Añade un barco a la flota si no se ha superado el límite de <see cref="MaxBarcos"/>.
    /// </summary>
    /// <param name="barco">Barco a incorporar.</param>
    /// <returns><c>true</c> si se añadió correctamente; <c>false</c> si la flota ya está llena.</returns>
    public bool AñadirBarco(BarcoJugador barco)
    {
        if (_barcos.Count >= MaxBarcos) return false;
        _barcos.Add(barco);
        return true;
    }

    /// <summary>
    /// Elimina el barco con el identificador indicado de la flota.
    /// </summary>
    /// <param name="idBarco">Identificador del barco a eliminar.</param>
    /// <returns><c>true</c> si se encontró y eliminó; <c>false</c> si no existe.</returns>
    public bool EliminarBarco(int idBarco)
    {
        BarcoJugador barco = ObtenerBarco(idBarco);
        if (barco == null) return false;
        _barcos.Remove(barco);
        return true;
    }

    /// <summary>
    /// Vacía la flota eliminando todos los barcos de la lista interna.
    /// Llamar exclusivamente desde <see cref="LoadManager"/> antes de restaurar
    /// los barcos desde la base de datos, para evitar duplicados en partidas cargadas.
    /// </summary>
    public void LimpiarTodos() => _barcos.Clear();

    /// <summary>
    /// Aplica el resultado de un combate naval a los barcos reales de la flota.
    /// Elimina los barcos perdidos (los últimos de la lista) y distribuye el daño
    /// restante entre los supervivientes, actualizando <see cref="BarcoJugador.VidaActual"/>.
    /// </summary>
    /// <param name="barcosPerdidos">Número de barcos a eliminar.</param>
    /// <param name="danioTotal">Daño total recibido por la flota en el combate.</param>
    public void AplicarDanioCombate(int barcosPerdidos, float danioTotal)
    {
        // Eliminar barcos hundidos del final de la lista
        for (int i = 0; i < barcosPerdidos && _barcos.Count > 0; i++)
            _barcos.RemoveAt(_barcos.Count - 1);

        // Distribuir el daño restante entre los supervivientes
        if (_barcos.Count > 0 && danioTotal > 0f)
        {
            float danioPerBarco = danioTotal / _barcos.Count;
            foreach (BarcoJugador b in _barcos)
                b.VidaActual = Mathf.Max(0, Mathf.RoundToInt(b.VidaActual - danioPerBarco));
        }
    }

    /// <summary>
    /// Devuelve el barco con el identificador indicado.
    /// </summary>
    /// <param name="idBarco">Identificador del barco a buscar.</param>
    /// <returns>El <see cref="BarcoJugador"/> encontrado, o <c>null</c> si no existe.</returns>
    public BarcoJugador ObtenerBarco(int idBarco)
    {
        foreach (BarcoJugador b in _barcos)
            if (b.IdBarco == idBarco) return b;
        return null;
    }

    /// <summary>
    /// Convierte la flota del jugador en un <see cref="FlotaRuntimeData"/> para que
    /// <see cref="CombateNavalResolver"/> pueda procesarla igual que cualquier otra flota.
    /// Usa id=-1 y nombre="Jugador".
    /// Nota: FuerzaCanhones y VelocidadFlota no tienen setter público en FlotaRuntimeData;
    /// se usan los valores por defecto del constructor hasta que se añadan dichos setters.
    /// </summary>
    /// <returns>Instancia de <see cref="FlotaRuntimeData"/> con los stats agregados de la flota.</returns>
    /// <summary>
    /// Convierte la flota del jugador a FlotaRuntimeData para el sistema de combate.
    /// Los stats se calculan en tiempo real desde los barcos actuales:
    /// - VidaActual/VidaMax: suma de vida de todos los barcos
    /// - FuerzaCanhones: suma de FuerzaCombateTotal de todos los barcos
    /// - VelocidadFlota: MIN de VelocidadTotal (el más lento limita)
    /// - NumBarcos: número de barcos operativos
    /// - Tripulacion: suma de tripulación de todos los barcos
    /// Si la flota está vacía usa valores por defecto mínimos para evitar
    /// división por cero en CombateNavalResolver.
    /// </summary>
    public FlotaRuntimeData ComoFlotaRuntime()
    {
        var runtime = new FlotaRuntimeData(-1, "Jugador", ModoPirata);
        if (Barcos == null || Barcos.Count == 0)
            return runtime; // valores por defecto del constructor

        float vidaTotal    = 0f;
        float vidaActual   = 0f;
        float fuerzaTotal  = 0f;
        float velocidadMin = float.MaxValue;
        int   tripulacion  = 0;

        foreach (BarcoJugador barco in Barcos)
        {
            vidaTotal    += barco.VidaTotal;
            vidaActual   += barco.VidaActual;
            fuerzaTotal  += barco.FuerzaCombateTotal;
            tripulacion  += barco.Tripulacion;
            if (barco.VelocidadTotal < velocidadMin)
                velocidadMin = barco.VelocidadTotal;
        }

        runtime.VidaMax        = vidaTotal;
        runtime.VidaActual     = vidaActual;
        runtime.FuerzaCanhones = fuerzaTotal;
        runtime.VelocidadFlota = velocidadMin == float.MaxValue ? 3f : velocidadMin;
        runtime.NumBarcos      = Barcos.Count;
        runtime.Tripulacion    = tripulacion;
        // Poblar la lista de barcos individuales para que PanelInspeccionFlota pueda mostrarlos
        runtime.BarcosFlota    = new List<BarcoJugador>(_barcos);
        return runtime;
    }
}
