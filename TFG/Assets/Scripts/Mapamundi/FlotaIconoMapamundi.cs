using System.Collections;
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

    /// <summary>
    /// Cuando es true, el icono detiene su movimiento en el sitio.
    /// Se activa durante la resolución de un combate y se desactiva al terminar.
    /// </summary>
    public bool EnCombate { get; set; }

    public bool _esJugador;

    /// <summary>
    /// Se dispara cada vez que la flota del jugador cruza el centro de un waypoint.
    /// El parámetro es la casilla offset que acaba de cruzarse.
    /// NavegacionJugadorController se suscribe para detectar llegadas a ciudad.
    /// </summary>
    public static event System.Action<Vector3Int> OnWaypointJugadorCruzado;

    /// <summary>
    /// Se dispara cuando el jugador hace click derecho sobre el icono de una flota PNJ.
    /// El parámetro es la FlotaRuntimeData de la flota clickada.
    /// PanelInspeccionFlota se suscribe para mostrar los barcos de esa flota.
    /// </summary>
    public static event System.Action<FlotaRuntimeData> OnFlotaClickada;

    private Collider2D _collider;

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

        if (Flota.IsPirata)
            _coroutineDeteccion = StartCoroutine(BucleDeteccionContinua());

        _esJugador = Flota != null && !Flota.IsPirata && Flota.Id == -1;

        _collider = gameObject.GetComponent<Collider2D>();
        if (_collider == null)
        {
            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
            _collider  = col;
        }
    }

    private void OnMouseDown()
    {
        if (_esJugador) return;
        FlotaIconoMapamundi.OnFlotaClickada?.Invoke(Flota);
    }

    /// <summary>Permite invocar OnFlotaClickada desde fuera de la clase.</summary>
    public static void InvocarFlotaClickada(FlotaRuntimeData flota) => OnFlotaClickada?.Invoke(flota);

    /// <summary>
    /// Asigna una nueva ruta al icono, cancelando la ruta anterior inmediatamente.
    /// Permite redirigir la flota del jugador en mitad de un viaje con un nuevo click.
    /// </summary>
    /// <param name="nuevaRuta">Lista de casillas offset que forman el camino.</param>
    public void AsignarRuta(List<Vector3Int> nuevaRuta)
    {
        if (nuevaRuta == null || nuevaRuta.Count == 0) return;
        Flota.RutaActualTilemap    = nuevaRuta;
        Flota.IndiceWaypointActual = 0;
        Flota.CasillaDestino       = nuevaRuta[nuevaRuta.Count - 1];
        Flota.EstadoActual         = EstadoFlotaPNJ.Viajando;
    }

    /// <summary>Referencia a la coroutine de detección continua, para poder detenerla en OnDestroy.</summary>
    private Coroutine _coroutineDeteccion;

    /// <summary>Verdadero mientras la coroutine de detección está buscando presas activamente.</summary>
    private bool _deteccionActiva;

    private void Update()
    {
        if (Flota == null || _tilemap == null || _rutaCalculador == null) return;
        if (SimulacionTiempo.Instance != null && SimulacionTiempo.Instance.EstaPausado) return;
        if (EnCombate) return;

        // ── Pirata: actualizar brain y consumir comandos ──────────────────────
        if (Flota.IsPirata)
        {
            PirataBrain brain = FlotaManager.Instance?.ObtenerPirataBrain(Flota.Id);
            if (brain != null)
            {
                // 1. Actualizar posición propia en el brain
                Vector3Int casillaPropia = _tilemap.WorldToCell(transform.position);
                brain.ActualizarPosPropia(transform.position, casillaPropia);

                // 2. Construir y enviar snapshot de todas las flotas
                IReadOnlyCollection<FlotaRuntimeData> flotas = FlotaManager.Instance.ObtenerTodasLasFlotas();
                var snapshots = new FlotaSnapshot[flotas.Count];
                int i = 0;
                foreach (FlotaRuntimeData f in flotas)
                {
                    snapshots[i++] = new FlotaSnapshot
                    {
                        Id           = f.Id,
                        PosX         = f.PosicionActual.x,
                        PosY         = f.PosicionActual.y,
                        IsPirata     = f.IsPirata,
                        EstaDestruida = f.EstaDestruida(),
                    };
                }
                brain.EnviarSnapshot(snapshots);

                // 3. Consumir máximo 1 comando por frame para no bloquear
                if (brain.ColaSalida.TryDequeue(out ComandoPirata cmd))
                {
                    switch (cmd.Tipo)
                    {
                        case ComandoPirataTipo.NuevaRuta:
                            if (cmd.Ruta != null && cmd.Ruta.Count > 0)
                            {
                                Flota.RutaActualTilemap    = cmd.Ruta;
                                Flota.IndiceWaypointActual = 0;
                                Flota.CasillaDestino       = cmd.Ruta[cmd.Ruta.Count - 1];
                            }
                            break;
                        case ComandoPirataTipo.InterceptarObjetivo:
                            Flota.EstadoActual = EstadoFlotaPNJ.Interceptando;
                            Debug.Log($"[FlotaIcono] Pirata {Flota.Id} interceptando objetivo {cmd.ObjetivoId}");
                            break;
                        case ComandoPirataTipo.Patrullar:
                            Flota.EstadoActual = EstadoFlotaPNJ.Patrullando;
                            break;
                    }
                }
            }
        }

        float velocidad = velocidadBase * Time.deltaTime *
            (SimulacionTiempo.Instance != null ? SimulacionTiempo.Instance.VelocidadActual : 1f);

        // Si no hay ruta pero está viajando, calcularla
        if (Flota.RutaActualTilemap == null || Flota.RutaActualTilemap.Count == 0 || Flota.IndiceWaypointActual >= Flota.RutaActualTilemap.Count)
        {
            if (Flota.CasillaDestino != Vector3Int.zero &&
                (Flota.EstadoActual == EstadoFlotaPNJ.Viajando ||
                Flota.EstadoActual == EstadoFlotaPNJ.Patrullando ||
                Flota.EstadoActual == EstadoFlotaPNJ.Interceptando ||
                Flota.EstadoActual == EstadoFlotaPNJ.HuyendoAPuerto))
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
            if (_esJugador && Flota.RutaActualTilemap != null && Flota.IndiceWaypointActual - 1 >= 0)
                OnWaypointJugadorCruzado?.Invoke(Flota.RutaActualTilemap[Flota.IndiceWaypointActual - 1]);
            if (Flota.IndiceWaypointActual >= Flota.RutaActualTilemap.Count)
            {
                // Forzar llegada a destino en la lógica de negocio
                Flota.CiudadOrigenId = Flota.CiudadDestinoId;
                Flota.CasillaDestino = Vector3Int.zero;
                if (Flota.EstadoActual == EstadoFlotaPNJ.Viajando)
                    Flota.EstadoActual = EstadoFlotaPNJ.Comerciando;
                else if (Flota.EstadoActual == EstadoFlotaPNJ.HuyendoAPuerto)
                    Flota.EstadoActual = EstadoFlotaPNJ.EnPuerto;
                Flota.RutaActualTilemap.Clear();
                Flota.IndiceWaypointActual = 0;

                // Solo comerciantes y jugador comprueban proximidad al llegar al destino.
                // Los piratas tienen BucleDeteccionContinua corriendo en paralelo.
                if (!Flota.IsPirata && MapamundiController.Instance != null)
                    MapamundiController.Instance.ComprobarProximidadCombate(Flota);
            }
        }
    }

    /// <summary>
    /// Coroutine exclusiva de piratas. Comprueba proximidad con flotas
    /// enemigas cada 0.1 segundos mientras el estado es Patrullando.
    /// Cuando detecta una presa llama a <see cref="MapamundiController.ComprobarProximidadCombate"/>
    /// y se suspende hasta que el pirata vuelva a Patrullar.
    /// </summary>
    private IEnumerator BucleDeteccionContinua()
    {
        var espera = new WaitForSeconds(0.1f);
        while (true)
        {
            if (Flota != null
                && Flota.EstadoActual == EstadoFlotaPNJ.Patrullando
                && (SimulacionTiempo.Instance == null || !SimulacionTiempo.Instance.EstaPausado))
            {
                float            radioMundo  = 3f;
                FlotaRuntimeData masCercana  = null;
                float            mejorDist   = float.MaxValue;

                foreach (FlotaRuntimeData candidato in FlotaManager.Instance.ObtenerTodasLasFlotas())
                {
                    if (candidato.IsPirata)        continue;
                    if (candidato.EstaDestruida()) continue;

                    float dist = Vector2.Distance(Flota.PosicionActual, candidato.PosicionActual);
                    if (dist < radioMundo && dist < mejorDist)
                    {
                        mejorDist  = dist;
                        masCercana = candidato;
                    }
                }

                if (masCercana != null)
                {
                    Debug.Log($"[DeteccionContinua] Pirata {Flota.Id} detectó a {masCercana.NombrePropietario} " +
                              $"a {mejorDist:F2}u. Disparando combate.");

                    if (MapamundiController.Instance != null)
                        MapamundiController.Instance.ComprobarProximidadCombate(Flota);

                    // Suspender detección hasta volver a Patrullando
                    yield return new WaitUntil(() =>
                        Flota == null ||
                        Flota.EstadoActual == EstadoFlotaPNJ.Patrullando);
                }
            }

            yield return espera;
        }
    }

    /// <summary>
    /// Ordena a la flota navegar hacia la ciudad más cercana en estado
    /// HuyendoAPuerto. Llamar tras un combate PNJ en el que la flota sobrevive.
    /// </summary>
    public void HuirAlPuertoMasCercano()
    {
        if (_rutaCalculador == null || GameManager.Instance == null) return;

        CiudadData ciudadMasCercana = null;
        float mejorDist = float.MaxValue;
        Vector3Int casillaActual = _tilemap.WorldToCell(transform.position);

        foreach (CiudadData ciudad in GameManager.Instance.CiudadesDisponibles)
        {
            float dist = Vector3Int.Distance(casillaActual, ciudad.CasillaMapamundi);
            if (dist < mejorDist)
            {
                mejorDist        = dist;
                ciudadMasCercana = ciudad;
            }
        }

        if (ciudadMasCercana == null) return;

        List<Vector3Int> ruta = _rutaCalculador.CalcularRuta(
            casillaActual, ciudadMasCercana.CasillaMapamundi);

        if (ruta == null || ruta.Count == 0) return;

        Flota.RutaActualTilemap    = ruta;
        Flota.IndiceWaypointActual = 0;
        Flota.CasillaDestino       = ciudadMasCercana.CasillaMapamundi;
        Flota.CiudadDestinoId      = ciudadMasCercana.IdCiudad;
        Flota.EstadoActual         = EstadoFlotaPNJ.HuyendoAPuerto;
        EnCombate                  = false;
    }

    private void OnDestroy()
    {
        if (_coroutineDeteccion != null)
            StopCoroutine(_coroutineDeteccion);
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
