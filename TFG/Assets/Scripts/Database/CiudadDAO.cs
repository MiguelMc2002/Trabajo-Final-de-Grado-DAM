using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Fila de la tabla Ciudad mapeada desde SQLite.
/// Distinta de <see cref="CiudadData"/> (ScriptableObject del Inspector).
/// </summary>
public class CiudadDto
{
    /// <summary>Identificador único de la ciudad (clave primaria en BD).</summary>
    public int IdCiudad { get; set; }

    /// <summary>Nombre de la ciudad tal como aparece en la interfaz.</summary>
    public string Nombre { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla Ciudad. Permite insertar las ciudades al crear
/// una partida nueva y leerlas al cargar, para reconstruir el estado del mundo.
/// </summary>
public class CiudadDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public CiudadDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Inserta una ciudad en la tabla Ciudad. Usa INSERT OR IGNORE para que la
    /// operación sea idempotente: si la ciudad ya existe no lanza error.
    /// </summary>
    /// <param name="idCiudad">Identificador fijo de la ciudad.</param>
    /// <param name="nombre">Nombre de la ciudad.</param>
    public void InsertarCiudad(int idCiudad, string nombre)
    {
        const string sql = "INSERT OR IGNORE INTO Ciudad (id_ciudad, nombre) VALUES (@id, @nombre);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@id",     idCiudad);
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CiudadDAO] Error al insertar la ciudad '{nombre}' (id={idCiudad}): {ex}");
        }
    }

    /// <summary>
    /// Inserta las 6 ciudades canónicas del juego con sus IDs fijos.
    /// La operación es segura de llamar varias veces gracias a INSERT OR IGNORE.
    /// </summary>
    public void InsertarCiudadesIniciales()
    {
        InsertarCiudad(1, "Lübeck");
        InsertarCiudad(2, "Barcelona");
        InsertarCiudad(3, "Génova");
        InsertarCiudad(4, "Venecia");
        InsertarCiudad(5, "Ruan");
        InsertarCiudad(6, "Brujas");
    }

    /// <summary>
    /// Devuelve todas las ciudades almacenadas en la tabla Ciudad.
    /// </summary>
    /// <returns>
    /// Lista de <see cref="CiudadDto"/> con todas las ciudades. Vacía si la tabla
    /// no contiene filas o si ocurre un error.
    /// </returns>
    public List<CiudadDto> ObtenerTodasLasCiudades()
    {
        var ciudades = new List<CiudadDto>();
        const string sql = "SELECT id_ciudad, nombre FROM Ciudad;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ciudades.Add(new CiudadDto
                        {
                            IdCiudad = reader.GetInt32(0),
                            Nombre   = reader.GetString(1)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CiudadDAO] Error al obtener las ciudades: {ex}");
        }

        return ciudades;
    }
}
