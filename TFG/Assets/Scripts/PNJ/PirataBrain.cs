using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Snapshot inmutable de la posición y estado de una flota,
/// seguro para pasar entre hilos (solo tipos primitivos y structs de valor).
/// </summary>
public struct FlotaSnapshot
{
    /// <summary>Identificador único de la flota.</summary>
    public int  Id;
    /// <summary>Coordenada X en espacio de mundo.</summary>
    public float PosX;
    /// <summary>Coordenada Y en espacio de mundo.</summary>
    public float PosY;
    /// <summary>Verdadero si la flota es pirata.</summary>
    public bool IsPirata;
    /// <summary>Verdadero si la flota ha sido destruida y no debe ser objetivo.</summary>
    public bool EstaDestruida;
}

/// <summary>Tipo de comando que el <see cref="PirataBrain"/> envía al hilo principal.</summary>
public enum ComandoPirataTipo
{
    /// <summary>Asignar una nueva lista de waypoints a la flota.</summary>
    NuevaRuta,
    /// <summary>Cambiar estado a Interceptando para perseguir un objetivo.</summary>
    InterceptarObjetivo,
    /// <summary>Volver al estado Patrullando porque el objetivo se perdió.</summary>
    Patrullar,
}

/// <summary>
/// Comando generado por el <see cref="PirataBrain"/> para ser consumido en el hilo
/// principal (Update). Contiene únicamente datos seguros entre hilos.
/// </summary>
public struct ComandoPirata
{
    /// <summary>Tipo de acción que debe aplicar el hilo principal.</summary>
    public ComandoPirataTipo  Tipo;
    /// <summary>Id del objetivo; solo válido cuando <see cref="Tipo"/> == <see cref="ComandoPirataTipo.InterceptarObjetivo"/>.</summary>
    public int                ObjetivoId;
    /// <summary>Ruta calculada; solo válido cuando <see cref="Tipo"/> == <see cref="ComandoPirataTipo.NuevaRuta"/>.</summary>
    public List<Vector3Int>   Ruta;
}

/// <summary>
/// Cerebro asíncrono de una flota pirata. Ejecuta dos <see cref="Task"/> en background:
/// <list type="bullet">
///   <item><description><b>BucleDeteccion</b>: consume snapshots de posiciones del mundo y decide si interceptar o patrullar.</description></item>
///   <item><description><b>BucleNavegacion</b>: calcula rutas A* puras sin tocar la API de Unity.</description></item>
/// </list>
/// El hilo principal solo envía snapshots y consume <see cref="ColaSalida"/> — nunca bloquea.
/// </summary>
public class PirataBrain
{
    // ─── Identificación ──────────────────────────────────────────────────────

    private readonly int   _flotaId;
    private readonly float _radioDeteccion;

    // ─── Grafo de navegación pre-calculado (inmutable tras construcción) ──────

    private readonly Dictionary<Vector3Int, List<Vector3Int>> _grafoNavegacion;
    private readonly HashSet<Vector3Int>                      _casillasCiudad;

    // ─── Comunicación thread-safe ─────────────────────────────────────────────

    private readonly ConcurrentQueue<FlotaSnapshot[]> _colaSnapshotsEntrada = new();

    /// <summary>
    /// Cola de comandos lista para ser consumida en el hilo principal (Update).
    /// Cada llamada a <c>TryDequeue</c> extrae como máximo un comando por frame.
    /// </summary>
    public readonly ConcurrentQueue<ComandoPirata> ColaSalida = new();

    private CancellationTokenSource _cts;

    // ─── Estado interno de los Tasks (solo accedidos desde ellos) ────────────

    private const float RadioAbandonoObjetivo = 12f; // unidades mundo

    private Vector2    _posPropia;
    private Vector3Int _casillaPropia;
    private bool       _tieneObjetivo;
    private int        _objetivoId;
    private Vector2    _posObjetivo;

    private readonly System.Random _rng = new();

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea el brain pero NO inicia los Tasks todavía.
    /// Llamar a <see cref="IniciarTasks"/> después de la construcción.
    /// </summary>
    /// <param name="flotaId">Id de la flota pirata que gobierna este brain.</param>
    /// <param name="radioDeteccion">Radio de detección en casillas tilemap.</param>
    /// <param name="grafoNavegacion">
    /// Grafo pre-calculado desde el hilo principal: casilla → vecinos navegables.
    /// Debe ser inmutable tras la construcción.
    /// </param>
    /// <param name="casillasCiudad">
    /// Conjunto de casillas que corresponden a ciudades; el brain las excluye como destino de patrulla.
    /// </param>
    public PirataBrain(int flotaId, float radioDeteccion,
                       Dictionary<Vector3Int, List<Vector3Int>> grafoNavegacion,
                       HashSet<Vector3Int> casillasCiudad)
    {
        _flotaId         = flotaId;
        _radioDeteccion  = radioDeteccion;
        _grafoNavegacion = grafoNavegacion;
        _casillasCiudad  = casillasCiudad;
    }

    // ─── Ciclo de vida ────────────────────────────────────────────────────────

    /// <summary>
    /// Lanza los dos Tasks de detección y navegación con un <see cref="CancellationToken"/> compartido.
    /// Debe llamarse desde el hilo principal tras la construcción.
    /// </summary>
    public void IniciarTasks()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => BucleDeteccion(_cts.Token));
        Task.Run(() => BucleNavegacion(_cts.Token));
    }

    /// <summary>
    /// Cancela los dos Tasks en background. Llamar desde <c>OnDestroy</c>.
    /// </summary>
    public void Detener()
    {
        _cts?.Cancel();
    }

    // ─── API del hilo principal ───────────────────────────────────────────────

    /// <summary>
    /// Encola un array de snapshots para que <see cref="BucleDeteccion"/> lo procese.
    /// Si la cola acumula más de 3 entradas, descarta la más antigua para evitar lag.
    /// Seguro para llamar desde cualquier hilo.
    /// </summary>
    /// <param name="snapshots">Array de snapshots del estado actual de todas las flotas.</param>
    public void EnviarSnapshot(FlotaSnapshot[] snapshots)
    {
        _colaSnapshotsEntrada.Enqueue(snapshots);
        while (_colaSnapshotsEntrada.Count > 3)
            _colaSnapshotsEntrada.TryDequeue(out _);
    }

    /// <summary>
    /// Actualiza la posición propia del pirata en el brain.
    /// Seguro para llamar desde el hilo principal en Update.
    /// </summary>
    /// <param name="posicion">Posición en espacio de mundo.</param>
    /// <param name="casilla">Posición en coordenadas offset del tilemap.</param>
    public void ActualizarPosPropia(Vector2 posicion, Vector3Int casilla)
    {
        _posPropia    = posicion;
        _casillaPropia = casilla;
    }

    // ─── Task 1: Detección ────────────────────────────────────────────────────

    /// <summary>
    /// Task de detección. Si ya tiene un objetivo comprometido, lo sigue hasta que
    /// se aleje más de <see cref="RadioAbandonoObjetivo"/> o sea destruido.
    /// Solo busca nuevo objetivo cuando <c>_tieneObjetivo == false</c>.
    /// </summary>
    private void BucleDeteccion(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_colaSnapshotsEntrada.TryDequeue(out FlotaSnapshot[] snapshots))
            {
                if (_tieneObjetivo)
                {
                    // Mantener o abandonar el objetivo actual
                    bool encontrado = false;
                    foreach (FlotaSnapshot snap in snapshots)
                    {
                        if (snap.Id != _objetivoId) continue;

                        float dx   = snap.PosX - _posPropia.x;
                        float dy   = snap.PosY - _posPropia.y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                        if (snap.EstaDestruida || dist > RadioAbandonoObjetivo)
                        {
                            // Objetivo perdido o destruido — volver a patrullar
                            _tieneObjetivo = false;
                            _objetivoId    = 0;
                            ColaSalida.Enqueue(new ComandoPirata { Tipo = ComandoPirataTipo.Patrullar });
                        }
                        else
                        {
                            // Actualizar posición del objetivo comprometido
                            _posObjetivo = new Vector2(snap.PosX, snap.PosY);
                        }

                        encontrado = true;
                        break;
                    }

                    // Si el snapshot no incluye al objetivo, no cambiar de estado (puede ser delay)
                    _ = encontrado;
                }
                else
                {
                    // Buscar el comerciante más cercano dentro del radio
                    FlotaSnapshot? mejorObjetivo = null;
                    float          mejorDist     = float.MaxValue;

                    foreach (FlotaSnapshot snap in snapshots)
                    {
                        if (snap.Id == _flotaId)    continue;
                        if (snap.IsPirata)          continue;
                        if (snap.EstaDestruida)     continue;

                        float dx   = snap.PosX - _posPropia.x;
                        float dy   = snap.PosY - _posPropia.y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                        // 1 casilla ≈ 1.0f unidades mundo
                        if (dist < _radioDeteccion * 1.0f && dist < mejorDist)
                        {
                            mejorDist     = dist;
                            mejorObjetivo = snap;
                        }
                    }

                    if (mejorObjetivo != null)
                    {
                        _tieneObjetivo = true;
                        _objetivoId    = mejorObjetivo.Value.Id;
                        _posObjetivo   = new Vector2(mejorObjetivo.Value.PosX, mejorObjetivo.Value.PosY);
                        ColaSalida.Enqueue(new ComandoPirata
                        {
                            Tipo       = ComandoPirataTipo.InterceptarObjetivo,
                            ObjetivoId = _objetivoId,
                        });
                    }
                }
            }

            Thread.Sleep(150);
        }
    }

    // ─── Task 2: Navegación ───────────────────────────────────────────────────

    private void BucleNavegacion(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!_tieneObjetivo)
            {
                Vector3Int destino = ElegirDestinoPatrulla();
                if (destino != Vector3Int.zero)
                {
                    List<Vector3Int> ruta = CalcularRutaAEstrella(_casillaPropia, destino);
                    if (ruta.Count > 0)
                    {
                        ColaSalida.Enqueue(new ComandoPirata
                        {
                            Tipo = ComandoPirataTipo.NuevaRuta,
                            Ruta = ruta,
                        });
                        Thread.Sleep(ruta.Count * 200);
                        continue;
                    }
                }
            }
            else
            {
                // Recalcular ruta hacia posición actual del objetivo cada 300ms
                Vector3Int destinoObj = MundoACasillaLocal(_posObjetivo);
                if (destinoObj != Vector3Int.zero && _grafoNavegacion.ContainsKey(destinoObj))
                {
                    List<Vector3Int> rutaObj = CalcularRutaAEstrella(_casillaPropia, destinoObj);
                    if (rutaObj.Count > 0)
                    {
                        // Limpiar comandos de ruta anteriores para no acumular rutas obsoletas
                        while (ColaSalida.TryDequeue(out _)) { }
                        ColaSalida.Enqueue(new ComandoPirata
                        {
                            Tipo = ComandoPirataTipo.NuevaRuta,
                            Ruta = rutaObj,
                        });
                    }
                }
                Thread.Sleep(300);
                continue;
            }

            Thread.Sleep(500);
        }
    }

    // ─── Helpers puros (sin API Unity) ───────────────────────────────────────

    /// <summary>
    /// Elige una casilla de mar aleatoria como destino de patrulla.
    /// Usa las keys del grafo de navegación, filtrando casillas de ciudad.
    /// Usa <see cref="System.Random"/> en lugar de <c>UnityEngine.Random</c> (no thread-safe).
    /// </summary>
    /// <returns>Casilla destino en coordenadas offset, o <see cref="Vector3Int.zero"/> si no hay candidatos.</returns>
    private Vector3Int ElegirDestinoPatrulla()
    {
        var candidatos = new List<Vector3Int>(_grafoNavegacion.Count);
        foreach (Vector3Int casilla in _grafoNavegacion.Keys)
            if (!_casillasCiudad.Contains(casilla))
                candidatos.Add(casilla);

        if (candidatos.Count == 0) return Vector3Int.zero;
        return candidatos[_rng.Next(candidatos.Count)];
    }

    /// <summary>
    /// A* hexagonal usando únicamente el grafo pre-calculado.
    /// No accede a ninguna API de Unity en runtime.
    /// </summary>
    /// <param name="origen">Casilla de inicio en coordenadas offset.</param>
    /// <param name="destino">Casilla de destino en coordenadas offset.</param>
    /// <returns>Lista de casillas en orden de recorrido, o lista vacía si no hay ruta.</returns>
    private List<Vector3Int> CalcularRutaAEstrella(Vector3Int origen, Vector3Int destino)
    {
        if (!_grafoNavegacion.ContainsKey(origen) || !_grafoNavegacion.ContainsKey(destino))
            return new List<Vector3Int>();

        var abiertos  = new MinHeap();
        var cameFrom  = new Dictionary<Vector3Int, Vector3Int>();
        var gScore    = new Dictionary<Vector3Int, float> { [origen] = 0f };

        abiertos.Enqueue(origen, HeuristicaCube(origen, destino));

        while (abiertos.Count > 0)
        {
            Vector3Int actual = abiertos.Dequeue();

            if (actual == destino)
                return ReconstruirCamino(cameFrom, actual);

            if (!_grafoNavegacion.TryGetValue(actual, out List<Vector3Int> vecinos)) continue;

            foreach (Vector3Int vecino in vecinos)
            {
                float g = gScore.GetValueOrDefault(actual, float.MaxValue) + 1f;
                if (g < gScore.GetValueOrDefault(vecino, float.MaxValue))
                {
                    cameFrom[vecino] = actual;
                    gScore[vecino]   = g;
                    abiertos.Enqueue(vecino, g + HeuristicaCube(vecino, destino));
                }
            }
        }

        return new List<Vector3Int>();
    }

    /// <summary>
    /// Heurística de distancia hexagonal en coordenadas cube.
    /// No usa API de Unity.
    /// </summary>
    private static float HeuristicaCube(Vector3Int a, Vector3Int b)
    {
        // Conversión offset → cube (filas pares)
        static Vector3Int OffsetACube(Vector3Int o)
        {
            int x = o.x - (o.y - (o.y & 1)) / 2;
            int z = o.y;
            return new Vector3Int(x, -x - z, z);
        }

        Vector3Int ca = OffsetACube(a);
        Vector3Int cb = OffsetACube(b);
        return Math.Max(Math.Abs(ca.x - cb.x),
               Math.Max(Math.Abs(ca.y - cb.y),
                        Math.Abs(ca.z - cb.z)));
    }

    /// <summary>
    /// Aproximación de conversión mundo → casilla para uso en el Task de navegación.
    /// La conversión exacta la realiza Unity en el hilo principal.
    /// </summary>
    /// <param name="posicion">Posición en espacio de mundo.</param>
    /// <returns>Casilla en coordenadas offset aproximada.</returns>
    private static Vector3Int MundoACasillaLocal(Vector2 posicion)
    {
        int x = Mathf.RoundToInt(posicion.x / 1.5f);
        int y = Mathf.RoundToInt(posicion.y / 1.3f);
        return new Vector3Int(x, y, 0);
    }

    private static List<Vector3Int> ReconstruirCamino(
        Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int actual)
    {
        var camino = new List<Vector3Int>();
        while (cameFrom.ContainsKey(actual))
        {
            camino.Add(actual);
            actual = cameFrom[actual];
        }
        camino.Add(actual);
        camino.Reverse();
        return camino;
    }

    // ─── MinHeap privado ─────────────────────────────────────────────────────

    private class MinHeap
    {
        private readonly List<(float prioridad, Vector3Int elemento)> _datos = new();

        public int Count => _datos.Count;

        public void Enqueue(Vector3Int elemento, float prioridad)
        {
            _datos.Add((prioridad, elemento));
            int i = _datos.Count - 1;
            while (i > 0)
            {
                int padre = (i - 1) / 2;
                if (_datos[padre].prioridad <= _datos[i].prioridad) break;
                (_datos[padre], _datos[i]) = (_datos[i], _datos[padre]);
                i = padre;
            }
        }

        public Vector3Int Dequeue()
        {
            Vector3Int raiz  = _datos[0].elemento;
            int        ultimo = _datos.Count - 1;
            _datos[0] = _datos[ultimo];
            _datos.RemoveAt(ultimo);
            int i = 0;
            while (true)
            {
                int izq   = 2 * i + 1;
                int der   = 2 * i + 2;
                int menor = i;
                if (izq < _datos.Count && _datos[izq].prioridad < _datos[menor].prioridad) menor = izq;
                if (der < _datos.Count && _datos[der].prioridad < _datos[menor].prioridad) menor = der;
                if (menor == i) break;
                (_datos[i], _datos[menor]) = (_datos[menor], _datos[i]);
                i = menor;
            }
            return raiz;
        }
    }
}
