using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hace que uno o VARIOS botones den un brinquito cada cierto tiempo para llamar la atención del
/// jugador (el que abre la lista de misiones, el del inventario…).
///
/// Vive en el GameController: los botones se asignan por referencia, no hace falta poner el
/// script en cada uno. Cada objetivo tiene su propio ritmo y su propio movimiento, porque no
/// todos piden lo mismo: el de misiones brinca y el del menú hace un vaivén.
///
/// Cada objetivo se calla solo mientras su panel está abierto, mientras el botón está apagado
/// (p. ej. cuando el tutorial lo deshabilita) y al tocarlo reinicia su cuenta atrás: si el
/// jugador ya lo vio, no hay que insistir.
/// </summary>
public class LlamadaAtencionBoton : MonoBehaviour
{
    public enum Tipo { BrincoVertical, ShakeHorizontal }

    [Serializable]
    public class Objetivo
    {
        [Tooltip("Solo para reconocerlo en esta lista. No afecta a nada.")]
        public string nombre = "Botón";

        [Header("Objetivo")]
        [Tooltip("RectTransform del botón que debe moverse.")]
        public RectTransform objetivo;
        [Tooltip("Opcional: mientras este panel esté abierto, el botón no se mueve. Vale cualquier " +
                 "panel que implemente IPanelConVisibilidad (lista de misiones, inventario, ajustes).")]
        public MonoBehaviour panelAsociado;

        [Header("Ritmo")]
        [Tooltip("Segundos entre repeticiones del aviso.")]
        public float intervalo = 8f;
        [Tooltip("Espera antes del primer aviso. Dale un valor distinto a cada objetivo para que " +
                 "no se muevan todos a la vez, que queda nervioso.")]
        public float esperaInicial = 4f;

        [Header("Movimiento")]
        public Tipo tipo = Tipo.BrincoVertical;
        public int repeticiones = 2;
        [Tooltip("Altura del brinco, o amplitud del vaivén, en píxeles.")]
        public float amplitud = 18f;
        public float duracion = 0.32f;
        [Tooltip("Respiro entre una repetición y la siguiente. Sin él, el segundo brinco arranca " +
                 "en el mismo frame en que acaba el primero y se ve como un tirón, no como dos " +
                 "brincos. Con una sola repetición da igual.")]
        public float pausaEntreRepeticiones = 0.12f;

        [Header("Solo brinco")]
        [Tooltip("0 = abajo, 1 = arriba. La curva por defecto sube y baja una vez.")]
        public AnimationCurve curva = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        [Header("Solo vaivén")]
        [Tooltip("Vaivenes completos por repetición.")]
        public float oscilaciones = 3f;

        // Estado en runtime
        [NonSerialized] public Vector2 posicionBase;
        [NonSerialized] public Coroutine ciclo;
        [NonSerialized] public IPanelConVisibilidad panel;
        [NonSerialized] public Button boton;
        [NonSerialized] public UnityEngine.Events.UnityAction alPulsar;
    }

    [Tooltip("Botones a los que se les llama la atención. Cada uno con su ritmo y su movimiento.")]
    [SerializeField] private List<Objetivo> objetivos = new List<Objetivo>();

    // --- Campos de la versión de un solo botón ---
    // Se conservan OCULTOS para no perder lo que ya está configurado en las escenas: si la lista
    // viene vacía y estos tienen algo, se migra solo al arrancar (ver MigrarConfiguracionAntigua).
    [HideInInspector, SerializeField] private RectTransform objetivo;
    [HideInInspector, SerializeField] private MonoBehaviour panelAsociado;
    [HideInInspector, SerializeField] private float intervalo = 8f;
    [HideInInspector, SerializeField] private float esperaInicial = 4f;
    [HideInInspector, SerializeField] private Tipo tipo = Tipo.BrincoVertical;
    [HideInInspector, SerializeField] private int repeticiones = 2;
    [HideInInspector, SerializeField] private float amplitud = 18f;
    [HideInInspector, SerializeField] private float duracion = 0.32f;
    [HideInInspector, SerializeField] private AnimationCurve curva;
    [HideInInspector, SerializeField] private float oscilaciones = 3f;

    void Start()
    {
        MigrarConfiguracionAntigua();

        if (objetivos.Count == 0)
        {
            enabled = false;
            return;
        }

        foreach (var o in objetivos)
            Preparar(o);
    }

    /// <summary>
    /// Convierte la configuración de la versión de un solo botón en la primera entrada de la
    /// lista. Así las escenas que ya tenían el script puesto siguen funcionando sin tocarlas.
    /// Para quitarlo de en medio: cuando abras la escena, vuelve a guardarla y estos campos
    /// quedan vacíos para siempre.
    /// </summary>
    private void MigrarConfiguracionAntigua()
    {
        if (objetivos.Count > 0 || objetivo == null) return;

        objetivos.Add(new Objetivo
        {
            nombre        = objetivo.name,
            objetivo      = objetivo,
            panelAsociado = panelAsociado,
            intervalo     = intervalo,
            esperaInicial = esperaInicial,
            tipo          = tipo,
            repeticiones  = repeticiones,
            amplitud      = amplitud,
            duracion      = duracion,
            curva         = curva ?? new AnimationCurve(
                                new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)),
            oscilaciones  = oscilaciones
        });

        Debug.Log($"[LlamadaAtencion] Configuración antigua migrada a la lista ('{objetivo.name}'). " +
                  $"Guarda la escena para dejarlo fijado.");
    }

    /// <summary>
    /// Rellena los valores que se quedaron a cero. Hace falta porque al añadir un elemento a una
    /// lista desde el Inspector, Unity lo crea A CEROS: NO aplica los valores por defecto que
    /// tiene la clase en el código. Con duración o repeticiones a 0 el botón no se mueve nada, y
    /// desde el Inspector no hay ninguna pista de por qué.
    /// </summary>
    private static void SanearValores(Objetivo o)
    {
        var corregido = new List<string>();

        if (o.duracion     <= 0f) { o.duracion     = 0.4f; corregido.Add("duración 0.4"); }
        if (o.repeticiones <= 0)  { o.repeticiones = 2;    corregido.Add("repeticiones 2"); }
        if (o.amplitud     <= 0f) { o.amplitud     = 25f;  corregido.Add("amplitud 25"); }
        if (o.intervalo    <= 0f) { o.intervalo    = 8f;   corregido.Add("intervalo 8"); }
        if (o.esperaInicial < 0f) { o.esperaInicial = 4f;  corregido.Add("espera inicial 4"); }

        if (o.tipo == Tipo.ShakeHorizontal && o.oscilaciones <= 0f)
        {
            o.oscilaciones = 3f;
            corregido.Add("oscilaciones 3");
        }

        if (o.tipo == Tipo.BrincoVertical)
        {
            // Una curva vacía evalúa siempre 0: el brinco no tendría altura ninguna.
            if (o.curva == null || o.curva.length == 0)
            {
                o.curva = CurvaBrincoPorDefecto();
                corregido.Add("curva sube-y-baja");
            }
            // Y una que no termina en 0 deja el botón arriba: al acabar la repetición se le
            // devuelve a su sitio de golpe y parece que la animación se corta y reinicia.
            else if (Mathf.Abs(o.curva.Evaluate(1f)) > 0.01f)
            {
                Debug.LogWarning($"[LlamadaAtencion] '{o.nombre}': la curva del brinco NO acaba en 0 " +
                                 $"(acaba en {o.curva.Evaluate(1f):0.00}). El botón se queda arriba y " +
                                 $"vuelve de un salto. Añade un punto final (1, 0).");
            }
        }

        if (corregido.Count > 0)
        {
            Debug.LogWarning($"[LlamadaAtencion] '{o.nombre}' tenía valores a cero y se han puesto por " +
                             $"defecto ({string.Join(", ", corregido)}). Ponlos a mano en el Inspector " +
                             $"para que quede guardado en la escena.");
        }
    }

    /// <summary>
    /// Brinco con aire de salto real: sube rápido, se frena arriba, cae y rebota una vez más
    /// bajito antes de quedarse quieto. Empieza y acaba en 0, que es lo que evita el tirón.
    /// </summary>
    private static AnimationCurve CurvaBrincoPorDefecto()
    {
        var c = new AnimationCurve(
            new Keyframe(0f,    0f),
            new Keyframe(0.32f, 1f),      // cima del salto grande
            new Keyframe(0.60f, 0f),      // aterriza
            new Keyframe(0.78f, 0.28f),   // rebote pequeño
            new Keyframe(1f,    0f));     // quieto

        for (int i = 0; i < c.length; i++)
            c.SmoothTangents(i, 0f);

        return c;
    }

    private void Preparar(Objetivo o)
    {
        if (o.objetivo == null)
        {
            Debug.LogWarning($"[LlamadaAtencion] '{o.nombre}' no tiene objetivo asignado → se ignora.");
            return;
        }

        SanearValores(o);

        o.posicionBase = o.objetivo.anchoredPosition;
        o.panel = o.panelAsociado as IPanelConVisibilidad;

        if (o.panelAsociado != null && o.panel == null)
        {
            Debug.LogWarning($"[LlamadaAtencion] '{o.nombre}': el panel asignado " +
                             $"({o.panelAsociado.GetType().Name}) no implementa IPanelConVisibilidad → " +
                             $"el botón se moverá también con el panel abierto.");
        }

        o.boton = o.objetivo.GetComponent<Button>();
        // Tocarlo reinicia SU cuenta atrás, no la de los demás. Se guarda la acción para poder
        // quitarla en OnDestroy (con una lambda suelta no habría forma de desengancharla).
        if (o.boton != null)
        {
            o.alPulsar = () => Reiniciar(o);
            o.boton.onClick.AddListener(o.alPulsar);
        }

        o.ciclo = StartCoroutine(Ciclo(o));
    }

    /// <summary>Reinicia la cuenta atrás de todos los objetivos.</summary>
    public void Reiniciar()
    {
        foreach (var o in objetivos) Reiniciar(o);
    }

    /// <summary>Reinicia la cuenta atrás del objetivo que usa este RectTransform.</summary>
    public void Reiniciar(RectTransform cual)
    {
        foreach (var o in objetivos)
            if (o.objetivo == cual) Reiniciar(o);
    }

    private void Reiniciar(Objetivo o)
    {
        if (!isActiveAndEnabled || o == null || o.objetivo == null) return;

        if (o.ciclo != null) StopCoroutine(o.ciclo);
        o.objetivo.anchoredPosition = o.posicionBase;
        o.ciclo = StartCoroutine(Ciclo(o));
    }

    private IEnumerator Ciclo(Objetivo o)
    {
        yield return new WaitForSecondsRealtime(o.esperaInicial);

        while (true)
        {
            if (DebeMoverse(o))
                yield return Mover(o);

            yield return new WaitForSecondsRealtime(o.intervalo);
        }
    }

    private static bool DebeMoverse(Objetivo o)
    {
        if (o.objetivo == null || !o.objetivo.gameObject.activeInHierarchy) return false;
        if (o.panel != null && o.panel.EstaPanelVisible()) return false;

        // Un botón apagado (p. ej. por un paso del tutorial que solo deja tocar otra cosa) no
        // debe pedir que lo toquen: sería invitar a algo que no responde.
        if (o.boton != null && !o.boton.interactable) return false;

        return true;
    }

    private static IEnumerator Mover(Objetivo o)
    {
        for (int i = 0; i < o.repeticiones; i++)
        {
            float t = 0f;
            while (t < o.duracion)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / o.duracion);

                Vector2 desvio = o.tipo == Tipo.BrincoVertical
                    ? new Vector2(0f, o.curva.Evaluate(p) * o.amplitud)
                    // Vaivén que se apaga hacia el final, para terminar quieto en el centro.
                    : new Vector2(Mathf.Sin(p * Mathf.PI * 2f * o.oscilaciones) * o.amplitud * (1f - p), 0f);

                o.objetivo.anchoredPosition = o.posicionBase + desvio;
                yield return null;
            }
            o.objetivo.anchoredPosition = o.posicionBase;

            // Respiro entre repeticiones (no después de la última).
            if (i < o.repeticiones - 1 && o.pausaEntreRepeticiones > 0f)
                yield return new WaitForSecondsRealtime(o.pausaEntreRepeticiones);
        }
    }

    void OnDisable()
    {
        foreach (var o in objetivos)
            if (o != null && o.objetivo != null) o.objetivo.anchoredPosition = o.posicionBase;
    }

    void OnDestroy()
    {
        foreach (var o in objetivos)
            if (o != null && o.boton != null && o.alPulsar != null)
                o.boton.onClick.RemoveListener(o.alPulsar);
    }
}
