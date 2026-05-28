using System.Collections.Generic;
using System.Linq;
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

    // ─── Construir — textos ───────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _txtCascoSeleccionado;
    [SerializeField] private TextMeshProUGUI _txtVidaCasco;
    [SerializeField] private TextMeshProUGUI _txtVelCasco;
    [SerializeField] private TextMeshProUGUI _txtManCasco;
    [SerializeField] private TextMeshProUGUI _txtCargaCasco;
    [SerializeField] private TextMeshProUGUI _txtSlotsCasco;
    [SerializeField] private TextMeshProUGUI _txtModuloArmamento;
    [SerializeField] private TextMeshProUGUI _txtModuloVelas;
    [SerializeField] private TextMeshProUGUI _txtModuloBodega;
    [SerializeField] private TextMeshProUGUI _txtPrecioConstruir;

    // ─── Menú principal — botones ─────────────────────────────────────────────
    [SerializeField] private Button _btnMenuConstruir;
    [SerializeField] private Button _btnMenuModificar;
    [SerializeField] private Button _btnMenuReparar;
    [SerializeField] private Button _btnMenuVender;
    [SerializeField] private Button _btnMenuCerrar;

    // ─── Construir — botones ──────────────────────────────────────────────────
    [SerializeField] private Button _btnCascoIzq;
    [SerializeField] private Button _btnCascoDer;
    [SerializeField] private Button _btnModuloArmamentoIzq;
    [SerializeField] private Button _btnModuloArmamentoDer;
    [SerializeField] private Button _btnModuloVelasIzq;
    [SerializeField] private Button _btnModuloVelasDer;
    [SerializeField] private Button _btnModuloBodegaIzq;
    [SerializeField] private Button _btnModuloBodegaDer;
    [SerializeField] private Button _btnConstruirConfirmar;
    [SerializeField] private Button _btnVolverConstruir;
    [SerializeField] private Button _btnVolverModificar;
    [SerializeField] private Button _btnVolverReparar;
    [SerializeField] private Button _btnVolverVender;

    // ─── Modificar — textos ───────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _txtBarcoMod;
    [SerializeField] private TextMeshProUGUI _txtModuloArmMod;
    [SerializeField] private TextMeshProUGUI _txtModuloVelMod;
    [SerializeField] private TextMeshProUGUI _txtModuloBodMod;
    [SerializeField] private TextMeshProUGUI _txtModulosInstalados;
    [SerializeField] private TextMeshProUGUI _txtDeltaStats;

    // ─── Modificar — botones ──────────────────────────────────────────────────
    [SerializeField] private Button _btnBarcoModIzq;
    [SerializeField] private Button _btnBarcoModDer;
    [SerializeField] private Button _btnArmamentoModIzq;
    [SerializeField] private Button _btnArmamentoModDer;
    [SerializeField] private Button _btnVelasModIzq;
    [SerializeField] private Button _btnVelasModDer;
    [SerializeField] private Button _btnBodegaModIzq;
    [SerializeField] private Button _btnBodegaModDer;
    [SerializeField] private Button _btnModificarConfirmar;

    // ─── Reparar — textos ─────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _txtBarcoRep;
    [SerializeField] private TextMeshProUGUI _txtVidaRep;
    [SerializeField] private TextMeshProUGUI _txtCosteRep;

    // ─── Reparar — botones ────────────────────────────────────────────────────
    [SerializeField] private Button _btnBarcoRepIzq;
    [SerializeField] private Button _btnBarcoRepDer;
    [SerializeField] private Button _btnRepararConfirmar;

    // ─── Vender — textos ──────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _txtBarcoVen;
    [SerializeField] private TextMeshProUGUI _txtStatsVen;
    [SerializeField] private TextMeshProUGUI _txtValorVen;

    // ─── Vender — botones ─────────────────────────────────────────────────────
    [SerializeField] private Button _btnBarcoVenIzq;
    [SerializeField] private Button _btnBarcoVenDer;
    [SerializeField] private Button _btnVenderConfirmar;

    // ─── Feedback compartido ──────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _txtFeedback;

    // ─── Índices de selección ─────────────────────────────────────────────────
    private int _indiceCasco           = 0;
    private int _indiceModuloArmamento = 0;
    private int _indiceModuloVelas     = 0;
    private int _indiceModuloBodega    = 0;

    private int _indiceBarcoMod        = 0;
    private int _indiceModuloArmMod    = 0;
    private int _indiceModuloVelMod    = 0;
    private int _indiceModuloBodMod    = 0;

    private int _indiceBarcoRep        = 0;
    private int _indiceBarcoVen        = 0;

    // ─── Referencia lazy a PanelFlotaUI ─────────────────────────────────────
    private PanelFlotaUI _panelFlotaRef;
    private PanelFlotaUI PanelFlota
    {
        get
        {
            if (_panelFlotaRef == null)
                _panelFlotaRef = FindFirstObjectByType<PanelFlotaUI>();
            return _panelFlotaRef;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _panelAstillero.SetActive(false);

        // Menú principal
        if (_btnMenuConstruir != null) _btnMenuConstruir.onClick.AddListener(() => MostrarPanel(1));
        if (_btnMenuModificar != null) _btnMenuModificar.onClick.AddListener(() => MostrarPanel(2));
        if (_btnMenuReparar   != null) _btnMenuReparar  .onClick.AddListener(() => MostrarPanel(3));
        if (_btnMenuVender    != null) _btnMenuVender   .onClick.AddListener(() => MostrarPanel(4));
        if (_btnMenuCerrar    != null) _btnMenuCerrar   .onClick.AddListener(CerrarAstillero);

        // Construir — casco
        _btnCascoIzq.onClick.AddListener(() => CiclarCasco(-1));
        _btnCascoDer.onClick.AddListener(() => CiclarCasco(+1));

        // Construir — módulos
        _btnModuloArmamentoIzq.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Armamento, -1));
        _btnModuloArmamentoDer.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Armamento, +1));
        _btnModuloVelasIzq.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Velas, -1));
        _btnModuloVelasDer.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Velas, +1));
        _btnModuloBodegaIzq.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Bodega, -1));
        _btnModuloBodegaDer.onClick.AddListener(() => CiclarModuloConstruir(TipoModulo.Bodega, +1));

        _btnConstruirConfirmar.onClick.AddListener(OnConstruirConfirmar);
        _btnVolverConstruir.onClick.AddListener(() => MostrarPanel(0));
        if (_btnVolverModificar != null) _btnVolverModificar.onClick.AddListener(() => MostrarPanel(0));
        if (_btnVolverReparar   != null) _btnVolverReparar  .onClick.AddListener(() => MostrarPanel(0));
        if (_btnVolverVender    != null) _btnVolverVender   .onClick.AddListener(() => MostrarPanel(0));

        // Modificar — barco y módulos
        _btnBarcoModIzq.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMod, -1, SincronizarSelectoresModificar));
        _btnBarcoModDer.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMod, +1, SincronizarSelectoresModificar));
        _btnArmamentoModIzq.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Armamento, -1));
        _btnArmamentoModDer.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Armamento, +1));
        _btnVelasModIzq.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Velas, -1));
        _btnVelasModDer.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Velas, +1));
        _btnBodegaModIzq.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Bodega, -1));
        _btnBodegaModDer.onClick.AddListener(() => CiclarModuloMod(TipoModulo.Bodega, +1));
        _btnModificarConfirmar.onClick.AddListener(OnModificarConfirmar);

        // Reparar
        _btnBarcoRepIzq.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoRep, -1, RefrescarUIReparar));
        _btnBarcoRepDer.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoRep, +1, RefrescarUIReparar));
        _btnRepararConfirmar.onClick.AddListener(OnRepararConfirmar);

        // Vender
        _btnBarcoVenIzq.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoVen, -1, RefrescarUIVender));
        _btnBarcoVenDer.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoVen, +1, RefrescarUIVender));
        _btnVenderConfirmar.onClick.AddListener(OnVenderConfirmar);
    }

    // ─────────────────────────────────────────────────────────────────────────

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el panel del astillero mostrando directamente el panel de construcción
    /// con las stats del Cog (primer casco) ya visibles.
    /// </summary>
    public void AbrirAstillero()
    {
        _panelAstillero.SetActive(true);
        _indiceCasco = 0;
        MostrarPanel(0);
    }

    /// <summary>
    /// Cierra el panel del astillero y reactiva el botón de mapa en la pantalla de ciudad.
    /// </summary>
    public void CerrarAstillero()
    {
        _panelAstillero.SetActive(false);
        CiudadController.Instance?.ReactivarBotonMapa();
    }

    /// <summary>Muestra el subpanel indicado y oculta los demás.</summary>
    /// <param name="indice">0=Menú, 1=Construir, 2=Modificar, 3=Reparar, 4=Vender.</param>
    public void MostrarPanel(int indice)
    {
        _panelMenu.SetActive(indice == 0);
        _panelConstruir.SetActive(indice == 1);
        _panelModificar.SetActive(indice == 2);
        _panelReparar.SetActive(indice == 3);
        _panelVender.SetActive(indice == 4);

        LimpiarFeedback();

        switch (indice)
        {
            case 1: RefrescarUIConstruir();  break;
            case 2:
                SincronizarSelectoresModificar();
                RefrescarUIModificar();
                break;
            case 3: RefrescarUIReparar(); break;
            case 4: RefrescarUIVender();  break;
        }
    }

    // ─── Ciclar selectores ────────────────────────────────────────────────────

    private void CiclarCasco(int dir)
    {
        var cascos = AstilleroManager.Instance?.CascosDisponibles;
        if (cascos == null || cascos.Count == 0) return;
        _indiceCasco = Modulo(_indiceCasco + dir, cascos.Count);
        RefrescarUIConstruir();
    }

    private void CiclarModuloConstruir(TipoModulo tipo, int dir)
    {
        var lista = ObtenerModulosFiltrados(tipo);
        SetIndiceConstruir(tipo, Modulo(GetIndiceConstruir(tipo) + dir, lista.Count));
        RefrescarUIConstruir();
    }

    private void CiclarModuloMod(TipoModulo tipo, int dir)
    {
        var lista = ObtenerModulosFiltrados(tipo);
        SetIndiceMod(tipo, Modulo(GetIndiceMod(tipo) + dir, lista.Count));
        RefrescarUIModificar();
    }

    private void CiclarBarco(ref int indice, int dir, System.Action postCiclo)
    {
        var barcos = GameManager.Instance?.FlotaJugador?.Barcos;
        if (barcos == null || barcos.Count == 0) return;
        indice = Modulo(indice + dir, barcos.Count);
        postCiclo?.Invoke();
    }

    // ─── Sincronizar Modificar ────────────────────────────────────────────────

    private void SincronizarSelectoresModificar()
    {
        BarcoJugador barco = ObtenerBarcoMod();
        if (barco == null) return;
        _indiceModuloArmMod = IndiceDeModuloEnLista(barco.ObtenerModuloPorTipo(TipoModulo.Armamento), TipoModulo.Armamento);
        _indiceModuloVelMod = IndiceDeModuloEnLista(barco.ObtenerModuloPorTipo(TipoModulo.Velas),     TipoModulo.Velas);
        _indiceModuloBodMod = IndiceDeModuloEnLista(barco.ObtenerModuloPorTipo(TipoModulo.Bodega),    TipoModulo.Bodega);
    }

    // ─── Refrescos por subpanel ───────────────────────────────────────────────

    private void RefrescarUIConstruir()
    {
        var cascos = AstilleroManager.Instance?.CascosDisponibles;
        if (cascos == null || cascos.Count == 0)
        {
            _txtCascoSeleccionado.text = "Sin cascos disponibles";
            if (_txtVidaCasco  != null) _txtVidaCasco.text  = "Vida: —";
            if (_txtVelCasco   != null) _txtVelCasco.text   = "Vel: —";
            if (_txtManCasco   != null) _txtManCasco.text   = "Man: —";
            if (_txtCargaCasco != null) _txtCargaCasco.text = "Carga: —";
            if (_txtSlotsCasco != null) _txtSlotsCasco.text = "Módulos: —";
            _txtModuloArmamento.text   = "Ninguno";
            _txtModuloVelas.text       = "Ninguno";
            _txtModuloBodega.text      = "Ninguno";
            _txtPrecioConstruir.text   = "";
            return;
        }

        IBarco casco = cascos[_indiceCasco];
        _txtCascoSeleccionado.text = casco.NombreCasco;

        // Módulos seleccionados (se usan para stats y slots)
        ModuloBarcoData modArm = ModuloSeleccionadoConstruir(TipoModulo.Armamento);
        ModuloBarcoData modVel = ModuloSeleccionadoConstruir(TipoModulo.Velas);
        ModuloBarcoData modBod = ModuloSeleccionadoConstruir(TipoModulo.Bodega);

        // Stats dinámicas: base del casco + deltas de módulos seleccionados
        int vidaTotal  = casco.VidaBase            + (modArm?.deltaVida            ?? 0) + (modVel?.deltaVida            ?? 0) + (modBod?.deltaVida            ?? 0);
        int velTotal   = casco.VelocidadBase        + (modArm?.deltaVelocidad       ?? 0) + (modVel?.deltaVelocidad       ?? 0) + (modBod?.deltaVelocidad       ?? 0);
        int manTotal   = casco.ManiobrabilidadBase  + (modArm?.deltaManiobrabilidad ?? 0) + (modVel?.deltaManiobrabilidad ?? 0) + (modBod?.deltaManiobrabilidad ?? 0);
        int cargaTotal = casco.CapacidadCargaBase   + (modArm?.deltaCargaMaxima     ?? 0) + (modVel?.deltaCargaMaxima     ?? 0) + (modBod?.deltaCargaMaxima     ?? 0);

        if (_txtVidaCasco  != null) _txtVidaCasco.text  = $"Vida: {vidaTotal}";
        if (_txtVelCasco   != null) _txtVelCasco.text   = $"Vel: {velTotal}";
        if (_txtManCasco   != null) _txtManCasco.text   = $"Man: {manTotal}";
        if (_txtCargaCasco != null) _txtCargaCasco.text = $"Carga: {cargaTotal}";

        int slotsUsados = 0;
        if (modArm != null) slotsUsados += modArm.slotsCosto;
        if (modVel != null) slotsUsados += modVel.slotsCosto;
        if (modBod != null) slotsUsados += modBod.slotsCosto;
        if (_txtSlotsCasco != null) _txtSlotsCasco.text = $"Módulos: {slotsUsados}/{casco.CapacidadModulos}";

        _txtModuloArmamento.text = modArm != null ? modArm.nombreModulo : "Ninguno";
        _txtModuloVelas.text     = modVel != null ? modVel.nombreModulo : "Ninguno";
        _txtModuloBodega.text    = modBod != null ? modBod.nombreModulo : "Ninguno";

        int precio = casco.CosteOro
            + (modArm?.costeOro ?? 0)
            + (modVel?.costeOro ?? 0)
            + (modBod?.costeOro ?? 0);
        _txtPrecioConstruir.text = $"Precio: {precio} oro";
    }

    private void RefrescarUIModificar()
    {
        var barcos = GameManager.Instance?.FlotaJugador?.Barcos;
        if (barcos == null || barcos.Count == 0)
        {
            _txtBarcoMod.text         = "Sin barcos disponibles";
            _txtModulosInstalados.text = "";
            _txtDeltaStats.text        = "";
            return;
        }

        BarcoJugador barco = ObtenerBarcoMod();
        if (barco == null) return;

        _txtBarcoMod.text = barco.Nombre;

        // Nombre del módulo seleccionado en cada selector
        var listaArm = ObtenerModulosFiltrados(TipoModulo.Armamento);
        var listaVel = ObtenerModulosFiltrados(TipoModulo.Velas);
        var listaBod = ObtenerModulosFiltrados(TipoModulo.Bodega);
        if (_txtModuloArmMod != null) _txtModuloArmMod.text = _indiceModuloArmMod < listaArm.Count && listaArm[_indiceModuloArmMod] != null ? listaArm[_indiceModuloArmMod].nombreModulo : "Ninguno";
        if (_txtModuloVelMod != null) _txtModuloVelMod.text = _indiceModuloVelMod < listaVel.Count && listaVel[_indiceModuloVelMod] != null ? listaVel[_indiceModuloVelMod].nombreModulo : "Ninguno";
        if (_txtModuloBodMod != null) _txtModuloBodMod.text = _indiceModuloBodMod < listaBod.Count && listaBod[_indiceModuloBodMod] != null ? listaBod[_indiceModuloBodMod].nombreModulo : "Ninguno";

        // Módulos seleccionados en los selectores
        var lineas = new System.Text.StringBuilder();
        ModuloBarcoData selArm = ObtenerModulosFiltrados(TipoModulo.Armamento).ElementAtOrDefault(_indiceModuloArmMod);
        ModuloBarcoData selVel = ObtenerModulosFiltrados(TipoModulo.Velas).ElementAtOrDefault(_indiceModuloVelMod);
        ModuloBarcoData selBod = ObtenerModulosFiltrados(TipoModulo.Bodega).ElementAtOrDefault(_indiceModuloBodMod);
        if (selArm != null) lineas.AppendLine($"• {selArm.nombreModulo} (Armamento)");
        else lineas.AppendLine("• Ninguno (Armamento)");
        if (selVel != null) lineas.AppendLine($"• {selVel.nombreModulo} (Velas)");
        else lineas.AppendLine("• Ninguno (Velas)");
        if (selBod != null) lineas.AppendLine($"• {selBod.nombreModulo} (Bodega)");
        else lineas.AppendLine("• Ninguno (Bodega)");
        int slotsSeleccionados = 0;
        if (selArm != null) slotsSeleccionados += selArm.slotsCosto;
        if (selVel != null) slotsSeleccionados += selVel.slotsCosto;
        if (selBod != null) slotsSeleccionados += selBod.slotsCosto;
        int capacidadTotal = barco?.CascoBase?.CapacidadModulos ?? 0;
        bool slotsSuficientes = slotsSeleccionados <= capacidadTotal;
        bool oroCuficiente = GameManager.Instance == null ||
            GameManager.Instance.Dinero >= CalcularCosteNetoModificacion(barco);
        bool puedeInstalar = slotsSuficientes && oroCuficiente;
        if (_btnModificarConfirmar != null)
            _btnModificarConfirmar.interactable = puedeInstalar;
        if (_txtModulosInstalados != null)
            _txtModulosInstalados.text = $"Módulos: {slotsSeleccionados}/{capacidadTotal}\n" + lineas.ToString().TrimEnd();

        // Deltas de la selección respecto al estado actual
        int dVida  = DeltaModulo(barco, TipoModulo.Armamento, _indiceModuloArmMod, m => m.deltaVida)
                   + DeltaModulo(barco, TipoModulo.Velas,     _indiceModuloVelMod, m => m.deltaVida)
                   + DeltaModulo(barco, TipoModulo.Bodega,    _indiceModuloBodMod, m => m.deltaVida);
        int dVel   = DeltaModulo(barco, TipoModulo.Armamento, _indiceModuloArmMod, m => m.deltaVelocidad)
                   + DeltaModulo(barco, TipoModulo.Velas,     _indiceModuloVelMod, m => m.deltaVelocidad)
                   + DeltaModulo(barco, TipoModulo.Bodega,    _indiceModuloBodMod, m => m.deltaVelocidad);
        int dMan   = DeltaModulo(barco, TipoModulo.Armamento, _indiceModuloArmMod, m => m.deltaManiobrabilidad)
                   + DeltaModulo(barco, TipoModulo.Velas,     _indiceModuloVelMod, m => m.deltaManiobrabilidad)
                   + DeltaModulo(barco, TipoModulo.Bodega,    _indiceModuloBodMod, m => m.deltaManiobrabilidad);
        int dCarga = DeltaModulo(barco, TipoModulo.Armamento, _indiceModuloArmMod, m => m.deltaCargaMaxima)
                   + DeltaModulo(barco, TipoModulo.Velas,     _indiceModuloVelMod, m => m.deltaCargaMaxima)
                   + DeltaModulo(barco, TipoModulo.Bodega,    _indiceModuloBodMod, m => m.deltaCargaMaxima);

        int costeNeto = CalcularCosteNetoModificacion(barco);

        int vidaFinal   = barco.CascoBase.VidaBase            + (selArm?.deltaVida ?? 0)            + (selVel?.deltaVida ?? 0)            + (selBod?.deltaVida ?? 0);
        int velFinal    = barco.CascoBase.VelocidadBase        + (selArm?.deltaVelocidad ?? 0)        + (selVel?.deltaVelocidad ?? 0)        + (selBod?.deltaVelocidad ?? 0);
        int manFinal    = barco.CascoBase.ManiobrabilidadBase  + (selArm?.deltaManiobrabilidad ?? 0)  + (selVel?.deltaManiobrabilidad ?? 0)  + (selBod?.deltaManiobrabilidad ?? 0);
        int cargaFinal  = barco.CascoBase.CapacidadCargaBase   + (selArm?.deltaCargaMaxima ?? 0)      + (selVel?.deltaCargaMaxima ?? 0)      + (selBod?.deltaCargaMaxima ?? 0);
        int fuerzaFinal = (selArm?.deltaFuerzaCombate ?? 0)   + (selVel?.deltaFuerzaCombate ?? 0)   + (selBod?.deltaFuerzaCombate ?? 0);

        _txtDeltaStats.text = $"Proyectado: Vida {vidaFinal} | Vel {velFinal} | Man {manFinal} | Carga {cargaFinal} | Fuerza {fuerzaFinal}\n"
                            + FormatearDeltas(dVida, dVel, dMan, dCarga, "vs. actual")
                            + $"\nCoste neto: {(costeNeto >= 0 ? $"{costeNeto} oro" : $"Recibirás {-costeNeto} oro")}";
    }

    private void RefrescarUIReparar()
    {
        var barcos = GameManager.Instance?.FlotaJugador?.Barcos;
        if (barcos == null || barcos.Count == 0)
        {
            _txtBarcoRep.text  = "Sin barcos disponibles";
            _txtVidaRep.text   = "";
            _txtCosteRep.text  = "";
            return;
        }

        BarcoJugador barco = ObtenerBarcoRep();
        if (barco == null) return;

        int danio = barco.VidaTotal - barco.VidaActual;
        _txtBarcoRep.text  = barco.Nombre;
        _txtVidaRep.text   = $"Vida: {barco.VidaActual} / {barco.VidaTotal}";
        _txtCosteRep.text  = danio > 0 ? $"Coste: {danio * 10} oro" : "Sin daño";
    }

    private void RefrescarUIVender()
    {
        var barcos = GameManager.Instance?.FlotaJugador?.Barcos;
        if (barcos == null || barcos.Count == 0)
        {
            _txtBarcoVen.text  = "Sin barcos disponibles";
            if (_txtStatsVen != null) _txtStatsVen.text  = "";
            _txtValorVen.text  = "";
            return;
        }

        BarcoJugador barco = ObtenerBarcoVen();
        if (barco == null) return;

        int costeTotal = barco.CascoBase.CosteOro;
        foreach (ModuloBarcoData m in barco.ModulosInstalados) costeTotal += m.costeOro;
        long valorVenta = (long)(costeTotal * 0.5f);

        _txtBarcoVen.text  = barco.Nombre;
        if (_txtStatsVen != null)
            _txtStatsVen.text = $"Vida {barco.VidaActual}/{barco.VidaTotal}" +
                                $"  Vel {barco.VelocidadTotal}  Man {barco.ManiobrabilidadTotal}" +
                                $"  Carga {barco.CargaMaximaTotal}";
        _txtValorVen.text  = $"Valor de venta: {valorVenta} oro";
    }

    // ─── Operaciones de confirmación ──────────────────────────────────────────

    private void OnConstruirConfirmar()
    {
        if (AstilleroManager.Instance == null)
        {
            Debug.LogWarning("[AstilleroUI] AstilleroManager.Instance es null.");
            return;
        }

        var cascos = AstilleroManager.Instance.CascosDisponibles;
        if (cascos == null || cascos.Count == 0) return;

        IBarco casco  = cascos[_indiceCasco];
        string nombre = GenerarNombreBarco();

        ResultadoOperacion res = AstilleroManager.Instance.ComprarBarco(casco, nombre);
        if (!res.Exito)
        {
            MostrarFeedback(res.MensajeError, false);
            return;
        }

        // Instalar módulos seleccionados sobre el barco recién creado
        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        BarcoJugador nuevoBarco = barcos.Count > 0 ? barcos[barcos.Count - 1] : null;

        if (nuevoBarco != null)
        {
            InstalarModuloSiSeleccionado(nuevoBarco, TipoModulo.Armamento, _indiceModuloArmamento);
            InstalarModuloSiSeleccionado(nuevoBarco, TipoModulo.Velas,     _indiceModuloVelas);
            InstalarModuloSiSeleccionado(nuevoBarco, TipoModulo.Bodega,    _indiceModuloBodega);
        }

        MostrarFeedback($"'{nombre}' construido.", true);
        RefrescarUIConstruir();
        PanelFlota?.RefrescarPanel();
    }

    private void OnModificarConfirmar()
    {
        if (AstilleroManager.Instance == null)
        {
            Debug.LogWarning("[AstilleroUI] AstilleroManager.Instance es null.");
            return;
        }

        BarcoJugador barco = ObtenerBarcoMod();
        if (barco == null) return;

        AplicarCambioModulo(barco, TipoModulo.Armamento, _indiceModuloArmMod);
        AplicarCambioModulo(barco, TipoModulo.Velas,     _indiceModuloVelMod);
        AplicarCambioModulo(barco, TipoModulo.Bodega,    _indiceModuloBodMod);

        MostrarFeedback($"'{barco.Nombre}' modificado.", true);
        SincronizarSelectoresModificar();
        RefrescarUIModificar();
        PanelFlota?.RefrescarPanel();
    }

    private void OnRepararConfirmar()
    {
        if (AstilleroManager.Instance == null)
        {
            Debug.LogWarning("[AstilleroUI] AstilleroManager.Instance es null.");
            return;
        }

        BarcoJugador barco = ObtenerBarcoRep();
        if (barco == null) return;

        ResultadoOperacion r = AstilleroManager.Instance.RepararBarco(barco);
        MostrarFeedback(r.Exito ? $"'{barco.Nombre}' reparado." : r.MensajeError, r.Exito);
        RefrescarUIReparar();
        PanelFlota?.RefrescarPanel();
    }

    private void OnVenderConfirmar()
    {
        if (AstilleroManager.Instance == null)
        {
            Debug.LogWarning("[AstilleroUI] AstilleroManager.Instance es null.");
            return;
        }

        BarcoJugador barco = ObtenerBarcoVen();
        if (barco == null) return;

        string nombreBarco = barco.Nombre;
        ResultadoOperacion r = AstilleroManager.Instance.VenderBarco(barco);
        if (r.Exito)
        {
            _indiceBarcoVen = 0;
            MostrarFeedback($"'{nombreBarco}' vendido.", true);
            RefrescarUIVender();
            PanelFlota?.RefrescarPanel();
        }
        else
        {
            MostrarFeedback(r.MensajeError, false);
        }
    }

    // ─── Helpers — instalación ────────────────────────────────────────────────

    private void InstalarModuloSiSeleccionado(BarcoJugador barco, TipoModulo tipo, int indice)
    {
        if (indice == 0) return; // 0 = Ninguno
        var lista = ObtenerModulosFiltrados(tipo);
        if (indice >= lista.Count) return;
        ModuloBarcoData modulo = lista[indice];
        if (modulo == null) return;
        ResultadoOperacion r = AstilleroManager.Instance.InstalarModulo(barco, modulo);
        if (!r.Exito) Debug.LogWarning($"[AstilleroUI] InstalarModulo({tipo}): {r.MensajeError}");
    }

    private void AplicarCambioModulo(BarcoJugador barco, TipoModulo tipo, int indiceSeleccionado)
    {
        var lista            = ObtenerModulosFiltrados(tipo);
        ModuloBarcoData sel  = indiceSeleccionado < lista.Count ? lista[indiceSeleccionado] : null;
        ModuloBarcoData inst = barco.ObtenerModuloPorTipo(tipo);

        if (sel == inst) return;

        if (sel == null)
        {
            // Desinstalar sin coste
            if (inst != null) barco.DesinstalarModulo(inst);
            return;
        }

        ResultadoOperacion r = AstilleroManager.Instance.InstalarModulo(barco, sel);
        if (!r.Exito) Debug.LogWarning($"[AstilleroUI] Modificar({tipo}): {r.MensajeError}");
    }

    // ─── Helpers — delta stats modificar ─────────────────────────────────────

    private int DeltaModulo(BarcoJugador barco, TipoModulo tipo, int indice,
                             System.Func<ModuloBarcoData, int> selector)
    {
        var lista            = ObtenerModulosFiltrados(tipo);
        ModuloBarcoData sel  = indice < lista.Count ? lista[indice] : null;
        ModuloBarcoData inst = barco.ObtenerModuloPorTipo(tipo);
        return (sel != null ? selector(sel) : 0) - (inst != null ? selector(inst) : 0);
    }

    private int CalcularCosteNetoModificacion(BarcoJugador barco)
    {
        if (barco == null) return 0;
        return CosteNetoTipo(barco, TipoModulo.Armamento, _indiceModuloArmMod)
             + CosteNetoTipo(barco, TipoModulo.Velas,     _indiceModuloVelMod)
             + CosteNetoTipo(barco, TipoModulo.Bodega,    _indiceModuloBodMod);
    }

    private int CosteNetoTipo(BarcoJugador barco, TipoModulo tipo, int indiceSeleccionado)
    {
        var lista           = ObtenerModulosFiltrados(tipo);
        ModuloBarcoData sel = indiceSeleccionado < lista.Count ? lista[indiceSeleccionado] : null;
        ModuloBarcoData ins = barco.ObtenerModuloPorTipo(tipo);

        if (sel == ins)   return 0;
        if (sel == null)  return -Mathf.RoundToInt((ins?.costeOro ?? 0) * 0.5f);
        if (ins != null)  return sel.costeOro - Mathf.RoundToInt(ins.costeOro * 0.5f);
        return sel.costeOro;
    }

    // ─── Helpers — obtener barco ──────────────────────────────────────────────

    private BarcoJugador ObtenerBarcoMod() => ObtenerBarcoEnIndice(_indiceBarcoMod);
    private BarcoJugador ObtenerBarcoRep() => ObtenerBarcoEnIndice(_indiceBarcoRep);
    private BarcoJugador ObtenerBarcoVen() => ObtenerBarcoEnIndice(_indiceBarcoVen);

    private BarcoJugador ObtenerBarcoEnIndice(int indice)
    {
        var barcos = GameManager.Instance?.FlotaJugador?.Barcos;
        if (barcos == null || barcos.Count == 0) return null;
        return barcos[Modulo(indice, barcos.Count)];
    }

    // ─── Helpers — listas de módulos ─────────────────────────────────────────

    /// <summary>
    /// Devuelve una lista con <c>null</c> en índice 0 (Ninguno) seguida de los módulos
    /// filtrados por tipo. El índice 0 equivale a desinstalar/no instalar.
    /// </summary>
    private List<ModuloBarcoData> ObtenerModulosFiltrados(TipoModulo tipo)
    {
        var resultado = new List<ModuloBarcoData> { null };
        var todos = AstilleroManager.Instance?.ModulosDisponibles;
        if (todos == null) return resultado;
        int anioActual = SimulacionTiempo.Instance != null
            ? SimulacionTiempo.Instance.AñoActual
            : 0;
        foreach (ModuloBarcoData m in todos)
        {
            if (m == null || m.tipoModulo != tipo) continue;
            if (m.requierePolvora && anioActual < ModuloBarcoData.AnioDesbloqueoPolvoraJuego) continue;
            resultado.Add(m);
        }
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

    // ─── Helpers — feedback ───────────────────────────────────────────────────

    private void MostrarFeedback(string mensaje, bool exito)
    {
        if (_txtFeedback == null) return;
        _txtFeedback.text  = mensaje;
        _txtFeedback.color = exito ? Color.green : Color.red;
    }

    private void LimpiarFeedback()
    {
        if (_txtFeedback == null) return;
        _txtFeedback.text = "";
    }

    // ─── Helpers — nombre aleatorio ──────────────────────────────────────────

    /// <summary>
    /// Genera un nombre temático único para el nuevo barco.
    /// Reintenta hasta 10 veces para evitar duplicados con la flota existente.
    /// </summary>
    private static string GenerarNombreBarco()
    {
        string[] nombres =
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
        var usados = new HashSet<string>();
        foreach (BarcoJugador b in barcos) usados.Add(b.Nombre);

        for (int i = 0; i < 10; i++)
        {
            string candidato = nombres[Random.Range(0, nombres.Length)];
            if (!usados.Contains(candidato)) return candidato;
        }

        return "Barco_" + (barcos.Count + 1);
    }

    // ─── Helpers — matemáticas ────────────────────────────────────────────────

    private static int Modulo(int valor, int total)
        => total <= 0 ? 0 : ((valor % total) + total) % total;

    // ─── Helpers — formateo ───────────────────────────────────────────────────

    private static string FormatearDeltas(int vida, int vel, int man, int carga, string sufijo)
    {
        var partes = new List<string>();
        if (vida  != 0) partes.Add($"{vida:+#;-#;0} vida");
        if (vel   != 0) partes.Add($"{vel:+#;-#;0} velocidad");
        if (man   != 0) partes.Add($"{man:+#;-#;0} maniob.");
        if (carga != 0) partes.Add($"{carga:+#;-#;0} carga");
        string base_ = partes.Count > 0 ? string.Join(", ", partes) : "Sin cambios";
        return string.IsNullOrEmpty(sufijo) ? base_ : $"{base_} {sufijo}";
    }
}
