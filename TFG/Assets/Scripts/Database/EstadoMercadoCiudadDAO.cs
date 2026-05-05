using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Fila de la tabla EstadoMercadoCiudad mapeada desde SQLite.
/// Contiene el estado económico completo de un bien en una ciudad concreta.
/// </summary>
public class EstadoMercadoDto
{
    /// <summary>Identificador de la ciudad a la que pertenece esta entrada.</summary>
    public int IdCiudad { get; set; }

    /// <summary>Identificador del bien al que corresponde esta entrada.</summary>
    public int IdBien { get; set; }

    /// <summary>Unidades disponibles del bien en el mercado de la ciudad.</summary>
    public int Stock { get; set; }

    /// <summary>Unidades que la ciudad genera de este bien cada día de juego.</summary>
    public int Produccion { get; set; }

    /// <summary>Unidades que la ciudad consume de este bien cada día de juego.</summary>
    public int Consumo { get; set; }

    /// <summary>Precio calculado en tiempo de ejecución según la fórmula de oferta y demanda.</summary>
    public decimal PrecioActual { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla EstadoMercadoCiudad. Es la tabla más importante del módulo
/// de guardado: almacena el estado económico completo de cada bien en cada ciudad.
/// Sin este DAO el estado del mercado se pierde al cerrar el juego.
/// </summary>
public class EstadoMercadoCiudadDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public EstadoMercadoCiudadDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Escribe (o sobreescribe) el estado de un bien concreto en el mercado de una ciudad.
    /// Usa INSERT OR REPLACE para que la operación sea idempotente.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <param name="idBien">Identificador del bien.</param>
    /// <param name="stock">Stock actual del bien en la ciudad.</param>
    /// <param name="produccion">Producción diaria del bien en la ciudad.</param>
    /// <param name="consumo">Consumo diario del bien en la ciudad.</param>
    /// <param name="precioActual">Precio calculado en tiempo de ejecución.</param>
    public void GuardarEstadoMercado(int idCiudad, int idBien, int stock, int produccion, int consumo, decimal precioActual)
    {
        const string sql = @"
            INSERT OR REPLACE INTO EstadoMercadoCiudad
                (id_ciudad, id_bien, stock, produccion, consumo, precio_actual)
            VALUES
                (@idCiudad, @idBien, @stock, @produccion, @consumo, @precio);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idCiudad",   idCiudad);
                cmd.Parameters.AddWithValue("@idBien",     idBien);
                cmd.Parameters.AddWithValue("@stock",      stock);
                cmd.Parameters.AddWithValue("@produccion", produccion);
                cmd.Parameters.AddWithValue("@consumo",    consumo);
                cmd.Parameters.AddWithValue("@precio",     (double)precioActual);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EstadoMercadoCiudadDAO] Error al guardar estado del bien id={idBien} en ciudad id={idCiudad}: {ex}");
        }
    }

    /// <summary>
    /// Guarda el estado completo del mercado de una ciudad iterando la lista de entradas
    /// procedente de <see cref="GameManager.MercadosPorCiudad"/>. Por cada bien busca su id
    /// en la tabla Bien usando el nombre del <see cref="BienData"/> asociado.
    /// Las entradas cuyo bien no se encuentre en la BD o no tengan <see cref="BienData"/> asignado
    /// se omiten con un aviso en consola.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad cuyo mercado se persiste. Debe ser mayor que 0.</param>
    /// <param name="entradas">Lista de entradas del mercado obtenida del estado central de la partida.</param>
    public void GuardarTodoElMercado(int idCiudad, IReadOnlyList<EntradaMercado> entradas)
    {
        if (entradas == null || entradas.Count == 0)
        {
            Debug.LogWarning($"[EstadoMercadoCiudadDAO] GuardarTodoElMercado: lista de entradas vacía o null para ciudad {idCiudad}; se omite.");
            return;
        }

        if (idCiudad <= 0)
        {
            Debug.LogError($"[EstadoMercadoCiudadDAO] GuardarTodoElMercado: idCiudad inválido ({idCiudad}).");
            return;
        }

        foreach (EntradaMercado entrada in entradas)
        {
            if (entrada.Bien == null)
            {
                Debug.LogWarning("[EstadoMercadoCiudadDAO] Entrada de mercado sin BienData asignado; se omite.");
                continue;
            }

            int idBien = ObtenerIdBienPorNombre(entrada.Bien.nombre);
            if (idBien == -1)
            {
                Debug.LogWarning($"[EstadoMercadoCiudadDAO] No se encontró en la BD el bien '{entrada.Bien.nombre}'; se omite.");
                continue;
            }

            GuardarEstadoMercado(
                idCiudad,
                idBien,
                entrada.StockActual,
                entrada.ProduccionDiaria,
                entrada.ConsumoDiario,
                (decimal)entrada.PrecioActual
            );
        }
    }

    /// <summary>
    /// Carga el estado de mercado de todos los bienes de una ciudad desde la BD.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad a cargar.</param>
    /// <returns>
    /// Lista de <see cref="EstadoMercadoDto"/> con una entrada por bien. Vacía si la
    /// ciudad no tiene datos guardados o si ocurre un error.
    /// </returns>
    public List<EstadoMercadoDto> CargarEstadoMercado(int idCiudad)
    {
        var resultado = new List<EstadoMercadoDto>();
        const string sql = @"
            SELECT id_ciudad, id_bien, stock, produccion, consumo, precio_actual
            FROM EstadoMercadoCiudad
            WHERE id_ciudad = @idCiudad;";

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
                        resultado.Add(new EstadoMercadoDto
                        {
                            IdCiudad    = reader.GetInt32(0),
                            IdBien      = reader.GetInt32(1),
                            Stock       = reader.GetInt32(2),
                            Produccion  = reader.GetInt32(3),
                            Consumo     = reader.GetInt32(4),
                            PrecioActual = (decimal)reader.GetDouble(5)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EstadoMercadoCiudadDAO] Error al cargar el mercado de la ciudad id={idCiudad}: {ex}");
        }

        return resultado;
    }

    /// <summary>
    /// Devuelve los identificadores de todas las ciudades que tienen al menos una fila
    /// en la tabla EstadoMercadoCiudad. Lo usa <see cref="LoadManager"/> para iterar
    /// todos los mercados guardados sin depender de ningún sistema en escena.
    /// Nunca devuelve <c>null</c>; devuelve lista vacía si la tabla está vacía.
    /// </summary>
    /// <returns>Lista de <c>id_ciudad</c> distintos ordenados de forma ascendente.</returns>
    public List<int> ObtenerIdsCiudadesConMercado()
    {
        var resultado = new List<int>();
        const string sql = "SELECT DISTINCT id_ciudad FROM EstadoMercadoCiudad ORDER BY id_ciudad;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        resultado.Add(reader.GetInt32(0));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EstadoMercadoCiudadDAO] Error al obtener IDs de ciudades con mercado: {ex}");
        }

        return resultado;
    }

    // ─── Auxiliares privados ──────────────────────────────────────────────────

    /// <summary>
    /// Busca el identificador de un bien en la tabla Bien por su nombre exacto.
    /// </summary>
    /// <param name="nombre">Nombre del bien tal como está almacenado en la BD.</param>
    /// <returns>El id_bien correspondiente, o -1 si no existe ninguna fila con ese nombre.</returns>
    private int ObtenerIdBienPorNombre(string nombre)
    {
        const string sql = "SELECT id_bien FROM Bien WHERE nombre = @nombre LIMIT 1;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@nombre", nombre);

                object resultado = cmd.ExecuteScalar();
                if (resultado == null || resultado == DBNull.Value)
                    return -1;

                return Convert.ToInt32(resultado);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EstadoMercadoCiudadDAO] Error al buscar el id del bien '{nombre}': {ex}");
            return -1;
        }
    }
}
