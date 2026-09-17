using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Fase 3a: crea el catálogo de ObjetoData a partir de objetos_iniciales.json.
/// Resuelve las rutas de Resources a referencias reales, siembra las tablas Objects (es/en)
/// y ObjectAudio (es), y enlaza cada LocalizedString/LocalizedAudioClip. Es idempotente:
/// si el asset ya existe lo actualiza. No toca el juego: el JSON sigue siendo la fuente
/// hasta el paso 3b.
/// </summary>
public static class LocalizacionFase3
{
    const string RutaJson = "Assets/StreamingAssets/objetos_iniciales.json";
    const string CarpetaObjetos = "Assets/Objetos";
    const string RutaCatalogo = CarpetaObjetos + "/ObjetoCatalogo.asset";
    const string TablaObjects = "Objects";
    const string TablaObjectAudio = "ObjectAudio";

    [MenuItem("Tremblay/Localización/Fase 3a - Crear catálogo de objetos desde el JSON")]
    public static void CrearCatalogo()
    {
        if (!File.Exists(RutaJson))
        {
            Debug.LogError($"[Fase 3a] No existe {RutaJson}");
            return;
        }

        var textos = LocalizationEditorSettings.GetStringTableCollection(TablaObjects);
        var audios = LocalizationEditorSettings.GetAssetTableCollection(TablaObjectAudio);
        var localeEs = LocalizationEditorSettings.GetLocale("es");
        var localeEn = LocalizationEditorSettings.GetLocale("en");
        if (textos == null || audios == null || localeEs == null || localeEn == null)
        {
            Debug.LogError("[Fase 3a] Faltan las tablas Objects/ObjectAudio o los locales. Ejecuta antes la Fase 0.");
            return;
        }

        var datos = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(RutaJson));
        if (datos?.objetos == null || datos.objetos.Count == 0)
        {
            Debug.LogError("[Fase 3a] El JSON no tiene objetos.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(CarpetaObjetos))
        {
            Directory.CreateDirectory(CarpetaObjetos);
            AssetDatabase.Refresh();
        }

        var catalogo = AssetDatabase.LoadAssetAtPath<ObjetoCatalogo>(RutaCatalogo);
        if (catalogo == null)
        {
            catalogo = ScriptableObject.CreateInstance<ObjetoCatalogo>();
            AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
        }

        var sb = new StringBuilder();
        var problemas = new List<string>();
        var creados = new List<ObjetoData>();

        foreach (var d in datos.objetos)
        {
            if (string.IsNullOrWhiteSpace(d.id)) { problemas.Add("objeto sin id en el JSON"); continue; }

            string ruta = $"{CarpetaObjetos}/{d.id}.asset";
            var so = AssetDatabase.LoadAssetAtPath<ObjetoData>(ruta);
            bool nuevo = so == null;
            if (nuevo)
            {
                so = ScriptableObject.CreateInstance<ObjetoData>();
                AssetDatabase.CreateAsset(so, ruta);
            }

            so.id = d.id;
            so.cuentos = d.cuentos ?? new string[0];
            so.usarConfiguracionPersonalizada = d.usarConfiguracionPersonalizada;
            so.posicionAgarradoPersonalizada = d.posicionAgarradoPersonalizada;
            so.rotacionAgarradaPersonalizada = d.rotacionAgarradaPersonalizada;
            so.escalaAgarradaPersonalizada = d.escalaAgarradaPersonalizada;
            so.notasConfiguracion = d.notasConfiguracion;

            so.prefab3D = Resources.Load<GameObject>(d.prefab3DPath);
            if (so.prefab3D == null) problemas.Add($"{d.id}: prefab no resuelto '{d.prefab3DPath}'");

            so.sprite2D = Resources.Load<Sprite>(d.sprite2DPath);
            if (so.sprite2D == null) problemas.Add($"{d.id}: sprite no resuelto '{d.sprite2DPath}'");

            // Textos: es/en desde el JSON, fr queda vacío.
            so.nombre = Enlazar(textos, $"obj.{d.id}.nombre", d.nombreEspanol, d.nombreIngles);
            so.color  = Enlazar(textos, $"obj.{d.id}.color",  d.colorEspanol,  d.colorIngles);

            // Audio: es siempre; en solo si el archivo es distinto (hoy es el mismo placeholder).
            so.audioNombre = EnlazarAudio(audios, localeEs, localeEn, $"obj.{d.id}.audioNombre",
                d.audioNombreEspanol, d.audioNombreIngles, d.id, problemas);
            so.audioColor = EnlazarAudio(audios, localeEs, localeEn, $"obj.{d.id}.audioColor",
                d.audioColorEspanol, d.audioColorIngles, d.id, problemas);

            EditorUtility.SetDirty(so);
            creados.Add(so);
            sb.AppendLine($"   {(nuevo ? "nuevo" : "actualizado")}  {d.id}  prefab={(so.prefab3D ? "ok" : "FALTA")} sprite={(so.sprite2D ? "ok" : "FALTA")}");
        }

        catalogo.objetos = creados.OrderBy(o => o.id).ToList();
        EditorUtility.SetDirty(catalogo);
        EditorUtility.SetDirty(textos.SharedData);
        EditorUtility.SetDirty(audios.SharedData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Fase 3a] Catálogo con {creados.Count} objetos en {RutaCatalogo}\n{sb}");
        if (problemas.Count > 0)
            Debug.LogWarning($"[Fase 3a] {problemas.Count} problemas:\n   " + string.Join("\n   ", problemas));

        catalogo.Validar();
    }

    static LocalizedString Enlazar(StringTableCollection col, string clave, string es, string en)
    {
        PonerSiVacio(col, "es", clave, es);
        PonerSiVacio(col, "en", clave, en);

        var entrada = col.SharedData.GetEntry(clave);
        var ls = new LocalizedString();
        ls.SetReference(col.SharedData.TableCollectionNameGuid, entrada.Id);
        return ls;
    }

    static LocalizedAudioClip EnlazarAudio(AssetTableCollection col, Locale es, Locale en, string clave,
        string rutaEs, string rutaEn, string id, List<string> problemas)
    {
        var clipEs = CargarClip(rutaEs);
        if (clipEs == null && !string.IsNullOrEmpty(rutaEs)) problemas.Add($"{id}: audio no resuelto '{rutaEs}'");

        if (clipEs != null) col.AddAssetToTable(es.Identifier, clave, clipEs);

        bool enDistinto = !string.IsNullOrEmpty(rutaEn) && rutaEn != rutaEs;
        if (enDistinto)
        {
            var clipEn = CargarClip(rutaEn);
            if (clipEn != null) col.AddAssetToTable(en.Identifier, clave, clipEn);
            else problemas.Add($"{id}: audio en no resuelto '{rutaEn}'");
        }

        var entrada = col.SharedData.GetEntry(clave) ?? col.SharedData.AddKey(clave);
        var la = new LocalizedAudioClip();
        la.SetReference(col.SharedData.TableCollectionNameGuid, entrada.Id);
        return la;
    }

    static AudioClip CargarClip(string ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return null;
        string limpia = ruta.Replace(".mp3", "").Replace(".wav", "").Replace(".ogg", "");
        return Resources.Load<AudioClip>(limpia);
    }

    static void PonerSiVacio(StringTableCollection col, string locale, string clave, string valor)
    {
        var tabla = col.GetTable(locale) as StringTable;
        if (tabla == null || string.IsNullOrEmpty(valor)) return;

        var entrada = tabla.GetEntry(clave);
        if (entrada == null) tabla.AddEntry(clave, valor);
        else if (string.IsNullOrEmpty(entrada.Value)) entrada.Value = valor;
        else return;

        EditorUtility.SetDirty(tabla);
    }
}
