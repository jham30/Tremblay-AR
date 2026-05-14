using System;
using UnityEngine;
using Vuforia;

/// <summary>
/// Wrapper reutilizable para eventos de Vuforia.
/// Se adjunta al mismo GameObject que tenga un DefaultObserverEventHandler
/// (o ImageTarget con ObserverBehaviour). Al detectarse/perderse el target
/// notifica al singleton con el ID configurado.
/// </summary>
// [RequireComponent] eliminado para que pueda vivir en ImageTargets del juego
// sin forzar dependencias que bloqueen editar el componente en el inspector.
public class VuforiaTargetTracker : MonoBehaviour
{
    public static event Action<string> OnTargetFound;
    public static event Action<string> OnTargetLost;

    [Tooltip("ID lógico del target (ej: 'vela', 'calabaza'). Si vacío, usa el TargetName del ObserverBehaviour.")]
    [SerializeField] private string targetID;

    private ObserverBehaviour observer;

    void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer == null)
            Debug.LogWarning($"[VuforiaTargetTracker] '{gameObject.name}' no tiene ObserverBehaviour — el tracker no funcionará.");
        else if (string.IsNullOrEmpty(targetID))
            targetID = observer.TargetName;
    }

    void OnEnable()
    {
        if (observer != null)
            observer.OnTargetStatusChanged += OnStatusChanged;
    }

    void OnDisable()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnStatusChanged;
    }

    private void OnStatusChanged(ObserverBehaviour b, TargetStatus status)
    {
        bool tracked = status.Status == Status.TRACKED ||
                       status.Status == Status.EXTENDED_TRACKED ||
                       status.Status == Status.LIMITED;

        if (tracked) OnTargetFound?.Invoke(targetID);
        else         OnTargetLost?.Invoke(targetID);
    }
}
