using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 📖 Álbum de fragmentos de la historia. Grid de 2 columnas con todos los fragmentos del cuento
/// en orden: bloqueado = gris y translúcido; desbloqueado = a color con botón para volver a verlo.
/// Cada celda muestra la misión que lo desbloquea. Los fragmentos que no dispara ninguna misión
/// (el prólogo y sus encadenados) se ven desbloqueados desde el inicio.
///
/// Con todos desbloqueados se activa "cuento completo": reproduce la historia entera, con un
/// botón para salir en cualquier momento. PanelFinJuego también lo ofrece al terminar el cuento.
///
/// Controla la UI por REFERENCIAS (CanvasGroup), no por su propio gameObject, así que puede vivir
/// en el GameController central.
/// </summary>
public class AlbumFragmentosUI : MonoBehaviour, IPanelConVisibilidad
{
    [Header("Datos")]
    [Tooltip("Fragmentos del cuento en el orden de la historia.")]
    [SerializeField] private AlbumCuento album;

    [Header("Panel")]
    [SerializeField] private CanvasGroup panelAlbum;
    [Tooltip("Alterna como pestaña: con las misiones a la vista abre el álbum; con el álbum, vuelve a las misiones.")]
    [SerializeField] private Button botonAbrir;
    [Tooltip("Opcional: texto del botón anterior. Cambia entre 'Álbum' y 'Misiones'. Si lo usas, " +
             "quítale el Localize String Event a ese texto para que no lo pise.")]
    [SerializeField] private TextMeshProUGUI textoBotonAbrir;
    [Tooltip("Opcional: botón aparte para cerrar el álbum (vuelve a las misiones).")]
    [SerializeField] private Button botonCerrar;

    [Header("Pestañas con la lista de misiones")]
    [Tooltip("Lo que se oculta mientras se ve el álbum (p. ej. el Scroll de la lista de misiones y " +
             "los filtros). Cada uno necesita un CanvasGroup.")]
    [SerializeField] private CanvasGroup[] gruposMisiones;
    [Tooltip("Opcional: si la persiana de misiones se cierra con el álbum abierto, el álbum se " +
             "cierra también, para que al volver a abrirla se vean las misiones.")]
    [SerializeField] private MissionListUI listaMisiones;
    [Tooltip("Opcional: muestra cuántos fragmentos lleva desbloqueados.")]
    [SerializeField] private TextMeshProUGUI textoProgreso;

    [Header("Grid")]
    [Tooltip("Padre de las celdas. Si tiene GridLayoutGroup se fuerza a 2 columnas.")]
    [SerializeField] private Transform contenedorGrid;
    [SerializeField] private CeldaAlbumUI prefabCelda;
    [SerializeField] private int columnas = 2;
    [Tooltip("True = la parte de imagen de la celda toma la proporción de las ilustraciones del " +
             "álbum (alto/ancho del sprite más alto). False = usa proporcionAltoAncho.")]
    [SerializeField] private bool proporcionDesdeImagen = true;
    [Tooltip("Alto de la imagen respecto a su ancho si no se toma de las ilustraciones (1.78 = 9:16 vertical).")]
    [SerializeField] private float proporcionAltoAncho = 1.78f;

    [Header("Aspecto bloqueado")]
    [Tooltip("Shader 'Tremblay/UI Desaturado'. Sin él, la imagen solo se tiñe con el color bloqueado.")]
    [SerializeField] private Shader shaderGris;
    [Tooltip("Color (y alpha = translucidez) de la imagen bloqueada.")]
    [SerializeField] private Color colorBloqueado = new Color(1f, 1f, 1f, 0.45f);

    [Header("Cuento completo")]
    [Tooltip("Botón dentro del álbum. Solo es interactuable con todos los fragmentos desbloqueados.")]
    [SerializeField] private Button botonCuentoCompleto;
    [Tooltip("Grupo con el botón de salir. Se muestra solo durante el cuento completo y se pone " +
             "por encima de todo (último hermano), así que debe compartir padre con el panel de historia.")]
    [SerializeField] private CanvasGroup grupoSalirCuento;
    [SerializeField] private Button botonSalirCuento;
    [Tooltip("Pausa entre un fragmento y el siguiente en el cuento completo (s).")]
    [SerializeField] private float pausaEntreFragmentos = 0.6f;

    private readonly List<CeldaAlbumUI> celdas = new List<CeldaAlbumUI>();
    private readonly Dictionary<StoryFragment, Mission> misionDeFragmento = new Dictionary<StoryFragment, Mission>();
    private Material materialGris;
    private GridLayoutGroup grid;
    private float ultimoAnchoGrid = -1f;
    private bool visible;
    private bool reproduciendoCuento;
    private Coroutine reproduccionCR;
    private System.Action alTerminarCuento;
    private StoryManager storyManager;

    /// <summary>True mientras suena el cuento completo.</summary>
    public bool ReproduciendoCuentoCompleto => reproduciendoCuento;

    void Start()
    {
        if (shaderGris == null) shaderGris = Shader.Find("Tremblay/UI Desaturado");
        if (shaderGris != null)
        {
            materialGris = new Material(shaderGris);
            materialGris.SetFloat("_Saturacion", 0f);
        }

        if (contenedorGrid != null)
        {
            grid = contenedorGrid.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columnas;
            }
        }
        AjustarTamanoCeldas();

        Conectar(botonAbrir, Alternar);
        Conectar(botonCerrar, Cerrar);
        Conectar(botonCuentoCompleto, () => ReproducirCuentoCompleto(Abrir));
        Conectar(botonSalirCuento, SalirCuentoCompleto);

        Cerrar();
        MostrarGrupo(grupoSalirCuento, false);

        storyManager = StoryManager.Instance;
        if (storyManager != null)
        {
            storyManager.OnFragmentoVisto += AlVerFragmento;
            storyManager.OnProgresoReseteado += Refrescar;
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMisionesActualizadas += AlActualizarMisiones;

        ConstruirGrid();
    }

    private static void Conectar(Button boton, UnityEngine.Events.UnityAction accion)
    {
        if (boton == null) return;
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(() =>
        {
            if (GlobalAudioManager.Instance != null)
                GlobalAudioManager.Instance.ReproducirSonidoClickBoton();
            accion();
        });
    }

    // ========== Grid ==========

    // El ancho del contenedor cambia con la pantalla (y vale 0 hasta el primer layout), así que se
    // vigila mientras el álbum está abierto y se recalcula la celda solo si cambió.
    void LateUpdate()
    {
        if (!visible) return;

        // La persiana de misiones se cerró con el álbum abierto: volver a la pestaña de misiones.
        if (listaMisiones != null && !listaMisiones.EstaPanelVisible())
        {
            Cerrar();
            return;
        }

        AjustarTamanoCeldas();
    }

    /// <summary>
    /// Celda = ancho del contenedor repartido entre las columnas (descontando padding y spacing
    /// del GridLayoutGroup); alto = ancho × proporción de la imagen. La celda tiene la forma exacta
    /// de la ilustración (la etiqueta va encima de la imagen). El tamaño del prefab da igual.
    /// </summary>
    private void AjustarTamanoCeldas()
    {
        if (grid == null || columnas < 1) return;

        float ancho = ((RectTransform)contenedorGrid).rect.width;
        if (ancho <= 0f || Mathf.Approximately(ancho, ultimoAnchoGrid)) return;
        ultimoAnchoGrid = ancho;

        float disponible = ancho - grid.padding.left - grid.padding.right - grid.spacing.x * (columnas - 1);
        float anchoCelda = Mathf.Floor(disponible / columnas);
        grid.cellSize = new Vector2(anchoCelda, Mathf.Round(anchoCelda * ProporcionImagen()));
    }

    // Alto/ancho de la ilustración más alta del álbum (todas las de Halloween son 9:16).
    private float ProporcionImagen()
    {
        if (!proporcionDesdeImagen || album == null) return proporcionAltoAncho;

        float max = 0f;
        foreach (var f in album.fragmentos)
        {
            if (f == null || f.imagenFondo == null) continue;
            Rect r = f.imagenFondo.rect;
            if (r.width > 0f) max = Mathf.Max(max, r.height / r.width);
        }
        return max > 0f ? max : proporcionAltoAncho;
    }

    private void ConstruirGrid()
    {
        foreach (var c in celdas)
            if (c != null) Destroy(c.gameObject);
        celdas.Clear();

        if (album == null || contenedorGrid == null || prefabCelda == null)
        {
            Debug.LogWarning("[AlbumFragmentosUI] Falta asignar el álbum, el contenedor o el prefab de celda.");
            return;
        }

        MapearMisiones();

        foreach (var fragmento in album.fragmentos)
        {
            if (fragmento == null) continue;
            var celda = Instantiate(prefabCelda, contenedorGrid);
            celda.Configurar(fragmento, EtiquetaDe(fragmento), ReproducirFragmentoSuelto);
            celdas.Add(celda);
        }

        Refrescar();
    }

    // Qué misión dispara cada fragmento (al descifrar o al completar).
    private void MapearMisiones()
    {
        misionDeFragmento.Clear();
        var mm = MissionManager.Instance;
        if (mm == null) return;

        foreach (var m in mm.misiones)
        {
            if (m == null) continue;
            if (m.fragmentoAlDescifrar != null && !misionDeFragmento.ContainsKey(m.fragmentoAlDescifrar))
                misionDeFragmento[m.fragmentoAlDescifrar] = m;
            if (m.fragmentoAlCompletar != null && !misionDeFragmento.ContainsKey(m.fragmentoAlCompletar))
                misionDeFragmento[m.fragmentoAlCompletar] = m;
        }
    }

    private string EtiquetaDe(StoryFragment fragmento)
    {
        if (misionDeFragmento.TryGetValue(fragmento, out var mision))
            return mision.DescripcionMeta;
        return LanguageManager.T("album.prologue");
    }

    private bool EstaDesbloqueado(StoryFragment fragmento)
    {
        // Sin misión que lo dispare (prólogo y encadenados): visible desde el inicio.
        if (!misionDeFragmento.ContainsKey(fragmento)) return true;
        return storyManager != null && storyManager.FragmentoYaVisto(fragmento.fragmentID);
    }

    /// <summary>True si todos los fragmentos del álbum están desbloqueados.</summary>
    public bool TodosDesbloqueados()
    {
        if (album == null || celdas.Count == 0) return false;
        foreach (var c in celdas)
            if (!EstaDesbloqueado(c.Fragmento)) return false;
        return true;
    }

    private void Refrescar()
    {
        if (storyManager == null) storyManager = StoryManager.Instance;

        int desbloqueados = 0;
        foreach (var c in celdas)
        {
            bool ok = EstaDesbloqueado(c.Fragmento);
            if (ok) desbloqueados++;
            c.AplicarEstado(ok, materialGris, colorBloqueado);
        }

        if (botonCuentoCompleto != null)
            botonCuentoCompleto.interactable = desbloqueados == celdas.Count && celdas.Count > 0;

        if (textoProgreso != null)
            textoProgreso.text = LanguageManager.T("album.progress", desbloqueados, celdas.Count);
    }

    private void AlVerFragmento(StoryFragment _) => Refrescar();

    // Las misiones pueden cargarse después del Start del álbum: rehacer el mapa y las etiquetas.
    private void AlActualizarMisiones()
    {
        MapearMisiones();
        ActualizarEtiquetas();
        Refrescar();
    }

    private void ActualizarEtiquetas()
    {
        foreach (var c in celdas)
            if (c.etiqueta != null) c.etiqueta.text = EtiquetaDe(c.Fragmento);
    }

    // ========== Panel ==========

    public void Abrir()
    {
        // Las etiquetas salen en idioma meta: se recalculan por si cambió desde que se construyó.
        MapearMisiones();
        ActualizarEtiquetas();
        Refrescar();
        visible = true;
        MostrarGrupo(panelAlbum, true);
        MostrarMisiones(false);
        AjustarTamanoCeldas();
    }

    /// <summary>Cierra el álbum y vuelve a mostrar la lista de misiones.</summary>
    public void Cerrar()
    {
        visible = false;
        MostrarGrupo(panelAlbum, false);
        MostrarMisiones(true);
    }

    /// <summary>Pestaña: misiones ↔ álbum.</summary>
    public void Alternar()
    {
        if (visible) Cerrar();
        else Abrir();
    }

    private void MostrarMisiones(bool mostrar)
    {
        if (gruposMisiones != null)
            foreach (var g in gruposMisiones) MostrarGrupo(g, mostrar);

        if (textoBotonAbrir != null)
            textoBotonAbrir.text = LanguageManager.T(mostrar ? "album.open" : "album.show_missions");
    }

    public bool EstaPanelVisible() => visible;

    private static void MostrarGrupo(CanvasGroup grupo, bool mostrar)
    {
        if (grupo == null) return;
        grupo.alpha = mostrar ? 1f : 0f;
        grupo.interactable = mostrar;
        grupo.blocksRaycasts = mostrar;
    }

    // ========== Reproducción ==========

    // Vuelve a ver UN fragmento, sin su encadenado (ese tiene su propia celda).
    private void ReproducirFragmentoSuelto(StoryFragment fragmento)
    {
        if (storyManager == null || storyManager.EstaReproduciendo || reproduciendoCuento) return;

        Cerrar();
        storyManager.ReproducirFragmento(fragmento, forzarReproduccion: true, encadenar: false);
        StartCoroutine(ReabrirAlTerminar());
    }

    private IEnumerator ReabrirAlTerminar()
    {
        while (storyManager != null && storyManager.EstaReproduciendo)
            yield return null;
        Abrir();
    }

    /// <summary>
    /// Reproduce el cuento entero en orden. Los fragmentos encadenados no se lanzan: suenan solos
    /// tras su anterior. alTerminar se llama al acabar o al salir (p. ej. volver a abrir el álbum,
    /// o volver a mostrar el panel de fin de juego).
    /// </summary>
    public void ReproducirCuentoCompleto(System.Action alTerminar)
    {
        if (reproduciendoCuento || storyManager == null || album == null) return;
        if (storyManager.EstaReproduciendo) return;

        alTerminarCuento = alTerminar;
        Cerrar();

        reproduciendoCuento = true;
        MostrarGrupo(grupoSalirCuento, true);
        reproduccionCR = StartCoroutine(CuentoCompletoCoroutine());
    }

    private IEnumerator CuentoCompletoCoroutine()
    {
        bool primero = true;
        foreach (var fragmento in album.fragmentos)
        {
            if (fragmento == null || album.EsContinuacion(fragmento)) continue;

            if (!primero) yield return new WaitForSeconds(pausaEntreFragmentos);
            primero = false;

            storyManager.ReproducirFragmento(fragmento, forzarReproduccion: true);

            // El panel de historia se crea/ordena al mostrarse: el botón de salir va siempre encima.
            if (grupoSalirCuento != null) grupoSalirCuento.transform.SetAsLastSibling();

            while (storyManager.EstaReproduciendo)
                yield return null;
        }

        reproduccionCR = null;
        TerminarCuento();
    }

    public void SalirCuentoCompleto()
    {
        if (!reproduciendoCuento) return;

        if (reproduccionCR != null) { StopCoroutine(reproduccionCR); reproduccionCR = null; }
        MostrarGrupo(grupoSalirCuento, false);
        if (storyManager != null) storyManager.CortarNarracionActual();

        StartCoroutine(TerminarAlCortar());
    }

    // Espera a que el panel de historia se cierre antes de devolver el control.
    private IEnumerator TerminarAlCortar()
    {
        while (storyManager != null && storyManager.EstaReproduciendo)
            yield return null;
        TerminarCuento();
    }

    private void TerminarCuento()
    {
        reproduciendoCuento = false;
        MostrarGrupo(grupoSalirCuento, false);

        var callback = alTerminarCuento;
        alTerminarCuento = null;
        callback?.Invoke();
    }

    void OnDestroy()
    {
        if (storyManager != null)
        {
            storyManager.OnFragmentoVisto -= AlVerFragmento;
            storyManager.OnProgresoReseteado -= Refrescar;
        }
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMisionesActualizadas -= AlActualizarMisiones;

        if (materialGris != null) Destroy(materialGris);
    }
}
