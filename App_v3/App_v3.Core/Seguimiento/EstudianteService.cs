using App_v3.Services;

namespace App_v3.Seguimiento;

/// <summary>H1 y H13: alta, listado, baja lógica y reactivación de estudiantes.</summary>
public class EstudianteService
{
    private readonly IDatabaseService _db;

    public EstudianteService(IDatabaseService db) => _db = db;

    // CA 1.3: todos los estudiantes registrados
    public async Task<List<Cursante>> ListarAsync() =>
        (await _db.GetAllAsync<Cursante>())
            .OrderBy(e => e.Apellido, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public async Task<List<Cursante>> ListarActivosAsync() =>
        (await ListarAsync()).Where(e => e.Activo).ToList();

    // CA 13.1: estudiantes dados de baja
    public async Task<List<Cursante>> ListarInactivosAsync() =>
        (await ListarAsync()).Where(e => !e.Activo).ToList();

    public async Task<Cursante?> ObtenerAsync(int id) =>
        (await _db.GetAllAsync<Cursante>()).FirstOrDefault(e => e.Id == id);

    // CA 1.1 y CA 1.2
    public async Task<Cursante> AltaAsync(
        string? legajo, string? dni, string? nombre, string? apellido, string? email, string? telefono = null)
    {
        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(legajo)) faltantes.Add("Legajo");
        if (string.IsNullOrWhiteSpace(dni)) faltantes.Add("DNI");
        if (string.IsNullOrWhiteSpace(nombre)) faltantes.Add("Nombre");
        if (string.IsNullOrWhiteSpace(apellido)) faltantes.Add("Apellido");
        if (string.IsNullOrWhiteSpace(email)) faltantes.Add("Email");

        if (faltantes.Count > 0)
            throw new ReglaDeNegocioException(
                "Complete los campos obligatorios: " + string.Join(", ", faltantes) + ".");

        var nuevo = new Cursante
        {
            Legajo = legajo!.Trim(),
            Dni = dni!.Trim(),
            Nombre = nombre!.Trim(),
            Apellido = apellido!.Trim(),
            Email = email!.Trim(),
            Telefono = telefono?.Trim() ?? string.Empty,
            Activo = true
        };

        // La duplicidad se controla contra todos, también contra los dados de baja.
        var existentes = await _db.GetAllAsync<Cursante>();

        if (existentes.Any(e => string.Equals(e.Legajo, nuevo.Legajo, StringComparison.OrdinalIgnoreCase)))
            throw new ReglaDeNegocioException(
                $"Ya existe un estudiante con el legajo {nuevo.Legajo}. No se guardó el registro.");

        if (existentes.Any(e => e.Dni == nuevo.Dni))
            throw new ReglaDeNegocioException(
                $"Ya existe un estudiante con el DNI {nuevo.Dni}. No se guardó el registro.");

        await _db.SaveAsync(nuevo);
        return nuevo;
    }

    // CA 1.4 y CA 13.3: baja lógica, conserva todo el historial
    public async Task InactivarAsync(int id)
    {
        var estudiante = await ObtenerAsync(id)
            ?? throw new ReglaDeNegocioException("El estudiante no existe.");

        if (!estudiante.Activo) return;

        estudiante.Activo = false;
        estudiante.FechaBaja = DateTime.Now;
        await _db.SaveAsync(estudiante);
    }

    public async Task ReactivarAsync(int id)
    {
        var estudiante = await ObtenerAsync(id)
            ?? throw new ReglaDeNegocioException("El estudiante no existe.");

        if (estudiante.Activo) return;

        estudiante.Activo = true;
        estudiante.FechaBaja = null;
        await _db.SaveAsync(estudiante);
    }
}
