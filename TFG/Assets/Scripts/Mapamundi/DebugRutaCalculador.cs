using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script temporal de debug para validar el A* de <see cref="RutaCalculadorTilemap"/>.
/// Adjuntar al mismo GameObject que RutaCalculadorTilemap en la escena Mapamundi.
/// ELIMINAR antes del freeze de producción (Día 32).
/// </summary>
public class DebugRutaCalculador : MonoBehaviour
{
    [SerializeField] private RutaCalculadorTilemap rutaCalculador;

    private static readonly Vector3Int PosLubeck    = new Vector3Int(-4,   0, 0);
    private static readonly Vector3Int PosBrujas    = new Vector3Int(-12, -4, 0);
    private static readonly Vector3Int PosRuan      = new Vector3Int(-16, -8, 0);
    private static readonly Vector3Int PosGenova    = new Vector3Int(-8, -16, 0);
    private static readonly Vector3Int PosVenecia   = new Vector3Int(-5, -15, 0);
    private static readonly Vector3Int PosBarcelona = new Vector3Int(-16, -21, 0);

    private IEnumerator Start()
    {
        yield return null;

        PruebaRutaLarga("Lübeck → Brujas",    PosLubeck,    PosBrujas);
        PruebaRutaLarga("Lübeck → Ruan",      PosLubeck,    PosRuan);
        PruebaRutaLarga("Lübeck → Génova",    PosLubeck,    PosGenova);
        PruebaRutaLarga("Lübeck → Venecia",   PosLubeck,    PosVenecia);
        PruebaRutaLarga("Lübeck → Barcelona", PosLubeck,    PosBarcelona);
        PruebaRutaLarga("Brujas → Ruan",      PosBrujas,    PosRuan);
        PruebaRutaLarga("Brujas → Génova",    PosBrujas,    PosGenova);
        PruebaRutaLarga("Brujas → Venecia",   PosBrujas,    PosVenecia);
        PruebaRutaLarga("Brujas → Barcelona", PosBrujas,    PosBarcelona);
        PruebaRutaLarga("Ruan → Génova",      PosRuan,      PosGenova);
        PruebaRutaLarga("Ruan → Venecia",     PosRuan,      PosVenecia);
        PruebaRutaLarga("Ruan → Barcelona",   PosRuan,      PosBarcelona);
        PruebaRutaLarga("Génova → Venecia",   PosGenova,    PosVenecia);
        PruebaRutaLarga("Génova → Barcelona", PosGenova,    PosBarcelona);
        PruebaRutaLarga("Venecia → Barcelona",PosVenecia,   PosBarcelona);
    }

    /// <summary>
    /// Loguea todos los vecinos transitables de una casilla para diagnóstico del A*.
    /// </summary>
    private void PruebaVecinos(string nombre, Vector3Int casilla)
    {
        List<Vector3Int> vecinos = rutaCalculador.GetVecinosDebug(casilla);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[DebugRuta] {nombre} | Transitables encontrados: {vecinos.Count}");
        if (vecinos.Count == 0)
        {
            sb.Append("  (ningún vecino transitable)");
        }
        else
        {
            for (int i = 0; i < vecinos.Count; i++)
                sb.AppendLine($"  [{i}] {vecinos[i]}");
        }
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Comprueba que la ruta entre dos ciudades devuelve más de 0 hexágonos.
    /// </summary>
    private void PruebaRutaLarga(string nombre, Vector3Int origen, Vector3Int destino)
    {
        List<Vector3Int> ruta = rutaCalculador.CalcularRuta(origen, destino);
        bool pass = ruta.Count > 0;
        Debug.Log($"[DebugRuta] {nombre} — {(pass ? "PASS" : "FAIL")} | Hexágonos: {ruta.Count}");
    }

    /// <summary>
    /// Comprueba que cuando origen == destino la ruta devuelve exactamente 1 elemento.
    /// </summary>
    private void PruebaOrigenIgualDestino(string nombre, Vector3Int casilla)
    {
        List<Vector3Int> ruta = rutaCalculador.CalcularRuta(casilla, casilla);
        bool pass = ruta.Count == 1 && ruta[0] == casilla;
        Debug.Log($"[DebugRuta] {nombre} — {(pass ? "PASS" : "FAIL")} | Elementos: {ruta.Count}");
    }

    /// <summary>
    /// Comprueba que cuando el destino es tierra la ruta devuelve lista vacía.
    /// </summary>
    private void PruebaDestinoIntransitable(string nombre, Vector3Int origen, Vector3Int destino)
    {
        List<Vector3Int> ruta = rutaCalculador.CalcularRuta(origen, destino);
        bool pass = ruta.Count == 0;
        Debug.Log($"[DebugRuta] {nombre} — {(pass ? "PASS" : "FAIL")} | Elementos: {ruta.Count}");
    }
}
