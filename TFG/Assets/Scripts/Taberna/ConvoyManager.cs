using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor singleton de convoyes del jugador. Crea, mantiene y disuelve formaciones
/// de barcos (<see cref="ConvoyData"/>) que navegan juntos por el mapamundi.
/// Adjuntar a un GameObject en la escena Mapamundi o Ciudad.
/// </summary>
public class ConvoyManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al gestor de convoyes activo.</summary>
    public static ConvoyManager Instance { get; private set; }

    // ─── Estado ───────────────────────────────────────────────────────────────

    private readonly List<ConvoyData> _convoysActivos = new List<ConvoyData>();

    /// <summary>Lista de convoyes activos en este momento. Solo lectura desde fuera.</summary>
    public IReadOnlyList<ConvoyData> ConvoysActivos => _convoysActivos;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo convoy liderado por el barco indicado y lo registra como activo.
    /// Si el barco ya lidera un convoy existente, devuelve ese convoy sin crear uno nuevo.
    /// </summary>
    /// <param name="barcoLider">Barco que encabezará la formación.</param>
    /// <returns>El <see cref="ConvoyData"/> nuevo o el ya existente para ese barco líder.</returns>
    public ConvoyData CrearConvoy(BarcoJugador barcoLider)
    {
        // Devolver el convoy existente si el barco ya lidera uno
        foreach (ConvoyData c in _convoysActivos)
            if (c.BarcoLider == barcoLider) return c;

        var convoy = new ConvoyData(barcoLider);
        _convoysActivos.Add(convoy);
        Debug.Log($"[ConvoyManager] Convoy '{convoy.NombreConvoy}' creado.");
        return convoy;
    }

    /// <summary>
    /// Añade un barco a un convoy existente.
    /// </summary>
    /// <param name="convoy">Convoy de destino.</param>
    /// <param name="barco">Barco a incorporar.</param>
    /// <returns><c>true</c> si se incorporó correctamente; <c>false</c> si el convoy está lleno
    /// o el barco ya era miembro.</returns>
    public bool UnirseAConvoy(ConvoyData convoy, BarcoJugador barco)
    {
        if (convoy == null || barco == null) return false;
        bool ok = convoy.AñadirMiembro(barco);
        if (ok) Debug.Log($"[ConvoyManager] '{barco.Nombre}' se unió al convoy '{convoy.NombreConvoy}'.");
        return ok;
    }

    /// <summary>
    /// Elimina un barco de un convoy. Si el convoy queda sin miembros tras la salida,
    /// se disuelve automáticamente.
    /// </summary>
    /// <param name="convoy">Convoy del que sale el barco.</param>
    /// <param name="barco">Barco que abandona la formación.</param>
    /// <returns><c>true</c> si el barco abandonó correctamente; <c>false</c> si era el líder
    /// o no pertenecía al convoy.</returns>
    public bool AbandonarConvoy(ConvoyData convoy, BarcoJugador barco)
    {
        if (convoy == null || barco == null) return false;
        bool ok = convoy.EliminarMiembro(barco);
        if (!ok) return false;

        Debug.Log($"[ConvoyManager] '{barco.Nombre}' abandonó el convoy '{convoy.NombreConvoy}'.");

        if (convoy.Miembros.Count == 0)
            DisolverConvoy(convoy);

        return true;
    }

    /// <summary>
    /// Busca el convoy al que pertenece el barco indicado, ya sea como líder o como miembro.
    /// </summary>
    /// <param name="barco">Barco a buscar.</param>
    /// <returns>El <see cref="ConvoyData"/> al que pertenece, o <c>null</c> si no está en ninguno.</returns>
    public ConvoyData GetConvoyDeBarco(BarcoJugador barco)
    {
        if (barco == null) return null;
        foreach (ConvoyData c in _convoysActivos)
            foreach (BarcoJugador m in c.Miembros)
                if (m == barco) return c;
        return null;
    }

    /// <summary>
    /// Disuelve el convoy indicado y lo elimina de la lista de activos.
    /// </summary>
    /// <param name="convoy">Convoy a disolver.</param>
    public void DisolverConvoy(ConvoyData convoy)
    {
        if (convoy == null) return;
        _convoysActivos.Remove(convoy);
        Debug.Log($"[ConvoyManager] Convoy '{convoy.NombreConvoy}' disuelto.");
    }
}
