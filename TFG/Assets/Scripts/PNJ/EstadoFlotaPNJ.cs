/// <summary>
/// Estados posibles de una flota PNJ comerciante durante la simulación.
/// La máquina de estados de <see cref="FlotaManager"/> hace transiciones
/// entre estos valores en función de las acciones de la flota.
/// </summary>
public enum EstadoFlotaPNJ
{
    /// <summary>
    /// La flota está atracada en un puerto.
    /// En este estado puede comprar o vender mercancías en el mercado de la ciudad.
    /// </summary>
    EnPuerto,

    /// <summary>
    /// La flota navega por el mapamundi hacia su ciudad de destino.
    /// No puede interactuar con mercados hasta llegar al puerto.
    /// </summary>
    Viajando,

    /// <summary>
    /// La flota está ejecutando una operación comercial en el mercado de su ciudad actual.
    /// Estado transitorio entre <see cref="EnPuerto"/> y <see cref="Viajando"/>.
    /// </summary>
    Comerciando,
}
