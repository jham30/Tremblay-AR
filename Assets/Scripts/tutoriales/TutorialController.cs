using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

public enum CondicionAvance
{
    TapPanel,
    ImageTargetDetectado,
    /// <summary>
    /// El jugador tocó el objeto 3D en AR y se abrió su panel de info (con los botones de
    /// nombre/color/guardar). Distinto de ImageTargetDetectado: ese es solo la lámina enfocada
    /// por la cámara, esto es el toque real sobre el objeto que hace aparecer los botones.
    /// </summary>
    ObjetoTocado,
    /// <summary>
    /// El jugador arrastró un objeto del inventario y lo soltó en un socket de misión
    /// (drag & drop del descifrado). Sirve para confirmar que entendió la mecánica ANTES de
    /// exigirle descifrar la misión entera (para eso ya está MisionDescifrada).
    /// Por defecto vale cualquier drop; con 'soloDropCorrecto' se exige que acierte el socket.
    /// </summary>
    ObjetoSoltadoEnSocket,
    /// <summary>
    /// El jugador pulsó "Agarrar" y el objeto quedó efectivamente agarrado en AR.
    /// USA ESTA en vez de BotonPulsado con el botón Agarrar: ObjectInfoUIManager
    /// .ConfigurarBotonesDinamicos hace onClick.RemoveAllListeners() cada vez que refresca los
    /// botones, y se lleva por delante el listener del tutorial → el paso se quedaba atascado.
    /// </summary>
    ObjetoAgarrado,
    /// <summary>
    /// El jugador colocó con éxito el objeto agarrado sobre un destino.
    /// Mismo motivo que ObjetoAgarrado para no usar BotonPulsado con el botón Colocar.
    /// </summary>
    ObjetoColocado,
    BotonNombrePulsado,
    BotonColorPulsado,
    BotonGuardarPulsado,
    BotonCerrarInfoPulsado,
    InventarioAbierto,
    InventarioCerrado,
    MisionDescifrada,
    MisionCompletada,
    Delay,
    /// <summary>
    /// GENÉRICA: espera a que se pulse CUALQUIER botón que arrastres al paso.
    /// No requiere tocar managers ni añadir valores nuevos al enum para cada botón.
    /// </summary>
    BotonPulsado
}

/// <summary>
/// Controla si el panel del tutorial bloquea los toques al juego que hay detrás.
/// </summary>
public enum BloqueoFondo
{
    /// <summary>Como hasta ahora: bloquea solo en pasos narrativos (TapPanel).</summary>
    Automatico,
    /// <summary>Fuerza bloqueo: el jugador NO puede tocar el juego detrás.</summary>
    Bloquear,
    /// <summary>Fuerza dejar pasar los toques al juego.</summary>
    NoBloquear
}

[Serializable]
public class TutorialStep
{
    [Tooltip("Texto del paso (tabla UI, idioma nativo). {0} = nombre del idioma meta.")]
    public LocalizedString texto;
    [Tooltip("Narración del paso (tabla StoryAudio, idioma nativo).")]
    public LocalizedAudioClip audio;
    public CondicionAvance condicion;
    [Tooltip("Parámetro para ImageTargetDetectado, ObjetoTocado, ObjetoSoltadoEnSocket, " +
             "ObjetoAgarrado, ObjetoColocado, MisionDescifrada, MisionCompletada (ID esperado).\n" +
             "En ObjetoSoltadoEnSocket es el ID del objeto ARRASTRADO; en ObjetoColocado, el del " +
             "objeto COLOCADO (no el destino). Vacío = cualquiera.")]
    public string parametro;
    [Tooltip("Sólo para condicion == Delay. Segundos antes de avanzar.")]
    public float delaySegundos = 0f;
    [Tooltip("Sólo para condicion == ObjetoSoltadoEnSocket.\n" +
             "false (por defecto) = avanza con CUALQUIER drop en un socket (basta con que entienda " +
             "la mecánica de arrastrar y soltar).\n" +
             "true = solo avanza si soltó el objeto CORRECTO en ese socket. Ojo: si el niño falla " +
             "no avanza y puede no entender por qué.")]
    public bool soloDropCorrecto = false;
    [Tooltip("Elemento UI a resaltar cuando este paso está activo (opcional).")]
    public GameObject elementoAResaltar;
    public TipoResaltado tipoResaltado = TipoResaltado.Flecha;
    [Tooltip("Sólo si tipoResaltado == Flecha. LADO del elemento donde se coloca la flecha " +
             "(Arriba = la flecha va encima del botón). Útil cuando el botón queda tapado por " +
             "un lado. Usa el sprite correspondiente configurado en el TutorialHighlighter.")]
    public DireccionFlecha direccionFlecha = DireccionFlecha.Izquierda;
    [Tooltip("Sólo para condicion == BotonPulsado. Botón que debe pulsar el jugador.\n" +
             "Si se deja vacío, se usa el Button de 'elementoAResaltar' (así arrastras el botón UNA vez " +
             "y sirve para resaltarlo Y para esperarlo).")]
    public Button botonEsperado;
    [Tooltip("¿El panel del tutorial bloquea los toques al juego de atrás en este paso?\n" +
             "Automatico = bloquea solo en pasos narrativos (TapPanel).\n" +
             "Bloquear = el jugador NO puede tocar el juego (evita que abra algo y se pierda).\n" +
             "NoBloquear = deja pasar siempre los toques (también en pasos narrativos: el botón " +
             "'Siguiente' sigue siendo pulsable).")]
    public BloqueoFondo bloqueoFondo = BloqueoFondo.Automatico;

    [Header("— Qué se puede tocar en este paso —")]
    [Tooltip("Todo = como siempre, el jugador puede pulsar cualquier botón.\n" +
             "SoloElResaltado = de la lista 'Elementos Gobernados' del TutorialController solo queda " +
             "pulsable el botón de este paso (más los de 'Permitidos Extra'); el resto sale " +
             "en gris. Evita que se vaya al inventario o al listado de misiones y se pierda.")]
    public ModoInteraccion modoInteraccion = ModoInteraccion.Todo;
    [Tooltip("Sólo con SoloElResaltado. Botones que ADEMÁS siguen pulsables en este paso " +
             "(ej. el de repetir audio junto al de guardar).")]
    public Selectable[] permitidosExtra;
    [Tooltip("ID del único objeto AR que responde al toque en este paso (ej. 'vela_tutorial'). " +
             "Vacío = todos. Sirve cuando en la lámina hay varios objetos y el paso pide uno " +
             "concreto: al tocar otro no se abre su panel y se muestra el texto de ayuda.")]
    public string objetoARPermitido;

    [Header("— Ayuda cuando se equivoca —")]
    [Tooltip("Texto que se muestra si el jugador hace la acción con el objeto/socket equivocado, " +
             "o si falla la comprobación de la misión. Vacío = no se muestra nada (como antes).\n" +
             "El paso NO avanza: solo se le dice qué pasó y vuelve el texto original.")]
    public LocalizedString textoAyuda;
    [Tooltip("Narración opcional de la ayuda.")]
    public LocalizedAudioClip audioAyuda;
    [Tooltip("Segundos que se ve el texto de ayuda antes de volver al texto del paso.")]
    public float segundosAyuda = 4f;
}

/// <summary>
/// Qué puede tocar el jugador durante un paso. Nació porque 'bloqueoFondo' solo sabe todo o nada:
/// hacía falta el punto medio ("solo esto está vivo"), que es lo que evita que el niño se salga
/// del carril pulsando el inventario o el listado de misiones.
/// </summary>
public enum ModoInteraccion
{
    /// <summary>Como hasta ahora: cualquier botón de la pantalla es pulsable.</summary>
    Todo,
    /// <summary>Solo el botón de este paso (y los 'Permitidos Extra'); el resto en gris.</summary>
    SoloElResaltado
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
    [Tooltip("Botón opcional para volver a escuchar la narración del paso actual. Se oculta solo " +
             "en los pasos que no tienen audio en el idioma activo.")]
    [SerializeField] private Button botonRepetirAudio;
    [Tooltip("Botón opcional para SALTAR el tutorial: lo marca como completado y va directo al juego. " +
             "Conviene ponerlo FUERA del panel de pasos (esquina de la pantalla) para que esté siempre a mano.")]
    [SerializeField] private Button botonSaltarTutorial;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [Tooltip("Gráfico que 'atrapa' los toques cuando un paso bloquea el fondo (se le activa/desactiva " +
             "el Raycast Target). Si se deja vacío se usa la Image del propio panelTutorial.\n" +
             "OJO: solo bloquea DONDE cubre; para bloquear toda la pantalla debe cubrirla entera.")]
    [SerializeField] private Graphic graficoBloqueo;
    [Tooltip("Opacidad del gráfico de bloqueo cuando el paso BLOQUEA el fondo (0 = invisible, " +
             "0.1 = 10%). Da pista visual de que el juego está congelado.")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaBloqueoActivo = 0.1f;
    [Tooltip("Opacidad del gráfico de bloqueo cuando el paso NO bloquea (normalmente 0).")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaBloqueoInactivo = 0f;

    [Header("Interacción gobernada por el tutorial")]
    [Tooltip("Los botones que el tutorial puede apagar y encender: el de inventario, el de " +
             "misiones, ajustes, guardar, cerrar, nombre, color, agarrar, comprobar…\n" +
             "Se declara UNA vez aquí; cada paso con 'SoloElResaltado' apaga todos menos el suyo. " +
             "Lo que no esté en esta lista, el tutorial no lo toca nunca.")]
    [SerializeField] private Selectable[] elementosGobernados;
    [Tooltip("Cada cuántos segundos se vuelve a aplicar la política de este paso. Hace falta " +
             "porque los botones del panel de ObjectInfo se reconstruyen al tocar un objeto " +
             "(ConfigurarBotonesDinamicos) y se llevarían por delante el estado que puso el paso.")]
    [SerializeField] private float intervaloReaplicarInteraccion = 0.25f;

    [Header("Pantalla Final")]
    [Tooltip("Panel que se muestra al terminar el tutorial (opcional). Si no está asignado se reutiliza el panel principal.")]
    [SerializeField] private GameObject panelFinal;
    [Tooltip("Botón que lleva al juego. Al pulsarlo se desactivan todos los objetos del tutorial y se carga la escena.")]
    [SerializeField] private Button botonIrAlJuego;
    [Tooltip("Texto del mensaje de cierre (opcional).")]
    [SerializeField] private TextMeshProUGUI textoFinal;
    [Tooltip("Etiqueta del botón que lleva al juego (opcional). Se usa para cambiarle el texto " +
             "cuando el panel final se reutiliza como confirmación de '¿saltar el tutorial?'.")]
    [SerializeField] private TextMeshProUGUI textoBotonIrAlJuego;

    [Header("Confirmación al saltar")]
    [Tooltip("Botón 'seguir con el tutorial' que cancela el salto. Vive en el panel final, que se " +
             "reutiliza como ventana de confirmación. Si se deja vacío, SALTAR NO PIDE " +
             "CONFIRMACIÓN (se va al juego directo).")]
    [SerializeField] private Button botonCancelarSalto;
    [Tooltip("Clave de la tabla UI con la pregunta de confirmación (ej. 'tutorial.confirmar_saltar').")]
    [SerializeField] private string claveMensajeConfirmarSalto = "tutorial.confirmar_saltar";
    [Tooltip("Clave de la tabla UI para la etiqueta del botón que confirma el salto.")]
    [SerializeField] private string claveBotonConfirmarSalto = "tutorial.saltar_si";
    [Tooltip("Clave de la tabla UI para la etiqueta normal del botón que lleva al juego.")]
    [SerializeField] private string claveBotonIrAlJuego = "tutorial.ir_al_juego";

    [Header("Recordatorio del botón Siguiente")]
    [Tooltip("Sacude el botón 'Siguiente' de vez en cuando para llamar la atención del jugador " +
             "cuando lleva rato sin pulsarlo. Solo actúa en los pasos narrativos, que son los " +
             "únicos donde el botón está visible.")]
    [SerializeField] private bool shakeBotonSiguiente = true;
    [Tooltip("Segundos sin pulsar el botón antes de cada sacudida. El contador se reinicia en cada paso.")]
    [SerializeField] private float intervaloShakeSiguiente = 5f;
    [Tooltip("Duración de una sacudida.")]
    [SerializeField] private float duracionShakeSiguiente = 0.5f;
    [Tooltip("Desplazamiento horizontal máximo, en unidades de UI.")]
    [SerializeField] private float amplitudShakeSiguiente = 18f;
    [Tooltip("Idas y vueltas completas dentro de cada sacudida.")]
    [SerializeField] private float oscilacionesShakeSiguiente = 3f;

    [Header("Narración")]
    [Tooltip("Segundos antes de que arranque la narración de cada paso. Sin esto el audio entra " +
             "a la vez que aparece el texto y no da tiempo ni a mirar la pantalla.")]
    [SerializeField] private float retardoNarracion = 1.5f;
    [Tooltip("Tope de espera a que el canal de voz quede libre (p. ej. el nombre de un objeto " +
             "que el niño acaba de pedir). Pasado este tiempo la narración entra igual, para no " +
             "quedarse callado si algo deja el canal ocupado.")]
    [SerializeField] private float esperaMaximaVoz = 8f;

    [Header("Resaltado")]
    [SerializeField] private TutorialHighlighter highlighter;

    [Header("Escena siguiente")]
    [SerializeField] private string nombreEscenaPrincipal = "halloween";

    [Header("Reset al iniciar el tutorial")]
    [Tooltip("Deja el progreso de los objetos/misión del tutorial LIMPIO al arrancar la escena.\n" +
             "Es necesario porque el tutorial enseña TRANSICIONES (no-guardado → guardado, " +
             "no-descifrada → descifrada → completada). Si el jugador abandona el tutorial a medias " +
             "y vuelve, el estado quedaría en el final y los botones Guardar/Agarrar ya no " +
             "aparecerían: se quedaría atascado SIN forma de salir del tutorial.\n" +
             "Desactívalo solo para depurar.")]
    [SerializeField] private bool resetearProgresoAlIniciar = true;
    [Tooltip("IDs de los objetos que usa el tutorial. Se marcan como NO guardados al arrancar. " +
             "Deben coincidir con los ids del JSON (los mismos que pones en ObjectDisplayController).")]
    [SerializeField] private string[] objetosAResetear;
    [Tooltip("IDs de las misiones que usa el tutorial. Se dejan sin descifrar ni completar al arrancar. " +
             "OJO: el ScriptableObject de la misión DEBE tener su 'misionID' relleno; si está vacío " +
             "la misión nunca se guarda ni dispara sus eventos.")]
    [SerializeField] private string[] misionesAResetear;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private int pasoActual = -1;
    private Coroutine delayCoroutine;
    private Coroutine resaltadoCoroutine;
    private Coroutine shakeCoroutine;
    private RectTransform rectBotonSiguiente;
    private Vector2 posBaseBotonSiguiente;
    private AudioClip clipPasoActual;
    private Coroutine repetirAudioCoroutine;
    private Coroutine narracionCoroutine;
    private AudioClip clipNarracionSonando;
    private Coroutine interaccionCoroutine;
    private Coroutine filtroARCoroutine;
    private Coroutine ayudaCoroutine;
    private bool confirmandoSalto;

    void Start()
    {
        // Si no se asignó, usar la Image del propio panel como bloqueador de toques.
        if (graficoBloqueo == null && panelTutorial != null)
            graficoBloqueo = panelTutorial.GetComponent<Graphic>();

        // Eventos estáticos: siempre disponibles (no dependen de que exista un singleton).
        VuforiaTargetTracker.OnTargetFound += HandleTargetFound;
        DropSocketAvanzado.OnObjetoSoltadoEnSocket += HandleObjetoSoltadoEnSocket;
        ObjectInfoGrabDropController.OnObjetoAgarrado += HandleObjetoAgarrado;
        ObjectInfoGrabDropController.OnObjetoColocado += HandleObjetoColocado;
        InputRouter.OnObjetoBloqueado += HandleObjetoBloqueado;
        // Los managers son singletons por escena: reintentar por si aún no se registraron.
        StartCoroutine(SuscribirConReintentos());

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

        if (botonRepetirAudio != null)
        {
            botonRepetirAudio.onClick.RemoveAllListeners();
            botonRepetirAudio.onClick.AddListener(RepetirAudioPaso);
            botonRepetirAudio.gameObject.SetActive(false);
        }

        if (botonCancelarSalto != null)
            botonCancelarSalto.gameObject.SetActive(false);

        if (botonSaltarTutorial != null)
        {
            botonSaltarTutorial.onClick.RemoveAllListeners();
            botonSaltarTutorial.onClick.AddListener(SaltarTutorial);
        }

        if (resetearProgresoAlIniciar)
            StartCoroutine(ResetearProgresoTutorialYEmpezar());
        else
            MostrarPaso(0);
    }

    /// <summary>
    /// Deja el progreso del tutorial limpio ANTES del primer paso. Espera a que los managers
    /// existan (son singletons por escena y pueden registrarse después de este Start); si no
    /// aparecen en un margen razonable, arranca igual para no dejar el tutorial en blanco.
    /// </summary>
    private IEnumerator ResetearProgresoTutorialYEmpezar()
    {
        const float margen = 2f;
        float esperado = 0f;

        while ((GameObjectManager.Instance == null || MissionManager.Instance == null) && esperado < margen)
        {
            esperado += Time.deltaTime;
            yield return null;
        }

        ResetearProgresoTutorial();
        MostrarPaso(0);
    }

    /// <summary>
    /// Reset QUIRÚRGICO: solo los objetos y misiones que usa el tutorial, por ID.
    /// No toca el progreso del resto de cuentos.
    /// </summary>
    public void ResetearProgresoTutorial()
    {
        var gom = GameObjectManager.Instance;
        if (gom != null && objetosAResetear != null)
        {
            var scrollView = FindObjectOfType<ScrollViewLoader>();

            foreach (string id in objetosAResetear)
            {
                if (string.IsNullOrEmpty(id)) continue;

                if (gom.BuscarObjetoPorId(id) == null)
                {
                    Debug.LogWarning($"[Tutorial] ⚠️ Reset: no existe ningún objeto con ID '{id}'. " +
                                     $"Revisa 'Objetos A Resetear' en el Inspector.");
                    continue;
                }

                gom.DesmarcarGuardado(id);          // guardadoPorJugador = false + persistir
                scrollView?.ActualizarItemPorID(id); // que el inventario refleje el cambio
            }
        }
        else if (gom == null)
        {
            Debug.LogWarning("[Tutorial] ⚠️ Reset: GameObjectManager no encontrado; los objetos del " +
                             "tutorial NO se reiniciaron.");
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.ReiniciarMisionesEspecificas(misionesAResetear);
        else
            Debug.LogWarning("[Tutorial] ⚠️ Reset: MissionManager no encontrado; la misión del " +
                             "tutorial NO se reinició.");

        if (debug) Debug.Log("[Tutorial] 🧹 Progreso del tutorial reiniciado (objetos + misión).");
    }

    void OnDestroy()
    {
        DesuscribirEventos();
        DesengancharBotonDelPaso();
        DetenerRecordatorioSiguiente();

        // El filtro de InputRouter es ESTÁTICO: si el tutorial muere sin limpiarlo (cambio de
        // escena a media ejecución, Stop en el editor), el juego se quedaría sin poder tocar
        // más que el objeto del último paso.
        InputRouter.FiltroObjetoPermitido = null;

        var inv = InventarioToggleController.Instance;
        if (inv != null) inv.BloquearSwipe(false);
    }

    // -----------------------------
    // Suscripción a eventos de juego
    // -----------------------------
    // Flags para que la suscripción sea idempotente y se pueda reintentar: si un manager
    // aún no existe en Start, antes se saltaba la suscripción EN SILENCIO y ese paso del
    // tutorial nunca avanzaba. Ahora se reintenta y, si no aparece, se avisa.
    private bool suscritoObjectInfo, suscritoInventario, suscritoMisiones;

    private void SuscribirEventos()
    {
        if (!suscritoObjectInfo && ObjectInfoUIManager.Instance != null)
        {
            ObjectInfoUIManager.Instance.OnBotonNombrePulsado  += HandleBotonNombre;
            ObjectInfoUIManager.Instance.OnBotonColorPulsado   += HandleBotonColor;
            ObjectInfoUIManager.Instance.OnBotonGuardarPulsado += HandleBotonGuardar;
            ObjectInfoUIManager.Instance.OnBotonCerrarPulsado  += HandleBotonCerrar;
            ObjectInfoUIManager.Instance.OnObjetoTocado        += HandleObjetoTocado;
            suscritoObjectInfo = true;
        }

        if (!suscritoInventario && InventarioToggleController.Instance != null)
        {
            InventarioToggleController.Instance.OnVisibilityChanged += HandleInventario;
            suscritoInventario = true;
        }

        if (!suscritoMisiones && MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMisionDescifrada += HandleMisionDescifrada;
            MissionManager.Instance.OnMisionCompletada += HandleMisionCompletada;
            MissionManager.Instance.OnMisionFallida    += HandleMisionFallida;
            suscritoMisiones = true;
        }
    }

    /// <summary>
    /// Reintenta suscribirse unos instantes (los managers son singletons por escena y pueden
    /// registrarse después de este Start). Avisa por consola si alguno nunca apareció.
    /// </summary>
    private IEnumerator SuscribirConReintentos()
    {
        for (int intento = 0; intento < 10; intento++)
        {
            SuscribirEventos();
            if (suscritoObjectInfo && suscritoInventario && suscritoMisiones) yield break;
            yield return new WaitForSeconds(0.2f);
        }

        if (!suscritoObjectInfo) Debug.LogWarning("[Tutorial] ⚠️ ObjectInfoUIManager no encontrado: los pasos que esperan sus botones NO avanzarán.");
        if (!suscritoInventario) Debug.LogWarning("[Tutorial] ⚠️ InventarioToggleController no encontrado: los pasos de inventario NO avanzarán.");
        if (!suscritoMisiones)   Debug.LogWarning("[Tutorial] ⚠️ MissionManager no encontrado: los pasos de misión NO avanzarán.");
    }

    private void DesuscribirEventos()
    {
        if (suscritoObjectInfo && ObjectInfoUIManager.Instance != null)
        {
            ObjectInfoUIManager.Instance.OnBotonNombrePulsado  -= HandleBotonNombre;
            ObjectInfoUIManager.Instance.OnBotonColorPulsado   -= HandleBotonColor;
            ObjectInfoUIManager.Instance.OnBotonGuardarPulsado -= HandleBotonGuardar;
            ObjectInfoUIManager.Instance.OnBotonCerrarPulsado  -= HandleBotonCerrar;
            ObjectInfoUIManager.Instance.OnObjetoTocado        -= HandleObjetoTocado;
        }
        suscritoObjectInfo = false;

        if (suscritoInventario && InventarioToggleController.Instance != null)
            InventarioToggleController.Instance.OnVisibilityChanged -= HandleInventario;
        suscritoInventario = false;

        if (suscritoMisiones && MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMisionDescifrada -= HandleMisionDescifrada;
            MissionManager.Instance.OnMisionCompletada -= HandleMisionCompletada;
            MissionManager.Instance.OnMisionFallida    -= HandleMisionFallida;
        }
        suscritoMisiones = false;

        VuforiaTargetTracker.OnTargetFound -= HandleTargetFound;
        InputRouter.OnObjetoBloqueado -= HandleObjetoBloqueado;
        DropSocketAvanzado.OnObjetoSoltadoEnSocket -= HandleObjetoSoltadoEnSocket;
        ObjectInfoGrabDropController.OnObjetoAgarrado -= HandleObjetoAgarrado;
        ObjectInfoGrabDropController.OnObjetoColocado -= HandleObjetoColocado;
    }

    /// <summary>
    /// El jugador tocó un objeto AR que el paso no permitía (el filtro lo rechazó, así que su
    /// panel NI se abrió). Antes esto no existía: el toque no hacía nada y el niño no sabía
    /// si se había equivocado o si el juego estaba roto.
    /// </summary>
    private void HandleObjetoBloqueado(string id)
    {
        if (debug) Debug.Log($"[Tutorial] 🚫 Paso {pasoActual}: tocó '{id}', que este paso no permite");
        MostrarAyudaDelPaso($"objeto '{id}' no permitido");
    }

    /// <summary>
    /// Falló la comprobación de la misión (hay objetos mal colocados, incorrectos o vacíos).
    /// Solo se responde si el paso esperaba que la misión saliera bien: es el gancho de
    /// "enseñar a fallar" — el niño ve el error, y la ayuda le dice que lea el resultado.
    /// </summary>
    private void HandleMisionFallida()
    {
        if (pasoActual < 0 || pasoActual >= pasos.Count) return;

        var c = pasos[pasoActual].condicion;
        if (c != CondicionAvance.MisionDescifrada && c != CondicionAvance.MisionCompletada) return;

        if (debug) Debug.Log($"[Tutorial] ❌ Paso {pasoActual}: comprobación de misión fallida");
        MostrarAyudaDelPaso("comprobación de misión fallida");
    }

    private void HandleObjetoTocado(string id)   => TryAvanzar(CondicionAvance.ObjetoTocado, id);
    private void HandleObjetoAgarrado(string id) => TryAvanzar(CondicionAvance.ObjetoAgarrado, id);
    // 'parametro' filtra por el objeto COLOCADO (coherente con las demás condiciones);
    // el destino se registra en el log por si hace falta depurar.
    private void HandleObjetoColocado(string objetoID, string destinoID)
    {
        if (debug) Debug.Log($"[Tutorial] 📦 Objeto '{objetoID}' colocado en destino '{destinoID}'");
        TryAvanzar(CondicionAvance.ObjetoColocado, objetoID);
    }

    /// <summary>
    /// Drag &amp; drop de un objeto del inventario a un socket de misión.
    /// El filtro por 'parametro' (ID del objeto arrastrado) lo hace TryAvanzar; aquí solo se
    /// aplica la puerta extra 'soloDropCorrecto', que es propia de este tipo de paso.
    /// </summary>
    private void HandleObjetoSoltadoEnSocket(string objetoID, string idCorrectoSocket, bool acerto)
    {
        if (pasoActual >= 0 && pasoActual < pasos.Count)
        {
            var p = pasos[pasoActual];
            if (p.condicion == CondicionAvance.ObjetoSoltadoEnSocket && p.soloDropCorrecto && !acerto)
            {
                if (debug)
                    Debug.Log($"[Tutorial] Paso {pasoActual}: soltó '{objetoID}' en un socket que esperaba " +
                              $"'{idCorrectoSocket}'. El paso exige acierto (soloDropCorrecto) → NO avanza.");
                MostrarAyudaDelPaso($"'{objetoID}' soltado donde iba '{idCorrectoSocket}'");
                return;
            }
        }

        TryAvanzar(CondicionAvance.ObjetoSoltadoEnSocket, objetoID);
    }
    private void HandleBotonNombre()            => TryAvanzar(CondicionAvance.BotonNombrePulsado);
    private void HandleBotonColor()             => TryAvanzar(CondicionAvance.BotonColorPulsado);
    private void HandleBotonGuardar()           => TryAvanzar(CondicionAvance.BotonGuardarPulsado);
    private void HandleBotonCerrar()            => TryAvanzar(CondicionAvance.BotonCerrarInfoPulsado);
    private void HandleMisionDescifrada(string id) => TryAvanzar(CondicionAvance.MisionDescifrada, id);
    private void HandleMisionCompletada(string id) => TryAvanzar(CondicionAvance.MisionCompletada, id);
    private void HandleTargetFound(string id)
    {
        if (debug) Debug.Log($"[Tutorial] 🎯 Target detectado por el tracker con id: '{id}'");
        TryAvanzar(CondicionAvance.ImageTargetDetectado, id);
    }

    /// <summary>
    /// Condiciones que el jugador solo puede cumplir TOCANDO algo del juego. Si el panel del
    /// tutorial bloquea el fondo en uno de estos pasos, no podría completarlo.
    /// (ImageTargetDetectado y Delay no entran: no dependen del toque.)
    /// </summary>
    private bool CondicionRequiereTocarElJuego(CondicionAvance c)
    {
        return c == CondicionAvance.BotonPulsado
            || c == CondicionAvance.ObjetoTocado
            || c == CondicionAvance.ObjetoSoltadoEnSocket
            || c == CondicionAvance.ObjetoAgarrado
            || c == CondicionAvance.ObjetoColocado
            || c == CondicionAvance.BotonNombrePulsado
            || c == CondicionAvance.BotonColorPulsado
            || c == CondicionAvance.BotonGuardarPulsado
            || c == CondicionAvance.BotonCerrarInfoPulsado
            || c == CondicionAvance.InventarioAbierto
            || c == CondicionAvance.InventarioCerrado
            || c == CondicionAvance.MisionDescifrada
            || c == CondicionAvance.MisionCompletada;
    }

    /// <summary>
    /// ¿El target esperado por este paso ya está visible ahora mismo?
    /// Si el paso no filtra por id (parametro vacío), vale cualquier target detectado.
    /// </summary>
    private bool TargetYaDetectado(string parametro)
    {
        return string.IsNullOrEmpty(parametro)
            ? VuforiaTargetTracker.HayAlgunTargetActivo()
            : VuforiaTargetTracker.EstaTargetActivo(parametro);
    }

    private void HandleInventario(bool visible)
    {
        TryAvanzar(visible ? CondicionAvance.InventarioAbierto : CondicionAvance.InventarioCerrado);
    }

    private void AvanzarPorTapPanel() => TryAvanzar(CondicionAvance.TapPanel);

    // -----------------------------
    // Condición genérica: esperar CUALQUIER botón
    // -----------------------------
    private Button botonPasoActual;

    private void EngancharBotonDelPaso(TutorialStep p)
    {
        DesengancharBotonDelPaso();

        if (p.condicion != CondicionAvance.BotonPulsado) return;

        // El botón del paso; si no se asignó, se toma el del elemento resaltado.
        Button b = p.botonEsperado;
        if (b == null && p.elementoAResaltar != null)
            b = p.elementoAResaltar.GetComponent<Button>();

        if (b == null)
        {
            Debug.LogWarning($"[Tutorial] ⚠️ Paso {pasoActual}: condición BotonPulsado pero no hay " +
                             $"'botonEsperado' ni un Button en 'elementoAResaltar'. El tutorial se quedará atascado.");
            return;
        }

        botonPasoActual = b;
        botonPasoActual.onClick.AddListener(HandleBotonEsperado);
    }

    private void DesengancharBotonDelPaso()
    {
        if (botonPasoActual != null)
            botonPasoActual.onClick.RemoveListener(HandleBotonEsperado);
        botonPasoActual = null;
    }

    private void HandleBotonEsperado() => TryAvanzar(CondicionAvance.BotonPulsado);

    // -----------------------------
    // Lógica de avance
    // -----------------------------
    private void TryAvanzar(CondicionAvance cond, string parametro = null)
    {
        // Mientras se pregunta "¿seguro que quieres saltar?", el tutorial de atrás no avanza:
        // si no, el paso podía cambiar bajo la ventana de confirmación.
        if (confirmandoSalto) return;

        if (pasoActual < 0 || pasoActual >= pasos.Count) return;
        var p = pasos[pasoActual];
        if (p.condicion != cond) return;

        if (!string.IsNullOrEmpty(p.parametro) && p.parametro != parametro)
        {
            if (debug)
                Debug.LogWarning($"[Tutorial] ⚠️ Paso {pasoActual}: llegó '{cond}' con id '{parametro}', " +
                                 $"pero el paso espera el parámetro '{p.parametro}' → NO avanza. " +
                                 $"(Deja 'parametro' vacío para aceptar cualquiera.)");

            // Hizo la acción correcta con el objeto equivocado: es justo el momento de ayudar.
            MostrarAyudaDelPaso($"{cond} con '{parametro}' en vez de '{p.parametro}'");
            return;
        }

        if (debug) Debug.Log($"[Tutorial] ✅ Paso {pasoActual} cumplido ({cond} {parametro})");
        MostrarPaso(pasoActual + 1);
    }

    private void MostrarPaso(int indice)
    {
        if (resaltadoCoroutine != null) { StopCoroutine(resaltadoCoroutine); resaltadoCoroutine = null; }
        if (highlighter != null) highlighter.OcultarResaltado();
        if (delayCoroutine != null) { StopCoroutine(delayCoroutine); delayCoroutine = null; }
        if (interaccionCoroutine != null) { StopCoroutine(interaccionCoroutine); interaccionCoroutine = null; }
        if (filtroARCoroutine != null) { StopCoroutine(filtroARCoroutine); filtroARCoroutine = null; }
        if (ayudaCoroutine != null) { StopCoroutine(ayudaCoroutine); ayudaCoroutine = null; }
        DesengancharBotonDelPaso();

        pasoActual = indice;
        if (pasoActual >= pasos.Count) { Completar(); return; }

        var p = pasos[pasoActual];

        // Qué puede tocar el jugador en este paso (UI y objetos AR).
        AplicarInteraccion(p);
        if (p.modoInteraccion != ModoInteraccion.Todo)
            interaccionCoroutine = StartCoroutine(ReaplicarInteraccion(p));
        AplicarFiltroAR(p);

        if (textoPaso != null)
            textoPaso.text = TextoDelPaso(p);

        clipPasoActual = AudioDelPaso(p);
        ReproducirNarracionPaso();

        // Resaltado: se gestiona con una corrutina que ESPERA a que el elemento esté activo.
        // Muchos elementos (p. ej. los botones del panel de ObjectInfo en AR) no existen en
        // pantalla cuando arranca el paso: aparecen cuando el jugador toca el objeto.
        if (highlighter == null)
        {
            Debug.LogWarning($"[Tutorial] ⚠️ Paso {pasoActual}: el campo 'Highlighter' NO está asignado " +
                             $"en el Inspector del TutorialController → NINGÚN paso mostrará resaltado.");
        }
        else if (p.elementoAResaltar == null)
        {
            Debug.LogWarning($"[Tutorial] ⚠️ Paso {pasoActual}: 'Elemento A Resaltar' está vacío → este paso no resalta nada.");
        }
        else
        {
            resaltadoCoroutine = StartCoroutine(GestionarResaltado(p));
        }

        bool esNarrativo = p.condicion == CondicionAvance.TapPanel;
        if (botonSiguiente != null)
        {
            botonSiguiente.gameObject.SetActive(esNarrativo);

            // Cada paso reinicia la cuenta: el shake solo aparece si el jugador se queda parado.
            if (esNarrativo) IniciarRecordatorioSiguiente();
            else             DetenerRecordatorioSiguiente();
        }

        // ¿Bloquear los toques al juego de atrás? Configurable por paso.
        bool bloquear;
        if (p.bloqueoFondo == BloqueoFondo.Bloquear)        bloquear = true;
        else if (p.bloqueoFondo == BloqueoFondo.NoBloquear) bloquear = false;
        else                                                bloquear = esNarrativo; // Automatico

        // Guarda: si se bloquea el fondo pero la condición exige tocar algo del juego,
        // el jugador no podrá cumplirla nunca.
        if (bloquear && CondicionRequiereTocarElJuego(p.condicion))
        {
            Debug.LogWarning($"[Tutorial] ⚠️ Paso {pasoActual}: bloqueo activado con condición '{p.condicion}', " +
                             $"que requiere tocar el juego. Si el panel cubre ese botón, el paso NO podrá completarse.");
        }

        // Son DOS cosas INDEPENDIENTES (antes se movían juntas y por eso NoBloquear no se
        // respetaba en pasos narrativos: se forzaba el bloqueo para salvar el botón 'Siguiente'):
        //  1) blocksRaycasts del CanvasGroup → que los botones DEL PANEL (el 'Siguiente') se puedan
        //     pulsar. Hace falta siempre que el paso sea narrativo, bloquee el fondo o no.
        //  2) raycastTarget del graficoBloqueo → que el FONDO trague los toques al juego.
        //     Esto es lo único que decide 'bloquear'.
        if (panelCanvasGroup != null)
            panelCanvasGroup.blocksRaycasts = bloquear || esNarrativo;

        if (graficoBloqueo != null)
        {
            graficoBloqueo.raycastTarget = bloquear;

            // Pista visual: al bloquear, oscurecer un poco (mantiene el color, solo cambia el alpha)
            Color c = graficoBloqueo.color;
            c.a = bloquear ? alphaBloqueoActivo : alphaBloqueoInactivo;
            graficoBloqueo.color = c;
        }
        else if (bloquear)
            Debug.LogWarning($"[Tutorial] ⚠️ Paso {pasoActual}: se pidió bloquear el fondo pero no hay " +
                             $"'graficoBloqueo' (ni Image en panelTutorial) → no se bloqueará nada.");

        // Condición genérica: engancharse al botón que se espera en este paso
        EngancharBotonDelPaso(p);

        if (p.condicion == CondicionAvance.Delay)
            delayCoroutine = StartCoroutine(EsperarDelay(p.delaySegundos));

        // Vuforia solo avisa en los CAMBIOS de estado. Si el target YA está enfocado al empezar
        // el paso, el evento no se repetiría y el paso se quedaría atascado → comprobarlo ahora.
        if (p.condicion == CondicionAvance.ImageTargetDetectado && TargetYaDetectado(p.parametro))
        {
            if (debug) Debug.Log($"[Tutorial] ✅ Paso {pasoActual}: el target '{p.parametro}' ya estaba detectado → avanzando");
            MostrarPaso(pasoActual + 1);
            return;
        }

        if (debug) Debug.Log($"[Tutorial] ▶ Paso {pasoActual}/{pasos.Count}: {p.condicion} esperado");
    }

    // -----------------------------
    // Narración del paso
    // -----------------------------

    /// <summary>
    /// Arranca la narración del paso actual y deja el botón de repetir como toca: oculto si el
    /// paso no tiene audio, y sin poder pulsarse mientras suena.
    ///
    /// No suena de golpe. Espera 'retardoNarracion' para que al niño le dé tiempo a mirar la
    /// pantalla, y luego espera a que el canal de voz esté LIBRE: si está sonando el nombre de
    /// un objeto (los pasos que piden escucharlo), la narración entra detrás en vez de pisarlo.
    /// </summary>
    private void ReproducirNarracionPaso(bool conRetardo = true)
    {
        if (repetirAudioCoroutine != null) { StopCoroutine(repetirAudioCoroutine); repetirAudioCoroutine = null; }
        if (narracionCoroutine != null)    { StopCoroutine(narracionCoroutine);    narracionCoroutine = null; }

        // Cortar NUESTRA narración anterior (al pulsar 'Siguiente' antes de que acabe), pero no
        // la voz de otro: si lo que suena es el nombre de un objeto, se le deja terminar.
        DetenerNarracionPropia();

        bool hayAudio = clipPasoActual != null;

        if (botonRepetirAudio != null)
        {
            botonRepetirAudio.gameObject.SetActive(hayAudio);
            if (hayAudio) botonRepetirAudio.interactable = false;
        }

        if (!hayAudio) return;

        narracionCoroutine = StartCoroutine(NarrarConRetardo(clipPasoActual, conRetardo));
    }

    private IEnumerator NarrarConRetardo(AudioClip clip, bool conRetardo)
    {
        if (conRetardo && retardoNarracion > 0f)
            yield return new WaitForSecondsRealtime(retardoNarracion);

        // Esperar a que la voz quede libre (nombre del objeto, ayuda anterior…), con tope para
        // no quedarse colgado si algo deja el canal ocupado.
        float esperado = 0f;
        var gam = GlobalAudioManager.Instance;
        while (gam != null && gam.VozOcupada && esperado < esperaMaximaVoz)
        {
            esperado += Time.unscaledDeltaTime;
            yield return null;
        }

        // El paso pudo cambiar mientras esperábamos.
        if (clip != clipPasoActual) { narracionCoroutine = null; yield break; }

        if (gam != null)
        {
            gam.ReproducirVoz(clip, 1f);
            clipNarracionSonando = clip;
        }

        narracionCoroutine = null;
        repetirAudioCoroutine = StartCoroutine(RehabilitarRepetirAudio(clip.length));
    }

    /// <summary>
    /// Corta la voz SOLO si lo que suena es la narración que lanzamos nosotros. Así al avanzar
    /// de paso no se pisa el audio del objeto que el niño acaba de pedir.
    /// </summary>
    private void DetenerNarracionPropia()
    {
        var gam = GlobalAudioManager.Instance;
        if (gam == null || clipNarracionSonando == null) return;

        if (gam.ClipVozActual == clipNarracionSonando) gam.DetenerVoz();
        clipNarracionSonando = null;
    }

    private IEnumerator RehabilitarRepetirAudio(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        if (botonRepetirAudio != null) botonRepetirAudio.interactable = true;
        repetirAudioCoroutine = null;
    }

    /// <summary>
    /// Vuelve a escuchar la narración del paso actual. Pública para poder engancharla también
    /// desde el onClick del Inspector.
    /// </summary>
    public void RepetirAudioPaso()
    {
        if (clipPasoActual == null) return;

        // Sin retardo: el jugador acaba de pedirlo, esperar aquí se siente a botón roto.
        ReproducirNarracionPaso(conRetardo: false);
    }

    // -----------------------------
    // Recordatorio (shake) del botón Siguiente
    // -----------------------------

    private void IniciarRecordatorioSiguiente()
    {
        DetenerRecordatorioSiguiente();

        if (!shakeBotonSiguiente || botonSiguiente == null) return;
        if (intervaloShakeSiguiente <= 0f || duracionShakeSiguiente <= 0f) return;

        rectBotonSiguiente = botonSiguiente.transform as RectTransform;
        if (rectBotonSiguiente == null) return;

        // Posición de reposo actual: si se detiene antes de la primera sacudida, es a la que
        // hay que volver (sin esto se restauraría un (0,0) que nunca fue su sitio).
        posBaseBotonSiguiente = rectBotonSiguiente.anchoredPosition;

        shakeCoroutine = StartCoroutine(RecordatorioBotonSiguiente());
    }

    private void DetenerRecordatorioSiguiente()
    {
        if (shakeCoroutine != null) { StopCoroutine(shakeCoroutine); shakeCoroutine = null; }

        // Si se cortó a mitad de una sacudida, devolver el botón a su sitio.
        if (rectBotonSiguiente != null) rectBotonSiguiente.anchoredPosition = posBaseBotonSiguiente;
        rectBotonSiguiente = null;
    }

    /// <summary>
    /// Sacude el botón en horizontal cada 'intervaloShakeSiguiente' segundos mientras el paso
    /// siga sin resolverse. La amplitud se va apagando dentro de cada sacudida para que el
    /// botón termine quieto en su posición original.
    /// Usa tiempo sin escalar: el aviso debe funcionar aunque el juego esté congelado.
    /// </summary>
    private IEnumerator RecordatorioBotonSiguiente()
    {
        var rect = rectBotonSiguiente;

        while (true)
        {
            yield return new WaitForSecondsRealtime(intervaloShakeSiguiente);

            if (rect == null || !rect.gameObject.activeInHierarchy) continue;

            // Se relee en cada ciclo por si un layout group movió el botón entre sacudidas.
            posBaseBotonSiguiente = rect.anchoredPosition;

            float t = 0f;
            while (t < duracionShakeSiguiente)
            {
                t += Time.unscaledDeltaTime;
                float progreso = Mathf.Clamp01(t / duracionShakeSiguiente);
                float amortiguacion = 1f - progreso;
                float offset = Mathf.Sin(progreso * Mathf.PI * 2f * oscilacionesShakeSiguiente)
                               * amplitudShakeSiguiente * amortiguacion;

                rect.anchoredPosition = posBaseBotonSiguiente + new Vector2(offset, 0f);
                yield return null;
            }

            rect.anchoredPosition = posBaseBotonSiguiente;
        }
    }

    private IEnumerator EsperarDelay(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        MostrarPaso(pasoActual + 1);
    }

    // -----------------------------
    // Ayuda cuando el jugador se equivoca
    // -----------------------------

    /// <summary>
    /// Muestra el texto de ayuda del paso actual (si tiene) y vuelve al texto del paso pasados
    /// 'segundosAyuda'. NO avanza ni retrocede: el paso sigue esperando su condición.
    /// Si el paso no tiene ayuda configurada, no hace nada (comportamiento de antes).
    /// </summary>
    private void MostrarAyudaDelPaso(string motivoDebug)
    {
        if (pasoActual < 0 || pasoActual >= pasos.Count) return;

        var p = pasos[pasoActual];
        string ayuda = TextoAyudaDelPaso(p);
        if (string.IsNullOrEmpty(ayuda))
        {
            if (debug)
                Debug.Log($"[Tutorial] 🤔 Paso {pasoActual}: {motivoDebug}, pero el paso no tiene " +
                          $"'Texto Ayuda' → el jugador no recibe ninguna pista.");
            return;
        }

        if (debug) Debug.Log($"[Tutorial] 💡 Paso {pasoActual}: ayuda mostrada ({motivoDebug})");

        if (ayudaCoroutine != null) { StopCoroutine(ayudaCoroutine); ayudaCoroutine = null; }

        if (textoPaso != null) textoPaso.text = ayuda;

        // La ayuda SÍ corta lo que esté sonando: es una respuesta a algo que el niño acaba de
        // hacer mal, y llega tarde si espera turno.
        AudioClip clipAyuda = AudioAyudaDelPaso(p);
        if (clipAyuda != null && GlobalAudioManager.Instance != null)
        {
            if (narracionCoroutine != null) { StopCoroutine(narracionCoroutine); narracionCoroutine = null; }
            GlobalAudioManager.Instance.ReproducirVoz(clipAyuda, 1f);
            clipNarracionSonando = clipAyuda;
        }

        ayudaCoroutine = StartCoroutine(VolverAlTextoDelPaso(p));
    }

    private IEnumerator VolverAlTextoDelPaso(TutorialStep p)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, p.segundosAyuda));

        // Guarda: si el paso cambió mientras se veía la ayuda, no pisar el texto del paso nuevo.
        if (pasoActual >= 0 && pasoActual < pasos.Count && pasos[pasoActual] == p && textoPaso != null)
            textoPaso.text = TextoDelPaso(p);

        ayudaCoroutine = null;
    }

    // -----------------------------
    // Qué se puede tocar en el paso
    // -----------------------------

    /// <summary>
    /// Aplica la política de interacción del paso: con 'SoloElResaltado' deja pulsable únicamente
    /// el botón del paso (más los 'permitidosExtra') y pone en gris el resto de la lista
    /// gobernada. Se reaplica en bucle porque los botones del panel de ObjectInfo se
    /// reconstruyen cuando el jugador toca un objeto y perderían este estado.
    /// </summary>
    private void AplicarInteraccion(TutorialStep p)
    {
        if (elementosGobernados == null || elementosGobernados.Length == 0) return;

        if (p.modoInteraccion == ModoInteraccion.Todo)
        {
            RestaurarInteraccion();
            return;
        }

        Selectable objetivo = SelectableDelPaso(p);

        foreach (var s in elementosGobernados)
        {
            if (s == null) continue;
            s.interactable = EsPermitido(s, objetivo, p.permitidosExtra);
        }

        // El inventario no solo se abre con su botón: también deslizando, y ese gesto vive en el
        // Update de InventarioToggleController, así que el 'interactable' del botón no lo toca.
        // Se cierra la misma puerta para los dos.
        var inv = InventarioToggleController.Instance;
        if (inv != null)
        {
            bool inventarioPermitido = inv.BotonToggle != null &&
                                       EsPermitido(inv.BotonToggle, objetivo, p.permitidosExtra);
            inv.BloquearSwipe(!inventarioPermitido);
        }
    }

    private static bool EsPermitido(Selectable s, Selectable objetivo, Selectable[] extras)
    {
        if (objetivo != null && s == objetivo) return true;

        if (extras != null)
        {
            foreach (var e in extras)
                if (e != null && e == s) return true;
        }

        return false;
    }

    /// <summary>
    /// El elemento que este paso quiere dejar pulsable: el botón esperado, o el Button/Selectable
    /// del elemento resaltado (así basta con arrastrar el botón una vez, igual que en BotonPulsado).
    /// </summary>
    private static Selectable SelectableDelPaso(TutorialStep p)
    {
        if (p.botonEsperado != null) return p.botonEsperado;
        if (p.elementoAResaltar != null) return p.elementoAResaltar.GetComponent<Selectable>();
        return null;
    }

    private void RestaurarInteraccion()
    {
        // El gesto del inventario se devuelve SIEMPRE, haya o no lista gobernada: si el tutorial
        // acaba (o se salta) con el swipe bloqueado, el jugador se queda sin poder deslizar el
        // inventario en todo el juego.
        var inv = InventarioToggleController.Instance;
        if (inv != null) inv.BloquearSwipe(false);

        if (elementosGobernados == null) return;

        foreach (var s in elementosGobernados)
            if (s != null) s.interactable = true;
    }

    private IEnumerator ReaplicarInteraccion(TutorialStep p)
    {
        float espera = Mathf.Max(0.05f, intervaloReaplicarInteraccion);

        while (true)
        {
            yield return new WaitForSecondsRealtime(espera);
            AplicarInteraccion(p);
        }
    }

    /// <summary>
    /// Instala (o quita) el filtro de objeto AR del paso. El filtro es estático en InputRouter,
    /// así que hay que limpiarlo SIEMPRE al salir del paso o del tutorial: si no, el juego se
    /// quedaría sin poder tocar más que un objeto.
    /// </summary>
    private void AplicarFiltroAR(TutorialStep p)
    {
        if (p != null && !string.IsNullOrEmpty(p.objetoARPermitido))
        {
            string permitido = p.objetoARPermitido;
            InputRouter.FiltroObjetoPermitido = id => id == permitido;
            filtroARCoroutine = StartCoroutine(VigilarPanelNoPermitido(permitido));

            if (debug) Debug.Log($"[Tutorial] 🎯 Paso {pasoActual}: solo '{permitido}' responde al toque en AR");
        }
        else
        {
            InputRouter.FiltroObjetoPermitido = null;
        }
    }

    /// <summary>
    /// Cierra el panel de info si está mostrando un objeto que este paso no permite.
    /// Hace falta ADEMÁS del filtro porque el canvas de ObjectInfoUIManager es ESTÁTICO: no
    /// cuelga del objeto AR, así que no se cierra al perder el target. Si el jugador abrió el
    /// panel de la calabaza en un paso anterior (donde aún no había filtro), seguía en pantalla
    /// al llegar aquí y parecía que el filtro no funcionaba.
    /// </summary>
    private IEnumerator VigilarPanelNoPermitido(string permitido)
    {
        var espera = new WaitForSecondsRealtime(0.2f);

        // Un frame de margen: MostrarPaso escribe el texto del paso DESPUÉS de arrancar esta
        // corrutina, y si la ayuda saliera antes se la llevaría por delante.
        yield return null;

        while (true)
        {
            var ui = ObjectInfoUIManager.Instance;
            if (ui != null && ui.TieneCanvasActivo() &&
                !string.IsNullOrEmpty(ui.ObjetoActualID) && ui.ObjetoActualID != permitido)
            {
                if (debug)
                    Debug.Log($"[Tutorial] 🚪 Paso {pasoActual}: cerrando el panel de " +
                              $"'{ui.ObjetoActualID}', que este paso no permite (espera '{permitido}')");

                string sobra = ui.ObjetoActualID;
                ui.CerrarCanvas();
                MostrarAyudaDelPaso($"panel de '{sobra}' abierto y no permitido");
            }

            yield return espera;
        }
    }

    /// <summary>
    /// ¿El elemento está REALMENTE a la vista? No basta con activeInHierarchy: los paneles que
    /// se abren deslizándose (inventario, misiones, ajustes) nunca se desactivan — se quedan
    /// activos con el CanvasGroup a alpha 0 y fuera de pantalla. Si el resaltado se creara ahí,
    /// se dibujaría sobre un panel invisible.
    /// </summary>
    private static bool ElementoVisible(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return false;

        // Basta con que un CanvasGroup de la cadena lo tenga apagado.
        var grupos = go.GetComponentsInParent<CanvasGroup>(false);
        foreach (var g in grupos)
            if (g != null && g.alpha < 0.05f) return false;

        return true;
    }

    /// <summary>
    /// Mantiene el resaltado sincronizado con el elemento durante todo el paso:
    /// espera a que esté ACTIVO para mostrarlo (los paneles de AR aparecen al tocar un objeto)
    /// y lo oculta si el elemento vuelve a desaparecer.
    /// </summary>
    private IEnumerator GestionarResaltado(TutorialStep p)
    {
        bool mostrado = false;
        float esperando = 0f;
        bool avisado = false;

        while (true)
        {
            bool activo = ElementoVisible(p.elementoAResaltar);

            if (activo && !mostrado)
            {
                highlighter.ConfigurarElemento(p.elementoAResaltar);
                highlighter.CambiarTipoResaltado(p.tipoResaltado);
                highlighter.CambiarDireccionFlecha(p.direccionFlecha); // dirección POR PASO
                highlighter.MostrarResaltado();
                mostrado = true;

                if (debug) Debug.Log($"[Tutorial] 🔍 Resaltando '{p.elementoAResaltar.name}' " +
                                     $"({p.tipoResaltado}, flecha {p.direccionFlecha})");
            }
            else if (!activo && mostrado)
            {
                highlighter.OcultarResaltado();
                mostrado = false;
            }

            // Aviso si el elemento nunca llega a activarse (posible mala asignación)
            if (!activo && !avisado)
            {
                esperando += Time.deltaTime;
                if (esperando > 10f)
                {
                    avisado = true;
                    Debug.LogWarning($"[Tutorial] ⚠️ Paso {pasoActual}: '{(p.elementoAResaltar != null ? p.elementoAResaltar.name : "null")}' " +
                                     $"lleva 10s inactivo. El resaltado aparecerá en cuanto se active (¿es el objeto correcto?).");
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// Deja de escuchar al juego y apaga todo lo del paso en curso (resaltado, delay, shake,
    /// audio, botones). Común a completar y a saltar el tutorial.
    /// </summary>
    private void DetenerTutorial()
    {
        DesuscribirEventos();
        DesengancharBotonDelPaso();

        if (resaltadoCoroutine != null)   { StopCoroutine(resaltadoCoroutine);   resaltadoCoroutine = null; }
        if (delayCoroutine != null)       { StopCoroutine(delayCoroutine);       delayCoroutine = null; }
        if (interaccionCoroutine != null) { StopCoroutine(interaccionCoroutine); interaccionCoroutine = null; }
        if (filtroARCoroutine != null)    { StopCoroutine(filtroARCoroutine);    filtroARCoroutine = null; }
        if (ayudaCoroutine != null)       { StopCoroutine(ayudaCoroutine);       ayudaCoroutine = null; }
        if (highlighter != null) highlighter.OcultarResaltado();

        // El juego que viene detrás tiene que quedar con TODO tocable otra vez.
        RestaurarInteraccion();
        InputRouter.FiltroObjetoPermitido = null;

        DetenerRecordatorioSiguiente();
        if (botonSiguiente != null) botonSiguiente.gameObject.SetActive(false);

        if (narracionCoroutine != null) { StopCoroutine(narracionCoroutine); narracionCoroutine = null; }
        DetenerNarracionPropia();
        clipPasoActual = null;
        if (repetirAudioCoroutine != null) { StopCoroutine(repetirAudioCoroutine); repetirAudioCoroutine = null; }
        if (botonRepetirAudio != null) botonRepetirAudio.gameObject.SetActive(false);
        if (botonSaltarTutorial != null) botonSaltarTutorial.gameObject.SetActive(false);

        // Índice fuera de rango: ningún evento que llegue tarde puede avanzar un paso.
        pasoActual = pasos.Count;
    }

    /// <summary>
    /// Salta el tutorial: se marca como completado (para que el menú no vuelva a mandar aquí)
    /// y se carga el juego directamente, sin pantalla final. Se puede volver a hacer desde
    /// ajustes con SettingsPanelManager.RepetirTutorial().
    /// </summary>
    public void SaltarTutorial()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();

        // Sin botón de cancelar no hay ventana posible: se salta directo (y se avisa, porque
        // normalmente es que falta asignarlo en el Inspector).
        if (botonCancelarSalto == null || panelFinal == null)
        {
            Debug.LogWarning("[Tutorial] ⚠️ Saltar sin confirmación: falta 'Boton Cancelar Salto' " +
                             "o 'Panel Final' en el Inspector.");
            SaltarTutorialConfirmado();
            return;
        }

        PedirConfirmacionSalto();
    }

    /// <summary>
    /// Reutiliza el panel final como ventana de confirmación: mismo panel, mismo botón de
    /// "ir al juego" (que aquí confirma) y el botón de cancelar al lado. El tutorial de atrás
    /// queda congelado mientras se decide (ver la guarda de 'confirmandoSalto' en TryAvanzar).
    /// </summary>
    private void PedirConfirmacionSalto()
    {
        confirmandoSalto = true;

        if (debug) Debug.Log("[Tutorial] ❓ Pidiendo confirmación para saltar el tutorial");

        panelFinal.SetActive(true);

        if (textoFinal != null)
            textoFinal.text = LanguageManager.T(claveMensajeConfirmarSalto);

        if (textoBotonIrAlJuego != null)
            textoBotonIrAlJuego.text = LanguageManager.T(claveBotonConfirmarSalto);

        if (botonIrAlJuego != null)
        {
            botonIrAlJuego.onClick.RemoveAllListeners();
            botonIrAlJuego.onClick.AddListener(SaltarTutorialConfirmado);
            botonIrAlJuego.gameObject.SetActive(true);
        }

        botonCancelarSalto.onClick.RemoveAllListeners();
        botonCancelarSalto.onClick.AddListener(CancelarSalto);
        botonCancelarSalto.gameObject.SetActive(true);
    }

    /// <summary>Vuelve al tutorial donde estaba, deshaciendo la ventana de confirmación.</summary>
    public void CancelarSalto()
    {
        if (debug) Debug.Log("[Tutorial] ↩ Salto cancelado: sigue el tutorial");

        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();

        confirmandoSalto = false;

        botonCancelarSalto.gameObject.SetActive(false);

        if (botonIrAlJuego != null)
        {
            botonIrAlJuego.onClick.RemoveAllListeners();
            botonIrAlJuego.onClick.AddListener(IrAlJuego);
            botonIrAlJuego.gameObject.SetActive(false);
        }

        if (textoBotonIrAlJuego != null)
            textoBotonIrAlJuego.text = LanguageManager.T(claveBotonIrAlJuego);

        if (panelFinal != null) panelFinal.SetActive(false);

        // Repintar el paso donde estaba: el texto pudo quedar en el mensaje de confirmación.
        if (pasoActual >= 0 && pasoActual < pasos.Count && textoPaso != null)
            textoPaso.text = TextoDelPaso(pasos[pasoActual]);
    }

    /// <summary>Confirmado: marca el tutorial como visto y se va al juego.</summary>
    public void SaltarTutorialConfirmado()
    {
        if (debug) Debug.Log("[Tutorial] ⏭ Tutorial saltado por el jugador");

        confirmandoSalto = false;

        TutorialProgreso.MarcarCompletado();
        DetenerTutorial();

        if (botonCancelarSalto != null) botonCancelarSalto.gameObject.SetActive(false);
        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (panelFinal != null) panelFinal.SetActive(false);

        IrAlJuego();
    }

    private void Completar()
    {
        if (debug) Debug.Log("[Tutorial] 🏁 Tutorial completado — mostrando pantalla final");

        TutorialProgreso.MarcarCompletado();
        DetenerTutorial();

        // El panel final se comparte con la confirmación de salto: devolverlo a su papel normal.
        confirmandoSalto = false;
        if (botonCancelarSalto != null) botonCancelarSalto.gameObject.SetActive(false);
        if (textoBotonIrAlJuego != null) textoBotonIrAlJuego.text = LanguageManager.T(claveBotonIrAlJuego);
        if (botonIrAlJuego != null)
        {
            botonIrAlJuego.onClick.RemoveAllListeners();
            botonIrAlJuego.onClick.AddListener(IrAlJuego);
        }

        // Mostrar pantalla final con botón "Ir al Juego"
        if (panelFinal != null)
            panelFinal.SetActive(true);

        if (textoFinal != null)
            textoFinal.text = LanguageManager.T("tutorial.final");
        else if (textoPaso != null)
            textoPaso.text = LanguageManager.T("tutorial.final");

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
        StartCoroutine(TransicionAlJuego());
    }

    /// <summary>
    /// Carga la escena del juego SIN tocar el ciclo de vida de Vuforia manualmente
    /// y SIN limpiar objetos DontDestroyOnLoad. Descubrimos que el "residuo DDOL"
    /// que destruíamos (GameObject genérico "New Game Object") es en realidad un
    /// objeto interno de Vuforia: al destruirlo, Vuforia se auto-deinicializaba
    /// como reacción, causando el mismo problema (pantalla negra) que intentábamos
    /// evitar. Unity ya destruye la escena vieja (y su VuforiaBehaviour) al cargar
    /// la nueva de forma normal — igual que en el arranque en frío de la app, que
    /// sí funciona — así que no interferimos con nada del ciclo de vida de Vuforia.
    /// </summary>
    private IEnumerator TransicionAlJuego()
    {
        const string TAG = "[VUFORIA_DIAG]";
        Debug.Log($"{TAG} (Tutorial) Iniciando transición sin tocar Vuforia manualmente.");

        yield return null;

        Debug.Log($"{TAG} (Tutorial) ▶ Cargando escena '{nombreEscenaPrincipal}'...");
        if (!string.IsNullOrEmpty(nombreEscenaPrincipal))
            SceneManager.LoadScene(nombreEscenaPrincipal);
    }

    // -----------------------------
    // API pública
    // -----------------------------
    // El idioma nativo manda en el texto y la narración; {0} es el nombre del idioma meta, para
    // frases como "¿Oíste cómo se dice en {0}?".
    private static string TextoDelPaso(TutorialStep p)
    {
        if (p.texto == null || p.texto.IsEmpty) return "";

        var lm = LanguageManager.Instance;
        string nombreMeta = lm != null ? lm.TextoNativo($"idiomas.nombre_{lm.CodigoMeta}") : "";
        return p.texto.GetLocalizedString(nombreMeta);
    }

    private static AudioClip AudioDelPaso(TutorialStep p) => CargarAudio(p.audio, "narración del paso");

    private static string TextoAyudaDelPaso(TutorialStep p)
    {
        if (p.textoAyuda == null || p.textoAyuda.IsEmpty) return "";

        var lm = LanguageManager.Instance;
        string nombreMeta = lm != null ? lm.TextoNativo($"idiomas.nombre_{lm.CodigoMeta}") : "";
        return p.textoAyuda.GetLocalizedString(nombreMeta);
    }

    private static AudioClip AudioAyudaDelPaso(TutorialStep p) => CargarAudio(p.audioAyuda, "audio de ayuda");

    /// <summary>
    /// Carga un clip de las tablas sin tumbar el paso si la entrada está rota.
    /// Que la entrada NO esté vacía no garantiza que cargue: si el asset no está marcado como
    /// Addressable, LoadAsset() lanza InvalidKeyException. Un paso sin audio se puede leer; un
    /// paso que revienta a mitad deja el tutorial atascado.
    /// </summary>
    private static AudioClip CargarAudio(LocalizedAudioClip clip, string queEs)
    {
        if (clip == null || clip.IsEmpty) return null;

        try
        {
            return clip.LoadAsset();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Tutorial] ⚠️ No se pudo cargar el {queEs}. ¿El clip está marcado " +
                             $"como Addressable? El paso sigue, en silencio. ({e.GetType().Name})");
            return null;
        }
    }

    [ContextMenu("🧹 Resetear SOLO objetos/misión del tutorial")]
    public void TestResetearProgresoTutorial() => ResetearProgresoTutorial();

    [ContextMenu("🔄 Resetear tutorial (borra progreso)")]
    public void ResetearProgreso()
    {
        TutorialProgreso.Borrar();
        Debug.Log("[Tutorial] Progreso borrado");
    }
}
