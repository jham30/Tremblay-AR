using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Fase 4: convierte las partes[] de cada Mission en una plantilla "{id}" y la escribe en la
/// tabla Missions, columna es, bajo mission.<misionID>.template. No pisa plantillas ya escritas.
/// </summary>
public static class LocalizacionFase4
{
    const string TablaMissions = "Missions";

    [MenuItem("Tremblay/Localización/Fase 4 - Migrar partes de misiones a plantillas (es)")]
    public static void MigrarMisiones()
    {
        var coleccion = LocalizationEditorSettings.GetStringTableCollection(TablaMissions);
        var tablaEs = coleccion?.GetTable("es") as StringTable;
        if (tablaEs == null)
        {
            Debug.LogError("[Fase 4] Falta la tabla Missions o su columna es. Ejecuta antes la Fase 0.");
            return;
        }

        var sb = new StringBuilder();
        int migradas = 0, saltadas = 0;
        var problemas = new List<string>();

        foreach (var guid in AssetDatabase.FindAssets("t:Mission"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var mision = AssetDatabase.LoadAssetAtPath<Mission>(ruta);
            if (mision == null) continue;

            if (string.IsNullOrWhiteSpace(mision.misionID))
            {
                problemas.Add($"{ruta}: sin misionID, no se puede migrar");
                continue;
            }
            if (mision.partes == null || mision.partes.Length == 0)
            {
                problemas.Add($"{mision.misionID} ({ruta}): sin partes");
                continue;
            }

            string clave = $"mission.{mision.misionID}.template";
            var existente = tablaEs.GetEntry(clave);
            if (existente != null && !string.IsNullOrEmpty(existente.Value))
            {
                saltadas++;
                sb.AppendLine($"   {mision.misionID}: ya tiene plantilla, se salta");
                continue;
            }

            string plantilla = ConstruirPlantilla(mision.partes);
            if (existente == null) tablaEs.AddEntry(clave, plantilla);
            else existente.Value = plantilla;

            migradas++;
            sb.AppendLine($"   {mision.misionID}: {plantilla}");
        }

        EditorUtility.SetDirty(tablaEs);
        EditorUtility.SetDirty(coleccion.SharedData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Fase 4] {migradas} plantillas creadas, {saltadas} ya existían.\n{sb}" +
                  (problemas.Count > 0 ? $"\nProblemas ({problemas.Count}):\n   " + string.Join("\n   ", problemas) : ""));
    }

    // "Enciende el " + [farol] + "con la" + [vela]  →  "Enciende el {farol} con la {vela}."
    static string ConstruirPlantilla(MissionPart[] partes)
    {
        var trozos = new List<string>();
        var vistos = new Dictionary<string, int>();

        foreach (var p in partes)
        {
            if (p == null) continue;

            if (p.EsSocket)
            {
                string id = (p.idCorrecto ?? "").Trim();
                if (id.Length == 0) { trozos.Add("{?}"); continue; }

                vistos.TryGetValue(id, out int n);
                vistos[id] = n + 1;
                trozos.Add(n == 0 ? $"{{{id}}}" : $"{{{id}#{n + 1}}}");
            }
            else
            {
                string t = (p.texto ?? "").Replace("\r", "").Trim();
                if (t.Length > 0) trozos.Add(t);
            }
        }

        string plantilla = Regex.Replace(string.Join(" ", trozos), @"\s+", " ").Trim();
        if (plantilla.Length > 0 && !plantilla.EndsWith(".") && !plantilla.EndsWith("!") && !plantilla.EndsWith("?"))
            plantilla += ".";
        return plantilla;
    }
}
