using UnityEngine;
using UnityEngine.SceneManagement;
using Vuforia;

/// <summary>
/// Componente de DIAGNÓSTICO temporal para depurar el arranque de Vuforia
/// entre escenas (tutorial → juego). Colócalo en el GameController de la
/// escena del juego (halloween).
///
/// Todos los logs usan el tag [VUFORIA_DIAG] para filtrarlos fácil en
/// logcat / Unity console:  adb logcat -s Unity | grep VUFORIA_DIAG
/// o en el Android Logcat de Unity filtra por: VUFORIA_DIAG
/// </summary>
public class VuforiaDiagnostics : MonoBehaviour
{
    private const string TAG = "[VUFORIA_DIAG]";

    private bool suscrito;
    private float tiempoDesdeInicio;
    private float ultimoLogPeriodico;
    private bool cameraStartedReportado;

    void Awake()
    {
        Debug.Log($"{TAG} Awake — escena activa: '{SceneManager.GetActiveScene().name}'");

        var app = VuforiaApplication.Instance;
        if (app == null)
        {
            Debug.LogError($"{TAG} ❌ VuforiaApplication.Instance es NULL en Awake");
            return;
        }

        Debug.Log($"{TAG} Estado inicial → IsInitialized={app.IsInitialized}, IsRunning={app.IsRunning}");

        app.OnVuforiaInitialized += OnInit;
        app.OnVuforiaStarted     += OnStarted;
        suscrito = true;
        Debug.Log($"{TAG} Suscrito a OnVuforiaInitialized + OnVuforiaStarted");

        // Reportar cuántos VuforiaBehaviour / cámaras hay en la escena
        var behaviours = FindObjectsOfType<VuforiaBehaviour>(true);
        Debug.Log($"{TAG} VuforiaBehaviour encontrados en escena: {behaviours.Length}");
        foreach (var b in behaviours)
            Debug.Log($"{TAG}   • VuforiaBehaviour en '{b.gameObject.name}' (activeInHierarchy={b.gameObject.activeInHierarchy}, enabled={b.enabled})");

        var cams = FindObjectsOfType<Camera>(true);
        Debug.Log($"{TAG} Cámaras en escena: {cams.Length}");
        foreach (var c in cams)
            Debug.Log($"{TAG}   • Camera '{c.gameObject.name}' (activeInHierarchy={c.gameObject.activeInHierarchy}, enabled={c.enabled}, tag={c.tag})");
    }

    private void OnInit(VuforiaInitError error)
    {
        if (error == VuforiaInitError.NONE)
            Debug.Log($"{TAG} ✅ OnVuforiaInitialized — SIN error (NONE). Motor inicializado.");
        else
            Debug.LogError($"{TAG} ❌ OnVuforiaInitialized — ERROR: {error}");
    }

    private void OnStarted()
    {
        cameraStartedReportado = true;
        Debug.Log($"{TAG} ✅ OnVuforiaStarted — Vuforia entrega frames de cámara (a los {tiempoDesdeInicio:F2}s del load).");
    }

    void Update()
    {
        tiempoDesdeInicio += Time.unscaledDeltaTime;

        // Log periódico cada 1s durante los primeros 15s para ver la evolución
        if (tiempoDesdeInicio < 15f && tiempoDesdeInicio - ultimoLogPeriodico >= 1f)
        {
            ultimoLogPeriodico = tiempoDesdeInicio;
            var app = VuforiaApplication.Instance;
            if (app != null)
            {
                Debug.Log($"{TAG} [t={tiempoDesdeInicio:F0}s] IsInitialized={app.IsInitialized}, " +
                          $"IsRunning={app.IsRunning}, cameraStarted={cameraStartedReportado}");
            }

            // A los 5s, si no arrancó la cámara, gritar
            if (tiempoDesdeInicio >= 5f && !cameraStartedReportado)
                Debug.LogWarning($"{TAG} ⚠️ Han pasado {tiempoDesdeInicio:F0}s y OnVuforiaStarted NO se ha disparado — la cámara no arrancó.");
        }
    }

    void OnDestroy()
    {
        if (!suscrito) return;
        var app = VuforiaApplication.Instance;
        if (app != null)
        {
            app.OnVuforiaInitialized -= OnInit;
            app.OnVuforiaStarted     -= OnStarted;
        }
        Debug.Log($"{TAG} OnDestroy — desuscrito.");
    }
}
