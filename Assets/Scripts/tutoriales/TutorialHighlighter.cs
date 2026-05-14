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
    [SerializeField] private float grosorBorde = 5f;
    [SerializeField] private bool bordeAnimado = true;
    
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
    private Image imagenBorde;
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
                CrearBorde();
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
        if (flechaObjeto != null)
        {
            DestroyImmediate(flechaObjeto);
            flechaObjeto = null;
            imagenFlecha = null;
        }
        
        if (bordeObjeto != null)
        {
            DestroyImmediate(bordeObjeto);
            bordeObjeto = null;
            imagenBorde = null;
        }
        
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
        
        // Configurar RectTransform
        RectTransform rectFlecha = flechaObjeto.AddComponent<RectTransform>();
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
    /// Crear borde alrededor del elemento
    /// </summary>
    private void CrearBorde()
    {
        if (rectElemento == null || canvasUI == null)
        {
            Debug.LogError("[TutorialHighlighter] No se puede crear borde - falta rectElemento o canvasUI");
            return;
        }
        
        // Crear GameObject para el borde
        bordeObjeto = new GameObject("TutorialBorde");
        bordeObjeto.transform.SetParent(canvasUI.transform, false);
        
        // Configurar RectTransform
        RectTransform rectBorde = bordeObjeto.AddComponent<RectTransform>();
        
        // Copiar posición y tamaño del elemento objetivo
        rectBorde.position = rectElemento.position;
        rectBorde.sizeDelta = rectElemento.sizeDelta + Vector2.one * grosorBorde * 2;
        rectBorde.anchorMin = rectElemento.anchorMin;
        rectBorde.anchorMax = rectElemento.anchorMax;
        rectBorde.anchoredPosition = rectElemento.anchoredPosition;
        
        // Configurar Image como borde
        imagenBorde = bordeObjeto.AddComponent<Image>();
        imagenBorde.raycastTarget = false;
        imagenBorde.color = colorBorde;
        
        // Crear efecto de borde hueco
        imagenBorde.type = Image.Type.Sliced;
        
        // Guardar color original para animación
        colorOriginalBorde = colorBorde;
        
        // Posicionar detrás del elemento objetivo pero al frente de otros
        bordeObjeto.transform.SetAsLastSibling();
        
        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] 🔲 Borde creado en Canvas: {canvasUI.name}");
    }

    /// <summary>
    /// Crear efecto glow (brillo suave)
    /// </summary>
    private void CrearGlow()
    {
        // Implementación básica de glow
        CrearBorde();
        if (imagenBorde != null)
        {
            imagenBorde.color = new Color(colorBorde.r, colorBorde.g, colorBorde.b, 0.5f);
            
            if (mostrarDebug)
                Debug.Log($"[TutorialHighlighter] ✨ Glow creado en Canvas: {canvasUI.name}");
        }
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
    /// Posicionar flecha según dirección
    /// </summary>
    private void PosicionarFlecha(RectTransform rectFlecha)
    {
        if (rectElemento == null) return;
        
        Vector2 posicionObjetivo = rectElemento.anchoredPosition;
        Vector2 tamañoObjetivo = rectElemento.sizeDelta;
        
        switch (direccionFlecha)
        {
            case DireccionFlecha.Izquierda:
                rectFlecha.anchoredPosition = new Vector2(posicionObjetivo.x - tamañoObjetivo.x/2 - distanciaFlecha, posicionObjetivo.y);
                break;
            case DireccionFlecha.Derecha:
                rectFlecha.anchoredPosition = new Vector2(posicionObjetivo.x + tamañoObjetivo.x/2 + distanciaFlecha, posicionObjetivo.y);
                break;
            case DireccionFlecha.Arriba:
                rectFlecha.anchoredPosition = new Vector2(posicionObjetivo.x, posicionObjetivo.y + tamañoObjetivo.y/2 + distanciaFlecha);
                break;
            case DireccionFlecha.Abajo:
                rectFlecha.anchoredPosition = new Vector2(posicionObjetivo.x, posicionObjetivo.y - tamañoObjetivo.y/2 - distanciaFlecha);
                break;
        }
        
        // Configurar anclaje igual al elemento objetivo
        rectFlecha.anchorMin = rectElemento.anchorMin;
        rectFlecha.anchorMax = rectElemento.anchorMax;
        
        if (mostrarDebug)
            Debug.Log($"[TutorialHighlighter] 📍 Flecha posicionada: {rectFlecha.anchoredPosition} (Dirección: {direccionFlecha})");
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
        
        // Animar borde
        if (bordeObjeto != null && imagenBorde != null && bordeAnimado)
        {
            float alpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(tiempoAnimacion) + 1f) / 2f);
            imagenBorde.color = new Color(colorOriginalBorde.r, colorOriginalBorde.g, colorOriginalBorde.b, alpha);
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