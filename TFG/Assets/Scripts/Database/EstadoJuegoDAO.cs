using System;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Contiene el estado global de la partida leído desde la tabla estadoJuego.
/// </summary>
public class EstadoJuegoData
{
    /// <summary>Día del calendario del juego en el momento del guardado.</summary>
    public int DiaJuego { get; set; }

    /// <summary>Mes del calendario del juego en el momento del guardado.</summary>
    public int MesJuego { get; set; }

    /// <summary>Año del calendario del juego en el momento del guardado.</summary>
    public int AñoJuego { get; set; }

    /// <summary>Multiplicador de velocidad del tiempo activo (25, 100, 200 ó 1000 en centésimas).</summary>
    public int VelocidadTiempo { get; set; }

    /// <summary>Fecha y hora UTC en que se guardó la partida.</summary>
    public DateTime FechaGuardado { get; set; }

    /// <summary>Monedas de oro del jugador en el momento del guardado.</summary>
    public long DineroJugador { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla estadoJuego. Es lo primero que se escribe al guardar
/// una partida y lo primero que se lee al cargarla, ya que contiene la fecha de
/// juego y la velocidad de simulación necesarias para reconstruir el mundo.
/// La tabla tiene siempre como máximo una fila con id_estado = 1.
/// </summary>
public class EstadoJuegoDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public EstadoJuegoDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Escribe (o sobreescribe) el estado global de la partida en estadoJuego.
    /// Usa INSERT OR REPLACE para garantizar que solo existe la fila con id_estado = 1.
    /// La fecha de guardado se toma automáticamente como DateTime.UtcNow.
    /// </summary>
    /// <param name="diaJuego">Día del calendario del juego.</param>
    /// <param name="mesJuego">Mes del calendario del juego.</param>
    /// <param name="añoJuego">Año del calendario del juego.</param>
    /// <param name="velocidadTiempo">Multiplicador de velocidad del tiempo activo.</param>
    /// <param name="dineroJugador">Monedas de oro del jugador en el momento del guardado.</param>
    public void Guardar(int diaJuego, int mesJuego, int añoJuego, int velocidadTiempo, long dineroJugador = 999_999_999L)
    {
        const string sql = @"
            INSERT OR REPLACE INTO estadoJuego
                (id_estado, dia_juego, mes_juego, año_juego, velocidad_tiempo, fecha_guardado, dinero_jugador)
            VALUES
                (1, @dia, @mes, @año, @velocidad, @fecha, @dinero);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@dia",       diaJuego);
                cmd.Parameters.AddWithValue("@mes",       mesJuego);
                cmd.Parameters.AddWithValue("@año",       añoJuego);
                cmd.Parameters.AddWithValue("@velocidad", velocidadTiempo);
                cmd.Parameters.AddWithValue("@fecha",     DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("@dinero",    dineroJugador);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EstadoJuegoDAO] Error al guardar el estado de juego: {ex}");
        }
    }

    /// <summary>
    /// Lee el estado global de la partida desde estadoJuego (fila con id_estado = 1).
    /// </summary>
    /// <returns>
    /// Un <see cref="EstadoJuegoData"/> con los valores guardados, o <c>null</c>
    /// si todavía no existe ninguna fila (partida nueva antes del primer guardado).
    /// </returns>
    public EstadoJuegoData Cargar()
    {
        const string sql = "SELECT dia_juego, mes_juego, año_juego, velocidad_tiempo, fecha_guardado, dinero_jugador FROM estadoJuego WHERE id_estado = 1;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    // dinero_jugador puede faltar en BDs muy antiguas; usar default seguro
                    long dinero = 999_999_999L;
                    try { dinero = reader.GetInt64(5); }
                    catch (Exception) { /* columna ausente en BD antigua */ }

                    return new EstadoJuegoData
                    {
                        DiaJuego        = reader.GetInt32(0),
                        MesJuego        = reader.GetInt32(1),
                        AñoJuego        = reader.GetInt32(2),
                        VelocidadTiempo = reader.GetInt32(3),
                        FechaGuardado   = DateTime.Parse(reader.GetString(4)),
                        DineroJugador   = dinero
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EstadoJuegoDAO] Error al cargar el estado de juego: {ex}");
            return null;
        }
    }
}
