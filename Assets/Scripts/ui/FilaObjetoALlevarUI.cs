using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Una fila del panel del objeto a llevar: [objeto] → [destino]. Va en el prefab de la fila;
/// PanelObjetoALlevar la rellena.
/// </summary>
public class FilaObjetoALlevarUI : MonoBehaviour
{
    [Tooltip("Icono del objeto tocado (el que hay que llevar).")]
    public Image iconoObjeto;
    [Tooltip("Icono del destino adonde hay que llevarlo.")]
    public Image iconoDestino;
    [Tooltip("Opcional: nombre del destino en idioma meta.")]
    public TextMeshProUGUI nombreDestino;

    public void Configurar(Sprite objeto, Sprite destino, string textoDestino)
    {
        Poner(iconoObjeto, objeto);
        Poner(iconoDestino, destino);
        if (nombreDestino != null) nombreDestino.text = textoDestino;
    }

    private static void Poner(Image imagen, Sprite sprite)
    {
        if (imagen == null) return;
        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        imagen.enabled = sprite != null;
    }
}
