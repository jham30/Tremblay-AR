using UnityEngine;
using UnityEngine.Localization;

public enum ColorObjeto
{
    Ninguno,
    Amarillo,
    Azul,
    Rojo,
    Verde,
    Violeta,
    Anaranjado,
    Blanco,
    Negro,
    Gris,
}

/// <summary>
/// Catálogo: definición fija de un objeto del juego. Lo que el jugador hace con él
/// (guardarlo, colocarlo) NO vive aquí, va al JSON de progreso.
/// Nombre, color y audios salen de las tablas Objects/ObjectAudio en el idioma META.
/// </summary>
[CreateAssetMenu(fileName = "NuevoObjeto", menuName = "Tremblay/Objeto")]
public class ObjetoData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Coincide con los ids de las misiones (idCorrecto, idObjetoDestino) y con los sprites TMP.")]
    public string id;

    [Header("Nombre (idioma meta)")]
    public LocalizedString nombre;
    public LocalizedAudioClip audioNombre;

    [Header("Color")]
    [Tooltip("Vocabulario compartido: texto y audio salen de las entradas color.<nombre> de las tablas.")]
    public ColorObjeto color = ColorObjeto.Ninguno;

    [Header("Assets")]
    public GameObject prefab3D;
    public Sprite sprite2D;

    [Header("Cuentos")]
    [Tooltip("Vacío = pertenece a todos los cuentos.")]
    public string[] cuentos;

    [Header("Objeto agarrado (posición respecto a la cámara)")]
    public bool usarConfiguracionPersonalizada;
    public Vector3 posicionAgarradoPersonalizada = new Vector3(0, -0.2f, 0.5f);
    public Vector3 rotacionAgarradaPersonalizada = Vector3.zero;
    public Vector3 escalaAgarradaPersonalizada = Vector3.one;
    [TextArea(2, 3)] public string notasConfiguracion;

    public bool PerteneceACuento(string cuentoID)
    {
        if (string.IsNullOrEmpty(cuentoID)) return true;
        if (cuentos == null || cuentos.Length == 0) return true;
        return System.Array.IndexOf(cuentos, cuentoID) >= 0;
    }
}
