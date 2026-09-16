using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Único punto de acceso al archivo que marca si el tutorial ya se completó.
/// Antes cada escena leía el JSON por su cuenta con criterios distintos (unas comprobaban
/// solo que existiera, otras que contuviera "completado":true) → aquí se centraliza.
/// </summary>
public static class TutorialProgreso
{
    public const string NombreArchivo = "tutorial_completado.json";

    private static string Ruta => Path.Combine(Application.persistentDataPath, NombreArchivo);

    public static bool EstaCompletado()
    {
        try
        {
            return File.Exists(Ruta) && File.ReadAllText(Ruta).Contains("\"completado\":true");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TutorialProgreso] No se pudo leer el progreso: {e.Message}");
            return false;
        }
    }

    public static void MarcarCompletado()
    {
        try
        {
            File.WriteAllText(Ruta, "{\"completado\":true}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TutorialProgreso] No se pudo guardar el progreso: {e.Message}");
        }
    }

    /// <summary>Borra la marca: la próxima vez que se entre al juego se volverá a pasar por el tutorial.</summary>
    public static void Borrar()
    {
        try
        {
            if (File.Exists(Ruta)) File.Delete(Ruta);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TutorialProgreso] No se pudo borrar el progreso: {e.Message}");
        }
    }
}
