using UnityEngine;
using UnityEngine.Tilemaps;

namespace HansaTrader.Mapamundi
{
    /// <summary>
    /// Tile base del mapamundi hexagonal. Extiende Tile de Unity añadiendo
    /// el coste de movimiento para el pathfinding A* (Día 17).
    /// - costeMovimiento = 1.0f → Mar Abierto (preferido por A*)
    /// - costeMovimiento = 1.2f → Aguas Costeras (ligeramente más lento)
    /// - costeMovimiento = 5.0f → Zona Peligro (penalizado pero transitable)
    /// - costeMovimiento = float.PositiveInfinity → Tierra (intransitable)
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoTileNavegable", menuName = "HansaTrader/Mapamundi/Tile Navegable")]
    public class TileNavegable : Tile
    {
        [Header("Pathfinding")]
        [Tooltip("Coste de movimiento para el A*. Infinity = intransitable.")]
        public float costeMovimiento = 1f;

        [Tooltip("Si es false, el A* ignorará esta casilla como destino o paso.")]
        public bool esTransitable = true;
    }
}
