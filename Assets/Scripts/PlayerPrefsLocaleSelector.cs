using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Selector de arranque de Unity Localization: elige como SelectedLocale el idioma nativo
/// guardado en PlayerPrefs. Corre dentro de la inicialización del paquete, así el locale
/// es correcto desde el primer frame sin depender del orden de Awake. Si no hay selección
/// guardada devuelve null y el paquete pasa al siguiente selector de la lista.
/// </summary>
[Serializable]
public class PlayerPrefsLocaleSelector : IStartupLocaleSelector
{
    public Locale GetStartupLocale(ILocalesProvider availableLocales)
    {
        if (!LanguageManager.TieneSeleccionGuardada) return null;

        string codigo = LanguageManager.CodigoNativoGuardado;
        return LanguageManager.EsCodigoValido(codigo) ? availableLocales.GetLocale(codigo) : null;
    }
}
