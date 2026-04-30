using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Fila de la tabla Flota mapeada desde SQLite.
/// Representa el estado de una flota en el mapamundi: posición, propietario,
/// estado de movimiento y ciudad de destino si está navegando.
/// </summary>
public class FlotaDto
{
    /// <summary>Identificador único de la flota (clave primaria en BD).</summary>
    public int IdFlota { get; set; }

    /// <summary>
    /// Tipo de propietario de la flota.
    /// Valores posibles: 'jugador', 'comerciante', 'pirata', 'neutral'.
    /// </summary>
    public string TipoPropietario { get; set; }

    /// <summary>
    /// Ciudad en la que está atracada la flota, o <c>null</c> si está navegando
    /// en mar abierto sin puerto asignado.
    /// </summary>
    public int? IdCiudadActual { get; set; }

    /// <summary>Coordenada X de la flota en el mapamundi.</summary>
    public float PosicionX { get; set; }

    /// <summary>Coordenada Y de la flota en el mapamundi.</summary>
    public float PosicionY { get; set; }

    /// <summary>
    /// Identificador del capitán asignado a esta flota, o <c>null</c> si no
    /// tiene capitán (flota neutral o sin asignar).
    /// </summary>
    public int? IdCapitan { get; set; }

    /// <summary>
    /// Estado de movimiento actual de la flota.
    /// Valores posibles: 'EnPuerto', 'Navegando', 'EnCombate', 'Huyendo'.
    /// </summary>
    public string EstadoActual { get; set; }

    /// <summary>
    /// Ciudad hacia la que se dirige la flota, o <c>null</c> si no tiene
    /// destino activo (está en puerto o a la espera).
    /// </summary>
    public int? IdCiudadDestino { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla Flota. Permite persistir y recuperar el estado
/// de todas las flotas del mapamundi — jugador, comerciantes PNJ, piratas y
/// neutrales — para que al cargar una partida cada flota retome exactamente
/// la posición y el estado que tenía al guardar.
/// </summary>
public class FlotaDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public FlotaDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Inserta o sobreescribe una flota completa en la tabla Flota.
    /// Usa INSERT OR REPLACE para que la operación sea idempotente.
    /// Los parámetros nullable se insertan como NULL en la BD cuando son <c>null</c>.
    /// </summary>
    /// <param name="idFlota">Identificador único de la flota.</param>
    /// <param name="tipoPropietario">Tipo de propietario: 'jugador', 'comerciante', 'pirata' o 'neutral'.</param>
    /// <param name="idCiudadActual">Ciudad en la que está atracada, o <c>null</c> si navega en mar abierto.</param>
    /// <param name="posX">Coordenada X en el mapamundi.</param>
    /// <param name="posY">Coordenada Y en el mapamundi.</param>
    /// <param name="idCapitan">Identificador del capitán asignado, o <c>null</c> si no tiene.</param>
    /// <param name="estadoActual">Estado de movimiento actual de la flota.</param>
    /// <param name="idCiudadDestino">Ciudad de destino si está navegando, o <c>null</c> si no tiene destino activo.</param>
    public void InsertarFlota(int idFlota, string tipoPropietario, int? idCiudadActual,
                              float posX, float posY, int? idCapitan,
                              string estadoActual, int? idCiudadDestino)
    {
        const string sql = @"
            INSERT OR REPLACE INTO Flota
                (id_flota, tipo_propietario, id_ciudad_actual, posicion_x, posicion_y,
                 id_capitan, estado_actual, id_ciudad_destino)
            VALUES
                (@idFlota, @tipo, @ciudadActual, @posX, @posY,
                 @capitan, @estado, @ciudadDestino);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idFlota",       idFlota);
                cmd.Parameters.AddWithValue("@tipo",          tipoPropietario);
                cmd.Parameters.AddWithValue("@ciudadActual",  (object)idCiudadActual  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@posX",          posX);
                cmd.Parameters.AddWithValue("@posY",          posY);
                cmd.Parameters.AddWithValue("@capitan",       (object)idCapitan       ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado",        estadoActual);
                cmd.Parameters.AddWithValue("@ciudadDestino", (object)idCiudadDestino ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlotaDAO] Error al insertar la flota id={idFlota}: {ex}");
        }
    }

    /// <summary>
    /// Actualiza la posición y el estado de movimiento de una flota existente.
    /// Solo modifica los campos de navegación; no toca <c>tipo_propietario</c>
    /// ni <c>id_capitan</c>.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota a actualizar.</param>
    /// <param name="posX">Nueva coordenada X en el mapamundi.</param>
    /// <param name="posY">Nueva coordenada Y en el mapamundi.</param>
    /// <param name="estadoActual">Nuevo estado de movimiento.</param>
    /// <param name="idCiudadActual">Ciudad en la que está atracada, o <c>null</c> si está en ruta.</param>
    /// <param name="idCiudadDestino">Ciudad de destino activa, o <c>null</c> si no tiene destino.</param>
    public void ActualizarPosicionFlota(int idFlota, float posX, float posY,
                                        string estadoActual, int? idCiudadActual,
                                        int? idCiudadDestino)
    {
        const string sql = @"
            UPDATE Flota
            SET posicion_x        = @posX,
                posicion_y        = @posY,
                estado_actual     = @estado,
                id_ciudad_actual  = @ciudadActual,
                id_ciudad_destino = @ciudadDestino
            WHERE id_flota = @idFlota;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@posX",          posX);
                cmd.Parameters.AddWithValue("@posY",          posY);
                cmd.Parameters.AddWithValue("@estado",        estadoActual);
                cmd.Parameters.AddWithValue("@ciudadActual",  (object)idCiudadActual  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ciudadDestino", (object)idCiudadDestino ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@idFlota",       idFlota);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlotaDAO] Error al actualizar la posición de la flota id={idFlota}: {ex}");
        }
    }

    /// <summary>
    /// Devuelve todas las flotas almacenadas en la tabla Flota.
    /// </summary>
    /// <returns>
    /// Lista de <see cref="FlotaDto"/> con todas las flotas. Vacía si no hay
    /// datos o si ocurre un error.
    /// </returns>
    public List<FlotaDto> ObtenerTodasLasFlotas()
    {
        var flotas = new List<FlotaDto>();
        const string sql = @"
            SELECT id_flota, tipo_propietario, id_ciudad_actual, posicion_x, posicion_y,
                   id_capitan, estado_actual, id_ciudad_destino
            FROM Flota;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        flotas.Add(LeerFlotaDto(reader));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlotaDAO] Error al obtener todas las flotas: {ex}");
        }

        return flotas;
    }

    /// <summary>
    /// Devuelve todas las flotas de un tipo de propietario concreto.
    /// </summary>
    /// <param name="tipoPropietario">Tipo a filtrar: 'jugador', 'comerciante', 'pirata' o 'neutral'.</param>
    /// <returns>
    /// Lista de <see cref="FlotaDto"/> que coinciden con el tipo indicado. Vacía si no
    /// hay resultados o si ocurre un error.
    /// </returns>
    public List<FlotaDto> ObtenerFlotasPorTipo(string tipoPropietario)
    {
        var flotas = new List<FlotaDto>();
        const string sql = @"
            SELECT id_flota, tipo_propietario, id_ciudad_actual, posicion_x, posicion_y,
                   id_capitan, estado_actual, id_ciudad_destino
            FROM Flota
            WHERE tipo_propietario = @tipo;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@tipo", tipoPropietario);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        flotas.Add(LeerFlotaDto(reader));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlotaDAO] Error al obtener las flotas de tipo '{tipoPropietario}': {ex}");
        }

        return flotas;
    }

    /// <summary>
    /// Elimina una flota de la tabla Flota. Se usa cuando la flota es destruida
    /// en combate y debe desaparecer permanentemente del mundo.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota a eliminar.</param>
    public void EliminarFlota(int idFlota)
    {
        const string sql = "DELETE FROM Flota WHERE id_flota = @idFlota;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idFlota", idFlota);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlotaDAO] Error al eliminar la flota id={idFlota}: {ex}");
        }
    }

    // ─── Auxiliares privados ──────────────────────────────────────────────────

    /// <summary>
    /// Convierte la fila actual del reader en un <see cref="FlotaDto"/>.
    /// Comprueba con <c>IsDBNull</c> cada columna nullable antes de leerla.
    /// </summary>
    /// <param name="reader">Reader posicionado en la fila a leer.</param>
    /// <returns>El <see cref="FlotaDto"/> construido a partir de la fila.</returns>
    private FlotaDto LeerFlotaDto(SqliteDataReader reader)
    {
        return new FlotaDto
        {
            IdFlota          = reader.GetInt32(0),
            TipoPropietario  = reader.GetString(1),
            IdCiudadActual   = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
            PosicionX        = reader.GetFloat(3),
            PosicionY        = reader.GetFloat(4),
            IdCapitan        = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
            EstadoActual     = reader.GetString(6),
            IdCiudadDestino  = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7)
        };
    }
}
