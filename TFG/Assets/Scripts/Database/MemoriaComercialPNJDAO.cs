using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Registro de precio que un PNJ comerciante ha observado en el mercado de una ciudad.
/// El precio solo se considera válido durante los primeros 7 días desde que fue anotado;
/// pasado ese plazo, la información se trata como desactualizada.
/// </summary>
public class MemoriaComercialPNJDto
{
    /// <summary>Identificador de la flota PNJ propietaria de este recuerdo.</summary>
    public int IdFlota { get; set; }

    /// <summary>Identificador del bien cuyo precio fue memorizado.</summary>
    public int IdBien { get; set; }

    /// <summary>Precio observado en el mercado en el momento de la visita.</summary>
    public double PrecioConocido { get; set; }

    /// <summary>Día de juego en que se registró el precio.</summary>
    public int DiaJuegoConocido { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla MemoriaComercialPNJ. Representa el conocimiento de
/// mercado que acumulan los comerciantes PNJ: cada flota recuerda el último precio
/// que observó para cada bien, pero ese dato caduca a los 7 días de juego, lo que
/// obliga a los PNJ a visitar ciudades con regularidad para mantener información
/// comercial fiable.
/// </summary>
public class MemoriaComercialPNJDAO
{
    private readonly SqliteConnection _conexion;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public MemoriaComercialPNJDAO(DatabaseManager dbManager)
    {
        _conexion = dbManager.Conexion;
    }

    /// <summary>
    /// Constructor alternativo para tests de EditMode: recibe la conexión SQLite directamente,
    /// sin necesitar un MonoBehaviour de Unity. No debe usarse en código de juego.
    /// </summary>
    /// <param name="conexion">Conexión SQLite ya abierta (p. ej. una BD en memoria).</param>
    public MemoriaComercialPNJDAO(SqliteConnection conexion)
    {
        _conexion = conexion;
    }

    /// <summary>
    /// Registra o actualiza el precio que una flota PNJ ha observado para un bien.
    /// Si ya existía un registro previo para la misma flota y bien, se sobreescribe.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota que anota el precio.</param>
    /// <param name="idBien">Identificador del bien observado.</param>
    /// <param name="precioConocido">Precio de mercado observado en la visita.</param>
    /// <param name="diaJuegoActual">Día de juego actual en el momento de la observación.</param>
    public void GuardarMemoria(int idFlota, int idBien, double precioConocido, int diaJuegoActual)
    {
        const string sql = @"
            INSERT OR REPLACE INTO MemoriaComercialPNJ
                (id_flota, id_bien, precio_conocido, dia_juego_conocido)
            VALUES
                (@idFlota, @idBien, @precioConocido, @diaJuegoActual);";

        try
        {
            using (SqliteCommand cmd = _conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idFlota",        idFlota);
                cmd.Parameters.AddWithValue("@idBien",         idBien);
                cmd.Parameters.AddWithValue("@precioConocido", precioConocido);
                cmd.Parameters.AddWithValue("@diaJuegoActual", diaJuegoActual);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MemoriaComercialPNJDAO] Error al guardar memoria de la flota id={idFlota} para bien id={idBien}: {ex}");
        }
    }

    /// <summary>
    /// Devuelve toda la memoria comercial registrada para una flota, sin filtrar
    /// por antigüedad. Útil para inspeccionar o serializar el estado completo de
    /// un PNJ comerciante.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota a consultar.</param>
    /// <returns>
    /// Lista de <see cref="MemoriaComercialPNJDto"/> con un registro por bien conocido.
    /// Vacía si la flota no tiene memoria guardada o si ocurre un error.
    /// </returns>
    public List<MemoriaComercialPNJDto> ObtenerMemoriaDeFlota(int idFlota)
    {
        var resultado = new List<MemoriaComercialPNJDto>();
        const string sql = @"
            SELECT id_flota, id_bien, precio_conocido, dia_juego_conocido
            FROM MemoriaComercialPNJ
            WHERE id_flota = @idFlota;";

        try
        {
            using (SqliteCommand cmd = _conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idFlota", idFlota);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultado.Add(new MemoriaComercialPNJDto
                        {
                            IdFlota          = reader.GetInt32(0),
                            IdBien           = reader.GetInt32(1),
                            PrecioConocido   = reader.GetDouble(2),
                            DiaJuegoConocido = reader.GetInt32(3)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MemoriaComercialPNJDAO] Error al obtener la memoria de la flota id={idFlota}: {ex}");
        }

        return resultado;
    }

    /// <summary>
    /// Consulta el precio que una flota PNJ recuerda para un bien concreto,
    /// aplicando la regla de los 7 días: si han transcurrido 7 o más días desde
    /// que se anotó el precio, se considera información obsoleta y se devuelve
    /// <c>null</c>. Así los PNJ comerciantes actúan con datos de mercado retrasados,
    /// igual que ocurría en la Liga Hanseática real.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota que consulta su memoria.</param>
    /// <param name="idBien">Identificador del bien cuyo precio se busca.</param>
    /// <param name="diaActual">Día de juego actual para calcular la antigüedad del dato.</param>
    /// <returns>
    /// El precio memorizado si existe y tiene menos de 7 días de antigüedad;
    /// <c>null</c> si no existe registro o si el dato ha caducado.
    /// </returns>
    public double? ObtenerPrecioConocido(int idFlota, int idBien, int diaActual)
    {
        const string sql = @"
            SELECT precio_conocido, dia_juego_conocido
            FROM MemoriaComercialPNJ
            WHERE id_flota = @idFlota AND id_bien = @idBien
            LIMIT 1;";

        try
        {
            using (SqliteCommand cmd = _conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idFlota", idFlota);
                cmd.Parameters.AddWithValue("@idBien",  idBien);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        double precioConocido   = reader.GetDouble(0);
                        int    diaJuegoConocido = reader.GetInt32(1);

                        // El dato caduca a los 7 días: el PNJ ignora precios que
                        // observó hace una semana o más de juego.
                        if (diaActual - diaJuegoConocido < 7)
                            return precioConocido;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MemoriaComercialPNJDAO] Error al consultar precio conocido de flota id={idFlota} para bien id={idBien}: {ex}");
        }

        return null;
    }

    /// <summary>
    /// Elimina toda la memoria comercial de una flota. Se llama cuando el PNJ es
    /// destruido o su flota se disuelve, evitando que queden registros huérfanos
    /// en la base de datos.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota cuya memoria se borra por completo.</param>
    public void EliminarMemoriaDeFlota(int idFlota)
    {
        const string sql = "DELETE FROM MemoriaComercialPNJ WHERE id_flota = @idFlota;";

        try
        {
            using (SqliteCommand cmd = _conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@idFlota", idFlota);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MemoriaComercialPNJDAO] Error al eliminar la memoria de la flota id={idFlota}: {ex}");
        }
    }
}
