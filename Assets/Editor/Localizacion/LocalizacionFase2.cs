using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

/// <summary>
/// Fase 2: labels de interfaz.
/// - Analizar/Localizar escena: recorre los TMP de la escena (o del prefab abierto), siembra la
///   tabla UI con su texto actual en la columna 'en' y les cablea un LocalizeStringEvent.
///   Salta los TMP que escribe el código (los referenciados desde un campo serializado de
///   cualquier MonoBehaviour), los placeholders y los que forman parte de una instancia de prefab
///   (esos se localizan abriendo el prefab).
/// - Sembrar textos de código: las claves que usan los scripts vía LanguageManager.T().
/// </summary>
public static class LocalizacionFase2
{
    const string TablaUI = "UI";
    const string LocaleOrigen = "en";
    const string PrefijoClave = "ui.";

    static readonly string[] Placeholders = { "new text", "text", "button", "texto", "label" };

    // ============================================================
    // Escena
    // ============================================================

    [MenuItem("Tremblay/Localización/Fase 2 - Analizar escena (sin cambios)")]
    public static void AnalizarEscena() => ProcesarEscena(aplicar: false);

    [MenuItem("Tremblay/Localización/Fase 2 - Localizar escena o prefab abierto")]
    public static void LocalizarEscena() => ProcesarEscena(aplicar: true);

    static void ProcesarEscena(bool aplicar)
    {
        var coleccion = LocalizationEditorSettings.GetStringTableCollection(TablaUI);
        if (coleccion == null)
        {
            Debug.LogError($"[Localización] No existe la tabla '{TablaUI}'. Ejecuta antes la Fase 0.");
            return;
        }
        var tablaOrigen = coleccion.GetTable(LocaleOrigen) as StringTable;
        if (tablaOrigen == null)
        {
            Debug.LogError($"[Localización] La tabla '{TablaUI}' no tiene columna '{LocaleOrigen}'.");
            return;
        }

        var stage = StageUtility.GetCurrentStageHandle();
        bool enPrefab = PrefabStageUtility.GetCurrentPrefabStage() != null;
        var textos = stage.FindComponentsOfType<TextMeshProUGUI>();
        var referenciados = RecogerTMPReferenciadosPorScripts(stage);

        var localizados = new List<string>();
        var yaLocalizados = new List<string>();
        var porCodigo = new List<string>();
        var placeholders = new List<string>();
        var enPrefabInstancia = new Dictionary<string, List<string>>();

        foreach (var tmp in textos.OrderBy(t => Ruta(t.transform)))
        {
            string ruta = Ruta(tmp.transform);
            string texto = tmp.text?.Trim() ?? "";

            if (tmp.GetComponent<LocalizeStringEvent>() != null) { yaLocalizados.Add(ruta); continue; }
            if (referenciados.Contains(tmp)) { porCodigo.Add($"{ruta}  «{texto}»"); continue; }
            if (EsPlaceholder(texto)) { placeholders.Add($"{ruta}  «{texto}»"); continue; }

            if (!enPrefab && PrefabUtility.IsPartOfPrefabInstance(tmp))
            {
                string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tmp);
                if (!enPrefabInstancia.TryGetValue(prefab, out var lista))
                    enPrefabInstancia[prefab] = lista = new List<string>();
                lista.Add($"{ruta}  «{texto}»");
                continue;
            }

            string clave = ClaveParaTexto(coleccion, tablaOrigen, texto, ruta);
            localizados.Add($"{clave}  ←  «{texto}»   ({ruta})");

            if (aplicar) Localizar(tmp, coleccion, tablaOrigen, clave, texto);
        }

        if (aplicar)
        {
            EditorUtility.SetDirty(tablaOrigen);
            EditorUtility.SetDirty(coleccion.SharedData);
            AssetDatabase.SaveAssets();
            foreach (var tmp in textos)
                if (tmp != null) EditorSceneManager.MarkSceneDirty(tmp.gameObject.scene);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[Localización] Fase 2 {(aplicar ? "APLICADA" : "análisis")} en {(enPrefab ? "prefab" : "escena")} · {textos.Length} TMP encontrados");
        Seccion(sb, $"{(aplicar ? "Localizados" : "Se localizarían")} ({localizados.Count})", localizados);
        Seccion(sb, $"Escritos por código, se saltan ({porCodigo.Count})", porCodigo);
        Seccion(sb, $"Placeholders, se saltan ({placeholders.Count})", placeholders);
        Seccion(sb, $"Ya localizados ({yaLocalizados.Count})", yaLocalizados);
        foreach (var kv in enPrefabInstancia)
            Seccion(sb, $"Instancia de prefab, abre y localiza {kv.Key} ({kv.Value.Count})", kv.Value);

        Debug.Log(sb.ToString());
    }

    static void Localizar(TextMeshProUGUI tmp, StringTableCollection coleccion, StringTable tablaOrigen, string clave, string texto)
    {
        if (tablaOrigen.GetEntry(clave) == null)
            tablaOrigen.AddEntry(clave, texto);

        var compartida = coleccion.SharedData.GetEntry(clave);

        var lse = Undo.AddComponent<LocalizeStringEvent>(tmp.gameObject);
        lse.StringReference.SetReference(coleccion.SharedData.TableCollectionNameGuid, compartida.Id);

        // Mismo cableado que hace el paquete con "Localize" en el menú contextual del TMP.
        var setter = typeof(TMP_Text).GetProperty("text").GetSetMethod();
        var accion = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), tmp, setter);
        UnityEventTools.AddPersistentListener(lse.OnUpdateString, accion);
        lse.OnUpdateString.SetPersistentListenerState(lse.OnUpdateString.GetPersistentEventCount() - 1, UnityEventCallState.EditorAndRuntime);

        EditorUtility.SetDirty(lse);
    }

    static HashSet<TextMeshProUGUI> RecogerTMPReferenciadosPorScripts(StageHandle stage)
    {
        var set = new HashSet<TextMeshProUGUI>();
        foreach (var mb in stage.FindComponentsOfType<MonoBehaviour>())
        {
            if (mb == null || mb is LocalizeStringEvent) continue;

            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.objectReferenceValue is TextMeshProUGUI t) set.Add(t);
            }
        }
        return set;
    }

    static bool EsPlaceholder(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Length <= 1) return true;
        if (!texto.Any(char.IsLetter)) return true;
        return Placeholders.Contains(texto.ToLowerInvariant());
    }

    // La clave sale del texto, no de la jerarquía: así "Close" en tres escenas es UNA entrada.
    static string ClaveParaTexto(StringTableCollection coleccion, StringTable tablaOrigen, string texto, string ruta)
    {
        string clave = PrefijoClave + Slug(texto);

        var existente = tablaOrigen.GetEntry(clave);
        if (existente == null || existente.Value == texto) return clave;

        // Mismo slug, texto distinto: desambiguar con la ruta.
        return clave + "_" + Slug(ruta).GetHashCode().ToString("x8").Substring(0, 6);
    }

    static string Slug(string s)
    {
        s = Regex.Replace(s, "<[^>]+>", "");
        s = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        s = new string(s.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        s = Regex.Replace(s, "[^a-z0-9]+", "_").Trim('_');
        return s.Length > 40 ? s.Substring(0, 40).TrimEnd('_') : s;
    }

    static string Ruta(Transform t)
    {
        var partes = new List<string>();
        for (; t != null; t = t.parent) partes.Add(t.name);
        partes.Reverse();
        return string.Join("/", partes);
    }

    static void Seccion(StringBuilder sb, string titulo, List<string> items)
    {
        if (items.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine("── " + titulo);
        foreach (var i in items) sb.AppendLine("   " + i);
    }

    // ============================================================
    // Textos de código
    // ============================================================

    // (clave, es, en, fr). Las que llevan {n:plural:...} se marcan Smart.
    static readonly (string clave, string es, string en, string fr)[] TextosCodigo =
    {
        // MissionManager
        ("mission.selected",
            "Misión seleccionada: {0}", "Selected mission: {0}", "Mission choisie : {0}"),
        ("mission.current",
            "Misión actual: {0}", "Current mission: {0}", "Mission en cours : {0}"),
        ("mission.none_available",
            "No hay misiones disponibles. Recolecta más objetos.",
            "No missions available. Collect more objects.",
            "Aucune mission disponible. Trouve plus d'objets."),
        ("mission.deciphered_go_ar",
            "¡Misión descifrada! Ve al AR para completarla.",
            "Mission deciphered! Go to AR to complete it.",
            "Mission déchiffrée ! Va en RA pour la terminer."),
        ("mission.deciphered_pick_another",
            "¡Misión descifrada! Elige otra o continúa automáticamente.",
            "Mission deciphered! Pick another one or continue automatically.",
            "Mission déchiffrée ! Choisis-en une autre ou continue automatiquement."),
        ("mission.all_deciphered",
            "¡Todas las misiones descifradas! Ve al AR para completarlas.",
            "All missions deciphered! Go to AR to complete them.",
            "Toutes les missions sont déchiffrées ! Va en RA pour les terminer."),
        ("mission.next",
            "Siguiente misión: {0}", "Next mission: {0}", "Mission suivante : {0}"),
        ("mission.summary",
            "Misiones: {0}/{1} completadas | {2} descifradas | {3} disponibles",
            "Missions: {0}/{1} completed | {2} deciphered | {3} available",
            "Missions : {0}/{1} terminées | {2} déchiffrées | {3} disponibles"),

        // Comprobación de misión (descifrado)
        ("check.complete",
            "¡Completa! ({0}/{0})", "Complete! ({0}/{0})", "Terminée ! ({0}/{0})"),
        ("check.needs",
            "Esta misión necesita {0} {0:plural:objeto|objetos}.",
            "This mission needs {0} {0:plural:object|objects}.",
            "Cette mission demande {0} {0:plural:objet|objets}."),
        ("check.none_placed",
            "Aún no has colocado ningún objeto.",
            "You haven't placed any objects yet.",
            "Tu n'as encore placé aucun objet."),
        ("check.still_need",
            "Todavía te falta colocar {0} {0:plural:objeto|objetos}.",
            "You still need to place {0} more {0:plural:object|objects}.",
            "Il te reste {0} {0:plural:objet|objets} à placer."),
        ("check.wrong_spot",
            "{0} {0:plural:objeto está|objetos están} en el lugar equivocado.",
            "{0} {0:plural:object is|objects are} in the wrong spot.",
            "{0} {0:plural:objet est|objets sont} au mauvais endroit."),
        ("check.not_belong",
            "{0} {0:plural:objeto no pertenece|objetos no pertenecen} a esta misión.",
            "{0} {0:plural:object doesn't|objects don't} belong to this mission.",
            "{0} {0:plural:objet n'appartient|objets n'appartiennent} pas à cette mission."),
        ("check.correct",
            "Correctos: {0} de {1}.", "Correct: {0} of {1}.", "Corrects : {0} sur {1}."),

        // Completar en AR
        ("ar.none_brought",
            "Aún no has traído ninguno.", "You haven't brought any yet.", "Tu n'en as encore apporté aucun."),
        ("ar.still_need_bring",
            "Todavía tienes que traer {0} {0:plural:objeto|objetos} más.",
            "You still need to bring {0} more {0:plural:object|objects}.",
            "Il te reste {0} {0:plural:objet|objets} à apporter."),
        ("ar.not_belong_here",
            "{0} {0:plural:objeto no va|objetos no van} aquí.",
            "{0} {0:plural:object doesn't|objects don't} belong here.",
            "{0} {0:plural:objet ne va|objets ne vont} pas ici."),
        ("ar.already_placed",
            "Ese ya lo colocaste.", "You already placed that one.", "Tu l'as déjà placé."),
        ("ar.got_it", "¡Guardado! ✓", "Got it ✓", "C'est bon ✓"),
        ("ar.save", "Guardar", "Get it", "Prendre"),
        ("ar.available_missions",
            "Misiones disponibles:", "Available missions:", "Missions disponibles :"),

        // Estadísticas (panel de reset)
        ("stats.objects", "📦 OBJETOS", "📦 OBJECTS", "📦 OBJETS"),
        ("stats.total", "Total: {0}", "Total: {0}", "Total : {0}"),
        ("stats.saved", "Guardados: {0}", "Saved: {0}", "Enregistrés : {0}"),
        ("stats.progress", "Progreso: {0}%", "Progress: {0}%", "Progression : {0} %"),
        ("stats.missions", "🧩 MISIONES", "🧩 MISSIONS", "🧩 MISSIONS"),
        ("stats.deciphered", "Descifradas: {0}", "Deciphered: {0}", "Déchiffrées : {0}"),
        ("stats.completed", "Completadas: {0}", "Completed: {0}", "Terminées : {0}"),
        ("stats.available", "Disponibles: {0}", "Available: {0}", "Disponibles : {0}"),
        ("stats.congrats",
            "🏆 ¡{0} {0:plural:misión completada|misiones completadas}!",
            "🏆 {0} {0:plural:mission|missions} completed!",
            "🏆 {0} {0:plural:mission terminée|missions terminées} !"),
        ("stats.error",
            "❌ No se pudieron cargar las estadísticas.",
            "❌ Could not load statistics.",
            "❌ Impossible de charger les statistiques."),

        // Reset
        ("reset.confirm.full",
            "🔄 ¿Reiniciar TODO el progreso?\n\nSe borrarán:\n• Todos los objetos guardados\n• El progreso de misiones\n• Los ajustes",
            "🔄 Reset ALL game progress?\n\nThis will delete:\n• All saved objects\n• Mission progress\n• Settings",
            "🔄 Réinitialiser TOUTE la progression ?\n\nCela effacera :\n• Tous les objets enregistrés\n• La progression des missions\n• Les réglages"),
        ("reset.confirm.objects",
            "📦 ¿Reiniciar solo los objetos guardados?\n\nSe borrarán:\n• Los objetos marcados como guardados\n• El progreso de la colección",
            "📦 Reset only saved objects?\n\nThis will delete:\n• Objects marked as saved\n• Collection progress",
            "📦 Réinitialiser seulement les objets enregistrés ?\n\nCela effacera :\n• Les objets marqués comme enregistrés\n• La progression de la collection"),
        ("reset.confirm.missions",
            "🧩 ¿Reiniciar solo el progreso de misiones?\n\nSe borrarán:\n• Las misiones descifradas\n• Las misiones completadas",
            "🧩 Reset only mission progress?\n\nThis will delete:\n• Deciphered missions\n• Completed missions",
            "🧩 Réinitialiser seulement la progression des missions ?\n\nCela effacera :\n• Les missions déchiffrées\n• Les missions terminées"),
        ("reset.cancelled", "❌ Acción cancelada", "❌ Action cancelled", "❌ Action annulée"),
        ("reset.done",
            "✅ Reinicio {0} completado", "✅ {0} reset completed successfully", "✅ Réinitialisation {0} terminée"),
        ("reset.type.full", "completo", "Full", "complète"),
        ("reset.type.objects", "de objetos", "Objects", "des objets"),
        ("reset.type.missions", "de misiones", "Missions", "des missions"),
        ("reset.stats_updated", "📊 Estadísticas actualizadas", "📊 Statistics updated", "📊 Statistiques mises à jour"),

        // Botones toggle y paneles de ajustes
        ("settings.open", "Configuración", "Settings", "Paramètres"),
        ("settings.close", "Cerrar", "Close", "Fermer"),
        ("settings.panel.settings", "Configuración", "Settings", "Paramètres"),
        ("settings.panel.reset_game", "Reiniciar juego", "Reset Game", "Réinitialiser"),
        ("settings.panel.about_us", "Acerca de", "About Us", "À propos"),
        ("settings.panel.exit", "Salir", "Exit", "Quitter"),
        ("missions.show", "Mostrar misiones", "Show Missions", "Voir les missions"),
        ("missions.hide", "Ocultar misiones", "Hide Missions", "Masquer les missions"),
        ("inventory.open", "Abrir", "Open", "Ouvrir"),
        ("inventory.close", "Cerrar", "Close", "Fermer"),

        // Tutorial
        ("tutorial.final",
            "¡Tutorial completado!\n¡Ahora a jugar!",
            "Tutorial complete!\nTime to play!",
            "Tutoriel terminé !\nÀ toi de jouer !"),

        // Estado de cada misión en la lista
        ("missions.state.completed", "Completada en AR", "Completed in AR", "Terminée en RA"),
        ("missions.state.deciphered", "Descifrada - Ve al AR", "Deciphered - Go to AR", "Déchiffrée - Va en RA"),
        ("missions.state.available", "Disponible para descifrar", "Ready to decipher", "Prête à déchiffrer"),
        ("missions.state.locked", "Bloqueada", "Locked", "Verrouillée"),
        ("missions.state.unknown", "Desconocido", "Unknown", "Inconnu"),
    };

    [MenuItem("Tremblay/Localización/Fase 2 - Sembrar textos de código y fallback de locales")]
    public static void SembrarTextosCodigo()
    {
        var coleccion = LocalizationEditorSettings.GetStringTableCollection(TablaUI);
        if (coleccion == null)
        {
            Debug.LogError($"[Localización] No existe la tabla '{TablaUI}'. Ejecuta antes la Fase 0.");
            return;
        }

        int nuevas = 0;
        foreach (var (clave, es, en, fr) in TextosCodigo)
        {
            nuevas += Poner(coleccion, "es", clave, es);
            nuevas += Poner(coleccion, "en", clave, en);
            nuevas += Poner(coleccion, "fr", clave, fr);
        }
        EditorUtility.SetDirty(coleccion.SharedData);

        ConfigurarFallback();

        AssetDatabase.SaveAssets();
        Debug.Log($"[Localización] Textos de código: {nuevas} valores sembrados ({TextosCodigo.Length} claves × 3). Fallback es/fr → en activado.");
    }

    static int Poner(StringTableCollection coleccion, string locale, string clave, string valor)
    {
        var tabla = coleccion.GetTable(locale) as StringTable;
        if (tabla == null) return 0;

        var entrada = tabla.GetEntry(clave);
        bool escrito = false;
        if (entrada == null) { entrada = tabla.AddEntry(clave, valor); escrito = true; }
        else if (string.IsNullOrEmpty(entrada.Value)) { entrada.Value = valor; escrito = true; }

        if (valor.Contains(":plural:") && !entrada.IsSmart) { entrada.IsSmart = true; escrito = true; }

        if (escrito) EditorUtility.SetDirty(tabla);
        return escrito ? 1 : 0;
    }

    // Si a es/fr le falta una entrada, cae al inglés en vez de mostrar el aviso del paquete.
    static void ConfigurarFallback()
    {
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        var en = LocalizationEditorSettings.GetLocale("en");
        if (settings == null || en == null) return;

        foreach (var codigo in new[] { "es", "fr" })
        {
            var locale = LocalizationEditorSettings.GetLocale(codigo);
            if (locale == null || locale.Metadata.GetMetadata<FallbackLocale>() != null) continue;
            locale.Metadata.AddMetadata(new FallbackLocale(en));
            EditorUtility.SetDirty(locale);
        }

        settings.GetStringDatabase().UseFallback = true;
        settings.GetAssetDatabase().UseFallback = true;
        EditorUtility.SetDirty(settings);
    }
}
