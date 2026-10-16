using SQLite;
using App_v3.Models;

namespace App_v3.Seguimiento;

/// <summary>Error de negocio con un mensaje listo para mostrar al usuario.</summary>
public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje) { }
}

/// <summary>H1/H9/H13: estudiante de la comisión. La baja es lógica (Activo = false), nunca se borra.</summary>
[Table("seg_estudiantes")]
public class Cursante : IEntidad
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Legajo { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime? FechaBaja { get; set; }

    [Ignore]
    public string NombreCompleto => $"{Nombre} {Apellido}";
}

/// <summary>H2: una clase dictada en una fecha del calendario de la comisión.</summary>
[Table("seg_clases")]
public class ClaseDictada : IEntidad
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime Fecha { get; set; }
    public string Tema { get; set; } = string.Empty;
}

/// <summary>H2: trabajo práctico con su fecha límite de entrega.</summary>
[Table("seg_trabajos")]
public class TrabajoCursada : IEntidad
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaLimite { get; set; }
}

/// <summary>H3/H4: estado de asistencia de un estudiante en una clase.</summary>
[Table("seg_asistencias")]
public class RegistroAsistencia : IEntidad
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ClaseId { get; set; }
    public int EstudianteId { get; set; }
    public bool Presente { get; set; }
}

/// <summary>H5: entrega (o no entrega) de un estudiante para un trabajo práctico.</summary>
[Table("seg_entregas")]
public class RegistroEntrega : IEntidad
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int TrabajoId { get; set; }
    public int EstudianteId { get; set; }
    public bool Entregado { get; set; }
}

/// <summary>H10: intervención (llamada o email) realizada con un estudiante.</summary>
[Table("seg_contactos")]
public class Contacto : IEntidad
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int EstudianteId { get; set; }
    public DateTime Fecha { get; set; }
    public string Medio { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
}
