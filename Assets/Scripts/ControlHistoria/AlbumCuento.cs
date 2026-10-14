using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 📖 Lista ordenada de los fragmentos de un cuento, en el orden de la historia.
/// El juego no tiene otro sitio donde viva ese orden (cada misión solo sabe qué fragmento
/// dispara), así que el álbum y el "cuento completo" lo leen de aquí.
/// </summary>
[CreateAssetMenu(fileName = "AlbumCuento", menuName = "Historia/Album Cuento")]
public class AlbumCuento : ScriptableObject
{
    [Tooltip("Todos los fragmentos del cuento, en el orden de la historia (incluidos el prólogo y sus encadenados).")]
    public List<StoryFragment> fragmentos = new List<StoryFragment>();

    /// <summary>
    /// True si el fragmento es continuación de otro de la lista (algún fragmento lo tiene como
    /// siguienteFragmento). El cuento completo NO los lanza: ya suenan solos tras su anterior.
    /// </summary>
    public bool EsContinuacion(StoryFragment fragmento)
    {
        if (fragmento == null) return false;
        foreach (var f in fragmentos)
            if (f != null && f.siguienteFragmento == fragmento) return true;
        return false;
    }
}
