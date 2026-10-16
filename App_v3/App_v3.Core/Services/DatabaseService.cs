using SQLite;
using App_v3.Models;
using App_v3.Seguimiento;

namespace App_v3.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _db;

    public DatabaseService(string dbPath) => _dbPath = dbPath;

    private async Task<SQLiteAsyncConnection> InitAsync(bool sembrar = true)
    {
        if (_db is not null) return _db;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return _db;

            var db = new SQLiteAsyncConnection(
                _dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            // Tablas originales del proyecto
            await db.CreateTableAsync<Persona>();
            await db.CreateTableAsync<Clase>();
            await db.CreateTableAsync<ConfiguracionRiesgo>();

            // Tablas del seguimiento (H1 a H13)
            await db.CreateTableAsync<Cursante>();
            await db.CreateTableAsync<ClaseDictada>();
            await db.CreateTableAsync<TrabajoCursada>();
            await db.CreateTableAsync<RegistroAsistencia>();
            await db.CreateTableAsync<RegistroEntrega>();
            await db.CreateTableAsync<Contacto>();

            if (sembrar)
            {
                await SembrarDatosAsync(db);
                await SembrarSeguimientoAsync(db);
            }

            _db = db;
            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // Datos de ejemplo originales: solo se cargan si las tablas están vacías
    private static async Task SembrarDatosAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<Persona>().CountAsync() == 0)
        {
            var estudiantes = new List<Persona>
            {
                new() { Nombre = "Martín", Apellido = "González", Legajo = "L-1001", Email = "martin.gonzalez@ejemplo.com", Telefono = "11-5555-0101", EstadoAlerta = "Normal" },
                new() { Nombre = "Sofía", Apellido = "Rodríguez", Legajo = "L-1002", Email = "sofia.rodriguez@ejemplo.com", Telefono = "11-5555-0102", EstadoAlerta = "Normal" },
                new() { Nombre = "Lucas", Apellido = "Fernández", Legajo = "L-1003", Email = "lucas.fernandez@ejemplo.com", Telefono = "11-5555-0103", EstadoAlerta = "Riesgo Moderado" },
                new() { Nombre = "Valentina", Apellido = "López", Legajo = "L-1004", Email = "valentina.lopez@ejemplo.com", Telefono = "11-5555-0104", EstadoAlerta = "Normal" },
                new() { Nombre = "Joaquín", Apellido = "Martínez", Legajo = "L-1005", Email = "joaquin.martinez@ejemplo.com", Telefono = "11-5555-0105", EstadoAlerta = "Riesgo Alto" },
                new() { Nombre = "Camila", Apellido = "Sánchez", Legajo = "L-1006", Email = "camila.sanchez@ejemplo.com", Telefono = "11-5555-0106", EstadoAlerta = "Normal" },
                new() { Nombre = "Tomás", Apellido = "Díaz", Legajo = "L-1007", Email = "tomas.diaz@ejemplo.com", Telefono = "11-5555-0107", EstadoAlerta = "Riesgo Moderado" },
                new() { Nombre = "Agustina", Apellido = "Romero", Legajo = "L-1008", Email = "agustina.romero@ejemplo.com", Telefono = "11-5555-0108", EstadoAlerta = "Normal" }
            };
            await db.InsertAllAsync(estudiantes);
        }

        if (await db.Table<Clase>().CountAsync() == 0)
        {
            var clases = new List<Clase>
            {
                new() { NombreMateria = "Programación III", Comision = "Comisión A" },
                new() { NombreMateria = "Base de Datos", Comision = "Comisión B" },
                new() { NombreMateria = "Ingeniería de Software", Comision = "Comisión A" }
            };
            await db.InsertAllAsync(clases);
        }
    }

    // Datos de ejemplo del seguimiento. Las fechas son relativas a hoy para que siempre
    // haya trabajos vencidos y la base muestre los tres niveles de riesgo.
    private static async Task SembrarSeguimientoAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<Cursante>().CountAsync() > 0) return;

        var hoy = DateTime.Today;

        await db.InsertAllAsync(new List<Cursante>
        {
            new() { Legajo = "L-1001", Dni = "40111222", Nombre = "Martín", Apellido = "González", Email = "martin.gonzalez@ejemplo.com", Telefono = "11-5555-0101" },
            new() { Legajo = "L-1002", Dni = "41222333", Nombre = "Sofía", Apellido = "Rodríguez", Email = "sofia.rodriguez@ejemplo.com", Telefono = "11-5555-0102" },
            new() { Legajo = "L-1003", Dni = "39333444", Nombre = "Lucas", Apellido = "Fernández", Email = "lucas.fernandez@ejemplo.com", Telefono = "11-5555-0103" },
            new() { Legajo = "L-1004", Dni = "42444555", Nombre = "Valentina", Apellido = "López", Email = "valentina.lopez@ejemplo.com", Telefono = "11-5555-0104" },
            new() { Legajo = "L-1005", Dni = "40555666", Nombre = "Joaquín", Apellido = "Martínez", Email = "joaquin.martinez@ejemplo.com", Telefono = "11-5555-0105" },
            new() { Legajo = "L-1006", Dni = "41666777", Nombre = "Camila", Apellido = "Sánchez", Email = "camila.sanchez@ejemplo.com", Telefono = "11-5555-0106" },
            new() { Legajo = "L-1007", Dni = "38777888", Nombre = "Tomás", Apellido = "Díaz", Email = "tomas.diaz@ejemplo.com", Telefono = "11-5555-0107", Activo = false, FechaBaja = hoy.AddDays(-10) },
            new() { Legajo = "L-1008", Dni = "42888999", Nombre = "Agustina", Apellido = "Romero", Email = "agustina.romero@ejemplo.com", Telefono = "11-5555-0108" }
        });
        var cursantes = await db.Table<Cursante>().ToListAsync();
        int IdDe(string legajo) => cursantes.First(c => c.Legajo == legajo).Id;

        // Cinco clases semanales ya dictadas
        var temas = new[]
        {
            "Clase 1: Presentación y requisitos",
            "Clase 2: Modelo de dominio",
            "Clase 3: Historias de usuario",
            "Clase 4: Pruebas unitarias",
            "Clase 5: Arquitectura"
        };
        await db.InsertAllAsync(temas.Select((t, i) => new ClaseDictada { Fecha = hoy.AddDays(-35 + 7 * i), Tema = t }).ToList());
        var clases = await db.Table<ClaseDictada>().OrderBy(c => c.Fecha).ToListAsync();

        // Índices de las clases en las que cada estudiante estuvo ausente
        var ausencias = new Dictionary<string, int[]>
        {
            ["L-1001"] = new[] { 0, 1, 2, 3, 4 },
            ["L-1002"] = Array.Empty<int>(),
            ["L-1003"] = new[] { 1, 2, 4 },
            ["L-1004"] = new[] { 3 },
            ["L-1005"] = new[] { 0, 1 },
            ["L-1006"] = new[] { 0, 2, 3, 4 },
            ["L-1007"] = new[] { 0, 1 },
            ["L-1008"] = Array.Empty<int>()
        };

        var asistencias = new List<RegistroAsistencia>();
        foreach (var (legajo, ausentes) in ausencias)
        {
            int hasta = legajo == "L-1007" ? 3 : clases.Count; // el inactivo solo tiene las primeras clases
            for (int i = 0; i < hasta; i++)
            {
                asistencias.Add(new RegistroAsistencia
                {
                    ClaseId = clases[i].Id,
                    EstudianteId = IdDe(legajo),
                    Presente = !ausentes.Contains(i)
                });
            }
        }
        await db.InsertAllAsync(asistencias);

        // Cuatro trabajos: tres vencidos y uno por vencer
        await db.InsertAllAsync(new List<TrabajoCursada>
        {
            new() { Nombre = "TP1: Diagrama de clases", FechaLimite = hoy.AddDays(-30) },
            new() { Nombre = "TP2: Prototipo de pantallas", FechaLimite = hoy.AddDays(-15) },
            new() { Nombre = "TP3: Pruebas unitarias", FechaLimite = hoy.AddDays(-3) },
            new() { Nombre = "TP4: Entrega final", FechaLimite = hoy.AddDays(10) }
        });
        var trabajos = await db.Table<TrabajoCursada>().OrderBy(t => t.FechaLimite).ToListAsync();

        // Estado de entrega de TP1, TP2 y TP3 por estudiante
        var entregas = new Dictionary<string, bool[]>
        {
            ["L-1001"] = new[] { false, false, false },
            ["L-1002"] = new[] { true, true, true },
            ["L-1003"] = new[] { true, true, false },
            ["L-1004"] = new[] { true, true, true },
            ["L-1005"] = new[] { false, false, false },
            ["L-1006"] = new[] { true, true, true },
            ["L-1007"] = new[] { true, false },
            ["L-1008"] = new[] { true, true, false }
        };

        var registrosEntrega = new List<RegistroEntrega>();
        foreach (var (legajo, estados) in entregas)
        {
            for (int i = 0; i < estados.Length; i++)
            {
                registrosEntrega.Add(new RegistroEntrega
                {
                    TrabajoId = trabajos[i].Id,
                    EstudianteId = IdDe(legajo),
                    Entregado = estados[i]
                });
            }
        }
        await db.InsertAllAsync(registrosEntrega);

        await db.InsertAllAsync(new List<Contacto>
        {
            new() { EstudianteId = IdDe("L-1001"), Fecha = hoy.AddDays(-1), Medio = "Llamada", Resultado = "Respondió", Notas = "Tuvo problemas laborales; se compromete a asistir y a entregar el TP1." },
            new() { EstudianteId = IdDe("L-1003"), Fecha = hoy.AddDays(-10), Medio = "Email", Resultado = "No respondió", Notas = "Se reenvía el recordatorio de entrega del TP3." }
        });
    }

    public async Task<List<T>> GetAllAsync<T>() where T : class, new()
        => await (await InitAsync()).Table<T>().ToListAsync();

    public async Task<int> SaveAsync<T>(T item) where T : class, IEntidad, new()
    {
        var db = await InitAsync();
        return item.Id != 0 ? await db.UpdateAsync(item) : await db.InsertAsync(item);
    }

    public async Task<int> DeleteAsync<T>(T item) where T : class, new()
        => await (await InitAsync()).DeleteAsync(item);

    public async Task ResetAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_db is not null)
            {
                await _db.CloseAsync();
                _db = null;
            }

            SQLiteAsyncConnection.ResetPool();

            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        finally
        {
            _initLock.Release();
        }

        // Reset deja la base vacía (sin datos de ejemplo), como antes
        await InitAsync(sembrar: false);
    }
}
