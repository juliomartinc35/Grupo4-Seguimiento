using App_v3.Models;
using App_v3.Services;

namespace App_v3.Seguimiento;

public class ItemHistorialAsistencia
{
    public DateTime Fecha { get; set; }
    public string Tema { get; set; } = string.Empty;
    public bool Presente { get; set; }
    public string Estado => Presente ? "Presente" : "Ausente";
}

public class ItemHistorialEntrega
{
    public string Trabajo { get; set; } = string.Empty;
    public DateTime FechaLimite { get; set; }
    public bool Entregado { get; set; }
    public bool Vencido { get; set; }
    public string Estado => Entregado ? "Entregado" : "No entregado";
}

public class FichaDetalle
{
    public Cursante Estudiante { get; set; } = new();
    public NivelRiesgo? Nivel { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public int TotalInasistencias { get; set; }
    public int TotalTpsNoEntregados { get; set; }
    public List<ItemHistorialAsistencia> Asistencias { get; set; } = new();
    public List<ItemHistorialEntrega> Entregas { get; set; } = new();
    public List<Contacto> Contactos { get; set; } = new();
}

/// <summary>H9, H12 y H13: ficha del estudiante con su historial (también para inactivos).</summary>
public class FichaService
{
    private readonly IDatabaseService _db;
    private readonly RiesgoService _riesgo;

    public FichaService(IDatabaseService db, RiesgoService riesgo)
    {
        _db = db;
        _riesgo = riesgo;
    }

    public async Task<FichaDetalle> ObtenerAsync(int estudianteId, DateTime hoy)
    {
        var estudiante = (await _db.GetAllAsync<Cursante>()).FirstOrDefault(e => e.Id == estudianteId)
            ?? throw new ReglaDeNegocioException("El estudiante no existe.");

        var clases = await _db.GetAllAsync<ClaseDictada>();
        var trabajos = await _db.GetAllAsync<TrabajoCursada>();
        var asistencias = (await _db.GetAllAsync<RegistroAsistencia>()).Where(a => a.EstudianteId == estudianteId).ToList();
        var entregas = (await _db.GetAllAsync<RegistroEntrega>()).Where(e => e.EstudianteId == estudianteId).ToList();

        // CA 12.1 y CA 12.4: las asistencias registradas se conservan aunque el estudiante esté inactivo
        var historialAsistencias = asistencias
            .Join(clases, a => a.ClaseId, c => c.Id,
                (a, c) => new ItemHistorialAsistencia { Fecha = c.Fecha, Tema = c.Tema, Presente = a.Presente })
            .OrderBy(i => i.Fecha)
            .ToList();

        // CA 12.2: cada trabajo con su estado
        var historialEntregas = trabajos
            .OrderBy(t => t.FechaLimite)
            .Select(t => new ItemHistorialEntrega
            {
                Trabajo = t.Nombre,
                FechaLimite = t.FechaLimite,
                Entregado = entregas.Any(x => x.TrabajoId == t.Id && x.Entregado),
                Vencido = t.FechaLimite.Date < hoy.Date
            })
            .ToList();

        // CA 12.3: totales de la cursada
        int inasistencias = historialAsistencias.Count(i => !i.Presente);
        int tpsNoEntregados = historialEntregas.Count(i => i.Vencido && !i.Entregado);

        var ficha = new FichaDetalle
        {
            Estudiante = estudiante,
            TotalInasistencias = inasistencias,
            TotalTpsNoEntregados = tpsNoEntregados,
            Asistencias = historialAsistencias,
            Entregas = historialEntregas,
            Contactos = (await _db.GetAllAsync<Contacto>())
                .Where(c => c.EstudianteId == estudianteId)
                .OrderByDescending(c => c.Fecha)
                .ToList()
        };

        // CA 9.2: nivel y motivo (los inactivos no participan del seguimiento)
        if (estudiante.Activo)
        {
            var umbrales = await _riesgo.ObtenerUmbralesAsync();
            ficha.Nivel = umbrales.CalcularNivel(inasistencias, tpsNoEntregados);
            ficha.Motivo = RiesgoService.ArmarMotivo(umbrales, inasistencias, tpsNoEntregados);
        }
        else
        {
            ficha.Nivel = null;
            ficha.Motivo = "Estudiante inactivo: no participa del seguimiento, pero conserva su historial.";
        }

        return ficha;
    }
}
