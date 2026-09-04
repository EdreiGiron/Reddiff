using RedDiff.Dominio.Entidades.Trazabilidad;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioAuditorias
{
    void Agregar(Auditoria auditoria);
}
