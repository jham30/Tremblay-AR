using UnityEngine;

/// <summary>
/// Reproduce el nombre y el color de un objeto en el idioma META (el que se aprende).
/// El clip sale de la tabla ObjectAudio vía LanguageManager.
/// </summary>
public class ObjectInfoAudioController : MonoBehaviour
{
    [Header("Audio System")]
    [SerializeField] private bool usarSistemaAudio = true;
    [SerializeField] private float volumenAudioObjetos = 0.8f;
    [SerializeField] private bool debugAudio = true;

    public enum TipoAudio { Nombre, Color }

    public bool TieneAudio(GameObjectData datos, TipoAudio tipo)
    {
        if (!usarSistemaAudio || datos == null) return false;
        return ObtenerClip(datos, tipo) != null;
    }

    public void ReproducirAudioNombre(GameObjectData datos) => ReproducirAudio(datos, TipoAudio.Nombre);
    public void ReproducirAudioColor(GameObjectData datos) => ReproducirAudio(datos, TipoAudio.Color);

    private void ReproducirAudio(GameObjectData datos, TipoAudio tipo)
    {
        if (!usarSistemaAudio || datos == null) return;

        AudioClip clip = ObtenerClip(datos, tipo);
        if (clip == null)
        {
            if (debugAudio) Debug.LogWarning($"🎵 [{tipo}] Sin clip para '{datos.id}' en idioma meta");
            return;
        }

        if (GlobalAudioManager.Instance == null)
        {
            if (debugAudio) Debug.LogWarning($"🎵 [{tipo}] GlobalAudioManager no disponible");
            return;
        }

        GlobalAudioManager.Instance.ReproducirSonidoSFX(clip, volumenAudioObjetos);
        if (debugAudio) Debug.Log($"🎵 [{tipo}] ✅ {datos.id} → {clip.name}");
    }

    private static AudioClip ObtenerClip(GameObjectData datos, TipoAudio tipo)
    {
        var lm = LanguageManager.Instance;
        if (lm == null) return null;
        return tipo == TipoAudio.Nombre ? lm.AudioNombreObjetoMeta(datos.id) : lm.AudioColorObjetoMeta(datos.id);
    }

    public bool UsarSistemaAudio => usarSistemaAudio;
    public float VolumenAudioObjetos => volumenAudioObjetos;
    public bool DebugAudio => debugAudio;

    public void ConfigurarVolumenAudio(float nuevoVolumen)
    {
        volumenAudioObjetos = Mathf.Clamp01(nuevoVolumen);
    }

    public void ActivarSistemaAudio(bool activar)
    {
        usarSistemaAudio = activar;
    }
}
