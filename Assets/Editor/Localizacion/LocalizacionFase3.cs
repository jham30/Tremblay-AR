using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Fase 3: audios de objetos y vocabulario de colores.
/// Carpeta de audio: Assets/Audio/Objetos/<locale>/
///   Nombre de objeto:  <id>.mp3  o  <locale>-<id>.mp3          (los *_tutorial usan el id base)
///   Color:             color-<color>.mp3  o  <locale>-color-<color>.mp3   (color en minúscula, ej. color-rojo)
/// </summary>
public static class LocalizacionFase3
{
    const string RutaCatalogo = "Assets/Objetos/ObjetoCatalogo.asset";
    const string TablaObjects = "Objects";
    const string TablaObjectAudio = "ObjectAudio";
    const string CarpetaSonidos = "Assets/Audio/Objetos";

    static readonly string[] Locales = { "es", "en", "fr" };
    static readonly string[] Extensiones = { ".mp3", ".wav", ".ogg" };

    // (color, es, en, fr)
    static readonly (ColorObjeto color, string es, string en, string fr)[] Colores =
    {
        (ColorObjeto.Amarillo,   "Amarillo",   "Yellow", "Jaune"),
        (ColorObjeto.Azul,       "Azul",       "Blue",   "Bleu"),
        (ColorObjeto.Rojo,       "Rojo",       "Red",    "Rouge"),
        (ColorObjeto.Verde,      "Verde",      "Green",  "Vert"),
        (ColorObjeto.Violeta,    "Violeta",    "Purple", "Violet"),
        (ColorObjeto.Anaranjado, "Anaranjado", "Orange", "Orange"),
        (ColorObjeto.Blanco,     "Blanco",     "White",  "Blanc"),
        (ColorObjeto.Negro,      "Negro",      "Black",  "Noir"),
        (ColorObjeto.Gris,       "Gris",       "Gray",   "Gris"),
    };

    // ============================================================
    // Colores como vocabulario compartido
    // ============================================================

    [MenuItem("Tremblay/Localización/Fase 3 - Colores: sembrar vocabulario y limpiar entradas por objeto")]
    public static void SembrarColores()
    {
        var textos = LocalizationEditorSettings.GetStringTableCollection(TablaObjects);
        var audios = LocalizationEditorSettings.GetAssetTableCollection(TablaObjectAudio);
        var catalogo = AssetDatabase.LoadAssetAtPath<ObjetoCatalogo>(RutaCatalogo);
        if (textos == null || audios == null || catalogo == null)
        {
            Debug.LogError("[Fase 3] Faltan las tablas Objects/ObjectAudio o el catálogo.");
            return;
        }

        int sembrados = 0;
        foreach (var (color, es, en, fr) in Colores)
        {
            string clave = LanguageManager.ClaveColor(color);
            sembrados += PonerSiVacio(textos, "es", clave, es);
            sembrados += PonerSiVacio(textos, "en", clave, en);
            sembrados += PonerSiVacio(textos, "fr", clave, fr);
            if (audios.SharedData.GetEntry(clave) == null) audios.SharedData.AddKey(clave);
        }

        // Las entradas por objeto (obj.<id>.color / .audioColor) sobran: el color es compartido.
        int borradas = 0;
        foreach (var o in catalogo.objetos)
        {
            if (o == null || string.IsNullOrEmpty(o.id)) continue;
            borradas += Quitar(textos, $"obj.{o.id}.color");
            borradas += Quitar(audios, $"obj.{o.id}.audioColor");
        }

        EditorUtility.SetDirty(textos.SharedData);
        EditorUtility.SetDirty(audios.SharedData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Fase 3] Colores: {sembrados} valores sembrados ({Colores.Length} colores × 3), {borradas} entradas por objeto eliminadas. " +
                  "Asigna el color de cada objeto en su ObjetoData (campo Color).");
    }

    static int Quitar(LocalizationTableCollection col, string clave)
    {
        if (col.SharedData.GetEntry(clave) == null) return 0;
        col.RemoveEntry(clave);
        return 1;
    }

    static int PonerSiVacio(StringTableCollection col, string locale, string clave, string valor)
    {
        var tabla = col.GetTable(locale) as StringTable;
        if (tabla == null) return 0;

        var entrada = tabla.GetEntry(clave);
        if (entrada == null) tabla.AddEntry(clave, valor);
        else if (string.IsNullOrEmpty(entrada.Value)) entrada.Value = valor;
        else return 0;

        EditorUtility.SetDirty(tabla);
        return 1;
    }

    // ============================================================
    // Audios por carpeta
    // ============================================================

    [MenuItem("Tremblay/Localización/Fase 3 - Vincular audios de objetos y colores por carpeta")]
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

                var clip = BuscarClip(carpeta, locale, idBase);
                if (clip != null) { audios.AddAssetToTable(loc.Identifier, $"obj.{o.id}.audioNombre", clip); vinculados++; }
                else sinClip.Add($"[{locale}] {o.id}");
            }

            foreach (ColorObjeto color in Enum.GetValues(typeof(ColorObjeto)))
            {
                if (color == ColorObjeto.Ninguno) continue;
                string nombre = "color-" + color.ToString().ToLowerInvariant();
                var clip = BuscarClip(carpeta, locale, nombre);
                if (clip != null) { audios.AddAssetToTable(loc.Identifier, LanguageManager.ClaveColor(color), clip); vinculados++; }
                else sinClip.Add($"[{locale}] {nombre}");
            }
        }

        EditorUtility.SetDirty(audios.SharedData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Fase 3] Audios vinculados: {vinculados}.\n{sb}" +
                  (sinClip.Count > 0 ? $"Sin clip ({sinClip.Count}): {string.Join(", ", sinClip)}" : ""));
    }

    static AudioClip BuscarClip(string carpeta, string locale, string nombre)
    {
        foreach (var candidato in new[] { nombre, $"{locale}-{nombre}", $"{locale}_{nombre}" })
            foreach (var ext in Extensiones)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{carpeta}/{candidato}{ext}");
                if (clip != null) return clip;
            }
        return null;
    }
}
