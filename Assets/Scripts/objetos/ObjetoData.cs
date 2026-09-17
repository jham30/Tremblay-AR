using UnityEngine;
using UnityEngine.Localization;

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

    [Header("Textos (idioma meta)")]
    public LocalizedString nombre;
    public LocalizedString color;

    [Header("Audio (idioma meta)")]
    public LocalizedAudioClip audioNombre;
    public LocalizedAudioClip audioColor;

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

    // Rutas de Resources que aún usan los consumidores que cargan por string (pasos 3d/3e).
    // Las rellena la migración; se borran en 3f cuando ya nadie las lea.
    [HideInInspector] public string prefab3DPathLegacy;
    [HideInInspector] public string sprite2DPathLegacy;
    [HideInInspector] public string audioNombreEsLegacy;
    [HideInInspector] public string audioNombreEnLegacy;
    [HideInInspector] public string audioColorEsLegacy;
    [HideInInspector] public string audioColorEnLegacy;

    public bool PerteneceACuento(string cuentoID)
    {
        if (string.IsNullOrEmpty(cuentoID)) return true;
        if (cuentos == null || cuentos.Length == 0) return true;
        return System.Array.IndexOf(cuentos, cuentoID) >= 0;
    }
}
