using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// Vista en memoria de un objeto: definición del catálogo (ObjetoData) más el estado del
/// jugador. Nombre, color y audio salen de las tablas en idioma META.
/// </summary>
[System.Serializable]
public class GameObjectData
{
    public string id;
    public bool guardadoPorJugador;

    [System.NonSerialized] public ObjetoData catalogo;

    public Sprite Sprite2D => catalogo != null ? catalogo.sprite2D : null;
    public GameObject Prefab3D => catalogo != null ? catalogo.prefab3D : null;

    public string NombreMeta =>
        LanguageManager.Instance != null ? LanguageManager.Instance.NombreObjetoMeta(id) : id;

    public string ColorMeta =>
        LanguageManager.Instance != null ? LanguageManager.Instance.ColorObjetoMeta(id) : "";

    public string[] cuentos;

    public bool PerteneceACuento(string cuentoID)
    {
        if (string.IsNullOrEmpty(cuentoID)) return true;
        if (cuentos == null || cuentos.Length == 0) return true;
        return Array.IndexOf(cuentos, cuentoID) >= 0;
    }

    public bool usarConfiguracionPersonalizada;
    public Vector3 posicionAgarradoPersonalizada = new Vector3(0, -0.2f, 0.5f);
    public Vector3 rotacionAgarradaPersonalizada = Vector3.zero;
    public Vector3 escalaAgarradaPersonalizada = Vector3.one;
    public string notasConfiguracion;
}

[System.Serializable]
public class MissionSaveData
{
    public List<string> descifradas = new List<string>();
    public List<string> completadas = new List<string>();
}

// Formato antiguo (objetos_guardados.json). Solo se lee para importar el estado una vez.
[System.Serializable]
public class GameSaveData
{
    public List<GameObjectData> objetos = new List<GameObjectData>();
    public MissionSaveData misiones = new MissionSaveData();
}

// Estado del jugador: solo lo que cambia en partida. Un archivo por cuento e idioma meta.
[System.Serializable]
public class ProgresoSaveData
{
    public List<string> objetosGuardados = new List<string>();
    public MissionSaveData misiones = new MissionSaveData();
}

public class GameObjectManager : MonoBehaviour
{
    public static GameObjectManager Instance { get; private set; }

    [Header("Catálogo")]
    public ObjetoCatalogo catalogo;

    [Header("Guardado")]
    [Tooltip("Archivo del formato antiguo. Si existe al arrancar un slot nuevo, se importa una vez.")]
    public string nombreArchivoAntiguo = "objetos_guardados.json";

    [Header("Lista de Objetos (solo lectura, se construye del catálogo)")]
    public List<GameObjectData> listaObjetos = new List<GameObjectData>();

    [Header("Debug Android")]
    public bool debugAndroid = true;

    [Header("Sistema de Misiones")]
    public MissionManager missionManager;

    public event Action OnDatosCargados;
    // 🎃 Se dispara al guardar un objeto en el inventario (feedback de la calabaza reactiva)
    public event Action<string> OnObjetoGuardado;
    public bool datosCargados { get; private set; } = false;

    public MissionSaveData DatosMisiones { get; private set; } = new MissionSaveData();

    private string RutaArchivoAntiguo => Path.Combine(Application.persistentDataPath, nombreArchivoAntiguo);

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

        StartCoroutine(InicializarDatos());
    }

    private IEnumerator InicializarDatos()
    {
        if (catalogo == null)
        {
            Debug.LogError("[GameObjectManager] Sin ObjetoCatalogo asignado en el Inspector.");
            datosCargados = true;
            OnDatosCargados?.Invoke();
            yield break;
        }

        // Los nombres se leen de las tablas: hay que esperar a que Localization esté lista.
        yield return LocalizationSettings.InitializationOperation;
        CargarDatos();
    }

    // === CARGA / GUARDADO ===

    public void CargarDatos()
    {
        listaObjetos = new List<GameObjectData>();
        foreach (var o in catalogo.objetos)
        {
            if (o == null || string.IsNullOrEmpty(o.id)) continue;
            listaObjetos.Add(new GameObjectData
            {
                id = o.id,
                catalogo = o,
                cuentos = o.cuentos,
                usarConfiguracionPersonalizada = o.usarConfiguracionPersonalizada,
                posicionAgarradoPersonalizada = o.posicionAgarradoPersonalizada,
                rotacionAgarradaPersonalizada = o.rotacionAgarradaPersonalizada,
                escalaAgarradaPersonalizada = o.escalaAgarradaPersonalizada,
                notasConfiguracion = o.notasConfiguracion,
            });
        }

        DatosMisiones = new MissionSaveData();

        ProgresoSaveData progreso = LeerProgreso() ?? ImportarFormatoAntiguo();
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
            Debug.Log($"[GameObjectManager] {listaObjetos.Count} objetos, slot {Path.GetFileName(RutaProgreso)}, " +
                      $"{ObtenerObjetosGuardados().Count} guardados, " +
                      $"misiones descifradas {DatosMisiones.descifradas.Count} / completadas {DatosMisiones.completadas.Count}");

        datosCargados = true;
        OnDatosCargados?.Invoke();
    }

    public void GuardarDatos()
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

    // Primera vez con este slot: si hay un archivo del formato viejo, se importa UNA vez.
    private ProgresoSaveData ImportarFormatoAntiguo()
    {
        if (!File.Exists(RutaArchivoAntiguo)) return null;
        try
        {
            var viejo = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(RutaArchivoAntiguo));
            if (viejo == null) return null;

            var progreso = new ProgresoSaveData
            {
                objetosGuardados = (viejo.objetos ?? new List<GameObjectData>())
                    .Where(o => o.guardadoPorJugador).Select(o => o.id).ToList(),
                misiones = viejo.misiones ?? new MissionSaveData()
            };

            File.Move(RutaArchivoAntiguo, RutaArchivoAntiguo + ".migrado");
            Debug.Log($"[GameObjectManager] Estado importado desde {nombreArchivoAntiguo} → {Path.GetFileName(RutaProgreso)}");
            return progreso;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameObjectManager] No se pudo importar el archivo antiguo: {e.Message}");
            return null;
        }
    }

    // === ESTADO DEL JUGADOR ===

    public bool MarcarComoGuardado(string id)
    {
        GameObjectData objeto = BuscarObjetoPorId(id);
        if (objeto == null)
        {
            Debug.LogWarning($"No se encontró objeto con ID '{id}'.");
            return false;
        }

        objeto.guardadoPorJugador = true;
        GuardarDatos();

        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoObjetoGuardado();

        NotificarObjetoGuardado();
        OnObjetoGuardado?.Invoke(id);
        return true;
    }

    public bool DesmarcarGuardado(string id)
    {
        GameObjectData objeto = BuscarObjetoPorId(id);
        if (objeto == null)
        {
            Debug.LogWarning($"No se encontró objeto con ID '{id}'.");
            return false;
        }

        objeto.guardadoPorJugador = false;
        GuardarDatos();
        NotificarObjetoGuardado();
        return true;
    }

    // === CONSULTAS ===

    public GameObjectData BuscarObjetoPorId(string id) => listaObjetos.Find(obj => obj.id == id);

    public bool ExisteObjeto(string id) => BuscarObjetoPorId(id) != null;

    public List<GameObjectData> ObtenerObjetosGuardados() => listaObjetos.FindAll(obj => obj.guardadoPorJugador);

    public List<GameObjectData> ObtenerObjetosNoGuardados() => listaObjetos.FindAll(obj => !obj.guardadoPorJugador);

    public List<GameObjectData> ObtenerObjetosDelCuentoActual()
    {
        string cuento = CuentoActual.GetCuentoActual();
        if (string.IsNullOrEmpty(cuento)) return new List<GameObjectData>(listaObjetos);
        return listaObjetos.FindAll(obj => obj.PerteneceACuento(cuento));
    }

    public List<GameObjectData> ObtenerObjetosGuardadosDelCuentoActual()
    {
        string cuento = CuentoActual.GetCuentoActual();
        if (string.IsNullOrEmpty(cuento)) return ObtenerObjetosGuardados();
        return listaObjetos.FindAll(obj => obj.guardadoPorJugador && obj.PerteneceACuento(cuento));
    }

    // === PROGRESO DE MISIONES ===

    public void GuardarMisiones(MissionSaveData datosMisiones)
    {
        DatosMisiones = datosMisiones;
        GuardarDatos();
    }

    public MissionSaveData CargarMisiones() => DatosMisiones;

    private void NotificarObjetoGuardado()
    {
        if (missionManager == null) missionManager = MissionManager.Instance;
        missionManager?.ReevaluarMisiones();
    }

    // === CONFIGURACIÓN DE AGARRE ===

    public (Vector3 posicion, Vector3 rotacion, Vector3 escala) ObtenerConfiguracionAgarrado(string objetoID)
    {
        GameObjectData objeto = BuscarObjetoPorId(objetoID);
        if (objeto != null && objeto.usarConfiguracionPersonalizada)
            return (objeto.posicionAgarradoPersonalizada, objeto.rotacionAgarradaPersonalizada, objeto.escalaAgarradaPersonalizada);

        return (new Vector3(0, -0.2f, 0.5f), Vector3.zero, Vector3.one);
    }

    // === ESTADÍSTICAS Y UTILIDADES ===

    public void MostrarEstadisticas()
    {
        int total = listaObjetos.Count;
        int guardados = ObtenerObjetosGuardados().Count;
        Debug.Log($"=== ESTADÍSTICAS ===\n" +
                  $"Total de objetos: {total}\n" +
                  $"Guardados: {guardados}\n" +
                  $"Progreso: {(total > 0 ? (guardados * 100f / total).ToString("F1") : "0")}%\n" +
                  $"Misiones descifradas: {DatosMisiones.descifradas.Count}\n" +
                  $"Misiones completadas: {DatosMisiones.completadas.Count}");
    }

    public void LimpiarTodos()
    {
        foreach (var o in listaObjetos) o.guardadoPorJugador = false;
        DatosMisiones = new MissionSaveData();
        GuardarDatos();
    }

    [ContextMenu("Guardar progreso")]
    public void GuardarDatosManual() => GuardarDatos();

    [ContextMenu("Recargar desde catálogo")]
    public void CargarDatosManual() => CargarDatos();

    [ContextMenu("Mostrar estadísticas")]
    public void MostrarEstadisticasManual() => MostrarEstadisticas();

    [ContextMenu("Limpiar progreso del slot actual")]
    public void LimpiarTodosManual() => LimpiarTodos();

    [ContextMenu("Mostrar ruta del progreso")]
    public void MostrarRutaArchivo() => Debug.Log($"Progreso: {RutaProgreso}");

    [ContextMenu("Abrir carpeta de datos")]
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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
