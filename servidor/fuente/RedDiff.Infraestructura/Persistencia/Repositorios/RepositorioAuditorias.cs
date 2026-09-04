using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Trazabilidad;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioAuditorias(ContextoRedDiff contexto) : IRepositorioAuditorias
{
    public void Agregar(Auditoria auditoria)
    {
        contexto.Auditorias.Add(auditoria);
    }
}
