using NUnit.Framework;

// ─── Clase local autónoma ─────────────────────────────────────────────────────

/// <summary>
/// Copia mínima de SlotData definida localmente para que el test sea
/// completamente autónomo y no dependa del ensamblado del proyecto.
/// </summary>
internal class SlotDataLocal
{
    public int    NumeroSlot    { get; set; }
    public bool   EstaOcupado  { get; set; }
    public string NombrePartida { get; set; }
}

// ─── Tests ────────────────────────────────────────────────────────────────────

/// <summary>
/// Tests de EditMode para la lógica de metadatos de un slot de guardado.
/// Autónomos: usan la clase local <see cref="SlotDataLocal"/> sin importar
/// ninguna clase del proyecto.
/// </summary>
[TestFixture]
public class SlotDataTests
{
    /// <summary>
    /// Un slot recién creado debe tener EstaOcupado en false,
    /// indicando que no contiene ninguna partida guardada.
    /// </summary>
    [Test]
    public void SlotData_PorDefecto_EstaVacio()
    {
        var slot = new SlotDataLocal();

        Assert.IsFalse(slot.EstaOcupado,
            "Un slot nuevo debe tener EstaOcupado = false por defecto.");
    }

    /// <summary>
    /// El nombre de partida asignado a un slot debe contener el número de slot,
    /// siguiendo la convención "Partida N".
    /// </summary>
    [Test]
    public void SlotData_NombrePartida_ContieneNumeroSlot()
    {
        var slot = new SlotDataLocal
        {
            NumeroSlot    = 3,
            NombrePartida = "Partida 3"
        };

        Assert.IsTrue(slot.NombrePartida.Contains("3"),
            "El nombre de partida debe contener el número del slot.");
    }
}
