using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🧭 Recordatorios de Éliane: avisos cortos para que el jugador no olvide Completar las misiones
/// que ya descifró (en las pruebas, muchos creían que el loop terminaba en Descifrar).
///
/// El aviso nombra el PASO que toca en la misión pendiente más antigua:
///   - Buscar: el objeto que falta por llevar (no el destino: ir al destino con las manos vacías confunde).
///   - Llevar: si el jugador lleva mucho rato con un objeto agarrado que hace falta, "Lleva X a: Y".
///   - Confirmar: todo está colocado en el destino pero falta terminar la misión.
///
/// Texto en idioma NATIVO con el nombre del objeto en idioma META y sin artículo (un artículo nativo
/// delante de un sustantivo meta no casa en otro idioma). Voz: frase de Éliane (nativo, tabla
/// StoryAudio) y después el nombre del objeto en meta (tabla ObjectAudio). Sin audios, solo texto.
///
/// Ritmo prudente: espera tras descifrar, intervalo que crece si se repite lo mismo, y nunca
/// mientras hay narración, animación, la voz ocupada o un panel abierto.
///
/// Controla la UI por REFERENCIAS (CanvasGroup), así que vive en el GameController. Solo en la
/// escena del cuento (no en el tutorial).
/// </summary>
public class RecordatoriosEliane : MonoBehaviour
{
    private enum TipoAviso { Buscar, Llevar, Confirmar }

    [Header("UI del aviso")]
    [SerializeField] private CanvasGroup grupoAviso;
    [Tooltip("Opcional: para el pop al aparecer.")]
    [SerializeField] private RectTransform rectAviso;
    [Tooltip("Retrato de Éliane (opcional hasta tener la ilustración).")]
    [SerializeField] private Image retrato;
    [SerializeField] private TextMeshProUGUI texto;
    [Tooltip("Icono del objeto que nombra el aviso.")]
    [SerializeField] private Image iconoObjeto;
    [Tooltip("Opcional: flecha entre objeto y destino (solo en el aviso de llevar).")]
    [SerializeField] private GameObject flecha;
    [Tooltip("Opcional: icono del destino (solo en los avisos de llevar y confirmar).")]
    [SerializeField] private Image iconoDestino;
    [Tooltip("Opcional: botón que cubre el aviso para cerrarlo con un toque.")]
    [SerializeField] private Button botonCerrar;

    [Header("Ritmo (segundos)")]
    [Tooltip("Espera desde que se descifra una misión hasta poder recordarla.")]
    [SerializeField] private float esperaTrasDescifrar = 45f;
    [Tooltip("Tiempo mínimo entre dos avisos.")]
    [SerializeField] private float intervaloBase = 90f;
    [Tooltip("Cada vez que se repite el mismo aviso, el intervalo se multiplica por esto.")]
    [SerializeField] private float factorRepeticion = 1.5f;
    [SerializeField] private float intervaloMaximo = 240f;
    [Tooltip("Con un objeto agarrado no se avisa de nada hasta pasado este tiempo (agarrar ya indica que sabe adónde va).")]
    [SerializeField] private float esperaConObjetoAgarrado = 180f;
    [Tooltip("Margen de calma tras una narración, animación o panel antes de poder avisar.")]
    [SerializeField] private float margenTrasBloqueo = 8f;
    [Tooltip("Cuánto se ve el aviso (si la voz dura más, espera a que termine).")]
    [SerializeField] private float duracionAviso = 5f;

    [Header("Avisos")]
    [Tooltip("Avisar cuando todo está colocado en el destino pero falta terminar la misión.")]
    [SerializeField] private bool avisarColocadoSinTerminar = true;
    [Tooltip("Paneles que bloquean los avisos mientras están abiertos (álbum, ajustes…). " +
             "Deben implementar IPanelConVisibilidad.")]
    [SerializeField] private MonoBehaviour[] panelesQueBloquean;
    [Tooltip("Opcional; si se deja vacío, se busca en la escena.")]
    [SerializeField] private PanelFinJuego panelFinJuego;

    [Header("Depuración")]
    [Tooltip("Escribe en consola, cada pocos segundos, por qué no se está mostrando el aviso.")]
    [SerializeField] private bool depurar = false;

    [Header("Pop")]
    [SerializeField] private float duracionPop = 0.3f;
    [SerializeField] private float escalaInicial = 0.8f;

    // Claves de texto (tabla UI) y de voz (tabla StoryAudio). Mismo nombre en las dos tablas.
    private static readonly string[] ClavesBuscar = { "eliane.reminder.find.1", "eliane.reminder.find.2", "eliane.reminder.find.3" };
    private static readonly string[] ClavesLlevar = { "eliane.reminder.carry.1", "eliane.reminder.carry.2" };
    private static readonly string[] ClavesConfirmar = { "eliane.reminder.finish.1" };

    private MissionManager missionManager;
    private ObjectInfoGrabDropController grabDrop;

    private readonly Dictionary<string, float> momentoDescifrada = new Dictionary<string, float>();
    private float proximoAvisoPermitido;
    private float ultimoBloqueo;
    private string ultimaClaveAviso;
    private int repeticionesSeguidas;
    private float momentoAgarre = -1f;
    private int indiceFrase;
    private bool avisoVisible;
    private Coroutine avisoCR;
    private Vector3 escalaBase = Vector3.one;

    void Start()
    {
        grabDrop = FindAnyObjectByType<ObjectInfoGrabDropController>();
        if (panelFinJuego == null) panelFinJuego = FindAnyObjectByType<PanelFinJuego>();
        if (rectAviso != null) escalaBase = rectAviso.localScale;

        ConectarMissionManager();
        ObjectInfoGrabDropController.OnObjetoAgarrado += AlAgarrar;

        if (botonCerrar != null)
        {
            botonCerrar.onClick.RemoveAllListeners();
            botonCerrar.onClick.AddListener(CerrarAviso);
        }

        MostrarGrupo(false);
        proximoAvisoPermitido = Time.time + intervaloBase * 0.5f;
        StartCoroutine(Vigilar());
    }

    void OnDestroy()
    {
        if (missionManager != null)
        {
            missionManager.OnMisionDescifrada -= AlDescifrar;
            missionManager.OnMisionCompletada -= AlCompletar;
        }
        ObjectInfoGrabDropController.OnObjetoAgarrado -= AlAgarrar;
    }

    private void ConectarMissionManager()
    {
        if (missionManager != null || MissionManager.Instance == null) return;

        missionManager = MissionManager.Instance;
        missionManager.OnMisionDescifrada += AlDescifrar;
        missionManager.OnMisionCompletada += AlCompletar;
    }

    /// <summary>
    /// Registra las misiones descifradas sin completar que aún no se conocían. Cubre las que vienen
    /// del progreso guardado, que puede cargarse después del Start de este componente: cuentan
    /// desde que se ven por primera vez.
    /// </summary>
    private void SincronizarPendientes()
    {
        if (missionManager == null) return;

        foreach (var id in missionManager.MisionesDescifradas)
            if (!missionManager.MisionesCompletadas.Contains(id) && !momentoDescifrada.ContainsKey(id))
                momentoDescifrada[id] = Time.time;
    }

    private void AlDescifrar(string misionID) => momentoDescifrada[misionID] = Time.time;

    private void AlCompletar(string misionID)
    {
        momentoDescifrada.Remove(misionID);

        // El jugador avanzó: el ritmo vuelve a empezar.
        repeticionesSeguidas = 0;
        ultimaClaveAviso = null;
        proximoAvisoPermitido = Mathf.Max(proximoAvisoPermitido, Time.time + intervaloBase * 0.5f);
    }

    private void AlAgarrar(string objetoID) => momentoAgarre = Time.time;

    // ========== Cuándo avisar ==========

    private IEnumerator Vigilar()
    {
        var espera = new WaitForSeconds(1f);
        float proximoLogDepuracion = 0f;

        while (true)
        {
            yield return espera;

            ConectarMissionManager();
            SincronizarPendientes();

            string motivo = MotivoParaNoAvisar();
            if (motivo == null && ElegirAviso(out TipoAviso tipo, out string objetoID, out string destinoID))
            {
                MostrarAviso(tipo, objetoID, destinoID);
                continue;
            }

            if (depurar && Time.time >= proximoLogDepuracion)
            {
                proximoLogDepuracion = Time.time + 5f;
                Debug.Log($"[RecordatoriosEliane] 🔍 Sin aviso: {motivo ?? MotivoSinPendientes()}");
            }
        }
    }

    // Null si se puede avisar; si no, el motivo (para el modo depuración).
    private string MotivoParaNoAvisar()
    {
        string bloqueo = Bloqueo();
        if (bloqueo != null)
        {
            ultimoBloqueo = Time.time;
            return $"bloqueado por {bloqueo}";
        }

        if (avisoVisible) return "ya hay un aviso a la vista";
        if (Time.time < proximoAvisoPermitido) return $"intervalo: faltan {proximoAvisoPermitido - Time.time:F0}s";
        if (Time.time - ultimoBloqueo < margenTrasBloqueo) return "margen de calma tras un bloqueo";
        return null;
    }

    private string MotivoSinPendientes()
    {
        if (missionManager == null) return "no hay MissionManager en la escena";
        if (momentoDescifrada.Count == 0) return "no hay misiones descifradas sin completar";

        float menor = momentoDescifrada.Values.Select(t => esperaTrasDescifrar - (Time.time - t)).Min();
        if (menor > 0f) return $"{momentoDescifrada.Count} pendientes, la primera se puede recordar en {menor:F0}s";
        if (grabDrop != null && grabDrop.TieneObjetoAgarrado()) return "objeto agarrado hace poco";
        return $"{momentoDescifrada.Count} pendientes, pero ninguna tiene objetos que llevar";
    }

    private string Bloqueo()
    {
        if (StoryManager.Instance != null && StoryManager.Instance.EstaReproduciendo) return "narración";
        if (AnimacionesMisionAR.Instance != null && AnimacionesMisionAR.Instance.HayAnimacionEnCurso) return "animación de misión";
        if (GlobalAudioManager.Instance != null && GlobalAudioManager.Instance.VozOcupada && !avisoVisible) return "voz sonando";
        if (panelFinJuego != null && panelFinJuego.EstaVisible) return "panel de fin de juego";
        if (ObjectInfoUIManager.Instance != null && ObjectInfoUIManager.Instance.CanvasActivo) return "panel de info AR abierto";

        if (panelesQueBloquean != null)
            foreach (var p in panelesQueBloquean)
                if (p is IPanelConVisibilidad panel && panel.EstaPanelVisible()) return p.GetType().Name;

        return null;
    }

    /// <summary>
    /// Elige qué decir: el paso pendiente de la misión descifrada más antigua que ya pasó su espera.
    /// </summary>
    private bool ElegirAviso(out TipoAviso tipo, out string objetoID, out string destinoID)
    {
        tipo = TipoAviso.Buscar; objetoID = null; destinoID = null;
        if (missionManager == null) return false;

        string agarrado = grabDrop != null && grabDrop.TieneObjetoAgarrado() ? grabDrop.ObjetoAgarradoID : null;
        bool agarradoHacePoco = agarrado != null && momentoAgarre >= 0f && Time.time - momentoAgarre < esperaConObjetoAgarrado;

        // Con un objeto en la mano desde hace poco, el jugador está en plena acción: no molestar.
        if (agarradoHacePoco) return false;

        var pendientes = missionManager.misiones
            .Where(m => m != null && momentoDescifrada.ContainsKey(m.misionID) &&
                        !missionManager.MisionesCompletadas.Contains(m.misionID) &&
                        Time.time - momentoDescifrada[m.misionID] >= esperaTrasDescifrar &&
                        m.idsObjetosCorrectos != null && m.idsObjetosCorrectos.Count > 0)
            .OrderBy(m => momentoDescifrada[m.misionID])
            .ToList();

        if (pendientes.Count == 0) return false;

        // 1) Lleva mucho rato con un objeto agarrado que una misión pendiente necesita.
        if (agarrado != null)
        {
            foreach (var m in pendientes)
            {
                if (!m.idsObjetosCorrectos.Contains(agarrado)) continue;
                if (Colocados(m.idObjetoDestino).Contains(agarrado)) continue;

                tipo = TipoAviso.Llevar; objetoID = agarrado; destinoID = m.idObjetoDestino;
                return true;
            }
        }

        // 2) La misión más antigua: buscar lo que falta, o terminarla si ya está todo colocado.
        var mision = pendientes[0];
        var colocados = Colocados(mision.idObjetoDestino);
        string falta = mision.idsObjetosCorrectos.FirstOrDefault(o => !colocados.Contains(o));

        if (falta != null)
        {
            tipo = TipoAviso.Buscar; objetoID = falta; destinoID = mision.idObjetoDestino;
            return true;
        }

        if (!avisarColocadoSinTerminar) return false;
        tipo = TipoAviso.Confirmar; objetoID = mision.idObjetoDestino; destinoID = mision.idObjetoDestino;
        return true;
    }

    private static List<string> Colocados(string destinoID)
    {
        var ui = ObjectInfoUIManager.Instance;
        return ui != null ? ui.ObtenerObjetosColocados(destinoID) ?? new List<string>() : new List<string>();
    }

    // ========== Mostrar ==========

    private void MostrarAviso(TipoAviso tipo, string objetoID, string destinoID)
    {
        // Ritmo: si se repite el mismo aviso, cada vez se espera más.
        string clave = $"{tipo}:{objetoID}";
        repeticionesSeguidas = clave == ultimaClaveAviso ? repeticionesSeguidas + 1 : 0;
        ultimaClaveAviso = clave;
        float intervalo = Mathf.Min(intervaloBase * Mathf.Pow(factorRepeticion, repeticionesSeguidas), intervaloMaximo);
        proximoAvisoPermitido = Time.time + intervalo;

        string[] claves = tipo == TipoAviso.Buscar ? ClavesBuscar : tipo == TipoAviso.Llevar ? ClavesLlevar : ClavesConfirmar;
        string claveFrase = claves[indiceFrase++ % claves.Length];

        string nombreObjeto = NombreMeta(objetoID);
        string nombreDestino = NombreMeta(destinoID);

        // {0} = objeto, {1} = destino. En "Confirmar" el objeto ES el destino.
        if (texto != null)
            texto.text = LanguageManager.T(claveFrase, $"<b>{nombreObjeto}</b>", $"<b>{nombreDestino}</b>");

        PonerIcono(iconoObjeto, objetoID, true);
        bool conDestino = tipo == TipoAviso.Llevar;
        if (flecha != null) flecha.SetActive(conDestino);
        PonerIcono(iconoDestino, destinoID, conDestino);

        // Voz: frase de Éliane y después la palabra en idioma meta (la que el aviso nombra al final).
        string idPalabra = tipo == TipoAviso.Buscar ? objetoID : destinoID;

        if (avisoCR != null) StopCoroutine(avisoCR);
        avisoCR = StartCoroutine(AvisoCoroutine(claveFrase, idPalabra));

        Debug.Log($"[RecordatoriosEliane] 🧭 {tipo}: objeto={objetoID} destino={destinoID} " +
                  $"(próximo aviso en {intervalo:F0}s)");
    }

    private IEnumerator AvisoCoroutine(string claveFrase, string idPalabra)
    {
        avisoVisible = true;
        MostrarGrupo(true);
        if (rectAviso != null) StartCoroutine(Pop());

        var audio = GlobalAudioManager.Instance;
        var lm = LanguageManager.Instance;
        float inicio = Time.time;

        if (audio != null && lm != null)
        {
            AudioClip frase = CargarAudio(() => lm.AudioHistoriaNativo(claveFrase));
            AudioClip palabra = CargarAudio(() => lm.AudioNombreObjetoMeta(idPalabra));

            if (frase != null)
            {
                audio.ReproducirVoz(frase);
                yield return null;
                while (avisoVisible && audio.VozOcupada) yield return null;
            }
            if (avisoVisible && palabra != null)
            {
                yield return new WaitForSeconds(0.15f);
                audio.ReproducirVoz(palabra);
                yield return null;
                while (avisoVisible && audio.VozOcupada) yield return null;
            }
        }
        else if (audio != null)
        {
            audio.ReproducirSonidoNotificacion();
        }

        while (avisoVisible && Time.time - inicio < duracionAviso) yield return null;

        avisoCR = null;
        CerrarAviso();
    }

    public void CerrarAviso()
    {
        if (avisoCR != null) { StopCoroutine(avisoCR); avisoCR = null; }
        avisoVisible = false;
        MostrarGrupo(false);
    }

    // Sin la entrada de audio todavía (los audios se cargan después), seguir solo con texto.
    private static AudioClip CargarAudio(System.Func<AudioClip> cargar)
    {
        try { return cargar(); }
        catch (System.Exception) { return null; }
    }

    private static string NombreMeta(string objetoID) =>
        LanguageManager.Instance != null ? LanguageManager.Instance.NombreObjetoMeta(objetoID) : objetoID;

    private static void PonerIcono(Image imagen, string objetoID, bool mostrar)
    {
        if (imagen == null) return;

        Sprite sprite = null;
        if (mostrar && GameObjectManager.Instance != null)
            sprite = GameObjectManager.Instance.BuscarObjetoPorId(objetoID)?.Sprite2D;

        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        imagen.gameObject.SetActive(mostrar && sprite != null);
    }

    private void MostrarGrupo(bool mostrar)
    {
        if (grupoAviso == null) return;
        grupoAviso.alpha = mostrar ? 1f : 0f;
        grupoAviso.interactable = mostrar;
        grupoAviso.blocksRaycasts = mostrar;
        if (!mostrar && rectAviso != null) rectAviso.localScale = escalaBase;
    }

    private IEnumerator Pop()
    {
        float t = 0f;
        while (t < duracionPop)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracionPop);
            // Crece hasta su tamaño con un pequeño rebote a mitad de camino.
            float s = Mathf.Lerp(escalaInicial, 1f, k) + 0.08f * Mathf.Sin(k * Mathf.PI);
            rectAviso.localScale = escalaBase * s;
            yield return null;
        }
        rectAviso.localScale = escalaBase;
    }
}
