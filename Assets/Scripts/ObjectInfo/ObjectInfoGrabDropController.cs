using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controlador de agarrar/colocar objetos para ObjectInfo - Maneja objetos en la cámara
/// </summary>
public class ObjectInfoGrabDropController : MonoBehaviour
{
    [Header("Grab Drop Configuration")]
    [SerializeField] private GameObject panelObjetoAgarrado;
    [SerializeField] private TextMeshProUGUI textoObjetoAgarrado;
    [SerializeField] private Button botonAbandonar;
    
    [Header("Debug Configuration")]
    [SerializeField] private bool debugAgarrarDetallado = true;
    
    // Estado del sistema
    private string objetoAgarradoID = "";
    private GameObject objetoClonAgarrado;
    private Dictionary<string, List<string>> objetosColocadosEnDestino = new Dictionary<string, List<string>>();
    
    // Referencias a otros componentes
    private ObjectInfoUIManager mainManager;
    private GameObjectManager gameObjectManager;
    private ObjectInfoMissionPanel missionPanel;
    
    void Awake()
    {
        mainManager = GetComponent<ObjectInfoUIManager>();
        gameObjectManager = FindObjectOfType<GameObjectManager>();
        missionPanel = GetComponent<ObjectInfoMissionPanel>();
    }
    
    void Start()
    {
        ConfigurarBotonAbandonar();
        OcultarPanelObjetoAgarrado();
    }
    
    private void ConfigurarBotonAbandonar()
    {
        if (botonAbandonar != null)
        {
            botonAbandonar.onClick.RemoveAllListeners();
            botonAbandonar.onClick.AddListener(AbandonarObjeto);
        }
    }
    
    /// <summary>
    /// Agarra un objeto y lo coloca en la cámara
    /// </summary>
    public void AgarrarObjeto(string objetoID)
    {
        Debug.Log($"🎯 [GrabDrop] ===== INICIO PROCESO AGARRAR =====");
        Debug.Log($"🎯 [GrabDrop] ObjetoID recibido: '{objetoID}'");

        // 1. Verificar Camera.main
        if (Camera.main == null)
        {
            Debug.LogError($"❌ [GrabDrop] Camera.main es NULL!");
            return;
        }

        // 2. Verificar GameObjectManager
        if (gameObjectManager == null)
        {
            Debug.LogError($"❌ [GrabDrop] GameObjectManager es NULL!");
            return;
        }

        // 3. Buscar datos del objeto
        GameObjectData datos = gameObjectManager.BuscarObjetoPorId(objetoID);
        if (datos == null)
        {
            Debug.LogError($"❌ [GrabDrop] No se encontraron datos para ID: '{objetoID}'");
            return;
        }

        // 4. GUARDAR EL ID DEL OBJETO AGARRADO
        objetoAgarradoID = objetoID;
        Debug.Log($"✅ [GrabDrop] ID del objeto agarrado guardado: '{objetoAgarradoID}'");

        // 5. Verificar y cargar prefab
        if (string.IsNullOrEmpty(datos.prefab3DPath))
        {
            Debug.LogError($"❌ [GrabDrop] Ruta del prefab3D está vacía para: {datos.nombreEspanol}");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(datos.prefab3DPath);
        if (prefab == null)
        {
            Debug.LogError($"❌ [GrabDrop] No se pudo cargar prefab desde: '{datos.prefab3DPath}'");
            return;
        }

        // 6. Limpiar objeto anterior
        if (objetoClonAgarrado != null) 
        {
            Destroy(objetoClonAgarrado);
        }

        // 7. Crear clon y posicionarlo
        objetoClonAgarrado = Instantiate(prefab);
        objetoClonAgarrado.name = $"AGARRADO_{datos.nombreEspanol}";

        // 8. Configurar posición del objeto agarrado
        ConfigurarPosicionObjetoAgarrado(datos);

        // 9. Configurar colisiones
        ConfigurarColisionesObjetoAgarrado();

        // 10. Mostrar panel temporal
        MostrarPanelObjetoAgarrado(datos.nombreEspanol);

        // 11. Audio feedback
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoAgarrarItem();
        }

        Debug.Log($"🎯 [GrabDrop] ===== PROCESO COMPLETADO =====");
    }
    
    private void ConfigurarPosicionObjetoAgarrado(GameObjectData datos)
    {
        Transform camara = Camera.main.transform;
        
        Vector3 posicion;
        Vector3 rotacion;
        Vector3 escala;
        
        if (datos.usarConfiguracionPersonalizada)
        {
            // Usar configuración específica del objeto
            posicion = datos.posicionAgarradoPersonalizada;
            rotacion = datos.rotacionAgarradaPersonalizada;
            escala = datos.escalaAgarradaPersonalizada;
            
            if (debugAgarrarDetallado)
            {
                Debug.Log($"🎮 [GrabDrop] Usando config personalizada para {datos.nombreEspanol}:");
                Debug.Log($"   Posición: {posicion}");
                Debug.Log($"   Rotación: {rotacion}");
                Debug.Log($"   Escala: {escala}");
            }
        }
        else
        {
            // Configuración por defecto
            posicion = new Vector3(0, -0.2f, 0.5f);
            rotacion = Vector3.zero;
            escala = Vector3.one;
            
            if (debugAgarrarDetallado)
            {
                Debug.Log($"🎮 [GrabDrop] Usando config por defecto para {datos.nombreEspanol}");
            }
        }
        
        // Aplicar transformaciones
        objetoClonAgarrado.transform.SetParent(camara, false);
        objetoClonAgarrado.transform.localPosition = posicion;
        objetoClonAgarrado.transform.localEulerAngles = rotacion;
        objetoClonAgarrado.transform.localScale = escala;
        
        if (debugAgarrarDetallado)
        {
            Debug.Log($"🎮 [GrabDrop] Objeto posicionado en cámara:");
            Debug.Log($"   Pos final: {objetoClonAgarrado.transform.localPosition}");
            Debug.Log($"   Rot final: {objetoClonAgarrado.transform.localEulerAngles}");
            Debug.Log($"   Escala final: {objetoClonAgarrado.transform.localScale}");
        }
    }
    
    private void ConfigurarColisionesObjetoAgarrado()
    {
        // Desactivar todos los colliders del objeto agarrado
        Collider[] colliders = objetoClonAgarrado.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        if (debugAgarrarDetallado && colliders.Length > 0)
        {
            Debug.Log($"🎮 [GrabDrop] {colliders.Length} colliders desactivados en objeto agarrado");
        }
    }
    
    /// <summary>
    /// Coloca el objeto agarrado en un destino
    /// </summary>
    public void ColocarObjetoAgarrado(string objetoDestinoID)
    {
        Debug.Log($"🎯 [GrabDrop] ===== INICIO COLOCACIÓN =====");
        Debug.Log($"🎯 [GrabDrop] Destino ID: '{objetoDestinoID}'");

        if (string.IsNullOrEmpty(objetoAgarradoID))
        {
            Debug.LogWarning("⚠️ [GrabDrop] No hay objeto agarrado para colocar");
            return;
        }

        if (objetoClonAgarrado == null)
        {
            Debug.LogError("❌ [GrabDrop] objetoClonAgarrado es NULL pero objetoAgarradoID no está vacío");
            objetoAgarradoID = "";
            return;
        }

        // Agregar objeto a la lista de colocados
        if (!objetosColocadosEnDestino.ContainsKey(objetoDestinoID))
        {
            objetosColocadosEnDestino[objetoDestinoID] = new List<string>();
        }

        objetosColocadosEnDestino[objetoDestinoID].Add(objetoAgarradoID);

        Debug.Log($"📦 [GrabDrop] Objeto {objetoAgarradoID} colocado en {objetoDestinoID}");
        Debug.Log($"📦 [GrabDrop] Total objetos en {objetoDestinoID}: {objetosColocadosEnDestino[objetoDestinoID].Count}");

        // Crear sprite visual si hay MissionPanel
        if (missionPanel != null)
        {
            missionPanel.CrearSpriteObjetoColocado(objetoAgarradoID);
        }

        // Audio feedback
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoSoltarExitoso();
        }

        // Limpiar objeto 3D agarrado
        Debug.Log($"🧹 [GrabDrop] Destruyendo objeto 3D visual");
        Destroy(objetoClonAgarrado);
        objetoClonAgarrado = null;
        objetoAgarradoID = "";
        OcultarPanelObjetoAgarrado();

        // Actualizar botones
        if (mainManager != null)
        {
            mainManager.ActualizarBotonesDinamicamente();
        }

        Debug.Log($"🎯 [GrabDrop] ===== PROCESO COMPLETADO =====");
    }
    
    /// <summary>
    /// Abandona el objeto agarrado
    /// </summary>
    public void AbandonarObjeto()
    {
        if (objetoClonAgarrado == null) return;

        Debug.Log($"🚫 [GrabDrop] Abandonando objeto: {objetoClonAgarrado.name}");
        
        // Destruir el objeto 3D agarrado
        Destroy(objetoClonAgarrado);
        objetoClonAgarrado = null;
        objetoAgarradoID = "";

        // Ocultar panel temporal
        OcultarPanelObjetoAgarrado();

        // Sonido de cancelar
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();
        }

        // Actualizar botones
        if (mainManager != null)
        {
            mainManager.ActualizarBotonesDinamicamente();
        }
    }
    
    /// <summary>
    /// Muestra el panel temporal cuando se agarra un objeto
    /// </summary>
    private void MostrarPanelObjetoAgarrado(string nombreObjeto)
    {
        if (panelObjetoAgarrado == null) return;

        panelObjetoAgarrado.SetActive(true);

        if (textoObjetoAgarrado != null)
        {
            textoObjetoAgarrado.text = $"Objeto: {nombreObjeto}";
        }
    }

    /// <summary>
    /// Oculta el panel temporal de objeto agarrado
    /// </summary>
    private void OcultarPanelObjetoAgarrado()
    {
        if (panelObjetoAgarrado != null)
        {
            panelObjetoAgarrado.SetActive(false);
        }
    }
    
    /// <summary>
    /// Verifica si hay un objeto actualmente agarrado
    /// </summary>
    public bool TieneObjetoAgarrado()
    {
        return !string.IsNullOrEmpty(objetoAgarradoID) && objetoClonAgarrado != null;
    }
    
    /// <summary>
    /// Obtiene la lista de objetos colocados en un destino específico
    /// </summary>
    public List<string> ObtenerObjetosColocados(string objetoDestinoID)
    {
        if (objetosColocadosEnDestino.ContainsKey(objetoDestinoID))
        {
            return new List<string>(objetosColocadosEnDestino[objetoDestinoID]);
        }
        return new List<string>();
    }
    
    /// <summary>
    /// Limpia todos los objetos colocados
    /// </summary>
    public void LimpiarObjetosColocados()
    {
        objetosColocadosEnDestino.Clear();
        Debug.Log("[GrabDrop] Objetos colocados limpiados");
    }
    
    /// <summary>
    /// Limpia los objetos colocados de un destino específico
    /// </summary>
    public void LimpiarObjetosColocadosDestino(string objetoDestinoID)
    {
        if (objetosColocadosEnDestino.ContainsKey(objetoDestinoID))
        {
            objetosColocadosEnDestino[objetoDestinoID].Clear();
            Debug.Log($"[GrabDrop] Objetos colocados limpiados para {objetoDestinoID}");
        }
    }
    
    // Getters públicos para compatibilidad
    public string ObjetoAgarradoID => objetoAgarradoID;
    public GameObject ObjetoClonAgarrado => objetoClonAgarrado;
    public Dictionary<string, List<string>> ObjetosColocadosEnDestino => objetosColocadosEnDestino;
    
    // Métodos de debug
    [ContextMenu("🎯 Debug Estado Grab/Drop")]
    public void DebugEstadoGrabDrop()
    {
        Debug.Log("=== 🎯 ESTADO GRAB/DROP ===");
        Debug.Log($"Objeto agarrado ID: '{objetoAgarradoID}'");
        Debug.Log($"Objeto clon existe: {objetoClonAgarrado != null}");
        Debug.Log($"Panel agarrado activo: {panelObjetoAgarrado != null && panelObjetoAgarrado.activeInHierarchy}");
        
        Debug.Log($"Objetos colocados en destinos:");
        foreach (var kvp in objetosColocadosEnDestino)
        {
            Debug.Log($"   {kvp.Key}: [{string.Join(", ", kvp.Value)}]");
        }
    }
}