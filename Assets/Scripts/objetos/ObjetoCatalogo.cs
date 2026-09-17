using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Lista de todos los ObjetoData del juego. GameObjectManager lo consulta por id.
/// </summary>
[CreateAssetMenu(fileName = "ObjetoCatalogo", menuName = "Tremblay/Catálogo de objetos")]
public class ObjetoCatalogo : ScriptableObject
{
    public List<ObjetoData> objetos = new List<ObjetoData>();

    private Dictionary<string, ObjetoData> porId;

    public ObjetoData Buscar(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (porId == null || porId.Count != objetos.Count) ReconstruirIndice();
        return porId.TryGetValue(id, out var o) ? o : null;
    }

    private void ReconstruirIndice()
    {
        porId = new Dictionary<string, ObjetoData>();
        foreach (var o in objetos)
            if (o != null && !string.IsNullOrEmpty(o.id) && !porId.ContainsKey(o.id))
                porId[o.id] = o;
    }

    void OnEnable() => porId = null;

    [ContextMenu("Validar catálogo")]
    public void Validar()
    {
        var problemas = new List<string>();

        var duplicados = objetos.Where(o => o != null).GroupBy(o => o.id).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var id in duplicados) problemas.Add($"id duplicado: '{id}'");

        foreach (var o in objetos)
        {
            if (o == null) { problemas.Add("entrada nula en la lista"); continue; }
            if (string.IsNullOrWhiteSpace(o.id)) problemas.Add($"{o.name}: sin id");
            if (o.prefab3D == null) problemas.Add($"{o.id}: sin prefab3D");
            if (o.sprite2D == null) problemas.Add($"{o.id}: sin sprite2D");
            if (o.nombre == null || o.nombre.IsEmpty) problemas.Add($"{o.id}: sin entrada de nombre");
            if (o.color == null || o.color.IsEmpty) problemas.Add($"{o.id}: sin entrada de color");
            if (o.cuentos == null || o.cuentos.Length == 0) problemas.Add($"{o.id}: sin cuento (aparecerá en todos)");
        }

        if (problemas.Count == 0)
            Debug.Log($"[ObjetoCatalogo] ✅ {objetos.Count} objetos, sin problemas.");
        else
            Debug.LogWarning($"[ObjetoCatalogo] ⚠️ {problemas.Count} avisos:\n   " + string.Join("\n   ", problemas));
    }
}
