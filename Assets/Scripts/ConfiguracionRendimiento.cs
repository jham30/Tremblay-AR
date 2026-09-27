using UnityEngine;

/// <summary>
/// Fija el frame rate objetivo al arrancar la app.
///
/// El juego es AR: Vuforia entrega el fotograma de cámara a ~30 fps, así que renderizar por
/// encima repite el mismo fotograma gastando batería y generando calor, sin ganancia visible.
/// En una tablet sostenida en las manos eso se nota, y Apple lo mira en la revisión.
///
/// vSyncCount no sirve aquí: iOS y Android lo ignoran, en móvil manda targetFrameRate.
///
/// No necesita GameObject ni cableado: se ejecuta una vez antes de cargar la primera escena.
/// </summary>
public static class ConfiguracionRendimiento
{
    const int FpsObjetivo = 30;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Aplicar()
    {
        QualitySettings.vSyncCount = 0;   // en escritorio, para que targetFrameRate mande
        Application.targetFrameRate = FpsObjetivo;

        Debug.Log($"[Rendimiento] targetFrameRate = {Application.targetFrameRate}");
    }
}
