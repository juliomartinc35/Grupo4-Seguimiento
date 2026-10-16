using App_v3.Services;

namespace App_v3.Seguimiento;

/// <summary>H10: registrar y consultar los contactos hechos con un estudiante.</summary>
public class ContactoService
{
    public static readonly IReadOnlyList<string> Medios = new[] { "Llamada", "Email" };

    public static readonly IReadOnlyList<string> Resultados = new[]
    {
        "Respondió", "No respondió", "Quedó en responder", "Se acordó seguimiento"
    };

    private readonly IDatabaseService _db;

    public ContactoService(IDatabaseService db) => _db = db;

    // CA 10.1 y CA 10.2
    public async Task<Contacto> RegistrarAsync(
        int estudianteId, DateTime? fecha, string? medio, string? resultado, string? notas)
    {
        var faltantes = new List<string>();
        if (fecha is null) faltantes.Add("Fecha");
        if (string.IsNullOrWhiteSpace(medio)) faltantes.Add("Medio");
        if (string.IsNullOrWhiteSpace(resultado)) faltantes.Add("Resultado");
        if (string.IsNullOrWhiteSpace(notas)) faltantes.Add("Nota del contacto");

        if (faltantes.Count > 0)
            throw new ReglaDeNegocioException(
                "Faltan campos obligatorios: " + string.Join(", ", faltantes) + ".");

        if (!Medios.Contains(medio!.Trim()))
            throw new ReglaDeNegocioException("El medio de contacto debe ser Llamada o Email.");

        var estudiantes = await _db.GetAllAsync<Cursante>();
        if (!estudiantes.Any(e => e.Id == estudianteId))
            throw new ReglaDeNegocioException("El estudiante no existe.");

        var contacto = new Contacto
        {
            EstudianteId = estudianteId,
            Fecha = fecha!.Value.Date,
            Medio = medio.Trim(),
            Resultado = resultado!.Trim(),
            Notas = notas!.Trim()
        };
        await _db.SaveAsync(contacto);
        return contacto;
    }

    public async Task<List<Contacto>> ListarAsync(int estudianteId) =>
        (await _db.GetAllAsync<Contacto>())
            .Where(c => c.EstudianteId == estudianteId)
            .OrderByDescending(c => c.Fecha)
            .ToList();
}
