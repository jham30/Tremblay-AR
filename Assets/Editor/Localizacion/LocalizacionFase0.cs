using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class LocalizacionFase0
{
    const string CarpetaBase = "Assets/Localizacion";
    const string CarpetaLocales = CarpetaBase + "/Locales";
    const string CarpetaTablas = CarpetaBase + "/Tablas";
    const string RutaFuente = "Assets/imageUI/Berlin Sans FB Regular SDF.asset";

    // Locale por defecto solo mientras no exista LanguageManager (Fase 1), que lo sobreescribe.
    const string LocaleInicial = "en";

    static readonly (string codigo, SystemLanguage idioma)[] Idiomas =
    {
        ("es", SystemLanguage.Spanish),
        ("en", SystemLanguage.English),
        ("fr", SystemLanguage.French),
    };

    static readonly string[] TablasString = { "UI", "Story", "Objects", "Missions" };
    static readonly string[] TablasAsset = { "StoryAudio", "ObjectAudio" };

    const string CadenaPrueba = "é à ç ñ á í ó ú ü ê è ô î â û œ Œ É À Ç Ñ ¿ ¡";

    [MenuItem("Tremblay/Localización/Fase 0 - Crear locales y tablas")]
    public static void CrearLocalesYTablas()
    {
        CrearCarpeta(CarpetaBase);
        CrearCarpeta(CarpetaLocales);
        CrearCarpeta(CarpetaTablas);

        var settings = ObtenerOCrearSettings();
        CrearLocales();
        CrearTablas();
        ConfigurarSelectorDeLocale(settings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Localización] Fase 0 lista: {Idiomas.Length} locales, " +
                  $"{TablasString.Length} tablas de texto y {TablasAsset.Length} de assets en {CarpetaBase}. " +
                  "Abre Window > Asset Management > Localization Tables para verlas.");
    }

    [MenuItem("Tremblay/Localización/Fase 0 - Verificar glifos de la fuente")]
    public static void VerificarGlifosFuente()
    {
        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuente);
        if (fuente == null)
        {
            Debug.LogError($"[Localización] No se encontró la fuente en {RutaFuente}");
            return;
        }

        if (fuente.HasCharacters(CadenaPrueba, out uint[] faltantes, searchFallbacks: false, tryAddCharacter: false))
        {
            Debug.Log($"[Localización] ✅ {fuente.name} tiene todos los glifos de prueba: {CadenaPrueba}");
            return;
        }

        var unicos = new HashSet<string>();
        foreach (var codigo in faltantes)
            if (codigo != ' ') unicos.Add($"{(char)codigo} (U+{codigo:X4})");

        Debug.LogWarning($"[Localización] ⚠️ A {fuente.name} le faltan {unicos.Count} glifos: " +
                         string.Join(", ", unicos) +
                         "\nRegenera el atlas con Font Asset Creator (rangos 20-7E,A0-FF,100-17F) y guarda con 'Save' sobre el mismo asset.");
    }

    static LocalizationSettings ObtenerOCrearSettings()
    {
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (settings != null) return settings;

        settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        AssetDatabase.CreateAsset(settings, CarpetaBase + "/LocalizationSettings.asset");
        LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        Debug.Log("[Localización] LocalizationSettings creado y activado.");
        return settings;
    }

    static void CrearLocales()
    {
        foreach (var (codigo, idioma) in Idiomas)
        {
            if (LocalizationEditorSettings.GetLocale(codigo) != null) continue;

            var locale = Locale.CreateLocale(idioma);
            AssetDatabase.CreateAsset(locale, $"{CarpetaLocales}/{locale.LocaleName}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            Debug.Log($"[Localización] Locale creado: {locale.LocaleName} ({codigo})");
        }
    }

    static void CrearTablas()
    {
        foreach (var nombre in TablasString)
        {
            var coleccion = LocalizationEditorSettings.GetStringTableCollection(nombre)
                            ?? LocalizationEditorSettings.CreateStringTableCollection(nombre, CarpetaTablas);
            MarcarPreload(coleccion.SharedData);
        }

        foreach (var nombre in TablasAsset)
        {
            var coleccion = LocalizationEditorSettings.GetAssetTableCollection(nombre)
                            ?? LocalizationEditorSettings.CreateAssetTableCollection(nombre, CarpetaTablas);
            MarcarPreload(coleccion.SharedData);
        }
    }

    // Sin preload los lookups son asíncronos y los textos aparecen vacíos un frame.
    static void MarcarPreload(SharedTableData shared)
    {
        if (shared.Metadata.GetMetadata<PreloadAssetTableMetadata>() != null) return;

        shared.Metadata.AddMetadata(new PreloadAssetTableMetadata
        {
            Behaviour = PreloadAssetTableMetadata.PreloadBehaviour.PreloadAll
        });
        EditorUtility.SetDirty(shared);
    }

    static void ConfigurarSelectorDeLocale(LocalizationSettings settings)
    {
        var selectores = settings.GetStartupLocaleSelectors();
        selectores.Clear();
        selectores.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(LocaleInicial) });
        EditorUtility.SetDirty(settings);
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        Directory.CreateDirectory(ruta);
        AssetDatabase.Refresh();
    }
}
