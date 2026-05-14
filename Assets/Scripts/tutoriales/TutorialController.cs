using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public enum CondicionAvance
{
    TapPanel,
    ImageTargetDetectado,
    BotonNombrePulsado,
    BotonColorPulsado,
    BotonGuardarPulsado,
    BotonCerrarInfoPulsado,
    InventarioAbierto,
    InventarioCerrado,
    MisionDescifrada,
    MisionCompletada,
    Delay
}

[Serializable]
public class TutorialStep
{
    [TextArea(2, 5)] public string textoES;
    [TextArea(2, 5)] public string textoEN;
    public AudioClip audioES;
    public AudioClip audioEN;
    public CondicionAvance condicion;
    [Tooltip("Parámetro para ImageTargetDetectado, MisionDescifrada, MisionCompletada (ID esperado).")]
    public string parametro;
    [Tooltip("Sólo para condicion == Delay. Segundos antes de avanzar.")]
    public float delaySegundos = 0f;
    [Tooltip("Elemento UI a resaltar cuando este paso está activo (opcional).")]
    public GameObject elementoAResaltar;
    public TipoResaltado tipoResaltado = TipoResaltado.Flecha;
}

/// <summary>
/// Controla el flujo de un tutorial interactivo paso a paso.
/// Avanza cuando el jugador realiza la acción esperada (evento real) o toca el panel.
/// Al completarse guarda un archivo en persistentDataPath y carga la escena principal.
/// </summary>
public class TutorialController : MonoBehaviour
{
    [Header("Pasos del Tutorial")]
    [SerializeField] private List<TutorialStep> pasos = new List<TutorialStep>();

    [Header("UI")]
    [SerializeField] private GameObject panelTutorial;
    [SerializeField] private TextMeshProUGUI textoPaso;
    [SerializeField] private Button botonSiguiente;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Pantalla Final")]
    [Tooltip("Panel que se muestra al terminar el tutorial (opcional). Si no está asignado se reutiliza el panel principal.")]
    [SerializeField] private GameObject panelFinal;
    [Tooltip("Botón que lleva al juego. Al pulsarlo se desactivan todos los objetos del tutorial y se carga la escena.")]
    [SerializeField] private Button botonIrAlJuego;
    [Tooltip("Texto del mensaje de cierre (opcional).")]
    [SerializeField] private TextMeshProUGUI textoFinal;
    [SerializeField] private string mensajeFinalES = "¡Tutorial completado!\n¡Ahora a jugar!";
    [SerializeField] private string mensajeFinalEN = "Tutorial complete!\nTime to play!";

    [Header("Resaltado")]
    [SerializeField] private TutorialHighlighter highlighter;

    [Header("Escena siguiente")]
    [SerializeField] private string nombreEscenaPrincipal = "MainScene";
    [SerializeField] private string archivoProgreso = "tutorial_completado.json";

    [Header("Idioma")]
    [SerializeField] private Idioma idiomaActual = Idioma.Espanol;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private int pasoActual = -1;
    private Coroutine delayCoroutine;

    void Start()
    {
        SuscribirEventos();

        if (botonIrAlJuego != null)
        {
            botonIrAlJuego.onClick.RemoveAllListeners();
            botonIrAlJuego.onClick.AddListener(IrAlJuego);
            botonIrAlJuego.gameObject.SetActive(false);
        }

        if (panelFinal != null)
            panelFinal.SetActive(false);

        if (botonSiguiente != null)
        {
            botonSiguiente.onClick.RemoveAllListeners();
            botonSiguiente.onClick.AddListener(AvanzarPorTapPanel);
        }
        MostrarPaso(0);
    }

    void OnDestroy()
    {
        DesuscribirEventos();
    }

    // -----------------------------
    // Suscripción a eventos de juego
    // -----------------------------
    private void SuscribirEventos()
    {
        if (ObjectInfoUIManager.Instance != null)
        {
            ObjectInfoUIManager.Instance.OnBotonNombrePulsado  += HandleBotonNombre;
            ObjectInfoUIManager.Instance.OnBotonColorPulsado   += HandleBotonColor;
            ObjectInfoUIManager.Instance.OnBotonGuardarPulsado += HandleBotonGuardar;
            ObjectInfoUIManager.Instance.OnBotonCerrarPulsado  += HandleBotonCerrar;
        }

        if (InventarioToggleController.Instance != null)
            InventarioToggleController.Instance.OnVisibilityChanged += HandleInventario;

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMisionDescifrada += HandleMisionDescifrada;
            MissionManager.Instance.OnMisionCompletada += HandleMisionCompletada;
        }

        VuforiaTargetTracker.OnTargetFound += HandleTargetFound;
    }

    private void DesuscribirEventos()
    {
        if (ObjectInfoUIManager.Instance != null)
        {
            ObjectInfoUIManager.Instance.OnBotonNombrePulsado  -= HandleBotonNombre;
            ObjectInfoUIManager.Instance.OnBotonColorPulsado   -= HandleBotonColor;
            ObjectInfoUIManager.Instance.OnBotonGuardarPulsado -= HandleBotonGuardar;
            ObjectInfoUIManager.Instance.OnBotonCerrarPulsado  -= HandleBotonCerrar;
        }

        if (InventarioToggleController.Instance != null)
            InventarioToggleController.Instance.OnVisibilityChanged -= HandleInventario;

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMisionDescifrada -= HandleMisionDescifrada;
            MissionManager.Instance.OnMisionCompletada -= HandleMisionCompletada;
        }

        VuforiaTargetTracker.OnTargetFound -= HandleTargetFound;
    }

    private void HandleBotonNombre()            => TryAvanzar(CondicionAvance.BotonNombrePulsado);
    private void HandleBotonColor()             => TryAvanzar(CondicionAvance.BotonColorPulsado);
    private void HandleBotonGuardar()           => TryAvanzar(CondicionAvance.BotonGuardarPulsado);
    private void HandleBotonCerrar()            => TryAvanzar(CondicionAvance.BotonCerrarInfoPulsado);
    private void HandleMisionDescifrada(string id) => TryAvanzar(CondicionAvance.MisionDescifrada, id);
    private void HandleMisionCompletada(string id) => TryAvanzar(CondicionAvance.MisionCompletada, id);
    private void HandleTargetFound(string id)       => TryAvanzar(CondicionAvance.ImageTargetDetectado, id);

    private void HandleInventario(bool visible)
    {
        TryAvanzar(visible ? CondicionAvance.InventarioAbierto : CondicionAvance.InventarioCerrado);
    }

    private void AvanzarPorTapPanel() => TryAvanzar(CondicionAvance.TapPanel);

    // -----------------------------
    // Lógica de avance
    // -----------------------------
    private void TryAvanzar(CondicionAvance cond, string parametro = null)
    {
        if (pasoActual < 0 || pasoActual >= pasos.Count) return;
        var p = pasos[pasoActual];
        if (p.condicion != cond) return;
        if (!string.IsNullOrEmpty(p.parametro) && p.parametro != parametro) return;

        if (debug) Debug.Log($"[Tutorial] ✅ Paso {pasoActual} cumplido ({cond} {parametro})");
        MostrarPaso(pasoActual + 1);
    }

    private void MostrarPaso(int indice)
    {
        if (highlighter != null) highlighter.OcultarResaltado();
        if (delayCoroutine != null) { StopCoroutine(delayCoroutine); delayCoroutine = null; }

        pasoActual = indice;
        if (pasoActual >= pasos.Count) { Completar(); return; }

        var p = pasos[pasoActual];

        if (textoPaso != null)
            textoPaso.text = idiomaActual == Idioma.Ingles ? p.textoEN : p.textoES;

        AudioClip clip = idiomaActual == Idioma.Ingles ? p.audioEN : p.audioES;
        if (clip != null && GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoSFX(clip, 1f);

        if (p.elementoAResaltar != null && highlighter != null)
        {
            highlighter.ConfigurarElemento(p.elementoAResaltar);
            highlighter.CambiarTipoResaltado(p.tipoResaltado);
            highlighter.MostrarResaltado();
        }

        bool esNarrativo = p.condicion == CondicionAvance.TapPanel;
        if (botonSiguiente != null)
            botonSiguiente.gameObject.SetActive(esNarrativo);

        // Permitir clics al juego cuando la acción requerida es del juego
        if (panelCanvasGroup != null)
            panelCanvasGroup.blocksRaycasts = esNarrativo;

        if (p.condicion == CondicionAvance.Delay)
            delayCoroutine = StartCoroutine(EsperarDelay(p.delaySegundos));

        if (debug) Debug.Log($"[Tutorial] ▶ Paso {pasoActual}/{pasos.Count}: {p.condicion} esperado");
    }

    private IEnumerator EsperarDelay(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        MostrarPaso(pasoActual + 1);
    }

    private void Completar()
    {
        if (debug) Debug.Log("[Tutorial] 🏁 Tutorial completado — mostrando pantalla final");

        // Guardar progreso
        try
        {
            string ruta = Path.Combine(Application.persistentDataPath, archivoProgreso);
            File.WriteAllText(ruta, "{\"completado\":true}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Tutorial] No se pudo guardar progreso: {e.Message}");
        }

        // Desuscribir eventos: ya no necesitamos escuchar al juego
        DesuscribirEventos();

        // Ocultar resaltados y botón Siguiente
        if (highlighter != null) highlighter.OcultarResaltado();
        if (botonSiguiente != null) botonSiguiente.gameObject.SetActive(false);

        // Mostrar pantalla final con botón "Ir al Juego"
        if (panelFinal != null)
            panelFinal.SetActive(true);

        if (textoFinal != null)
            textoFinal.text = idiomaActual == Idioma.Ingles ? mensajeFinalEN : mensajeFinalES;
        else if (textoPaso != null)
            textoPaso.text = idiomaActual == Idioma.Ingles ? mensajeFinalEN : mensajeFinalES;

        if (botonIrAlJuego != null)
            botonIrAlJuego.gameObject.SetActive(true);
        else
        {
            // Fallback: si no hay botón configurado, cargar después de 2 segundos
            if (debug) Debug.LogWarning("[Tutorial] botonIrAlJuego no asignado, cargando automáticamente en 2s");
            StartCoroutine(CargarEscenaConDelay(2f));
        }
    }

    private IEnumerator CargarEscenaConDelay(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        IrAlJuego();
    }

    /// <summary>
    /// Desactiva todos los objetos de la escena del tutorial y carga la escena principal.
    /// Llamado por el botón "Ir al Juego".
    /// </summary>
    public void IrAlJuego()
    {
        if (debug) Debug.Log($"[Tutorial] ▶ Ir al juego → {nombreEscenaPrincipal}");

        // ─── LIMPIEZA DDOL ────────────────────────────────────────────────────
        // Destruir TODOS los objetos que viven en la escena DontDestroyOnLoad
        // (singletons nuestros + objetos internos de Vuforia como [Debug Updater]).
        // Sin esto, Vuforia del tutorial impide que el juego reinicialice su
        // sesión AR → pantalla blanca + input roto.
        LimpiarDontDestroyOnLoad();

        if (!string.IsNullOrEmpty(nombreEscenaPrincipal))
            SceneManager.LoadScene(nombreEscenaPrincipal);
    }

    private void LimpiarDontDestroyOnLoad()
    {
        // Recopilar en lista primero para no modificar la colección al iterar
        var objetos = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject go in FindObjectsOfType<GameObject>(true))
        {
            if (go.scene.name == "DontDestroyOnLoad")
                objetos.Add(go);
        }

        foreach (GameObject go in objetos)
        {
            if (debug) Debug.Log($"[Tutorial] 🗑️ Destruyendo DDOL: {go.name}");
            Destroy(go);
        }
    }

    // -----------------------------
    // API pública
    // -----------------------------
    public void CambiarIdioma(Idioma nuevo)
    {
        idiomaActual = nuevo;
        if (pasoActual >= 0 && pasoActual < pasos.Count)
        {
            var p = pasos[pasoActual];
            if (textoPaso != null)
                textoPaso.text = idiomaActual == Idioma.Ingles ? p.textoEN : p.textoES;
        }
    }

    [ContextMenu("🔄 Resetear tutorial (borra progreso)")]
    public void ResetearProgreso()
    {
        string ruta = Path.Combine(Application.persistentDataPath, archivoProgreso);
        if (File.Exists(ruta)) File.Delete(ruta);
        Debug.Log("[Tutorial] Progreso borrado");
    }
}
