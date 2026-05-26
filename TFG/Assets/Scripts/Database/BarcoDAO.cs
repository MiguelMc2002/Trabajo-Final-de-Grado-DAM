using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Fila de la tabla Barco mapeada desde SQLite.
/// Contiene el estado completo de un barco individual: casco, vida, tripulación y flota asignada.
/// </summary>
public class BarcoDto
{
    /// <summary>Identificador único del barco (clave primaria en BD).</summary>
    public int IdBarco { get; set; }

    /// <summary>Identificador del tipo de casco base del barco (FK a TipoCasco).</summary>
    public int IdTipoCasco { get; set; }

    /// <summary>Nombre propio del barco asignado por el jugador o generado para los PNJ.</summary>
    public string NombreBarco { get; set; }

    /// <summary>
    /// Indica si el barco es una unidad de combate activa dentro de su flota.
    /// Los barcos de carga no participan directamente en el combate naval.
    /// </summary>
    public bool EsBarcosCombate { get; set; }

    /// <summary>Puntos de vida actuales del barco. Se reduce al recibir daño en combate.</summary>
    public int VidaActual { get; set; }

    /// <summary>Número de tripulantes embarcados en este momento.</summary>
    public int TripulacionActual { get; set; }

    /// <summary>Capacidad máxima de tripulación que admite el casco.</summary>
    public int CapacidadTripulacion { get; set; }

    /// <summary>
    /// Identificador de la flota a la que pertenece el barco, o <c>null</c> si
    /// está en astillero o sin asignar.
    /// </summary>
    public int? IdFlota { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla Barco. Permite persistir y recuperar el estado individual
/// de cada barco — vida, tripulación y flota — de modo que al cargar una partida
/// el estado de combate se restaure exactamente como estaba al guardar.
/// También gestiona la tabla TipoCasco, de la que Barco depende por FK.
/// </summary>
public class BarcoDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public BarcoDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Inserta o sobreescribe un barco completo en la tabla Barco.
    /// Usa INSERT OR REPLACE para que la operación sea idempotente.
    /// </summary>
    /// <param name="idBarco">Identificador único del barco.</param>
    /// <param name="idTipoCasco">Tipo de casco base del barco (FK a TipoCasco).</param>
    /// <param name="nombreBarco">Nombre propio del barco.</param>
    /// <param name="esBarcosCombate">Si el barco participa activamente en el combate naval.</param>
    /// <param name="vidaActual">Puntos de vida actuales.</param>
    /// <param name="tripulacionActual">Tripulantes embarcados en este momento.</param>
    /// <param name="capacidadTripulacion">Capacidad máxima de tripulación del casco.</param>
    /// <param name="idFlota">Flota a la que pertenece, o <c>null</c> si no está asignado.</param>
    public void InsertarBarco(int idBarco, int idTipoCasco, string nombreBarco, bool esBarcosCombate,
                              int vidaActual, int tripulacionActual, int capacidadTripulacion,
                              int? idFlota)
    {
        const string sql = @"
            INSERT OR REPLACE INTO Barco
                (id_barco, id_tipo_casco, nombre_barco, es_barco_combate,
                 vida_actual, tripulacion_actual, capacidad_tripulacion, id_flota)
            VALUES
                (@idBarco, @idTipoCasco, @nombre, @esCombate,
                 @vida, @tripulacion, @capacidad, @idFlota);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idBarco",     idBarco);
                cmd.Parameters.AddWithValue("@idTipoCasco", idTipoCasco);
                cmd.Parameters.AddWithValue("@nombre",      nombreBarco);
                cmd.Parameters.AddWithValue("@esCombate",   esBarcosCombate ? 1 : 0);
                cmd.Parameters.AddWithValue("@vida",        vidaActual);
                cmd.Parameters.AddWithValue("@tripulacion", tripulacionActual);
                cmd.Parameters.AddWithValue("@capacidad",   capacidadTripulacion);
                cmd.Parameters.AddWithValue("@idFlota",     (object)idFlota ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BarcoDAO] Error al insertar el barco id={idBarco}: {ex}");
        }
    }

    /// <summary>
    /// Actualiza el estado dinámico de un barco existente: vida, tripulación y flota asignada.
    /// No modifica el tipo de casco ni el nombre.
    /// </summary>
    /// <param name="idBarco">Identificador del barco a actualizar.</param>
    /// <param name="vidaActual">Nuevos puntos de vida (puede haber recibido daño en combate).</param>
    /// <param name="tripulacionActual">Número actual de tripulantes.</param>
    /// <param name="idFlota">Nueva flota asignada, o <c>null</c> si se desvincula de su flota.</param>
    public void ActualizarEstadoBarco(int idBarco, int vidaActual, int tripulacionActual, int? idFlota)
    {
        const string sql = @"
            UPDATE Barco
            SET vida_actual        = @vida,
                tripulacion_actual = @tripulacion,
                id_flota           = @idFlota
            WHERE id_barco = @idBarco;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@vida",        vidaActual);
                cmd.Parameters.AddWithValue("@tripulacion", tripulacionActual);
                cmd.Parameters.AddWithValue("@idFlota",     (object)idFlota ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@idBarco",     idBarco);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BarcoDAO] Error al actualizar el estado del barco id={idBarco}: {ex}");
        }
    }

    /// <summary>
    /// Devuelve todos los barcos que pertenecen a una flota concreta.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota cuyos barcos se quieren obtener.</param>
    /// <returns>
    /// Lista de <see cref="BarcoDto"/> con todos los barcos de la flota. Vacía si la flota
    /// no tiene barcos asignados o si ocurre un error.
    /// </returns>
    public List<BarcoDto> ObtenerBarcosDeFlota(int idFlota)
    {
        var barcos = new List<BarcoDto>();
        const string sql = @"
            SELECT id_barco, id_tipo_casco, nombre_barco, es_barco_combate,
                   vida_actual, tripulacion_actual, capacidad_tripulacion, id_flota
            FROM Barco
            WHERE id_flota = @idFlota;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idFlota", idFlota);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        barcos.Add(LeerBarcoDto(reader));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BarcoDAO] Error al obtener los barcos de la flota id={idFlota}: {ex}");
        }

        return barcos;
    }

    /// <summary>
    /// Elimina un barco de la tabla Barco. Se usa cuando el barco es hundido en combate
    /// y debe desaparecer permanentemente del mundo.
    /// </summary>
    /// <param name="idBarco">Identificador del barco a eliminar.</param>
    public void EliminarBarco(int idBarco)
    {
        const string sql = "DELETE FROM Barco WHERE id_barco = @idBarco;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idBarco", idBarco);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BarcoDAO] Error al eliminar el barco id={idBarco}: {ex}");
        }
    }

    /// <summary>
    /// Borra todos los barcos asignados a la flota indicada y sus módulos asociados.
    /// Elimina primero los registros de ModuloBarco para respetar la restricción de FK
    /// que apunta a Barco. Usar antes de reescribir la flota completa al guardar.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota cuyos barcos se eliminan (0 = jugador).</param>
    public void EliminarBarcosDeFlota(int idFlota)
    {
        try
        {
            // Borrar módulos antes que barcos por la FK ModuloBarco → Barco
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = @"
                    DELETE FROM ModuloBarco
                    WHERE id_barco IN (SELECT id_barco FROM Barco WHERE id_flota = @idFlota);";
                cmd.Parameters.AddWithValue("@idFlota", idFlota);
                cmd.ExecuteNonQuery();
            }

            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Barco WHERE id_flota = @idFlota;";
                cmd.Parameters.AddWithValue("@idFlota", idFlota);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BarcoDAO] Error al eliminar barcos de flota id={idFlota}: {ex}");
        }
    }

    /// <summary>
    /// Inserta los 3 tipos de casco base en la tabla TipoCasco si todavía no existen.
    /// Debe llamarse antes de insertar barcos para respetar la restricción de FK.
    /// La operación es segura de llamar varias veces gracias a INSERT OR IGNORE.
    /// </summary>
    // TODO: en el futuro los valores deberían leerse de los assets IBarco (CascoCog, CascoHulk, etc.)
    //       en vez de estar hardcodeados aquí, para que exista una única fuente de verdad.
    public void InsertarTiposCascoSiNoExisten()
    {
        const string sql = @"
            INSERT OR IGNORE INTO TipoCasco
                (id_tipo_casco, nombre, vida_base, velocidad_base, maniobrabilidad_base, capacidad_carga_base)
            VALUES
                (@id, @nombre, @vida, @velocidad, @maniobrabilidad, @carga);";

        (int id, string nombre, int vida, int velocidad, int maniobra, int carga)[] tipos =
        {
            (1, "Cog",     100, 3, 2, 200),
            (2, "Hulk",    150, 2, 1, 350),
            (3, "Carraca", 200, 2, 2, 300),
            (4, "Galera",   80, 5, 4, 150),
        };

        try
        {
            foreach (var t in tipos)
            {
                using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@id",             t.id);
                    cmd.Parameters.AddWithValue("@nombre",         t.nombre);
                    cmd.Parameters.AddWithValue("@vida",           t.vida);
                    cmd.Parameters.AddWithValue("@velocidad",      t.velocidad);
                    cmd.Parameters.AddWithValue("@maniobrabilidad",t.maniobra);
                    cmd.Parameters.AddWithValue("@carga",          t.carga);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BarcoDAO] Error al insertar los tipos de casco: {ex}");
        }
    }

    // ─── Auxiliares privados ──────────────────────────────────────────────────

    /// <summary>
    /// Convierte la fila actual del reader en un <see cref="BarcoDto"/>.
    /// Comprueba con <c>IsDBNull</c> la columna nullable <c>id_flota</c> antes de leerla.
    /// </summary>
    /// <param name="reader">Reader posicionado en la fila a leer.</param>
    /// <returns>El <see cref="BarcoDto"/> construido a partir de la fila.</returns>
    private BarcoDto LeerBarcoDto(SqliteDataReader reader)
    {
        return new BarcoDto
        {
            IdBarco              = reader.GetInt32(0),
            IdTipoCasco          = reader.GetInt32(1),
            NombreBarco          = reader.GetString(2),
            EsBarcosCombate      = reader.GetInt32(3) == 1,
            VidaActual           = reader.GetInt32(4),
            TripulacionActual    = reader.GetInt32(5),
            CapacidadTripulacion = reader.GetInt32(6),
            IdFlota              = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7)
        };
    }
}
