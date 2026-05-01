using System;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Determina si el panel de slots se abrió para guardar o para cargar una partida.
/// Controla qué acción ejecuta el botón principal de cada fila.
/// </summary>
public enum SlotModo
{
    /// <summary>El jugador quiere guardar la partida actual en un slot.</summary>
    Guardar,
    /// <summary>El jugador quiere cargar una partida guardada anteriormente.</summary>
    Cargar
}

/// <summary>
/// Gestiona el panel completo de selección de slots de guardado y carga.
/// Al abrirse, escanea los cinco archivos <c>slot_N.db</c> en disco, lee sus
/// metadatos mediante una conexión SQLite temporal de solo lectura y rellena
/// cada <see cref="SlotUI"/> con la información correspondiente.
/// </summary>
public class PantallaSlotsUI : MonoBehaviour
{
    // ─── Referencias de Inspector ─────────────────────────────────────────────

    /// <summary>
    /// Las cinco filas de slot del panel. Cada elemento corresponde al slot N+1
    /// (índice 0 → slot 1, índice 4 → slot 5). Asignar desde el Inspector.
    /// </summary>
    [SerializeField] private SlotUI[] _slots;

    /// <summary>Botón que cierra el panel sin realizar ninguna acción.</summary>
    [SerializeField] private Button _botonCerrar;

    // ─── Panel de confirmación de sobrescritura ───────────────────────────────

    /// <summary>Panel secundario que pregunta "¿Sobrescribir partida?".</summary>
    [SerializeField] private GameObject _panelConfirmacion;

    /// <summary>Texto del panel de confirmación (muestra el nombre del slot afectado).</summary>
    [SerializeField] private TextMeshProUGUI _textoConfirmacion;

    /// <summary>Botón "Sí" del panel de confirmación.</summary>
    [SerializeField] private Button _botonConfirmarSi;

    /// <summary>Botón "No" del panel de confirmación.</summary>
    [SerializeField] private Button _botonConfirmarNo;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private SlotModo _modoActual;
    private const int NumeroDeSlots = 5;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _botonCerrar.onClick.AddListener(Cerrar);
        _botonConfirmarNo.onClick.AddListener(() => _panelConfirmacion.SetActive(false));
        _panelConfirmacion.SetActive(false);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el panel en el modo indicado, escanea los archivos de partida en disco
    /// y rellena cada fila con los metadatos del slot correspondiente.
    /// </summary>
    /// <param name="modo">
    /// <see cref="SlotModo.Guardar"/> para mostrar el panel de guardado;
    /// <see cref="SlotModo.Cargar"/> para el de carga.
    /// </param>
    public void Abrir(SlotModo modo)
    {
        _modoActual = modo;
        gameObject.SetActive(true);
        _panelConfirmacion.SetActive(false);
        RefrescarSlots();
    }

    /// <summary>
    /// Invocado por el <see cref="SlotUI"/> cuando el jugador pulsa Guardar en una fila.
    /// Si el slot está ocupado muestra el panel de confirmación de sobrescritura;
    /// si está vacío guarda directamente.
    /// </summary>
    /// <param name="slotUI">Fila de slot sobre la que actuar.</param>
    public void OnGuardar(SlotUI slotUI)
    {
        if (slotUI.Datos.EstaOcupado)
        {
            MostrarConfirmacion(
                $"¿Sobrescribir '{slotUI.Datos.NombrePartida}'?",
                () => EjecutarGuardado(slotUI.Datos.NumeroSlot)
            );
        }
        else
        {
            EjecutarGuardado(slotUI.Datos.NumeroSlot);
        }
    }

    /// <summary>
    /// Invocado por el <see cref="SlotUI"/> cuando el jugador pulsa Cargar en una fila.
    /// Carga la partida del slot indicado y navega al mapamundi.
    /// </summary>
    /// <param name="slotUI">Fila de slot que contiene la partida a cargar.</param>
    public void OnCargar(SlotUI slotUI)
    {
        if (!slotUI.Datos.EstaOcupado) return;

        LoadManager.Instance.CargarPartida(slotUI.Datos.NumeroSlot);
        Cerrar();
        SceneController.IrAMapamundi();
    }

    /// <summary>
    /// Invocado por el <see cref="SlotUI"/> cuando el jugador pulsa Borrar en una fila.
    /// Elimina el archivo <c>.db</c> del slot y refresca la UI.
    /// </summary>
    /// <param name="slotUI">Fila de slot cuya partida se elimina.</param>
    public void OnBorrar(SlotUI slotUI)
    {
        if (!slotUI.Datos.EstaOcupado) return;

        MostrarConfirmacion(
            $"¿Eliminar '{slotUI.Datos.NombrePartida}'? Esta acción no se puede deshacer.",
            () =>
            {
                string ruta = RutaSlot(slotUI.Datos.NumeroSlot);
                if (File.Exists(ruta))
                {
                    File.Delete(ruta);
                    Debug.Log($"[PantallaSlotsUI] Slot {slotUI.Datos.NumeroSlot} eliminado: {ruta}");
                }
                RefrescarSlots();
            }
        );
    }

    // ─── Privados ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Desactiva el panel de slots y oculta el de confirmación.
    /// </summary>
    private void Cerrar()
    {
        _panelConfirmacion.SetActive(false);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Escanea los cinco archivos de slot en disco y actualiza cada <see cref="SlotUI"/>
    /// con los metadatos leídos.
    /// </summary>
    private void RefrescarSlots()
    {
        for (int i = 0; i < NumeroDeSlots; i++)
        {
            int numeroSlot = i + 1;
            SlotData datos = LeerMetadatosSlot(numeroSlot);
            _slots[i].Inicializar(datos, this);
        }
    }

    /// <summary>
    /// Lee los metadatos de un slot abriendo una conexión SQLite temporal de solo lectura.
    /// Si el archivo no existe devuelve un <see cref="SlotData"/> vacío.
    /// La conexión se cierra y libera antes de que el método retorne.
    /// </summary>
    /// <param name="numeroSlot">Número de slot a leer (1 a 5).</param>
    /// <returns>Metadatos del slot, con <see cref="SlotData.EstaOcupado"/> según si el archivo existe.</returns>
    private SlotData LeerMetadatosSlot(int numeroSlot)
    {
        var datos = new SlotData
        {
            NumeroSlot   = numeroSlot,
            NombrePartida = $"Partida {numeroSlot}",
            EstaOcupado  = false,
            FechaGuardado = string.Empty,
            DiasJugados  = 0
        };

        string ruta = RutaSlot(numeroSlot);
        if (!File.Exists(ruta))
            return datos;

        string cadenaConexion = $"URI=file:{ruta};Read Only=True";
        SqliteConnection conexion = null;

        try
        {
            conexion = new SqliteConnection(cadenaConexion);
            conexion.Open();

            const string sql = "SELECT dia_juego, mes_juego, año_juego, fecha_guardado FROM estadoJuego WHERE id_estado = 1 LIMIT 1;";

            using (SqliteCommand cmd = conexion.CreateCommand())
            {
                cmd.CommandText = sql;

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int dia  = reader.GetInt32(0);
                        int mes  = reader.GetInt32(1);
                        int anio = reader.GetInt32(2);

                        // La fecha_guardado se almacenó en formato ISO 8601; convertir a formato local
                        string fechaIso = reader.GetString(3);
                        if (DateTime.TryParse(fechaIso, out DateTime fechaUtc))
                        {
                            DateTime fechaLocal = fechaUtc.ToLocalTime();
                            datos.FechaGuardado = fechaLocal.ToString("dd/MM/yyyy HH:mm");
                        }
                        else
                        {
                            datos.FechaGuardado = fechaIso;
                        }

                        // Los días jugados se cuentan desde el inicio del juego (1/1/año inicio)
                        datos.DiasJugados = CalcularDiasJugados(dia, mes, anio);
                        datos.EstaOcupado = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PantallaSlotsUI] No se pudieron leer los metadatos del slot {numeroSlot}: {ex.Message}");
        }
        finally
        {
            if (conexion != null && conexion.State != ConnectionState.Closed)
            {
                conexion.Close();
                conexion.Dispose();
            }
        }

        return datos;
    }

    /// <summary>
    /// Ejecuta el guardado en el slot indicado y refresca la UI.
    /// </summary>
    /// <param name="numeroSlot">Número de slot destino (1 a 5).</param>
    private void EjecutarGuardado(int numeroSlot)
    {
        SaveManager.Instance.GuardarPartida(numeroSlot);
        Debug.Log($"[PantallaSlotsUI] Partida guardada en slot {numeroSlot}.");
        RefrescarSlots();
    }

    /// <summary>
    /// Muestra el panel de confirmación con el mensaje indicado y registra la acción
    /// a ejecutar si el jugador pulsa "Sí". Limpia el listener anterior antes de
    /// registrar el nuevo para evitar que se acumulen callbacks de confirmaciones previas.
    /// </summary>
    /// <param name="mensaje">Texto de la pregunta de confirmación.</param>
    /// <param name="alConfirmar">Acción a ejecutar si el jugador pulsa "Sí".</param>
    private void MostrarConfirmacion(string mensaje, Action alConfirmar)
    {
        _textoConfirmacion.text = mensaje;

        _botonConfirmarSi.onClick.RemoveAllListeners();
        _botonConfirmarSi.onClick.AddListener(() =>
        {
            _panelConfirmacion.SetActive(false);
            alConfirmar?.Invoke();
        });

        _panelConfirmacion.SetActive(true);
    }

    /// <summary>
    /// Devuelve la ruta absoluta del archivo de base de datos para el slot indicado.
    /// </summary>
    /// <param name="numeroSlot">Número de slot (1 a 5).</param>
    /// <returns>Ruta completa al archivo <c>slot_N.db</c>.</returns>
    private static string RutaSlot(int numeroSlot)
    {
        return Path.Combine(Application.persistentDataPath, $"slot_{numeroSlot}.db");
    }

    /// <summary>
    /// Calcula los días de juego transcurridos desde la fecha de inicio de la partida
    /// (1 de enero del año de inicio configurado en <see cref="SimulacionTiempo"/>)
    /// hasta la fecha guardada. Usa un calendario simplificado de 30 días por mes.
    /// </summary>
    /// <param name="dia">Día de juego actual.</param>
    /// <param name="mes">Mes de juego actual.</param>
    /// <param name="anio">Año de juego actual.</param>
    /// <returns>Total de días de juego transcurridos desde el inicio de la partida.</returns>
    private static int CalcularDiasJugados(int dia, int mes, int anio)
    {
        // Año de inicio por defecto según SimulacionTiempo (1 de marzo de 1350)
        const int AnioInicio = 1350;
        const int MesInicio  = 3;
        const int DiaInicio  = 1;
        const int DiasPorMes = 30;
        const int MesesPorAnio = 12;

        int totalDiasGuardado = anio * MesesPorAnio * DiasPorMes + mes * DiasPorMes + dia;
        int totalDiasInicio   = AnioInicio * MesesPorAnio * DiasPorMes + MesInicio * DiasPorMes + DiaInicio;

        return Mathf.Max(0, totalDiasGuardado - totalDiasInicio);
    }
}
