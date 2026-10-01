/// <summary>
/// Panel que sabe decir si está a la vista. Lo implementan los paneles que se abren y cierran
/// (inventario, lista de misiones, ajustes), que ya tenían este método cada uno por su cuenta.
///
/// Existe para que otros scripts puedan preguntarlo sin atarse a una clase concreta: por ejemplo
/// LlamadaAtencionBoton, que calla el brinco de un botón mientras su panel está abierto y antes
/// solo sabía hacerlo con la lista de misiones.
/// </summary>
public interface IPanelConVisibilidad
{
    bool EstaPanelVisible();
}
