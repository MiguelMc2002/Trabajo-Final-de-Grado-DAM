using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Fila de la tabla CargaBarco mapeada desde SQLite, enriquecida con el nombre
/// del bien obtenido mediante JOIN con la tabla Bien.
/// </summary>
public class CargaBarcoDto
{
    /// <summary>Identificador del barco que transporta la mercancía.</summary>
    public int IdBarco { get; set; }

    /// <summary>Identificador del bien almacenado en la bodega.</summary>
    public int IdBien { get; set; }

    /// <summary>Nombre legible del bien (p. ej. "Grano", "Madera").</summary>
    public string NombreBien { get; set; }

    /// <summary>Unidades de ese bien que lleva el barco en este momento.</summary>
    public int Cantidad { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla CargaBarco. Conecta cada barco con los bienes que
/// transporta en su bodega. Sin este DAO toda la mercancía embarcada se perdería
/// al cerrar el juego, ya que no quedaría persistida en la BD.
/// </summary>
public class CargaBarcoDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public CargaBarcoDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Inserta o sobreescribe la cantidad de un bien concreto en la bodega de un barco.
    /// Usa INSERT OR REPLACE para que la operación sea idempotente.
    /// </summary>
    /// <param name="idBarco">Identificador del barco.</param>
    /// <param name="idBien">Identificador del bien.</param>
    /// <param name="cantidad">Unidades del bien almacenadas en la bodega.</param>
    public void GuardarCarga(int idBarco, int idBien, int cantidad)
    {
        const string sql = @"
            INSERT OR REPLACE INTO CargaBarco (id_barco, id_bien, cantidad)
            VALUES (@idBarco, @idBien, @cantidad);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idBarco",  idBarco);
                cmd.Parameters.AddWithValue("@idBien",   idBien);
                cmd.Parameters.AddWithValue("@cantidad", cantidad);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CargaBarcoDAO] Error al guardar la carga del bien id={idBien} en barco id={idBarco}: {ex}");
        }
    }

    /// <summary>
    /// Elimina toda la carga registrada para un barco. Se llama antes de guardar
    /// la carga actualizada para evitar que queden filas huérfanas de bienes que
    /// el jugador ya no transporta.
    /// </summary>
    /// <param name="idBarco">Identificador del barco cuya carga se limpia.</param>
    public void EliminarCargaDeBarco(int idBarco)
    {
        const string sql = "DELETE FROM CargaBarco WHERE id_barco = @idBarco;";

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
            Debug.LogError($"[CargaBarcoDAO] Error al eliminar la carga del barco id={idBarco}: {ex}");
        }
    }

    /// <summary>
    /// Persiste la bodega completa de un barco de forma atómica: primero elimina la
    /// carga anterior con <see cref="EliminarCargaDeBarco"/> y luego inserta cada
    /// entrada del diccionario con <see cref="GuardarCarga"/>.
    /// Solo se insertan las entradas con cantidad mayor que cero.
    /// </summary>
    /// <param name="idBarco">Identificador del barco cuya bodega se persiste.</param>
    /// <param name="cargaPorIdBien">
    /// Diccionario donde la clave es el <c>idBien</c> y el valor es la cantidad
    /// de unidades que el barco lleva de ese bien.
    /// </param>
    public void GuardarCargaCompleta(int idBarco, Dictionary<int, int> cargaPorIdBien)
    {
        if (cargaPorIdBien == null)
        {
            Debug.LogError($"[CargaBarcoDAO] GuardarCargaCompleta: el diccionario de carga es null para barco id={idBarco}.");
            return;
        }

        EliminarCargaDeBarco(idBarco);

        foreach (KeyValuePair<int, int> entrada in cargaPorIdBien)
        {
            if (entrada.Value > 0)
                GuardarCarga(idBarco, entrada.Key, entrada.Value);
        }
    }

    /// <summary>
    /// Devuelve la carga completa de un barco con el nombre de cada bien,
    /// obtenido mediante JOIN con la tabla Bien.
    /// </summary>
    /// <param name="idBarco">Identificador del barco a consultar.</param>
    /// <returns>
    /// Lista de <see cref="CargaBarcoDto"/> con una entrada por bien embarcado.
    /// Vacía si el barco no lleva mercancía o si ocurre un error.
    /// </returns>
    public List<CargaBarcoDto> ObtenerCargaDeBarco(int idBarco)
    {
        var resultado = new List<CargaBarcoDto>();
        const string sql = @"
            SELECT cb.id_barco, cb.id_bien, b.nombre, cb.cantidad
            FROM CargaBarco cb
            JOIN Bien b ON b.id_bien = cb.id_bien
            WHERE cb.id_barco = @idBarco;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idBarco", idBarco);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultado.Add(new CargaBarcoDto
                        {
                            IdBarco    = reader.GetInt32(0),
                            IdBien     = reader.GetInt32(1),
                            NombreBien = reader.GetString(2),
                            Cantidad   = reader.GetInt32(3)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CargaBarcoDAO] Error al obtener la carga del barco id={idBarco}: {ex}");
        }

        return resultado;
    }
}
