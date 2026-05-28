using System;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Singleton central de persistencia del juego. Abre (o crea) el fichero .db
/// correspondiente al slot de guardado indicado y garantiza que las 15 tablas
/// del esquema existen antes de que cualquier DAO intente operar sobre ellas.
/// </summary>
public class DatabaseManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    /// <summary>
    /// Punto de acceso global al gestor de base de datos activo.
    /// </summary>
    public static DatabaseManager Instance { get; private set; }

    // ─── Conexión pública ────────────────────────────────────────────────────

    /// <summary>
    /// Conexión SQLite activa. Los DAOs la reutilizan para ejecutar sus propias
    /// consultas sin abrir conexiones adicionales.
    /// </summary>
    public SqliteConnection Conexion { get; private set; }

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Conexion != null && Conexion.State != ConnectionState.Closed)
        {
            Conexion.Close();
            Conexion.Dispose();
            Conexion = null;
        }
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Abre (o crea) el fichero .db del slot indicado y ejecuta la creación de
    /// tablas si todavía no existen. Debe llamarse una única vez al iniciar o
    /// cargar una partida, antes de que cualquier otro sistema acceda a la BD.
    /// </summary>
    /// <param name="numeroSlot">Número de slot de guardado (1 a 5).</param>
    public void InicializarSlot(int numeroSlot)
    {
        string ruta = System.IO.Path.Combine(
            Application.persistentDataPath,
            $"slot_{numeroSlot}.db"
        );

        string cadenaConexion = $"URI=file:{ruta}";

        try
        {
            // Cerrar la conexión anterior para evitar leaks si se cambia de slot
            if (Conexion != null && Conexion.State != System.Data.ConnectionState.Closed)
            {
                Conexion.Close();
                Conexion.Dispose();
                Conexion = null;
            }

            Conexion = new SqliteConnection(cadenaConexion);
            Conexion.Open();
            CrearTablasSiNoExisten();
            MigrarColumnaDineroJugador();
            MigrarColumnaModopirata();
            new CiudadDAO(this).MigrarColumnasCasilla();
            new FlotaDAO(this).MigrarColumnasMapamundi();
            MigrarTablaAlmacenCiudad();
            MigrarTablasFlotaPNJ();
            MigrarColumnasTripulacionBarco();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Error al abrir el slot {numeroSlot} en '{ruta}': {ex}");
        }
    }

    // ─── Privados ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Añade la columna dinero_jugador a estadoJuego en bases de datos creadas antes
    /// de esta versión. SQLite no soporta ADD COLUMN IF NOT EXISTS, por lo que se
    /// intenta el ALTER y se atrapa la excepción si la columna ya existe.
    /// </summary>
    private void MigrarColumnaDineroJugador()
    {
        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE estadoJuego ADD COLUMN dinero_jugador INTEGER NOT NULL DEFAULT 999999999;";
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception)
        {
            // La columna ya existe en esta BD; comportamiento esperado en bases de datos nuevas
            Debug.Log("[DatabaseManager] Columna dinero_jugador ya existe (BD nueva).");
        }
    }

    /// <summary>
    /// Añade la columna modo_pirata a estadoJuego en bases de datos creadas antes de esta versión.
    /// SQLite no soporta ADD COLUMN IF NOT EXISTS, por lo que se intenta el ALTER y se atrapa
    /// la excepción si la columna ya existe.
    /// </summary>
    private void MigrarColumnaModopirata()
    {
        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE estadoJuego ADD COLUMN modo_pirata INTEGER NOT NULL DEFAULT 0;";
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception)
        {
            // La columna ya existe en esta BD; comportamiento esperado en bases de datos nuevas
            Debug.Log("[DatabaseManager] Columna modo_pirata ya existe (BD nueva).");
        }
    }

    /// <summary>
    /// Añade las columnas tripulacion_actual y capacidad_tripulacion a la tabla Barco
    /// en bases de datos creadas antes de que se implementara el sistema de tripulación.
    /// El try/catch atrapa la excepción de SQLite cuando la columna ya existe.
    /// </summary>
    private void MigrarColumnasTripulacionBarco()
    {
        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE Barco ADD COLUMN tripulacion_actual INTEGER NOT NULL DEFAULT 0;";
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception)
        {
            Debug.Log("[DatabaseManager] Columna tripulacion_actual ya existe en Barco (BD nueva).");
        }

        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE Barco ADD COLUMN capacidad_tripulacion INTEGER NOT NULL DEFAULT 50;";
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception)
        {
            Debug.Log("[DatabaseManager] Columna capacidad_tripulacion ya existe en Barco (BD nueva).");
        }
    }

    /// <summary>
    /// AlmacenCiudadJugador ya está incluida en CrearTablasSiNoExisten(); este método
    /// existe únicamente como compatibilidad hacia atrás para partidas guardadas antes
    /// del Día 18 y se mantiene vacío de forma segura.
    /// </summary>
    private void MigrarTablaAlmacenCiudad()
    {
        // Tabla ya creada en el DDL principal con IF NOT EXISTS — nada que hacer aquí
    }

    /// <summary>
    /// Crea las tablas FlotaPNJ y CargaFlotaPNJ para la persistencia de flotas PNJ comerciantes.
    /// Separadas de la tabla Flota original (que usa id_flota y tipo_propietario) para evitar
    /// conflictos de schema con FlotaDAO. Seguro de llamar siempre gracias a IF NOT EXISTS.
    /// </summary>
    private void MigrarTablasFlotaPNJ()
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS FlotaPNJ (
                id                INTEGER PRIMARY KEY,
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

        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Error al crear tablas FlotaPNJ/CargaFlotaPNJ: {ex}");
        }
    }

    /// <summary>
    /// Ejecuta el bloque DDL completo con CREATE TABLE IF NOT EXISTS para las
    /// 15 tablas del esquema, respetando el orden de dependencias de FK.
    /// Las restricciones de clave foránea se activan con PRAGMA foreign_keys.
    /// </summary>
    private void CrearTablasSiNoExisten()
    {
        string sql = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS estadoJuego (
                id_estado        INTEGER PRIMARY KEY,
                dia_juego        INTEGER NOT NULL,
                mes_juego        INTEGER NOT NULL,
                año_juego        INTEGER NOT NULL,
                velocidad_tiempo INTEGER NOT NULL,
                fecha_guardado   TIMESTAMP NOT NULL,
                dinero_jugador   INTEGER NOT NULL DEFAULT 999999999,
                modo_pirata      INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Ciudad (
                id_ciudad INTEGER PRIMARY KEY,
                nombre    TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Bien (
                id_bien     INTEGER PRIMARY KEY,
                nombre      TEXT NOT NULL,
                categoria   TEXT NOT NULL,
                precio_base DECIMAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS AlmacenJugador (
                id_bien  INTEGER NOT NULL REFERENCES Bien(id_bien),
                cantidad INTEGER NOT NULL,
                PRIMARY KEY (id_bien)
            );

            CREATE TABLE IF NOT EXISTS RecetaProduccion (
                id_bien_resultado   INTEGER NOT NULL REFERENCES Bien(id_bien),
                id_bien_ingrediente INTEGER NOT NULL REFERENCES Bien(id_bien),
                cantidad_requerida  INTEGER NOT NULL,
                PRIMARY KEY (id_bien_resultado, id_bien_ingrediente)
            );

            CREATE TABLE IF NOT EXISTS EstadoMercadoCiudad (
                id_ciudad     INTEGER NOT NULL REFERENCES Ciudad(id_ciudad),
                id_bien       INTEGER NOT NULL REFERENCES Bien(id_bien),
                stock         INTEGER NOT NULL,
                produccion    INTEGER NOT NULL,
                consumo       INTEGER NOT NULL,
                precio_actual DECIMAL NOT NULL,
                PRIMARY KEY (id_ciudad, id_bien)
            );

            CREATE TABLE IF NOT EXISTS TipoEdificio (
                id_tipo_edificio INTEGER PRIMARY KEY,
                nombre           TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS EdificiosCiudad (
                id_ciudad        INTEGER NOT NULL REFERENCES Ciudad(id_ciudad),
                id_tipo_edificio INTEGER NOT NULL REFERENCES TipoEdificio(id_tipo_edificio),
                cantidad         INTEGER NOT NULL,
                PRIMARY KEY (id_ciudad, id_tipo_edificio)
            );

            CREATE TABLE IF NOT EXISTS Capitan (
                id_capitan INTEGER PRIMARY KEY,
                nombre     TEXT NOT NULL,
                asignado   BOOLEAN NOT NULL DEFAULT FALSE
            );

            CREATE TABLE IF NOT EXISTS TipoCasco (
                id_tipo_casco            INTEGER PRIMARY KEY,
                nombre                   TEXT NOT NULL,
                vida_base                INTEGER NOT NULL,
                velocidad_base           INTEGER NOT NULL,
                maniobrabilidad_base     INTEGER NOT NULL,
                capacidad_carga_base     INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Flota (
                id_flota          INTEGER PRIMARY KEY,
                tipo_propietario  TEXT NOT NULL,
                id_ciudad_actual  INTEGER REFERENCES Ciudad(id_ciudad),
                posicion_x        FLOAT,
                posicion_y        FLOAT,
                id_capitan        INTEGER REFERENCES Capitan(id_capitan),
                estado_actual     TEXT NOT NULL DEFAULT 'EnPuerto',
                id_ciudad_destino INTEGER REFERENCES Ciudad(id_ciudad)
            );

            CREATE TABLE IF NOT EXISTS Barco (
                id_barco              INTEGER PRIMARY KEY,
                id_tipo_casco         INTEGER NOT NULL REFERENCES TipoCasco(id_tipo_casco),
                nombre_barco          TEXT NOT NULL,
                es_barco_combate      BOOLEAN NOT NULL DEFAULT FALSE,
                vida_actual           INTEGER NOT NULL,
                tripulacion_actual    INTEGER NOT NULL,
                capacidad_tripulacion INTEGER NOT NULL,
                id_flota              INTEGER REFERENCES Flota(id_flota)
            );

            CREATE TABLE IF NOT EXISTS ModuloBarco (
                id_modulo_barco INTEGER PRIMARY KEY,
                id_barco        INTEGER NOT NULL REFERENCES Barco(id_barco),
                tipo_modulo     TEXT NOT NULL,
                nombre_modulo   TEXT NOT NULL,
                valor_a         INTEGER NOT NULL,
                valor_b         INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS EstadoSeccionBarco (
                id_barco     INTEGER NOT NULL REFERENCES Barco(id_barco),
                seccion      TEXT NOT NULL,
                vida_seccion INTEGER NOT NULL,
                PRIMARY KEY (id_barco, seccion)
            );

            CREATE TABLE IF NOT EXISTS CargaBarco (
                id_barco INTEGER NOT NULL REFERENCES Barco(id_barco),
                id_bien  INTEGER NOT NULL REFERENCES Bien(id_bien),
                cantidad INTEGER NOT NULL,
                PRIMARY KEY (id_barco, id_bien)
            );

            CREATE TABLE IF NOT EXISTS MemoriaComercialPNJ (
                id_flota           INTEGER NOT NULL,
                id_bien            INTEGER NOT NULL,
                id_ciudad          INTEGER NOT NULL DEFAULT 0,
                precio_conocido    DECIMAL NOT NULL,
                dia_juego_conocido INTEGER NOT NULL,
                PRIMARY KEY (id_flota, id_bien, id_ciudad)
            );

            CREATE TABLE IF NOT EXISTS AlmacenCiudadJugador (
                id_ciudad INTEGER NOT NULL,
                id_bien   INTEGER NOT NULL,
                cantidad  INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (id_ciudad, id_bien)
            );
        ";

        try
        {
            using (SqliteCommand cmd = Conexion.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Error al crear las tablas: {ex}");
        }
    }
}
