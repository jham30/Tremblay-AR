using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

/// <summary>
/// Mete en la tabla StoryAudio los mp3 que genera herramientas/GenerarAudioHistoria.ps1,
/// creando las claves que falten. Las referencias de cada paso del tutorial se asignan
/// a mano en el Inspector: esta herramienta solo deja la tabla lista.
///
/// Carpetas y nombres que espera (los crea el script de generación):
///   Assets/misiones/Halloween/Story/sounds/&lt;locale&gt;/&lt;locale&gt;-story-&lt;sufijo&gt;.mp3
///   Assets/misiones/Halloween/tutorial/sounds/&lt;locale&gt;/&lt;locale&gt;-tutorial-&lt;NN&gt;.mp3
/// </summary>
public static class VincularAudioNarracion
{
    const string TablaAudio = "StoryAudio";

    static readonly string[] Locales = { "es", "en", "fr" };
    static readonly string[] Extensiones = { ".mp3", ".wav", ".ogg" };

    // Carpeta de los mp3, prefijo del nombre de archivo, prefijo de la clave y tabla de
    // texto de la que salen las claves esperadas, para avisar de las que queden sin audio.
    static readonly (string nombre, string carpeta, string prefijoArchivo, string prefijoClave, string tablaTexto)[] Fuentes =
    {
        ("historia", "Assets/misiones/Halloween/Story/sounds",    "story",    "story.",          "Story"),
        ("tutorial", "Assets/misiones/Halloween/tutorial/sounds", "tutorial", "tutorial.step.",  "UI"),
    };

    // Los trozos por hablante (es-story-02-01-nina.mp3) son material para montar a mano
    // en el editor de audio, no para el juego: el que se vincula es el fragmento ya unido.
    static readonly Regex Segmento = new Regex(@"-\d{2}-[a-z]+$");

    [MenuItem("Tremblay/Localización/Vincular audios de narración (historia y tutorial)")]
    public static void Vincular()
    {
        var audios = LocalizationEditorSettings.GetAssetTableCollection(TablaAudio);
        if (audios == null)
        {
            Debug.LogError($"[Narración] No encuentro la tabla {TablaAudio}.");
            return;
        }

        var sb = new StringBuilder();
        int vinculados = 0, clavesNuevas = 0;

        foreach (var fuente in Fuentes)
        {
            sb.AppendLine($"── {fuente.nombre}");

            var esperadas = ClavesEsperadas(fuente.tablaTexto, fuente.prefijoClave);
            var conAudio = new HashSet<string>();

            foreach (var locale in Locales)
            {
                var loc = LocalizationEditorSettings.GetLocale(locale);
                if (loc == null) continue;

                string carpeta = $"{fuente.carpeta}/{locale}";
                if (!AssetDatabase.IsValidFolder(carpeta))
                {
                    sb.AppendLine($"   [{locale}] sin carpeta {carpeta}");
                    continue;
                }

                int n = 0;
                string prefijo = $"{locale}-{fuente.prefijoArchivo}-";

                foreach (var ruta in AssetDatabase.FindAssets("t:AudioClip", new[] { carpeta })
                                                  .Select(AssetDatabase.GUIDToAssetPath)
                                                  .OrderBy(r => r))
                {
                    string archivo = System.IO.Path.GetFileNameWithoutExtension(ruta);
                    if (!archivo.StartsWith(prefijo)) continue;

                    string sufijo = archivo.Substring(prefijo.Length);
                    if (Segmento.IsMatch(sufijo)) continue;

                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                    if (clip == null) continue;

                    string clave = fuente.prefijoClave + sufijo;
                    if (audios.SharedData.GetEntry(clave) == null)
                    {
                        audios.SharedData.AddKey(clave);
                        clavesNuevas++;
                    }

                    audios.AddAssetToTable(loc.Identifier, clave, clip);
                    conAudio.Add(clave);
                    vinculados++;
                    n++;
                }

                sb.AppendLine($"   [{locale}] {n} archivo(s)");
            }

            var sinAudio = esperadas.Where(c => !conAudio.Contains(c)).ToList();
            if (sinAudio.Count > 0)
                sb.AppendLine($"   sin audio todavía ({sinAudio.Count}): {string.Join(", ", sinAudio)}");
        }

        EditorUtility.SetDirty(audios.SharedData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Narración] Vinculados: {vinculados} | claves nuevas: {clavesNuevas}\n{sb}");
    }

    /// <summary>
    /// Claves de texto que deberían acabar teniendo audio, para listar las que falten.
    /// </summary>
    static List<string> ClavesEsperadas(string tablaTexto, string prefijoClave)
    {
        var col = LocalizationEditorSettings.GetStringTableCollection(tablaTexto);
        if (col == null) return new List<string>();

        return col.SharedData.Entries
                  .Where(e => e != null && !string.IsNullOrEmpty(e.Key) && e.Key.StartsWith(prefijoClave))
                  .Select(e => e.Key)
                  .OrderBy(k => k)
                  .ToList();
    }
}
