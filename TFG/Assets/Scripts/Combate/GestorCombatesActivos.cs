using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour que gestiona todos los combates navales activos en el mapamundi.
/// Avanza cada combate turno a turno cada hora de juego (<see cref="SimulacionTiempo.OnNuevaHora"/>).
/// Los combates PNJ vs PNJ se resuelven en silencio; los del jugador disparan
/// <see cref="OnCombateJugadorTerminado"/> para que <see cref="ResultadoCombateUI"/> muestre el resultado.
/// Añadir a un GameObject en la escena Mapamundi.
/// </summary>
public class GestorCombatesActivos : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    private static GestorCombatesActivos _instance;

    /// <summary>Punto de acceso global. Vive solo en la escena Mapamundi.</summary>
    public static GestorCombatesActivos Instance => _instance;

    // ─── Eventos ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Disparado cuando termina un combate en el que participa el jugador.
    /// <see cref="ResultadoCombateUI"/> se suscribe para mostrar el panel de resultado.
    /// </summary>
    public static event Action<CombateEnCurso> OnCombateJugadorTerminado;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private readonly List<CombateEnCurso> _combatesActivos = new();

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _instance = this;
    }

    private void OnEnable()
    {
        SimulacionTiempo.OnNuevaHora += OnNuevaHora;
    }

    private void OnDisable()
    {
        SimulacionTiempo.OnNuevaHora -= OnNuevaHora;
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Registra un nuevo combate entre dos flotas y bloquea sus iconos en el mapamundi.
    /// Si el jugador está involucrado, los iconos se colorean de naranja.
    /// </summary>
    /// <param name="atacante">Flota atacante.</param>
    /// <param name="defensor">Flota defensora.</param>
    /// <param name="esDelJugador">Verdadero si alguna flota tiene Id == -1.</param>
    public void IniciarCombate(FlotaRuntimeData atacante, FlotaRuntimeData defensor, bool esDelJugador)
    {
        if (atacante == null || defensor == null) return;

        // Rechazar si alguna de las dos flotas ya está en un combate activo
        foreach (CombateEnCurso c in _combatesActivos)
        {
            if (c.Atacante.Id == atacante.Id || c.Defensor.Id == atacante.Id) return;
            if (c.Atacante.Id == defensor.Id || c.Defensor.Id == defensor.Id) return;
        }

        CombateEnCurso combate = new CombateEnCurso(atacante, defensor, esDelJugador);
        _combatesActivos.Add(combate);

        MapamundiController.Instance?.PausarIcono(atacante.Id);
        MapamundiController.Instance?.PausarIcono(defensor.Id);
    }

    // ─── Procesamiento por hora ───────────────────────────────────────────────

    private void OnNuevaHora()
    {
        if (_combatesActivos.Count == 0) return;

        for (int i = _combatesActivos.Count - 1; i >= 0; i--)
        {
            CombateEnCurso c = _combatesActivos[i];
            bool terminado   = c.ResolverTurno();

            if (!terminado) continue;

            _combatesActivos.RemoveAt(i);
            FinalizarCombate(c);
        }
    }

    private void FinalizarCombate(CombateEnCurso c)
    {
        if (c.EsDelJugador)
        {
            bool jugadorEsAtacante        = c.Atacante.Id == -1;
            FlotaRuntimeData flotaJugador = jugadorEsAtacante ? c.Atacante : c.Defensor;
            FlotaRuntimeData flotaEnemiga = jugadorEsAtacante ? c.Defensor : c.Atacante;

            // Los barcos del jugador en BarcosFlota son las mismas referencias que en FlotaJugador._barcos.
            // La VidaActual ya fue actualizada turno a turno; solo hay que eliminar los hundidos.
            FlotaJugador flotaReal = GameManager.Instance?.FlotaJugador;
            if (flotaReal != null && flotaJugador.BarcosFlota != null)
            {
                var hundidos = new System.Collections.Generic.List<int>();
                foreach (BarcoJugador barco in flotaJugador.BarcosFlota)
                    if (barco.VidaActual <= 0) hundidos.Add(barco.IdBarco);
                foreach (int id in hundidos)
                    flotaReal.EliminarBarco(id);
            }

            // Eliminar flota enemiga si fue destruida
            if (flotaEnemiga.EstaDestruida() && flotaEnemiga.Id != -1)
                FlotaManager.Instance?.EliminarFlota(flotaEnemiga.Id);

            // Restaurar icono enemigo si sobrevivió
            if (!flotaEnemiga.EstaDestruida())
                MapamundiController.Instance?.ReanudarIcono(flotaEnemiga.Id);

            // Restaurar icono jugador
            MapamundiController.Instance?.ReanudarIcono(-1);

            OnCombateJugadorTerminado?.Invoke(c);
        }
        else
        {
            // Combate PNJ vs PNJ silencioso
            FlotaRuntimeData ganador = c.Ganador;

            if (c.Atacante.EstaDestruida())
            {
                FlotaManager.Instance?.EliminarFlota(c.Atacante.Id);
            }
            else
            {
                MapamundiController.Instance?.ReanudarIcono(c.Atacante.Id);
                c.Atacante.EstadoActual = EstadoFlotaPNJ.Patrullando;
                c.Atacante.RutaActualTilemap?.Clear();
            }

            if (c.Defensor.EstaDestruida())
            {
                // El ganador PNJ captura barcos inutilizados del perdedor si tiene hueco
                if (ganador != null)
                    IntentarCapturaPNJ(ganador, c.Defensor);
                FlotaManager.Instance?.EliminarFlota(c.Defensor.Id);
            }
            else
            {
                MapamundiController.Instance?.HuirAlPuerto(c.Defensor.Id);
            }

            Debug.Log($"[GestorCombates] PNJ vs PNJ terminado en turno {c.TurnoActual}: " +
                      $"{c.Atacante.NombrePropietario} vs {c.Defensor.NombrePropietario}");
        }
    }

    /// <summary>
    /// Lógica de captura automática PNJ: añade barcos inutilizados del perdedor
    /// a la flota ganadora si esta tiene menos de <see cref="FlotaJugador.MaxBarcos"/> barcos.
    /// Los barcos capturados se restauran al 25% de vida.
    /// </summary>
    private static void IntentarCapturaPNJ(FlotaRuntimeData ganador, FlotaRuntimeData perdedor)
    {
        if (perdedor.BarcosFlota == null) return;

        foreach (BarcoJugador barco in perdedor.BarcosFlota)
        {
            if (barco.VidaActual > 0) continue;                      // no inutilizado
            if (ganador.NumBarcos >= FlotaJugador.MaxBarcos) break;  // sin hueco

            int vidaRestaurada = Mathf.Max(1, barco.VidaTotal / 4);
            barco.VidaActual   = vidaRestaurada;

            ganador.BarcosFlota.Add(barco);
            ganador.NumBarcos++;
            ganador.VidaMax    += barco.VidaTotal;
            ganador.VidaActual += vidaRestaurada;
            ganador.FuerzaCanhones += barco.FuerzaCombateTotal;
        }
    }
}
