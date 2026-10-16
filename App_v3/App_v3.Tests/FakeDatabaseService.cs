using System.Text.Json;
using App_v3.Models;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.Tests;

/// <summary>Base de datos en memoria para probar los servicios sin SQLite ni MAUI.</summary>
public sealed class FakeDatabaseService : IDatabaseService
{
    private readonly Dictionary<Type, SortedDictionary<int, string>> _tablas = new();
    private int _ultimoId;

    private SortedDictionary<int, string> Tabla(Type tipo)
    {
        if (!_tablas.TryGetValue(tipo, out var tabla))
        {
            tabla = new SortedDictionary<int, string>();
            _tablas[tipo] = tabla;
        }
        return tabla;
    }

    // Devuelve copias (como haría SQLite), no las instancias guardadas
    public Task<List<T>> GetAllAsync<T>() where T : class, new() =>
        Task.FromResult(Tabla(typeof(T)).Values.Select(json => JsonSerializer.Deserialize<T>(json)!).ToList());

    public Task<int> SaveAsync<T>(T item) where T : class, IEntidad, new()
    {
        if (item.Id == 0)
            typeof(T).GetProperty("Id")!.SetValue(item, ++_ultimoId);

        Tabla(typeof(T))[item.Id] = JsonSerializer.Serialize(item);
        return Task.FromResult(1);
    }

    public Task<int> DeleteAsync<T>(T item) where T : class, new()
    {
        var id = (int)typeof(T).GetProperty("Id")!.GetValue(item)!;
        return Task.FromResult(Tabla(typeof(T)).Remove(id) ? 1 : 0);
    }

    public Task ResetAsync()
    {
        _tablas.Clear();
        return Task.CompletedTask;
    }
}

/// <summary>Arma todos los servicios sobre una base en memoria.</summary>
internal sealed class Escenario
{
    public static readonly DateTime Hoy = new(2026, 10, 15);

    public FakeDatabaseService Db { get; } = new();
    public EstudianteService Estudiantes { get; }
    public CalendarioService Calendario { get; }
    public AsistenciaService Asistencia { get; }
    public EntregaService Entregas { get; }
    public RiesgoService Riesgo { get; }
    public ContactoService Contactos { get; }
    public FichaService Fichas { get; }

    public Escenario()
    {
        Estudiantes = new EstudianteService(Db);
        Calendario = new CalendarioService(Db);
        Asistencia = new AsistenciaService(Db);
        Entregas = new EntregaService(Db);
        Riesgo = new RiesgoService(Db);
        Contactos = new ContactoService(Db);
        Fichas = new FichaService(Db, Riesgo);
    }

    public Task<Cursante> Alta(string legajo, string dni, string nombre = "Juan", string apellido = "Pérez") =>
        Estudiantes.AltaAsync(legajo, dni, nombre, apellido, $"{legajo.ToLower()}@mail.com");

    /// <summary>Crea una clase y guarda su asistencia; los legajos indicados quedan ausentes.</summary>
    public async Task<ClaseDictada> ClaseConAsistencia(DateTime fecha, params string[] legajosAusentes)
    {
        var clase = await Calendario.AgregarClaseAsync(fecha, $"Clase {fecha:dd/MM}");
        var planilla = await Asistencia.ObtenerPlanillaAsync(clase.Id);
        foreach (var fila in planilla.Filas.Where(f => legajosAusentes.Contains(f.Legajo)))
            fila.Presente = false;
        await Asistencia.GuardarAsync(clase.Id, planilla.Filas);
        return clase;
    }

    /// <summary>Crea un trabajo y registra qué legajos entregaron.</summary>
    public async Task<TrabajoCursada> TrabajoConEntregas(DateTime fechaLimite, params string[] legajosQueEntregaron)
    {
        var trabajo = await Calendario.AgregarTrabajoAsync($"TP {fechaLimite:dd/MM}", fechaLimite);
        var planilla = await Entregas.ObtenerPlanillaAsync(trabajo.Id);
        foreach (var fila in planilla.Filas)
            fila.Entregado = legajosQueEntregaron.Contains(fila.Legajo);
        await Entregas.GuardarAsync(trabajo.Id, planilla.Filas);
        return trabajo;
    }
}

public static class Verificar
{
    /// <summary>Verifica que la acción asíncrona lance la excepción esperada y la devuelve.</summary>
    public static async Task<T> LanzaAsync<T>(Func<Task> accion) where T : Exception
    {
        try
        {
            await accion();
        }
        catch (T ex)
        {
            return ex;
        }

        Assert.Fail($"Se esperaba una excepción {typeof(T).Name}.");
        return null!;
    }
}
