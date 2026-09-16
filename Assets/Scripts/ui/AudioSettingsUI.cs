using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🔊 UI del panel de configuración de audio. Enlaza dos sliders (música y efectos) con
/// GlobalAudioManager, muestra el porcentaje, permite silenciar (mute) y restablecer, y
/// reproduce un sonido de prueba al ajustar los efectos.
///
/// Va en el GameController, junto al resto de controladores; los sliders y botones del panel de
/// audio se referencian por Inspector. Como el GameController nunca se desactiva, la
/// sincronización con el audio real no depende de OnEnable: se hace en Start y cada vez que
/// SettingsPanelManager avisa de que se mostró un panel.
///
/// Los volúmenes se guardan en PlayerPrefs desde GlobalAudioManager, así que persisten entre sesiones.
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderEfectos;

    [Header("Porcentaje (opcional)")]
    [SerializeField] private TextMeshProUGUI textoPorcentajeMusica;
    [SerializeField] private TextMeshProUGUI textoPorcentajeEfectos;

    [Header("Mute (opcional)")]
    [SerializeField] private Button botonMuteMusica;
    [SerializeField] private Button botonMuteEfectos;
    [SerializeField] private TextMeshProUGUI iconoMuteMusica;    // muestra 🔊 / 🔇
    [SerializeField] private TextMeshProUGUI iconoMuteEfectos;

    [Header("Restablecer (opcional)")]
    [SerializeField] private Button botonRestablecer;

    [Header("Sonido de prueba")]
    [SerializeField] private bool sonidoPruebaAlAjustar = true;
    [SerializeField] private float cooldownSonidoPrueba = 0.12f;

    [Header("Guardado")]
    [Tooltip("Segundos de espera tras el último cambio antes de escribir PlayerPrefs a disco.")]
    [SerializeField] private float retrasoGuardado = 0.5f;

    private float _ultimoBip = -1f;
    private Coroutine _guardadoPendiente;

    void Awake()
    {
        if (sliderMusica != null)     sliderMusica.onValueChanged.AddListener(OnMusica);
        if (sliderEfectos != null)    sliderEfectos.onValueChanged.AddListener(OnEfectos);
        if (botonMuteMusica != null)  botonMuteMusica.onClick.AddListener(OnMuteMusica);
        if (botonMuteEfectos != null) botonMuteEfectos.onClick.AddListener(OnMuteEfectos);
        if (botonRestablecer != null) botonRestablecer.onClick.AddListener(OnRestablecer);
    }

    void OnEnable()
    {
        SettingsPanelManager.PanelMostrado += OnPanelSettingsMostrado;
        RefrescarUI();
    }

    // Este script vive en el GameController, que nunca se desactiva, así que OnEnable solo
    // corre una vez y puede adelantarse al Awake de GlobalAudioManager. Start garantiza una
    // sincronización con el manager ya creado.
    void Start() => RefrescarUI();

    void OnDisable()
    {
        SettingsPanelManager.PanelMostrado -= OnPanelSettingsMostrado;
        PlayerPrefs.Save();
    }

    // Abrir cualquier panel de configuración vuelve a sincronizar los sliders con el audio real.
    private void OnPanelSettingsMostrado(SettingsPanelManager.SettingsPanel panel) => RefrescarUI();

    /// <summary>
    /// Sincroniza sliders, porcentajes e iconos con el estado actual de GlobalAudioManager.
    /// </summary>
    public void RefrescarUI()
    {
        var am = GlobalAudioManager.Instance;
        if (am == null) return;

        if (sliderMusica != null)  sliderMusica.SetValueWithoutNotify(am.VolumenMusica);
        if (sliderEfectos != null) sliderEfectos.SetValueWithoutNotify(am.VolumenEfectos);

        ActualizarPorcentajes();
        ActualizarIconosMute();
    }

    private void OnMusica(float v)
    {
        GlobalAudioManager.Instance?.CambiarVolumenMusica(v);
        ActualizarPorcentajes();
        ProgramarGuardado();
    }

    private void OnEfectos(float v)
    {
        var am = GlobalAudioManager.Instance;
        am?.CambiarVolumenEfectos(v);
        ActualizarPorcentajes();
        ProgramarGuardado();

        // Sonido de prueba al ajustar (con cooldown), al volumen de efectos actual.
        if (sonidoPruebaAlAjustar && am != null &&
            Time.unscaledTime - _ultimoBip >= cooldownSonidoPrueba)
        {
            _ultimoBip = Time.unscaledTime;
            am.ReproducirSonidoClickBoton();
        }
    }

    private void OnMuteMusica()  { GlobalAudioManager.Instance?.ToggleMuteMusica();  ActualizarIconosMute(); }
    private void OnMuteEfectos() { GlobalAudioManager.Instance?.ToggleMuteEfectos(); ActualizarIconosMute(); }
    private void OnRestablecer() { GlobalAudioManager.Instance?.RestablecerVolumenes(); RefrescarUI(); }

    /// <summary>
    /// Escribe PlayerPrefs a disco poco después del último cambio. Los sliders solo guardan en
    /// memoria (GlobalAudioManager no hace flush al arrastrar), y este componente no se desactiva
    /// al cerrar el panel, así que sin esto el flush dependía de OnApplicationQuit, poco fiable
    /// cuando la app se mata desde el selector de tareas.
    /// </summary>
    private void ProgramarGuardado()
    {
        if (!isActiveAndEnabled) { PlayerPrefs.Save(); return; }

        if (_guardadoPendiente != null) StopCoroutine(_guardadoPendiente);
        _guardadoPendiente = StartCoroutine(GuardarTrasRetraso());
    }

    private IEnumerator GuardarTrasRetraso()
    {
        yield return new WaitForSecondsRealtime(retrasoGuardado);
        PlayerPrefs.Save();
        _guardadoPendiente = null;
    }

    private void ActualizarPorcentajes()
    {
        var am = GlobalAudioManager.Instance;
        if (am == null) return;

        if (textoPorcentajeMusica != null)
            textoPorcentajeMusica.text = Mathf.RoundToInt(am.VolumenMusica * 100f) + "%";
        if (textoPorcentajeEfectos != null)
            textoPorcentajeEfectos.text = Mathf.RoundToInt(am.VolumenEfectos * 100f) + "%";
    }

    private void ActualizarIconosMute()
    {
        var am = GlobalAudioManager.Instance;
        if (am == null) return;

        if (iconoMuteMusica != null)  iconoMuteMusica.text  = am.MusicaSilenciada   ? "🔇" : "🔊";
        if (iconoMuteEfectos != null) iconoMuteEfectos.text = am.EfectosSilenciados ? "🔇" : "🔊";
    }

    void OnDestroy()
    {
        if (sliderMusica != null)     sliderMusica.onValueChanged.RemoveListener(OnMusica);
        if (sliderEfectos != null)    sliderEfectos.onValueChanged.RemoveListener(OnEfectos);
        if (botonMuteMusica != null)  botonMuteMusica.onClick.RemoveListener(OnMuteMusica);
        if (botonMuteEfectos != null) botonMuteEfectos.onClick.RemoveListener(OnMuteEfectos);
        if (botonRestablecer != null) botonRestablecer.onClick.RemoveListener(OnRestablecer);
    }
}
