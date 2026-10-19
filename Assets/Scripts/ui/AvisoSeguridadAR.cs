using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ⚠️ Aviso de seguridad que aparece al entrar a la sección AR.
///
/// Existe por exigencia de la Families Policy de Google Play: una app infantil con realidad
/// aumentada tiene que mostrar un aviso NADA MÁS empezar la parte AR, recordando dos cosas
/// concretas: que conviene la supervisión de un adulto y que hay que estar atento al entorno.
/// La versión 6 fue rechazada justo por no tenerlo.
///
/// Por eso aparece CADA VEZ que arranca una escena AR y no solo la primera: el revisor abre la
/// app, entra, y tiene que verlo. 'soloUnaVezPorSesion' existe para pruebas internas, pero para
/// una build que se envía a revisión debe quedar desactivado.
///
/// Controla el panel por REFERENCIAS (CanvasGroup), no por su propio gameObject, así que puede
/// vivir en el GameController central como el resto de paneles.
/// </summary>
public class AvisoSeguridadAR : MonoBehaviour, IPanelConVisibilidad
{
    [Header("Referencias")]
    [Tooltip("CanvasGroup del panel modal. Debe cubrir la pantalla y bloquear lo que hay detrás.")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Button botonEntendido;

    [Header("Textos (se rellenan solos desde la tabla UI)")]
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoSupervision;
    [SerializeField] private TextMeshProUGUI textoEntorno;
    [SerializeField] private TextMeshProUGUI textoBoton;

    [Header("Comportamiento")]
    [Tooltip("Solo para pruebas internas. En una build que se envía a revisión debe estar en false, " +
             "porque la política pide el aviso al entrar a la sección AR, no una vez y ya.")]
    [SerializeField] private bool soloUnaVezPorSesion = false;
    [Tooltip("Segundos mínimos antes de poder cerrarlo. Evita que se salte sin leerlo de un toque " +
             "accidental. 0 = se puede cerrar al instante.")]
    [SerializeField] private float segundosMinimos = 1.5f;

    [Header("Esperar al aviso para arrancar el AR (opcional)")]
    [Tooltip("Objetos que quedan desactivados hasta que el jugador acepta. Sirve para no abrir la " +
             "cámara detrás del aviso. Dejar vacío si no hace falta.")]
    [SerializeField] private GameObject[] activarAlAceptar;

    [Header("Al aceptar")]
    public UnityEvent alAceptar;

    private static bool yaMostradoEstaSesion = false;
    private bool visible = false;
    private float tiempoMostrado = 0f;

    void Start()
    {
        if (botonEntendido != null)
        {
            botonEntendido.onClick.RemoveAllListeners();
            botonEntendido.onClick.AddListener(OnEntendido);
        }

        if (soloUnaVezPorSesion && yaMostradoEstaSesion)
        {
            Aceptar();
            return;
        }

        Mostrar();
    }

    private void Mostrar()
    {
        RellenarTextos();

        foreach (GameObject go in activarAlAceptar)
            if (go != null) go.SetActive(false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        visible = true;
        tiempoMostrado = Time.unscaledTime;
        yaMostradoEstaSesion = true;

        if (botonEntendido != null && segundosMinimos > 0f)
            StartCoroutine(HabilitarBotonTrasEspera());
    }

    private IEnumerator HabilitarBotonTrasEspera()
    {
        botonEntendido.interactable = false;
        yield return new WaitForSecondsRealtime(segundosMinimos);
        if (botonEntendido != null) botonEntendido.interactable = true;
    }

    /// <summary>
    /// Saca los textos de la tabla UI en el idioma nativo. Si LanguageManager todavía no está
    /// listo deja el texto que venga puesto en el prefab, que nunca debe quedar vacío.
    /// </summary>
    private void RellenarTextos()
    {
        var lm = LanguageManager.Instance;
        if (lm == null)
        {
            Debug.LogWarning("[AvisoSeguridadAR] LanguageManager no disponible, se usan los textos del prefab");
            return;
        }

        Poner(textoTitulo,      lm.TextoNativo("ar.aviso.titulo"));
        Poner(textoSupervision, lm.TextoNativo("ar.aviso.supervision"));
        Poner(textoEntorno,     lm.TextoNativo("ar.aviso.entorno"));
        Poner(textoBoton,       lm.TextoNativo("ar.aviso.boton"));
    }

    private void Poner(TextMeshProUGUI campo, string texto)
    {
        if (campo != null && !string.IsNullOrEmpty(texto)) campo.text = texto;
    }

    private void OnEntendido()
    {
        if (segundosMinimos > 0f && Time.unscaledTime - tiempoMostrado < segundosMinimos) return;
        Aceptar();
    }

    private void Aceptar()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        visible = false;

        foreach (GameObject go in activarAlAceptar)
            if (go != null) go.SetActive(true);

        alAceptar?.Invoke();
        Debug.Log("[AvisoSeguridadAR] ✅ Aviso aceptado, sección AR disponible");
    }

    public bool EstaPanelVisible() => visible;

    [ContextMenu("🔄 Olvidar que ya se mostró (pruebas)")]
    public void OlvidarSesion() => yaMostradoEstaSesion = false;
}
