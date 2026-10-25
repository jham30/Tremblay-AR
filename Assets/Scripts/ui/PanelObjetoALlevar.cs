using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

/// <summary>
/// 🧭 Panel del objeto a llevar: al tocar un objeto en AR, muestra adónde hay que llevarlo para
/// las misiones descifradas que lo necesitan, con un gráfico [objeto] → [destino] por misión.
///
/// Hoy el panel de misiones solo sale en el DESTINO; este cubre el ORIGEN, que es el primer paso
/// real de Completar (agarrar el objeto). En misiones de varios objetos solo se muestra el objeto
/// tocado (en el gorro: gorro → mesa, sin la escoba), para no confundir.
///
/// Sigue al panel de info AR por sondeo (objeto mostrado, panel abierto, progreso), así que no
/// hace falta engancharlo desde ObjectInfoUIManager. Controla la UI por REFERENCIAS (CanvasGroup):
/// vive en el GameController. Conviene poner el panel dentro del canvas de info AR, para que se
/// oculte con él (p. ej. mientras suena la historia).
/// </summary>
public class PanelObjetoALlevar : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [Tooltip("Padre de las filas (con un Vertical Layout Group, por ejemplo).")]
    [SerializeField] private Transform contenedorFilas;
    [SerializeField] private FilaObjetoALlevarUI prefabFila;
    [Tooltip("Opcional: título del panel (clave ar.carry_to).")]
    [SerializeField] private TextMeshProUGUI titulo;

    private readonly List<FilaObjetoALlevarUI> filas = new List<FilaObjetoALlevarUI>();
    private string firmaMostrada;

    void Start()
    {
        Mostrar(false);

        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMisionesActualizadas += Invalidar;
        ObjectInfoGrabDropController.OnObjetoColocado += AlColocar;
    }

    void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMisionesActualizadas -= Invalidar;
        ObjectInfoGrabDropController.OnObjetoColocado -= AlColocar;
    }

    private void Invalidar() => firmaMostrada = null;
    private void AlColocar(string objetoID, string destinoID) => firmaMostrada = null;

    void LateUpdate()
    {
        var info = ObjectInfoUIManager.Instance;
        string objetoID = info != null && info.CanvasActivo ? info.ObjetoActualID : null;

        // Rehacer solo si cambió lo que se ve (otro objeto, panel cerrado o progreso nuevo).
        string firma = objetoID ?? "";
        if (firma == firmaMostrada) return;
        firmaMostrada = firma;

        Refrescar(objetoID);
    }

    private void Refrescar(string objetoID)
    {
        LimpiarFilas();

        var destinos = string.IsNullOrEmpty(objetoID) ? null : DestinosPendientes(objetoID);
        if (destinos == null || destinos.Count == 0)
        {
            Mostrar(false);
            return;
        }

        Sprite spriteObjeto = SpriteDe(objetoID);
        foreach (var destino in destinos)
        {
            if (prefabFila == null || contenedorFilas == null) break;
            var fila = Instantiate(prefabFila, contenedorFilas);
            fila.Configurar(spriteObjeto, SpriteDe(destino), NombreMeta(destino));
            filas.Add(fila);
        }

        if (titulo != null) titulo.text = LanguageManager.T("ar.carry_to");
        Mostrar(true);
    }

    /// <summary>
    /// Destinos de las misiones descifradas sin completar que necesitan este objeto y donde aún no
    /// está colocado. Sin repetir: si dos misiones lo piden para el mismo destino, una sola fila.
    /// </summary>
    private static List<string> DestinosPendientes(string objetoID)
    {
        var resultado = new List<string>();
        var mm = MissionManager.Instance;
        if (mm == null) return resultado;

        foreach (var m in mm.misiones)
        {
            if (m == null || m.idsObjetosCorrectos == null) continue;
            if (!mm.MisionesDescifradas.Contains(m.misionID) || mm.MisionesCompletadas.Contains(m.misionID)) continue;
            if (!m.idsObjetosCorrectos.Contains(objetoID)) continue;
            if (m.idObjetoDestino == objetoID) continue;
            if (resultado.Contains(m.idObjetoDestino)) continue;

            var colocados = ObjectInfoUIManager.Instance != null
                ? ObjectInfoUIManager.Instance.ObtenerObjetosColocados(m.idObjetoDestino)
                : null;
            if (colocados != null && colocados.Contains(objetoID)) continue;

            resultado.Add(m.idObjetoDestino);
        }

        return resultado;
    }

    private static Sprite SpriteDe(string objetoID) =>
        GameObjectManager.Instance != null ? GameObjectManager.Instance.BuscarObjetoPorId(objetoID)?.Sprite2D : null;

    private static string NombreMeta(string objetoID) =>
        LanguageManager.Instance != null ? LanguageManager.Instance.NombreObjetoMeta(objetoID) : objetoID;

    private void LimpiarFilas()
    {
        foreach (var f in filas)
            if (f != null) Destroy(f.gameObject);
        filas.Clear();
    }

    private void Mostrar(bool mostrar)
    {
        if (panel == null) return;
        panel.alpha = mostrar ? 1f : 0f;
        panel.interactable = mostrar;
        panel.blocksRaycasts = mostrar;
    }
}
