using App_v3.Services;

namespace App_v3.Seguimiento;

public class FilaEntrega
{
    public int EstudianteId { get; set; }
    public string Legajo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Entregado { get; set; }
}

public class PlanillaEntrega
{
    public int TrabajoId { get; set; }
    public bool YaRegistrado { get; set; }
    public List<FilaEntrega> Filas { get; set; } = new();
}

/// <summary>H5: registrar la entrega o no entrega de un trabajo práctico.</summary>
public class EntregaService
{
    private readonly IDatabaseService _db;

    public EntregaService(IDatabaseService db) => _db = db;

    /// <summary>
    /// CA 5.3: lista a todos los estudiantes activos (por defecto "No entregó").
    /// CA 5.4: si el trabajo ya tiene información, muestra el estado guardado de cada estudiante.
    /// </summary>
    public async Task<PlanillaEntrega> ObtenerPlanillaAsync(int trabajoId)
    {
        var trabajos = await _db.GetAllAsync<TrabajoCursada>();
        if (!trabajos.Any(t => t.Id == trabajoId))
            throw new ReglaDeNegocioException("El trabajo seleccionado no existe en el calendario.");

        var registros = (await _db.GetAllAsync<RegistroEntrega>())
            .Where(r => r.TrabajoId == trabajoId)
            .ToList();
        var estudiantes = await _db.GetAllAsync<Cursante>();

        // Activos, más los dados de baja que tengan un registro previo (se conserva el historial).
        var visibles = estudiantes
            .Where(e => e.Activo || registros.Any(r => r.EstudianteId == e.Id))
            .OrderBy(e => e.Apellido)
            .ThenBy(e => e.Nombre)
            .ToList();

        if (visibles.Count == 0)
            throw new ReglaDeNegocioException("No hay estudiantes activos en la comisión.");

        return new PlanillaEntrega
        {
            TrabajoId = trabajoId,
            YaRegistrado = registros.Count > 0,
            Filas = visibles
                .Select(e => new FilaEntrega
                {
                    EstudianteId = e.Id,
                    Legajo = e.Legajo,
                    NombreCompleto = e.NombreCompleto,
                    Entregado = registros.FirstOrDefault(r => r.EstudianteId == e.Id)?.Entregado ?? false
                })
                .ToList()
        };
    }

    // CA 5.1 y CA 5.2: registra (o actualiza) la entrega de cada estudiante
    public async Task GuardarAsync(int trabajoId, IEnumerable<FilaEntrega> filas)
    {
        ArgumentNullException.ThrowIfNull(filas);
        var lista = filas.ToList();

        if (lista.Count == 0)
            throw new ReglaDeNegocioException("No hay estudiantes para guardar.");

        var registros = (await _db.GetAllAsync<RegistroEntrega>())
            .Where(r => r.TrabajoId == trabajoId)
            .ToList();

        foreach (var fila in lista)
        {
            var existente = registros.FirstOrDefault(r => r.EstudianteId == fila.EstudianteId);
            if (existente is null)
            {
                await _db.SaveAsync(new RegistroEntrega
                {
                    TrabajoId = trabajoId,
                    EstudianteId = fila.EstudianteId,
                    Entregado = fila.Entregado
                });
            }
            else if (existente.Entregado != fila.Entregado)
            {
                existente.Entregado = fila.Entregado;
                await _db.SaveAsync(existente);
            }
        }
    }
}
