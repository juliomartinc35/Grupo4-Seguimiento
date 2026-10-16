using App_v3.Services;

namespace App_v3.Seguimiento;

/// <summary>H2: calendario de clases y de trabajos prácticos del cuatrimestre.</summary>
public class CalendarioService
{
    private readonly IDatabaseService _db;

    public CalendarioService(IDatabaseService db) => _db = db;

    // CA 2.3: visualizar el calendario
    public async Task<List<ClaseDictada>> ListarClasesAsync() =>
        (await _db.GetAllAsync<ClaseDictada>()).OrderBy(c => c.Fecha).ToList();

    public async Task<List<TrabajoCursada>> ListarTrabajosAsync() =>
        (await _db.GetAllAsync<TrabajoCursada>()).OrderBy(t => t.FechaLimite).ToList();

    // CA 2.1 y CA 2.4
    public async Task<ClaseDictada> AgregarClaseAsync(DateTime? fecha, string? tema)
    {
        var faltantes = new List<string>();
        if (fecha is null) faltantes.Add("Fecha de la clase");
        if (string.IsNullOrWhiteSpace(tema)) faltantes.Add("Número o tema");

        if (faltantes.Count > 0)
            throw new ReglaDeNegocioException(
                "Complete los datos obligatorios: " + string.Join(", ", faltantes) + ".");

        var dia = fecha!.Value.Date;
        var existentes = await _db.GetAllAsync<ClaseDictada>();
        if (existentes.Any(c => c.Fecha.Date == dia))
            throw new ReglaDeNegocioException($"Ya hay una clase cargada para el {dia:dd/MM/yyyy}.");

        var clase = new ClaseDictada { Fecha = dia, Tema = tema!.Trim() };
        await _db.SaveAsync(clase);
        return clase;
    }

    // CA 2.2 y CA 2.4
    public async Task<TrabajoCursada> AgregarTrabajoAsync(string? nombre, DateTime? fechaLimite)
    {
        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(nombre)) faltantes.Add("Nombre del trabajo");
        if (fechaLimite is null) faltantes.Add("Fecha límite de entrega");

        if (faltantes.Count > 0)
            throw new ReglaDeNegocioException(
                "Complete los datos obligatorios: " + string.Join(", ", faltantes) + ".");

        var trabajo = new TrabajoCursada { Nombre = nombre!.Trim(), FechaLimite = fechaLimite!.Value.Date };
        await _db.SaveAsync(trabajo);
        return trabajo;
    }
}
