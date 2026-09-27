using System.Collections;
using UnityEngine;

/// <summary>
/// Hace que un botón dé un brinquito cada cierto tiempo para llamar la atención del jugador
/// (por ejemplo el que abre la lista de misiones).
///
/// Vive en el GameController: el botón se asigna por referencia, no hace falta poner el script
/// en él. Se calla solo mientras el panel asociado está abierto, y al tocar el botón reinicia
/// la cuenta atrás (si el jugador ya lo vio, no hay que insistir).
/// </summary>
public class LlamadaAtencionBoton : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("RectTransform del botón que debe brincar.")]
    [SerializeField] private RectTransform objetivo;
    [Tooltip("Opcional: mientras este panel esté abierto, el botón no brinca.")]
    [SerializeField] private MissionListUI panelAsociado;

    [Header("Ritmo")]
    [Tooltip("Segundos entre brincos.")]
    [SerializeField] private float intervalo = 8f;
    [Tooltip("Espera antes del primer brinco, para no saltar nada más cargar la escena.")]
    [SerializeField] private float esperaInicial = 4f;

    public enum Tipo { BrincoVertical, ShakeHorizontal }

    [Header("Movimiento")]
    [SerializeField] private Tipo tipo = Tipo.BrincoVertical;
    [SerializeField] private int repeticiones = 2;
    [Tooltip("Altura del brinco, o amplitud del shake, en píxeles.")]
    [SerializeField] private float amplitud = 18f;
    [SerializeField] private float duracion = 0.32f;

    [Header("Solo brinco")]
    [Tooltip("0 = abajo, 1 = arriba. La curva por defecto sube y baja una vez.")]
    [SerializeField] private AnimationCurve curva = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

    [Header("Solo shake")]
    [Tooltip("Vaivenes completos por repetición.")]
    [SerializeField] private float oscilaciones = 3f;

    private Vector2 posicionBase;
    private Coroutine cicloCR;

    void Start()
    {
        if (objetivo == null)
        {
            enabled = false;
            return;
        }

        posicionBase = objetivo.anchoredPosition;

        var boton = objetivo.GetComponent<UnityEngine.UI.Button>();
        if (boton != null) boton.onClick.AddListener(Reiniciar);

        cicloCR = StartCoroutine(Ciclo());
    }

    /// <summary>Vuelve a empezar la cuenta atrás. Útil tras interactuar con el botón.</summary>
    public void Reiniciar()
    {
        if (!isActiveAndEnabled || objetivo == null) return;

        if (cicloCR != null) StopCoroutine(cicloCR);
        objetivo.anchoredPosition = posicionBase;
        cicloCR = StartCoroutine(Ciclo());
    }

    private IEnumerator Ciclo()
    {
        yield return new WaitForSecondsRealtime(esperaInicial);

        while (true)
        {
            if (DebeBrincar())
                yield return Brincar();

            yield return new WaitForSecondsRealtime(intervalo);
        }
    }

    private bool DebeBrincar()
    {
        if (objetivo == null || !objetivo.gameObject.activeInHierarchy) return false;
        if (panelAsociado != null && panelAsociado.EstaPanelVisible()) return false;
        return true;
    }

    private IEnumerator Brincar()
    {
        for (int i = 0; i < repeticiones; i++)
        {
            float t = 0f;
            while (t < duracion)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duracion);

                Vector2 desvio = tipo == Tipo.BrincoVertical
                    ? new Vector2(0f, curva.Evaluate(p) * amplitud)
                    // Vaivén que se apaga hacia el final, para terminar quieto en el centro.
                    : new Vector2(Mathf.Sin(p * Mathf.PI * 2f * oscilaciones) * amplitud * (1f - p), 0f);

                objetivo.anchoredPosition = posicionBase + desvio;
                yield return null;
            }
            objetivo.anchoredPosition = posicionBase;
        }
    }

    void OnDisable()
    {
        if (objetivo != null) objetivo.anchoredPosition = posicionBase;
    }

    void OnDestroy()
    {
        if (objetivo == null) return;
        var boton = objetivo.GetComponent<UnityEngine.UI.Button>();
        if (boton != null) boton.onClick.RemoveListener(Reiniciar);
    }
}
