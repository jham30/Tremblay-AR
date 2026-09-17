using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Fase 5: vuelca el texto y el audio de cada StoryFragment a las tablas Story / StoryAudio
/// (clave story.<fragmentID>) y enlaza el LocalizedString / LocalizedAudioClip del asset.
/// El texto y el audio actuales están en inglés → columna en. Idempotente.
/// </summary>
public static class LocalizacionFase5
{
    const string TablaStory = "Story";
    const string TablaStoryAudio = "StoryAudio";
    const string LocaleOrigen = "en";

    [MenuItem("Tremblay/Localización/Fase 5 - Migrar fragmentos de historia a las tablas (en)")]
    public static void MigrarFragmentos()
    {
        var textos = LocalizationEditorSettings.GetStringTableCollection(TablaStory);
        var audios = LocalizationEditorSettings.GetAssetTableCollection(TablaStoryAudio);
        var locale = LocalizationEditorSettings.GetLocale(LocaleOrigen);
        var tablaTexto = textos?.GetTable(LocaleOrigen) as StringTable;
        if (tablaTexto == null || audios == null || locale == null)
        {
            Debug.LogError("[Fase 5] Faltan las tablas Story/StoryAudio o el locale en. Ejecuta antes la Fase 0.");
            return;
        }

        var sb = new StringBuilder();
        int migrados = 0, saltados = 0, sinId = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:StoryFragment"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var f = AssetDatabase.LoadAssetAtPath<StoryFragment>(ruta);
            if (f == null) continue;

            if (string.IsNullOrWhiteSpace(f.fragmentID))
            {
                sinId++;
                sb.AppendLine($"   SIN fragmentID: {ruta}");
                continue;
            }

            if (f.texto != null && !f.texto.IsEmpty)
            {
                saltados++;
                continue;
            }

            string clave = $"story.{f.fragmentID}";
            Undo.RecordObject(f, "Migrar fragmento a tablas");

            // Texto → Story[en]
            if (!string.IsNullOrEmpty(f.textoFragmento) && !EsPlaceholder(f.textoFragmento))
            {
                var entrada = tablaTexto.GetEntry(clave);
                if (entrada == null) tablaTexto.AddEntry(clave, f.textoFragmento);
                else if (string.IsNullOrEmpty(entrada.Value)) entrada.Value = f.textoFragmento;
            }
            else if (textos.SharedData.GetEntry(clave) == null)
            {
                textos.SharedData.AddKey(clave);
            }

            f.texto = new LocalizedString();
            f.texto.SetReference(textos.SharedData.TableCollectionNameGuid, textos.SharedData.GetEntry(clave).Id);

            // Audio → StoryAudio[en]
            if (f.audioNarracion != null)
                audios.AddAssetToTable(locale.Identifier, clave, f.audioNarracion);
            else if (audios.SharedData.GetEntry(clave) == null)
                audios.SharedData.AddKey(clave);

            f.audio = new LocalizedAudioClip();
            f.audio.SetReference(audios.SharedData.TableCollectionNameGuid, audios.SharedData.GetEntry(clave).Id);

            EditorUtility.SetDirty(f);
            migrados++;
            sb.AppendLine($"   {clave}  texto={(string.IsNullOrEmpty(f.textoFragmento) ? "-" : "ok")}  audio={(f.audioNarracion ? f.audioNarracion.name : "-")}");
        }

        EditorUtility.SetDirty(tablaTexto);
        EditorUtility.SetDirty(textos.SharedData);
        EditorUtility.SetDirty(audios.SharedData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Fase 5] {migrados} fragmentos migrados, {saltados} ya migrados, {sinId} sin fragmentID.\n{sb}");
    }

    static bool EsPlaceholder(string s)
    {
        string t = s.Trim();
        return t.Length == 0 || t.Replace("a", "").Length == 0;
    }
}
