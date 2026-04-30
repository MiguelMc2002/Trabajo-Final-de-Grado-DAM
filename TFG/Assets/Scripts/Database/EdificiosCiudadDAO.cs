using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Fila de la tabla EdificiosCiudad mapeada desde SQLite, enriquecida con el
/// nombre del tipo de edificio obtenido mediante JOIN con TipoEdificio.
/// </summary>
public class EdificiosCiudadDto
{
    /// <summary>Identificador de la ciudad a la que pertenece el edificio.</summary>
    public int IdCiudad { get; set; }

    /// <summary>Identificador del tipo de edificio (clave foránea a TipoEdificio).</summary>
    public int IdTipoEdificio { get; set; }

    /// <summary>Nombre legible del tipo de edificio (p. ej. "Mercado", "Granja").</summary>
    public string NombreTipoEdificio { get; set; }

    /// <summary>Número de edificios de este tipo presentes en la ciudad.</summary>
    public int Cantidad { get; set; }
}

/// <summary>
/// DAO que gestiona las tablas EdificiosCiudad y TipoEdificio. Los edificios de una
/// ciudad determinan su capacidad productiva: más granjas implican más producción de
/// materias primas, más astilleros aceleran la construcción naval, etc.
/// Al iniciar una partida nueva se insertan los tipos de edificio y los valores base
/// de cada ciudad; al cargar, se leen para reconstruir la producción diaria.
/// </summary>
public class EdificiosCiudadDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public EdificiosCiudadDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Inserta o sobreescribe la cantidad de un tipo de edificio en una ciudad.
    /// Usa INSERT OR REPLACE para que la operación sea idempotente.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <param name="idTipoEdificio">Identificador del tipo de edificio.</param>
    /// <param name="cantidad">Número de edificios de ese tipo en la ciudad.</param>
    public void InsertarEdificio(int idCiudad, int idTipoEdificio, int cantidad)
    {
        const string sql = @"
            INSERT OR REPLACE INTO EdificiosCiudad (id_ciudad, id_tipo_edificio, cantidad)
            VALUES (@idCiudad, @idTipo, @cantidad);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idCiudad",  idCiudad);
                cmd.Parameters.AddWithValue("@idTipo",    idTipoEdificio);
                cmd.Parameters.AddWithValue("@cantidad",  cantidad);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EdificiosCiudadDAO] Error al insertar edificio tipo={idTipoEdificio} en ciudad id={idCiudad}: {ex}");
        }
    }

    /// <summary>
    /// Inserta los edificios base de una ciudad con los valores de partida comunes
    /// a todas las ciudades en esta fase del desarrollo.
    /// La operación es segura de llamar varias veces gracias a INSERT OR REPLACE.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad a la que se asignan los edificios.</param>
    public void InsertarEdificiosInicialesCiudad(int idCiudad)
    {
        InsertarEdificio(idCiudad, 1, 1); // Mercado
        InsertarEdificio(idCiudad, 2, 1); // Astillero
        InsertarEdificio(idCiudad, 3, 1); // Taberna
        InsertarEdificio(idCiudad, 4, 2); // Almacén
        InsertarEdificio(idCiudad, 5, 3); // Granja
        InsertarEdificio(idCiudad, 6, 2); // Aserradero
    }

    /// <summary>
    /// Inserta los 6 tipos de edificio canónicos en la tabla TipoEdificio si todavía
    /// no existen. Debe llamarse antes de <see cref="InsertarEdificiosInicialesCiudad"/>
    /// para respetar la restricción de clave foránea.
    /// </summary>
    public void InsertarTiposEdificioSiNoExisten()
    {
        const string sql = "INSERT OR IGNORE INTO TipoEdificio (id_tipo_edificio, nombre) VALUES (@id, @nombre);";

        (int id, string nombre)[] tipos =
        {
            (1, "Mercado"),
            (2, "Astillero"),
            (3, "Taberna"),
            (4, "Almacén"),
            (5, "Granja"),
            (6, "Aserradero")
        };

        try
        {
            foreach (var (id, nombre) in tipos)
            {
                using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@id",     id);
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EdificiosCiudadDAO] Error al insertar los tipos de edificio: {ex}");
        }
    }

    /// <summary>
    /// Devuelve todos los edificios de una ciudad junto con el nombre de su tipo,
    /// obtenido mediante JOIN con la tabla TipoEdificio.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad a consultar.</param>
    /// <returns>
    /// Lista de <see cref="EdificiosCiudadDto"/> con una entrada por tipo de edificio
    /// presente en la ciudad. Vacía si no hay datos o si ocurre un error.
    /// </returns>
    public List<EdificiosCiudadDto> ObtenerEdificiosDeCiudad(int idCiudad)
    {
        var resultado = new List<EdificiosCiudadDto>();
        const string sql = @"
            SELECT ec.id_ciudad, ec.id_tipo_edificio, te.nombre, ec.cantidad
            FROM EdificiosCiudad ec
            JOIN TipoEdificio te ON te.id_tipo_edificio = ec.id_tipo_edificio
            WHERE ec.id_ciudad = @idCiudad;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idCiudad", idCiudad);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultado.Add(new EdificiosCiudadDto
                        {
                            IdCiudad          = reader.GetInt32(0),
                            IdTipoEdificio    = reader.GetInt32(1),
                            NombreTipoEdificio = reader.GetString(2),
                            Cantidad          = reader.GetInt32(3)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EdificiosCiudadDAO] Error al obtener los edificios de la ciudad id={idCiudad}: {ex}");
        }

        return resultado;
    }
}
