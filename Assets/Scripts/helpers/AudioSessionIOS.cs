using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// En iOS, Unity usa la categoría de audio "Ambient", que se silencia con el
/// interruptor de silencio del iPhone. Esto la cambia a "Playback" al arrancar
/// y cada vez que la app vuelve a primer plano (Unity puede restablecerla).
/// No necesita estar en ninguna escena.
/// </summary>
public static class AudioSessionIOS
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _ConfigurarAudioPlayback();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Inicializar()
    {
        Configurar();
        Application.focusChanged += enFoco => { if (enFoco) Configurar(); };
    }

    private static void Configurar()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _ConfigurarAudioPlayback();
#endif
    }
}
