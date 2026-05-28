using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel modal que muestra el resultado de un combate naval al jugador.
/// Se suscribe a <see cref="GestorCombatesActivos.OnCombateJugadorTerminado"/> y
/// pausa el tiempo mientras está visible.
/// Post-victoria: botones Destruir, Saquear y Capturar.
/// Adjuntar a un GameObject en la escena Mapamundi y cablear los campos desde el Inspector.
/// </summary>
public class ResultadoCombateUI : MonoBehaviour
{
    [Header("Panel principal")]
    [SerializeField] private GameObject      _panelResultado;
    [SerializeField] private TextMeshProUGUI _txtTitulo;
    [SerializeField] private TextMeshProUGUI _txtNarrativo;
    [SerializeField] private TextMeshProUGUI _txtBajas;
    [SerializeField] private Button          _btnContinuar;

    [Header("Botones post-victoria")]
    [SerializeField] private GameObject _panelAccionesVictoria;
    [SerializeField] private Button     _btnDestruir;
    [SerializeField] private Button     _btnSaquear;
    [SerializeField] private Button     _btnCapturar;

    [Header("Sub-panel saqueo")]
    [SerializeField] private GameObject      _panelSaqueo;
    [SerializeField] private TextMeshProUGUI _txtBotinSaqueo;
    [SerializeField] private Button          _btnConfirmarSaqueo;

    [Header("Sub-panel captura")]
    [SerializeField] private GameObject      _panelCaptura;
    [SerializeField] private TextMeshProUGUI _txtInfoCaptura;
    [SerializeField] private Button          _btnConfirmarCaptura;

    private CombateEnCurso _combate;

    private void Awake()
    {
        if (_panelResultado != null)   _panelResultado.SetActive(false);
        if (_panelSaqueo    != null)   _panelSaqueo.SetActive(false);
        if (_panelCaptura   != null)   _panelCaptura.SetActive(false);

        _btnContinuar?.onClick.AddListener(OcultarResultado);
        _btnDestruir?.onClick.AddListener(OnDestruir);
        _btnSaquear?.onClick.AddListener(OnSaquear);
        _btnCapturar?.onClick.AddListener(OnCapturar);
        _btnConfirmarSaqueo?.onClick.AddListener(OnConfirmarSaqueo);
        _btnConfirmarCaptura?.onClick.AddListener(OnConfirmarCaptura);
    }

    private void OnEnable()
    {
        GestorCombatesActivos.OnCombateJugadorTerminado += OnCombateTerminado;
    }

    private void OnDisable()
    {
        GestorCombatesActivos.OnCombateJugadorTerminado -= OnCombateTerminado;
    }

    // ─── Entrada desde GestorCombatesActivos ─────────────────────────────────

    private void OnCombateTerminado(CombateEnCurso combate)
    {
        _combate = combate;

        if (_panelResultado != null)
            _panelResultado.SetActive(true);

        Time.timeScale = 0f;

        ActualizarTitulo();
        ActualizarNarrativo();
        ActualizarBajas();
        ActualizarAccionesVictoria();
    }

    /// <summary>
    /// Cierra el panel de resultados y reanuda el tiempo.
    /// Vinculado al botón Continuar.
    /// </summary>
    public void OcultarResultado()
    {
        if (_panelResultado != null) _panelResultado.SetActive(false);
        if (_panelSaqueo    != null) _panelSaqueo.SetActive(false);
        if (_panelCaptura   != null) _panelCaptura.SetActive(false);

        Time.timeScale = 1f;
        CombateEventos.DispararFinCombate();
    }

    // ─── Actualización de textos ──────────────────────────────────────────────

    private void ActualizarTitulo()
    {
        if (_txtTitulo == null) return;
        if (_combate.TerminoPorTimeout)
            _txtTitulo.text = "EVASIÓN";
        else
            _txtTitulo.text = _combate.JugadorGano ? "VICTORIA" : "DERROTA";
    }

    private void ActualizarNarrativo()
    {
        if (_txtNarrativo == null) return;
        if (_combate.TerminoPorTimeout)
            _txtNarrativo.text =
                $"Tras 5 días de combate, la flota de {_combate.FlotaEnemiga.NombrePropietario} " +
                "ha conseguido escapar. Ambas flotas sufren los daños acumulados.";
        else if (_combate.JugadorGano)
            _txtNarrativo.text = $"¡Victoria! La flota de {_combate.FlotaEnemiga.NombrePropietario} ha sido derrotada.";
        else
            _txtNarrativo.text = "Habéis sufrido una derrota. Regresad al puerto más cercano.";
    }

    private void ActualizarBajas()
    {
        if (_txtBajas == null) return;

        bool jugadorEsAtacante = _combate.Atacante.Id == -1;
        FlotaRuntimeData flotaJugador = jugadorEsAtacante ? _combate.Atacante : _combate.Defensor;
        FlotaRuntimeData flotaEnemiga = _combate.FlotaEnemiga;

        int inicialJugador = jugadorEsAtacante
            ? _combate.NumBarcosInicialAtacante
            : _combate.NumBarcosInicialDefensor;
        int inicialEnemigo = jugadorEsAtacante
            ? _combate.NumBarcosInicialDefensor
            : _combate.NumBarcosInicialAtacante;

        int perdidosJugador = Mathf.Max(0, inicialJugador - flotaJugador.NumBarcos);
        int perdidosEnemigo = Mathf.Max(0, inicialEnemigo - flotaEnemiga.NumBarcos);

        _txtBajas.text =
            $"Tus bajas: {perdidosJugador} de {inicialJugador} barcos hundidos" +
            $", vida restante: {flotaJugador.VidaActual:F0}\n" +
            $"Bajas enemigas: {perdidosEnemigo} de {inicialEnemigo} barcos hundidos" +
            $", vida restante: {flotaEnemiga.VidaActual:F0}";
    }

    private void ActualizarAccionesVictoria()
    {
        bool mostrarAcciones = _combate.JugadorGano && _panelAccionesVictoria != null;
        if (_panelAccionesVictoria != null)
            _panelAccionesVictoria.SetActive(mostrarAcciones);
        if (_btnContinuar != null)
            _btnContinuar.gameObject.SetActive(!mostrarAcciones);

        // Ocultar botón Capturar si la flota ya está al límite
        if (_btnCapturar != null && mostrarAcciones)
        {
            int slotsLibres = FlotaJugador.MaxBarcos - (GameManager.Instance?.FlotaJugador?.Barcos.Count ?? 0);
            _btnCapturar.gameObject.SetActive(slotsLibres > 0);
        }
    }

    // ─── Acciones post-victoria ───────────────────────────────────────────────

    private void OnDestruir()
    {
        // Flota enemiga ya fue eliminada por GestorCombatesActivos; solo cerrar el panel
        if (_panelAccionesVictoria != null) _panelAccionesVictoria.SetActive(false);
        if (_btnContinuar != null)          _btnContinuar.gameObject.SetActive(true);
    }

    private void OnSaquear()
    {
        if (_panelAccionesVictoria != null) _panelAccionesVictoria.SetActive(false);
        if (_panelSaqueo           != null) _panelSaqueo.SetActive(true);

        if (_txtBotinSaqueo == null) return;

        var sb = new StringBuilder();
        long oro = (long)(_combate.FlotaEnemiga.VidaMax * 10f);
        sb.AppendLine($"Oro: {oro} monedas");

        int espacioLibre = EspacioLibreAlmacen();
        int acumulado    = 0;

        if (_combate.FlotaEnemiga.Carga != null && GameManager.Instance != null)
        {
            IReadOnlyList<BienData> catalogo = GameManager.Instance.CatalogoBienes;
            foreach (KeyValuePair<int, int> kv in _combate.FlotaEnemiga.Carga)
            {
                int disponible = Mathf.RoundToInt(kv.Value * 0.40f);
                if (disponible <= 0) continue;

                int cabe       = Mathf.Max(0, espacioLibre - acumulado);
                int tomaremos  = Mathf.Min(disponible, cabe);
                string nombre  = ResolverNombreBien(kv.Key, catalogo);

                if (tomaremos > 0)
                    sb.AppendLine($"  + {tomaremos}x {nombre}");
                else
                    sb.AppendLine($"  (Sin hueco para {disponible}x {nombre})");

                acumulado += tomaremos;
            }
        }

        if (espacioLibre == 0)
            sb.AppendLine("\n¡Bodega llena! Solo recibirás el oro.");

        _txtBotinSaqueo.text = sb.Length > 0 ? sb.ToString() : "La bodega enemiga está vacía.";
    }

    private void OnConfirmarSaqueo()
    {
        long oro = (long)(_combate.FlotaEnemiga.VidaMax * 10f);
        GameManager.Instance?.ModificarDinero(oro);

        if (_combate.FlotaEnemiga.Carga != null && GameManager.Instance != null)
        {
            IReadOnlyList<BienData> catalogo = GameManager.Instance.CatalogoBienes;
            int espacioLibre = EspacioLibreAlmacen();

            // Snapshot de claves para no modificar el diccionario durante la iteración
            var keys = new List<int>(_combate.FlotaEnemiga.Carga.Keys);
            foreach (int idBien in keys)
            {
                int disponible = Mathf.RoundToInt(_combate.FlotaEnemiga.Carga[idBien] * 0.40f);
                int tomaremos  = Mathf.Min(disponible, espacioLibre);

                if (tomaremos > 0)
                {
                    BienData bien = ResolverBienData(idBien, catalogo);
                    if (bien != null)
                    {
                        GameManager.Instance.ModificarCantidadBien(bien, tomaremos);
                        espacioLibre -= tomaremos;
                    }
                }

                _combate.FlotaEnemiga.Carga[idBien] = 0;
            }
        }

        if (_panelSaqueo  != null) _panelSaqueo.SetActive(false);
        if (_btnContinuar != null) _btnContinuar.gameObject.SetActive(true);
    }

    // ─── Helpers de saqueo ───────────────────────────────────────────────────

    /// <summary>Espacio disponible en bodega según la capacidad real de la flota.</summary>
    private int EspacioLibreAlmacen()
    {
        int capacidad = GameManager.Instance?.FlotaJugador?.CargaMaximaTotal ?? 0;
        int usado     = GameManager.Instance?.GetTotalUnidadesAlmacen()      ?? 0;
        return Mathf.Max(0, capacidad - usado);
    }

    /// <summary>Resuelve el <see cref="BienData"/> a partir del id_bien (1-based en catálogo).</summary>
    private static BienData ResolverBienData(int idBien, IReadOnlyList<BienData> catalogo)
    {
        if (catalogo == null) return null;
        int idx = idBien - 1;
        return (idx >= 0 && idx < catalogo.Count) ? catalogo[idx] : null;
    }

    /// <summary>Devuelve el nombre del bien o un fallback legible si no está en el catálogo.</summary>
    private static string ResolverNombreBien(int idBien, IReadOnlyList<BienData> catalogo)
    {
        BienData bien = ResolverBienData(idBien, catalogo);
        return bien != null ? bien.nombre : $"Bien #{idBien}";
    }

    private void OnCapturar()
    {
        if (_panelAccionesVictoria != null) _panelAccionesVictoria.SetActive(false);
        if (_panelCaptura          != null) _panelCaptura.SetActive(true);

        int barcosCapturaDisponibles = Mathf.Min(
            _combate.FlotaEnemiga.NumBarcos,
            FlotaJugador.MaxBarcos - (GameManager.Instance?.FlotaJugador?.Barcos.Count ?? 0));

        if (_txtInfoCaptura != null)
        {
            if (barcosCapturaDisponibles <= 0)
                _txtInfoCaptura.text = "Tu flota ya está al límite (5 barcos). No puedes capturar más barcos.";
            else
                _txtInfoCaptura.text =
                    $"Puedes capturar hasta {barcosCapturaDisponibles} barco(s) enemigo(s).\n" +
                    $"Se añadirán a tu flota con el daño recibido.";
        }

        if (_btnConfirmarCaptura != null)
            _btnConfirmarCaptura.interactable = barcosCapturaDisponibles > 0;
    }

    private void OnConfirmarCaptura()
    {
        FlotaJugador flotaJugador = GameManager.Instance?.FlotaJugador;
        if (flotaJugador == null) { OcultarResultado(); return; }

        int slots    = FlotaJugador.MaxBarcos - flotaJugador.Barcos.Count;
        int capturar = Mathf.Min(slots, _combate.FlotaEnemiga.NumBarcos);

        // Vida por barco capturado: proporcional al daño recibido por la flota enemiga
        int vidaPorBarco = Mathf.Max(10,
            Mathf.RoundToInt(_combate.FlotaEnemiga.VidaActual
                             / Mathf.Max(1f, _combate.FlotaEnemiga.NumBarcos)));

        for (int i = 0; i < capturar; i++)
        {
            var casco = new CascoCapturado(vidaPorBarco);
            var barco = new BarcoJugador(
                idBarco: 1000 + i,
                nombre:  $"Presa {i + 1} ({_combate.FlotaEnemiga.NombrePropietario})",
                casco:   casco);
            barco.VidaActual  = vidaPorBarco;
            barco.Tripulacion = Mathf.RoundToInt(
                _combate.FlotaEnemiga.Tripulacion / Mathf.Max(1f, _combate.FlotaEnemiga.NumBarcos));
            flotaJugador.AñadirBarco(barco);
        }

        if (_panelCaptura  != null) _panelCaptura.SetActive(false);
        if (_btnContinuar  != null) _btnContinuar.gameObject.SetActive(true);
    }

    // ─── Casco mínimo para barcos capturados ─────────────────────────────────

    private sealed class CascoCapturado : IBarco
    {
        private readonly int _vida;
        public CascoCapturado(int vida) { _vida = vida; }
        public int    IdTipoCasco         => -1;
        public string NombreCasco         => "Capturado";
        public int    VidaBase            => _vida;
        public int    VelocidadBase       => 3;
        public int    ManiobrabilidadBase => 3;
        public int    CapacidadCargaBase  => 50;
        public int    CapacidadModulos    => 0;
        public int    CapacidadTripulacion => 20;
        public int    CosteOro            => 0;
    }
}
