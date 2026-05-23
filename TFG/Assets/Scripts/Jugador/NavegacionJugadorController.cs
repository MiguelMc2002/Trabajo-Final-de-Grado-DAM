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
    }

    private void OnDisable()
    {
        FlotaIconoMapamundi.OnWaypointJugadorCruzado -= AlCruzarWaypoint;
    }

    private void Update()
    {
        // Tecla M → navegar a la última ciudad visitada
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (GameManager.Instance?.UltimaCiudad != null)
                IniciarNavegacion(GameManager.Instance.UltimaCiudad.CasillaMapamundi);
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

        // Click derecho → mover flota
        if (Input.GetMouseButtonDown(1))
            ProcesarClick(Input.mousePosition);
    }

    // ── Navegación ────────────────────────────────────────────────────────────

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
}
