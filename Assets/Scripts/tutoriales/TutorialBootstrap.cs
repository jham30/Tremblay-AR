using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Redirige a la escena principal si el tutorial ya fue completado.
/// Colocar en un GameObject raíz de la escena de tutorial.
/// Si el archivo de progreso existe, carga la escena principal inmediatamente.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class TutorialBootstrap : MonoBehaviour
{
    [SerializeField] private string nombreEscenaPrincipal = "MainScene";
    [SerializeField] private string archivoProgreso = "tutorial_completado.json";
    [SerializeField] private bool debug = true;

    void Awake()
    {
        string ruta = Path.Combine(Application.persistentDataPath, archivoProgreso);
        if (File.Exists(ruta))
        {
            if (debug) Debug.Log($"[TutorialBootstrap] Tutorial ya completado → cargando {nombreEscenaPrincipal}");

            // Desactivar los demás roots para que sus Start() no corran
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
            {
                if (go != gameObject)
                    go.SetActive(false);
            }

            // Limpiar cualquier objeto DDOL que haya podido quedar de sesiones anteriores
            // (Vuforia [Debug Updater], GlobalAudioManager, StoryManager, etc.)
            LimpiarDontDestroyOnLoad();

            SceneManager.LoadScene(nombreEscenaPrincipal);
        }
        else if (debug)
        {
            Debug.Log("[TutorialBootstrap] Tutorial no completado, ejecutando escena de tutorial");
        }
    }

    private void LimpiarDontDestroyOnLoad()
    {
        var objetos = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject go in FindObjectsOfType<GameObject>(true))
        {
            if (go.scene.name == "DontDestroyOnLoad")
                objetos.Add(go);
        }
        foreach (GameObject go in objetos)
        {
            if (debug) Debug.Log($"[TutorialBootstrap] 🗑️ Destruyendo DDOL: {go.name}");
            Destroy(go);
        }
    }

    [ContextMenu("🔄 Borrar progreso del tutorial")]
    public void ResetearProgreso()
    {
        string ruta = Path.Combine(Application.persistentDataPath, archivoProgreso);
        if (File.Exists(ruta))
        {
            File.Delete(ruta);
            Debug.Log("[TutorialBootstrap] Progreso del tutorial borrado");
        }
    }
}
