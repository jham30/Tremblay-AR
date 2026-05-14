using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;
    
    [Header("📚 Referencias")]
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private GameObjectManager gameObjectManager;
    [SerializeField] private StoryUIController storyUI;
    
    [Header("📖 Configuración")]
    [SerializeField] private bool reproducirFragmentosAutomaticamente = true;
    [SerializeField] private bool permitirSaltarFragmentos = true;
    [SerializeField] private float delayEntreFragmentos = 0.5f;
    
    [Header("💾 Sistema de Guardado")]
    [SerializeField] private string nombreArchivoProgreso = "story_progress.json";
    [SerializeField] private bool debugMode = true;
    
    private Queue<StoryFragment> colaFragmentos = new Queue<StoryFragment>();
    private StoryFragment fragmentoActual;
    private bool reproduciendoFragmento = false;
    private HashSet<string> fragmentosVistos = new HashSet<string>();
    private Coroutine reproduccionActual;
    private StoryProgressData progresoActual;
    
    void Awake()
    {
        // Singleton "last wins": la nueva escena reemplaza al viejo DDOL
        // para evitar que queden referencias de inspector colgando.
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InicializarSistema();
    }
    
    void Start()
    {
        BuscarReferencias();
        SuscribirseAEventos();
        CargarProgreso();
    }
    
    private void InicializarSistema()
    {
        progresoActual = new StoryProgressData();
        
        if (debugMode)
            Debug.Log("📖 [StoryManager] Sistema de narrativa inicializado");
    }
    
    private void BuscarReferencias()
    {
        if (missionManager == null)
            missionManager = FindObjectOfType<MissionManager>();
        
        if (gameObjectManager == null)
            gameObjectManager = FindObjectOfType<GameObjectManager>();
        
        if (storyUI == null)
            storyUI = FindObjectOfType<StoryUIController>();
        
        if (storyUI == null)
        {
            Debug.LogError("📖 [StoryManager] No se encontró StoryUIController. Creando UI básico...");
            CrearUIBasico();
        }
    }
    
    private void SuscribirseAEventos()
    {
        if (missionManager != null)
        {
            missionManager.OnMisionesActualizadas += HandleMisionesActualizadas;
            
            if (debugMode)
                Debug.Log("📖 [StoryManager] Suscrito a eventos de MissionManager");
        }
    }
    
    public void ReproducirFragmento(StoryFragment fragmento, bool forzarReproduccion = false)
    {
        if (fragmento == null || !fragmento.EsValido())
        {
            Debug.LogWarning("📖 [StoryManager] Intento de reproducir fragmento inválido");
            return;
        }
        
        if (!forzarReproduccion && fragmentosVistos.Contains(fragmento.fragmentID))
        {
            if (debugMode)
                Debug.Log($"📖 [StoryManager] Fragmento ya visto: {fragmento.fragmentID}");
            return;
        }
        
        if (reproduciendoFragmento)
        {
            colaFragmentos.Enqueue(fragmento);
            
            if (debugMode)
                Debug.Log($"📖 [StoryManager] Fragmento agregado a cola: {fragmento.fragmentID}");
            
            return;
        }
        
        if (reproduccionActual != null)
            StopCoroutine(reproduccionActual);
        
        reproduccionActual = StartCoroutine(ReproducirFragmentoCoroutine(fragmento));
    }
    
    private IEnumerator ReproducirFragmentoCoroutine(StoryFragment fragmento)
    {
        reproduciendoFragmento = true;
        fragmentoActual = fragmento;
        
        if (debugMode)
            Debug.Log($"📖 [StoryManager] ▶️ Reproduciendo: {fragmento.fragmentID}");
        
        PausarGameplay(true);
        
        if (storyUI != null)
        {
            yield return StartCoroutine(storyUI.MostrarFragmento(fragmento));
        }
        else
        {
            yield return StartCoroutine(ReproducirSoloAudio(fragmento));
        }
        
        MarcarFragmentoVisto(fragmento.fragmentID);
        GuardarProgreso();
        PausarGameplay(false);
        
        if (fragmento.siguienteFragmento != null)
        {
            yield return new WaitForSeconds(fragmento.delayAntesSiguiente);
            ReproducirFragmento(fragmento.siguienteFragmento);
        }
        
        reproduciendoFragmento = false;
        fragmentoActual = null;
        
        if (colaFragmentos.Count > 0)
        {
            yield return new WaitForSeconds(delayEntreFragmentos);
            StoryFragment siguiente = colaFragmentos.Dequeue();
            ReproducirFragmento(siguiente);
        }
    }
    
    private IEnumerator ReproducirSoloAudio(StoryFragment fragmento)
    {
        if (fragmento.audioNarracion != null && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoSFX(
                fragmento.audioNarracion, 
                fragmento.volumenAudio
            );
            
            yield return new WaitForSeconds(fragmento.ObtenerDuracionTotal());
        }
        else
        {
            yield return new WaitForSeconds(5f);
        }
    }
    
    private void HandleMisionesActualizadas()
    {
        if (!reproducirFragmentosAutomaticamente) return;
        
        if (debugMode)
            Debug.Log("📖 [StoryManager] Misiones actualizadas, verificando fragmentos...");
    }
    
    public void OnMisionDescifrada(Mission mision, StoryFragment fragmento)
    {
        if (fragmento == null) return;
        
        if (debugMode)
            Debug.Log($"📖 [StoryManager] Misión descifrada: {mision.misionID} → Fragmento: {fragmento.fragmentID}");
        
        StartCoroutine(ReproducirConDelay(fragmento, 1f));
    }
    
    public void OnMisionCompletada(Mission mision, StoryFragment fragmento)
    {
        if (fragmento == null) return;
        
        if (debugMode)
            Debug.Log($"📖 [StoryManager] Misión completada: {mision.misionID} → Fragmento: {fragmento.fragmentID}");
        
        StartCoroutine(ReproducirConDelay(fragmento, 1.5f));
    }
    
    private IEnumerator ReproducirConDelay(StoryFragment fragmento, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReproducirFragmento(fragmento);
    }
    
    private void MarcarFragmentoVisto(string fragmentoID)
    {
        if (!fragmentosVistos.Contains(fragmentoID))
        {
            fragmentosVistos.Add(fragmentoID);
            progresoActual.fragmentosVistos.Add(fragmentoID);
            
            if (debugMode)
                Debug.Log($"📖 [StoryManager] ✅ Fragmento marcado como visto: {fragmentoID}");
        }
    }
    
    public void GuardarProgreso()
    {
        // Guard: si este era el duplicado destruido en Awake, progresoActual es null
        if (progresoActual == null) return;

        try
        {
            progresoActual.fragmentosVistos = fragmentosVistos.ToList();
            progresoActual.ultimaActualizacion = System.DateTime.Now.ToString();
            
            string json = JsonUtility.ToJson(progresoActual, true);
            string ruta = System.IO.Path.Combine(Application.persistentDataPath, nombreArchivoProgreso);
            System.IO.File.WriteAllText(ruta, json);
            
            if (debugMode)
                Debug.Log($"📖 [StoryManager] 💾 Progreso guardado: {fragmentosVistos.Count} fragmentos");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"📖 [StoryManager] Error guardando progreso: {e.Message}");
        }
    }
    
    public void CargarProgreso()
    {
        try
        {
            string ruta = System.IO.Path.Combine(Application.persistentDataPath, nombreArchivoProgreso);
            
            if (System.IO.File.Exists(ruta))
            {
                string json = System.IO.File.ReadAllText(ruta);
                progresoActual = JsonUtility.FromJson<StoryProgressData>(json);
                
                fragmentosVistos = new HashSet<string>(progresoActual.fragmentosVistos);
                
                if (debugMode)
                    Debug.Log($"📖 [StoryManager] 📂 Progreso cargado: {fragmentosVistos.Count} fragmentos vistos");
            }
            else
            {
                progresoActual = new StoryProgressData();
                
                if (debugMode)
                    Debug.Log("📖 [StoryManager] No hay progreso previo, creando nuevo");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"📖 [StoryManager] Error cargando progreso: {e.Message}");
            progresoActual = new StoryProgressData();
        }
    }
    
    private void PausarGameplay(bool pausar)
    {
        if (debugMode)
            Debug.Log($"📖 [StoryManager] Gameplay {(pausar ? "pausado" : "reanudado")}");
    }
    
    public bool FragmentoYaVisto(string fragmentoID)
    {
        return fragmentosVistos.Contains(fragmentoID);
    }
    
    public void SaltarFragmentoActual()
    {
        if (!permitirSaltarFragmentos || !reproduciendoFragmento) return;
        
        if (storyUI != null)
        {
            storyUI.SaltarFragmento();
        }
        
        if (reproduccionActual != null)
        {
            StopCoroutine(reproduccionActual);
            reproduciendoFragmento = false;
        }
    }
    
    public void LimpiarColaFragmentos()
    {
        colaFragmentos.Clear();
        
        if (debugMode)
            Debug.Log("📖 [StoryManager] Cola de fragmentos limpiada");
    }
    
    public int ObtenerFragmentosVistos()
    {
        return fragmentosVistos.Count;
    }
    
    public List<string> ObtenerListaFragmentosVistos()
    {
        return fragmentosVistos.ToList();
    }
    
    [ContextMenu("📖 Debug Estado Completo")]
    public void DebugEstadoCompleto()
    {
        Debug.Log("=== 📖 ESTADO STORY MANAGER ===");
        Debug.Log($"Reproduciendo: {reproduciendoFragmento}");
        Debug.Log($"Fragmento actual: {fragmentoActual?.fragmentID ?? "ninguno"}");
        Debug.Log($"Cola: {colaFragmentos.Count} fragmentos");
        Debug.Log($"Fragmentos vistos: {fragmentosVistos.Count}");
        
        if (fragmentosVistos.Count > 0)
        {
            Debug.Log("Fragmentos vistos:");
            foreach (string id in fragmentosVistos)
            {
                Debug.Log($"  - {id}");
            }
        }
    }
    
    [ContextMenu("🔄 Resetear Progreso")]
    public void ResetearProgreso()
    {
        fragmentosVistos.Clear();
        progresoActual = new StoryProgressData();
        GuardarProgreso();
        
        Debug.Log("📖 [StoryManager] Progreso reseteado");
    }
    
    private void CrearUIBasico()
    {
        GameObject uiObj = new GameObject("StoryUI");
        storyUI = uiObj.AddComponent<StoryUIController>();
        uiObj.transform.SetParent(transform);
        
        Debug.Log("📖 [StoryManager] UI básico creado");
    }
    
    void OnDestroy()
    {
        if (missionManager != null)
        {
            missionManager.OnMisionesActualizadas -= HandleMisionesActualizadas;
        }
        
        GuardarProgreso();
    }
}

[System.Serializable]
public class StoryProgressData
{
    public List<string> fragmentosVistos = new List<string>();
    public string ultimaActualizacion;
}