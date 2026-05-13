using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static ResultadoCombate;

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

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        foreach (MarcadorCiudad marcador in Ciudades)
            marcador.Inicializar(this);

        SpawnIconosFlotas();
    }

    // ─── Flotas PNJ ──────────────────────────────────────────────────────────

    /// <summary>
    /// Instancia un icono en el mapamundi por cada flota PNJ activa registrada
    /// en el <see cref="FlotaManager"/>. Cada icono recibe su referencia al
    /// tilemap y al calculador A* para poder navegar de forma autónoma.
    /// </summary>
    private void SpawnIconosFlotas()
    {
        if (FlotaManager.Instance == null)
        {
            Debug.LogWarning("[MapamundiController] FlotaManager.Instance es null — no se crean iconos de flotas.");
            return;
        }

        foreach (FlotaRuntimeData flota in FlotaManager.Instance.ObtenerTodasLasFlotas())
        {
            GameObject go = new GameObject("FlotaIcono_" + flota.Id);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spriteBarco;
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

            icono.InicializarIcono();
        }
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
        if (ciudadDestino == null)
        {
            Debug.LogError("[MapamundiController] ViajarACiudad recibió un CiudadData nulo.");
            return;
        }

        Debug.Log($"[MapamundiController] Viajando a {ciudadDestino.NombreCiudad}...");
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

    // ─── Detección y resolución de combate ───────────────────────────────────

    /// <summary>
    /// Comprueba si la flota que acaba de moverse está lo bastante cerca de otra flota
    /// enemiga (pirata vs. no-pirata) para desencadenar un combate automático.
    /// Solo se considera un combate por llamada; el primero encontrado interrumpe el bucle.
    /// </summary>
    /// <param name="flotaQueSeMovio">Flota que acaba de terminar un segmento de ruta.</param>
    public void ComprobarProximidadCombate(FlotaRuntimeData flotaQueSeMovio)
    {
        if (FlotaManager.Instance == null) return;

        foreach (FlotaRuntimeData otra in FlotaManager.Instance.ObtenerTodasLasFlotas())
        {
            if (otra.Id == flotaQueSeMovio.Id) continue;
            if (flotaQueSeMovio.IsPirata == otra.IsPirata) continue; // solo pirata vs. no-pirata

            float distancia = Vector2.Distance(flotaQueSeMovio.PosicionActual, otra.PosicionActual);
            if (distancia > 1.5f) continue;

            FlotaRuntimeData pirata  = flotaQueSeMovio.IsPirata ? flotaQueSeMovio : otra;
            FlotaRuntimeData victima = flotaQueSeMovio.IsPirata ? otra : flotaQueSeMovio;
            TriggerCombate(pirata, victima);
            break;
        }
    }

    /// <summary>
    /// Resuelve el combate naval entre pirata y víctima, aplica los cambios de estado
    /// a ambas flotas y reanuda la simulación al terminar.
    /// </summary>
    /// <param name="pirata">Flota atacante.</param>
    /// <param name="victima">Flota defensora.</param>
    private void TriggerCombate(FlotaRuntimeData pirata, FlotaRuntimeData victima)
    {
        SimulacionTiempo.Instance?.PausarPorMenu();

        var rng = new System.Random();
        ResultadoCombate resultado = CombateNavalResolver.Resolver(pirata, victima, rng);
        Debug.Log($"[Combate] {resultado.Descripcion}");

        switch (resultado.Desenlace)
        {
            case DesenlaceCombate.ComercianteEscapa:
                // Sin cambios. Log ya hecho.
                break;

            case DesenlaceCombate.Rendicion:
                // Aplicar botín completo al pirata
                foreach (var kvp in resultado.BotonCapturado)
                    pirata.Carga[kvp.Key] = pirata.Carga.ContainsKey(kvp.Key) ? pirata.Carga[kvp.Key] + kvp.Value : kvp.Value;
                victima.Carga.Clear();
                victima.NumBarcos = 0;
                FlotaManager.Instance.CambiarEstado(victima.Id, EstadoFlotaPNJ.Huyendo);
                break;

            case DesenlaceCombate.PirataGana:
                pirata.NumBarcos  = Mathf.Max(0, pirata.NumBarcos  - resultado.BarcosHundidosAtacante);
                pirata.VidaActual = resultado.VidaFinalAtacante;
                victima.NumBarcos = Mathf.Max(0, victima.NumBarcos - resultado.BarcosHundidosDefensor - resultado.BarcosCapturedDefensor);
                victima.VidaActual = resultado.VidaFinalDefensor;

                foreach (var kvp in resultado.BotonCapturado)
                {
                    pirata.Carga[kvp.Key] = pirata.Carga.ContainsKey(kvp.Key) ? pirata.Carga[kvp.Key] + kvp.Value : kvp.Value;
                    if (victima.Carga.ContainsKey(kvp.Key))
                        victima.Carga[kvp.Key] = Mathf.Max(0, victima.Carga[kvp.Key] - kvp.Value);
                }

                FlotaManager.Instance.CambiarEstado(victima.Id, EstadoFlotaPNJ.Huyendo);

                // Teleport víctima a su ciudad origen como refugio
                CiudadData ciudadRefugio = null;
                foreach (CiudadData c in GameManager.Instance.CiudadesDisponibles)
                    if (c.IdCiudad == victima.CiudadOrigenId) { ciudadRefugio = c; break; }

                if (ciudadRefugio != null)
                    victima.PosicionActual = tilemap.GetCellCenterWorld(ciudadRefugio.CasillaMapamundi);
                else
                    victima.PosicionActual += new Vector2(5f, 5f);

                victima.RutaActualTilemap?.Clear();
                victima.IndiceWaypointActual = 0;
                break;

            case DesenlaceCombate.ComercianteGana:
                pirata.NumBarcos   = Mathf.Max(0, pirata.NumBarcos  - resultado.BarcosHundidosAtacante);
                pirata.VidaActual  = resultado.VidaFinalAtacante;
                victima.VidaActual = resultado.VidaFinalDefensor;
                FlotaManager.Instance.CambiarEstado(pirata.Id, EstadoFlotaPNJ.Huyendo);
                pirata.PosicionActual += new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
                pirata.RutaActualTilemap?.Clear();
                pirata.IndiceWaypointActual = 0;
                break;

            case DesenlaceCombate.Empate:
                pirata.NumBarcos   = Mathf.Max(0, pirata.NumBarcos  - resultado.BarcosHundidosAtacante);
                pirata.VidaActual  = resultado.VidaFinalAtacante;
                victima.NumBarcos  = Mathf.Max(0, victima.NumBarcos - resultado.BarcosHundidosDefensor);
                victima.VidaActual = resultado.VidaFinalDefensor;
                FlotaManager.Instance.CambiarEstado(pirata.Id,  EstadoFlotaPNJ.Huyendo);
                FlotaManager.Instance.CambiarEstado(victima.Id, EstadoFlotaPNJ.Huyendo);
                break;
        }

        SimulacionTiempo.Instance?.ReanudarDesdMenu();
    }
}
