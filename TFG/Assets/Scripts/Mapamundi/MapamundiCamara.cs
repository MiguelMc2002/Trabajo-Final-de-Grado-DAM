using UnityEngine;

/// <summary>
/// Controla la cámara del mapamundi.
/// - Zoom con la rueda del ratón (orthographicSize entre MinZoom y MaxZoom).
/// - Desplazamiento llevando el ratón a los bordes de la pantalla.
/// - Desplazamiento con WASD.
/// - Drag con clic medio o clic izquierdo sobre el mapa (sin hit en ciudad/flota).
/// Adjuntar a la Main Camera de la escena Mapamundi.
/// </summary>
public class MapamundiCamara : MonoBehaviour
{
    [Header("Zoom")]
    /// <summary>Tamaño ortográfico mínimo de la cámara (máximo acercamiento).</summary>
    public float MinZoom = 3f;

    /// <summary>Tamaño ortográfico máximo de la cámara (máximo alejamiento).</summary>
    public float MaxZoom = 15f;

    /// <summary>Multiplicador de velocidad aplicado al delta de la rueda del ratón al hacer zoom.</summary>
    public float VelocidadZoom = 2f;

    [Header("Desplazamiento por bordes")]
    /// <summary>Anchura en píxeles del margen de pantalla que activa el desplazamiento automático.</summary>
    public float ZonaBorde = 50f;

    /// <summary>Velocidad de desplazamiento (u/s) al llevar el ratón a los bordes de pantalla.</summary>
    public float VelocidadScroll = 5f;

    [Header("Desplazamiento WASD")]
    /// <summary>Velocidad de desplazamiento (u/s) con las teclas WASD.</summary>
    public float VelocidadWASD = 8f;

    [Header("Límites del mapa")]
    /// <summary>Coordenada X mínima a la que puede llegar el centro de la cámara.</summary>
    public float LimiteIzquierdo = -30f;

    /// <summary>Coordenada X máxima a la que puede llegar el centro de la cámara.</summary>
    public float LimiteDerecho   =  10f;

    /// <summary>Coordenada Y mínima a la que puede llegar el centro de la cámara.</summary>
    public float LimiteInferior  = -30f;

    /// <summary>Coordenada Y máxima a la que puede llegar el centro de la cámara.</summary>
    public float LimiteSuperior  =  10f;

    private Camera  _camara;
    private bool    _arrastrando;
    private Vector3 _origenArrastre;

    private void Awake()
    {
        _camara = GetComponent<Camera>();
    }

    private void Update()
    {
        GestionarZoom();
        GestionarArrastre();
        GestionarClickDerecho();
        if (!_arrastrando)
        {
            GestionarWASD();
            GestionarScroll();
        }
        ClampPosicion();
    }

    private void GestionarZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;
        _camara.orthographicSize -= scroll * VelocidadZoom;
        float anchoMapa   = (LimiteDerecho  - LimiteIzquierdo) / 2f;
        float alturaMapa  = (LimiteSuperior - LimiteInferior)  / 2f;
        float maxZoomMapa = Mathf.Min(anchoMapa / _camara.aspect, alturaMapa);
        _camara.orthographicSize = Mathf.Clamp(_camara.orthographicSize, MinZoom, Mathf.Min(MaxZoom, maxZoomMapa));
    }

    private void GestionarArrastre()
    {
        // Iniciar arrastre con clic medio O clic izquierdo
        if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(0))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 posM = _camara.ScreenToWorldPoint(Input.mousePosition);
                posM.z = 0f;
                RaycastHit2D hit = Physics2D.Raycast(posM, Vector2.zero);
                Debug.Log($"[Camara] Raycast en {posM} — hit: {hit.collider?.gameObject.name ?? "NADA"}");
                if (hit.collider != null)
                {
                    FlotaIconoMapamundi icono = hit.collider.GetComponent<FlotaIconoMapamundi>();
                if (icono != null)
                {
                    if (icono._esJugador)
                    {
                        // Click en el icono del jugador: panel de inspección propia flota
                        if (MapamundiController.Instance != null)
                            MapamundiController.Instance.AbrirPanelJugador();
                    }
                    else
                    {
                        FlotaIconoMapamundi.InvocarFlotaClickada(icono.Flota);
                        if (MapamundiController.Instance != null)
                            MapamundiController.Instance.AbrirPanelInspeccion(icono.Flota);
                    }
                }
                return;
                }
            }
            _arrastrando    = true;
            _origenArrastre = _camara.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(0))
            _arrastrando = false;

        if (!_arrastrando) return;

        Vector3 diferencia = _origenArrastre - _camara.ScreenToWorldPoint(Input.mousePosition);
        transform.position += diferencia;
    }

    private void GestionarWASD()
    {
        Vector3 direccion = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    direccion.y =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  direccion.y = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  direccion.x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direccion.x =  1f;
        if (direccion == Vector3.zero) return;
        transform.position += direccion * VelocidadWASD * Time.deltaTime;
    }

    private void GestionarScroll()
    {
        Vector3 direccion = Vector3.zero;
        Vector2 posRaton  = Input.mousePosition;
        if (posRaton.x < ZonaBorde)                 direccion.x = -1f;
        if (posRaton.x > Screen.width  - ZonaBorde) direccion.x =  1f;
        if (posRaton.y < ZonaBorde)                 direccion.y = -1f;
        if (posRaton.y > Screen.height - ZonaBorde) direccion.y =  1f;
        if (direccion == Vector3.zero) return;
        transform.position += direccion * VelocidadScroll * Time.deltaTime;
    }

    /// <summary>
    /// Gestiona el click derecho: en modo pirata sobre un icono PNJ inicia la persecución;
    /// en cualquier otro caso ordena al jugador navegar hacia la casilla clickada.
    /// Usa <see cref="_camara"/> para garantizar que ScreenToWorldPoint sea coherente
    /// con el resto de raycasts de esta clase.
    /// </summary>
    private void GestionarClickDerecho()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        Vector3 posM = _camara.ScreenToWorldPoint(Input.mousePosition);
        posM.z = 0f;

        if (GameManager.Instance?.FlotaJugador?.ModoPirata == true)
        {
            RaycastHit2D hit = Physics2D.Raycast(posM, Vector2.zero);
            if (hit.collider != null)
            {
                FlotaIconoMapamundi icono = hit.collider.GetComponent<FlotaIconoMapamundi>();
                if (icono != null && !icono._esJugador &&
                    icono.Flota != null && icono.Flota.EstadoActual != EstadoFlotaPNJ.EnPuerto)
                {
                    NavegacionJugadorController.Instance?.IniciarPersecucion(icono.Flota);
                    return;
                }
            }
        }

        // Click en casilla vacía o fuera de modo pirata: navegar
        NavegacionJugadorController.Instance?.CancelarPersecucionYNavegar(Input.mousePosition);
    }

    private void ClampPosicion()
    {
        float mitadAltura = _camara.orthographicSize;
        float mitadAncho  = _camara.orthographicSize * _camara.aspect;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, LimiteIzquierdo + mitadAncho,  LimiteDerecho  - mitadAncho);
        pos.y = Mathf.Clamp(pos.y, LimiteInferior  + mitadAltura, LimiteSuperior - mitadAltura);
        transform.position = pos;
    }
}
