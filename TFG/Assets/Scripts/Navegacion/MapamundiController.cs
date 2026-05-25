using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controla el mapamundi: inicializa los marcadores de ciudad visibles en el mapa,
/// gestiona la navegación del jugador y resuelve los encuentros navales entre
/// flotas piratas y comerciantes cuando se cruzan en el mapa.
/// </summary>
public class MapamundiController : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    private static MapamundiController _instance;

    /// <summary>Punto de acceso global al controlador del mapamundi activo.</summary>
    public static MapamundiController Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }

    // ─── Ciudades del mapa ────────────────────────────────────────────────────

    /// <summary>
    /// Marcadores de las ciudades que aparecen en el mapamundi.
    /// Asignar desde el Inspector: uno por cada puerto navegable de la beta.
    /// </summary>
    public MarcadorCiudad[] Ciudades;

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private RutaCalculadorTilemap rutaCalculador;
    [SerializeField] private Sprite spriteBarco;
    [SerializeField] private Color colorFlotaJugador = Color.yellow;
    [SerializeField] private Color colorFlotaPirata  = Color.red;
    [SerializeField] private PanelInspeccionFlota panelInspeccionFlota;


    private FlotaIconoMapamundi _iconoFlotaJugador;

    /// <summary>Icono de la flota del jugador en el mapamundi.</summary>
    public FlotaIconoMapamundi IconoFlotaJugador => _iconoFlotaJugador;

    /// <summary>Índice de iconos por Id de flota para pausarlos o redirigirlos durante combates.</summary>
    private readonly Dictionary<int, FlotaIconoMapamundi> _iconosPorId = new();

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        foreach (MarcadorCiudad marcador in Ciudades)
            marcador.Inicializar(this);

        FlotaManager.Instance?.AsignarRutaCalculadorAPiratas(rutaCalculador);
        SpawnIconosFlotas();
        SpawnIconoFlotaJugador();
    }

    // ─── Flotas PNJ ──────────────────────────────────────────────────────────

    /// <summary>
    /// Instancia un icono en el mapamundi por cada flota PNJ activa registrada
    /// en el <see cref="FlotaManager"/>. Cada icono recibe su referencia al
    /// tilemap y al calculador A* para poder navegar de forma autónoma.
    /// </summary>
    private void SpawnIconosFlotas()
    {
        if (FlotaManager.Instance == null) return;

        foreach (FlotaRuntimeData flota in FlotaManager.Instance.ObtenerTodasLasFlotas())
        {
            GameObject go = new GameObject("FlotaIcono_" + flota.Id);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spriteBarco;
            sr.color        = flota.IsPirata ? colorFlotaPirata : Color.white;
            sr.sortingOrder = 10;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;

            FlotaIconoMapamundi icono = go.AddComponent<FlotaIconoMapamundi>();
            icono.Flota = flota;
            icono.Inicializar(tilemap, rutaCalculador);

            // Resolver CasillaDestino desde CiudadDestinoId para PNJs en viaje cuya casilla
            // no está asignada (ComerciantePNJController solo guarda CiudadDestinoId, no la casilla)
            if (flota.CasillaDestino == Vector3Int.zero &&
                flota.EstadoActual == EstadoFlotaPNJ.Viajando &&
                flota.CiudadDestinoId != -1 &&
                GameManager.Instance != null)
            {
                CiudadData ciudadDestino = GameManager.Instance.CiudadesDisponibles
                    .FirstOrDefault(c => c.IdCiudad == flota.CiudadDestinoId);
                if (ciudadDestino != null)
                    flota.CasillaDestino = ciudadDestino.CasillaMapamundi;
            }

            // Calcular ruta si la flota tiene destino asignado
            if (flota.CasillaDestino != Vector3Int.zero)
            {
                // Usar la posición actual de la flota como punto de inicio (puede estar a mitad de ruta)
                Vector3Int casillaInicio;
                if (flota.PosicionActual != Vector2.zero)
                {
                    casillaInicio = tilemap.WorldToCell(new Vector3(flota.PosicionActual.x, flota.PosicionActual.y, 0f));
                }
                else
                {
                    CiudadData ciudadOrigen = GameManager.Instance.CiudadesDisponibles
                        .FirstOrDefault(c => c.IdCiudad == flota.CiudadOrigenId);
                    casillaInicio = ciudadOrigen != null
                        ? ciudadOrigen.CasillaMapamundi
                        : flota.CasillaDestino;
                }

                flota.RutaActualTilemap = rutaCalculador.CalcularRuta(casillaInicio, flota.CasillaDestino);
                flota.IndiceWaypointActual = 0;

                // Si la ruta falló (casilla inicio en tierra), intentar desde ciudad origen
                if ((flota.RutaActualTilemap == null || flota.RutaActualTilemap.Count == 0) && flota.PosicionActual != Vector2.zero)
                {
                    CiudadData ciudadFallback = GameManager.Instance.CiudadesDisponibles
                        .FirstOrDefault(c => c.IdCiudad == flota.CiudadOrigenId);
                    if (ciudadFallback != null)
                    {
                        casillaInicio = ciudadFallback.CasillaMapamundi;
                        Vector3 posFallback = tilemap.GetCellCenterWorld(casillaInicio);
                        flota.PosicionActual = posFallback;
                        go.transform.position = posFallback;
                        flota.RutaActualTilemap = rutaCalculador.CalcularRuta(casillaInicio, flota.CasillaDestino);
                        flota.IndiceWaypointActual = 0;
                        Debug.LogWarning($"[SpawnFlota] Flota {flota.Id} reubicada a ciudad origen {ciudadFallback.NombreCiudad} por ruta inválida.");
                    }
                }

                Debug.Log($"[SpawnFlota] Flota {flota.Id} ({flota.NombrePropietario}) " +
                          $"PosicionActual={flota.PosicionActual} " +
                          $"casillaInicio={casillaInicio} " +
                          $"EsTransitable={rutaCalculador.EsTransitable(casillaInicio)} " +
                          $"CasillaDestino={flota.CasillaDestino} " +
                          $"RutaCount={flota.RutaActualTilemap?.Count ?? -1}");
            }
            else
            {
                // Sin destino: limpiar la ruta antigua que pueda haber quedado en memoria
                // para que el PNJ no la siga con índice reseteado a 0
                flota.RutaActualTilemap?.Clear();
                flota.IndiceWaypointActual = 0;
            }

            // Posicionar icono: usar PosicionActual si es válida (distinta de zero y dentro de bounds),
            // si no, usar la ciudad origen como fallback
            Vector3 posInicial;
            if (flota.PosicionActual != Vector2.zero)
            {
                posInicial = new Vector3(flota.PosicionActual.x, flota.PosicionActual.y, 0f);
            }
            else
            {
                CiudadData ciudadOrigen = GameManager.Instance.CiudadesDisponibles
                    .FirstOrDefault(c => c.IdCiudad == flota.CiudadOrigenId);
                if (ciudadOrigen == null)
                {
                    var primeras = GameManager.Instance.CiudadesDisponibles;
                    if (primeras == null || primeras.Count == 0) continue;
                    ciudadOrigen = primeras[0];
                }
                posInicial           = tilemap.GetCellCenterWorld(ciudadOrigen.CasillaMapamundi);
                flota.PosicionActual = posInicial;
            }
            go.transform.position = posInicial;

            // Reubicar piratas con posición inválida en una casilla de mar válida
            if (flota.IsPirata)
            {
                Vector3Int casilla = BuscarCasillaMarValida();
                if (casilla != Vector3Int.zero)
                {
                    Vector3 posicion      = tilemap.GetCellCenterWorld(casilla);
                    go.transform.position = posicion;
                    flota.PosicionActual  = posicion;
                }
            }

            _iconosPorId[flota.Id] = icono;
            icono.InicializarIcono();
        }
    }

    /// <summary>
    /// Busca una casilla de mar válida aleatoria en el tilemap.
    /// Prueba hasta 200 posiciones aleatorias dentro de cellBounds.
    /// Una casilla es válida si su sprite es mar abierto o costa.
    /// </summary>
    private Vector3Int BuscarCasillaMarValida()
    {
        BoundsInt bounds = tilemap.cellBounds;
        for (int i = 0; i < 200; i++)
        {
            int x = Random.Range(bounds.xMin, bounds.xMax);
            int y = Random.Range(bounds.yMin, bounds.yMax);
            Vector3Int casilla = new Vector3Int(x, y, 0);
            Sprite sprite = tilemap.GetSprite(casilla);
            if (sprite != null &&
                (sprite.name == "loonapix_17783290501031121577" || sprite.name == "Costa"))
                return casilla;
        }

        Debug.LogWarning("[MapamundiController] BuscarCasillaMarValida: no se encontró casilla de mar en 200 intentos.");
        return Vector3Int.zero;
    }

    // ─── Flota jugador ────────────────────────────────────────────────────────

    /// <summary>
    /// Instancia el icono del jugador en el mapamundi a partir de su
    /// <see cref="FlotaJugador"/>. Posiciona el icono en la ciudad actual
    /// (o en la primera ciudad disponible como fallback) e inicializa el
    /// componente <see cref="FlotaIconoMapamundi"/> para que pueda navegar
    /// por el tilemap igual que las flotas PNJ.
    /// </summary>
    private void SpawnIconoFlotaJugador()
    {
        FlotaRuntimeData flota = GameManager.Instance.FlotaJugador.ComoFlotaRuntime();

        CiudadData ciudad = GameManager.Instance.CiudadActual;
        if (ciudad == null)
        {
            var ciudades = GameManager.Instance.CiudadesDisponibles;
            if (ciudades == null || ciudades.Count == 0) return;
            ciudad = ciudades[0];
        }

        GameObject go = new GameObject("FlotaIcono_Jugador");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = spriteBarco;
        sr.color        = colorFlotaJugador;
        sr.sortingOrder = 11;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        CircleCollider2D colJugador = go.AddComponent<CircleCollider2D>();
        colJugador.radius = 0.6f;

        Vector3 posicion = tilemap.GetCellCenterWorld(ciudad.CasillaMapamundi);
        go.transform.position  = posicion;
        flota.PosicionActual   = posicion;
        flota.CiudadOrigenId   = ciudad.IdCiudad;

        FlotaIconoMapamundi icono = go.AddComponent<FlotaIconoMapamundi>();
        icono.Flota = flota;
        icono.Inicializar(tilemap, rutaCalculador);
        icono.InicializarIcono();

        FlotaRuntimeData flotaRuntime = GameManager.Instance.FlotaJugador.ComoFlotaRuntime();
        _iconosPorId[flotaRuntime.Id] = icono;
        _iconoFlotaJugador = icono;
    }

    // ─── Input por teclado ────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el CiudadData cuya CasillaMapamundi coincide con la casilla indicada,
    /// o null si no hay ninguna ciudad en esa casilla.
    /// </summary>
    /// <param name="casilla">Casilla offset del tilemap a comprobar.</param>
    public CiudadData ObtenerCiudadEnCasilla(Vector3Int casilla)
    {
        if (GameManager.Instance == null) return null;
        foreach (CiudadData ciudad in GameManager.Instance.CiudadesDisponibles)
            if (ciudad.CasillaMapamundi == casilla)
                return ciudad;
        return null;
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registra la ciudad de destino en el estado de la partida y carga
    /// la pantalla de ciudad para que el jugador interactúe con el puerto.
    /// En la beta el traslado es instantáneo; en la release incluirá animación.
    /// </summary>
    /// <param name="ciudadDestino">Datos del puerto al que viaja el jugador.</param>
    /// <summary>
    /// Abre el panel de inspección de flota con los datos de la flota indicada.
    /// Llamado desde MapamundiCamara cuando el jugador hace click sobre un icono PNJ.
    /// </summary>
    public void AbrirPanelInspeccion(FlotaRuntimeData flota)
    {
        if (panelInspeccionFlota != null)
            panelInspeccionFlota.Mostrar(flota);
    }

    public void ViajarACiudad(CiudadData ciudadDestino)
    {
        if (ciudadDestino == null) return;

        GameManager.Instance.EstablecerCiudadActual(ciudadDestino);
        SceneController.IrACiudad();
    }

    /// <summary>
    /// Abandona la partida en curso y regresa al menú principal.
    /// </summary>
    public void IrAMenuPrincipal()
    {
        SceneController.IrAMenuPrincipal();
    }

    // ─── Helpers internos ────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el icono de mapamundi asociado a la flota indicada,
    /// o <c>null</c> si no está registrado.
    /// </summary>
    /// <param name="flotaId">Identificador de la flota.</param>
    private FlotaIconoMapamundi ObtenerIcono(int flotaId)
        => _iconosPorId.TryGetValue(flotaId, out var icono) ? icono : null;

    // ─── Detección y resolución de combate ───────────────────────────────────

    /// <summary>
    /// Comprueba si la flota que acaba de moverse está lo bastante cerca de una flota
    /// enemiga (pirata vs. no-pirata) dentro del umbral de 1,5 unidades de mundo.
    /// <list type="bullet">
    ///   <item><description>
    ///     Si el jugador está involucrado (atacante o defensor coincide con
    ///     <see cref="FlotaJugador"/>), dispara <see cref="CombateEventos.OnCombateIniciado"/>
    ///     para que <see cref="EncuentroNavalUI"/> muestre el panel de decisión.
    ///   </description></item>
    ///   <item><description>
    ///     Si es un combate PNJ vs PNJ, resuelve en silencio mediante
    ///     <see cref="CombateNavalResolver.Resolver"/> y vuelca el resultado al log
    ///     sin interrumpir al jugador.
    ///   </description></item>
    /// </list>
    /// Solo se considera un combate por llamada; el primero encontrado interrumpe el bucle.
    /// </summary>
    /// <param name="flotaQueSeMovio">Flota que acaba de terminar un segmento de ruta.</param>
    public void ComprobarProximidadCombate(FlotaRuntimeData flotaQueSeMovio)
    {
        if (FlotaManager.Instance == null) return;

        foreach (FlotaRuntimeData otra in FlotaManager.Instance.ObtenerTodasLasFlotas())
        {
            if (otra.Id == flotaQueSeMovio.Id) continue;
            if (flotaQueSeMovio.IsPirata == otra.IsPirata) continue;

            float distancia = Vector2.Distance(flotaQueSeMovio.PosicionActual, otra.PosicionActual);
            if (distancia > 1.5f) continue;

            FlotaRuntimeData atacante = flotaQueSeMovio.IsPirata ? flotaQueSeMovio : otra;
            FlotaRuntimeData defensor = flotaQueSeMovio.IsPirata ? otra : flotaQueSeMovio;

            // Solo mostrar UI de combate si el jugador está involucrado
            FlotaRuntimeData flotaJugador = GameManager.Instance?.FlotaJugador?.ComoFlotaRuntime();
            bool jugadorInvolucrado = flotaJugador != null &&
                (atacante.Id == flotaJugador.Id || defensor.Id == flotaJugador.Id);

            if (jugadorInvolucrado)
            {
                CombateEventos.DispararCombate(atacante, defensor);
            }
            else
            {
                // Pausar movimiento de ambas flotas durante la resolución
                FlotaIconoMapamundi iconoAtacante = ObtenerIcono(atacante.Id);
                FlotaIconoMapamundi iconoDefensor = ObtenerIcono(defensor.Id);
                if (iconoAtacante != null) iconoAtacante.EnCombate = true;
                if (iconoDefensor != null) iconoDefensor.EnCombate = true;

                ResultadoCombate resultado = CombateNavalResolver.Resolver(
                    atacante, defensor, jugadorEsAtacante: false);
                Debug.Log($"[Combate PNJ] {atacante.NombrePropietario} vs " +
                          $"{defensor.NombrePropietario} — {resultado.TextoNarrativo}");

                // Pirata vuelve a patrullar; el brain lo retomará
                if (iconoAtacante != null)
                {
                    iconoAtacante.EnCombate = false;
                    atacante.EstadoActual = EstadoFlotaPNJ.Patrullando;
                    atacante.RutaActualTilemap?.Clear();
                }

                if (iconoDefensor != null)
                {
                    if (!defensor.EstaDestruida())
                        iconoDefensor.HuirAlPuertoMasCercano();
                    else
                    {
                        iconoDefensor.EnCombate = false;
                        iconoDefensor.gameObject.SetActive(false);
                    }
                }
            }

            break;
        }
    }
}
