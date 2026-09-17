using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

/// <summary>
/// Fase 3: vincula los audios de los objetos a la tabla ObjectAudio por carpeta e id.
/// </summary>
public static class LocalizacionFase3
{
    const string RutaCatalogo = "Assets/Objetos/ObjetoCatalogo.asset";
    const string TablaObjectAudio = "ObjectAudio";

    // ============================================================
    // Audios por carpeta: Assets/Audio/Objetos/<locale>/
    // Nombre:  <id>.mp3  o  <locale>-<id>.mp3
    // Color:   <id>-color.mp3  o  <locale>-<id>-color.mp3
    // Los ids *_tutorial usan el clip del id base.
    // ============================================================

    const string CarpetaSonidos = "Assets/Audio/Objetos";
    static readonly string[] Locales = { "es", "en", "fr" };
    static readonly string[] Extensiones = { ".mp3", ".wav", ".ogg" };

    [MenuItem("Tremblay/Localización/Fase 3 - Vincular audios de objetos por carpeta")]
    public static void VincularAudiosPorCarpeta()
    {
        var catalogo = AssetDatabase.LoadAssetAtPath<ObjetoCatalogo>(RutaCatalogo);
        var audios = LocalizationEditorSettings.GetAssetTableCollection(TablaObjectAudio);
        if (catalogo == null || audios == null)
        {
            Debug.LogError("[Fase 3] Falta el catálogo o la tabla ObjectAudio.");
            return;
        }

        var sb = new StringBuilder();
        int vinculados = 0;
        var sinClip = new List<string>();

        foreach (var locale in Locales)
        {
            var loc = LocalizationEditorSettings.GetLocale(locale);
            if (loc == null) continue;

            string carpeta = $"{CarpetaSonidos}/{locale}";
            if (!AssetDatabase.IsValidFolder(carpeta)) { sb.AppendLine($"   [{locale}] sin carpeta {carpeta}"); continue; }

            foreach (var o in catalogo.objetos)
            {
                if (o == null || string.IsNullOrEmpty(o.id)) continue;
                string idBase = o.id.EndsWith("_tutorial") ? o.id.Substring(0, o.id.Length - "_tutorial".Length) : o.id;

                var clipNombre = BuscarClip(carpeta, locale, idBase, "");
                var clipColor = BuscarClip(carpeta, locale, idBase, "-color");

                if (clipNombre != null) { audios.AddAssetToTable(loc.Identifier, $"obj.{o.id}.audioNombre", clipNombre); vinculados++; }
                else sinClip.Add($"[{locale}] {o.id} nombre");

                if (clipColor != null) { audios.AddAssetToTable(loc.Identifier, $"obj.{o.id}.audioColor", clipColor); vinculados++; }
            }
        }

        EditorUtility.SetDirty(audios.SharedData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Fase 3] Audios vinculados: {vinculados}.\n{sb}" +
                  (sinClip.Count > 0 ? $"Sin clip de nombre ({sinClip.Count}): {string.Join(", ", sinClip)}" : ""));
    }

    static AudioClip BuscarClip(string carpeta, string locale, string id, string sufijo)
    {
        foreach (var nombre in new[] { $"{id}{sufijo}", $"{locale}-{id}{sufijo}", $"{locale}_{id}{sufijo}" })
            foreach (var ext in Extensiones)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{carpeta}/{nombre}{ext}");
                if (clip != null) return clip;
            }
        return null;
    }

}
