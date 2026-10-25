using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 🎬 Animaciones 3D de misión en las cartas AR: reproducir el Timeline al completar una misión
/// y dejar los modelos en su estado de reposo cuando no suena ninguno.
///
/// Flujo (lo llama ObjectInfoUIManager al completar una misión en AR):
///   BuscarDirector → Reproducir → esperar a que termine → VolverAReposo → callback
///   (el callback lanza el fragmento de historia y refresca el panel).
///
/// Estado de reposo, en un solo sitio (VolverAReposo / AplicarReposo):
///  - Pose base de los modelos de la lista (clip + segundo). Hace falta porque un FBX con varios
///    clips se dibuja, sin clip, con la pose que tenía el esqueleto en Blender al exportar.
///  - Lo que un Activation Track enciende es un objeto "de animación" (la vela que entra en la
///    calabaza): se detecta solo y se oculta en reposo.
///  - Los modelos que no están en la lista vuelven a la pose que tenían antes de su primera
///    reproducción (Timeline no revierte los transforms al detenerse).
///
/// Va en el GameController: los modelos y directores se referencian, no llevan componente propio.
/// </summary>
public class AnimacionesMisionAR : MonoBehaviour
{
    public static AnimacionesMisionAR Instance { get; private set; }

    [System.Serializable]
    public class ModeloEnReposo
    {
        [Tooltip("Solo para orientarse en el Inspector (ej. 'Calabaza en su carta').")]
        public string nota;

        [Tooltip("Raíz del modelo animado (la instancia del FBX, la que tiene el Animator).")]
        public GameObject modelo;

        [Tooltip("Clip del que se toma la pose base. Vacío = no se toca la pose.")]
        public AnimationClip clipPoseBase;

        [Tooltip("Segundo del clip que es la pose base (0 = primer fotograma).")]
        public float segundoPoseBase;

        [Tooltip("True = el modelo solo existe durante una animación: en reposo se oculta. " +
                 "No hace falta si ya lo enciende un Activation Track (se detecta solo).")]
        public bool ocultoEnReposo;
    }

    [Header("Reposo")]
    [Tooltip("Ocultar en reposo todo lo que encienden los Activation Track de los Timelines de la escena.")]
    [SerializeField] private bool ocultarObjetosDeActivationTracks = true;

    [Tooltip("Pose base de los modelos principales (y objetos a ocultar que no tengan Activation Track).")]
    [SerializeField] private List<ModeloEnReposo> modelos = new List<ModeloEnReposo>();

    // Objetos detectados en Activation Tracks (props de animación).
    private readonly HashSet<GameObject> objetosDeAnimacion = new HashSet<GameObject>();

    // Pose de los modelos de cada Timeline antes de su primera reproducción (respaldo para
    // los modelos que no están en la lista).
    private readonly Dictionary<PlayableDirector, List<PoseTransform>> posesIniciales =
        new Dictionary<PlayableDirector, List<PoseTransform>>();

    private struct PoseTransform
    {
        public Transform transform;
        public Vector3 posicionLocal;
        public Quaternion rotacionLocal;
        public Vector3 escalaLocal;
    }

    void Awake()
    {
        // Por escena (last-wins): cada escena tiene sus propias cartas.
        Instance = this;
    }

    void Start()
    {
        if (ocultarObjetosDeActivationTracks) DetectarObjetosDeAnimacion();

        // Si se creó al vuelo con una animación ya sonando, no ocultarle sus objetos a mitad.
        if (animacionesEnCurso == 0) AplicarReposo(null);
    }

    private int animacionesEnCurso;

    /// <summary>True mientras suena alguna animación de misión.</summary>
    public bool HayAnimacionEnCurso => animacionesEnCurso > 0;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== Reproducción ==========

    /// <summary>
    /// Busca, dentro del objeto AR tocado, el PlayableDirector cuyo GameObject o asset se llame
    /// como 'nombreTimeline'. Avisa en consola si la misión pide uno que no existe.
    /// </summary>
    public PlayableDirector BuscarDirector(string nombreTimeline, GameObject objetoAR)
    {
        if (string.IsNullOrEmpty(nombreTimeline) || objetoAR == null) return null;

        foreach (var director in objetoAR.GetComponentsInChildren<PlayableDirector>(true))
        {
            if (director.gameObject.name == nombreTimeline) return director;
            if (director.playableAsset != null && director.playableAsset.name == nombreTimeline) return director;
        }

        Debug.LogWarning($"[AnimacionesMisionAR] ⚠️ La misión pide el timeline '{nombreTimeline}', pero no hay " +
                         $"ningún PlayableDirector con ese nombre (ni de GameObject ni de asset) dentro de " +
                         $"'{objetoAR.name}'. La misión se completa sin animación.");
        return null;
    }

    /// <summary>
    /// Reproduce el Timeline, espera a que termine, devuelve los modelos a reposo y llama a
    /// alTerminar.
    /// </summary>
    public void Reproducir(PlayableDirector director, System.Action alTerminar)
    {
        if (director == null) { alTerminar?.Invoke(); return; }
        StartCoroutine(ReproducirCoroutine(director, alTerminar));
    }

    // No se usa el evento 'stopped' del PlayableDirector: con Wrap Mode = Hold el director se queda
    // sosteniendo el último fotograma y el evento no llega, así que se consulta su estado.
    private IEnumerator ReproducirCoroutine(PlayableDirector director, System.Action alTerminar)
    {
        animacionesEnCurso++;
        CapturarPoseInicial(director);

        director.time = 0d;
        director.Play();

        // Un frame para que el director entre en estado Playing antes de empezar a medir.
        yield return null;

        while (director != null &&
               director.state == PlayState.Playing &&
               director.time < director.duration)
        {
            yield return null;
        }

        animacionesEnCurso--;
        if (director == null) yield break;

        VolverAReposo(director);
        alTerminar?.Invoke();
    }

    // ========== Reposo ==========

    /// <summary>
    /// Deja los modelos de un Timeline recién terminado en su estado de reposo. Único punto de
    /// vuelta a reposo tras una animación.
    /// </summary>
    private void VolverAReposo(PlayableDirector director)
    {
        // Suelta el grafo: mientras siga vivo, la Animation Track pisa cualquier valor que
        // escribamos (y si el modelo tiene Animator Controller, le devuelve el control).
        director.Stop();

        // Rebind() devuelve al Animator los valores que tenía al vincularse. Sin esto, un
        // Animator sin Controller se queda sosteniendo el último fotograma de Timeline.
        foreach (Animator animator in AnimatorsDe(director))
        {
            if (animator == null) continue;
            animator.Rebind();
            animator.Update(0f);
        }

        // 1) Respaldo: la pose de antes de la primera reproducción.
        if (posesIniciales.TryGetValue(director, out List<PoseTransform> poses))
        {
            foreach (PoseTransform pose in poses)
            {
                if (pose.transform == null) continue;
                pose.transform.localPosition = pose.posicionLocal;
                pose.transform.localRotation = pose.rotacionLocal;
                pose.transform.localScale = pose.escalaLocal;
            }
        }

        // 2) Lo configurado (pose base de la lista y objetos de animación) manda sobre el respaldo.
        var carta = director.GetComponentInParent<Vuforia.ObserverBehaviour>();
        AplicarReposo(carta != null ? carta.transform : director.transform.parent);

        Debug.Log($"[AnimacionesMisionAR] 🎬 '{director.name}' terminado, modelos en reposo");
    }

    /// <summary>
    /// Aplica el reposo configurado a los modelos que cuelgan de 'raiz' (p. ej. la carta donde
    /// acaba de sonar un Timeline). Con raiz = null, a todos.
    /// </summary>
    public void AplicarReposo(Transform raiz)
    {
        foreach (var m in modelos)
        {
            if (m == null || m.modelo == null) continue;
            if (raiz != null && !m.modelo.transform.IsChildOf(raiz)) continue;

            if (m.clipPoseBase != null)
            {
                // SampleAnimation necesita el objeto activo para escribir la pose.
                bool estabaActivo = m.modelo.activeSelf;
                if (!estabaActivo) m.modelo.SetActive(true);
                m.clipPoseBase.SampleAnimation(m.modelo, Mathf.Clamp(m.segundoPoseBase, 0f, m.clipPoseBase.length));
                if (!estabaActivo) m.modelo.SetActive(false);
            }

            m.modelo.SetActive(!m.ocultoEnReposo && !objetosDeAnimacion.Contains(m.modelo));
        }

        foreach (var go in objetosDeAnimacion)
        {
            if (go == null) continue;
            if (raiz != null && !go.transform.IsChildOf(raiz)) continue;
            go.SetActive(false);
        }
    }

    private void DetectarObjetosDeAnimacion()
    {
        objetosDeAnimacion.Clear();

        foreach (var director in FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include))
        {
            if (director.playableAsset is not TimelineAsset timeline) continue;

            foreach (var pista in timeline.GetOutputTracks())
            {
                if (pista is not ActivationTrack) continue;
                if (director.GetGenericBinding(pista) is GameObject go && go != null)
                    objetosDeAnimacion.Add(go);
            }
        }

        if (objetosDeAnimacion.Count > 0)
            Debug.Log($"[AnimacionesMisionAR] {objetosDeAnimacion.Count} objetos de animación ocultos en reposo: " +
                      string.Join(", ", System.Linq.Enumerable.Select(objetosDeAnimacion, o => o.name)));
    }

    /// <summary>
    /// Guarda la pose de todos los modelos vinculados a un Timeline, antes de reproducirlo por
    /// primera vez. Solo una vez por director: después la pose podría venir alterada.
    /// </summary>
    private void CapturarPoseInicial(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null) return;
        if (posesIniciales.ContainsKey(director)) return;

        var poses = new List<PoseTransform>();

        foreach (PlayableBinding binding in director.playableAsset.outputs)
        {
            if (binding.sourceObject == null) continue;

            Object bindeado = director.GetGenericBinding(binding.sourceObject);

            Transform raiz = null;
            if (bindeado is Animator animator) raiz = animator.transform;
            else if (bindeado is GameObject go) raiz = go.transform;
            else if (bindeado is Component componente) raiz = componente.transform;

            if (raiz == null) continue;

            // Se guarda toda la jerarquía: el clip puede animar huesos/hijos, no solo la raíz.
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                poses.Add(new PoseTransform
                {
                    transform = t,
                    posicionLocal = t.localPosition,
                    rotacionLocal = t.localRotation,
                    escalaLocal = t.localScale
                });
            }
        }

        posesIniciales[director] = poses;
    }

    private static List<Animator> AnimatorsDe(PlayableDirector director)
    {
        var animators = new List<Animator>();
        if (director == null || director.playableAsset == null) return animators;

        foreach (PlayableBinding binding in director.playableAsset.outputs)
        {
            if (binding.sourceObject == null) continue;
            if (director.GetGenericBinding(binding.sourceObject) is Animator animator)
                animators.Add(animator);
        }

        return animators;
    }
}
