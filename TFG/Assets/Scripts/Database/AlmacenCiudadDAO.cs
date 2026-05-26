using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// DAO que gestiona la tabla AlmacenCiudadJugador. Registra qué bienes y en qué
/// cantidad ha dejado el jugador almacenados en cada ciudad, de forma que persistan
/// entre sesiones sin ocupar espacio en la bodega del barco.
/// </summary>
public class AlmacenCiudadDAO
{
    private readonly DatabaseManager _dbManager;
    private readonly SqliteConnection _conexionDirecta;

    private SqliteConnection Conexion => _conexionDirecta ?? _dbManager.Conexion;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public AlmacenCiudadDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Constructor secundario para tests automatizados.
    /// Permite instanciar el DAO con una conexión SQLite directa sin depender de DatabaseManager.
    /// </summary>
    /// <param name="conexion">Conexión SQLite ya abierta.</param>
    public AlmacenCiudadDAO(SqliteConnection conexion)
    {
        _conexionDirecta = conexion;
        _dbManager = null;
    }

    /// <summary>
    /// Devuelve las unidades almacenadas del bien indicado en la ciudad.
    /// Retorna 0 si no existe la fila.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <param name="idBien">Identificador del bien.</param>
    /// <returns>Cantidad almacenada, o 0 si no hay registro.</returns>
    public int GetCantidad(int idCiudad, int idBien)
    {
        const string sql = @"
            SELECT cantidad FROM AlmacenCiudadJugador
            WHERE id_ciudad = @idCiudad AND id_bien = @idBien;";

        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idCiudad", idCiudad);
                cmd.Parameters.AddWithValue("@idBien",   idBien);

                object resultado = cmd.ExecuteScalar();
                return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AlmacenCiudadDAO] Error al obtener cantidad (ciudad={idCiudad}, bien={idBien}): {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Establece la cantidad exacta de un bien en el almacén de una ciudad.
    /// Usa INSERT OR REPLACE para crear la fila si no existía o actualizarla si ya existía.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <param name="idBien">Identificador del bien.</param>
    /// <param name="cantidad">Cantidad a almacenar. Debe ser mayor o igual a 0.</param>
    public void SetCantidad(int idCiudad, int idBien, int cantidad)
    {
        const string sql = @"
            INSERT OR REPLACE INTO AlmacenCiudadJugador (id_ciudad, id_bien, cantidad)
            VALUES (@idCiudad, @idBien, @cantidad);";

        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idCiudad", idCiudad);
                cmd.Parameters.AddWithValue("@idBien",   idBien);
                cmd.Parameters.AddWithValue("@cantidad", cantidad);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AlmacenCiudadDAO] Error al establecer cantidad (ciudad={idCiudad}, bien={idBien}): {ex}");
        }
    }

    /// <summary>
    /// Suma <paramref name="delta"/> a la cantidad actual del bien en la ciudad.
    /// El delta puede ser negativo para restar unidades.
    /// Lanza <see cref="InvalidOperationException"/> si el resultado sería menor que 0.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <param name="idBien">Identificador del bien.</param>
    /// <param name="delta">Unidades a añadir (positivo) o retirar (negativo).</param>
    /// <exception cref="InvalidOperationException">
    /// Si la cantidad resultante sería negativa (el jugador no tiene suficiente stock).
    /// </exception>
    public void Incrementar(int idCiudad, int idBien, int delta)
    {
        int actual = GetCantidad(idCiudad, idBien);
        int nuevo  = actual + delta;

        if (nuevo < 0)
            throw new InvalidOperationException(
                $"[AlmacenCiudadDAO] Stock insuficiente en ciudad={idCiudad}, bien={idBien}: actual={actual}, delta={delta}.");

        SetCantidad(idCiudad, idBien, nuevo);
    }

    /// <summary>
    /// Devuelve todos los bienes con cantidad mayor que 0 almacenados en la ciudad.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <returns>
    /// Diccionario con <c>id_bien → cantidad</c>. Vacío si no hay stock; nunca devuelve <c>null</c>.
    /// </returns>
    public Dictionary<int, int> GetTodosPorCiudad(int idCiudad)
    {
        var resultado = new Dictionary<int, int>();
        const string sql = @"
            SELECT id_bien, cantidad FROM AlmacenCiudadJugador
            WHERE id_ciudad = @idCiudad AND cantidad > 0;";

        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idCiudad", idCiudad);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idBien   = reader.GetInt32(0);
                        int cantidad = reader.GetInt32(1);
                        resultado[idBien] = cantidad;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AlmacenCiudadDAO] Error al obtener almacén de ciudad={idCiudad}: {ex}");
        }

        return resultado;
    }

    /// <summary>
    /// Elimina todas las filas del almacén correspondientes a la ciudad indicada.
    /// Se usa al abandonar una ciudad o al resetear su estado comercial.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad cuyo almacén se vacía.</param>
    public void LimpiarCiudad(int idCiudad)
    {
        const string sql = "DELETE FROM AlmacenCiudadJugador WHERE id_ciudad = @idCiudad;";

        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idCiudad", idCiudad);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AlmacenCiudadDAO] Error al limpiar almacén de ciudad={idCiudad}: {ex}");
        }
    }
}
