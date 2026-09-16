using System;
using System.Collections.Generic;
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

    // Targets visibles AHORA MISMO. Vuforia solo avisa en los CAMBIOS de estado, así que si un
    // paso del tutorial empieza con el target ya enfocado, el evento no se repetiría y el paso
    // se quedaría atascado. Con este registro se puede consultar el estado actual.
    private static readonly HashSet<string> targetsActivos = new HashSet<string>();

    /// <summary>True si ese target está siendo detectado en este momento.</summary>
    public static bool EstaTargetActivo(string id) =>
        !string.IsNullOrEmpty(id) && targetsActivos.Contains(id);

    /// <summary>True si hay CUALQUIER target detectado en este momento.</summary>
    public static bool HayAlgunTargetActivo() => targetsActivos.Count > 0;

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

        // Al desactivarse (o al cambiar de escena) este target deja de estar visible.
        if (!string.IsNullOrEmpty(targetID))
            targetsActivos.Remove(targetID);
    }

    private void OnStatusChanged(ObserverBehaviour b, TargetStatus status)
    {
        bool tracked = status.Status == Status.TRACKED ||
                       status.Status == Status.EXTENDED_TRACKED ||
                       status.Status == Status.LIMITED;

        if (tracked)
        {
            targetsActivos.Add(targetID);
            OnTargetFound?.Invoke(targetID);
        }
        else
        {
            targetsActivos.Remove(targetID);
            OnTargetLost?.Invoke(targetID);
        }
    }
}
