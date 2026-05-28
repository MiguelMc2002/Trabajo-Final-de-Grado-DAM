using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Gestiona toda la navegación del jugador en el mapamundi.
/// - Click DERECHO sobre una casilla navegable → calcula ruta A* y mueve la flota.
/// - Un nuevo click derecho en cualquier momento cancela la ruta anterior y asigna la nueva (redirección).
/// - Al cruzar una casilla de ciudad, detiene la flota y muestra PopUpEntradaCiudad.
/// - Tecla M → navega directamente a la última ciudad visitada.
/// Añadir este componente a un GameObject dedicado en la escena Mapamundi (por ejemplo "NavegacionJugador").
/// Requiere que MapamundiController e IconoFlotaJugador estén inicializados antes de recibir input
/// (el Start de MapamundiController los crea, así que el primer frame es seguro).
/// </summary>
public class NavegacionJugadorController : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    private static NavegacionJugadorController _instance;

    /// <summary>Punto de acceso global. Sin DontDestroyOnLoad: vive solo en la escena Mapamundi.</summary>
    public static NavegacionJugadorController Instance => _instance;

    // ── Dependencias (asignar desde Inspector) ────────────────────────────────

    /// <summary>Tilemap del mapamundi. Necesario para convertir posición de pantalla a casilla.</summary>
    [SerializeField] private Tilemap tilemap;

    /// <summary>Calculador A* del mapamundi. Mismo asset que usan los PNJ.</summary>
    [SerializeField] private RutaCalculadorTilemap rutaCalculador;

    /// <summary>Panel modal que pregunta al jugador si quiere entrar a la ciudad.</summary>
    [SerializeField] private PopUpEntradaCiudad popUpEntradaCiudad;

    /// <summary>
    /// True cuando el jugador acaba de salir de una ciudad (por transición de escena
    /// o por pulsar "Continuar navegando" en el pop-up). Se desactiva en cuanto la
    /// flota cruza su primer waypoint, garantizando que el pop-up no se dispara
    /// en la casilla de ciudad de partida.
    /// </summary>
    private bool _recienSalidoDeCiudad;

    // ── Persecución (modo pirata) ─────────────────────────────────────────────
    private FlotaRuntimeData _flotaObjetivo;
    private Coroutine        _coroutinePersecucion;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        // Si venimos de una ciudad, ignorar el primer waypoint cruzado
        _recienSalidoDeCiudad = GameManager.Instance?.CiudadActual != null;
    }

    private void OnEnable()
    {
        FlotaIconoMapamundi.OnWaypointJugadorCruzado += AlCruzarWaypoint;
        CombateEventos.OnCombateTerminado            += AlTerminarCombateJugador;
    }

    private void OnDisable()
    {
        FlotaIconoMapamundi.OnWaypointJugadorCruzado -= AlCruzarWaypoint;
        CombateEventos.OnCombateTerminado            -= AlTerminarCombateJugador;
    }

    private void Update()
    {
        // Tecla M → alternar entre mapamundi y ciudad si la flota está en puerto
        if (Input.GetKeyDown(KeyCode.M))
        {
            FlotaIconoMapamundi icono = MapamundiController.Instance?.IconoFlotaJugador;
            if (icono?.Flota?.EstadoActual == EstadoFlotaPNJ.EnPuerto && GameManager.Instance != null)
            {
                // Buscar la ciudad actual por CiudadOrigenId; si no, usar CiudadActual como fallback
                CiudadData ciudad = null;
                int idCiudad = icono.Flota.CiudadOrigenId;
                if (idCiudad != -1)
                    foreach (CiudadData c in GameManager.Instance.CiudadesDisponibles)
                        if (c.IdCiudad == idCiudad) { ciudad = c; break; }
                if (ciudad == null)
                    ciudad = GameManager.Instance.CiudadActual;

                if (ciudad != null)
                {
                    GameManager.Instance.EstablecerCiudadActual(ciudad);
                    SceneController.IrACiudad();
                }
            }
            return;
        }

        // Click izquierdo → inspeccionar flota PNJ si hay una bajo el cursor
        if (Input.GetMouseButtonDown(0))
        {
            Camera camara = Camera.main;
            if (camara != null)
            {
                Vector3 posicionMundo = camara.ScreenToWorldPoint(Input.mousePosition);
                posicionMundo.z = 0f;
                RaycastHit2D hit = Physics2D.Raycast(posicionMundo, Vector2.zero);
                if (hit.collider != null)
                {
                    FlotaIconoMapamundi iconoHit = hit.collider.GetComponent<FlotaIconoMapamundi>();
                    if (iconoHit != null && !iconoHit._esJugador)
                    {
                        FlotaIconoMapamundi.InvocarFlotaClickada(iconoHit.Flota);
                        return;
                    }
                }
            }
        }

        // El click derecho lo gestiona MapamundiCamara (tiene la referencia de cámara correcta)
    }

    // ── Navegación ────────────────────────────────────────────────────────────

    /// <summary>
    /// Cancela la persecución activa e inicia navegación hacia la posición de pantalla dada.
    /// Llamar desde <see cref="MapamundiCamara"/> cuando el jugador hace click derecho en una casilla vacía.
    /// </summary>
    public void CancelarPersecucionYNavegar(Vector2 posicionPantalla)
    {
        CancelarPersecucion();
        ProcesarClick(posicionPantalla);
    }

    /// <summary>
    /// Convierte la posición de pantalla a casilla de tilemap y, si es navegable,
    /// calcula la ruta A* y la asigna al icono del jugador.
    /// Si el jugador ya estaba en ruta, la cancela y empieza la nueva (redirección).
    /// </summary>
    private void ProcesarClick(Vector2 posicionPantalla)
    {
        if (MapamundiController.Instance == null) return;
        FlotaIconoMapamundi icono = MapamundiController.Instance.IconoFlotaJugador;
        if (icono == null) return;
        Camera camara = Camera.main;
        if (camara == null) return;

        Vector3 posicionMundo     = camara.ScreenToWorldPoint(posicionPantalla);
        posicionMundo.z           = 0f;
        Vector3Int casillaDestino = tilemap.WorldToCell(posicionMundo);

        // Ignorar casillas vacías o sin sprite (fuera del tilemap)
        Sprite sprite = tilemap.GetSprite(casillaDestino);
        if (sprite == null) return;

        // Ignorar tierra e intransitables — ajustar nombres si el proyecto usa otros sprites
        if (sprite.name.Contains("Tierra") || sprite.name.Contains("Intransitable")) return;

        if (!rutaCalculador.EsTransitable(casillaDestino)) return;

        IniciarNavegacion(casillaDestino);
    }

    /// <summary>
    /// Calcula la ruta A* desde la posición actual del jugador hasta la casilla destino
    /// y la asigna al icono, cancelando cualquier ruta anterior en curso.
    /// </summary>
    private void IniciarNavegacion(Vector3Int casillaDestino)
    {
        if (MapamundiController.Instance == null) return;
        FlotaIconoMapamundi icono = MapamundiController.Instance.IconoFlotaJugador;
        if (icono == null || icono.Flota == null) return;

        Vector3Int casillaActual = tilemap.WorldToCell(icono.transform.position);
        if (casillaActual == casillaDestino) return;

        List<Vector3Int> ruta = rutaCalculador.CalcularRuta(casillaActual, casillaDestino);
        if (ruta == null || ruta.Count == 0)
        {
            Debug.LogWarning($"[NavegacionJugador] Sin ruta hacia {casillaDestino}");
            return;
        }

        icono.AsignarRuta(ruta);
        Debug.Log($"[NavegacionJugador] Nueva ruta: {ruta.Count} casillas → {casillaDestino}");
    }

    // ── Entrada a ciudad ──────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por MarcadorCiudad cuando el jugador hace click sobre el sprite de una ciudad.
    /// Si la flota ya está en esa casilla muestra el pop-up directamente.
    /// Si no, navega hasta allí; el pop-up aparecerá al cruzar la casilla.
    /// </summary>
    /// <param name="ciudad">Ciudad sobre la que se hizo clic.</param>
    public void SolicitarEntradaCiudad(CiudadData ciudad)
    {
        if (ciudad == null) return;
        FlotaIconoMapamundi icono = MapamundiController.Instance?.IconoFlotaJugador;
        if (icono == null) return;

        Vector3Int casillaActual = tilemap.WorldToCell(icono.transform.position);
        if (casillaActual == ciudad.CasillaMapamundi)
            MostrarPopUpCiudad(ciudad);
        else
            IniciarNavegacion(ciudad.CasillaMapamundi);
    }

    /// <summary>
    /// Callback suscrito a FlotaIconoMapamundi.OnWaypointJugadorCruzado.
    /// Si la casilla corresponde a una ciudad, detiene la flota y muestra el pop-up.
    /// </summary>
    private void AlCruzarWaypoint(Vector3Int casilla)
    {
        if (_recienSalidoDeCiudad)
        {
            _recienSalidoDeCiudad = false;
            return;
        }

        FlotaIconoMapamundi icono = MapamundiController.Instance?.IconoFlotaJugador;
        if (icono == null || icono.Flota == null) return;
        if (icono.Flota.IndiceWaypointActual == 0) return;

        if (MapamundiController.Instance == null) return;
        CiudadData ciudad = MapamundiController.Instance.ObtenerCiudadEnCasilla(casilla);
        if (ciudad == null) return;

        // Detener la flota vaciando la ruta
        icono.Flota.RutaActualTilemap?.Clear();
        icono.Flota.IndiceWaypointActual = 0;
        icono.Flota.EstadoActual         = EstadoFlotaPNJ.EnPuerto;
        MostrarPopUpCiudad(ciudad);
    }

    /// <summary>
    /// Marca que el jugador acaba de rechazar entrar a una ciudad.
    /// El siguiente waypoint cruzado (la casilla de ciudad actual) se ignorará
    /// para evitar que el pop-up se dispare de nuevo inmediatamente.
    /// </summary>
    public void MarcarSalidaDeCiudad()
    {
        _recienSalidoDeCiudad = true;
    }

    private void MostrarPopUpCiudad(CiudadData ciudad)
    {
        if (popUpEntradaCiudad != null)
            popUpEntradaCiudad.Mostrar(ciudad);
        else
            Debug.LogWarning("[NavegacionJugador] PopUpEntradaCiudad no asignado en el Inspector.");
    }

    // ── Persecución (modo pirata) ─────────────────────────────────────────────

    /// <summary>
    /// Inicia la persecución de una flota PNJ en modo pirata.
    /// Cancela cualquier persecución previa y lanza la coroutine de recálculo de ruta.
    /// </summary>
    /// <param name="objetivo">Flota PNJ que se perseguirá.</param>
    public void IniciarPersecucion(FlotaRuntimeData objetivo)
    {
        if (objetivo == null) return;
        CancelarPersecucion();
        _flotaObjetivo        = objetivo;
        _coroutinePersecucion = StartCoroutine(BuclePersecucion());
        Debug.Log($"[NavegacionJugador] Persecución iniciada hacia {objetivo.NombrePropietario}");
    }

    /// <summary>Cancela la persecución activa y detiene al jugador en su posición actual.</summary>
    private void CancelarPersecucion()
    {
        if (_coroutinePersecucion != null)
        {
            StopCoroutine(_coroutinePersecucion);
            _coroutinePersecucion = null;
        }
        _flotaObjetivo = null;

        // Limpiar ruta y destino para que el jugador se detenga
        FlotaIconoMapamundi icono = MapamundiController.Instance?.IconoFlotaJugador;
        if (icono?.Flota != null)
        {
            icono.Flota.RutaActualTilemap?.Clear();
            icono.Flota.IndiceWaypointActual = 0;
            icono.Flota.CasillaDestino       = Vector3Int.zero;
        }
    }

    /// <summary>
    /// Recalcula la ruta hacia el objetivo cada 0,5 segundos.
    /// Se detiene si el objetivo entra en puerto, es destruido o el jugador lo alcanza.
    /// </summary>
    private IEnumerator BuclePersecucion()
    {
        var espera = new WaitForSeconds(0.5f);

        while (_flotaObjetivo != null)
        {
            // Objetivo destruido: ya no está en FlotaManager
            if (FlotaManager.Instance == null ||
                FlotaManager.Instance.ObtenerFlota(_flotaObjetivo.Id) == null)
            {
                Debug.Log("[NavegacionJugador] Objetivo destruido, persecución cancelada.");
                CancelarPersecucion();
                yield break;
            }

            // Objetivo en puerto: dejar de perseguir
            if (_flotaObjetivo.EstadoActual == EstadoFlotaPNJ.EnPuerto)
            {
                Debug.Log("[NavegacionJugador] Objetivo en puerto, persecución cancelada.");
                CancelarPersecucion();
                yield break;
            }

            FlotaIconoMapamundi iconoJugador = MapamundiController.Instance?.IconoFlotaJugador;
            if (iconoJugador == null) { CancelarPersecucion(); yield break; }

            Vector3Int casillaJugador  = tilemap.WorldToCell(iconoJugador.transform.position);
            Vector3Int casillaObjetivo = tilemap.WorldToCell(
                new Vector3(_flotaObjetivo.PosicionActual.x, _flotaObjetivo.PosicionActual.y, 0f));

            // Jugador alcanzó al objetivo: disparar combate con el jugador como atacante
            if (casillaJugador == casillaObjetivo)
            {
                Debug.Log($"[NavegacionJugador] Jugador alcanzó a {_flotaObjetivo.NombrePropietario}. Iniciando combate.");
                FlotaRuntimeData objetivoCapturado = _flotaObjetivo;
                CancelarPersecucion();
                MapamundiController.Instance?.IniciarCombateJugadorAtaca(objetivoCapturado);
                yield break;
            }

            // Recalcular ruta hacia la posición actual del objetivo
            IniciarNavegacion(casillaObjetivo);

            yield return espera;
        }
    }

    /// <summary>
    /// Suscrito a <see cref="CombateEventos.OnCombateTerminado"/>.
    /// Si el jugador sale del combate sin barcos, navega automáticamente a la ciudad más cercana.
    /// </summary>
    private void AlTerminarCombateJugador()
    {
        FlotaJugador flota = GameManager.Instance?.FlotaJugador;
        if (flota == null || flota.Barcos.Count > 0) return;

        FlotaIconoMapamundi icono = MapamundiController.Instance?.IconoFlotaJugador;
        if (icono == null || GameManager.Instance == null) return;

        Vector3Int casillaJugador  = tilemap.WorldToCell(icono.transform.position);
        CiudadData ciudadMasCercana = null;
        float      distMin          = float.MaxValue;
        foreach (CiudadData ciudad in GameManager.Instance.CiudadesDisponibles)
        {
            float dist = Vector3Int.Distance(casillaJugador, ciudad.CasillaMapamundi);
            if (dist < distMin) { distMin = dist; ciudadMasCercana = ciudad; }
        }

        if (ciudadMasCercana != null)
            SolicitarEntradaCiudad(ciudadMasCercana);
    }
}
