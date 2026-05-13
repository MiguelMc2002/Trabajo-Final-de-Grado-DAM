using System;
using System.Collections.Generic;
using System.IO;
using Mono.Data.Sqlite;
using NUnit.Framework;

// ─── DAO local ────────────────────────────────────────────────────────────────

/// <summary>
/// Réplica autónoma de la lógica SQL de AlmacenCiudadDAO.
/// Usa SqliteConnection directamente para que el test no dependa de
/// ninguna clase de Assembly-CSharp ni de MonoBehaviours de Unity.
/// </summary>
internal class TestableAlmacenCiudadDAO
{
    private readonly SqliteConnection _conexion;

    internal TestableAlmacenCiudadDAO(SqliteConnection conexion)
    {
        _conexion = conexion;
    }

    internal int GetCantidad(int idCiudad, int idBien)
    {
        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT cantidad FROM AlmacenCiudadJugador
                WHERE id_ciudad = @idCiudad AND id_bien = @idBien;";
            cmd.Parameters.AddWithValue("@idCiudad", idCiudad);
            cmd.Parameters.AddWithValue("@idBien",   idBien);
            object resultado = cmd.ExecuteScalar();
            return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
        }
    }

    internal void SetCantidad(int idCiudad, int idBien, int cantidad)
    {
        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR REPLACE INTO AlmacenCiudadJugador (id_ciudad, id_bien, cantidad)
                VALUES (@idCiudad, @idBien, @cantidad);";
            cmd.Parameters.AddWithValue("@idCiudad", idCiudad);
            cmd.Parameters.AddWithValue("@idBien",   idBien);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            cmd.ExecuteNonQuery();
        }
    }

    internal void Incrementar(int idCiudad, int idBien, int delta)
    {
        int actual = GetCantidad(idCiudad, idBien);
        int nuevo  = actual + delta;

        if (nuevo < 0)
            throw new InvalidOperationException(
                $"[TestableAlmacenCiudadDAO] Stock insuficiente en ciudad={idCiudad}, bien={idBien}: actual={actual}, delta={delta}.");

        SetCantidad(idCiudad, idBien, nuevo);
    }

    internal Dictionary<int, int> GetTodosPorCiudad(int idCiudad)
    {
        var resultado = new Dictionary<int, int>();
        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT id_bien, cantidad FROM AlmacenCiudadJugador
                WHERE id_ciudad = @idCiudad AND cantidad > 0;";
            cmd.Parameters.AddWithValue("@idCiudad", idCiudad);

            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    resultado[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }
        return resultado;
    }

    internal void LimpiarCiudad(int idCiudad)
    {
        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM AlmacenCiudadJugador WHERE id_ciudad = @idCiudad;";
            cmd.Parameters.AddWithValue("@idCiudad", idCiudad);
            cmd.ExecuteNonQuery();
        }
    }
}

// ─── Tests ────────────────────────────────────────────────────────────────────

/// <summary>
/// Tests de Play Mode para la lógica SQL del almacén de ciudad del jugador.
/// Completamente autónomos: usan un fichero .db temporal y TestableAlmacenCiudadDAO
/// sin depender de GameManager ni MonoBehaviours activos en escena.
/// </summary>
[TestFixture]
public class SaveLoadAlmacenCiudadTests
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
        CrearTabla(_conexion);
    }

    /// <summary>Cierra la conexión y borra el fichero .db temporal tras cada test.</summary>
    [TearDown]
    public void TearDown()
    {
        _conexion?.Close();
        _conexion?.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private void CrearTabla(SqliteConnection con)
    {
        using (SqliteCommand cmd = con.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AlmacenCiudadJugador (
                    id_ciudad INTEGER NOT NULL,
                    id_bien   INTEGER NOT NULL,
                    cantidad  INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (id_ciudad, id_bien)
                );";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// SetCantidad y GetCantidad deben persistir los valores correctamente,
    /// incluyendo tras cerrar y reabrir la conexión al mismo fichero .db.
    /// </summary>
    [Test]
    public void GuardarYCargarAlmacenCiudad_DatosCorrectos()
    {
        var dao = new TestableAlmacenCiudadDAO(_conexion);
        dao.SetCantidad(idCiudad: 1, idBien: 3, cantidad: 50);
        dao.SetCantidad(idCiudad: 2, idBien: 7, cantidad: 120);

        Assert.AreEqual(50,  dao.GetCantidad(1, 3));
        Assert.AreEqual(120, dao.GetCantidad(2, 7));

        Dictionary<int, int> ciudad1 = dao.GetTodosPorCiudad(1);
        Assert.AreEqual(1,  ciudad1.Count);
        Assert.IsTrue(ciudad1.ContainsKey(3));
        Assert.AreEqual(50, ciudad1[3]);

        // Reabrir la conexión y verificar que los datos persisten en disco
        _conexion.Close();
        using (SqliteConnection nuevaConexion = new SqliteConnection($"URI=file:{_dbPath}"))
        {
            nuevaConexion.Open();
            var dao2 = new TestableAlmacenCiudadDAO(nuevaConexion);
            Assert.AreEqual(50, dao2.GetCantidad(1, 3), "Los datos deben persistir al reabrir la BD.");
        }
    }

    /// <summary>
    /// Incrementar con delta negativo mayor que el stock disponible debe lanzar
    /// InvalidOperationException sin modificar la cantidad almacenada.
    /// </summary>
    [Test]
    public void IncrementarAlmacenCiudad_NoPermiteNegativo()
    {
        var dao = new TestableAlmacenCiudadDAO(_conexion);
        dao.SetCantidad(1, 1, 10);

        Assert.Throws<InvalidOperationException>(() => dao.Incrementar(1, 1, -20));
        Assert.AreEqual(10, dao.GetCantidad(1, 1), "La cantidad no debe cambiar tras un intento fallido.");
    }

    /// <summary>
    /// LimpiarCiudad debe borrar únicamente las filas de la ciudad indicada,
    /// dejando intactas las de otras ciudades.
    /// </summary>
    [Test]
    public void LimpiarCiudad_EliminaSoloEsaCiudad()
    {
        var dao = new TestableAlmacenCiudadDAO(_conexion);
        dao.SetCantidad(1, 1, 30);
        dao.SetCantidad(1, 2, 40);
        dao.SetCantidad(2, 1, 50);

        dao.LimpiarCiudad(1);

        Assert.AreEqual(0,  dao.GetTodosPorCiudad(1).Count, "La ciudad 1 debe quedar vacía.");
        Assert.AreEqual(50, dao.GetCantidad(2, 1),           "La ciudad 2 no debe verse afectada.");
    }
}
