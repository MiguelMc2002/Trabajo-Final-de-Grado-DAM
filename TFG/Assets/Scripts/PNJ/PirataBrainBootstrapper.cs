using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// MonoBehaviour responsable de construir el grafo de navegación en el hilo principal
/// (donde <c>Tilemap.GetSprite</c> es seguro) y arrancarlo en cada <see cref="PirataBrain"/>.
/// Adjuntar al mismo GameObject que <see cref="MapamundiController"/> en la escena Mapamundi
/// y asignar los campos desde el Inspector.
/// </summary>
public class PirataBrainBootstrapper : MonoBehaviour
{
    [SerializeField] private Tilemap              _tilemap;
    [SerializeField] private RutaCalculadorTilemap _rutaCalculador;

    private readonly List<PirataBrain> _brains = new();

    private void Start()
    {
        StartCoroutine(IniciarBrainsNextFrame());
    }

    /// <summary>
    /// Espera un frame para que <see cref="FlotaManager"/> haya registrado todas las flotas,
    /// construye el grafo y arranca un <see cref="PirataBrain"/> por cada flota pirata.
    /// </summary>
    private IEnumerator IniciarBrainsNextFrame()
    {
        yield return null;

        Dictionary<Vector3Int, List<Vector3Int>> grafo = ConstruirGrafo();
        HashSet<Vector3Int> casillasCiudad = ObtenerCasillasCiudad();

        if (FlotaManager.Instance == null)
        {
            Debug.LogWarning("[PirataBrainBootstrapper] FlotaManager.Instance es null — no se crean brains.");
            yield break;
        }

        foreach (FlotaRuntimeData flota in FlotaManager.Instance.ObtenerTodasLasFlotas())
        {
            if (!flota.IsPirata) continue;

            var brain = new PirataBrain(flota.Id, radioDeteccion: 2f, grafo, casillasCiudad);
            brain.IniciarTasks();
            _brains.Add(brain);
            FlotaManager.Instance.RegistrarPirataBrain(flota.Id, brain);
        }

        Debug.Log($"[PirataBrainBootstrapper] {_brains.Count} brains iniciados.");
    }

    /// <summary>
    /// Construye el grafo de navegación recorriendo todas las casillas del tilemap
    /// y registrando sus vecinos navegables. Solo casillas de mar, costa y ciudad
    /// son incluidas (las ciudades se excluirán como destino vía <c>casillasCiudad</c>).
    /// Debe ejecutarse en el hilo principal.
    /// </summary>
    /// <returns>Diccionario casilla → vecinos navegables en coordenadas offset.</returns>
    private Dictionary<Vector3Int, List<Vector3Int>> ConstruirGrafo()
    {
        var grafo = new Dictionary<Vector3Int, List<Vector3Int>>();

        foreach (Vector3Int pos in _tilemap.cellBounds.allPositionsWithin)
        {
            Sprite sprite = _tilemap.GetSprite(pos);
            if (sprite == null) continue;

            bool esNavegable = sprite.name == "loonapix_17783290501031121577"
                            || sprite.name == "Costa"
                            || sprite.name == "medieval_openCastle_0";

            if (!esNavegable) continue;

            grafo[pos] = _rutaCalculador.GetVecinosNavegables(pos);
        }

        Debug.Log($"[PirataBrainBootstrapper] Grafo construido: {grafo.Count} casillas navegables.");
        return grafo;
    }

    /// <summary>
    /// Devuelve el conjunto de casillas tilemap ocupadas por ciudades del juego.
    /// Usado por el brain para excluir puertos como destinos de patrulla.
    /// </summary>
    private HashSet<Vector3Int> ObtenerCasillasCiudad()
    {
        var set = new HashSet<Vector3Int>();
        if (GameManager.Instance == null) return set;

        foreach (CiudadData ciudad in GameManager.Instance.CiudadesDisponibles)
            set.Add(ciudad.CasillaMapamundi);

        return set;
    }

    private void OnDestroy()
    {
        foreach (PirataBrain brain in _brains)
            brain.Detener();
    }
}
