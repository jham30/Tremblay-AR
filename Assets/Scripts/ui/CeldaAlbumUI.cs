using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Una celda del álbum de fragmentos: la imagen del fragmento, la misión que lo desbloquea y el
/// botón para volver a verlo. Va en el prefab de la celda; AlbumFragmentosUI la rellena.
/// </summary>
public class CeldaAlbumUI : MonoBehaviour
{
    [Tooltip("Imagen del fragmento (imagenFondo). Bloqueada se ve gris y translúcida.")]
    public Image imagen;
    [Tooltip("Misión que desbloquea el fragmento (o 'Prólogo'). Va encima de la imagen. Usa el " +
             "mismo sprite asset de iconos que la lista de misiones, porque la descripción lleva iconos.")]
    public TextMeshProUGUI etiqueta;
    [Tooltip("Vuelve a reproducir el fragmento. Solo se ve cuando está desbloqueado.")]
    public Button botonReproducir;
    [Tooltip("Opcional: candado u otro adorno que solo se ve cuando está bloqueado.")]
    public GameObject indicadorBloqueado;

    public StoryFragment Fragmento { get; private set; }

    public void Configurar(StoryFragment fragmento, string textoEtiqueta, System.Action<StoryFragment> alReproducir)
    {
        Fragmento = fragmento;

        if (imagen != null)
        {
            imagen.sprite = fragmento.imagenFondo;
            imagen.preserveAspect = true;
        }

        if (etiqueta != null) etiqueta.text = textoEtiqueta;

        if (botonReproducir != null)
        {
            botonReproducir.onClick.RemoveAllListeners();
            botonReproducir.onClick.AddListener(() => alReproducir?.Invoke(fragmento));
        }
    }

    public void AplicarEstado(bool desbloqueado, Material materialGris, Color colorBloqueado)
    {
        if (imagen != null)
        {
            imagen.material = desbloqueado ? null : materialGris;
            imagen.color = desbloqueado ? Color.white : colorBloqueado;
        }

        if (botonReproducir != null) botonReproducir.gameObject.SetActive(desbloqueado);
        if (indicadorBloqueado != null) indicadorBloqueado.SetActive(!desbloqueado);
    }
}
