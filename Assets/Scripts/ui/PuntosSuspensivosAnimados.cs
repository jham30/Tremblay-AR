using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

/// <summary>
/// Añade puntos suspensivos animados al final de un texto ("Continuar" → "Continuar...",
/// apareciendo uno a uno).
///
/// El texto suele venir de la tabla UI vía LocalizeStringEvent, así que concatenar los puntos
/// no vale: la localización los pisaría al refrescar o al cambiar de idioma. En su lugar este
/// componente escucha al localizador para saber el texto base, escribe UNA vez "base..." y
/// anima 'maxVisibleCharacters'. Así los puntos ya ocupan su sitio desde el principio y el
/// texto no se descoloca al aparecer, que es lo que pasaría concatenando.
///
/// Vive en el GameController y controla el TMP por referencia.
/// </summary>
public class PuntosSuspensivosAnimados : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI texto;
    [SerializeField] private string puntos = "...";
    [Tooltip("Segundos entre punto y punto.")]
    [SerializeField] private float intervalo = 0.35f;
    [Tooltip("Pausa extra con todos los puntos visibles, antes de volver a empezar.")]
    [SerializeField] private float pausaAlCompletar = 0.7f;

    private LocalizeStringEvent localizador;
    private string textoBase = "";
    private int visibles;

    void Awake()
    {
        if (texto == null)
        {
            enabled = false;
            return;
        }

        localizador = texto.GetComponent<LocalizeStringEvent>();
        if (localizador != null)
            localizador.OnUpdateString.AddListener(OnTextoLocalizado);
        else
            textoBase = texto.text;
    }

    void OnEnable()
    {
        StartCoroutine(Animar());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        Restaurar();
    }

    // Lo llama la localización cada vez que resuelve el texto (arranque y cambio de idioma).
    private void OnTextoLocalizado(string valor)
    {
        textoBase = valor ?? "";
        Pintar();
    }

    private IEnumerator Animar()
    {
        while (true)
        {
            for (visibles = 0; visibles <= puntos.Length; visibles++)
            {
                Pintar();
                yield return new WaitForSecondsRealtime(
                    visibles == puntos.Length ? pausaAlCompletar : intervalo);
            }
        }
    }

    private void Pintar()
    {
        if (texto == null || string.IsNullOrEmpty(textoBase)) return;

        string completo = textoBase + puntos;
        if (texto.text != completo) texto.text = completo;

        texto.maxVisibleCharacters = textoBase.Length + visibles;
    }

    private void Restaurar()
    {
        if (texto == null) return;
        texto.maxVisibleCharacters = int.MaxValue;
        if (!string.IsNullOrEmpty(textoBase)) texto.text = textoBase;
    }

    void OnDestroy()
    {
        if (localizador != null)
            localizador.OnUpdateString.RemoveListener(OnTextoLocalizado);
    }
}
