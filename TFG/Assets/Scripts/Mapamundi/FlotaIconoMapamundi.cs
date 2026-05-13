using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlotaIconoMapamundi : MonoBehaviour
{
    private Tilemap _tilemap;
    private RutaCalculadorTilemap _rutaCalculador;
    private SpriteRenderer _sr;

    [SerializeField] private float velocidadBase = 2f;

    public FlotaRuntimeData Flota;

    public void Inicializar(Tilemap t, RutaCalculadorTilemap r)
    {
        _tilemap        = t;
        _rutaCalculador = r;
    }

    public void InicializarIcono()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
        Flota.IndiceWaypointActual = 0;
    }

    private void Update()
    {
        if (Flota == null || _tilemap == null || _rutaCalculador == null) return;
        if (SimulacionTiempo.Instance != null && SimulacionTiempo.Instance.EstaPausado) return;

        float velocidad = velocidadBase * Time.deltaTime *
            (SimulacionTiempo.Instance != null ? SimulacionTiempo.Instance.VelocidadActual : 1f);

        // Si no hay ruta pero está viajando, calcularla
        if (Flota.RutaActualTilemap == null || Flota.RutaActualTilemap.Count == 0 || Flota.IndiceWaypointActual >= Flota.RutaActualTilemap.Count)
        {
            if (Flota.EstadoActual == EstadoFlotaPNJ.Viajando && Flota.CasillaDestino != Vector3Int.zero)
            {
                Vector3Int casillaActual = _tilemap.WorldToCell(transform.position);
                if (casillaActual != Flota.CasillaDestino)
                {
                    Flota.RutaActualTilemap    = _rutaCalculador.CalcularRuta(casillaActual, Flota.CasillaDestino);
                    Flota.IndiceWaypointActual = 0;
                }
            }
            return;
        }

        Vector3 objetivo = _tilemap.GetCellCenterWorld(Flota.RutaActualTilemap[Flota.IndiceWaypointActual]);
        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad);
        Flota.PosicionActual = transform.position;

        // Flip sprite
        if (_sr != null)
        {
            float dirX = objetivo.x - transform.position.x;
            if (Mathf.Abs(dirX) > 0.001f)
                _sr.flipX = dirX < 0f;
        }

        // Avanzar waypoint
        if (Vector3.Distance(transform.position, objetivo) < 0.01f)
        {
            Flota.IndiceWaypointActual++;
            if (Flota.IndiceWaypointActual >= Flota.RutaActualTilemap.Count)
            {
                // Forzar llegada a destino en la lógica de negocio
                Flota.CiudadOrigenId = Flota.CiudadDestinoId;
                Flota.CasillaDestino = Vector3Int.zero;
                if (Flota.EstadoActual == EstadoFlotaPNJ.Viajando)
                {
                    Flota.EstadoActual = EstadoFlotaPNJ.Comerciando;
                    Debug.Log($"[FlotaIcono] Flota {Flota.Id} llegó a ciudad {Flota.CiudadOrigenId} — cambiando a Comerciando");
                }
                Flota.RutaActualTilemap.Clear();
                Flota.IndiceWaypointActual = 0;

                if (MapamundiController.Instance != null)
                    MapamundiController.Instance.ComprobarProximidadCombate(Flota);
            }
        }
    }

    /// <summary>Devuelve la casilla offset de la ciudad origen de la flota.</summary>
    public Vector3Int CasillaOrigenDesdeFlota()
    {
        if (GameManager.Instance == null) return Vector3Int.zero;
        foreach (CiudadData ciudad in GameManager.Instance.CiudadesDisponibles)
            if (ciudad.IdCiudad == Flota.CiudadOrigenId)
                return ciudad.CasillaMapamundi;
        return Vector3Int.zero;
    }
}
