using App_v3.Services;

namespace App_v3.Seguimiento;

public class FilaAsistencia
{
    public int EstudianteId { get; set; }
    public string Legajo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Presente { get; set; }
}

public class PlanillaAsistencia
{
    public int ClaseId { get; set; }
    public bool YaRegistrada { get; set; }
    public List<FilaAsistencia> Filas { get; set; } = new();
}

/// <summary>H3 y H4: toma de asistencia y corrección de una fecha ya registrada.</summary>
public class AsistenciaService
{
    private readonly IDatabaseService _db;

    public AsistenciaService(IDatabaseService db) => _db = db;

    /// <summary>
    /// CA 3.1: si la clase no tiene registro, lista a los activos como Presente.
    /// CA 3.4 / CA 4.1: si ya tiene registro, devuelve los estados guardados y YaRegistrada = true.
    /// </summary>
    public async Task<PlanillaAsistencia> ObtenerPlanillaAsync(int claseId)
    {
        var clases = await _db.GetAllAsync<ClaseDictada>();
        if (!clases.Any(c => c.Id == claseId))
            throw new ReglaDeNegocioException("La clase seleccionada no existe en el calendario.");

        var registros = (await _db.GetAllAsync<RegistroAsistencia>())
            .Where(r => r.ClaseId == claseId)
            .ToList();
        var estudiantes = await _db.GetAllAsync<Cursante>();

        var planilla = new PlanillaAsistencia { ClaseId = claseId, YaRegistrada = registros.Count > 0 };

        if (registros.Count > 0)
        {
            planilla.Filas = registros
                .Join(estudiantes, r => r.EstudianteId, e => e.Id, (r, e) => new { Registro = r, Estudiante = e })
                .OrderBy(x => x.Estudiante.Apellido)
                .ThenBy(x => x.Estudiante.Nombre)
                .Select(x => new FilaAsistencia
                {
                    EstudianteId = x.Estudiante.Id,
                    Legajo = x.Estudiante.Legajo,
                    NombreCompleto = x.Estudiante.NombreCompleto,
                    Presente = x.Registro.Presente
                })
                .ToList();
            return planilla;
        }

        var activos = estudiantes
            .Where(e => e.Activo)
            .OrderBy(e => e.Apellido)
            .ThenBy(e => e.Nombre)
            .ToList();

        if (activos.Count == 0)
            throw new ReglaDeNegocioException("No hay estudiantes activos en la comisión.");

        planilla.Filas = activos
            .Select(e => new FilaAsistencia
            {
                EstudianteId = e.Id,
                Legajo = e.Legajo,
                NombreCompleto = e.NombreCompleto,
                Presente = true
            })
            .ToList();
        return planilla;
    }

    // CA 3.3: guardado masivo de los estados vinculados a la clase
    public async Task GuardarAsync(int claseId, IEnumerable<FilaAsistencia> filas)
    {
        ArgumentNullException.ThrowIfNull(filas);
        var lista = filas.ToList();

        if (lista.Count == 0)
            throw new ReglaDeNegocioException("No hay estudiantes para guardar.");

        var yaExiste = (await _db.GetAllAsync<RegistroAsistencia>()).Any(r => r.ClaseId == claseId);
        if (yaExiste)
            throw new ReglaDeNegocioException(
                "Ya existe un registro de asistencia para esta clase. Use \"Modificar Asistencia\" para corregirlo.");

        foreach (var fila in lista)
        {
            await _db.SaveAsync(new RegistroAsistencia
            {
                ClaseId = claseId,
                EstudianteId = fila.EstudianteId,
                Presente = fila.Presente
            });
        }
    }

    /// <summary>
    /// CA 4.3: actualiza el registro histórico. Devuelve cuántos estados cambiaron.
    /// El riesgo se recalcula siempre a partir de estos registros (ver RiesgoService).
    /// </summary>
    public async Task<int> CorregirAsync(int claseId, IEnumerable<FilaAsistencia> filas)
    {
        ArgumentNullException.ThrowIfNull(filas);

        var registros = (await _db.GetAllAsync<RegistroAsistencia>())
            .Where(r => r.ClaseId == claseId)
            .ToList();

        if (registros.Count == 0)
            throw new ReglaDeNegocioException("Esta clase todavía no tiene asistencia registrada.");

        int cambios = 0;
        foreach (var fila in filas)
        {
            var registro = registros.FirstOrDefault(r => r.EstudianteId == fila.EstudianteId);
            if (registro is null || registro.Presente == fila.Presente) continue;

            registro.Presente = fila.Presente;
            await _db.SaveAsync(registro);
            cambios++;
        }

        return cambios;
    }
}
