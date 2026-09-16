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
    [SerializeField] private string nombreEscenaPrincipal = "halloween";
    [SerializeField] private bool debug = true;

    void Awake()
    {
        if (TutorialProgreso.EstaCompletado())
        {
            if (debug) Debug.Log($"[TutorialBootstrap] Tutorial ya completado → cargando {nombreEscenaPrincipal}");

            // Desactivar los demás roots para que sus Start() no corran.
            // Esto corre en Awake (order -1000), ANTES de que Vuforia inicie,
            // así que no hay motor AR que deinicializar: se carga directo.
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
            {
                if (go != gameObject)
                    go.SetActive(false);
            }

            SceneManager.LoadScene(nombreEscenaPrincipal);
        }
        else if (debug)
        {
            Debug.Log("[TutorialBootstrap] Tutorial no completado, ejecutando escena de tutorial");
        }
    }

    [ContextMenu("🔄 Borrar progreso del tutorial")]
    public void ResetearProgreso()
    {
        TutorialProgreso.Borrar();
        Debug.Log("[TutorialBootstrap] Progreso del tutorial borrado");
    }
}
