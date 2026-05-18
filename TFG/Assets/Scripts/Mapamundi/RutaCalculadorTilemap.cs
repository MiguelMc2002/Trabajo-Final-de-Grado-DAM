using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Calcula rutas A* sobre el tilemap hexagonal Pointy Top del mapamundi.
/// Adjuntar a un GameObject vacío en la escena Mapamundi y asignar el Tilemap desde el Inspector.
/// </summary>
public class RutaCalculadorTilemap : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;

    // Costes por nombre de sprite
    private const float CosteMarAbierto   = 1.0f;
    private const float CosteCosta        = 1.2f;
    private const float CosteCiudad       = 1.0f;
    private const float CosteZonaPeligro  = 5.0f;

    private const string NombreMarAbierto = "loonapix_17783290501031121577";
    private const string NombreCosta      = "Costa";
    private const string NombreCiudad     = "medieval_openCastle_0";

    /// <summary>
    /// Nombre del sprite que identifica las casillas de zona de peligro en el tilemap.
    /// Configurable desde el Inspector para adaptarse al nombre real del asset.
    /// </summary>
    [SerializeField] private string _nombreTileZonaPeligro = "ZonaPeligro";

    // Direcciones estándar en coordenadas cube para hexágonos Pointy Top
    private static readonly Vector3Int[] DireccionesCube =
    {
        new Vector3Int( 1, -1,  0),
        new Vector3Int( 1,  0, -1),
        new Vector3Int( 0,  1, -1),
        new Vector3Int(-1,  1,  0),
        new Vector3Int(-1,  0,  1),
        new Vector3Int( 0, -1,  1),
    };

    /// <summary>
    /// Convierte coordenadas offset (Pointy Top, odd-r) a coordenadas cube.
    /// </summary>
    private Vector3Int OffsetACube(Vector3Int offset)
    {
        int x = offset.x - (offset.y - (offset.y & 1)) / 2;
        int z = offset.y;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    /// <summary>
    /// Convierte coordenadas cube a coordenadas offset (Pointy Top, odd-r).
    /// </summary>
    private Vector3Int CubeAOffset(Vector3Int cube)
    {
        int col = cube.x + (cube.z - (cube.z & 1)) / 2;
        int row = cube.z;
        return new Vector3Int(col, row, 0);
    }

    /// <summary>
    /// Devuelve el coste de transitar por la casilla en coordenadas offset,
    /// o float.PositiveInfinity si es intransitable o el tile es nulo.
    /// </summary>
    private float CosteCasilla(Vector3Int offsetPos)
    {
        Sprite sprite = tilemap.GetSprite(offsetPos);
        if (sprite == null)
            return float.PositiveInfinity;

        if (sprite.name == _nombreTileZonaPeligro) return CosteZonaPeligro;

        return sprite.name switch
        {
            NombreMarAbierto => CosteMarAbierto,
            NombreCosta      => CosteCosta,
            NombreCiudad     => CosteCiudad,
            _                => float.PositiveInfinity,
        };
    }

    /// <summary>
    /// Indica si la casilla en coordenadas offset es transitable.
    /// </summary>
    private bool EsTransitable(Vector3Int offsetPos)
        => CosteCasilla(offsetPos) < float.PositiveInfinity;

    /// <summary>
    /// Devuelve los vecinos transitables de una casilla en coordenadas offset.
    /// </summary>
    private List<Vector3Int> GetVecinosHex(Vector3Int offsetPos)
    {
        Vector3Int cubePos = OffsetACube(offsetPos);
        var vecinos = new List<Vector3Int>(6);

        foreach (Vector3Int dir in DireccionesCube)
        {
            Vector3Int vecinoCube   = cubePos + dir;
            Vector3Int vecinoOffset = CubeAOffset(vecinoCube);
            if (EsTransitable(vecinoOffset))
                vecinos.Add(vecinoOffset);
        }

        return vecinos;
    }

    /// <summary>
    /// Devuelve los vecinos transitables de una casilla. Método temporal de debug — eliminar antes del freeze (Día 32).
    /// </summary>
    /// <param name="pos">Casilla en coordenadas offset del Tilemap.</param>
    /// <returns>Lista de vecinos transitables en coordenadas offset.</returns>
    public List<Vector3Int> GetVecinosDebug(Vector3Int pos)
    {
        Vector3Int cubePos = OffsetACube(pos);
        var transitables = new List<Vector3Int>(6);

        foreach (Vector3Int dir in DireccionesCube)
        {
            Vector3Int vecinoCube   = cubePos + dir;
            Vector3Int vecinoOffset = CubeAOffset(vecinoCube);
            Sprite sprite           = tilemap.GetSprite(vecinoOffset);
            string nombreSprite     = sprite != null ? sprite.name : "null";
            bool transitable        = EsTransitable(vecinoOffset);

            //Debug.Log($"[VecinosDebug] offset={vecinoOffset} | sprite={nombreSprite} | transitable={transitable}");

            if (transitable)
                transitables.Add(vecinoOffset);
        }

        return transitables;
    }

    /// <summary>
    /// Devuelve los vecinos navegables (agua o costa) de la casilla indicada
    /// en coordenadas offset. Usado por PirataPNJController para movimiento aleatorio.
    /// </summary>
    /// <param name="casilla">Casilla en coordenadas offset del Tilemap.</param>
    /// <returns>Lista de casillas vecinas transitables en coordenadas offset.</returns>
    public List<Vector3Int> GetVecinosNavegables(Vector3Int casilla)
    {
        return GetVecinosHex(casilla);
    }

    /// <summary>
    /// Heurística A*: distancia hexagonal en coordenadas cube.
    /// Con <paramref name="conRuido"/> aplica un factor aleatorio entre 1.0 y 1.15
    /// para que las rutas de PNJs no sean siempre idénticas.
    /// </summary>
    private float Heuristica(Vector3Int a, Vector3Int b, bool conRuido = false)
    {
        Vector3Int ca = OffsetACube(a);
        Vector3Int cb = OffsetACube(b);
        float baseH = Mathf.Max(
            Mathf.Abs(ca.x - cb.x),
            Mathf.Abs(ca.y - cb.y),
            Mathf.Abs(ca.z - cb.z)
        );
        return conRuido ? baseH * Random.Range(1.0f, 1.15f) : baseH;
    }

    /// <summary>
    /// Reconstruye el camino desde el diccionario cameFrom.
    /// </summary>
    private List<Vector3Int> ReconstruirCamino(
        Dictionary<Vector3Int, Vector3Int> cameFrom,
        Vector3Int actual)
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

    private class MinHeap
    {
        private readonly List<(float prioridad, Vector3Int elemento)> _datos = new List<(float, Vector3Int)>();

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
            Vector3Int raiz = _datos[0].elemento;
            int ultimo = _datos.Count - 1;
            _datos[0] = _datos[ultimo];
            _datos.RemoveAt(ultimo);
            int i = 0;
            while (true)
            {
                int izq = 2 * i + 1;
                int der = 2 * i + 2;
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

    /// <summary>
    /// Calcula la ruta óptima entre dos casillas del tilemap usando A* hexagonal.
    /// </summary>
    /// <param name="origen">Casilla de inicio en coordenadas offset del Tilemap.</param>
    /// <param name="destino">Casilla de destino en coordenadas offset del Tilemap.</param>
    /// <returns>
    /// Lista de casillas en coordenadas offset desde origen hasta destino (ambos incluidos),
    /// en orden de recorrido. Devuelve lista con un único elemento si origen == destino,
    /// o lista vacía si no existe ruta.
    /// </returns>
    /// <summary>
    /// Convierte una posición en coordenadas de mundo a coordenadas offset del Tilemap.
    /// </summary>
    /// <param name="posicionMundo">Posición world-space a convertir.</param>
    /// <returns>Coordenadas offset de la casilla que contiene esa posición.</returns>
    public Vector3Int MundoACasilla(Vector2 posicionMundo)
        => tilemap.WorldToCell(posicionMundo);

    public List<Vector3Int> CalcularRuta(Vector3Int origen, Vector3Int destino)
        => CalcularRutaInterna(origen, destino, conRuido: false);

    /// <summary>
    /// Igual que <see cref="CalcularRuta"/> pero aplica un factor de ruido a la heurística
    /// para que las rutas de flotas PNJ no sean siempre idénticas entre sí.
    /// Usar desde <c>ComerciantePNJController</c> en lugar de <see cref="CalcularRuta"/>.
    /// </summary>
    /// <param name="origen">Casilla de inicio en coordenadas offset del Tilemap.</param>
    /// <param name="destino">Casilla de destino en coordenadas offset del Tilemap.</param>
    /// <returns>
    /// Lista de casillas en coordenadas offset con ruta levemente aleatorizada,
    /// o lista vacía si no existe ruta.
    /// </returns>
    public List<Vector3Int> CalcularRutaConRuido(Vector3Int origen, Vector3Int destino)
        => CalcularRutaInterna(origen, destino, conRuido: true);

    /// <summary>
    /// Calcula una ruta en la que las casillas de zona de peligro tienen coste 0.2f
    /// en lugar de 5.0f, haciendo que los piratas las prefieran activamente.
    /// El resto de costes son idénticos a <see cref="CalcularRuta"/>.
    /// </summary>
    /// <param name="origen">Casilla de inicio en coordenadas offset del Tilemap.</param>
    /// <param name="destino">Casilla de destino en coordenadas offset del Tilemap.</param>
    /// <returns>
    /// Lista de casillas en coordenadas offset que prefiere zonas de peligro,
    /// o lista vacía si no existe ruta.
    /// </returns>
    public List<Vector3Int> CalcularRutaConPreferenciaZonaPeligro(Vector3Int origen, Vector3Int destino)
        => CalcularRutaInterna(origen, destino, conRuido: false, prefiereZonaPeligro: true);

    private List<Vector3Int> CalcularRutaInterna(Vector3Int origen, Vector3Int destino, bool conRuido,
                                                  bool prefiereZonaPeligro = false)
    {
        if (origen == destino)
            return new List<Vector3Int> { origen };

        if (!EsTransitable(destino))
            return new List<Vector3Int>();

        var cola       = new MinHeap();
        var cameFrom   = new Dictionary<Vector3Int, Vector3Int>();
        var costeSoFar = new Dictionary<Vector3Int, float>();

        costeSoFar[origen] = 0f;
        cola.Enqueue(origen, 0f);

        while (cola.Count > 0)
        {
            Vector3Int actual = cola.Dequeue();

            if (actual == destino)
                return ReconstruirCamino(cameFrom, actual);

            foreach (Vector3Int vecino in GetVecinosHex(actual))
            {
                float coste = CosteCasilla(vecino);

                // Los piratas prefieren las zonas de peligro (coste muy bajo en lugar de penalización)
                if (prefiereZonaPeligro && tilemap.GetSprite(vecino)?.name == _nombreTileZonaPeligro)
                    coste = 0.2f;

                float nuevoCosto = costeSoFar[actual] + coste;

                if (!costeSoFar.TryGetValue(vecino, out float costoAnterior) || nuevoCosto < costoAnterior)
                {
                    costeSoFar[vecino] = nuevoCosto;
                    cameFrom[vecino]   = actual;
                    float prioridad    = nuevoCosto + Heuristica(vecino, destino, conRuido);
                    cola.Enqueue(vecino, prioridad);
                }
            }
        }

        return new List<Vector3Int>();
    }
}
