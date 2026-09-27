using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cooperativa.Models
{
    public enum PerfilUsuario
    {
        Administrador,
        Coordenador,
        Cooperado,
        Admin = Administrador,
        Gestor = Coordenador,
        Operacional = Cooperado
    }
}