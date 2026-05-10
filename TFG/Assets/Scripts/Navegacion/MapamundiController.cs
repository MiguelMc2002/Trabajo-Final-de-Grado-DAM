using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controla el mapamundi: inicializa los marcadores de ciudad visibles en el mapa
/// y gestiona la navegación del jugador hacia un puerto o al menú principal.
/// En la beta el viaje es inmediato; en la release se animará la flota sobre el mapa.
/// </summary>
public class MapamundiController : MonoBehaviour
{
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
}
