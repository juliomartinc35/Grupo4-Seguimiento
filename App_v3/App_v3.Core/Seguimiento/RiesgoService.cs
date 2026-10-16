using App_v3.Models;
using App_v3.Services;

namespace App_v3.Seguimiento;

public class FilaSeguimiento
{
    public int EstudianteId { get; set; }
    public string Legajo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public int Inasistencias { get; set; }
    public int TpsVencidosNoEntregados { get; set; }
    public NivelRiesgo Nivel { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime? UltimoContacto { get; set; }
    public bool Contactado => UltimoContacto.HasValue;
}

/// <summary>
/// H6, H8 y H11: calcula el riesgo de cada estudiante activo a partir de los registros y los
/// umbrales vigentes (H7). Se calcula siempre en el momento, por eso cualquier cambio en una
/// asistencia, una entrega o los umbrales se refleja en el próximo cálculo (CA 6.3 y CA 8.4).
/// </summary>
public class RiesgoService
{
    private readonly IDatabaseService _db;

    public RiesgoService(IDatabaseService db) => _db = db;

    public async Task<ConfiguracionRiesgo> ObtenerUmbralesAsync() =>
        (await _db.GetAllAsync<ConfiguracionRiesgo>()).FirstOrDefault() ?? new ConfiguracionRiesgo();

    /// <summary>
    /// CA 6.1, 6.2 y 6.4: solo estudiantes activos; nivel general = el más alto de las dos señales.
    /// CA 8.1: ordenados de mayor a menor riesgo.
    /// </summary>
    public async Task<List<FilaSeguimiento>> CalcularAsync(DateTime hoy)
    {
        var estudiantes = await _db.GetAllAsync<Cursante>();
        var asistencias = await _db.GetAllAsync<RegistroAsistencia>();
        var trabajos = await _db.GetAllAsync<TrabajoCursada>();
        var entregas = await _db.GetAllAsync<RegistroEntrega>();
        var contactos = await _db.GetAllAsync<Contacto>();
        var umbrales = await ObtenerUmbralesAsync();

        return estudiantes
            .Where(e => e.Activo)
            .Select(e =>
            {
                int inasistencias = asistencias.Count(a => a.EstudianteId == e.Id && !a.Presente);

                int tpsPendientes = trabajos.Count(t =>
                    t.FechaLimite.Date < hoy.Date &&
                    !entregas.Any(x => x.TrabajoId == t.Id && x.EstudianteId == e.Id && x.Entregado));

                var propios = contactos.Where(c => c.EstudianteId == e.Id).ToList();

                return new FilaSeguimiento
                {
                    EstudianteId = e.Id,
                    Legajo = e.Legajo,
                    NombreCompleto = e.NombreCompleto,
                    Inasistencias = inasistencias,
                    TpsVencidosNoEntregados = tpsPendientes,
                    Nivel = umbrales.CalcularNivel(inasistencias, tpsPendientes),
                    Motivo = ArmarMotivo(umbrales, inasistencias, tpsPendientes),
                    UltimoContacto = propios.Count > 0 ? propios.Max(c => c.Fecha) : null
                };
            })
            .OrderByDescending(f => (int)f.Nivel)
            .ThenByDescending(f => f.Inasistencias)
            .ThenByDescending(f => f.TpsVencidosNoEntregados)
            .ThenBy(f => f.NombreCompleto)
            .ToList();
    }

    /// <summary>
    /// CA 8.1 y CA 11.2. Con el filtro activo se muestran solo los estudiantes con riesgo
    /// Medio o Alto que no tuvieron ningún contacto en los últimos 7 días.
    /// </summary>
    public async Task<List<FilaSeguimiento>> ObtenerPanelAsync(DateTime hoy, bool soloNoContactadosEstaSemana = false)
    {
        var filas = await CalcularAsync(hoy);
        if (!soloNoContactadosEstaSemana) return filas;

        var limite = hoy.Date.AddDays(-7);
        return filas
            .Where(f => f.Nivel != NivelRiesgo.Bajo)
            .Where(f => !(f.UltimoContacto.HasValue && f.UltimoContacto.Value.Date >= limite))
            .ToList();
    }

    /// <summary>CA 8.2 y CA 9.2: explica qué señales justifican el nivel.</summary>
    public static string ArmarMotivo(ConfiguracionRiesgo umbrales, int inasistencias, int tpsPendientes)
    {
        var porInasistencias = umbrales.CalcularNivel(inasistencias, 0);
        var porTps = umbrales.CalcularNivel(0, tpsPendientes);

        var ina = inasistencias == 1 ? "1 inasistencia" : $"{inasistencias} inasistencias";
        var tps = tpsPendientes == 1
            ? "1 TP vencido sin entregar"
            : $"{tpsPendientes} TPs vencidos sin entregar";

        return $"{ina} (riesgo {porInasistencias}) y {tps} (riesgo {porTps}).";
    }
}
