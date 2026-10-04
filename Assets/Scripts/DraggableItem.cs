using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public string objetoID;
    [HideInInspector] public bool esArrastrable = true;
    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public Transform startParent;

    [Header("Efectos de Sonido")]
    [SerializeField] private AudioClip sonidoAgarrar; // Sonido al comenzar drag
    [SerializeField] private AudioClip sonidoSoltar;  // Sonido al terminar drag
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool usarAudioGlobal = true; // Usar AudioSource global si existe

    [Header("Configuración Audio")]
    [SerializeField] private float volumenAgarrar = 0.7f;
    [SerializeField] private float volumenSoltar = 0.6f;
    [SerializeField] private bool reproducirSonidoSoloEnExito = false;

    [Header("Hueco en la lista")]
    [Tooltip("Opacidad de la silueta que queda en la lista mientras el ítem está fuera de ella.")]
    [SerializeField, Range(0f, 1f)] private float alphaHueco = 0.3f;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // Mientras el ítem está en la lista del inventario, un arrastre a lo largo del eje del
    // scroll se le pasa al ScrollRect (el ítem no se mueve). Solo un arrastre en el otro eje
    // agarra el ítem. Sin esto el ítem se quedaba con todos los arrastres y la lista no
    // podía desplazarse más que tocando huecos vacíos.
    private ScrollRect scrollReenviado;

    // Al salir de la lista el ítem deja una silueta en su lugar para que el resto no se
    // recorra, y al volver ocupa ese mismo lugar. Antes volvía al final de la lista y, con
    // scroll, parecía desaparecer. TODO regreso a la lista pasa por VolverALista().
    private GameObject hueco;
    private Transform ultimaLista;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
        
        ConfigurarAudio();
    }

    private void ConfigurarAudio()
    {
        // Si GlobalAudioManager está disponible lo usaremos directamente; no necesitamos nada más.
        if (usarAudioGlobal && GlobalAudioManager.Instance != null) return;

        // Fallback: AudioSource local en este GameObject
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        scrollReenviado = null;
        ScrollRect scroll = ScrollQueDebeRecibir(eventData);
        if (scroll != null)
        {
            scrollReenviado = scroll;
            ExecuteEvents.Execute(scroll.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(scroll.gameObject, eventData, ExecuteEvents.beginDragHandler);
            return;
        }

        if (!esArrastrable) return;

        startPosition = transform.position;
        startParent = transform.parent;

        if (EstaEnLista(startParent))
            DejarHueco();

        DropSocketAvanzado socket = startParent.GetComponent<DropSocketAvanzado>();
        if (socket != null)
            socket.VaciarSocket();

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform);

        // 🎵 REPRODUCIR SONIDO AL AGARRAR
        ReproducirSonidoAgarrar();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollReenviado != null)
        {
            ExecuteEvents.Execute(scrollReenviado.gameObject, eventData, ExecuteEvents.dragHandler);
            return;
        }

        if (!esArrastrable) return;
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollReenviado != null)
        {
            ExecuteEvents.Execute(scrollReenviado.gameObject, eventData, ExecuteEvents.endDragHandler);
            scrollReenviado = null;
            return;
        }

        if (!esArrastrable) return;

        canvasGroup.blocksRaycasts = true;
        bool dropExitoso = transform.parent != canvas.transform;

        if (!dropExitoso)
        {
            if (EstaEnLista(startParent))
            {
                VolverALista();
            }
            else
            {
                rectTransform.position = startPosition;
                transform.SetParent(startParent);
            }

            // 🎵 SONIDO AL SOLTAR SIN ÉXITO
            if (!reproducirSonidoSoloEnExito)
            {
                ReproducirSonidoSoltar(false);
            }
        }
        else
        {
            if (transform.parent.CompareTag("ListaObjetos"))
            {
                startParent = transform.parent;
                startPosition = transform.position;
            }
            
            // 🎵 SONIDO AL SOLTAR CON ÉXITO - SOLO si no hay socket que lo maneje
            // El DropSocketAvanzado manejará su propio sonido
            if (!HaySocketEnParent())
            {
                ReproducirSonidoSoltar(true);
            }
        }
    }

    /// <summary>
    /// Devuelve el ítem a la lista, en el lugar de donde salió (su hueco). Si no hay hueco
    /// (p. ej. la lista se recargó), va al final de 'listaRespaldo' o de la última lista conocida.
    /// Devuelve false si no sabe a qué lista volver.
    /// </summary>
    public bool VolverALista(Transform listaRespaldo = null)
    {
        Transform lista = hueco != null ? hueco.transform.parent
                        : listaRespaldo != null ? listaRespaldo
                        : ultimaLista;
        if (lista == null) return false;

        int indice = hueco != null ? hueco.transform.GetSiblingIndex() : -1;

        // worldPositionStays = true, igual que al sacarlo hacia el canvas: así la escala vuelve tal cual.
        transform.SetParent(lista, true);
        if (indice >= 0) transform.SetSiblingIndex(indice);
        else             transform.SetAsLastSibling();

        DestruirHueco();

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)lista);
        startParent = lista;
        startPosition = transform.position;
        return true;
    }

    // La lista se reconoce por su DropAreaLista (o por el tag histórico). No solo por el tag:
    // el Content actual del inventario no lo tiene.
    private static bool EstaEnLista(Transform padre)
    {
        return padre != null &&
               (padre.CompareTag("ListaObjetos") || padre.GetComponent<DropAreaLista>() != null);
    }

    private void DejarHueco()
    {
        Transform lista = transform.parent;
        ultimaLista = lista;
        if (hueco != null) return; // ya tiene uno guardándole el sitio

        hueco = new GameObject($"Hueco_{objetoID}", typeof(RectTransform));
        hueco.layer = gameObject.layer;
        hueco.transform.SetParent(lista, false);
        hueco.transform.SetSiblingIndex(transform.GetSiblingIndex());
        CopiarRect(rectTransform, (RectTransform)hueco.transform);

        // Mismos componentes de layout que el ítem → ocupa exactamente el mismo espacio
        // tanto con Grid como con Horizontal Layout Group (controle o no el tamaño de los hijos).
        CopiarImagen(GetComponent<Image>(), hueco);
        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null)
        {
            LayoutElement leHueco = hueco.AddComponent<LayoutElement>();
            leHueco.ignoreLayout    = le.ignoreLayout;
            leHueco.minWidth        = le.minWidth;
            leHueco.minHeight       = le.minHeight;
            leHueco.preferredWidth  = le.preferredWidth;
            leHueco.preferredHeight = le.preferredHeight;
            leHueco.flexibleWidth   = le.flexibleWidth;
            leHueco.flexibleHeight  = le.flexibleHeight;
        }

        Transform sprite = transform.Find("SpriteImage");
        if (sprite != null)
        {
            var spriteHueco = new GameObject("SpriteImage", typeof(RectTransform));
            spriteHueco.layer = gameObject.layer;
            spriteHueco.transform.SetParent(hueco.transform, false);
            CopiarRect((RectTransform)sprite, (RectTransform)spriteHueco.transform);
            CopiarImagen(sprite.GetComponent<Image>(), spriteHueco);
        }

        // El hueco SÍ recibe el toque: el Content de la lista no tiene Image, así que un drop solo
        // llega a DropAreaLista si cae sobre un hijo y sube por la jerarquía. Si el hueco fuera
        // transparente al toque, soltar el ítem justo en su hueco (lo natural) no lo devolvía.
        CanvasGroup cg = hueco.AddComponent<CanvasGroup>();
        cg.alpha = alphaHueco;
        cg.interactable = false;
        cg.blocksRaycasts = true;
    }

    private void DestruirHueco()
    {
        if (hueco == null) return;
        // Desactivar ya: Destroy es diferido y el layout lo seguiría contando este frame.
        hueco.SetActive(false);
        Destroy(hueco);
        hueco = null;
    }

    private static void CopiarRect(RectTransform origen, RectTransform destino)
    {
        destino.anchorMin        = origen.anchorMin;
        destino.anchorMax        = origen.anchorMax;
        destino.pivot            = origen.pivot;
        destino.sizeDelta        = origen.sizeDelta;
        destino.anchoredPosition = origen.anchoredPosition;
        destino.localRotation    = origen.localRotation;
        destino.localScale       = origen.localScale;
    }

    private static void CopiarImagen(Image origen, GameObject destino)
    {
        if (origen == null) return;
        Image img = destino.AddComponent<Image>();
        img.sprite         = origen.sprite;
        img.type           = origen.type;
        img.preserveAspect = origen.preserveAspect;
        img.color          = origen.color;
        img.raycastTarget  = true; // ver comentario del CanvasGroup en DejarHueco()
    }

    void OnDestroy()
    {
        DestruirHueco();
    }

    /// <summary>
    /// Devuelve el ScrollRect de la lista si el ítem está dentro de él y el gesto va a lo
    /// largo de su eje de desplazamiento; null si el gesto debe agarrar el ítem.
    /// </summary>
    private ScrollRect ScrollQueDebeRecibir(PointerEventData eventData)
    {
        ScrollRect scroll = GetComponentInParent<ScrollRect>();
        if (scroll == null || transform.parent != scroll.content) return null;

        Vector2 gesto = eventData.position - eventData.pressPosition;
        bool esHorizontal = Mathf.Abs(gesto.x) > Mathf.Abs(gesto.y);

        if (scroll.horizontal && !scroll.vertical) return esHorizontal ? scroll : null;
        if (scroll.vertical && !scroll.horizontal) return esHorizontal ? null : scroll;
        return null; // scroll en ambos ejes: no hay forma de distinguir, se prioriza agarrar
    }

    private bool HaySocketEnParent()
    {
        return transform.parent != null && 
               transform.parent.GetComponent<DropSocketAvanzado>() != null;
    }

    private void ReproducirSonidoAgarrar()
    {
        if (usarAudioGlobal && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoAgarrarItem();
            return;
        }
        ReproducirSonidoLocal(sonidoAgarrar, volumenAgarrar);
    }

    private void ReproducirSonidoSoltar(bool exitoso)
    {
        if (reproducirSonidoSoloEnExito && !exitoso) return;

        if (usarAudioGlobal && GlobalAudioManager.Instance != null)
        {
            if (exitoso) GlobalAudioManager.Instance.ReproducirSonidoSoltarExitoso();
            else         GlobalAudioManager.Instance.ReproducirSonidoSoltarFallido();
            return;
        }
        ReproducirSonidoLocal(sonidoSoltar, volumenSoltar);
    }

    private void ReproducirSonidoLocal(AudioClip clip, float volumen)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.volume = volumen;
            audioSource.PlayOneShot(clip);
        }
    }

    public void ConfigurarSonidos(AudioClip agarrar, AudioClip soltar)
    {
        sonidoAgarrar = agarrar;
        sonidoSoltar = soltar;
    }

    public void ConfigurarVolumen(float volAgarrar, float volSoltar)
    {
        volumenAgarrar = Mathf.Clamp01(volAgarrar);
        volumenSoltar = Mathf.Clamp01(volSoltar);
    }

    [ContextMenu("🎵 Probar Sonido Agarrar")]
    public void ProbarSonidoAgarrar()
    {
        ReproducirSonidoAgarrar();
    }

    [ContextMenu("🎵 Probar Sonido Soltar Exitoso")]
    public void ProbarSonidoSoltarExitoso()
    {
        ReproducirSonidoSoltar(true);
    }

    [ContextMenu("🎵 Probar Sonido Soltar Fallido")]
    public void ProbarSonidoSoltarFallido()
    {
        ReproducirSonidoSoltar(false);
    }
}
