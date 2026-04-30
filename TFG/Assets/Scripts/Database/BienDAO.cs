using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Fila de la tabla Bien mapeada desde SQLite.
/// Distinta de <see cref="BienDto"/> (ScriptableObject del Inspector).
/// </summary>
public class BienDto
{
    /// <summary>Identificador único del bien (clave primaria en BD).</summary>
    public int IdBien { get; set; }

    /// <summary>Nombre del bien tal como aparece en el mercado.</summary>
    public string Nombre { get; set; }

    /// <summary>Categoría del bien: 'primario', 'intermedio' o 'avanzado'.</summary>
    public string Categoria { get; set; }

    /// <summary>Precio de referencia usado en la fórmula de precios reactivos.</summary>
    public decimal PrecioBase { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla Bien. Los bienes son las mercancías del juego.
/// Al iniciar una partida nueva se insertan todos los bienes con sus precios base.
/// Al cargar una partida se leen para inicializar los mercados de cada ciudad.
/// </summary>
public class BienDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public BienDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Inserta un bien en la tabla Bien. Usa INSERT OR IGNORE para que la
    /// operación sea idempotente: si el bien ya existe no lanza error.
    /// </summary>
    /// <param name="idBien">Identificador fijo del bien.</param>
    /// <param name="nombre">Nombre del bien.</param>
    /// <param name="categoria">Categoría: 'primario', 'intermedio' o 'avanzado'.</param>
    /// <param name="precioBase">Precio base usado en la fórmula de precios reactivos.</param>
    public void InsertarBien(int idBien, string nombre, string categoria, decimal precioBase)
    {
        const string sql = @"
            INSERT OR IGNORE INTO Bien (id_bien, nombre, categoria, precio_base)
            VALUES (@id, @nombre, @categoria, @precio);";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@id",        idBien);
                cmd.Parameters.AddWithValue("@nombre",    nombre);
                cmd.Parameters.AddWithValue("@categoria", categoria);
                cmd.Parameters.AddWithValue("@precio",    (double)precioBase);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BienDAO] Error al insertar el bien '{nombre}' (id={idBien}): {ex}");
        }
    }

    /// <summary>
    /// Inserta los 19 bienes canónicos del juego con sus IDs y precios base fijos.
    /// La operación es segura de llamar varias veces gracias a INSERT OR IGNORE.
    /// </summary>
    public void InsertarBienesIniciales()
    {
        // Primarios
        InsertarBien(1,  "Grano",             "primario",    1.0m);
        InsertarBien(2,  "Carne",             "primario",    2.5m);
        InsertarBien(3,  "Pescado",           "primario",    1.5m);
        InsertarBien(4,  "Madera",            "primario",    1.2m);
        InsertarBien(5,  "Lana",              "primario",    1.8m);
        InsertarBien(6,  "Mineral de hierro", "primario",    2.0m);
        InsertarBien(7,  "Brea",              "primario",    1.3m);
        InsertarBien(8,  "Uvas",              "primario",    1.6m);
        InsertarBien(9,  "Ladrillos",         "primario",    0.8m);

        // Intermedios
        InsertarBien(10, "Harina",            "intermedio",  2.0m);
        InsertarBien(11, "Cerveza",           "intermedio",  2.5m);
        InsertarBien(12, "Tela",              "intermedio",  3.5m);
        InsertarBien(13, "Lingotes de hierro","intermedio",  4.0m);
        InsertarBien(14, "Vino",              "intermedio",  5.0m);

        // Avanzados
        InsertarBien(15, "Pan",               "avanzado",    3.0m);
        InsertarBien(16, "Ropa",              "avanzado",    6.0m);
        InsertarBien(17, "Herramientas",      "avanzado",    7.0m);
        InsertarBien(18, "Armas",             "avanzado",   10.0m);
        InsertarBien(19, "Secciones de barco","avanzado",   15.0m);
    }

    /// <summary>
    /// Devuelve todos los bienes almacenados en la tabla Bien.
    /// </summary>
    /// <returns>
    /// Lista de <see cref="BienDto"/> con todos los bienes. Vacía si la tabla
    /// no contiene filas o si ocurre un error.
    /// </returns>
    public List<BienDto> ObtenerTodosLosBienes()
    {
        var bienes = new List<BienDto>();
        const string sql = "SELECT id_bien, nombre, categoria, precio_base FROM Bien;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bienes.Add(new BienDto
                        {
                            IdBien     = reader.GetInt32(0),
                            Nombre     = reader.GetString(1),
                            Categoria  = reader.GetString(2),
                            PrecioBase = (decimal)reader.GetDouble(3)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BienDAO] Error al obtener todos los bienes: {ex}");
        }

        return bienes;
    }

    /// <summary>
    /// Busca un bien concreto por su identificador.
    /// </summary>
    /// <param name="idBien">Identificador del bien a buscar.</param>
    /// <returns>
    /// El <see cref="BienDto"/> correspondiente, o <c>null</c> si no existe
    /// ningún bien con ese identificador o si ocurre un error.
    /// </returns>
    public BienDto ObtenerBienPorId(int idBien)
    {
        const string sql = "SELECT id_bien, nombre, categoria, precio_base FROM Bien WHERE id_bien = @id;";

        try
        {
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@id", idBien);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new BienDto
                    {
                        IdBien     = reader.GetInt32(0),
                        Nombre     = reader.GetString(1),
                        Categoria  = reader.GetString(2),
                        PrecioBase = (decimal)reader.GetDouble(3)
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BienDAO] Error al obtener el bien con id={idBien}: {ex}");
            return null;
        }
    }
}
