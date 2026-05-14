using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ObjectInfoUIManager refactorizado para usar canvas estático en jerarquía
/// 
/// 🎯 FILOSOFÍA DEL NUEVO SISTEMA:
/// - El canvas NUNCA se destruye, solo se oculta/muestra
/// - Al cambiar entre objetos, solo se actualiza el contenido
/// - El canvas es PERSISTENTE y puede manejar múltiples objetos
/// - Optimizado para móviles - sin instanciación/destrucción
/// - Las animaciones se cancelan correctamente para evitar conflictos
/// 
/// 📱 OPTIMIZACIONES MÓVILES:
/// - Sin garbage collection por instanciación
/// - Referencias cacheadas para mejor performance
/// - Transiciones rápidas sin reconstruir UI
/// - Manejo robusto de referencias para evitar MissingReferenceException
/// 
/// ⚠️ IMPORTANTE: El canvas debe existir en la jerarquía y nunca destruirse
/// </summary>
public class ObjectInfoUIManager : MonoBehaviour
{
    public static ObjectInfoUIManager Instance;

    // Eventos expuestos para el tutorial (no modifican comportamiento existente)
    public event Action OnBotonNombrePulsado;
    public event Action OnBotonColorPulsado;
    public event Action OnBotonGuardarPulsado;
    public event Action OnBotonCerrarPulsado;

    [Header("🎯 Canvas Estático (en jerarquía)")]
    [Tooltip("Canvas que ya existe en la jerarquía y se mostrará/ocultará")]
    public Canvas canvasEstatico;
    
    [Tooltip("Panel principal dentro del canvas que contiene toda la UI")]
    public GameObject panelPrincipal;

    [Header("📱 Configuración Móvil")]
    [SerializeField] private bool optimizarParaMovil = true;
    [Tooltip("Tiempo de fade in/out para animaciones suaves")]
    [SerializeField] private float tiempoFade = 0.3f;
    [SerializeField] private bool usarAnimaciones = true;

    [Header("🎵 Audio Controller")]
    [SerializeField] private ObjectInfoAudioController audioController;

    [Header("Feedback Visual de Misiones")]
    public GameObject imagenObjetoColocadoPrefab;
    public Vector2 tamañoSprite = new Vector2(50, 50);
    public Color colorSpriteColocado = Color.white;

    [Header("🔧 DEBUG")]
    [SerializeField] private bool debugAndroid = true;
    [SerializeField] private bool debugAgarrarDetallado = true;

    // Referencias a componentes del canvas estático
    [Header("📋 Referencias UI Canvas Estático")]
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoColor;
    [SerializeField] private Image imagenObjeto;
    [SerializeField] private Button botonGuardar;
    [SerializeField] private Button botonAudioNombre;
    [SerializeField] private Button botonAudioColor;
    [SerializeField] private Button botonAgarrar;
    [SerializeField] private Button botonColocar;
    [SerializeField] private Button botonCompletarMision;
    [SerializeField] private Button botonCerrar;
    [SerializeField] private Transform panelMisiones;

    [Header("🎨 Mission Panel Controller")]
    [SerializeField] private ObjectInfoMissionPanel missionPanel;

    [Header("🎮 Grab Drop Controller")]
    [SerializeField] private ObjectInfoGrabDropController grabDropController;

    // Sistema de control
    private GameObjectManager gameObjectManager;
    private ScrollViewLoader scrollViewLoader;
    private MissionManager missionManager;
    private string objetoActualID;
    private bool canvasActivo = false;
    private CanvasGroup canvasGroup;
    private Coroutine animacionActual;

    // ✅ Propiedades públicas para acceso externo
    public string ObjetoActualID => objetoActualID;
    public bool CanvasActivo => canvasActivo;

    private void Awake()
    {
        // Singleton "last wins": la nueva escena reemplaza a la anterior
        // (antes el patrón first-wins dejaba referencias colgando si este objeto
        // alguna vez fue DDOL o el orden de carga lo favorecía).
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    private void Start()
    {
        InicializarSistema();
    }

    /// <summary>
    /// Limpia recursos al destruir el objeto para prevenir MissingReferenceException
    /// </summary>
    private void OnDestroy()
    {
        // Liberar singleton para que la nueva escena pueda registrar su propia instancia
        if (Instance == this) Instance = null;

        // ✅ Cancelar cualquier animación en curso
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
            animacionActual = null;
        }

        // (El canvas ya no se marca DDOL, así que muere con la escena automáticamente.)

        canvasGroup = null;
        canvasEstatico = null;
        panelPrincipal = null;
    }

    private void InicializarSistema()
    {
        // Buscar managers
        gameObjectManager = FindObjectOfType<GameObjectManager>();
        scrollViewLoader = FindObjectOfType<ScrollViewLoader>();
        missionManager = FindObjectOfType<MissionManager>();

        // Inicializar audio controller
        if (audioController == null)
            audioController = GetComponent<ObjectInfoAudioController>();
        
        if (audioController == null)
        {
            Debug.LogWarning("[ObjectInfoUI] AudioController no encontrado, agregando componente...");
            audioController = gameObject.AddComponent<ObjectInfoAudioController>();
        }

        // Inicializar mission panel
        if (missionPanel == null)
            missionPanel = GetComponent<ObjectInfoMissionPanel>();
        
        if (missionPanel == null)
        {
            Debug.LogWarning("[ObjectInfoUI] MissionPanel no encontrado, agregando componente...");
            missionPanel = gameObject.AddComponent<ObjectInfoMissionPanel>();
        }

        // Inicializar grab drop controller
        if (grabDropController == null)
            grabDropController = GetComponent<ObjectInfoGrabDropController>();
        
        if (grabDropController == null)
        {
            Debug.LogWarning("[ObjectInfoUI] GrabDropController no encontrado, agregando componente...");
            grabDropController = gameObject.AddComponent<ObjectInfoGrabDropController>();
        }

        // Configurar canvas estático
        ConfigurarCanvasEstatico();
        
        // Auto-buscar referencias si no están asignadas
        if (!VerificarReferenciasUI())
        {
            BuscarReferenciasAutomaticamente();
        }

        // Configurar botones
        ConfigurarEventosBotones();

        Debug.Log("🎯 [ObjectInfoUI] Sistema refactorizado inicializado - Canvas estático listo");
    }

    private void ConfigurarCanvasEstatico()
    {
        // Buscar canvas si no está asignado
        if (canvasEstatico == null)
        {
            canvasEstatico = GetComponentInChildren<Canvas>();
            if (canvasEstatico == null)
            {
                Debug.LogError("🎯 [ObjectInfoUI] ❌ No se encontró canvas estático en jerarquía!");
                return;
            }
        }

        // NOTA: el canvas NO se marca como DDOL. Debe vivir y morir con su escena.
        // Marcarlo DDOL dejaba el canvas del tutorial tapando la pantalla al cargar
        // MainScene (pantalla blanca).

        // Buscar panel principal si no está asignado
        if (panelPrincipal == null)
        {
            panelPrincipal = canvasEstatico.transform.Find("PanelPrincipal")?.gameObject;
            if (panelPrincipal == null)
            {
                // Buscar cualquier panel que pueda ser el principal
                foreach (Transform child in canvasEstatico.transform)
                {
                    if (child.name.ToLower().Contains("panel") || 
                        child.name.ToLower().Contains("main") ||
                        child.name.ToLower().Contains("info"))
                    {
                        panelPrincipal = child.gameObject;
                        break;
                    }
                }
            }
        }

        // Configurar CanvasGroup para animaciones
        canvasGroup = canvasEstatico.GetComponent<CanvasGroup>();
        if (canvasGroup == null && usarAnimaciones)
        {
            canvasGroup = canvasEstatico.gameObject.AddComponent<CanvasGroup>();
            Debug.Log("🎯 [ObjectInfoUI] ✅ CanvasGroup agregado automáticamente");
        }

        // ✅ IMPORTANTE: Configurar el canvas para que esté inicialmente oculto pero NUNCA destruido
        OcultarCanvasInicialmente();

        Debug.Log($"🎯 [ObjectInfoUI] ✅ Canvas estático configurado correctamente - Nunca se destruirá");
    }

    /// <summary>
    /// Oculta el canvas inicialmente sin marcarlo como inactivo
    /// </summary>
    private void OcultarCanvasInicialmente()
    {
        if (CanvasEstaticoValido())
        {
            // Mantener el GameObject activo pero oculto visualmente
            canvasEstatico.gameObject.SetActive(false);
            
            if (CanvasGroupValido())
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            
            canvasActivo = false;
            Debug.Log("🎯 [ObjectInfoUI] Canvas estático inicialmente oculto (pero preservado)");
        }
    }

    private bool VerificarReferenciasUI()
    {
        return textoNombre != null && textoColor != null && imagenObjeto != null && 
               botonGuardar != null && botonCerrar != null;
    }

    private void BuscarReferenciasAutomaticamente()
    {
        if (canvasEstatico == null) return;

        Debug.Log("🔍 [ObjectInfoUI] Buscando referencias UI automáticamente...");

        // Buscar textos
        var todosLosTextos = canvasEstatico.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var texto in todosLosTextos)
        {
            string nombre = texto.name.ToLower();
            if (textoNombre == null && (nombre.Contains("nombre") || nombre.Contains("name")))
                textoNombre = texto;
            else if (textoColor == null && (nombre.Contains("color") || nombre.Contains("colour")))
                textoColor = texto;
        }

        // Buscar imagen principal
        var todasLasImagenes = canvasEstatico.GetComponentsInChildren<Image>(true);
        foreach (var img in todasLasImagenes)
        {
            if (imagenObjeto == null && 
                (img.name.ToLower().Contains("objeto") || img.name.ToLower().Contains("image")))
            {
                imagenObjeto = img;
                break;
            }
        }

        // Buscar botones
        var todosLosBotones = canvasEstatico.GetComponentsInChildren<Button>(true);
        foreach (var boton in todosLosBotones)
        {
            string nombre = boton.name.ToLower();
            
            if (botonGuardar == null && (nombre.Contains("guardar") || nombre.Contains("save")))
                botonGuardar = boton;
            else if (botonCerrar == null && (nombre.Contains("cerrar") || nombre.Contains("close")))
                botonCerrar = boton;
            else if (botonAudioNombre == null && nombre.Contains("audionombre"))
                botonAudioNombre = boton;
            else if (botonAudioColor == null && nombre.Contains("audiocolor"))
                botonAudioColor = boton;
            else if (botonAgarrar == null && (nombre.Contains("agarrar") || nombre.Contains("grab")))
                botonAgarrar = boton;
            else if (botonColocar == null && (nombre.Contains("colocar") || nombre.Contains("place")))
                botonColocar = boton;
            else if (botonCompletarMision == null && nombre.Contains("completar"))
                botonCompletarMision = boton;
        }

        // Buscar panel de misiones
        if (panelMisiones == null)
        {
            panelMisiones = BuscarTransformPorNombre(canvasEstatico.transform, "PanelMisiones");
        }

        Debug.Log($"🔍 [ObjectInfoUI] Referencias encontradas - Textos: {(textoNombre != null ? 1 : 0) + (textoColor != null ? 1 : 0)}, Imagen: {imagenObjeto != null}, Botones: {ContarBotonesEncontrados()}");
    }

    private int ContarBotonesEncontrados()
    {
        int contador = 0;
        if (botonGuardar != null) contador++;
        if (botonCerrar != null) contador++;
        if (botonAudioNombre != null) contador++;
        if (botonAudioColor != null) contador++;
        if (botonAgarrar != null) contador++;
        if (botonColocar != null) contador++;
        if (botonCompletarMision != null) contador++;
        return contador;
    }

    private Transform BuscarTransformPorNombre(Transform parent, string nombre)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(nombre, System.StringComparison.OrdinalIgnoreCase))
                return child;
            
            Transform resultado = BuscarTransformPorNombre(child, nombre);
            if (resultado != null)
                return resultado;
        }
        return null;
    }

    private void ConfigurarEventosBotones()
    {
        // Botón cerrar
        if (botonCerrar != null)
        {
            botonCerrar.onClick.RemoveAllListeners();
            botonCerrar.onClick.AddListener(() => { CerrarCanvas(); OnBotonCerrarPulsado?.Invoke(); });
        }

        // Los demás botones se configurarán dinámicamente en MostrarSobreObjeto
    }

    /// <summary>
    /// Método principal refactorizado - ahora solo actualiza el canvas estático
    /// </summary>
    public GameObject MostrarSobreObjeto(GameObject objeto, GameObjectData datos)
    {
        if (canvasEstatico == null || panelPrincipal == null)
        {
            Debug.LogError("🎯 [ObjectInfoUI] ❌ Canvas estático no configurado correctamente");
            return null;
        }

        // ✅ CORREGIDO: Si ya hay un canvas activo, solo cancelar animaciones pero NO ocultarlo
        if (canvasActivo && objetoActualID != datos.id)
        {
            Debug.Log($"🎯 [ObjectInfoUI] Cambiando de {objetoActualID} a {datos.id} - actualizando contenido");
            
            // Solo cancelar animaciones previas, pero mantener el canvas visible
            if (animacionActual != null)
            {
                StopCoroutine(animacionActual);
                animacionActual = null;
            }
            
            // ❌ NO hacer esto: OcultarCanvasSinAnimacion();
            // ❌ NO hacer esto: canvasActivo = false;
        }

        objetoActualID = datos.id;

        // Asegurar que el canvas esté visible
        if (!canvasEstatico.gameObject.activeInHierarchy)
        {
            canvasEstatico.gameObject.SetActive(true);
        }

        // Actualizar contenido del canvas (sin ocultar/mostrar)
        ActualizarContenidoUI(datos);
        
        // Configurar botones dinámicamente
        ConfigurarBotonesDinamicos(datos);
        
        // Configurar panel de misiones
        if (missionPanel != null && panelMisiones != null)
        {
            missionPanel.ConfigurarPanelMisionesVisual(datos.id, datos.nombreEspanol, panelMisiones);
        }

        // Solo animar si el canvas NO estaba activo previamente
        if (!canvasActivo)
        {
            MostrarCanvas();
        }
        else
        {
            // Si ya estaba activo, asegurar que tenga alpha correcto
            if (CanvasGroupValido())
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        // Reproducir audio
        if (audioController != null)
        {
            audioController?.ReproducirAudioNombre(datos);
        }

        canvasActivo = true;
        Debug.Log($"🎯 [ObjectInfoUI] Canvas estático actualizado para: {datos.nombreEspanol}");

        return canvasEstatico.gameObject;
    }

    private void ActualizarContenidoUI(GameObjectData datos)
    {
        // Actualizar textos
        if (textoNombre != null)
            textoNombre.text = datos.nombreEspanol;
        
        if (textoColor != null)
            textoColor.text = datos.colorEspanol;

        // Actualizar imagen
        if (imagenObjeto != null && !string.IsNullOrEmpty(datos.sprite2DPath))
        {
            Sprite sprite = Resources.Load<Sprite>(datos.sprite2DPath);
            if (sprite != null)
            {
                imagenObjeto.sprite = sprite;
                imagenObjeto.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"🎯 [ObjectInfoUI] ⚠️ Sprite no encontrado: {datos.sprite2DPath}");
                imagenObjeto.gameObject.SetActive(false);
            }
        }
    }

    private void ConfigurarBotonesDinamicos(GameObjectData datos)
    {
        // Botón Guardar
        if (botonGuardar != null)
        {
            botonGuardar.onClick.RemoveAllListeners();
            botonGuardar.onClick.AddListener(() => { GuardarObjeto(datos.id, botonGuardar); OnBotonGuardarPulsado?.Invoke(); });
            
            if (datos.guardadoPorJugador)
                ActualizarBotonGuardado(botonGuardar);
            else
                RestaurarBotonGuardar(botonGuardar);
            
            botonGuardar.gameObject.SetActive(true);
        }

        // Botón Audio Nombre
        if (botonAudioNombre != null)
        {
            botonAudioNombre.onClick.RemoveAllListeners();
            botonAudioNombre.onClick.AddListener(() => {
                if (audioController != null)
                    audioController?.ReproducirAudioNombre(datos);
                OnBotonNombrePulsado?.Invoke();
            });
            
            bool tieneAudioNombre = audioController != null && 
                audioController.TieneAudio(datos, ObjectInfoAudioController.TipoAudio.Nombre);
            botonAudioNombre.gameObject.SetActive(tieneAudioNombre);
        }

        // Botón Audio Color
        if (botonAudioColor != null)
        {
            botonAudioColor.onClick.RemoveAllListeners();
            botonAudioColor.onClick.AddListener(() => {
                if (audioController != null)
                    audioController?.ReproducirAudioColor(datos);
                OnBotonColorPulsado?.Invoke();
            });
            
            bool tieneAudioColor = audioController != null && 
                audioController.TieneAudio(datos, ObjectInfoAudioController.TipoAudio.Color);
            botonAudioColor.gameObject.SetActive(tieneAudioColor);
        }

        // Botón Agarrar
        if (botonAgarrar != null)
        {
            botonAgarrar.onClick.RemoveAllListeners();
            botonAgarrar.onClick.AddListener(() => {
                if (grabDropController != null)
                    grabDropController.AgarrarObjeto(datos.id);
            });
            
            bool debeActivarse = DebeActivarBotonAgarrar(datos.id);
            botonAgarrar.gameObject.SetActive(debeActivarse);
        }

        // Botón Colocar
        if (botonColocar != null)
        {
            botonColocar.onClick.RemoveAllListeners();
            botonColocar.onClick.AddListener(() => {
                if (grabDropController != null)
                    grabDropController.ColocarObjetoAgarrado(datos.id);
            });
            
            bool debeActivarse = DebeActivarBotonColocar(datos.id);
            botonColocar.gameObject.SetActive(debeActivarse);
        }

        // Botón Completar Misión
        if (botonCompletarMision != null)
        {
            botonCompletarMision.onClick.RemoveAllListeners();
            botonCompletarMision.onClick.AddListener(() => CompletarMisionEnDestino(datos.id));
            
            bool debeActivarse = DebeActivarBotonCompletarMision(datos.id);
            botonCompletarMision.gameObject.SetActive(debeActivarse);
        }
    }

    #region Métodos de Control de Visibilidad

    private void MostrarCanvas()
    {
        if (!CanvasEstaticoValido()) return;

        // ✅ CRÍTICO: Cancelar cualquier animación en curso antes de mostrar
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
            animacionActual = null;
            Debug.Log("🎯 [ObjectInfoUI] Animación previa cancelada al mostrar canvas");
        }

        canvasEstatico.gameObject.SetActive(true);
        
        if (usarAnimaciones && CanvasGroupValido())
        {
            animacionActual = StartCoroutine(AnimarFade(true));
        }
        else
        {
            if (CanvasGroupValido())
            {
                try
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                catch (MissingReferenceException)
                {
                    Debug.LogWarning("🎯 [ObjectInfoUI] CanvasGroup destruido al mostrar canvas");
                }
            }
        }

        // Audio feedback
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoTogglePanel();
        }
    }

    public void CerrarCanvas()
    {
        if (!canvasActivo) return;

        // ✅ CRÍTICO: Cancelar cualquier animación en curso antes de cerrar
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
            animacionActual = null;
            Debug.Log("🎯 [ObjectInfoUI] Animación cancelada al cerrar canvas");
        }

        if (usarAnimaciones && CanvasGroupValido() && CanvasEstaticoValido())
        {
            animacionActual = StartCoroutine(AnimarFade(false));
        }
        else
        {
            OcultarCanvasSinAnimacion();
        }

        canvasActivo = false;
        objetoActualID = "";

        // Audio feedback
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoTogglePanel();
        }

        Debug.Log("🎯 [ObjectInfoUI] Canvas estático cerrado");
    }

    private void OcultarCanvasSinAnimacion()
    {
        // ✅ Verificación robusta para canvas estático
        if (CanvasEstaticoValido())
        {
            try
            {
                canvasEstatico.gameObject.SetActive(false);
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning("🎯 [ObjectInfoUI] Canvas estático destruido al intentar ocultar");
                canvasEstatico = null;
            }
        }
        
        // ✅ Verificación robusta para CanvasGroup
        if (CanvasGroupValido())
        {
            try
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning("🎯 [ObjectInfoUI] CanvasGroup destruido al intentar ocultar");
                canvasGroup = null;
            }
        }
    }

    private System.Collections.IEnumerator AnimarFade(bool mostrar)
    {
        float alphaInicial = mostrar ? 0f : 1f;
        float alphaFinal = mostrar ? 1f : 0f;
        float tiempo = 0f;

        // ✅ Verificación robusta inicial
        if (!CanvasGroupValido())
        {
            Debug.LogWarning("🎯 [ObjectInfoUI] CanvasGroup no válido, cancelando animación");
            animacionActual = null;
            yield break;
        }

        if (mostrar)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        while (tiempo < tiempoFade)
        {
            // ✅ Verificación robusta en cada frame
            if (!CanvasGroupValido())
            {
                Debug.LogWarning("🎯 [ObjectInfoUI] CanvasGroup destruido durante animación");
                animacionActual = null;
                yield break;
            }

            tiempo += Time.deltaTime;
            float progreso = tiempo / tiempoFade;
            
            // ✅ Try-catch adicional para prevenir errores de Unity
            try
            {
                canvasGroup.alpha = Mathf.Lerp(alphaInicial, alphaFinal, progreso);
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning("🎯 [ObjectInfoUI] CanvasGroup destruido al cambiar alpha");
                animacionActual = null;
                yield break;
            }
            
            yield return null;
        }

        // ✅ Verificación final robusta
        if (CanvasGroupValido())
        {
            try
            {
                canvasGroup.alpha = alphaFinal;

                if (!mostrar)
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    
                    // ✅ Verificación robusta para canvas estático
                    if (CanvasEstaticoValido())
                        canvasEstatico.gameObject.SetActive(false);
                }
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning("🎯 [ObjectInfoUI] Referencias destruidas al finalizar animación");
            }
        }

        animacionActual = null;
    }

    public void OcultarCanvasTemporalmente()
    {
        if (CanvasEstaticoValido() && canvasActivo)
        {
            // Cancelar animaciones en curso
            if (animacionActual != null)
            {
                StopCoroutine(animacionActual);
                animacionActual = null;
            }
            
            canvasEstatico.gameObject.SetActive(false);
            Debug.Log("🎯 [ObjectInfoUI] Canvas estático ocultado temporalmente (ej: inventario abierto)");
        }
    }

    public void MostrarCanvasTemporalmente()
    {
        if (CanvasEstaticoValido() && canvasActivo)
        {
            canvasEstatico.gameObject.SetActive(true);
            
            // Restaurar alpha del CanvasGroup
            if (CanvasGroupValido())
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            Debug.Log("🎯 [ObjectInfoUI] Canvas estático mostrado nuevamente");
        }
    }

    public bool TieneCanvasActivo()
    {
        return canvasActivo;
    }

    #endregion

    #region Métodos Utilitarios

    /// <summary>
    /// Verifica si un objeto Unity es válido y no ha sido destruido
    /// </summary>
    private bool EsObjetoValido(UnityEngine.Object obj)
    {
        return obj != null && !ReferenceEquals(obj, null);
    }

    /// <summary>
    /// Verificación robusta para CanvasGroup
    /// </summary>
    private bool CanvasGroupValido()
    {
        try
        {
            return canvasGroup != null && EsObjetoValido(canvasGroup);
        }
        catch (MissingReferenceException)
        {
            canvasGroup = null;
            return false;
        }
    }

    /// <summary>
    /// Verificación robusta para Canvas Estático
    /// </summary>
    private bool CanvasEstaticoValido()
    {
        try
        {
            return canvasEstatico != null && EsObjetoValido(canvasEstatico);
        }
        catch (MissingReferenceException)
        {
            canvasEstatico = null;
            return false;
        }
    }

    #endregion

    #region Métodos de Lógica de Negocio

    private bool DebeActivarBotonAgarrar(string objetoID)
    {
        if (missionManager == null) return false;
        
        // Buscar si hay misiones descifradas que requieran este objeto
        var misionesDescifradas = missionManager.MisionesDescifradas;
        var misionesCompletadas = missionManager.MisionesCompletadas;
        var misiones = missionManager.misiones;
        
        return misiones.Any(m => 
            misionesDescifradas.Contains(m.misionID) &&
            !misionesCompletadas.Contains(m.misionID) &&
            m.idsObjetosCorrectos != null &&
            m.idsObjetosCorrectos.Contains(objetoID)
        );
    }

    private bool DebeActivarBotonColocar(string objetoDestinoID)
    {
        if (grabDropController == null) return false;
        return grabDropController.TieneObjetoAgarrado();
    }

    private bool DebeActivarBotonCompletarMision(string objetoDestinoID)
    {
        if (missionManager == null) return false;
        return missionManager.TieneMisionesDescifradasPendientes(objetoDestinoID);
    }

    private void GuardarObjeto(string objetoID, Button boton)
    {
        if (gameObjectManager == null) return;
        
        bool exitoso = gameObjectManager.MarcarComoGuardado(objetoID);
        if (exitoso)
        {
            ActualizarBotonGuardado(boton);
            if (scrollViewLoader != null)
                scrollViewLoader.ActualizarItemPorID(objetoID);
            ActualizarEstadoMisiones();
        }
    }

    private void ActualizarBotonGuardado(Button boton)
    {
        TextMeshProUGUI textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>();
        if (textoBoton != null) textoBoton.text = "Got it ✓";
        boton.interactable = false;
    }

    private void RestaurarBotonGuardar(Button boton)
    {
        TextMeshProUGUI textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>();
        if (textoBoton != null) textoBoton.text = "Guardar";
        boton.interactable = true;
    }

    private void CompletarMisionEnDestino(string objetoDestinoID)
    {
        if (missionManager == null) return;
        if (grabDropController == null || grabDropController.ObtenerObjetosColocados(objetoDestinoID).Count == 0) return;

        List<string> objetosColocados = grabDropController.ObtenerObjetosColocados(objetoDestinoID);
        Debug.Log($"Verificando misión en {objetoDestinoID} con objetos: [{string.Join(", ", objetosColocados)}]");

        // Buscar misión que coincida con los objetos colocados
        var misionesDescifradas = missionManager.MisionesDescifradas;
        var misionesCompletadas = missionManager.MisionesCompletadas;
        var misiones = missionManager.misiones;

        Mission misionACompletar = misiones.FirstOrDefault(m =>
            m.idObjetoDestino == objetoDestinoID &&
            misionesDescifradas.Contains(m.misionID) &&
            !misionesCompletadas.Contains(m.misionID) &&
            m.idsObjetosCorrectos != null &&
            m.idsObjetosCorrectos.Count == objetosColocados.Count &&
            m.idsObjetosCorrectos.All(req => objetosColocados.Contains(req))
        );

        if (misionACompletar == null)
        {
            Debug.LogWarning($"No se encontró misión completable para {objetoDestinoID}");
            return;
        }

        // Completar la misión
        Debug.Log($"¡Misión {misionACompletar.misionID} completada exitosamente!");
        missionManager.CompletarMisionAR(misionACompletar.misionID);
        
        // Audio feedback
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoMisionCompletadaAR();
        }

        // Limpiar objetos colocados
        grabDropController.LimpiarObjetosColocadosDestino(objetoDestinoID);

        // Actualizar UI
        if (objetoActualID == objetoDestinoID && panelMisiones != null && missionPanel != null)
        {
            missionPanel.ConfigurarPanelMisionesVisual(objetoActualID, "", panelMisiones);
        }

        // Actualizar botones
        ActualizarBotonesDinamicamente();
    }

    public void ActualizarEstadoMisiones()
    {
        if (canvasActivo && !string.IsNullOrEmpty(objetoActualID))
        {
            // Buscar datos del objeto actual
            var datos = gameObjectManager?.BuscarObjetoPorId(objetoActualID);
            if (datos != null)
            {
                ConfigurarBotonesDinamicos(datos);
            }
        }
    }

    /// <summary>
    /// Actualiza dinámicamente el estado de todos los botones
    /// </summary>
    public void ActualizarBotonesDinamicamente()
    {
        ActualizarEstadoMisiones();
    }

    #endregion

    #region Métodos de Debug

    [ContextMenu("🔍 Auto-buscar Referencias UI")]
    public void AutoBuscarReferenciasUI()
    {
        BuscarReferenciasAutomaticamente();
        Debug.Log("🔍 Búsqueda automática de referencias completada");
    }

    [ContextMenu("🎯 Test Mostrar Canvas")]
    public void TestMostrarCanvas()
    {
        if (gameObjectManager != null && gameObjectManager.listaObjetos.Count > 0)
        {
            var primerObjeto = gameObjectManager.listaObjetos[0];
            if (primerObjeto != null)
            {
                MostrarSobreObjeto(gameObject, primerObjeto);
            }
        }
    }

    [ContextMenu("📱 Test Optimización Móvil")]
    public void TestOptimizacionMovil()
    {
        Debug.Log("📱 Estado de optimización móvil:");
        Debug.Log($"  - Optimizar para móvil: {optimizarParaMovil}");
        Debug.Log($"  - Usar animaciones: {usarAnimaciones}");
        Debug.Log($"  - Tiempo de fade: {tiempoFade}s");
        Debug.Log($"  - Canvas Group presente: {canvasGroup != null}");
    }

    [ContextMenu("🔍 Debug Canvas Persistente")]
    public void DebugCanvasPersistente()
    {
        Debug.Log("=== 🔍 ESTADO CANVAS PERSISTENTE ===");
        Debug.Log($"Canvas estático existe: {canvasEstatico != null}");
        Debug.Log($"Canvas estático válido: {CanvasEstaticoValido()}");
        Debug.Log($"GameObject activo: {canvasEstatico?.gameObject.activeInHierarchy}");
        Debug.Log($"CanvasGroup válido: {CanvasGroupValido()}");
        Debug.Log($"Canvas activo (lógico): {canvasActivo}");
        Debug.Log($"Objeto actual ID: {objetoActualID}");
        Debug.Log($"Animación en curso: {animacionActual != null}");
        
        if (canvasEstatico != null)
        {
            Debug.Log($"Canvas sorting order: {canvasEstatico.sortingOrder}");
            Debug.Log($"Canvas render mode: {canvasEstatico.renderMode}");
        }
        
        if (CanvasGroupValido())
        {
            Debug.Log($"CanvasGroup alpha: {canvasGroup.alpha}");
            Debug.Log($"CanvasGroup interactable: {canvasGroup.interactable}");
        }
    }

    [ContextMenu("🧪 Test Cambio Múltiple Objetos")]
    public void TestCambioMultipleObjetos()
    {
        if (gameObjectManager != null && gameObjectManager.listaObjetos.Count >= 3)
        {
            Debug.Log("🧪 Iniciando test de cambio múltiple de objetos...");
            StartCoroutine(TestCambiosRapidos());
        }
        else
        {
            Debug.LogWarning("🧪 Necesitas al menos 3 objetos para el test");
        }
    }

    private System.Collections.IEnumerator TestCambiosRapidos()
    {
        for (int i = 0; i < 3 && i < gameObjectManager.listaObjetos.Count; i++)
        {
            var datos = gameObjectManager.listaObjetos[i];
            Debug.Log($"🧪 Mostrando objeto {i}: {datos.nombreEspanol}");
            MostrarSobreObjeto(gameObject, datos);
            yield return new WaitForSeconds(0.1f); // Cambio muy rápido
        }
        Debug.Log("🧪 Test completado - verificar que canvas sigue existiendo");
        DebugCanvasPersistente();
    }

    #endregion

    // Mantener compatibilidad con métodos existentes
    public List<string> ObtenerObjetosColocados(string objetoDestinoID)
    {
        if (grabDropController != null)
            return grabDropController.ObtenerObjetosColocados(objetoDestinoID);
        return new List<string>();
    }

    [ContextMenu("Limpiar Objetos Colocados")]
    public void LimpiarObjetosColocados()
    {
        if (grabDropController != null)
            grabDropController.LimpiarObjetosColocados();
    }
}