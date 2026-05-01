using System.Collections.Generic;
using Mono.Data.Sqlite;
using NUnit.Framework;

// ─── DTO local ────────────────────────────────────────────────────────────────

/// <summary>
/// Copia mínima del DTO de memoria comercial, definida localmente para que el
/// test no dependa del ensamblado del proyecto.
/// </summary>
internal class MemoriaComercialPNJDtoLocal
{
    public int    IdFlota          { get; set; }
    public int    IdBien           { get; set; }
    public double PrecioConocido   { get; set; }
    public int    DiaJuegoConocido { get; set; }
}

// ─── DAO local ────────────────────────────────────────────────────────────────

/// <summary>
/// Réplica autónoma de la lógica SQL de MemoriaComercialPNJDAO.
/// Usa <see cref="SqliteConnection"/> directamente para que el test no dependa
/// de ninguna clase del proyecto ni de MonoBehaviours de Unity.
/// </summary>
internal class TestableMemoriaDAO
{
    private readonly SqliteConnection _conexion;

    internal TestableMemoriaDAO(SqliteConnection conexion)
    {
        _conexion = conexion;
    }

    internal void GuardarMemoria(int idFlota, int idBien, double precioConocido, int diaJuegoActual)
    {
        const string sql = @"
            INSERT OR REPLACE INTO MemoriaComercialPNJ
                (id_flota, id_bien, precio_conocido, dia_juego_conocido)
            VALUES (@idFlota, @idBien, @precio, @dia);";

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@idFlota", idFlota);
            cmd.Parameters.AddWithValue("@idBien",  idBien);
            cmd.Parameters.AddWithValue("@precio",  precioConocido);
            cmd.Parameters.AddWithValue("@dia",     diaJuegoActual);
            cmd.ExecuteNonQuery();
        }
    }

    internal List<MemoriaComercialPNJDtoLocal> ObtenerMemoriaDeFlota(int idFlota)
    {
        var resultado = new List<MemoriaComercialPNJDtoLocal>();
        const string sql = @"
            SELECT id_flota, id_bien, precio_conocido, dia_juego_conocido
            FROM MemoriaComercialPNJ
            WHERE id_flota = @idFlota;";

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@idFlota", idFlota);

            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    resultado.Add(new MemoriaComercialPNJDtoLocal
                    {
                        IdFlota          = reader.GetInt32(0),
                        IdBien           = reader.GetInt32(1),
                        PrecioConocido   = reader.GetDouble(2),
                        DiaJuegoConocido = reader.GetInt32(3)
                    });
                }
            }
        }

        return resultado;
    }

    internal double? ObtenerPrecioConocido(int idFlota, int idBien, int diaActual)
    {
        const string sql = @"
            SELECT precio_conocido, dia_juego_conocido
            FROM MemoriaComercialPNJ
            WHERE id_flota = @idFlota AND id_bien = @idBien
            LIMIT 1;";

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@idFlota", idFlota);
            cmd.Parameters.AddWithValue("@idBien",  idBien);

            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    double precio           = reader.GetDouble(0);
                    int    diaJuegoConocido = reader.GetInt32(1);

                    if (diaActual - diaJuegoConocido < 7)
                        return precio;
                }
            }
        }

        return null;
    }

    internal void EliminarMemoriaDeFlota(int idFlota)
    {
        const string sql = "DELETE FROM MemoriaComercialPNJ WHERE id_flota = @idFlota;";

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@idFlota", idFlota);
            cmd.ExecuteNonQuery();
        }
    }
}

// ─── Tests ────────────────────────────────────────────────────────────────────

/// <summary>
/// Tests de Play Mode para la lógica SQL de MemoriaComercialPNJ.
/// Requieren Play Mode porque los plugins nativos de SQLite (sqlite3.dll)
/// solo se cargan con el runtime del player activo.
/// Completamente autónomos: usan un archivo .db temporal y una clase DAO
/// local sin depender de ninguna clase del proyecto.
/// </summary>
[TestFixture]
public class MemoriaComercialPNJDAOTests
{
    private SqliteConnection   _conexion;
    private TestableMemoriaDAO _dao;
    private string             _rutaDb;

    /// <summary>
    /// Crea un archivo .db temporal único en disco y abre la conexión antes de cada test.
    /// El nombre único via Guid evita colisiones entre ejecuciones paralelas.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rutaDb = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "test_memoria_" + System.Guid.NewGuid().ToString("N") + ".db"
        );

        _conexion = new SqliteConnection("Data Source=" + _rutaDb + ";Version=3;");
        _conexion.Open();

        using (SqliteCommand cmd = _conexion.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MemoriaComercialPNJ (
                    id_flota           INTEGER NOT NULL,
                    id_bien            INTEGER NOT NULL,
                    precio_conocido    REAL    NOT NULL,
                    dia_juego_conocido INTEGER NOT NULL,
                    PRIMARY KEY (id_flota, id_bien)
                );";
            cmd.ExecuteNonQuery();
        }

        _dao = new TestableMemoriaDAO(_conexion);
    }

    /// <summary>Cierra la conexión y borra el archivo .db temporal tras cada test.</summary>
    [TearDown]
    public void TearDown()
    {
        _conexion?.Close();
        _conexion?.Dispose();
        _conexion = null;

        if (System.IO.File.Exists(_rutaDb))
            System.IO.File.Delete(_rutaDb);
    }

    /// <summary>
    /// GuardarMemoria debe insertar un registro nuevo cuando no existía ninguno
    /// previo para esa combinación de flota y bien.
    /// </summary>
    [Test]
    public void GuardarMemoria_InsertaNuevoRegistro_CuandoNoExistia()
    {
        _dao.GuardarMemoria(idFlota: 1, idBien: 1, precioConocido: 50.0, diaJuegoActual: 10);

        List<MemoriaComercialPNJDtoLocal> resultado = _dao.ObtenerMemoriaDeFlota(idFlota: 1);

        Assert.AreEqual(1, resultado.Count, "Debe existir exactamente un registro.");
        Assert.AreEqual(50.0, resultado[0].PrecioConocido, 0.001,
            "El precio guardado debe coincidir con el consultado.");
    }

    /// <summary>
    /// GuardarMemoria debe sobreescribir el precio existente cuando ya había
    /// un registro para la misma flota y bien (INSERT OR REPLACE).
    /// </summary>
    [Test]
    public void GuardarMemoria_SobrescribeRegistro_CuandoYaExistia()
    {
        _dao.GuardarMemoria(idFlota: 1, idBien: 1, precioConocido: 50.0, diaJuegoActual: 10);
        _dao.GuardarMemoria(idFlota: 1, idBien: 1, precioConocido: 75.0, diaJuegoActual: 12);

        List<MemoriaComercialPNJDtoLocal> resultado = _dao.ObtenerMemoriaDeFlota(idFlota: 1);

        Assert.AreEqual(1, resultado.Count, "No debe duplicarse el registro.");
        Assert.AreEqual(75.0, resultado[0].PrecioConocido, 0.001,
            "El precio debe haberse sobreescrito con el valor más reciente.");
    }

    /// <summary>
    /// ObtenerPrecioConocido debe devolver el precio cuando han transcurrido
    /// menos de 7 días desde que se anotó (dato vigente).
    /// </summary>
    [Test]
    public void ObtenerPrecioConocido_DevuelvePrecio_CuandoTieneMenosDe7Dias()
    {
        _dao.GuardarMemoria(idFlota: 1, idBien: 1, precioConocido: 50.0, diaJuegoActual: 10);

        double? resultado = _dao.ObtenerPrecioConocido(idFlota: 1, idBien: 1, diaActual: 15);

        Assert.IsNotNull(resultado, "El precio debe ser válido con menos de 7 días de antigüedad.");
        Assert.AreEqual(50.0, resultado.Value, 0.001);
    }

    /// <summary>
    /// ObtenerPrecioConocido debe devolver null cuando han transcurrido 7 o más
    /// días desde que se anotó el precio (dato caducado).
    /// </summary>
    [Test]
    public void ObtenerPrecioConocido_DevuelveNull_CuandoHanPasado7OmasDias()
    {
        _dao.GuardarMemoria(idFlota: 1, idBien: 1, precioConocido: 50.0, diaJuegoActual: 10);

        double? resultado = _dao.ObtenerPrecioConocido(idFlota: 1, idBien: 1, diaActual: 17);

        Assert.IsNull(resultado,
            "El precio debe considerarse caducado cuando la diferencia es igual o mayor a 7 días.");
    }

    /// <summary>
    /// ObtenerPrecioConocido debe devolver null cuando no existe ningún registro
    /// para la flota y bien indicados.
    /// </summary>
    [Test]
    public void ObtenerPrecioConocido_DevuelveNull_CuandoNoExisteRegistro()
    {
        double? resultado = _dao.ObtenerPrecioConocido(idFlota: 99, idBien: 99, diaActual: 1);

        Assert.IsNull(resultado,
            "Debe devolver null si no existe ningún registro para esa flota y bien.");
    }

    /// <summary>
    /// EliminarMemoriaDeFlota debe borrar todos los registros de la flota indicada.
    /// </summary>
    [Test]
    public void EliminarMemoriaDeFlota_BorraTodosLosRegistros()
    {
        _dao.GuardarMemoria(idFlota: 1, idBien: 1, precioConocido: 10.0, diaJuegoActual: 1);
        _dao.GuardarMemoria(idFlota: 1, idBien: 2, precioConocido: 20.0, diaJuegoActual: 1);
        _dao.GuardarMemoria(idFlota: 1, idBien: 3, precioConocido: 30.0, diaJuegoActual: 1);

        _dao.EliminarMemoriaDeFlota(idFlota: 1);

        List<MemoriaComercialPNJDtoLocal> resultado = _dao.ObtenerMemoriaDeFlota(idFlota: 1);

        Assert.AreEqual(0, resultado.Count,
            "La lista debe estar vacía después de eliminar la memoria de la flota.");
    }
}
