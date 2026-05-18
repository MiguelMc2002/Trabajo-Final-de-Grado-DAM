using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz del astillero con cinco subpaneles: Menú principal, Construir,
/// Modificar, Reparar y Vender. Solo un subpanel es visible a la vez.
/// Adjuntar a un GameObject en la escena Ciudad y cablear todos los campos
/// desde el Inspector antes de ejecutar.
/// </summary>
public class AstilleroUI : MonoBehaviour
{
    // ─── Panel raíz ───────────────────────────────────────────────────────────
    [SerializeField] private GameObject _panelAstillero;

    // ─── Subpaneles ───────────────────────────────────────────────────────────
    [SerializeField] private GameObject _panelMenu;
    [SerializeField] private GameObject _panelConstruir;
    [SerializeField] private GameObject _panelModificar;
    [SerializeField] private GameObject _panelReparar;
    [SerializeField] private GameObject _panelVender;

    // ─── Panel Construir ──────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoCascoConstruir;
    [SerializeField] private TextMeshProUGUI _statsVidaCasco;
    [SerializeField] private TextMeshProUGUI _statsVelocidadCasco;
    [SerializeField] private TextMeshProUGUI _statsManiobrabilidadCasco;
    [SerializeField] private TextMeshProUGUI _statsCargaCasco;
    [SerializeField] private TextMeshProUGUI _statsSlotsDisponibles;
    [SerializeField] private TextMeshProUGUI _textoModuloArmamento;
    [SerializeField] private TextMeshProUGUI _textoModuloVelas;
    [SerializeField] private TextMeshProUGUI _textoModuloBodega;
    [SerializeField] private TextMeshProUGUI _statsArmamento;
    [SerializeField] private TextMeshProUGUI _statsVelas;
    [SerializeField] private TextMeshProUGUI _statsBodega;
    [SerializeField] private TextMeshProUGUI _textoPrecioConstruir;
    [SerializeField] private Button          _btnConstruir;

    // ─── Panel Modificar ──────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoBarcoModificar;
    [SerializeField] private TextMeshProUGUI _textoModuloArmamentoMod;
    [SerializeField] private TextMeshProUGUI _textoModuloVelasMod;
    [SerializeField] private TextMeshProUGUI _textoModuloBodegaMod;
    [SerializeField] private TextMeshProUGUI _statsArmamentoMod;
    [SerializeField] private TextMeshProUGUI _statsVelasMod;
    [SerializeField] private TextMeshProUGUI _statsBodegaMod;
    [SerializeField] private TextMeshProUGUI _textoPrecioModificar;
    [SerializeField] private Button          _btnAplicarModificacion;

    // ─── Panel Reparar ────────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoBarcoReparar;
    [SerializeField] private TextMeshProUGUI _textoVidaReparar;
    [SerializeField] private TextMeshProUGUI _textoPrecioReparar;
    [SerializeField] private Button          _btnReparar;

    // ─── Panel Vender ─────────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoBarcoVender;
    [SerializeField] private TextMeshProUGUI _textoValorVenta;
    [SerializeField] private Button          _btnVender;

    // ─── Flechas Construir ────────────────────────────────────────────────────
    [SerializeField] private Button _btnCascoIzq;
    [SerializeField] private Button _btnCascoDer;
    [SerializeField] private Button _btnArmamentoIzq;
    [SerializeField] private Button _btnArmamentoDer;
    [SerializeField] private Button _btnVelasIzq;
    [SerializeField] private Button _btnVelasDer;
    [SerializeField] private Button _btnBodegaIzq;
    [SerializeField] private Button _btnBodegaDer;

    // ─── Flechas Modificar ────────────────────────────────────────────────────
    [SerializeField] private Button _btnBarcoModIzq;
    [SerializeField] private Button _btnBarcoModDer;
    [SerializeField] private Button _btnArmamentoModIzq;
    [SerializeField] private Button _btnArmamentoModDer;
    [SerializeField] private Button _btnVelasModIzq;
    [SerializeField] private Button _btnVelasModDer;
    [SerializeField] private Button _btnBodegaModIzq;
    [SerializeField] private Button _btnBodegaModDer;

    // ─── Flechas Reparar ──────────────────────────────────────────────────────
    [SerializeField] private Button _btnBarcoRepIzq;
    [SerializeField] private Button _btnBarcoRepDer;

    // ─── Flechas Vender ───────────────────────────────────────────────────────
    [SerializeField] private Button _btnBarcoVenIzq;
    [SerializeField] private Button _btnBarcoVenDer;

    // ─── Índices de selección ─────────────────────────────────────────────────

    // Construir
    private int _indiceCasco             = 0;
    private int _indiceModuloArmamento   = 0;
    private int _indiceModuloVelas       = 0;
    private int _indiceModuloBodega      = 0;

    // Modificar
    private int _indiceBarcoMod          = 0;
    private int _indiceModuloArmMod      = 0;
    private int _indiceModuloVelMod      = 0;
    private int _indiceModuloBodMod      = 0;

    // Reparar / Vender
    private int _indiceBarcoRep          = 0;
    private int _indiceBarcoVen          = 0;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Menú principal
        _panelAstillero.SetActive(false);

        // Flechas Construir — casco
        _btnCascoIzq.onClick.AddListener(() => CiclarCasco(-1));
        _btnCascoDer.onClick.AddListener(() => CiclarCasco(+1));

        // Flechas Construir — módulos
        _btnArmamentoIzq.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Armamento, -1));
        _btnArmamentoDer.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Armamento, +1));
        _btnVelasIzq.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Velas, -1));
        _btnVelasDer.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Velas, +1));
        _btnBodegaIzq.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Bodega, -1));
        _btnBodegaDer.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Bodega, +1));

        // Botón Construir
        _btnConstruir.onClick.AddListener(OnConstruir);

        // Flechas Modificar — barco y módulos
        _btnBarcoModIzq.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMod, -1, SincronizarSelectoresModificar));
        _btnBarcoModDer.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMod, +1, SincronizarSelectoresModificar));
        _btnArmamentoModIzq.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Armamento, -1));
        _btnArmamentoModDer.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Armamento, +1));
        _btnVelasModIzq.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Velas, -1));
        _btnVelasModDer.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Velas, +1));
        _btnBodegaModIzq.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Bodega, -1));
        _btnBodegaModDer.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Bodega, +1));

        // Botón Aplicar modificación
        _btnAplicarModificacion.onClick.AddListener(OnAplicarModificacion);

        // Flechas Reparar
        _btnBarcoRepIzq.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoRep, -1, null));
        _btnBarcoRepDer.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoRep, +1, null));
        _btnReparar.onClick.AddListener(OnReparar);

        // Flechas Vender
        _btnBarcoVenIzq.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoVen, -1, null));
        _btnBarcoVenDer.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoVen, +1, null));
        _btnVender.onClick.AddListener(OnVender);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el panel del astillero y muestra el menú principal.
    /// Refresca todos los datos antes de mostrar.
    /// </summary>
    public void AbrirAstillero()
    {
        _panelAstillero.SetActive(true);
        MostrarPanel(0);
        RefrescarUI();
    }

    /// <summary>
    /// Cierra el panel del astillero.
    /// </summary>
    public void CerrarAstillero()
    {
        _panelAstillero.SetActive(false);
    }

    /// <summary>
    /// Actualiza todos los textos y estados de los botones según los índices actuales.
    /// Llamar siempre que cambie cualquier selector.
    /// </summary>
    public void RefrescarUI()
    {
        RefrescarPanelConstruir();
        RefrescarPanelModificar();
        RefrescarPanelReparar();
        RefrescarPanelVender();
    }

    // ─── Navegación de subpaneles ─────────────────────────────────────────────

    /// <summary>Muestra el subpanel indicado y oculta los demás.</summary>
    /// <param name="indice">0=Menú, 1=Construir, 2=Modificar, 3=Reparar, 4=Vender.</param>
    public void MostrarPanel(int indice)
    {
        _panelMenu.SetActive(indice == 0);
        _panelConstruir.SetActive(indice == 1);
        _panelModificar.SetActive(indice == 2);
        _panelReparar.SetActive(indice == 3);
        _panelVender.SetActive(indice == 4);

        if (indice == 2) SincronizarSelectoresModificar();
    }

    // ─── Ciclar selectores ────────────────────────────────────────────────────

    private void CiclarCasco(int dir)
    {
        var cascos = AstilleroManager.Instance.CascosDisponibles;
        if (cascos == null || cascos.Count == 0) return;
        _indiceCasco = Modulo(_indiceCasco + dir, cascos.Count);
        RefrescarUI();
    }

    private void CiclarModuloConstruir(TipoModulo tipo, int dir)
    {
        var lista = ObtenerModulosFiltrados(tipo);
        int nuevoIndice = Modulo(GetIndiceConstruir(tipo) + dir, lista.Count);
        SetIndiceConstruir(tipo, nuevoIndice);
        RefrescarUI();
    }

    private void CiclarModuloMod(TipoModulo tipo, int dir)
    {
        var lista = ObtenerModulosFiltrados(tipo);
        int nuevoIndice = Modulo(GetIndiceMod(tipo) + dir, lista.Count);
        SetIndiceMod(tipo, nuevoIndice);
        RefrescarUI();
    }

    private void CiclarBarco(ref int indice, int dir, System.Action postCiclo)
    {
        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        if (barcos == null || barcos.Count == 0) return;
        indice = Modulo(indice + dir, barcos.Count);
        postCiclo?.Invoke();
        RefrescarUI();
    }

    // ─── Sincronizar Modificar con el barco seleccionado ─────────────────────

    private void SincronizarSelectoresModificar()
    {
        BarcoJugador barco = ObtenerBarcoMod();
        if (barco == null) return;

        _indiceModuloArmMod = IndiceDeModuloEnLista(barco.ObtenerModuloPorTipo(TipoModulo.Armamento), TipoModulo.Armamento);
        _indiceModuloVelMod = IndiceDeModuloEnLista(barco.ObtenerModuloPorTipo(TipoModulo.Velas),     TipoModulo.Velas);
        _indiceModuloBodMod = IndiceDeModuloEnLista(barco.ObtenerModuloPorTipo(TipoModulo.Bodega),    TipoModulo.Bodega);
    }

    // ─── Refresco por panel ───────────────────────────────────────────────────

    private void RefrescarPanelConstruir()
    {
        var cascos = AstilleroManager.Instance?.CascosDisponibles;
        if (cascos == null || cascos.Count == 0)
        {
            _textoCascoConstruir.text = "Sin cascos disponibles";
            return;
        }

        IBarco casco = cascos[_indiceCasco];
        _textoCascoConstruir.text        = casco.NombreCasco;
        _statsVidaCasco.text             = $"Vida: {casco.VidaBase}";
        _statsVelocidadCasco.text        = $"Velocidad: {casco.VelocidadBase}";
        _statsManiobrabilidadCasco.text  = $"Maniobrabilidad: {casco.ManiobrabilidadBase}";
        _statsCargaCasco.text            = $"Carga: {casco.CapacidadCargaBase}";
        _statsSlotsDisponibles.text      = $"Slots: {casco.CapacidadModulos}";

        ModuloBarcoData modArm = ModuloSeleccionadoConstruir(TipoModulo.Armamento);
        ModuloBarcoData modVel = ModuloSeleccionadoConstruir(TipoModulo.Velas);
        ModuloBarcoData modBod = ModuloSeleccionadoConstruir(TipoModulo.Bodega);

        _textoModuloArmamento.text = modArm != null ? modArm.nombreModulo : "Ninguno";
        _textoModuloVelas.text     = modVel != null ? modVel.nombreModulo : "Ninguno";
        _textoModuloBodega.text    = modBod != null ? modBod.nombreModulo : "Ninguno";

        _statsArmamento.text = modArm != null ? FormatearDeltasModulo(modArm) : "—";
        _statsVelas.text     = modVel != null ? FormatearDeltasModulo(modVel) : "—";
        _statsBodega.text    = modBod != null ? FormatearDeltasModulo(modBod) : "—";

        int precioTotal = casco.CosteOro
            + (modArm != null ? modArm.costeOro : 0)
            + (modVel != null ? modVel.costeOro : 0)
            + (modBod != null ? modBod.costeOro : 0);
        _textoPrecioConstruir.text = $"Precio: {precioTotal} oro";
    }

    private void RefrescarPanelModificar()
    {
        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        if (barcos == null || barcos.Count == 0)
        {
            _textoBarcoModificar.text    = "Sin barcos en la flota";
            _textoPrecioModificar.text   = "";
            return;
        }

        BarcoJugador barco = ObtenerBarcoMod();
        _textoBarcoModificar.text = barco?.Nombre ?? "—";

        RefrescarSelectorModMod(TipoModulo.Armamento, barco, _indiceModuloArmMod, _textoModuloArmamentoMod, _statsArmamentoMod);
        RefrescarSelectorModMod(TipoModulo.Velas,     barco, _indiceModuloVelMod, _textoModuloVelasMod,     _statsVelasMod);
        RefrescarSelectorModMod(TipoModulo.Bodega,    barco, _indiceModuloBodMod, _textoModuloBodegaMod,    _statsBodegaMod);

        int costeNeto = CalcularCosteNetoModificacion(barco);
        _textoPrecioModificar.text = costeNeto >= 0
            ? $"Coste: {costeNeto} oro"
            : $"Recibirás {-costeNeto} oro";
    }

    private void RefrescarSelectorModMod(TipoModulo tipo, BarcoJugador barco,
        int indiceSeleccionado, TextMeshProUGUI textoNombre, TextMeshProUGUI textoStats)
    {
        var lista = ObtenerModulosFiltrados(tipo);
        ModuloBarcoData seleccionado = indiceSeleccionado < lista.Count ? lista[indiceSeleccionado] : null;
        ModuloBarcoData instalado    = barco?.ObtenerModuloPorTipo(tipo);

        textoNombre.text = seleccionado != null ? seleccionado.nombreModulo : "Ninguno";

        if (seleccionado == null && instalado == null)
        { textoStats.text = "—"; return; }

        int dVida  = (seleccionado?.deltaVida          ?? 0) - (instalado?.deltaVida          ?? 0);
        int dVel   = (seleccionado?.deltaVelocidad     ?? 0) - (instalado?.deltaVelocidad     ?? 0);
        int dMan   = (seleccionado?.deltaManiobrabilidad ?? 0) - (instalado?.deltaManiobrabilidad ?? 0);
        int dCarga = (seleccionado?.deltaCargaMaxima   ?? 0) - (instalado?.deltaCargaMaxima   ?? 0);
        int dFuerz = (seleccionado?.deltaFuerzaCombate ?? 0) - (instalado?.deltaFuerzaCombate ?? 0);

        textoStats.text = FormatearDeltas(dVida, dVel, dMan, dCarga, dFuerz, "respecto al actual");
    }

    private void RefrescarPanelReparar()
    {
        BarcoJugador barco = ObtenerBarcoRep();
        if (barco == null) { _textoBarcoReparar.text = "Sin barcos"; return; }

        int danio = barco.VidaTotal - barco.VidaActual;
        _textoBarcoReparar.text  = barco.Nombre;
        _textoVidaReparar.text   = $"Vida: {barco.VidaActual} / {barco.VidaTotal}";
        _textoPrecioReparar.text = danio > 0 ? $"Coste: {danio * 10} oro" : "Sin daño";
    }

    private void RefrescarPanelVender()
    {
        BarcoJugador barco = ObtenerBarcoVen();
        if (barco == null) { _textoBarcoVender.text = "Sin barcos"; return; }

        int costeTotal = barco.CascoBase.CosteOro;
        foreach (ModuloBarcoData m in barco.ModulosInstalados) costeTotal += m.costeOro;
        long valorVenta = (long)(costeTotal * 0.5f);

        _textoBarcoVender.text = barco.Nombre;
        _textoValorVenta.text  = $"Valor de venta: {valorVenta} oro";
    }

    // ─── Operaciones ──────────────────────────────────────────────────────────

    private void OnConstruir()
    {
        var cascos = AstilleroManager.Instance.CascosDisponibles;
        if (cascos == null || cascos.Count == 0) return;

        IBarco casco = cascos[_indiceCasco];
        string nombre = GenerarNombreBarco();

        ResultadoOperacion res = AstilleroManager.Instance.ComprarBarco(casco, nombre);
        if (!res.Exito) { Debug.LogWarning($"[AstilleroUI] Construir: {res.MensajeError}"); return; }

        // Recuperar el barco recién añadido (último de la lista)
        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        BarcoJugador nuevoBarco = barcos.Count > 0 ? barcos[barcos.Count - 1] : null;

        if (nuevoBarco != null)
        {
            InstalarModuloSiSeleccionado(nuevoBarco, TipoModulo.Armamento, _indiceModuloArmamento);
            InstalarModuloSiSeleccionado(nuevoBarco, TipoModulo.Velas,     _indiceModuloVelas);
            InstalarModuloSiSeleccionado(nuevoBarco, TipoModulo.Bodega,    _indiceModuloBodega);
        }

        Debug.Log($"[AstilleroUI] Barco construido: {nombre} con casco {casco.NombreCasco}.");
        RefrescarUI();
    }

    private void InstalarModuloSiSeleccionado(BarcoJugador barco, TipoModulo tipo, int indice)
    {
        if (indice == 0) return; // índice 0 = Ninguno
        var lista = ObtenerModulosFiltrados(tipo);
        if (indice >= lista.Count) return;
        ModuloBarcoData modulo = lista[indice];
        if (modulo == null) return;
        ResultadoOperacion r = AstilleroManager.Instance.InstalarModulo(barco, modulo);
        if (!r.Exito) Debug.LogWarning($"[AstilleroUI] InstalarModulo({tipo}): {r.MensajeError}");
    }

    private void OnAplicarModificacion()
    {
        BarcoJugador barco = ObtenerBarcoMod();
        if (barco == null) return;

        AplicarCambioModulo(barco, TipoModulo.Armamento, _indiceModuloArmMod);
        AplicarCambioModulo(barco, TipoModulo.Velas,     _indiceModuloVelMod);
        AplicarCambioModulo(barco, TipoModulo.Bodega,    _indiceModuloBodMod);

        Debug.Log($"[AstilleroUI] Modificación aplicada a '{barco.Nombre}'.");
        SincronizarSelectoresModificar();
        RefrescarUI();
    }

    private void AplicarCambioModulo(BarcoJugador barco, TipoModulo tipo, int indiceSeleccionado)
    {
        var lista      = ObtenerModulosFiltrados(tipo);
        ModuloBarcoData seleccionado = indiceSeleccionado < lista.Count ? lista[indiceSeleccionado] : null;
        ModuloBarcoData instalado    = barco.ObtenerModuloPorTipo(tipo);

        if (seleccionado == instalado) return; // sin cambio

        if (seleccionado == null)
        {
            // Selector en Ninguno: desinstalar sin coste
            if (instalado != null) barco.DesinstalarModulo(instalado);
            return;
        }

        ResultadoOperacion r = AstilleroManager.Instance.InstalarModulo(barco, seleccionado);
        if (!r.Exito) Debug.LogWarning($"[AstilleroUI] Modificar {tipo}: {r.MensajeError}");
    }

    private void OnReparar()
    {
        BarcoJugador barco = ObtenerBarcoRep();
        if (barco == null) return;

        ResultadoOperacion r = AstilleroManager.Instance.RepararBarco(barco);
        Debug.Log(r.Exito
            ? $"[AstilleroUI] '{barco.Nombre}' reparado."
            : $"[AstilleroUI] Reparar: {r.MensajeError}");

        RefrescarUI();
    }

    private void OnVender()
    {
        BarcoJugador barco = ObtenerBarcoVen();
        if (barco == null) return;

        ResultadoOperacion r = AstilleroManager.Instance.VenderBarco(barco);
        Debug.Log(r.Exito
            ? $"[AstilleroUI] '{barco.Nombre}' vendido."
            : $"[AstilleroUI] Vender: {r.MensajeError}");

        _indiceBarcoVen = 0;
        MostrarPanel(0);
        RefrescarUI();
    }

    // ─── Helpers — nombre de barco ───────────────────────────────────────────

    /// <summary>
    /// Genera un nombre temático para el nuevo barco eligiendo aleatoriamente entre
    /// un repertorio de 50 nombres hanseáticos y mediterráneos.
    /// Reintenta hasta 10 veces para evitar duplicados con barcos ya existentes en
    /// la flota del jugador. Si no encuentra nombre libre, devuelve un fallback
    /// numérico basado en el tamaño actual de la flota.
    /// </summary>
    /// <returns>Nombre único para el barco recién construido.</returns>
    private static string GenerarNombreBarco()
    {
        string[] _nombres = new[]
        {
            "Der Adler", "Hansekogge", "Die Möwe", "Lubecker Bär", "Nordstern",
            "Gott Mit Uns", "Das Einhorn", "Silberfisch", "Meereswind", "Eisvogel",
            "Santa María", "San Juan", "La Esperanza", "El Halcón", "Mar del Norte",
            "La Fortuna", "Viento del Sur", "San Cristóbal", "El Trueno", "La Paloma",
            "Santa Cruz", "Madonna del Mare", "San Giorgio", "La Serenissima", "Stella Maris",
            "Sant'Elmo", "Il Corvo", "Aquila d'Oro", "La Sirena", "Buona Fortuna",
            "L'Étoile du Nord", "La Couronne", "Saint Michel", "Le Faucon", "Fleur de Lys",
            "La Bonne Chance", "L'Hirondelle", "Saint Jacques", "Le Lion d'Or", "La Tempête",
            "De Gouden Leeuw", "Het Anker", "De Zeemeeuw", "Vliegende Hollander", "Het Zwaard",
            "Konrad von Lübeck", "Heinrich der Seefahrer", "Wilhelm der Starke", "Klaus Störtebeker", "Der Hanseat"
        };

        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        var nombresUsados = new System.Collections.Generic.HashSet<string>();
        foreach (var b in barcos) nombresUsados.Add(b.Nombre);

        for (int i = 0; i < 10; i++)
        {
            string candidato = _nombres[UnityEngine.Random.Range(0, _nombres.Length)];
            if (!nombresUsados.Contains(candidato)) return candidato;
        }

        return "Barco_" + (barcos.Count + 1);
    }

    // ─── Helpers — índices ────────────────────────────────────────────────────

    private int GetIndiceConstruir(TipoModulo tipo) => tipo switch
    {
        TipoModulo.Armamento => _indiceModuloArmamento,
        TipoModulo.Velas     => _indiceModuloVelas,
        _                    => _indiceModuloBodega,
    };

    private void SetIndiceConstruir(TipoModulo tipo, int valor)
    {
        switch (tipo)
        {
            case TipoModulo.Armamento: _indiceModuloArmamento = valor; break;
            case TipoModulo.Velas:     _indiceModuloVelas     = valor; break;
            default:                   _indiceModuloBodega     = valor; break;
        }
    }

    private int GetIndiceMod(TipoModulo tipo) => tipo switch
    {
        TipoModulo.Armamento => _indiceModuloArmMod,
        TipoModulo.Velas     => _indiceModuloVelMod,
        _                    => _indiceModuloBodMod,
    };

    private void SetIndiceMod(TipoModulo tipo, int valor)
    {
        switch (tipo)
        {
            case TipoModulo.Armamento: _indiceModuloArmMod = valor; break;
            case TipoModulo.Velas:     _indiceModuloVelMod = valor; break;
            default:                   _indiceModuloBodMod = valor; break;
        }
    }

    // ─── Helpers — obtener barco seleccionado ─────────────────────────────────

    private BarcoJugador ObtenerBarcoMod()  => ObtenerBarcoEnIndice(_indiceBarcoMod);
    private BarcoJugador ObtenerBarcoRep()  => ObtenerBarcoEnIndice(_indiceBarcoRep);
    private BarcoJugador ObtenerBarcoVen()  => ObtenerBarcoEnIndice(_indiceBarcoVen);

    private BarcoJugador ObtenerBarcoEnIndice(int indice)
    {
        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        if (barcos == null || barcos.Count == 0) return null;
        return barcos[Modulo(indice, barcos.Count)];
    }

    // ─── Helpers — listas de módulos ─────────────────────────────────────────

    /// <summary>
    /// Devuelve una lista con <c>null</c> en el índice 0 (Ninguno) seguida de los módulos
    /// de <see cref="AstilleroManager.ModulosDisponibles"/> filtrados por tipo.
    /// </summary>
    private List<ModuloBarcoData> ObtenerModulosFiltrados(TipoModulo tipo)
    {
        var resultado = new List<ModuloBarcoData> { null }; // índice 0 = Ninguno
        var todos = AstilleroManager.Instance?.ModulosDisponibles;
        if (todos == null) return resultado;
        foreach (ModuloBarcoData m in todos)
            if (m != null && m.tipoModulo == tipo) resultado.Add(m);
        return resultado;
    }

    private ModuloBarcoData ModuloSeleccionadoConstruir(TipoModulo tipo)
    {
        var lista  = ObtenerModulosFiltrados(tipo);
        int indice = GetIndiceConstruir(tipo);
        return indice < lista.Count ? lista[indice] : null;
    }

    private int IndiceDeModuloEnLista(ModuloBarcoData modulo, TipoModulo tipo)
    {
        if (modulo == null) return 0;
        var lista = ObtenerModulosFiltrados(tipo);
        for (int i = 0; i < lista.Count; i++)
            if (lista[i] == modulo) return i;
        return 0;
    }

    // ─── Helpers — coste neto modificación ───────────────────────────────────

    private int CalcularCosteNetoModificacion(BarcoJugador barco)
    {
        if (barco == null) return 0;
        int total = 0;
        total += CosteNetoTipo(barco, TipoModulo.Armamento, _indiceModuloArmMod);
        total += CosteNetoTipo(barco, TipoModulo.Velas,     _indiceModuloVelMod);
        total += CosteNetoTipo(barco, TipoModulo.Bodega,    _indiceModuloBodMod);
        return total;
    }

    private int CosteNetoTipo(BarcoJugador barco, TipoModulo tipo, int indiceSeleccionado)
    {
        var lista      = ObtenerModulosFiltrados(tipo);
        ModuloBarcoData sel = indiceSeleccionado < lista.Count ? lista[indiceSeleccionado] : null;
        ModuloBarcoData ins = barco.ObtenerModuloPorTipo(tipo);

        if (sel == ins) return 0;                                              // sin cambio
        if (sel == null) return -Mathf.RoundToInt((ins?.costeOro ?? 0) * 0.5f); // desinstalar = ingreso
        if (ins != null) return sel.costeOro - Mathf.RoundToInt(ins.costeOro * 0.5f); // swap
        return sel.costeOro;                                                   // instalación nueva
    }

    // ─── Helpers — formateo ───────────────────────────────────────────────────

    private string FormatearDeltasModulo(ModuloBarcoData m)
        => FormatearDeltas(m.deltaVida, m.deltaVelocidad, m.deltaManiobrabilidad,
                           m.deltaCargaMaxima, m.deltaFuerzaCombate, "");

    private string FormatearDeltas(int vida, int vel, int man, int carga, int fuerza, string sufijo)
    {
        var partes = new List<string>();
        if (vida  != 0) partes.Add($"{vida:+#;-#;0} vida");
        if (vel   != 0) partes.Add($"{vel:+#;-#;0} velocidad");
        if (man   != 0) partes.Add($"{man:+#;-#;0} maniob.");
        if (carga != 0) partes.Add($"{carga:+#;-#;0} carga");
        if (fuerza!= 0) partes.Add($"{fuerza:+#;-#;0} fuerza");
        string base_ = partes.Count > 0 ? string.Join(", ", partes) : "Sin cambios";
        return string.IsNullOrEmpty(sufijo) ? base_ : $"{base_} {sufijo}";
    }

    // ─── Helpers — matemáticas ────────────────────────────────────────────────

    private static int Modulo(int valor, int total)
        => total <= 0 ? 0 : ((valor % total) + total) % total;
}
