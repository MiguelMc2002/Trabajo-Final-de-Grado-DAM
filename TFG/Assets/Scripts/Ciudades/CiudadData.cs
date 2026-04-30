using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que describe la configuración estática de una ciudad de la Liga Hanseática:
/// su nombre y los bienes que comercia su mercado con sus valores de producción y consumo diarios.
/// Crear instancias desde el menú: Assets → Create → TFG → Ciudad.
/// </summary>
[CreateAssetMenu(fileName = "NuevaCiudad", menuName = "TFG/Ciudad")]
public class CiudadData : ScriptableObject
{
    // ─── Identificación ──────────────────────────────────────────────────────

    /// <summary>
    /// Identificador numérico único de la ciudad. Debe coincidir con el id_ciudad
    /// de la tabla Ciudad en SQLite (1=Lübeck, 2=Barcelona, 3=Génova, 4=Venecia, 5=Ruan, 6=Brujas).
    /// </summary>
    [Header("Identificación")]
    [SerializeField] public int IdCiudad;

    /// <summary>
    /// Nombre de la ciudad que se mostrará en la interfaz (p. ej. "Lübeck", "Brujas").
    /// </summary>
    public string NombreCiudad;

    // ─── Mercado ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Lista de bienes disponibles en el mercado de esta ciudad, con su stock inicial
    /// y sus cadencias de producción y consumo diarios.
    /// </summary>
    [Header("Mercado")]
    public List<EntradaMercado> Mercado = new List<EntradaMercado>();
}

/// <summary>
/// Agrupa la configuración de un bien dentro del mercado de una ciudad concreta:
/// el bien de referencia, el stock inicial, el stock máximo que puede albergar la ciudad,
/// y las tasas diarias de producción y consumo.
/// También almacena el precio calculado en tiempo de ejecución (oculto en el Inspector).
/// </summary>
[Serializable]
public class EntradaMercado
{
    /// <summary>
    /// Referencia al <see cref="BienData"/> que define nombre, categoría y precio base del bien.
    /// </summary>
    public BienData Bien;

    /// <summary>
    /// Unidades del bien disponibles actualmente en el mercado de la ciudad.
    /// Se reduce al comprar y aumenta al vender (hasta <see cref="StockMax"/>).
    /// </summary>
    [Min(0)]
    public int StockActual;

    /// <summary>
    /// Capacidad máxima de este bien en la ciudad. Limita las ventas del jugador
    /// y determina el rango dinámico del precio junto con <see cref="BienData.stockMaximo"/>.
    /// </summary>
    [Min(1)]
    public int StockMax = 500;

    /// <summary>
    /// Unidades que la ciudad genera de este bien cada día de juego por sus edificios productivos.
    /// </summary>
    [Min(0)]
    public int ProduccionDiaria;

    /// <summary>
    /// Unidades que la ciudad consume de este bien cada día de juego para satisfacer a su población.
    /// </summary>
    [Min(0)]
    public int ConsumoDiario;

    /// <summary>
    /// Precio calculado en tiempo de ejecución según la fórmula de oferta y demanda.
    /// No editable desde el Inspector; se inicializa en <c>Start</c> del <see cref="MarketManager"/>.
    /// </summary>
    [HideInInspector]
    public float PrecioActual;
}
