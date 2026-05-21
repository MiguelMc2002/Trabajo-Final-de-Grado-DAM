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

    private FlotaIconoMapamundi _iconoFlotaJugador;

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

            FlotaIconoMapamundi icono = go.AddComponent<FlotaIconoMapamundi>();
            icono.Flota = flota;
            icono.Inicializar(tilemap, rutaCalculador);

            // Calcular ruta si la flota tiene destino asignado
            if (flota.CasillaDestino != Vector3Int.zero)
            {
                CiudadData ciudadOrigen = GameManager.Instance.CiudadesDisponibles
                    .FirstOrDefault(c => c.IdCiudad == flota.CiudadOrigenId);
                if (ciudadOrigen != null)
                {
                    flota.RutaActualTilemap = rutaCalculador.CalcularRuta(
                        ciudadOrigen.CasillaMapamundi, flota.CasillaDestino);
                }
            }

            // Posicionar el icono en la casilla origen si PosicionActual es zero
            if (flota.PosicionActual == Vector2.zero)
            {
                CiudadData ciudadOrigen = GameManager.Instance.CiudadesDisponibles
                    .FirstOrDefault(c => c.IdCiudad == flota.CiudadOrigenId);
                if (ciudadOrigen != null)
                {
                    Vector3 posInicial = tilemap.GetCellCenterWorld(ciudadOrigen.CasillaMapamundi);
                    go.transform.position = posInicial;
                    flota.PosicionActual  = posInicial;
                }
            }

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
    /// Detecta atajos de teclado del mapamundi.
    /// M → viaja directamente a la última ciudad visitada, si existe.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (GameManager.Instance != null && GameManager.Instance.UltimaCiudad != null)
                ViajarACiudad(GameManager.Instance.UltimaCiudad);
        }
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registra la ciudad de destino en el estado de la partida y carga
    /// la pantalla de ciudad para que el jugador interactúe con el puerto.
    /// En la beta el traslado es instantáneo; en la release incluirá animación.
    /// </summary>
    /// <param name="ciudadDestino">Datos del puerto al que viaja el jugador.</param>
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
