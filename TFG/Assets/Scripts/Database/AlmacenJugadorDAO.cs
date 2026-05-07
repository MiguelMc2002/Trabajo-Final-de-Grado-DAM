using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// DAO que gestiona la tabla AlmacenJugador. Persiste y restaura el inventario
/// personal del jugador —los bienes que lleva en su bodega— entre sesiones de juego.
/// Sigue el mismo patrón atómico de borrar-e-insertar que <see cref="CargaBarcoDAO"/>
/// para evitar filas huérfanas de bienes que el jugador ya no posee.
/// </summary>
public class AlmacenJugadorDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public AlmacenJugadorDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Persiste el inventario completo del jugador de forma atómica: primero elimina
    /// todas las filas existentes con <see cref="LimpiarAlmacen"/> y luego inserta
    /// solo las entradas con cantidad mayor que cero.
    /// </summary>
    /// <param name="almacenPorIdBien">
    /// Diccionario donde la clave es el <c>id_bien</c> y el valor es la cantidad
    /// de unidades que el jugador posee de ese bien.
    /// </param>
    public void GuardarAlmacen(Dictionary<int, int> almacenPorIdBien)
    {
        if (almacenPorIdBien == null)
        {
            Debug.LogError("[AlmacenJugadorDAO] GuardarAlmacen: el diccionario de almacén es null.");
            return;
        }

        LimpiarAlmacen();

        const string sql = @"
            INSERT OR REPLACE INTO AlmacenJugador (id_bien, cantidad)
            VALUES (@idBien, @cantidad);";

        foreach (KeyValuePair<int, int> entrada in almacenPorIdBien)
        {
            if (entrada.Value <= 0) continue;

            try
            {
                using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@idBien",   entrada.Key);
                    cmd.Parameters.AddWithValue("@cantidad", entrada.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AlmacenJugadorDAO] Error al guardar bien id={entrada.Key}: {ex}");
            }
        }
    }

    /// <summary>
    /// Lee el inventario completo del jugador desde la tabla AlmacenJugador.
    /// </summary>
    /// <returns>
    /// Diccionario con <c>id_bien → cantidad</c>. Vacío si el almacén no tiene
    /// datos o si ocurre un error; nunca devuelve <c>null</c>.
    /// </returns>
    public Dictionary<int, int> ObtenerAlmacen()
    {
        var resultado = new Dictionary<int, int>();
        const string sql = "SELECT id_bien, cantidad FROM AlmacenJugador;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;

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
            Debug.LogError($"[AlmacenJugadorDAO] Error al obtener el almacén del jugador: {ex}");
        }

        return resultado;
    }

    /// <summary>
    /// Elimina todas las filas de la tabla AlmacenJugador.
    /// Se llama antes de guardar el estado actualizado del inventario.
    /// </summary>
    public void LimpiarAlmacen()
    {
        const string sql = "DELETE FROM AlmacenJugador;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AlmacenJugadorDAO] Error al limpiar el almacén del jugador: {ex}");
        }
    }
}
