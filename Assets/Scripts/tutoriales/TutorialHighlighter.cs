using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema simple de resaltado para elementos UI en el tutorial
/// Configurable desde Inspector con drag & drop
/// VERSIÓN CORREGIDA: Usa el Canvas específico del elemento
/// </summary>
public class TutorialHighlighter : MonoBehaviour
{
    [Header("📍 Elemento a Resaltar")]
    [Tooltip("Arrastra aquí el botón, panel o UI que quieres resaltar")]
    [SerializeField] private GameObject elementoUI;
    
    [Header("🎨 Tipo de Resaltado")]
    [SerializeField] private TipoResaltado tipoResaltado = TipoResaltado.Flecha;
    
    [Header("➡️ Configuración Flecha (Para Botones)")]
    [Tooltip("Sprite de la flecha que apunta al botón")]
    [SerializeField] private Sprite spriteFlechaIzquierda;
    [SerializeField] private Sprite spriteFlechaDerecha;
    [SerializeField] private Sprite spriteFlechaArriba;
    [SerializeField] private Sprite spriteFlechaAbajo;
    [SerializeField] private DireccionFlecha direccionFlecha = DireccionFlecha.Izquierda;
    [SerializeField] private float distanciaFlecha = 100f;
    [SerializeField] private float tamañoFlecha = 64f;
    
    [Header("🔲 Configuración Borde (Para Paneles)")]
    [Tooltip("Color del borde que aparece alrededor del panel")]
    [SerializeField] private Color colorBorde = Color.yellow;
    [Tooltip("Grosor del marco en px. El marco es HUECO: rodea al elemento por fuera sin taparlo.")]
    [SerializeField] private float grosorBorde = 5f;
    [Tooltip("OPCIONAL. Sprite de marco con bordes 9-slice configurados en su import. Si se asigna, " +
             "se usa ese sprite (Image.Type.Sliced) en vez de construir el marco con 4 barras. " +
             "Un sprite con los bordes difuminados da un glow suave de verdad.")]
    [SerializeField] private Sprite spriteBorde;
    [SerializeField] private bool bordeAnimado = true;
    [Tooltip("El Glow usa el mismo marco pero más grueso y traslúcido. Multiplica el grosor.")]
    [SerializeField] private float glowMultiplicadorGrosor = 2.5f;
    
    [Header("✨ Efectos")]
    [SerializeField] private bool usarAnimacion = true;
    [SerializeField] private float velocidadPulso = 2f;
    [SerializeField] private float intensidadPulso = 0.3f;
    
    [Header("🔧 Debug")]
    [SerializeField] private bool mostrarDebug = true;
    
    // Componentes creados dinámicamente
    private GameObject flechaObjeto;
    private GameObject bordeObjeto;
    private Image imagenFlecha;
    // El marco puede ser 1 Image (sprite 9-slice) o 4 barras (marco hueco construido a mano).
    private readonly List<Image> imagenesBorde = new List<Image>();
    private RectTransform rectElemento;
    private Canvas canvasUI;
    
    // Para animaciones
    private float tiempoAnimacion = 0f;
    private Vector3 escalaOriginalFlecha;
    private Color colorOriginalBorde;
    
    private bool resaltadoActivo = false;

    void Awake()
    {
        // NO buscar Canvas aquí - lo haremos cuando configuremos el elemento específico
        if (mostrarDebug)
            Debug.Log("[TutorialHighlighter] Inicializado - Canvas se asignará con el elemento");
    }

    void Start()
    {
        // Configurar elemento si está asignado
        if (elementoUI != null)
        {
            ConfigurarElemento();
        }
    }

    void Update()
    {
        // Animaciones si están activas
        if (resaltadoActivo && usarAnimacion)
        {
            ActualizarAnimaciones();
        }
    }

    /// <summary>
    /// Configurar el elemento UI a resaltar
    /// </summary>
    public void ConfigurarElemento(GameObject nuevoElemento = null)
    {
        if (nuevoElemento != null)
        {
            elementoUI = nuevoElemento;
        }
        
        if (elementoUI == null)
        {
            Debug.LogWarning("[TutorialHighlighter] No hay elemento UI asignado!");
            return;
        }
        
        rectElemento = elementoUI.GetComponent<RectTransform>();
        if (rectElemento == null)
        {
            Debug.LogError($"[TutorialHighlighter] El elemento {elementoUI.name} no tiene RectTransform!");
            return;
        }
        
        // 🎯 NUEVO: Encontrar el Canvas del elemento específico
        canvasUI = elementoUI.GetComponentInParent<Canvas>(true); // true = busca en padres inactivos
        if (canvasUI == null)
        {
            Debug.LogError($"[TutorialHighlighter] El elemento {elementoUI.name} no está en un Canvas!");
            return;
        }
        
        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] ✅ Elemento configurado: {elementoUI.name} en Canvas: {canvasUI.name}");
    }

    /// <summary>
    /// Mostrar el resaltado
    /// </summary>
    public void MostrarResaltado()
    {
        if (elementoUI == null)
        {
            Debug.LogWarning("[TutorialHighlighter] No hay elemento para resaltar!");
            return;
        }
        
        if (canvasUI == null)
        {
            Debug.LogWarning("[TutorialHighlighter] No hay Canvas asignado! Configurando elemento...");
            ConfigurarElemento();
            if (canvasUI == null) return;
        }
        
        // Ocultar cualquier resaltado previo
        OcultarResaltado();
        
        switch (tipoResaltado)
        {
            case TipoResaltado.Flecha:
                CrearFlecha();
                break;
            case TipoResaltado.Borde:
                CrearBorde(grosorBorde);
                break;
            case TipoResaltado.Glow:
                CrearGlow();
                break;
        }
        
        resaltadoActivo = true;
        
        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] 🔍 Resaltado mostrado: {tipoResaltado} en {elementoUI.name} (Canvas: {canvasUI.name})");
    }

    /// <summary>
    /// Ocultar el resaltado
    /// </summary>
    public void OcultarResaltado()
    {
        // Destroy (no DestroyImmediate): DestroyImmediate es para el editor y en runtime
        // puede romper si se llama a media ejecución de un frame.
        if (flechaObjeto != null)
        {
            Destroy(flechaObjeto);
            flechaObjeto = null;
            imagenFlecha = null;
        }

        if (bordeObjeto != null)
        {
            Destroy(bordeObjeto);
            bordeObjeto = null;
        }
        imagenesBorde.Clear();
        
        resaltadoActivo = false;
        
        if (mostrarDebug)
            Debug.Log("[TutorialHighlighter] ❌ Resaltado ocultado");
    }

    /// <summary>
    /// Crear flecha apuntando al elemento
    /// </summary>
    private void CrearFlecha()
    {
        if (rectElemento == null || canvasUI == null) 
        {
            Debug.LogError("[TutorialHighlighter] No se puede crear flecha - falta rectElemento o canvasUI");
            return;
        }
        
        // Crear GameObject para la flecha
        flechaObjeto = new GameObject("TutorialFlecha");
        flechaObjeto.transform.SetParent(canvasUI.transform, false);
        
        // Configurar RectTransform — anclado al CENTRO del canvas para que anchoredPosition
        // sea una coordenada absoluta dentro del canvas (ver CalcularRectEnCanvas).
        RectTransform rectFlecha = flechaObjeto.AddComponent<RectTransform>();
        rectFlecha.anchorMin = rectFlecha.anchorMax = new Vector2(0.5f, 0.5f);
        rectFlecha.pivot = new Vector2(0.5f, 0.5f);
        rectFlecha.sizeDelta = new Vector2(tamañoFlecha, tamañoFlecha);
        
        // Configurar Image
        imagenFlecha = flechaObjeto.AddComponent<Image>();
        imagenFlecha.raycastTarget = false; // No bloquear interacciones
        
        // Seleccionar sprite según dirección
        Sprite spriteAUsar = ObtenerSpriteFlecha();
        if (spriteAUsar != null)
        {
            imagenFlecha.sprite = spriteAUsar;
            imagenFlecha.color = Color.white; // Usar color original del sprite
        }
        else
        {
            // Crear flecha básica si no hay sprite
            imagenFlecha.color = Color.yellow;
            
            if (mostrarDebug)
                Debug.Log("[TutorialHighlighter] ⚠️ No hay sprite de flecha asignado, usando color sólido");
        }
        
        // Posicionar la flecha
        PosicionarFlecha(rectFlecha);
        
        // Guardar escala original para animación
        escalaOriginalFlecha = rectFlecha.localScale;
        
        // Asegurar que aparezca al frente
        flechaObjeto.transform.SetAsLastSibling();
        
        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] ➡️ Flecha creada en Canvas: {canvasUI.name}");
    }

    /// <summary>
    /// Crear borde alrededor del elemento.
    /// El marco es HUECO: rodea al elemento por fuera sin taparlo.
    /// OJO: un Image NO tiene "borde" como propiedad — pintar un Image sin sprite da un
    /// rectángulo SÓLIDO que tapa el elemento (y Image.Type.Sliced se ignora si no hay sprite).
    /// Por eso el marco se construye con 4 barras finas, o con un sprite 9-slice si se asigna.
    /// </summary>
    private void CrearBorde(float grosor)
    {
        if (rectElemento == null || canvasUI == null)
        {
            Debug.LogError("[TutorialHighlighter] No se puede crear borde - falta rectElemento o canvasUI");
            return;
        }

        // Contenedor del marco (sin Image propio): su rect es el CONTORNO EXTERIOR.
        bordeObjeto = new GameObject("TutorialBorde");
        bordeObjeto.transform.SetParent(canvasUI.transform, false);

        // Configurar RectTransform — anclado al centro del canvas y colocado con la
        // conversión de coordenadas (el elemento puede estar anidado varios niveles).
        RectTransform rectBorde = bordeObjeto.AddComponent<RectTransform>();
        rectBorde.anchorMin = rectBorde.anchorMax = new Vector2(0.5f, 0.5f);
        rectBorde.pivot = new Vector2(0.5f, 0.5f);

        if (CalcularRectEnCanvas(out Vector2 centroBorde, out Vector2 tamanoBorde))
        {
            rectBorde.anchoredPosition = centroBorde;
            rectBorde.sizeDelta = tamanoBorde + Vector2.one * grosor * 2f;
        }
        else
        {
            Debug.LogWarning("[TutorialHighlighter] No se pudo calcular el rect del elemento para el borde");
            rectBorde.sizeDelta = rectElemento.sizeDelta + Vector2.one * grosor * 2f;
        }

        colorOriginalBorde = colorBorde;
        imagenesBorde.Clear();

        if (spriteBorde != null)
        {
            // Camino A: sprite de marco con 9-slice. Aquí Image.Type.Sliced SÍ funciona
            // (el sprite debe traer los bordes definidos en su import settings).
            Image img = bordeObjeto.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = colorBorde;
            img.sprite = spriteBorde;
            img.type = Image.Type.Sliced;
            imagenesBorde.Add(img);
        }
        else
        {
            // Camino B: marco hueco con 4 barras (arriba/abajo a lo ancho; izq/der recortadas
            // en vertical para no pintar dos veces las esquinas y que no se vean más oscuras).
            CrearBarraBorde("Arriba",   new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, grosor));
            CrearBarraBorde("Abajo",    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, grosor));
            CrearBarraBorde("Izquierda", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(grosor, -grosor * 2f));
            CrearBarraBorde("Derecha",   new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(grosor, -grosor * 2f));
        }

        // Al frente: como el marco es hueco, no tapa el elemento.
        bordeObjeto.transform.SetAsLastSibling();

        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] 🔲 Borde creado ({(spriteBorde != null ? "sprite 9-slice" : "4 barras")}, " +
                      $"grosor {grosor}) en Canvas: {canvasUI.name}");
    }

    /// <summary>
    /// Crea una de las 4 barras del marco hueco, anclada a un lado del contenedor
    /// (así se estira sola con el tamaño del marco).
    /// </summary>
    private void CrearBarraBorde(string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        GameObject barra = new GameObject(nombre);
        barra.transform.SetParent(bordeObjeto.transform, false);

        RectTransform rt = barra.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;

        Image img = barra.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = colorBorde;
        imagenesBorde.Add(img);
    }

    /// <summary>
    /// Crear efecto glow (brillo suave): el mismo marco, más grueso y traslúcido.
    /// Para un glow realmente difuminado, asigna un 'spriteBorde' con los bordes suavizados.
    /// </summary>
    private void CrearGlow()
    {
        CrearBorde(grosorBorde * Mathf.Max(1f, glowMultiplicadorGrosor));

        foreach (Image img in imagenesBorde)
        {
            if (img != null)
                img.color = new Color(colorBorde.r, colorBorde.g, colorBorde.b, 0.5f);
        }

        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] ✨ Glow creado en Canvas: {canvasUI.name}");
    }

    /// <summary>
    /// Obtener sprite de flecha según dirección
    /// </summary>
    private Sprite ObtenerSpriteFlecha()
    {
        switch (direccionFlecha)
        {
            case DireccionFlecha.Izquierda: return spriteFlechaIzquierda;
            case DireccionFlecha.Derecha: return spriteFlechaDerecha;
            case DireccionFlecha.Arriba: return spriteFlechaArriba;
            case DireccionFlecha.Abajo: return spriteFlechaAbajo;
            default: return spriteFlechaIzquierda;
        }
    }

    /// <summary>
    /// Calcula el centro y el tamaño del elemento objetivo EN EL ESPACIO LOCAL DEL CANVAS.
    /// Necesario porque el resaltado se crea como hijo del Canvas raíz, mientras que el
    /// elemento puede estar anidado varios niveles (su anchoredPosition es relativa a SU padre,
    /// no al Canvas). Sin esta conversión el resaltado aparecía fuera de pantalla.
    /// </summary>
    private bool CalcularRectEnCanvas(out Vector2 centroLocal, out Vector2 tamano)
    {
        centroLocal = Vector2.zero;
        tamano = Vector2.zero;

        if (rectElemento == null || canvasUI == null) return false;

        RectTransform canvasRect = canvasUI.transform as RectTransform;
        if (canvasRect == null) return false;

        Camera cam = canvasUI.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasUI.worldCamera;

        // Esquinas del elemento en mundo → pantalla → local del canvas
        Vector3[] esquinas = new Vector3[4];
        rectElemento.GetWorldCorners(esquinas);

        Vector2 pantallaMin = RectTransformUtility.WorldToScreenPoint(cam, esquinas[0]); // inferior-izq
        Vector2 pantallaMax = RectTransformUtility.WorldToScreenPoint(cam, esquinas[2]); // superior-der

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pantallaMin, cam, out Vector2 localMin)) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pantallaMax, cam, out Vector2 localMax)) return false;

        centroLocal = (localMin + localMax) * 0.5f;
        tamano = new Vector2(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y));
        return true;
    }

    /// <summary>
    /// Posicionar flecha según dirección (en espacio del Canvas)
    /// </summary>
    private void PosicionarFlecha(RectTransform rectFlecha)
    {
        if (!CalcularRectEnCanvas(out Vector2 centro, out Vector2 tamano))
        {
            Debug.LogWarning("[TutorialHighlighter] No se pudo calcular la posición del elemento en el Canvas");
            return;
        }

        Vector2 offset = Vector2.zero;
        switch (direccionFlecha)
        {
            case DireccionFlecha.Izquierda: offset = new Vector2(-(tamano.x / 2f + distanciaFlecha), 0f); break;
            case DireccionFlecha.Derecha:   offset = new Vector2( (tamano.x / 2f + distanciaFlecha), 0f); break;
            case DireccionFlecha.Arriba:    offset = new Vector2(0f,  (tamano.y / 2f + distanciaFlecha)); break;
            case DireccionFlecha.Abajo:     offset = new Vector2(0f, -(tamano.y / 2f + distanciaFlecha)); break;
        }

        rectFlecha.anchoredPosition = centro + offset;

        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] 📍 Flecha en {rectFlecha.anchoredPosition} (centro elemento: {centro}, dir: {direccionFlecha})");
    }

    /// <summary>
    /// Actualizar animaciones de pulso
    /// </summary>
    private void ActualizarAnimaciones()
    {
        if (!usarAnimacion) return;
        
        tiempoAnimacion += Time.deltaTime * velocidadPulso;
        float factorPulso = 1f + Mathf.Sin(tiempoAnimacion) * intensidadPulso;
        
        // Animar flecha
        if (flechaObjeto != null && imagenFlecha != null)
        {
            flechaObjeto.transform.localScale = escalaOriginalFlecha * factorPulso;
        }
        
        // Animar borde (todas las barras del marco a la vez)
        if (bordeObjeto != null && bordeAnimado && imagenesBorde.Count > 0)
        {
            float alpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(tiempoAnimacion) + 1f) / 2f);
            Color c = new Color(colorOriginalBorde.r, colorOriginalBorde.g, colorOriginalBorde.b, alpha);

            foreach (Image img in imagenesBorde)
            {
                if (img != null) img.color = c;
            }
        }
    }

    /// <summary>
    /// Cambiar el tipo de resaltado dinámicamente
    /// </summary>
    public void CambiarTipoResaltado(TipoResaltado nuevoTipo)
    {
        bool estabaActivo = resaltadoActivo;
        OcultarResaltado();
        tipoResaltado = nuevoTipo;
        
        if (estabaActivo)
        {
            MostrarResaltado();
        }
        
        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] 🔄 Tipo cambiado a: {nuevoTipo}");
    }

    /// <summary>
    /// Cambiar dirección de flecha dinámicamente
    /// </summary>
    public void CambiarDireccionFlecha(DireccionFlecha nuevaDireccion)
    {
        direccionFlecha = nuevaDireccion;
        
        if (resaltadoActivo && tipoResaltado == TipoResaltado.Flecha)
        {
            MostrarResaltado(); // Recrear con nueva dirección
        }
        
        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] 🧭 Dirección cambiada a: {nuevaDireccion}");
    }

    /// <summary>
    /// Obtener información de debug del estado actual
    /// </summary>
    public string ObtenerInfoDebug()
    {
        string info = "=== TUTORIAL HIGHLIGHTER DEBUG ===\n";
        info += $"Elemento UI: {(elementoUI != null ? elementoUI.name : "null")}\n";
        info += $"Canvas UI: {(canvasUI != null ? canvasUI.name : "null")}\n";
        info += $"RectTransform: {(rectElemento != null ? "OK" : "null")}\n";
        info += $"Resaltado Activo: {resaltadoActivo}\n";
        info += $"Tipo Resaltado: {tipoResaltado}\n";
        info += $"Dirección Flecha: {direccionFlecha}\n";
        info += $"Flecha Objeto: {(flechaObjeto != null ? "Creado" : "null")}\n";
        info += $"Borde Objeto: {(bordeObjeto != null ? "Creado" : "null")}\n";
        return info;
    }

    // =======================================
    // 🧪 MÉTODOS DE TESTING
    // =======================================
    
    [ContextMenu("🔍 Test Mostrar Resaltado")]
    public void TestMostrarResaltado()
    {
        MostrarResaltado();
    }
    
    [ContextMenu("❌ Test Ocultar Resaltado")]
    public void TestOcultarResaltado()
    {
        OcultarResaltado();
    }
    
    [ContextMenu("🔄 Test Cambiar a Flecha")]
    public void TestCambiarAFlecha()
    {
        CambiarTipoResaltado(TipoResaltado.Flecha);
    }
    
    [ContextMenu("🔲 Test Cambiar a Borde")]
    public void TestCambiarABorde()
    {
        CambiarTipoResaltado(TipoResaltado.Borde);
    }

    [ContextMenu("✨ Test Cambiar a Glow")]
    public void TestCambiarAGlow()
    {
        CambiarTipoResaltado(TipoResaltado.Glow);
    }

    [ContextMenu("🧭 Test Cambiar Dirección")]
    public void TestCambiarDireccion()
    {
        // Ciclar entre direcciones
        DireccionFlecha[] direcciones = { DireccionFlecha.Izquierda, DireccionFlecha.Derecha, DireccionFlecha.Arriba, DireccionFlecha.Abajo };
        int indiceActual = System.Array.IndexOf(direcciones, direccionFlecha);
        int siguienteIndice = (indiceActual + 1) % direcciones.Length;
        CambiarDireccionFlecha(direcciones[siguienteIndice]);
    }

    [ContextMenu("📊 Test Mostrar Info Debug")]
    public void TestMostrarInfoDebug()
    {
        Debug.Log(ObtenerInfoDebug());
    }

    [ContextMenu("🔧 Test Reconfigurar")]
    public void TestReconfigurar()
    {
        ConfigurarElemento();
    }

    void OnDestroy()
    {
        // Limpiar objetos creados
        OcultarResaltado();
    }
}

/// <summary>
/// Tipos de resaltado disponibles
/// </summary>
[System.Serializable]
public enum TipoResaltado
{
    Flecha,     // Sprite apuntando (para botones)
    Borde,      // Marco alrededor (para paneles)
    Glow        // Brillo suave (para elementos especiales)
}

/// <summary>
/// Direcciones para las flechas
/// </summary>
[System.Serializable]
public enum DireccionFlecha
{
    Izquierda,
    Derecha,
    Arriba,
    Abajo
}