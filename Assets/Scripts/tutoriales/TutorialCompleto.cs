using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Sistema completo de Tutorial - VERSIÓN UNIFICADA
/// Incluye Manager + UI + Audio en un solo script
/// Sin errores de compilación
/// </summary>
public class TutorialCompleto : MonoBehaviour
{
    [Header("📋 Configuración Tutorial")]
    [SerializeField] private bool iniciarAutomaticamente = false;
    [SerializeField] private bool modoDebug = true;
    
    [Header("🎯 Pasos del Tutorial (25 pasos)")]
    [SerializeField] private PasoTutorialSimple[] pasosTutorial = new PasoTutorialSimple[25];
    
    [Header("🔧 Referencias del Sistema")]
    [SerializeField] private TutorialHighlighter highlighter;
    
    [Header("🎨 UI del Tutorial")]
    [SerializeField] private GameObject panelTutorial;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private TextMeshProUGUI textoProgreso;
    [SerializeField] private Button botonSiguiente;
    [SerializeField] private Button botonSalir;
    [SerializeField] private Button botonAudio;
    [SerializeField] private Slider sliderProgreso;
    
    [Header("🎵 Sistema de Audio")]
    [SerializeField] private AudioSource audioSourceTutorial;
    [SerializeField] private bool usarGlobalAudioManager = true;
    
    [Header("📊 Estado Actual")]
    [SerializeField] private int pasoActual = 0;
    [SerializeField] private bool tutorialActivo = false;
    
    // Variables internas
    private CanvasGroup canvasGroup;
    private AudioClip audioActual;
    private bool audioReproduciendose = false;
    
    void Start()
    {
        // Verificar highlighter
        if (highlighter == null)
        {
            highlighter = FindObjectOfType<TutorialHighlighter>();
        }
        
        // Configurar UI
        ConfigurarUI();
        
        // Inicializar pasos
        InicializarPasos();
        
        // Ocultar UI inicialmente
        if (panelTutorial != null)
            panelTutorial.SetActive(false);
        
        if (iniciarAutomaticamente)
        {
            IniciarTutorial();
        }
        
        if (modoDebug)
            Debug.Log("[TutorialCompleto] ✅ Sistema inicializado");
    }
    
    /// <summary>
    /// Configurar UI y eventos
    /// </summary>
    private void ConfigurarUI()
    {
        if (panelTutorial != null)
        {
            canvasGroup = panelTutorial.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panelTutorial.AddComponent<CanvasGroup>();
        }
        
        if (botonSiguiente != null)
            botonSiguiente.onClick.AddListener(AvanzarPaso);
        
        if (botonSalir != null)
            botonSalir.onClick.AddListener(DetenerTutorial);
        
        if (botonAudio != null)
            botonAudio.onClick.AddListener(ReproducirAudio);
        
        if (audioSourceTutorial == null && !usarGlobalAudioManager)
        {
            audioSourceTutorial = gameObject.AddComponent<AudioSource>();
            audioSourceTutorial.playOnAwake = false;
        }
    }
    
    /// <summary>
    /// Inicializar pasos con valores por defecto
    /// </summary>
    private void InicializarPasos()
    {
        if (pasosTutorial == null || pasosTutorial.Length != 25)
        {
            pasosTutorial = new PasoTutorialSimple[25];
        }
        
        string[] textosDefault = {
            "Hola bienvenido a nuestra historia interactiva con realidad aumentada...",
            "Antes que nada debes haber impreso las imágenes objetivo...",
            "Ahora se ha activado la cámara de tu teléfono...",
            "Ahora toca el objeto",
            "Muy bien ves que al tocarlo la apariencia del juego cambia?",
            "Aquí puedes escuchar de nuevo la pronunciación",
            "Acá guardas el objeto en el inventario",
            "Y aquí cierras para ver otros objetos",
            "Ahora escucha de nuevo la pronunciación",
            "Muy bien y cierra para mostrarte lo más divertido",
            "Ves la calabaza abajo? Toca su frente para que se abra",
            "¡Super! Ves que hay varios objetos pero solo el que guardaste se ve claro?",
            "Aquí tenemos la misión... arrastra el objeto aquí",
            "Toca el botón descifrar si es el correcto",
            "Ves que aquí hay varias misiones de diferentes colores?",
            "Tocando de nuevo sobre la frente de la calabaza la cierras",
            "Muy bien",
            "Ves que ahora se activó un icono abajo a la derecha?",
            "Muy bien ahora ve al otro imagen objetivo",
            "Ahora hay un área nueva donde debes poner el objeto",
            "Solo clica en el botón de abajo que ahora es para soltar",
            "Muy bien",
            "Y clica en el botón completar",
            "Como la misión era pon la vela en la calabaza...",
            "Cierra este panel"
        };
        
        for (int i = 0; i < pasosTutorial.Length; i++)
        {
            if (pasosTutorial[i] == null)
            {
                pasosTutorial[i] = new PasoTutorialSimple
                {
                    numeroPaso = i + 1,
                    textoInstruccion = textosDefault[i],
                    tipoResaltado = TipoResaltado.Flecha,
                    direccionFlecha = DireccionFlecha.Izquierda
                };
            }
        }
    }
    
    // =======================================
    // 🎮 MÉTODOS PRINCIPALES
    // =======================================
    
    /// <summary>
    /// Iniciar tutorial
    /// </summary>
    public void IniciarTutorial()
    {
        tutorialActivo = true;
        pasoActual = 0;
        
        if (panelTutorial != null)
            panelTutorial.SetActive(true);
        
        MostrarPasoActual();
        
        if (modoDebug)
            Debug.Log("[TutorialCompleto] 🚀 Tutorial iniciado");
    }
    
    /// <summary>
    /// Avanzar al siguiente paso
    /// </summary>
    public void AvanzarPaso()
    {
        if (!tutorialActivo) return;
        
        pasoActual++;
        
        if (pasoActual >= pasosTutorial.Length)
        {
            CompletarTutorial();
            return;
        }
        
        MostrarPasoActual();
        
        if (modoDebug)
            Debug.Log($"[TutorialCompleto] ➡️ Avanzado a paso {pasoActual + 1}");
    }
    
    /// <summary>
    /// Mostrar paso actual
    /// </summary>
    public void MostrarPasoActual()
    {
        if (!tutorialActivo || pasoActual >= pasosTutorial.Length)
            return;
        
        PasoTutorialSimple paso = pasosTutorial[pasoActual];
        if (paso == null) return;
        
        // Actualizar UI
        ActualizarUI(paso);
        
        // Mostrar resaltado
        MostrarResaltado(paso);
        
        // Configurar audio
        ConfigurarAudio(paso);
        
        if (modoDebug)
            Debug.Log($"[TutorialCompleto] 📝 Paso {pasoActual + 1}: {paso.textoInstruccion}");
    }
    
    /// <summary>
    /// Actualizar UI con información del paso
    /// </summary>
    private void ActualizarUI(PasoTutorialSimple paso)
    {
        if (textoInstruccion != null)
            textoInstruccion.text = paso.textoInstruccion;
        
        if (textoProgreso != null)
            textoProgreso.text = $"Paso {pasoActual + 1}/25";
        
        if (sliderProgreso != null)
            sliderProgreso.value = (float)(pasoActual + 1) / pasosTutorial.Length;
    }
    
    /// <summary>
    /// Mostrar resaltado del elemento
    /// </summary>
    private void MostrarResaltado(PasoTutorialSimple paso)
    {
        if (highlighter == null || paso.elementoAResaltar == null) return;
        
        highlighter.ConfigurarElemento(paso.elementoAResaltar);
        highlighter.CambiarTipoResaltado(paso.tipoResaltado);
        
        if (paso.tipoResaltado == TipoResaltado.Flecha)
        {
            highlighter.CambiarDireccionFlecha(paso.direccionFlecha);
        }
        
        highlighter.MostrarResaltado();
    }
    
    /// <summary>
    /// Configurar audio del paso
    /// </summary>
   private void ConfigurarAudio(PasoTutorialSimple paso)
{
    audioActual = paso.audioDelPaso;
    
    if (botonAudio != null)
        botonAudio.interactable = (audioActual != null);
    
    // 🎵 NUEVO: Reproducir automáticamente
    if (audioActual != null)
    {
        ReproducirAudio();
    }
}
    
    /// <summary>
    /// Reproducir audio del paso actual
    /// </summary>
    public void ReproducirAudio()
    {
        if (audioActual == null) return;
        
        if (usarGlobalAudioManager && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoSFX(audioActual);
        }
        else if (audioSourceTutorial != null)
        {
            audioSourceTutorial.clip = audioActual;
            audioSourceTutorial.Play();
        }
        
        if (modoDebug)
            Debug.Log("[TutorialCompleto] 🔊 Audio reproducido");
    }
    
    /// <summary>
    /// Completar tutorial
    /// </summary>
    private void CompletarTutorial()
    {
        tutorialActivo = false;
        
        if (highlighter != null)
            highlighter.OcultarResaltado();
        
        if (panelTutorial != null)
            panelTutorial.SetActive(false);
        
        if (modoDebug)
            Debug.Log("[TutorialCompleto] 🎉 Tutorial completado!");
    }
    
    /// <summary>
    /// Detener tutorial
    /// </summary>
    public void DetenerTutorial()
    {
        tutorialActivo = false;
        pasoActual = 0;
        
        if (highlighter != null)
            highlighter.OcultarResaltado();
        
        if (panelTutorial != null)
            panelTutorial.SetActive(false);
        
        if (modoDebug)
            Debug.Log("[TutorialCompleto] 🛑 Tutorial detenido");
    }
    
    /// <summary>
    /// Ir a paso específico
    /// </summary>
    public void IrAPaso(int numeroPaso)
    {
        if (numeroPaso < 1 || numeroPaso > 25) return;
        
        pasoActual = numeroPaso - 1;
        tutorialActivo = true;
        
        if (panelTutorial != null && !panelTutorial.activeInHierarchy)
            panelTutorial.SetActive(true);
        
        MostrarPasoActual();
    }
    
    // =======================================
    // 📊 MÉTODOS DE INFORMACIÓN
    // =======================================
    
    public bool EsTutorialActivo() => tutorialActivo;
    public int ObtenerPasoActual() => pasoActual + 1;
    public float ObtenerProgreso() => (float)(pasoActual + 1) / pasosTutorial.Length;
    
    // =======================================
    // 🧪 MÉTODOS DE TESTING
    // =======================================
    
    [ContextMenu("🚀 Test Iniciar Tutorial")]
    public void TestIniciarTutorial() => IniciarTutorial();
    
    [ContextMenu("➡️ Test Avanzar Paso")]
    public void TestAvanzarPaso() => AvanzarPaso();
    
    [ContextMenu("🎯 Test Ir a Paso 6")]
    public void TestIrAPaso6() => IrAPaso(6);
    
    [ContextMenu("🎯 Test Ir a Paso 13")]
    public void TestIrAPaso13() => IrAPaso(13);
    
    [ContextMenu("🛑 Test Detener")]
    public void TestDetenerTutorial() => DetenerTutorial();
    
    [ContextMenu("🔊 Test Audio")]
    public void TestReproducirAudio() => ReproducirAudio();
    
    void OnDestroy()
    {
        if (highlighter != null)
            highlighter.OcultarResaltado();
    }
}

/// <summary>
/// Clase simple para cada paso del tutorial
/// Sin conflictos de compilación
/// </summary>
[System.Serializable]
public class PasoTutorialSimple
{
    [Header("📋 Información")]
    public int numeroPaso = 1;
    
    [TextArea(3, 5)]
    public string textoInstruccion = "Instrucción del paso";
    
    [Header("🎯 Resaltado")]
    public GameObject elementoAResaltar;
    public TipoResaltado tipoResaltado = TipoResaltado.Flecha;
    public DireccionFlecha direccionFlecha = DireccionFlecha.Izquierda;
    
    [Header("🎵 Audio")]
    public AudioClip audioDelPaso;
}
