using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>
/// Idioma nativo (interfaz e historia) e idioma meta (sustantivos y misiones) del jugador.
///
/// Unity Localization tiene UN solo SelectedLocale: aquí siempre es el nativo, así los
/// LocalizeStringEvent de la UI lo siguen solos. El contenido en idioma meta se pide SIEMPRE
/// con locale explícito a través de los helpers de esta clase. Si se llama a
/// GetLocalizedString sin locale, el contenido meta sale en nativo en silencio.
///
/// Los idiomas solo cambian en la escena Inicio. Cada escena lee PlayerPrefs en Awake, y el
/// SelectedLocale inicial lo pone PlayerPrefsLocaleSelector durante el arranque de
/// Localization, antes de que nada se pinte.
/// </summary>
public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public const string CodigoEspanol = "es";
    public const string CodigoIngles = "en";
    public const string CodigoFrances = "fr";
    public static readonly string[] CodigosDisponibles = { CodigoEspanol, CodigoIngles, CodigoFrances };

    const string PrefNativo = "idioma_nativo";
    const string PrefMeta = "idioma_meta";

    const string TablaUI = "UI";
    const string TablaObjects = "Objects";
    const string TablaMissions = "Missions";
    const string TablaStory = "Story";
    const string TablaObjectAudio = "ObjectAudio";
    const string TablaStoryAudio = "StoryAudio";

    [Header("Debug")]
    [SerializeField] private bool debugIdiomas = true;

    public string CodigoNativo { get; private set; }
    public string CodigoMeta { get; private set; }

    public Locale LocaleNativo => ObtenerLocale(CodigoNativo);
    public Locale LocaleMeta => ObtenerLocale(CodigoMeta);

    // === Lectura de PlayerPrefs, usable sin instancia (la necesita el selector de arranque) ===

    public static bool TieneSeleccionGuardada =>
        PlayerPrefs.HasKey(PrefNativo) && PlayerPrefs.HasKey(PrefMeta);

    public static string CodigoNativoGuardado => PlayerPrefs.GetString(PrefNativo, "");
    public static string CodigoMetaGuardado => PlayerPrefs.GetString(PrefMeta, "");

    public static bool EsCodigoValido(string codigo) =>
        System.Array.IndexOf(CodigosDisponibles, codigo) >= 0;

    void Awake()
    {
        // Last-wins: ver comentario en InputRouter.Awake().
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);
        Instance = this;

        CodigoNativo = CodigoNativoGuardado;
        CodigoMeta = CodigoMetaGuardado;

        if (debugIdiomas)
            Debug.Log($"[LanguageManager] Nativo='{CodigoNativo}' Meta='{CodigoMeta}' (guardado: {TieneSeleccionGuardada})");
    }

    /// <summary>
    /// Guarda la pareja de idiomas y aplica el nativo como SelectedLocale. Solo la llama la
    /// UI de selección en Inicio.
    /// </summary>
    public void Guardar(string nativo, string meta)
    {
        if (!EsCodigoValido(nativo) || !EsCodigoValido(meta))
        {
            Debug.LogError($"[LanguageManager] Códigos inválidos: nativo='{nativo}' meta='{meta}'");
            return;
        }
        if (nativo == meta)
        {
            Debug.LogError($"[LanguageManager] Nativo y meta no pueden coincidir: '{nativo}'");
            return;
        }

        CodigoNativo = nativo;
        CodigoMeta = meta;
        PlayerPrefs.SetString(PrefNativo, nativo);
        PlayerPrefs.SetString(PrefMeta, meta);
        PlayerPrefs.Save();

        AplicarLocaleNativo();

        if (debugIdiomas)
            Debug.Log($"[LanguageManager] Guardado nativo='{nativo}' meta='{meta}'");
    }

    private void AplicarLocaleNativo()
    {
        var locale = LocaleNativo;
        if (locale == null)
        {
            Debug.LogWarning($"[LanguageManager] No existe locale '{CodigoNativo}' en AvailableLocales");
            return;
        }
        LocalizationSettings.SelectedLocale = locale;
    }

    // === Helpers de lookup. Todo acceso a las tablas pasa por aquí. ===

    /// <summary>Texto de interfaz en nativo. Atajo estático para los mensajes que escribe el código.</summary>
    public static string T(string clave, params object[] args) =>
        Instance != null ? Instance.TextoNativo(clave, args) : clave;

    public string TextoNativo(string clave, params object[] args) =>
        Texto(TablaUI, clave, LocaleNativo, args);

    public string TextoNativoEn(string codigoLocale, string clave, params object[] args) =>
        Texto(TablaUI, clave, ObtenerLocale(codigoLocale), args);

    public string TextoHistoria(string clave) =>
        Texto(TablaStory, clave, LocaleNativo);

    /// <summary>Nombre o color de un objeto en un locale concreto (lo usa GameObjectManager para rellenar los campos por idioma).</summary>
    public string TextoObjetoEn(string idObjeto, string campo, string codigoLocale) =>
        Texto(TablaObjects, $"obj.{idObjeto}.{campo}", ObtenerLocale(codigoLocale));

    public string NombreObjetoMeta(string idObjeto) =>
        Texto(TablaObjects, $"obj.{idObjeto}.nombre", LocaleMeta);

    public string ColorObjetoMeta(string idObjeto) =>
        Texto(TablaObjects, $"obj.{idObjeto}.color", LocaleMeta);

    public string PlantillaMisionMeta(string misionID) =>
        Texto(TablaMissions, $"mission.{misionID}.template", LocaleMeta);

    public AudioClip AudioNombreObjetoMeta(string idObjeto) =>
        Audio(TablaObjectAudio, $"obj.{idObjeto}.audioNombre", LocaleMeta);

    public AudioClip AudioColorObjetoMeta(string idObjeto) =>
        Audio(TablaObjectAudio, $"obj.{idObjeto}.audioColor", LocaleMeta);

    public AudioClip AudioHistoriaNativo(string clave) =>
        Audio(TablaStoryAudio, clave, LocaleNativo);

    private string Texto(string tabla, string clave, Locale locale, params object[] args)
    {
        if (locale == null) return clave;

        var resultado = LocalizationSettings.StringDatabase.GetLocalizedString(
            tabla, clave, locale, FallbackBehavior.UseProjectSettings, args);

        return string.IsNullOrEmpty(resultado) ? clave : resultado;
    }

    private AudioClip Audio(string tabla, string clave, Locale locale)
    {
        if (locale == null) return null;
        return LocalizationSettings.AssetDatabase.GetLocalizedAsset<AudioClip>(tabla, clave, locale);
    }

    private static Locale ObtenerLocale(string codigo)
    {
        if (string.IsNullOrEmpty(codigo)) return null;
        return LocalizationSettings.AvailableLocales.GetLocale(codigo);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
