using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Fase 4: traducciones en/fr de las plantillas de misión.
/// </summary>
public static class LocalizacionFase4
{
    const string TablaMissions = "Missions";

    // ============================================================
    // Traducciones en/fr de las plantillas. Los {id} son los mismos en los tres idiomas;
    // cambian el orden, los artículos y la elisión (l'{arana}).
    // ============================================================

    static readonly (string id, string es)[] CorreccionesEs =
    {
        ("14", "Lleva el {craneo} al {mausoleo}."),
        ("41", "Lleva el {hongo} y la {calabaza} al caldero para preparar el té."),
    };

    static readonly (string id, string en, string fr)[] Traducciones =
    {
        ("01", "Light the {farol} with the {vela}.",
               "Allume la {farol} avec la {vela}."),
        ("02", "Sweep the {arana} away with the {escoba}.",
               "Chasse l'{arana} avec le {escoba}."),
        ("03", "Put a {vela} inside the {calabaza}.",
               "Mets une {vela} dans la {calabaza}."),
        ("04", "Bring the {gorro} and the {escoba} to the {mesa}.",
               "Apporte le {gorro} et le {escoba} sur la {mesa}."),
        ("05", "Give the {salchicha} to the {gato}.",
               "Donne la {salchicha} au {gato}."),
        ("06", "Cover the {gato} with the {gorro}.",
               "Couvre le {gato} avec le {gorro}."),
        ("07", "Put the {farol} on the {escoba} and light the {lampara}.",
               "Pose la {farol} sur le {escoba} et allume la {lampara}."),
        ("08", "Put the {hongo} in the {gorro}.",
               "Mets le {hongo} dans le {gorro}."),
        ("09", "Put the {vela} on the {craneo}.",
               "Pose la {vela} sur le {craneo}."),
        ("10", "Scare the {murcielago} with the {escoba}.",
               "Fais peur à la {murcielago} avec le {escoba}."),
        ("11", "Hit the {mano} with the {pala}.",
               "Frappe la {mano} avec la {pala}."),
        ("12", "Hit the {tumba} with the {pala}.",
               "Frappe la {tumba} avec la {pala}."),
        ("13", "Light up the {libro} with the {farol}.",
               "Éclaire le {libro} avec la {farol}."),
        ("14", "Bring the {craneo} to the {mausoleo}.",
               "Apporte le {craneo} au {mausoleo}."),
        ("15", "Bring the {craneo} and the {vela} to light up the {lapida}.",
               "Apporte le {craneo} et la {vela} pour éclairer la {lapida}."),
        ("16", "Light the {lampara} with the {vela}.",
               "Allume la {lampara} avec la {vela}."),
        ("17", "Open the {ataud} with the {pala}.",
               "Ouvre le {ataud} avec la {pala}."),
        ("18", "Put the {craneo} and the {calabaza} in the {mausoleo}.",
               "Mets le {craneo} et la {calabaza} dans le {mausoleo}."),
        ("19", "Catch the {arana} with the {escoba} and the {gorro}.",
               "Attrape l'{arana} avec le {escoba} et le {gorro}."),
        ("20", "Give a {hongo} to the {gato}.",
               "Donne un {hongo} au {gato}."),
        ("21", "Put the {arana} on the {mano}.",
               "Pose l'{arana} sur la {mano}."),
        ("22", "Put the {mano} in the {gorro}.",
               "Mets la {mano} dans le {gorro}."),
        ("23", "Bring a {vela} to the {reja}.",
               "Apporte une {vela} à la {reja}."),
        ("24", "Bring the {calabaza}, the {vela} and the {farol} to the {arbol}.",
               "Apporte la {calabaza}, la {vela} et la {farol} à l'{arbol}."),
        ("25", "Use the {escoba} to move the {arana} away from the {farol}.",
               "Utilise le {escoba} pour éloigner l'{arana} de la {farol}."),
        ("26", "Clean the {lapida} with the {escoba}.",
               "Nettoie la {lapida} avec le {escoba}."),
        ("27", "Light a {vela} in the {calabaza}.",
               "Allume une {vela} dans la {calabaza}."),
        ("28", "Push the {reja} with the {escoba}.",
               "Pousse la {reja} avec le {escoba}."),
        ("29", "Place the {lampara} on the {tumba} and light it with the {vela}.",
               "Pose la {lampara} sur la {tumba} et allume-la avec la {vela}."),
        ("30", "Put the {calabaza} and the {farol} in the {arbol}.",
               "Mets la {calabaza} et la {farol} dans l'{arbol}."),
        ("31", "Put the {lampara} and the {calabaza} next to the {arbol} to scare the {murcielago} away.",
               "Pose la {lampara} et la {calabaza} près de l'{arbol} pour faire fuir la {murcielago}."),
        ("32", "Open the {ataud} with the {pala}.",
               "Ouvre le {ataud} avec la {pala}."),
        ("33", "Put the {farol} on the {tumba}.",
               "Pose la {farol} sur la {tumba}."),
        ("34", "Bring the {calabaza} to the {tumba}.",
               "Apporte la {calabaza} à la {tumba}."),
        ("35", "Put the {mano} in the {ataud}.",
               "Mets la {mano} dans le {ataud}."),
        ("36", "Light a {vela} to scare the {bruja}.",
               "Allume une {vela} pour faire peur à la {bruja}."),
        ("37", "Dig out the {flor} with the {pala}.",
               "Déterre la {flor} avec la {pala}."),
        ("38", "Put the {flor} on the {tumba}.",
               "Pose la {flor} sur la {tumba}."),
        ("39", "Give the {gorro} and the {escoba} to the {bruja}.",
               "Donne le {gorro} et le {escoba} à la {bruja}."),
        ("40", "Put the {escoba} next to the {ataud} and cover it with the witch's {gorro}.",
               "Pose le {escoba} près du {ataud} et couvre-le avec le {gorro} de la sorcière."),
        ("41", "Bring the {hongo} and the {calabaza} to the cauldron to make the tea.",
               "Apporte le {hongo} et la {calabaza} au chaudron pour préparer le thé."),
        ("42", "Put the {farol} next to the {reja} to see the {arbol}.",
               "Pose la {farol} près de la {reja} pour voir l'{arbol}."),
        ("43", "Bring the {pala} and the {craneo} to the {tumba} to dig and put it inside.",
               "Apporte la {pala} et le {craneo} à la {tumba} pour creuser et le mettre dedans."),
        ("44", "Place the {farol} on the {lapida} and add a {flor}.",
               "Pose la {farol} sur la {lapida} et ajoute une {flor}."),
        ("45", "Bring the {gorro} and the {farol} to the {bruja} so she can say goodbye to her last night.",
               "Apporte le {gorro} et la {farol} à la {bruja} pour qu'elle fasse ses adieux à sa dernière nuit."),
        ("46", "Take the {bruja} to the {tumba}.",
               "Emmène la {bruja} à la {tumba}."),
        ("47", "Give the {salchicha} to the {gato}.",
               "Donne la {salchicha} au {gato}."),
        ("t01", "Put the {vela_tutorial} inside the {calabaza_tutorial}.",
                "Mets la {vela_tutorial} dans la {calabaza_tutorial}."),
    };

    [MenuItem("Tremblay/Localización/Fase 4 - Rellenar plantillas de misión en en y fr")]
    public static void RellenarTraducciones()
    {
        var coleccion = LocalizationEditorSettings.GetStringTableCollection(TablaMissions);
        if (coleccion == null) { Debug.LogError("[Fase 4] Falta la tabla Missions."); return; }

        int corregidas = 0, nuevas = 0;
        var faltan = new List<string>();

        foreach (var (id, es) in CorreccionesEs)
            corregidas += Sobrescribir(coleccion, "es", $"mission.{id}.template", es);

        foreach (var (id, en, fr) in Traducciones)
        {
            string clave = $"mission.{id}.template";
            if (coleccion.SharedData.GetEntry(clave) == null) { faltan.Add(clave); continue; }
            nuevas += PonerSiVacio(coleccion, "en", clave, en);
            nuevas += PonerSiVacio(coleccion, "fr", clave, fr);
        }

        EditorUtility.SetDirty(coleccion.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Fase 4] Plantillas: {nuevas} valores rellenados, {corregidas} correcciones en es." +
                  (faltan.Count > 0 ? $" Sin clave ({faltan.Count}): {string.Join(", ", faltan)}" : ""));
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

    static int Sobrescribir(StringTableCollection col, string locale, string clave, string valor)
    {
        var tabla = col.GetTable(locale) as StringTable;
        if (tabla == null || col.SharedData.GetEntry(clave) == null) return 0;

        var entrada = tabla.GetEntry(clave);
        if (entrada == null) tabla.AddEntry(clave, valor);
        else if (entrada.Value == valor) return 0;
        else entrada.Value = valor;

        EditorUtility.SetDirty(tabla);
        return 1;
    }

}
