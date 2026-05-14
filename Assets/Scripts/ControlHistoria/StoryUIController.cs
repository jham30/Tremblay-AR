using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 🔄 StoryUIController - VERSIÓN INTEGRADA CON CANVAS EXISTENTE
/// Esta versión NO crea su propio Canvas, sino que se integra con el Canvas principal de la escena
/// Esto evita conflictos con otros elementos de UI como inventario y misiones
/// </summary>
public class StoryUIController : MonoBehaviour
{
    [Header("📱 Referencias UI")]
    [SerializeField] private Canvas canvasExistente; // ✅ Usar Canvas existente de la escena
    [SerializeField] private GameObject panelPrincipal;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("🎨 Elementos Visuales")]
    [SerializeField] private Image imagenFondo;
    [SerializeField] private Image imagenFondoSprite;
    [SerializeField] private Image viñeta;
    [SerializeField] private Image imagenAdicional;
    
    [Header("📝 Texto")]
    [SerializeField] private TextMeshProUGUI textoFragmento;
    [SerializeField] private TextMeshProUGUI textoNombreFragmento;
    
    [Header("🎮 Controles")]
    [SerializeField] private Button botonContinuar;
    [SerializeField] private Button botonSaltar;
    [SerializeField] private GameObject indicadorCargando;
    
    [Header("⚙️ Configuración")]
    [SerializeField] private bool crearUIAutomaticamente = true;
    [SerializeField] private bool buscarCanvasAutomaticamente = true;
    
    [Header("🖼️ Configuración de Imágenes")]
    [SerializeField] private bool mostrarImagenFondoCompleta = true;
    [SerializeField] private bool usarTransparenciaAdaptiva = true;
    [SerializeField] private Color colorFondoFallback = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] [Range(0f, 1f)] private float opacidadImagenFondo = 0.8f;
    
    // ✅ NUEVO: Control de sorting para no interferir con otros UI
    [Header("🎭 Control de Capas")]
    [SerializeField] private int sortingOrderStory = 50; // Menor que otros UI críticos
    [SerializeField] private bool respetarOtrosCanvas = true;
    
    private Coroutine animacionActual;
    private Coroutine typewriterActual;
    private bool fragmentoCompleto = false;
    private bool saltado = false;
    private StoryFragment fragmentoActual;

    [Header("📱 Configuración Móvil")]
    [SerializeField] private float factorEscalaMobil = 1.2f;
    [SerializeField] private int tamanoFuenteBaseMobil = 24;
    [SerializeField] private int tamanoFuenteBaseTablet = 28;
    
    [Header("⏱️ Control de Timing")]
    [SerializeField] private bool esperarFinAudio = true;
    [SerializeField] private float tiempoEsperaExtra = 0.5f;
    [SerializeField] private bool usarSincronizacionAudio = true;

    private bool esMobil;
    private AudioSource audioActual;
    
    // ✅ Referencias a otros componentes de UI que NO deben ser afectados
    private InventarioToggleController inventarioToggle;
    private ObjectInfoUIManager objectInfoUI;
    
    void Awake()
    {
        Debug.Log("📖 [StoryUI] 🔄 INICIANDO VERSIÓN INTEGRADA");
        DetectarTipoDispositivo();
        // Las referencias y la UI se configuran en Start() para garantizar que
        // todos los Canvas de la escena ya estén activos e inicializados.
    }

    void Start()
    {
        // Buscar referencias ahora que todos los objetos de la escena están activos
        BuscarReferenciasSistemaExistente();

        if (canvasExistente == null || panelPrincipal == null)
        {
            Debug.Log($"📖 [StoryUI] Referencias faltantes (canvas={canvasExistente != null}, panel={panelPrincipal != null}) — auto-creando...");
            CrearUIEnCanvasExistente();
        }

        ConfigurarUI();

        // Asegurar que el panel empiece oculto
        if (panelPrincipal != null)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            panelPrincipal.SetActive(false);
        }

        Debug.Log("📖 [StoryUI] ✅ Sistema inicializado correctamente");
    }
    
    // ✅ NUEVO: Buscar componentes del sistema existente
    private void BuscarReferenciasSistemaExistente()
    {
        // Buscar Canvas existente — includeInactive:true para no fallar si el Canvas
        // está momentáneamente inactivo al cargar la escena.
        if (buscarCanvasAutomaticamente && canvasExistente == null)
        {
            canvasExistente = FindObjectOfType<Canvas>(true);

            if (canvasExistente != null)
                Debug.Log($"📖 [StoryUI] ✅ Canvas encontrado: {canvasExistente.name}");
            else
                Debug.LogWarning("📖 [StoryUI] ⚠️ No se encontró Canvas en la escena. Asignar 'canvasExistente' en inspector.");
        }
        
        // Buscar referencias a otros sistemas
        inventarioToggle = FindObjectOfType<InventarioToggleController>();
        objectInfoUI = ObjectInfoUIManager.Instance;
        
        Debug.Log($"📖 [StoryUI] Referencias encontradas - Inventario: {inventarioToggle != null} | ObjectInfo: {objectInfoUI != null}");
    }
    
    private void DetectarTipoDispositivo()
    {
        float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float diagonal = Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight) / dpi;
        
        float aspectRatio = screenHeight > 0 ? (float)Screen.width / Screen.height : 1f;
        bool esAspectRatioMovil = aspectRatio < 0.75f || aspectRatio > 1.5f;
        
        esMobil = diagonal < 7.0f || esAspectRatioMovil || Screen.width < 1200;
        
        Debug.Log($"📱 [StoryUI] Dispositivo: {(esMobil ? "MÓVIL" : "TABLET")} | " +
                 $"Diagonal: {diagonal:F1}\" | Resolución: {Screen.width}x{Screen.height}");
    }

    public IEnumerator MostrarFragmento(StoryFragment fragmento)
    {
        if (fragmento == null || !fragmento.EsValido())
        {
            Debug.LogWarning("📖 [StoryUI] ⚠️ Fragmento inválido");
            yield break;
        }
        
        fragmentoActual = fragmento;
        fragmentoCompleto = false;
        saltado = false;
        audioActual = null;
        
        Debug.Log($"📖 [StoryUI] 🎬 MOSTRANDO FRAGMENTO: {fragmento.fragmentID}");
        
        // ✅ GESTIÓN INTELIGENTE DE OTROS UI
        bool inventarioEstabAbierto = false;
        bool objectInfoEstabAbierto = false;
        
        if (respetarOtrosCanvas)
        {
            // Verificar y cerrar temporalmente otros UI si están abiertos
            if (inventarioToggle != null && inventarioToggle.EstaPanelVisible())
            {
                inventarioEstabAbierto = true;
                inventarioToggle.OcultarPanel();
                Debug.Log("📖 [StoryUI] Inventario temporalmente cerrado para mostrar historia");
            }
            
            if (objectInfoUI != null && objectInfoUI.TieneCanvasActivo())
            {
                objectInfoEstabAbierto = true;
                objectInfoUI.OcultarCanvasTemporalmente();
                Debug.Log("📖 [StoryUI] ObjectInfo temporalmente oculto para mostrar historia");
            }
        }
        
        // Configurar y mostrar fragmento
        ConfigurarParaFragmento(fragmento);
        
        yield return StartCoroutine(FadeIn(fragmento.tiempoFadeIn));
        
        if (fragmento.audioNarracion != null)
        {
            ReproducirAudio(fragmento);
        }
        
        if (!string.IsNullOrEmpty(fragmento.textoFragmento))
        {
            if (fragmento.usarTypewriter)
            {
                typewriterActual = StartCoroutine(MostrarTextoTypewriter(
                    fragmento.textoFragmento,
                    fragmento.velocidadTypewriter
                ));
                yield return typewriterActual;
                typewriterActual = null;
            }
            else
            {
                textoFragmento.text = fragmento.textoFragmento;
            }
        }
        
        fragmentoCompleto = true;
        
        if (fragmento.avanceAutomatico)
        {
            yield return StartCoroutine(EsperarFinalizacionFragmento(fragmento));
        }
        else
        {
            botonContinuar.gameObject.SetActive(true);
            yield return StartCoroutine(EsperarBotonContinuar());
        }
        
        if (!saltado)
        {
            yield return StartCoroutine(FadeOut(fragmento.tiempoFadeOut));
        }
        
        OcultarUI();
        
        // ✅ RESTAURAR OTROS UI SI ESTABAN ABIERTOS
        if (respetarOtrosCanvas)
        {
            if (inventarioEstabAbierto && inventarioToggle != null)
            {
                yield return new WaitForSeconds(0.2f); // Pequeña pausa
                inventarioToggle.MostrarPanel();
                Debug.Log("📖 [StoryUI] Inventario restaurado después de la historia");
            }
            
            if (objectInfoEstabAbierto && objectInfoUI != null)
            {
                objectInfoUI.MostrarCanvasTemporalmente();
                Debug.Log("📖 [StoryUI] ObjectInfo restaurado después de la historia");
            }
        }
        
        fragmentoActual = null;
    }
    
    private IEnumerator EsperarFinalizacionFragmento(StoryFragment fragmento)
    {
        if (fragmento.audioNarracion != null && esperarFinAudio && usarSincronizacionAudio)
        {
            float elapsed = 0f;
            float duracion = fragmento.audioNarracion.length;
            while (elapsed < duracion && !saltado)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!saltado && tiempoEsperaExtra > 0f)
                yield return new WaitForSeconds(tiempoEsperaExtra);
        }
        else
        {
            float tiempoEspera = Mathf.Max(0f, fragmento.ObtenerDuracionTotal() - fragmento.tiempoFadeIn);
            float elapsed = 0f;
            while (elapsed < tiempoEspera && !saltado)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    private void ConfigurarParaFragmento(StoryFragment fragmento)
    {
        Debug.Log($"📖 [StoryUI] ⚙️ Configurando fragmento: {fragmento.fragmentID}");
        
        if (imagenFondo != null)
        {
            imagenFondo.color = fragmento.colorFondo;
        }
        
        if (fragmento.imagenFondo != null)
        {
            ConfigurarImagenFondoFragmento(fragmento);
        }
        else
        {
            OcultarImagenesFondo();
        }
        
        if (textoFragmento != null)
        {
            textoFragmento.color = fragmento.colorTexto;
            int tamañoFinal = CalcularTamañoFuente(fragmento.tamañoFuente);
            textoFragmento.fontSize = tamañoFinal;
            textoFragmento.text = "";
        }
        
        if (textoNombreFragmento != null && !string.IsNullOrEmpty(fragmento.nombreFragmento))
        {
            textoNombreFragmento.text = fragmento.nombreFragmento;
            textoNombreFragmento.gameObject.SetActive(true);
        }
        else if (textoNombreFragmento != null)
        {
            textoNombreFragmento.gameObject.SetActive(false);
        }
        
        if (viñeta != null)
        {
            Color colorViñeta = new Color(0f, 0f, 0f, fragmento.intensidadViñeta);
            viñeta.color = colorViñeta;
        }
        
        Debug.Log($"📖 [StoryUI] ✅ Fragmento configurado");
    }
    
    private void ConfigurarImagenFondoFragmento(StoryFragment fragmento)
    {
        Image targetImage = imagenFondoSprite != null ? imagenFondoSprite : imagenAdicional;
        
        if (targetImage != null)
        {
            targetImage.sprite = fragmento.imagenFondo;
            
            if (mostrarImagenFondoCompleta)
            {
                RectTransform rectTransform = targetImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
                
                targetImage.type = Image.Type.Simple;
                targetImage.preserveAspect = esMobil;
            }
            
            Color colorImagen = Color.white;
            if (usarTransparenciaAdaptiva)
            {
                colorImagen.a = esMobil ? opacidadImagenFondo * 0.8f : opacidadImagenFondo;
            }
            else
            {
                colorImagen.a = opacidadImagenFondo;
            }
            
            targetImage.color = colorImagen;
            targetImage.gameObject.SetActive(true);
            
            Debug.Log($"🖼️ [StoryUI] ✅ Imagen de fondo configurada: {fragmento.imagenFondo.name}");
        }
        else
        {
            Debug.LogWarning("📖 [StoryUI] ⚠️ No se encontró componente Image para imagen de fondo");
        }
    }
    
    private void OcultarImagenesFondo()
    {
        if (imagenFondoSprite != null)
        {
            imagenFondoSprite.gameObject.SetActive(false);
        }
        
        if (imagenAdicional != null)
        {
            imagenAdicional.gameObject.SetActive(false);
        }
    }
    
    private int CalcularTamañoFuente(int tamañoBase)
    {
        return esMobil ? Mathf.Max(tamañoBase, tamanoFuenteBaseMobil) 
                       : Mathf.Max(tamañoBase, tamanoFuenteBaseTablet);
    }
    
    private void ReproducirAudio(StoryFragment fragmento)
    {
        if (fragmento.audioNarracion == null) return;
        
        GameObject audioObj = new GameObject("AudioFragmento");
        audioObj.transform.SetParent(transform);
        
        audioActual = audioObj.AddComponent<AudioSource>();
        audioActual.clip = fragmento.audioNarracion;
        audioActual.volume = fragmento.volumenAudio;
        audioActual.Play();
        
        Debug.Log($"🔊 [StoryUI] Reproduciendo audio: {fragmento.audioNarracion.name}");
    }
    
    private IEnumerator MostrarTextoTypewriter(string texto, float velocidad)
    {
        if (textoFragmento == null) yield break;
        
        textoFragmento.text = "";
        float tiempoPorCaracter = 1f / velocidad;
        
        for (int i = 0; i <= texto.Length; i++)
        {
            if (saltado) break;
            
            textoFragmento.text = texto.Substring(0, i);
            yield return new WaitForSeconds(tiempoPorCaracter);
        }
        
        textoFragmento.text = texto;
    }
    
    private IEnumerator FadeIn(float duracion)
    {
        if (canvasGroup == null) 
        {
            Debug.LogWarning("📖 [StoryUI] ⚠️ CanvasGroup es null en FadeIn");
            yield break;
        }
        
        Debug.Log($"📖 [StoryUI] 🎭 Iniciando FadeIn ({duracion}s)");
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        
        if (panelPrincipal != null)
        {
            panelPrincipal.SetActive(true);
        }
        
        float tiempo = 0f;
        while (tiempo < duracion && duracion > 0)
        {
            tiempo += Time.deltaTime;
            canvasGroup.alpha = tiempo / duracion;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        Debug.Log("📖 [StoryUI] ✅ FadeIn completado");
    }
    
    private IEnumerator FadeOut(float duracion)
    {
        if (canvasGroup == null) yield break;
        
        Debug.Log($"📖 [StoryUI] 🎭 Iniciando FadeOut ({duracion}s)");
        
        float tiempo = 0f;
        float alphaInicial = canvasGroup.alpha;
        
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            canvasGroup.alpha = alphaInicial * (1f - tiempo / duracion);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        Debug.Log("📖 [StoryUI] ✅ FadeOut completado");
    }
    
    private IEnumerator EsperarBotonContinuar()
    {
        bool continuarPresionado = false;
        
        botonContinuar.onClick.RemoveAllListeners();
        botonContinuar.onClick.AddListener(() => continuarPresionado = true);
        
        while (!continuarPresionado && !saltado)
        {
            yield return null;
        }
        
        botonContinuar.gameObject.SetActive(false);
    }
    
    public void SaltarFragmento()
    {
        saltado = true;
        fragmentoCompleto = true;

        // NO llamar StopCoroutine(typewriterActual): si se cancela mid-typewriter,
        // detener la coroutine hija deja a MostrarFragmento colgada en yield return.
        // El typewriter checa `saltado` internamente y termina solo en el siguiente frame.

        if (audioActual != null)
            audioActual.Stop();

        if (fragmentoActual != null && textoFragmento != null)
            textoFragmento.text = fragmentoActual.textoFragmento;

        Debug.Log("📖 [StoryUI] ⏭️ Fragmento saltado");
    }
    
    // ✅ MÉTODO SUAVE PARA OCULTAR (NO AFECTA OTROS UI)
    public void OcultarUI()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        if (audioActual != null)
        {
            Destroy(audioActual.gameObject);
            audioActual = null;
        }
        
        Debug.Log("📖 [StoryUI] UI ocultado (alpha = 0, otros UI no afectados)");
    }
    
    private void ConfigurarUI()
    {
        if (botonSaltar != null)
        {
            botonSaltar.onClick.RemoveAllListeners();
            botonSaltar.onClick.AddListener(SaltarFragmento);
        }
        
        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(false);
        }
        
        Debug.Log("📖 [StoryUI] ✅ UI configurado");
    }
    
    // ✅ CREAR UI EN CANVAS EXISTENTE (NO CREAR NUEVO CANVAS)
    private void CrearUIEnCanvasExistente()
    {
        if (canvasExistente == null)
        {
            Debug.LogError("📖 [StoryUI] ❌ No hay Canvas existente para crear UI");
            return;
        }
        
        Debug.Log($"📖 [StoryUI] 🏗️ Creando UI en Canvas existente: {canvasExistente.name}");
        
        int tamanoFuenteBase = esMobil ? tamanoFuenteBaseMobil : tamanoFuenteBaseTablet;
        
        // ✅ Panel principal - HIJO del Canvas existente
        GameObject panelObj = new GameObject("StoryPanel");
        panelObj.transform.SetParent(canvasExistente.transform, false);
        
        // ✅ CONFIGURAR SORTING ORDER RELATIVO
        Canvas panelCanvas = panelObj.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = sortingOrderStory; // Menor que otros UI críticos
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        panelPrincipal = panelObj;
        canvasGroup = panelObj.AddComponent<CanvasGroup>();

        // Crear elementos básicos
        CrearImagenFondoBase(panelObj);
        CrearImagenFondoSprite(panelObj);
        CrearTextoBasico(panelObj, tamanoFuenteBase);
        CrearBotonesBasicos(panelObj);

        // Ocultar inmediatamente — solo se muestra cuando MostrarFragmento() es invocado
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelPrincipal.SetActive(false);

        Debug.Log("📖 [StoryUI] ✅ UI creado en Canvas existente exitosamente (oculto hasta primer fragmento)");
    }
    
    private void CrearImagenFondoBase(GameObject parent)
    {
        GameObject fondoObj = new GameObject("ImagenFondoBase");
        fondoObj.transform.SetParent(parent.transform, false);
        
        RectTransform fondoRect = fondoObj.AddComponent<RectTransform>();
        fondoRect.anchorMin = Vector2.zero;
        fondoRect.anchorMax = Vector2.one;
        fondoRect.sizeDelta = Vector2.zero;
        
        imagenFondo = fondoObj.AddComponent<Image>();
        imagenFondo.color = colorFondoFallback;
    }
    
    private void CrearImagenFondoSprite(GameObject parent)
    {
        GameObject imagenSpriteObj = new GameObject("ImagenFondoSprite");
        imagenSpriteObj.transform.SetParent(parent.transform, false);
        
        RectTransform spriteRect = imagenSpriteObj.AddComponent<RectTransform>();
        spriteRect.anchorMin = Vector2.zero;
        spriteRect.anchorMax = Vector2.one;
        spriteRect.sizeDelta = Vector2.zero;
        
        imagenFondoSprite = imagenSpriteObj.AddComponent<Image>();
        imagenFondoSprite.type = Image.Type.Simple;
        imagenFondoSprite.gameObject.SetActive(false);
        
        // También crear imagenAdicional para compatibilidad
        GameObject adicionalObj = new GameObject("ImagenAdicional");
        adicionalObj.transform.SetParent(parent.transform, false);
        
        RectTransform adicionalRect = adicionalObj.AddComponent<RectTransform>();
        adicionalRect.anchorMin = Vector2.zero;
        adicionalRect.anchorMax = Vector2.one;
        adicionalRect.sizeDelta = Vector2.zero;
        
        imagenAdicional = adicionalObj.AddComponent<Image>();
        imagenAdicional.gameObject.SetActive(false);
    }
    
    private void CrearTextoBasico(GameObject parent, int tamanoFuenteBase)
    {
        GameObject textoObj = new GameObject("TextoFragmento");
        textoObj.transform.SetParent(parent.transform, false);
        
        RectTransform textoRect = textoObj.AddComponent<RectTransform>();
        if (esMobil)
        {
            textoRect.anchorMin = new Vector2(0.05f, 0.25f);
            textoRect.anchorMax = new Vector2(0.95f, 0.75f);
        }
        else
        {
            textoRect.anchorMin = new Vector2(0.1f, 0.2f);
            textoRect.anchorMax = new Vector2(0.9f, 0.8f);
        }
        textoRect.sizeDelta = Vector2.zero;
        
        textoFragmento = textoObj.AddComponent<TextMeshProUGUI>();
        textoFragmento.fontSize = tamanoFuenteBase;
        textoFragmento.color = Color.white;
        textoFragmento.alignment = TextAlignmentOptions.Center;
        textoFragmento.enableWordWrapping = true;
        textoFragmento.lineSpacing = esMobil ? 1.2f : 1.0f;
    }
    
    private void CrearBotonesBasicos(GameObject parent)
    {
        // Botón Continuar
        GameObject botonObj = new GameObject("BotonContinuar");
        botonObj.transform.SetParent(parent.transform, false);
        
        RectTransform botonRect = botonObj.AddComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(0.5f, 0.1f);
        botonRect.anchorMax = new Vector2(0.5f, 0.1f);
        botonRect.sizeDelta = esMobil ? new Vector2(280, 70) : new Vector2(200, 50);
        botonRect.anchoredPosition = Vector2.zero;
        
        Image botonImg = botonObj.AddComponent<Image>();
        botonImg.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        
        botonContinuar = botonObj.AddComponent<Button>();
        botonContinuar.gameObject.SetActive(false);
        
        // Botón Saltar
        GameObject saltarObj = new GameObject("BotonSaltar");
        saltarObj.transform.SetParent(parent.transform, false);
        
        RectTransform saltarRect = saltarObj.AddComponent<RectTransform>();
        saltarRect.anchorMin = new Vector2(1f, 1f);
        saltarRect.anchorMax = new Vector2(1f, 1f);
        saltarRect.sizeDelta = esMobil ? new Vector2(140, 60) : new Vector2(100, 40);
        saltarRect.anchoredPosition = esMobil ? new Vector2(-80, -40) : new Vector2(-60, -30);
        
        Image saltarImg = saltarObj.AddComponent<Image>();
        saltarImg.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        
        botonSaltar = saltarObj.AddComponent<Button>();
    }
    
    [ContextMenu("🧪 Test UI Básico")]
    public void TestUIBasico()
    {
        StartCoroutine(TestUICoroutine());
    }
    
    private IEnumerator TestUICoroutine()
    {
        StoryFragment fragmentoPrueba = ScriptableObject.CreateInstance<StoryFragment>();
        fragmentoPrueba.fragmentID = "test_integrado";
        fragmentoPrueba.nombreFragmento = "Prueba Versión Integrada";
        fragmentoPrueba.textoFragmento = "✅ PRUEBA INTEGRADA: El Canvas no interfiere con inventario ni misiones.";
        fragmentoPrueba.usarTypewriter = false;
        fragmentoPrueba.avanceAutomatico = false;
        
        yield return StartCoroutine(MostrarFragmento(fragmentoPrueba));
        
        Debug.Log("📖 [StoryUI] ✅ Test integrado completado");
    }
    
    [ContextMenu("📖 Debug Estado Completo")]
    public void DebugEstadoCompleto()
    {
        Debug.Log("=== 📖 ESTADO STORY UI INTEGRADO ===");
        Debug.Log($"Canvas Existente: {canvasExistente?.name ?? "NULL"}");
        Debug.Log($"Panel Principal: {panelPrincipal != null} | Activo: {(panelPrincipal != null ? panelPrincipal.activeInHierarchy : false)}");
        Debug.Log($"CanvasGroup: {canvasGroup != null} | Alpha: {(canvasGroup != null ? canvasGroup.alpha.ToString("F2") : "N/A")}");
        Debug.Log($"Sorting Order: {sortingOrderStory}");
        Debug.Log($"Respetar Otros Canvas: {respetarOtrosCanvas}");
        Debug.Log($"Inventario Toggle: {inventarioToggle != null}");
        Debug.Log($"ObjectInfo UI: {objectInfoUI != null}");
        Debug.Log($"Dispositivo: {(esMobil ? "MÓVIL" : "TABLET")}");
        Debug.Log($"Fragmento Actual: {fragmentoActual?.fragmentID ?? "ninguno"}");
        
        // Estado de otros sistemas
        if (inventarioToggle != null)
        {
            Debug.Log($"Inventario Panel Visible: {inventarioToggle.EstaPanelVisible()}");
        }
        
        if (objectInfoUI != null)
        {
            Debug.Log($"ObjectInfo Canvas Activo: {objectInfoUI.TieneCanvasActivo()}");
        }
        
        Debug.Log("========================================");
    }
    
    void OnDestroy()
    {
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }
        
        if (typewriterActual != null)
        {
            StopCoroutine(typewriterActual);
        }
        
        if (audioActual != null && audioActual.gameObject != null)
        {
            Destroy(audioActual.gameObject);
        }
    }
}