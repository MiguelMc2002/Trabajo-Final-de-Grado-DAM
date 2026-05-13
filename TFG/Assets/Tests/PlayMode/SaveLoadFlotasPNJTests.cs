using System;
using System.Collections.Generic;
using System.IO;
using Mono.Data.Sqlite;
using NUnit.Framework;

// ─── DTO local ────────────────────────────────────────────────────────────────

/// <summary>
/// Copia mínima de FlotaRuntimeData definida localmente para que el test
/// no dependa de Assembly-CSharp. Solo contiene los campos necesarios para
/// verificar la persistencia en FlotaPNJ y CargaFlotaPNJ.
/// </summary>
internal class FlotaRuntimeDataLocal
{
    public int                  Id                { get; }
    public string               NombrePropietario { get; }
    public int                  CiudadOrigenId    { get; set; }
    public int                  CiudadDestinoId   { get; set; }
    public string               EstadoActual      { get; set; }
    public float                PosicionX         { get; set; }
    public float                PosicionY         { get; set; }
    public int                  CasillaDestinoX   { get; set; }
    public int                  CasillaDestinoY   { get; set; }
    public int                  CasillaDestinoZ   { get; set; }
    public Dictionary<int, int> Carga             { get; set; }

    public FlotaRuntimeDataLocal(int id, string nombre)
    {
        Id                = id;
        NombrePropietario = nombre;
        EstadoActual      = "EnPuerto";
        Carga             = new Dictionary<int, int>();
    }
}

// ─── Tests ────────────────────────────────────────────────────────────────────

/// <summary>
/// Tests de Play Mode para la persistencia de flotas PNJ en FlotaPNJ y CargaFlotaPNJ.
/// Completamente autónomos: usan un fichero .db temporal y operaciones SQL directas.
/// No dependen de GameManager, FlotaManager, FlotaRuntimeData ni MonoBehaviours activos en escena.
/// </summary>
[TestFixture]
public class SaveLoadFlotasPNJTests
{
    private string           _dbPath;
    private SqliteConnection _conexion;

    /// <summary>Crea un fichero .db temporal único y abre la conexión antes de cada test.</summary>
    [SetUp]
    public void SetUp()
    {
        _dbPath   = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        _conexion = new SqliteConnection($"URI=file:{_dbPath}");
        _conexion.Open();
        CrearTablas(_conexion);
    }

    /// <summary>Cierra la conexión y borra el fichero .db temporal tras cada test.</summary>
    [TearDown]
    public void TearDown()
    {
        _conexion?.Close();
        _conexion?.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private void CrearTablas(SqliteConnection con)
    {
        using (SqliteCommand cmd = con.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS FlotaPNJ (
                    id                 INTEGER PRIMARY KEY,
                    nombre_propietario TEXT NOT NULL,
                    ciudad_origen_id   INTEGER NOT NULL DEFAULT -1,
                    ciudad_destino_id  INTEGER NOT NULL DEFAULT -1,
                    estado             TEXT NOT NULL DEFAULT 'EnPuerto',
                    posicion_actual_x  REAL NOT NULL DEFAULT 0,
                    posicion_actual_y  REAL NOT NULL DEFAULT 0,
                    casilla_destino_x  INTEGER NOT NULL DEFAULT 0,
                    casilla_destino_y  INTEGER NOT NULL DEFAULT 0,
                    casilla_destino_z  INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS CargaFlotaPNJ (
                    id_flota  INTEGER NOT NULL,
                    id_bien   INTEGER NOT NULL,
                    cantidad  INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (id_flota, id_bien)
                );";
            cmd.ExecuteNonQuery();
        }
    }

    private void InsertarFlota(int id, string nombre, int origenId, int destinoId,
                               string estado, float posX, float posY,
                               int cDestX, int cDestY, int cDestZ)
    {
        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO FlotaPNJ
                    (id, nombre_propietario, ciudad_origen_id, ciudad_destino_id, estado,
                     posicion_actual_x, posicion_actual_y,
                     casilla_destino_x, casilla_destino_y, casilla_destino_z)
                VALUES
                    (@id, @nombre, @origen, @destino, @estado,
                     @posX, @posY,
                     @cDestX, @cDestY, @cDestZ);";
            cmd.Parameters.AddWithValue("@id",      id);
            cmd.Parameters.AddWithValue("@nombre",  nombre);
            cmd.Parameters.AddWithValue("@origen",  origenId);
            cmd.Parameters.AddWithValue("@destino", destinoId);
            cmd.Parameters.AddWithValue("@estado",  estado);
            cmd.Parameters.AddWithValue("@posX",    posX);
            cmd.Parameters.AddWithValue("@posY",    posY);
            cmd.Parameters.AddWithValue("@cDestX",  cDestX);
            cmd.Parameters.AddWithValue("@cDestY",  cDestY);
            cmd.Parameters.AddWithValue("@cDestZ",  cDestZ);
            cmd.ExecuteNonQuery();
        }
    }

    private void InsertarCarga(int idFlota, int idBien, int cantidad)
    {
        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO CargaFlotaPNJ (id_flota, id_bien, cantidad)
                VALUES (@idFlota, @idBien, @cantidad);";
            cmd.Parameters.AddWithValue("@idFlota",  idFlota);
            cmd.Parameters.AddWithValue("@idBien",   idBien);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            cmd.ExecuteNonQuery();
        }
    }

    private FlotaRuntimeDataLocal LeerFlota(int id)
    {
        FlotaRuntimeDataLocal flota = null;

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT id, nombre_propietario, ciudad_origen_id, ciudad_destino_id, estado,
                       posicion_actual_x, posicion_actual_y,
                       casilla_destino_x, casilla_destino_y, casilla_destino_z
                FROM FlotaPNJ WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);

            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    flota = new FlotaRuntimeDataLocal(reader.GetInt32(0), reader.GetString(1))
                    {
                        CiudadOrigenId  = reader.GetInt32(2),
                        CiudadDestinoId = reader.GetInt32(3),
                        EstadoActual    = reader.GetString(4),
                        PosicionX       = reader.GetFloat(5),
                        PosicionY       = reader.GetFloat(6),
                        CasillaDestinoX = reader.GetInt32(7),
                        CasillaDestinoY = reader.GetInt32(8),
                        CasillaDestinoZ = reader.GetInt32(9),
                    };
                }
            }
        }

        if (flota == null) return null;

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "SELECT id_bien, cantidad FROM CargaFlotaPNJ WHERE id_flota = @id;";
            cmd.Parameters.AddWithValue("@id", id);

            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    flota.Carga[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }

        return flota;
    }

    /// <summary>
    /// Una flota insertada en FlotaPNJ y su carga en CargaFlotaPNJ deben
    /// reconstruirse exactamente con todos sus campos tras leer de nuevo la BD.
    /// </summary>
    [Test]
    public void GuardarYCargarFlota_DatosCorrectos()
    {
        InsertarFlota(1001, "Hans Test", 1, 3, "Viajando", 2.5f, 4.0f, 8, 6, 0);
        InsertarCarga(1001, 1, 100);
        InsertarCarga(1001, 5, 50);

        FlotaRuntimeDataLocal flota = LeerFlota(1001);

        Assert.IsNotNull(flota);
        Assert.AreEqual(1001,        flota.Id);
        Assert.AreEqual("Hans Test", flota.NombrePropietario);
        Assert.AreEqual(1,           flota.CiudadOrigenId);
        Assert.AreEqual(3,           flota.CiudadDestinoId);
        Assert.AreEqual("Viajando",  flota.EstadoActual);
        Assert.AreEqual(2.5f, flota.PosicionX, 0.001f);
        Assert.AreEqual(4.0f, flota.PosicionY, 0.001f);
        Assert.AreEqual(8, flota.CasillaDestinoX);
        Assert.AreEqual(6, flota.CasillaDestinoY);
        Assert.AreEqual(0, flota.CasillaDestinoZ);
        Assert.AreEqual(2,   flota.Carga.Count);
        Assert.AreEqual(100, flota.Carga[1]);
        Assert.AreEqual(50,  flota.Carga[5]);
    }

    /// <summary>
    /// Al borrar y reinsertar una flota con datos distintos, los nuevos valores
    /// deben reemplazar completamente a los anteriores, incluyendo la carga.
    /// </summary>
    [Test]
    public void GuardOnDuplicate_SobreescribeCarga()
    {
        InsertarFlota(1001, "Hans Test", 1, 3, "EnPuerto", 0f, 0f, 0, 0, 0);
        InsertarCarga(1001, 1, 100);

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM FlotaPNJ WHERE id = 1001;";
            cmd.ExecuteNonQuery();
        }
        InsertarFlota(1001, "Hans Test", 1, 5, "Comerciando", 1f, 1f, 3, 3, 0);

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM CargaFlotaPNJ WHERE id_flota = 1001;";
            cmd.ExecuteNonQuery();
        }
        InsertarCarga(1001, 2, 75);

        FlotaRuntimeDataLocal flota = LeerFlota(1001);

        Assert.AreEqual(5,  flota.CiudadDestinoId,          "El destino debe ser el nuevo valor.");
        Assert.AreEqual(1,  flota.Carga.Count,               "Solo debe existir la nueva entrada de carga.");
        Assert.AreEqual(75, flota.Carga[2],                  "La cantidad del bien 2 debe ser 75.");
        Assert.IsFalse(flota.Carga.ContainsKey(1),           "La carga anterior (bien 1) debe haber sido eliminada.");
    }
}
