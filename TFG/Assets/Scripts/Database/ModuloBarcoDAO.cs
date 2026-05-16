using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// DTO que representa una fila de la tabla ModuloBarco leída desde SQLite.
/// </summary>
public class ModuloDto
{
    /// <summary>Identificador único del módulo en la tabla ModuloBarco.</summary>
    public int    IdModulo    { get; set; }

    /// <summary>Tipo de módulo como cadena de texto: "Armamento", "Velas" o "Bodega".</summary>
    public string TipoModulo  { get; set; }

    /// <summary>Nombre del módulo, coincide con <see cref="ModuloBarcoData.nombreModulo"/>.</summary>
    public string NombreModulo { get; set; }

    /// <summary>Primer valor auxiliar. Almacena <see cref="ModuloBarcoData.costeOro"/>.</summary>
    public int    ValorA       { get; set; }

    /// <summary>Segundo valor auxiliar. Almacena <see cref="ModuloBarcoData.slotsCosto"/>.</summary>
    public int    ValorB       { get; set; }
}

/// <summary>
/// DAO que gestiona la tabla ModuloBarco. Persiste y restaura los módulos instalados
/// en cada barco del jugador, borrando y reinsertando la lista completa en cada guardado
/// para garantizar consistencia sin rastros de módulos desinstalados.
/// </summary>
public class ModuloBarcoDAO
{
    private readonly DatabaseManager _dbManager;

    /// <summary>
    /// Crea una nueva instancia del DAO vinculada al gestor de base de datos activo.
    /// </summary>
    /// <param name="dbManager">Gestor que expone la conexión SQLite de la partida.</param>
    public ModuloBarcoDAO(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Borra todos los módulos del barco indicado y los reinserta a partir de la lista actual.
    /// El identificador de cada módulo se genera como <c>idBarco * 100 + índice</c>.
    /// </summary>
    /// <param name="idBarco">Identificador del barco cuyos módulos se persisten.</param>
    /// <param name="modulos">Lista de <see cref="ModuloBarcoData"/> actualmente instalados.</param>
    public void GuardarModulosDeBarco(int idBarco, IReadOnlyList<ModuloBarcoData> modulos)
    {
        try
        {
            // Borrar módulos anteriores del barco
            using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM ModuloBarco WHERE id_barco = @idBarco;";
                cmd.Parameters.AddWithValue("@idBarco", idBarco);
                cmd.ExecuteNonQuery();
            }

            if (modulos == null || modulos.Count == 0) return;

            const string sqlInsert = @"
                INSERT OR REPLACE INTO ModuloBarco
                    (id_modulo_barco, id_barco, tipo_modulo, nombre_modulo, valor_a, valor_b)
                VALUES
                    (@idModulo, @idBarco, @tipo, @nombre, @valorA, @valorB);";

            for (int i = 0; i < modulos.Count; i++)
            {
                ModuloBarcoData m = modulos[i];
                using (SqliteCommand cmd = _dbManager.Conexion.CreateCommand())
                {
                    cmd.CommandText = sqlInsert;
                    cmd.Parameters.AddWithValue("@idModulo", idBarco * 100 + i);
                    cmd.Parameters.AddWithValue("@idBarco",  idBarco);
                    cmd.Parameters.AddWithValue("@tipo",     m.tipoModulo.ToString());
                    cmd.Parameters.AddWithValue("@nombre",   m.nombreModulo);
                    cmd.Parameters.AddWithValue("@valorA",   m.costeOro);
                    cmd.Parameters.AddWithValue("@valorB",   m.slotsCosto);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModuloBarcoDAO] Error al guardar módulos del barco id={idBarco}: {ex}");
        }
    }

    /// <summary>
    /// Devuelve todos los módulos persistidos para el barco indicado.
    /// </summary>
    /// <param name="idBarco">Identificador del barco cuyos módulos se quieren obtener.</param>
    /// <returns>Lista de <see cref="ModuloDto"/>. Vacía si el barco no tiene módulos guardados.</returns>
    public List<ModuloDto> CargarModulosDeBarco(int idBarco)
    {
        var resultado = new List<ModuloDto>();
        const string sql = @"
            SELECT id_modulo_barco, tipo_modulo, nombre_modulo, valor_a, valor_b
            FROM ModuloBarco
            WHERE id_barco = @idBarco;";

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
                        resultado.Add(new ModuloDto
                        {
                            IdModulo    = reader.GetInt32(0),
                            TipoModulo  = reader.GetString(1),
                            NombreModulo = reader.GetString(2),
                            ValorA      = reader.GetInt32(3),
                            ValorB      = reader.GetInt32(4)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModuloBarcoDAO] Error al cargar módulos del barco id={idBarco}: {ex}");
        }

        return resultado;
    }
}
