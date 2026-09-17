using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class LocalizacionFase1
{
    const string TablaUI = "UI";

    // (clave, es, en, fr). No sobreescribe valores que ya tengan texto.
    static readonly (string clave, string es, string en, string fr)[] Textos =
    {
        ("idiomas.titulo_nativo",
            "¿Qué idioma hablas?",
            "What language do you speak?",
            "Quelle langue parles-tu ?"),
        ("idiomas.titulo_meta",
            "¿Qué idioma quieres aprender?",
            "What language do you want to learn?",
            "Quelle langue veux-tu apprendre ?"),
        ("idiomas.continuar", "Continuar", "Continue", "Continuer"),
        ("idiomas.cerrar", "Cerrar", "Close", "Fermer"),
        ("idiomas.si", "Sí", "Yes", "Oui"),
        ("idiomas.cancelar", "Cancelar", "Cancel", "Annuler"),
        ("idiomas.aviso_cambio_meta",
            "Vas a cambiar el idioma que aprendes. Tu progreso en {0} queda guardado y podrás volver a él. ¿Continuar?",
            "You're about to change the language you're learning. Your {0} progress is saved and you can come back to it. Continue?",
            "Tu vas changer la langue que tu apprends. Ta progression en {0} est sauvegardée et tu pourras y revenir. Continuer ?"),
        ("idiomas.nombre_es", "español", "Spanish", "espagnol"),
        ("idiomas.nombre_en", "inglés", "English", "anglais"),
        ("idiomas.nombre_fr", "francés", "French", "français"),
    };

    [MenuItem("Tremblay/Localización/Fase 1 - Selector PlayerPrefs y textos del panel de idiomas")]
    public static void Ejecutar()
    {
        RegistrarSelectorPlayerPrefs();
        SembrarTextosPanel();

        AssetDatabase.SaveAssets();
        Debug.Log("[Localización] Fase 1 lista: selector PlayerPrefs registrado y textos del panel de idiomas en la tabla UI.");
    }

    [MenuItem("Tremblay/Localización/Borrar idiomas guardados (simular primer arranque)")]
    public static void BorrarIdiomasGuardados()
    {
        PlayerPrefs.DeleteKey("idioma_nativo");
        PlayerPrefs.DeleteKey("idioma_meta");
        PlayerPrefs.Save();
        Debug.Log("[Localización] Idiomas borrados de PlayerPrefs. El próximo Play mostrará el panel de primer arranque.");
    }

    // Va PRIMERO en la lista: si hay idioma guardado manda él; si no, cae al Specific(en).
    static void RegistrarSelectorPlayerPrefs()
    {
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (settings == null)
        {
            Debug.LogError("[Localización] No hay LocalizationSettings activo. Ejecuta antes la Fase 0.");
            return;
        }

        var selectores = settings.GetStartupLocaleSelectors();
        if (selectores.Any(s => s is PlayerPrefsLocaleSelector)) return;

        selectores.Insert(0, new PlayerPrefsLocaleSelector());
        EditorUtility.SetDirty(settings);
        Debug.Log("[Localización] PlayerPrefsLocaleSelector insertado al inicio de los selectores de arranque.");
    }

    static void SembrarTextosPanel()
    {
        var coleccion = LocalizationEditorSettings.GetStringTableCollection(TablaUI);
        if (coleccion == null)
        {
            Debug.LogError($"[Localización] No existe la tabla '{TablaUI}'. Ejecuta antes la Fase 0.");
            return;
        }

        int nuevas = 0;
        foreach (var (clave, es, en, fr) in Textos)
        {
            nuevas += Poner(coleccion, "es", clave, es);
            nuevas += Poner(coleccion, "en", clave, en);
            nuevas += Poner(coleccion, "fr", clave, fr);
        }

        EditorUtility.SetDirty(coleccion.SharedData);
        Debug.Log($"[Localización] Tabla UI: {nuevas} valores sembrados ({Textos.Length} claves × 3 idiomas).");
    }

    static int Poner(StringTableCollection coleccion, string locale, string clave, string valor)
    {
        var tabla = coleccion.GetTable(locale) as StringTable;
        if (tabla == null)
        {
            Debug.LogWarning($"[Localización] La tabla '{TablaUI}' no tiene columna '{locale}'.");
            return 0;
        }

        var entrada = tabla.GetEntry(clave);
        if (entrada == null)
        {
            tabla.AddEntry(clave, valor);
        }
        else if (string.IsNullOrEmpty(entrada.Value))
        {
            entrada.Value = valor;
        }
        else
        {
            return 0;
        }

        EditorUtility.SetDirty(tabla);
        return 1;
    }
}
