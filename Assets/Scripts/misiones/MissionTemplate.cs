using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Plantilla de misión por idioma meta: texto libre con sockets {id} (o {id#2} si el mismo
/// objeto se repite). El texto de unión lleva los artículos, así cada idioma enseña su género.
///   "Pon la {vela} en el {farol}."  →  Pon la [vela] en el [farol].
/// El nombre del socket se resuelve con el nombre del objeto en idioma meta, en minúscula, y
/// la primera letra de la frase se pone en mayúscula al renderizar.
/// </summary>
public static class MissionTemplate
{
    static readonly Regex Placeholder = new Regex(@"\{([A-Za-z0-9_]+)(?:#\d+)?\}", RegexOptions.Compiled);

    public static bool TienePlaceholders(string plantilla) =>
        !string.IsNullOrEmpty(plantilla) && Placeholder.IsMatch(plantilla);

    /// <summary>Partes alternando texto/socket para montar el puzle de descifrado.</summary>
    public static List<MissionPart> Parsear(string plantilla, Func<string, string> nombreDe)
    {
        var partes = new List<MissionPart>();
        if (string.IsNullOrEmpty(plantilla)) return partes;

        int pos = 0;
        foreach (Match m in Placeholder.Matches(plantilla))
        {
            string antes = plantilla.Substring(pos, m.Index - pos).Trim();
            if (antes.Length > 0)
                partes.Add(new MissionPart { tipo = MissionPart.PartType.Texto, texto = antes });

            string id = m.Groups[1].Value;
            partes.Add(new MissionPart
            {
                tipo = MissionPart.PartType.Socket,
                idCorrecto = id,
                texto = Minuscula(nombreDe(id)),
            });

            pos = m.Index + m.Length;
        }

        string resto = plantilla.Substring(pos).Trim();
        if (resto.Length > 0)
            partes.Add(new MissionPart { tipo = MissionPart.PartType.Texto, texto = resto });

        if (partes.Count > 0 && partes[0].tipo == MissionPart.PartType.Texto)
            partes[0].texto = Capitalizar(partes[0].texto);

        return partes;
    }

    /// <summary>Frase resuelta con icono TMP tras cada objeto, para listas y paneles.</summary>
    public static string Descripcion(string plantilla, Func<string, string> nombreDe)
    {
        if (string.IsNullOrEmpty(plantilla)) return "";

        string texto = Placeholder.Replace(plantilla, m =>
        {
            string id = m.Groups[1].Value;
            return $"{Minuscula(nombreDe(id))} <sprite name=\"{SpriteDe(id)}\">";
        });

        return Capitalizar(Regex.Replace(texto, @"\s+", " ").Trim());
    }

    // Los objetos del tutorial (vela_tutorial) usan el sprite del objeto base.
    static string SpriteDe(string id) =>
        id.EndsWith("_tutorial") ? id.Substring(0, id.Length - "_tutorial".Length) : id;

    static string Minuscula(string s) => string.IsNullOrEmpty(s) ? s : s.ToLowerInvariant();

    static string Capitalizar(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
}
