using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

[System.Serializable]
public class GameObjectData
{
    public string id;
    public string nombreEspanol;
    public string nombreIngles;
    public string colorEspanol;
    public string colorIngles;
    public string audioNombreEspanol;
    public string audioNombreIngles;
    public string audioColorEspanol;
    public string audioColorIngles;
    public string prefab3DPath;
    public string sprite2DPath;
    public bool guardadoPorJugador;

    // Referencia al asset del catálogo cuando la lista se construye desde él. No se serializa.
    [System.NonSerialized] public ObjetoData catalogo;

    // Con catálogo, referencias directas; sin él, la carga por ruta de siempre.
    public Sprite Sprite2D =>
        catalogo != null ? catalogo.sprite2D
        : (string.IsNullOrEmpty(sprite2DPath) ? null : Resources.Load<Sprite>(sprite2DPath));

    public GameObject Prefab3D =>
        catalogo != null ? catalogo.prefab3D
        : (string.IsNullOrEmpty(prefab3DPath) ? null : Resources.Load<GameObject>(prefab3DPath));

    // Lo que ve el jugador va en idioma META (el que aprende). Sin LanguageManager, español.
    public string NombreMeta =>
        LanguageManager.Instance != null ? LanguageManager.Instance.NombreObjetoMeta(id) : nombreEspanol;

    public string ColorMeta =>
        LanguageManager.Instance != null ? LanguageManager.Instance.ColorObjetoMeta(id) : colorEspanol;

    // === Cuentos a los que pertenece este objeto ===
    // Vacío/null = pertenece a todos los cuentos (compat hacia atrás).
    public string[] cuentos;

    public bool PerteneceACuento(string cuentoID)
    {
        if (string.IsNullOrEmpty(cuentoID)) return true;
        if (cuentos == null || cuentos.Length == 0) return true;
        return System.Array.IndexOf(cuentos, cuentoID) >= 0;
    }

    // === NUEVOS CAMPOS PARA POSICIONAMIENTO PERSONALIZADO ===
    [Header("🎯 Configuración Objeto Agarrado")]
    public bool usarConfiguracionPersonalizada = false;
    public Vector3 posicionAgarradoPersonalizada = new Vector3(0, -0.2f, 0.5f);
    public Vector3 rotacionAgarradaPersonalizada = Vector3.zero;
    public Vector3 escalaAgarradaPersonalizada = Vector3.one;
    
    [Header("📱 Notas de Configuración")]
    [TextArea(2, 3)]
    public string notasConfiguracion = "Ajustes específicos para cuando el objeto se agarra a la cámara";

    public GameObjectData()
    {
        id = "";
        nombreEspanol = "";
        nombreIngles = "";
        colorEspanol = "";
        colorIngles = "";
        audioNombreEspanol = "";
        audioNombreIngles = "";
        audioColorEspanol = "";
        audioColorIngles = "";
        prefab3DPath = "";
        sprite2DPath = "";
        guardadoPorJugador = false;
        // Los nuevos campos ya tienen valores por defecto arriba
        usarConfiguracionPersonalizada = false;
        posicionAgarradoPersonalizada = new Vector3(0, -0.2f, 0.5f);
        rotacionAgarradaPersonalizada = Vector3.zero;
        escalaAgarradaPersonalizada = Vector3.one;
        notasConfiguracion = "Configuración por defecto";
    }
}

[System.Serializable]
public class MissionSaveData
{
    public List<string> descifradas = new List<string>();
    public List<string> completadas = new List<string>();
}

[System.Serializable]
public class GameSaveData
{
    public List<GameObjectData> objetos = new List<GameObjectData>();
    public MissionSaveData misiones = new MissionSaveData();
}

// Estado del jugador cuando el catálogo va en ScriptableObjects: solo lo que cambia en partida.
// Un archivo por cuento e idioma meta (progreso_<cuento>_<meta>.json).
[System.Serializable]
public class ProgresoSaveData
{
    public List<string> objetosGuardados = new List<string>();
    public MissionSaveData misiones = new MissionSaveData();
}

public class GameObjectManager : MonoBehaviour
{
    public static GameObjectManager Instance { get; private set; }

    [Header("Configuración del Sistema")]
    public string nombreArchivo = "objetos_guardados.json";
    public string archivoInicialStreamingAssets = "objetos_iniciales.json";

    [Header("Catálogo (Fase 3)")]
    [Tooltip("Activo: la lista se construye desde el ObjetoCatalogo y el JSON solo aporta el estado del jugador. Desactívalo para volver al JSON completo.")]
    public bool usarCatalogoSO = false;
    public ObjetoCatalogo catalogo;

    [Header("Lista de Objetos")]
    public List<GameObjectData> listaObjetos = new List<GameObjectData>();

    [Header("Debug Android")]
    public bool debugAndroid = true;

    [Header("Sistema de Misiones")]
    [Tooltip("Referencia al MissionManager para notificar cambios")]
    public MissionManager missionManager;

    public event Action OnDatosCargados;
    // 🎃 Se dispara al guardar un objeto en el inventario (feedback de la calabaza reactiva)
    public event Action<string> OnObjetoGuardado;
    public bool datosCargados { get; private set; } = false;

    private string rutaArchivo;
    private string RutaArchivo
    {
        get
        {
            if (string.IsNullOrEmpty(rutaArchivo))
                rutaArchivo = Path.Combine(Application.persistentDataPath, nombreArchivo);
            return rutaArchivo;
        }
    }

    // progreso_<cuento>_<meta>.json: cambiar de idioma meta cambia de archivo, nada se pisa.
    private string RutaProgreso
    {
        get
        {
            string cuento = CuentoActual.GetCuentoActual();
            if (string.IsNullOrEmpty(cuento)) cuento = "general";
            string meta = LanguageManager.CodigoMetaGuardado;
            if (string.IsNullOrEmpty(meta)) meta = LanguageManager.CodigoEspanol;
            return Path.Combine(Application.persistentDataPath, $"progreso_{cuento}_{meta}.json");
        }
    }

    private string RutaArchivoInicial
    {
        get
        {
            return Path.Combine(Application.streamingAssetsPath, archivoInicialStreamingAssets);
        }
    }

    // Datos de misiones separados
    public MissionSaveData DatosMisiones { get; private set; } = new MissionSaveData();

    void Awake()
    {
        // Last-wins: ver comentario en InputRouter.Awake(). GameObjectManager
        // se buscaba en toda la escena con FindObjectOfType<GameObjectManager>()
        // desde StoryManager, MissionManager, ObjectInfoUIManager, etc. — sin
        // singleton, esa búsqueda podía devolver la instancia vieja (todavía
        // viva ese mismo frame por compartir GameObject con StoryManager DDOL)
        // en vez de la nueva, dejando toda la escena nueva sin inicializar
        // correctamente (paneles sin ocultar, fragmentos de historia sin disparar).
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);
        Instance = this;

        if (debugAndroid) Debug.Log("[GameObjectManager] Awake - Iniciando carga de datos...");
        StartCoroutine(InicializarDatos());
    }

    private IEnumerator InicializarDatos()
    {
        if (usarCatalogoSO)
        {
            if (catalogo == null)
            {
                Debug.LogError("[GameObjectManager] usarCatalogoSO activo pero sin ObjetoCatalogo asignado; se usa el JSON.");
            }
            else
            {
                yield return UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation;
                CargarDesdeCatalogo();
                yield break;
            }
        }

        if (debugAndroid) Debug.Log($"[GameObjectManager] Verificando archivo en: {RutaArchivo}");
        if (debugAndroid) Debug.Log($"[GameObjectManager] StreamingAssets: {RutaArchivoInicial}");

        if (!File.Exists(RutaArchivo))
        {
            if (debugAndroid) Debug.Log("[GameObjectManager] Archivo no existe en persistentDataPath, copiando desde StreamingAssets...");
            yield return StartCoroutine(CopiarArchivoInicial());
        }

        CargarDatos();
    }

    private IEnumerator CopiarArchivoInicial()
    {
        string rutaOrigen = RutaArchivoInicial;

        if (debugAndroid) Debug.Log($"[GameObjectManager] Intentando copiar desde: {rutaOrigen}");

        if (Application.platform == RuntimePlatform.Android)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(rutaOrigen))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        File.WriteAllText(RutaArchivo, request.downloadHandler.text);
                        if (debugAndroid) Debug.Log("[GameObjectManager] Archivo copiado exitosamente desde StreamingAssets (Android)");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GameObjectManager] Error escribiendo archivo: {e.Message}");
                        CrearDatosIniciales();
                    }
                }
                else
                {
                    Debug.LogWarning($"[GameObjectManager] No se pudo cargar desde StreamingAssets: {request.error}");
                    CrearDatosIniciales();
                }
            }
        }
        else
        {
            try
            {
                if (File.Exists(rutaOrigen))
                {
                    string contenido = File.ReadAllText(rutaOrigen);
                    File.WriteAllText(RutaArchivo, contenido);
                    if (debugAndroid) Debug.Log("[GameObjectManager] Archivo copiado exitosamente desde StreamingAssets (Editor)");
                }
                else
                {
                    Debug.LogWarning("[GameObjectManager] Archivo no existe en StreamingAssets, creando datos iniciales...");
                    CrearDatosIniciales();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameObjectManager] Error copiando archivo: {e.Message}");
                CrearDatosIniciales();
            }
        }
    }

    // === CATÁLOGO (paso 3b) ===
    // La lista sale del ObjetoCatalogo; del JSON solo se mezcla el estado (guardado, misiones)
    // por id. Los consumidores siguen viendo GameObjectData, con las rutas y los nombres por
    // idioma rellenados desde el asset y las tablas.
    private void CargarDesdeCatalogo()
    {
        var lm = LanguageManager.Instance;
        if (lm == null)
            Debug.LogWarning("[GameObjectManager] Sin LanguageManager en la escena: los nombres saldrán como claves.");

        listaObjetos = new List<GameObjectData>();
        foreach (var o in catalogo.objetos)
        {
            if (o == null || string.IsNullOrEmpty(o.id)) continue;
            listaObjetos.Add(ConstruirDesdeCatalogo(o, lm));
        }

        DatosMisiones = new MissionSaveData();

        ProgresoSaveData progreso = LeerProgreso();
        if (progreso == null)
        {
            // Primera vez con este slot: si hay un archivo del formato viejo, se importa UNA vez.
            GameSaveData viejo = LeerEstadoGuardado();
            if (viejo != null)
            {
                progreso = new ProgresoSaveData
                {
                    objetosGuardados = viejo.objetos.Where(o => o.guardadoPorJugador).Select(o => o.id).ToList(),
                    misiones = viejo.misiones ?? new MissionSaveData()
                };
                try { File.Move(RutaArchivo, RutaArchivo + ".migrado"); }
                catch (Exception e) { Debug.LogWarning($"[GameObjectManager] No se pudo renombrar el archivo viejo: {e.Message}"); }
                Debug.Log($"[GameObjectManager] Estado importado desde {nombreArchivo} → {Path.GetFileName(RutaProgreso)}");
            }
        }

        if (progreso != null)
        {
            foreach (var id in progreso.objetosGuardados)
            {
                var obj = BuscarObjetoPorId(id);
                if (obj != null) obj.guardadoPorJugador = true;
            }
            DatosMisiones = progreso.misiones ?? new MissionSaveData();
        }

        if (debugAndroid)
            Debug.Log($"[GameObjectManager] Catálogo: {listaObjetos.Count} objetos, slot {Path.GetFileName(RutaProgreso)}, " +
                      $"{ObtenerObjetosGuardados().Count} guardados, " +
                      $"misiones descifradas {DatosMisiones.descifradas.Count} / completadas {DatosMisiones.completadas.Count}");

        datosCargados = true;
        OnDatosCargados?.Invoke();
    }

    private static GameObjectData ConstruirDesdeCatalogo(ObjetoData o, LanguageManager lm)
    {
        return new GameObjectData
        {
            id = o.id,
            catalogo = o,
            nombreEspanol = lm != null ? lm.TextoObjetoEn(o.id, "nombre", LanguageManager.CodigoEspanol) : o.id,
            nombreIngles  = lm != null ? lm.TextoObjetoEn(o.id, "nombre", LanguageManager.CodigoIngles)  : o.id,
            colorEspanol  = lm != null ? lm.TextoObjetoEn(o.id, "color",  LanguageManager.CodigoEspanol) : "",
            colorIngles   = lm != null ? lm.TextoObjetoEn(o.id, "color",  LanguageManager.CodigoIngles)  : "",
            audioNombreEspanol = o.audioNombreEsLegacy,
            audioNombreIngles  = o.audioNombreEnLegacy,
            audioColorEspanol  = o.audioColorEsLegacy,
            audioColorIngles   = o.audioColorEnLegacy,
            prefab3DPath = o.prefab3DPathLegacy,
            sprite2DPath = o.sprite2DPathLegacy,
            cuentos = o.cuentos,
            usarConfiguracionPersonalizada = o.usarConfiguracionPersonalizada,
            posicionAgarradoPersonalizada = o.posicionAgarradoPersonalizada,
            rotacionAgarradaPersonalizada = o.rotacionAgarradaPersonalizada,
            escalaAgarradaPersonalizada = o.escalaAgarradaPersonalizada,
            notasConfiguracion = o.notasConfiguracion,
            guardadoPorJugador = false,
        };
    }

    private ProgresoSaveData LeerProgreso()
    {
        if (!File.Exists(RutaProgreso)) return null;
        try
        {
            var p = JsonUtility.FromJson<ProgresoSaveData>(File.ReadAllText(RutaProgreso));
            if (p != null && p.objetosGuardados == null) p.objetosGuardados = new List<string>();
            return p;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameObjectManager] Error leyendo progreso: {e.Message}");
            return null;
        }
    }

    private void GuardarProgreso()
    {
        try
        {
            var p = new ProgresoSaveData
            {
                objetosGuardados = listaObjetos.Where(o => o.guardadoPorJugador).Select(o => o.id).ToList(),
                misiones = DatosMisiones
            };
            File.WriteAllText(RutaProgreso, JsonUtility.ToJson(p, true));
            if (debugAndroid) Debug.Log($"[GameObjectManager] Progreso guardado en: {RutaProgreso}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameObjectManager] Error al guardar progreso: {e.Message}");
        }
    }

    private GameSaveData LeerEstadoGuardado()
    {
        if (!File.Exists(RutaArchivo)) return null;
        try
        {
            var data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(RutaArchivo));
            if (data != null && data.objetos == null) data.objetos = new List<GameObjectData>();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameObjectManager] Error leyendo estado guardado: {e.Message}");
            return null;
        }
    }

    private void CrearDatosIniciales()
    {
        if (debugAndroid) Debug.Log("[GameObjectManager] Creando datos iniciales...");

        listaObjetos = new List<GameObjectData>();
        DatosMisiones = new MissionSaveData();
        AgregarObjetosEjemplo();
        GuardarDatos();

        if (debugAndroid) Debug.Log("[GameObjectManager] Datos iniciales creados y guardados");
    }

    // === CRUD OBJETOS ===
    public void AgregarObjeto(string id, string nombreEsp, string nombreIng,
                             string colorEsp, string colorIng,
                             string audioNomEsp, string audioNomIng,
                             string audioColEsp, string audioColIng,
                             string prefab3D, string sprite2D)
    {
        if (ExisteObjeto(id))
        {
            Debug.LogWarning($"El objeto con ID '{id}' ya existe.");
            return;
        }

        GameObjectData nuevoObjeto = new GameObjectData
        {
            id = id,
            nombreEspanol = nombreEsp,
            nombreIngles = nombreIng,
            colorEspanol = colorEsp,
            colorIngles = colorIng,
            audioNombreEspanol = audioNomEsp,
            audioNombreIngles = audioNomIng,
            audioColorEspanol = audioColEsp,
            audioColorIngles = audioColIng,
            prefab3DPath = prefab3D,
            sprite2DPath = sprite2D,
            guardadoPorJugador = false
        };

        listaObjetos.Add(nuevoObjeto);
        GuardarDatos();
        Debug.Log($"Objeto '{nombreEsp}' agregado exitosamente.");
    }

    public GameObjectData BuscarObjetoPorNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || listaObjetos == null)
        {
            Debug.LogWarning("Nombre vacío o lista de objetos no inicializada.");
            return null;
        }

        var resultado = listaObjetos.FirstOrDefault(o =>
            o.nombreEspanol.Equals(nombre, System.StringComparison.OrdinalIgnoreCase) ||
            o.id.Equals(nombre, System.StringComparison.OrdinalIgnoreCase));

        return resultado;
    }

    public bool MarcarComoGuardado(string id)
    {
        GameObjectData objeto = BuscarObjetoPorId(id);
        if (objeto != null)
        {
            objeto.guardadoPorJugador = true;
            GuardarDatos();
            Debug.Log($"Objeto '{objeto.nombreEspanol}' marcado como guardado.");

            // 🎵 AGREGAR ESTAS LÍNEAS:
            if (GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoObjetoGuardado();
            }

            NotificarObjetoGuardado(id);

            // 🎃 Notificar a la calabaza reactiva (solo al guardar, no al desmarcar)
            OnObjetoGuardado?.Invoke(id);

            return true;
        }
        Debug.LogWarning($"No se encontró objeto con ID '{id}'.");
        return false;
    }

    public bool DesmarcarGuardado(string id)
    {
        GameObjectData objeto = BuscarObjetoPorId(id);
        if (objeto != null)
        {
            objeto.guardadoPorJugador = false;
            GuardarDatos();
            Debug.Log($"Objeto '{objeto.nombreEspanol}' desmarcado como guardado.");

            // Notificar al sistema de misiones
            NotificarObjetoGuardado(id);

            return true;
        }
        Debug.LogWarning($"No se encontró objeto con ID '{id}'.");
        return false;
    }

    public bool EliminarObjeto(string id)
    {
        GameObjectData objeto = BuscarObjetoPorId(id);
        if (objeto != null)
        {
            listaObjetos.Remove(objeto);
            GuardarDatos();
            Debug.Log($"Objeto con ID '{id}' eliminado.");
            return true;
        }
        Debug.LogWarning($"No se encontró objeto con ID '{id}' para eliminar.");
        return false;
    }

    // === CONSULTAS ===
    public GameObjectData BuscarObjetoPorId(string id)
    {
        return listaObjetos.Find(obj => obj.id == id);
    }

    public bool ExisteObjeto(string id) => BuscarObjetoPorId(id) != null;

    public List<GameObjectData> ObtenerObjetosGuardados()
    {
        return listaObjetos.FindAll(obj => obj.guardadoPorJugador);
    }

    public List<GameObjectData> ObtenerObjetosNoGuardados()
    {
        return listaObjetos.FindAll(obj => !obj.guardadoPorJugador);
    }

    // === FILTRADO POR CUENTO ===
    public List<GameObjectData> ObtenerObjetosDelCuentoActual()
    {
        string cuento = CuentoActual.GetCuentoActual();
        if (string.IsNullOrEmpty(cuento)) return new List<GameObjectData>(listaObjetos);
        return listaObjetos.FindAll(obj => obj.PerteneceACuento(cuento));
    }

    public List<GameObjectData> ObtenerObjetosGuardadosDelCuentoActual()
    {
        string cuento = CuentoActual.GetCuentoActual();
        if (string.IsNullOrEmpty(cuento))
            return ObtenerObjetosGuardados();
        return listaObjetos.FindAll(obj => obj.guardadoPorJugador && obj.PerteneceACuento(cuento));
    }

    // Método público para obtener objeto por ID (usado por ObjectInfoUIManager)
    public GameObjectData ObtenerObjetoPorID(string id)
    {
        return BuscarObjetoPorId(id);
    }

    // === GUARDADO / CARGA ===
    public void GuardarDatos()
    {
        if (usarCatalogoSO && catalogo != null)
        {
            GuardarProgreso();
            return;
        }

        try
        {
            GameSaveData data = new GameSaveData
            {
                objetos = listaObjetos,
                misiones = DatosMisiones
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(RutaArchivo, json);
            if (debugAndroid) Debug.Log($"[GameObjectManager] Datos guardados en: {RutaArchivo}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameObjectManager] Error al guardar datos: {e.Message}");
        }
    }

    public void CargarDatos()
    {
        if (usarCatalogoSO && catalogo != null)
        {
            CargarDesdeCatalogo();
            return;
        }

        try
        {
            if (File.Exists(RutaArchivo))
            {
                string json = File.ReadAllText(RutaArchivo);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                listaObjetos = data.objetos ?? new List<GameObjectData>();
                DatosMisiones = data.misiones ?? new MissionSaveData();

                if (debugAndroid) Debug.Log($"[GameObjectManager] Datos cargados desde: {RutaArchivo}");
                if (debugAndroid) Debug.Log($"[GameObjectManager] Misiones cargadas - Descifradas: {DatosMisiones.descifradas.Count}, Completadas: {DatosMisiones.completadas.Count}");
            }
            else
            {
                Debug.LogWarning("[GameObjectManager] No existe archivo de guardado. Lista vacía.");
                listaObjetos = new List<GameObjectData>();
                DatosMisiones = new MissionSaveData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameObjectManager] Error al cargar datos: {e.Message}");
            listaObjetos = new List<GameObjectData>();
            DatosMisiones = new MissionSaveData();
        }

        datosCargados = true;
        OnDatosCargados?.Invoke();
    }

    // === PROGRESO DE MISIONES ===
    public void GuardarMisiones(MissionSaveData datosMisiones)
    {
        DatosMisiones = datosMisiones;
        GuardarDatos();
        if (debugAndroid) Debug.Log($"[GameObjectManager] Misiones guardadas - Descifradas: {DatosMisiones.descifradas.Count}, Completadas: {DatosMisiones.completadas.Count}");
    }

    public MissionSaveData CargarMisiones()
    {
        return DatosMisiones;
    }

    private void NotificarObjetoGuardado(string objetoID)
    {
        if (missionManager != null)
        {
            missionManager.ReevaluarMisiones();
        }
        else
        {
            missionManager = MissionManager.Instance;
            if (missionManager != null)
            {
                missionManager.ReevaluarMisiones();
            }
        }
    }

    // === MÉTODOS PARA POSICIONAMIENTO PERSONALIZADO ===
    /// <summary>
    /// Configura la transformación personalizada para un objeto cuando se agarra
    /// </summary>
    public void ConfigurarTransformacionAgarrado(string objetoID, Vector3 posicion, Vector3 rotacion, Vector3 escala, string notas = "")
    {
        GameObjectData objeto = BuscarObjetoPorId(objetoID);
        if (objeto == null)
        {
            Debug.LogError($"❌ No se encontró objeto con ID: {objetoID}");
            return;
        }

        objeto.usarConfiguracionPersonalizada = true;
        objeto.posicionAgarradoPersonalizada = posicion;
        objeto.rotacionAgarradaPersonalizada = rotacion;
        objeto.escalaAgarradaPersonalizada = escala;
        
        if (!string.IsNullOrEmpty(notas))
        {
            objeto.notasConfiguracion = notas;
        }

        GuardarDatos();
        Debug.Log($"✅ Configuración personalizada aplicada a: {objeto.nombreEspanol}");
        Debug.Log($"   Posición: {posicion}");
        Debug.Log($"   Rotación: {rotacion}");
        Debug.Log($"   Escala: {escala}");
    }

    /// <summary>
    /// Restaura la configuración por defecto para un objeto
    /// </summary>
    public void RestaurarConfiguracionDefectoAgarrado(string objetoID)
    {
        GameObjectData objeto = BuscarObjetoPorId(objetoID);
        if (objeto == null)
        {
            Debug.LogError($"❌ No se encontró objeto con ID: {objetoID}");
            return;
        }

        objeto.usarConfiguracionPersonalizada = false;
        objeto.posicionAgarradoPersonalizada = new Vector3(0, -0.2f, 0.5f);
        objeto.rotacionAgarradaPersonalizada = Vector3.zero;
        objeto.escalaAgarradaPersonalizada = Vector3.one;
        objeto.notasConfiguracion = "Usando configuración por defecto";

        GuardarDatos();
        Debug.Log($"✅ Configuración por defecto restaurada para: {objeto.nombreEspanol}");
    }

    /// <summary>
    /// Obtiene la configuración de transformación para un objeto agarrado
    /// </summary>
    public (Vector3 posicion, Vector3 rotacion, Vector3 escala) ObtenerConfiguracionAgarrado(string objetoID)
    {
        GameObjectData objeto = BuscarObjetoPorId(objetoID);
        if (objeto == null)
        {
            Debug.LogWarning($"⚠️ Objeto no encontrado, usando configuración por defecto");
            return (new Vector3(0, -0.2f, 0.5f), Vector3.zero, Vector3.one);
        }

        if (objeto.usarConfiguracionPersonalizada)
        {
            return (objeto.posicionAgarradoPersonalizada, objeto.rotacionAgarradaPersonalizada, objeto.escalaAgarradaPersonalizada);
        }
        else
        {
            // Configuración por defecto
            return (new Vector3(0, -0.2f, 0.5f), Vector3.zero, Vector3.one);
        }
    }

    /// <summary>
    /// Lista todos los objetos con configuración personalizada
    /// </summary>
    [ContextMenu("📋 Listar Objetos con Configuración Personalizada")]
    public void ListarObjetosConfiguracionPersonalizada()
    {
        Debug.Log("=== 📋 OBJETOS CON CONFIGURACIÓN PERSONALIZADA ===");
        
        var objetosPersonalizados = listaObjetos.Where(obj => obj.usarConfiguracionPersonalizada).ToList();
        
        if (objetosPersonalizados.Count == 0)
        {
            Debug.Log("ℹ️ No hay objetos con configuración personalizada");
            return;
        }

        foreach (var objeto in objetosPersonalizados)
        {
            Debug.Log($"🎯 {objeto.nombreEspanol} (ID: {objeto.id})");
            Debug.Log($"   Posición: {objeto.posicionAgarradoPersonalizada}");
            Debug.Log($"   Rotación: {objeto.rotacionAgarradaPersonalizada}");
            Debug.Log($"   Escala: {objeto.escalaAgarradaPersonalizada}");
            Debug.Log($"   Notas: {objeto.notasConfiguracion}");
            Debug.Log("---");
        }
    }

    // === ESTADÍSTICAS ===
    public void MostrarEstadisticas()
    {
        int totalObjetos = listaObjetos.Count;
        int objetosGuardados = ObtenerObjetosGuardados().Count;
        int objetosNoGuardados = ObtenerObjetosNoGuardados().Count;

        Debug.Log($"=== ESTADÍSTICAS ===");
        Debug.Log($"Total de objetos: {totalObjetos}");
        Debug.Log($"Objetos guardados por jugador: {objetosGuardados}");
        Debug.Log($"Objetos no guardados: {objetosNoGuardados}");
        Debug.Log($"Progreso objetos: {(totalObjetos > 0 ? (objetosGuardados * 100f / totalObjetos).ToString("F1") : "0")}%");
        Debug.Log($"Misiones descifradas: {DatosMisiones.descifradas.Count}");
        Debug.Log($"Misiones completadas: {DatosMisiones.completadas.Count}");
    }

    // === MENÚS CONTEXTUALES ===
    [ContextMenu("Guardar Datos Manualmente")]
    public void GuardarDatosManual() => GuardarDatos();

    [ContextMenu("Cargar Datos Manualmente")]
    public void CargarDatosManual() => CargarDatos();

    [ContextMenu("Mostrar Estadísticas")]
    public void MostrarEstadisticasManual() => MostrarEstadisticas();

    [ContextMenu("Limpiar Todos los Datos")]
    public void LimpiarTodosManual() => LimpiarTodos();

    [ContextMenu("Mostrar Ruta del Archivo")]
    public void MostrarRutaArchivo()
    {
        Debug.Log($"Ruta del archivo JSON: {RutaArchivo}");
        Debug.Log($"Carpeta persistente: {Application.persistentDataPath}");
        Debug.Log($"StreamingAssets: {RutaArchivoInicial}");
    }

    [ContextMenu("Abrir Carpeta de Datos")]
    public void AbrirCarpetaDatos()
    {
#if UNITY_EDITOR_WIN
        System.Diagnostics.Process.Start("explorer.exe", Application.persistentDataPath.Replace('/', '\\'));
#elif UNITY_EDITOR_OSX
        System.Diagnostics.Process.Start("open", Application.persistentDataPath);
#elif UNITY_EDITOR_LINUX
        System.Diagnostics.Process.Start("xdg-open", Application.persistentDataPath);
#endif
    }

    [ContextMenu("Agregar Objetos de Ejemplo")]
    public void AgregarObjetosEjemplo()
    {
        AgregarObjeto("001", "Manzana", "Apple", "Rojo", "Red",
                     "audio_manzana_es.wav", "audio_apple_en.wav",
                     "audio_rojo_es.wav", "audio_red_en.wav",
                     "Prefabs/Manzana3D", "Sprites/ManzanaSprite");

        AgregarObjeto("002", "Pelota", "Ball", "Azul", "Blue",
                     "audio_pelota_es.wav", "audio_ball_en.wav",
                     "audio_azul_es.wav", "audio_blue_en.wav",
                     "Prefabs/Pelota3D", "Sprites/PelotaSprite");

        AgregarObjeto("003", "Casa", "House", "Verde", "Green",
                     "audio_casa_es.wav", "audio_house_en.wav",
                     "audio_verde_es.wav", "audio_green_en.wav",
                     "Prefabs/Casa3D", "Sprites/CasaSprite");
    }

    [ContextMenu("Forzar Copia desde StreamingAssets")]
    public void ForzarCopiaStreamingAssets()
    {
        StartCoroutine(CopiarArchivoInicial());
    }

    public void LimpiarTodos()
    {
        listaObjetos.Clear();
        DatosMisiones = new MissionSaveData();
        GuardarDatos();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}