using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// DTO que representa una fila de la tabla Capitan leída desde SQLite.
/// </summary>
public class CapitanDto
{
    /// <summary>Identificador único del capitán (clave primaria en BD).</summary>
    public int    IdCapitan           { get; set; }

    /// <summary>Nombre propio del capitán.</summary>
    public string Nombre              { get; set; }

    /// <summary>Indica si el capitán está asignado a un barco del jugador.</summary>
    public bool   Asignado            { get; set; }

    /// <summary>Habilidad de navegación guardada (0.0 – 5.0).</summary>
    public float  HabilidadNavegacion { get; set; }

    /// <summary>Habilidad de combate guardada (0.0 – 5.0).</summary>
    public float  HabilidadCombate    { get; set; }

    /// <summary>Identificador del barco al que está asignado. -1 si no asignado.</summary>
    public int    IdBarcoAsignado     { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla Capitan. Persiste y restaura los capitanes contratados
/// por el jugador, incluyendo sus habilidades y el barco al que están asignados.
/// Ejecuta migraciones automáticas al construirse para añadir columnas ausentes en
/// bases de datos creadas antes de esta versión.
/// </summary>
public class CapitanDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO y migra las columnas necesarias si no existen.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public CapitanDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
        MigrarColumnasCapitan();
        MigrarColumnaIdBarcoAsignado();
    }

    /// <summary>
    /// Inserta o sobreescribe un capitán en la tabla Capitan con sus habilidades y barco asignado.
    /// </summary>
    /// <param name="capitan">Datos del capitán a persistir.</param>
    /// <param name="idBarcoAsignado">Identificador del barco asignado, o -1 si libre.</param>
    public void GuardarCapitan(CapitanData capitan, int idBarcoAsignado)
    {
        const string sql = @"
            INSERT OR REPLACE INTO Capitan
                (id_capitan, nombre, asignado, habilidad_navegacion, habilidad_combate, id_barco_asignado)
            VALUES
                (@id, @nombre, @asignado, @nav, @combate, @idBarco);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@id",       capitan.IdCapitan);
                cmd.Parameters.AddWithValue("@nombre",   capitan.Nombre);
                cmd.Parameters.AddWithValue("@asignado", idBarcoAsignado > 0 ? 1 : 0);
                cmd.Parameters.AddWithValue("@nav",      capitan.HabilidadNavegacion);
                cmd.Parameters.AddWithValue("@combate",  capitan.HabilidadCombate);
                cmd.Parameters.AddWithValue("@idBarco",  idBarcoAsignado);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CapitanDAO] Error al guardar capitán id={capitan.IdCapitan}: {ex}");
        }
    }

    /// <summary>
    /// Devuelve todos los capitanes almacenados en la tabla Capitan.
    /// </summary>
    /// <returns>Lista de <see cref="CapitanDto"/>. Vacía si no hay capitanes guardados.</returns>
    public List<CapitanDto> CargarTodosLosCapitanes()
    {
        var resultado = new List<CapitanDto>();
        const string sql = @"
            SELECT id_capitan, nombre, asignado, habilidad_navegacion, habilidad_combate, id_barco_asignado
            FROM Capitan;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultado.Add(new CapitanDto
                        {
                            IdCapitan           = reader.GetInt32(0),
                            Nombre              = reader.GetString(1),
                            Asignado            = reader.GetInt32(2) == 1,
                            HabilidadNavegacion = reader.GetFloat(3),
                            HabilidadCombate    = reader.GetFloat(4),
                            IdBarcoAsignado     = reader.IsDBNull(5) ? -1 : reader.GetInt32(5)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CapitanDAO] Error al cargar capitanes: {ex}");
        }

        return resultado;
    }

    /// <summary>
    /// Borra todos los capitanes de la tabla. Se llama antes de reinsertarlos al guardar.
    /// </summary>
    public void EliminarTodosLosCapitanes()
    {
        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Capitan;";
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CapitanDAO] Error al eliminar capitanes: {ex}");
        }
    }

    // ─── Migraciones ─────────────────────────────────────────────────────────

    private void MigrarColumnasCapitan()
    {
        EjecutarAlterTable("ALTER TABLE Capitan ADD COLUMN habilidad_navegacion REAL NOT NULL DEFAULT 1.0;", "habilidad_navegacion");
        EjecutarAlterTable("ALTER TABLE Capitan ADD COLUMN habilidad_combate    REAL NOT NULL DEFAULT 1.0;", "habilidad_combate");
    }

    private void MigrarColumnaIdBarcoAsignado()
    {
        EjecutarAlterTable("ALTER TABLE Capitan ADD COLUMN id_barco_asignado INTEGER NOT NULL DEFAULT -1;", "id_barco_asignado");
    }

    private void EjecutarAlterTable(string sql, string columna)
    {
        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception)
        {
            Debug.Log($"[CapitanDAO] Columna '{columna}' ya existe (BD nueva o ya migrada).");
        }
    }
}
