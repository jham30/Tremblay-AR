using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de selección de idiomas de la escena Inicio: qué idioma habla el jugador (nativo)
/// y cuál quiere aprender (meta). Dos filas de tres botones (orden es, en, fr).
///
/// Se abre solo en dos casos: primer arranque (modal, sin cerrar) y cuando el jugador
/// pulsa "Cambiar idiomas" en ajustes desde el juego (vuelve a Inicio con AbrirAlCargar).
/// Vive en el GameController y controla el panel por referencias y CanvasGroup.
/// Los textos del panel salen de la tabla UI en el idioma nativo que se está tocando, para
/// que cambien al instante sin esperar a Continuar.
/// </summary>
public class LanguageSelectorUI : MonoBehaviour
{
    /// <summary>Lo pone SettingsPanelManager antes de cargar Inicio para que el panel se abra solo.</summary>
    public static bool AbrirAlCargar;

    [Header("Panel")]
    [SerializeField] private CanvasGroup panel;

    [Header("Botones de idioma (orden: es, en, fr)")]
    [SerializeField] private Button[] botonesNativo = new Button[3];
    [SerializeField] private Button[] botonesMeta = new Button[3];

    [Header("Acciones")]
    [SerializeField] private Button botonContinuar;
    [Tooltip("Solo visible cuando se abre desde ajustes; en el primer arranque no se puede cerrar.")]
    [SerializeField] private Button botonCerrar;

    [Header("Textos (se rellenan desde la tabla UI)")]
    [SerializeField] private TextMeshProUGUI tituloNativo;
    [SerializeField] private TextMeshProUGUI tituloMeta;
    [SerializeField] private TextMeshProUGUI textoContinuar;
    [SerializeField] private TextMeshProUGUI textoCerrar;

    [Header("Confirmación al cambiar el idioma meta")]
    [SerializeField] private CanvasGroup panelConfirmacion;
    [SerializeField] private TextMeshProUGUI textoConfirmacion;
    [SerializeField] private Button botonConfirmar;
    [SerializeField] private Button botonCancelar;
    [SerializeField] private TextMeshProUGUI textoConfirmar;
    [SerializeField] private TextMeshProUGUI textoCancelar;

    [Header("Sprites por estado")]
    [Tooltip("Idioma elegido. Si se deja vacío se usa el borde Outline en su lugar.")]
    [SerializeField] private Sprite spriteElegido;
    [Tooltip("Idioma que no se puede elegir como meta por ser el nativo (versión apagada).")]
    [SerializeField] private Sprite spriteBloqueado;
    [SerializeField] private Color colorBorde = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float grosorBorde = 4f;

    // Estado normal: el sprite que cada botón ya trae de la escena, capturado en Awake.
    private readonly Dictionary<Button, Sprite> spritesNormales = new Dictionary<Button, Sprite>();

    private enum EstadoBoton { Normal, Elegido, Bloqueado }

    [Header("Debug")]
    [SerializeField] private bool debugSelector = true;

    private string nativoSel;
    private string metaSel;
    private bool modoCambio;

    void Awake()
    {
        for (int i = 0; i < botonesNativo.Length; i++)
        {
            int idx = i;
            if (botonesNativo[i] != null) botonesNativo[i].onClick.AddListener(() => OnNativo(idx));
        }
        for (int i = 0; i < botonesMeta.Length; i++)
        {
            int idx = i;
            if (botonesMeta[i] != null) botonesMeta[i].onClick.AddListener(() => OnMeta(idx));
        }

        foreach (var b in botonesNativo)
            if (b != null && b.image != null) spritesNormales[b] = b.image.sprite;
        foreach (var b in botonesMeta)
            if (b != null && b.image != null) spritesNormales[b] = b.image.sprite;

        if (botonContinuar != null) botonContinuar.onClick.AddListener(OnContinuar);
        if (botonCerrar != null) botonCerrar.onClick.AddListener(Ocultar);
        if (botonConfirmar != null) botonConfirmar.onClick.AddListener(Aplicar);
        if (botonCancelar != null) botonCancelar.onClick.AddListener(OcultarConfirmacion);
    }

    void Start()
    {
        OcultarConfirmacion();

        if (!LanguageManager.TieneSeleccionGuardada)
        {
            nativoSel = null;
            metaSel = null;
            Abrir(conCerrar: false);
        }
        else if (AbrirAlCargar)
        {
            AbrirAlCargar = false;
            nativoSel = LanguageManager.CodigoNativoGuardado;
            metaSel = LanguageManager.CodigoMetaGuardado;
            Abrir(conCerrar: true);
        }
        else
        {
            Ocultar();
        }
    }

    private void Abrir(bool conCerrar)
    {
        modoCambio = conCerrar;
        if (botonCerrar != null) botonCerrar.gameObject.SetActive(conCerrar);

        MostrarCanvasGroup(panel, true);
        Refrescar();

        if (debugSelector)
            Debug.Log($"[LanguageSelectorUI] Panel abierto (modo cambio: {modoCambio})");
    }

    private void Ocultar()
    {
        OcultarConfirmacion();
        MostrarCanvasGroup(panel, false);
    }

    // === Interacción ===

    private void OnNativo(int idx)
    {
        Click();
        nativoSel = LanguageManager.CodigosDisponibles[idx];
        if (metaSel == nativoSel) metaSel = null;
        Refrescar();
    }

    private void OnMeta(int idx)
    {
        string codigo = LanguageManager.CodigosDisponibles[idx];
        if (codigo == nativoSel) return;

        Click();
        metaSel = codigo;
        Refrescar();
    }

    private void OnContinuar()
    {
        Click();
        if (string.IsNullOrEmpty(nativoSel) || string.IsNullOrEmpty(metaSel)) return;

        bool cambiaMeta = modoCambio && metaSel != LanguageManager.CodigoMetaGuardado;
        if (cambiaMeta) MostrarConfirmacion();
        else Aplicar();
    }

    private void Aplicar()
    {
        var lm = LanguageManager.Instance;
        if (lm == null)
        {
            Debug.LogError("[LanguageSelectorUI] No hay LanguageManager en la escena");
            return;
        }

        lm.Guardar(nativoSel, metaSel);
        Ocultar();
    }

    // === Confirmación ===

    private void MostrarConfirmacion()
    {
        var lm = LanguageManager.Instance;
        string metaAnterior = LanguageManager.CodigoMetaGuardado;

        if (textoConfirmacion != null && lm != null)
        {
            string nombreIdioma = lm.TextoNativoEn(nativoSel, $"idiomas.nombre_{metaAnterior}");
            textoConfirmacion.text = lm.TextoNativoEn(nativoSel, "idiomas.aviso_cambio_meta", nombreIdioma);
        }
        if (textoConfirmar != null && lm != null) textoConfirmar.text = lm.TextoNativoEn(nativoSel, "idiomas.si");
        if (textoCancelar != null && lm != null) textoCancelar.text = lm.TextoNativoEn(nativoSel, "idiomas.cancelar");

        MostrarCanvasGroup(panelConfirmacion, true);
    }

    private void OcultarConfirmacion() => MostrarCanvasGroup(panelConfirmacion, false);

    // === Refresco visual ===

    private void Refrescar()
    {
        string idiomaTextos = nativoSel ?? IdiomaDelSistema();
        var lm = LanguageManager.Instance;

        if (lm != null)
        {
            if (tituloNativo != null)   tituloNativo.text   = lm.TextoNativoEn(idiomaTextos, "idiomas.titulo_nativo");
            if (tituloMeta != null)     tituloMeta.text     = lm.TextoNativoEn(idiomaTextos, "idiomas.titulo_meta");
            if (textoContinuar != null) textoContinuar.text = lm.TextoNativoEn(idiomaTextos, "idiomas.continuar");
            if (textoCerrar != null)    textoCerrar.text    = lm.TextoNativoEn(idiomaTextos, "idiomas.cerrar");
        }

        for (int i = 0; i < LanguageManager.CodigosDisponibles.Length; i++)
        {
            string codigo = LanguageManager.CodigosDisponibles[i];

            if (i < botonesNativo.Length && botonesNativo[i] != null)
                Resaltar(botonesNativo[i], codigo == nativoSel ? EstadoBoton.Elegido : EstadoBoton.Normal);

            if (i < botonesMeta.Length && botonesMeta[i] != null)
            {
                bool esElNativo = codigo == nativoSel;
                botonesMeta[i].interactable = !esElNativo;

                Resaltar(botonesMeta[i], esElNativo ? EstadoBoton.Bloqueado
                                       : codigo == metaSel ? EstadoBoton.Elegido
                                       : EstadoBoton.Normal);
            }
        }

        if (botonContinuar != null)
            botonContinuar.interactable = !string.IsNullOrEmpty(nativoSel) && !string.IsNullOrEmpty(metaSel);
    }

    // "Elegido" y "bloqueado" son estados de la app, no de interacción: los estados del Button
    // (Normal, Highlighted, Pressed, Selected, Disabled) no sirven, porque Selected se pierde
    // en cuanto el jugador toca otra cosa. Por eso el sprite se cambia aquí a mano.
    private void Resaltar(Button boton, EstadoBoton estado)
    {
        var img = boton.image;
        if (spriteElegido != null && img != null)
        {
            Sprite normal = spritesNormales.TryGetValue(boton, out var s) ? s : img.sprite;

            img.sprite = estado == EstadoBoton.Elegido ? spriteElegido
                       : estado == EstadoBoton.Bloqueado && spriteBloqueado != null ? spriteBloqueado
                       : normal;

            // Sprite Swap pinta encima con overrideSprite; limpiarlo deja mandar al nuestro.
            img.overrideSprite = null;
            return;
        }

        var outline = boton.GetComponent<Outline>();
        if (outline == null) outline = boton.gameObject.AddComponent<Outline>();

        outline.effectColor = colorBorde;
        outline.effectDistance = new Vector2(grosorBorde, grosorBorde);
        outline.enabled = estado == EstadoBoton.Elegido;
    }

    private static string IdiomaDelSistema()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Spanish: return LanguageManager.CodigoEspanol;
            case SystemLanguage.French:  return LanguageManager.CodigoFrances;
            default:                     return LanguageManager.CodigoIngles;
        }
    }

    private static void MostrarCanvasGroup(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private static void Click()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();
    }

    void OnDestroy()
    {
        foreach (var b in botonesNativo) if (b != null) b.onClick.RemoveAllListeners();
        foreach (var b in botonesMeta)   if (b != null) b.onClick.RemoveAllListeners();
        if (botonContinuar != null) botonContinuar.onClick.RemoveListener(OnContinuar);
        if (botonCerrar != null)    botonCerrar.onClick.RemoveListener(Ocultar);
        if (botonConfirmar != null) botonConfirmar.onClick.RemoveListener(Aplicar);
        if (botonCancelar != null)  botonCancelar.onClick.RemoveListener(OcultarConfirmacion);
    }
}
