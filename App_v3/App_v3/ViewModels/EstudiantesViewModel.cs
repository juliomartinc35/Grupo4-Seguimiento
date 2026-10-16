using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Windows.Input;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.ViewModels;

public class EstudianteFila
{
    public EstudianteFila(Cursante c, ICommand ficha, ICommand baja, ICommand reactivar)
    {
        Estudiante = c;
        FichaCommand = ficha;
        BajaCommand = baja;
        ReactivarCommand = reactivar;
    }

    public Cursante Estudiante { get; }
    public ICommand FichaCommand { get; }
    public ICommand BajaCommand { get; }
    public ICommand ReactivarCommand { get; }

    public string NombreCompleto => $"{Estudiante.Apellido}, {Estudiante.Nombre}";
    public string Identificacion => $"Legajo {Estudiante.Legajo}   ·   DNI {Estudiante.Dni}";
    public string Contacto =>
        $"{Estudiante.Email}   ·   {(string.IsNullOrWhiteSpace(Estudiante.Telefono) ? "sin teléfono" : Estudiante.Telefono)}";

    // CA 13.1: el estado se indica claramente
    public string EstadoTexto => Estudiante.Activo ? "ACTIVO" : "INACTIVO";
    public Color ColorEstado => Apariencia.Activo(Estudiante.Activo);
    public bool PuedeDarDeBaja => Estudiante.Activo;
    public bool PuedeReactivar => !Estudiante.Activo;
}

/// <summary>H1 y H13: alta, nómina y baja lógica de estudiantes.</summary>
public class EstudiantesViewModel : ObservableBase
{
    private readonly EstudianteService _servicio;
    private readonly IDialogService _dialog;
    private readonly IServiceProvider _servicios;

    public EstudiantesViewModel(EstudianteService servicio, IDialogService dialog, IServiceProvider servicios)
    {
        _servicio = servicio;
        _dialog = dialog;
        _servicios = servicios;

        GuardarCommand = new Command(async () => await GuardarAsync());
        ActualizarCommand = new Command(async () => await CargarAsync());
    }

    // ---- Formulario (CA 1.1) ----
    private string _legajo = string.Empty;
    public string Legajo { get => _legajo; set => Set(ref _legajo, value); }

    private string _dni = string.Empty;
    public string Dni { get => _dni; set => Set(ref _dni, value); }

    private string _nombre = string.Empty;
    public string Nombre { get => _nombre; set => Set(ref _nombre, value); }

    private string _apellido = string.Empty;
    public string Apellido { get => _apellido; set => Set(ref _apellido, value); }

    private string _email = string.Empty;
    public string Email { get => _email; set => Set(ref _email, value); }

    private string _telefono = string.Empty;
    public string Telefono { get => _telefono; set => Set(ref _telefono, value); }

    // ---- Listado ----
    public IReadOnlyList<string> Filtros { get; } = new[] { "Activos", "Inactivos", "Todos" };

    private string _filtro = "Activos";
    public string Filtro
    {
        get => _filtro;
        set
        {
            if (Set(ref _filtro, string.IsNullOrEmpty(value) ? "Activos" : value))
                _ = CargarAsync();
        }
    }

    private string _resumen = string.Empty;
    public string Resumen { get => _resumen; private set => Set(ref _resumen, value); }

    public ObservableCollection<EstudianteFila> Estudiantes { get; } = new();

    public ICommand GuardarCommand { get; }
    public ICommand ActualizarCommand { get; }

    public async Task CargarAsync()
    {
        try
        {
            var todos = await _servicio.ListarAsync();
            var visibles = Filtro switch
            {
                "Inactivos" => todos.Where(e => !e.Activo),
                "Todos" => todos,
                _ => todos.Where(e => e.Activo)
            };

            Estudiantes.Clear();
            foreach (var e in visibles)
            {
                var id = e.Id;
                Estudiantes.Add(new EstudianteFila(
                    e,
                    new Command(async () => await Navegacion.AbrirFichaAsync(_servicios, id)),
                    new Command(async () => await DarDeBajaAsync(id)),
                    new Command(async () => await ReactivarAsync(id))));
            }

            int activos = todos.Count(e => e.Activo);
            Resumen = $"{todos.Count} registrados   ·   {activos} activos   ·   {todos.Count - activos} inactivos";
        }
        catch (Exception ex)
        {
            await _dialog.AlertAsync("No se pudo cargar la nómina", ex.Message);
        }
    }

    private async Task GuardarAsync()
    {
        try
        {
            await _servicio.AltaAsync(Legajo, Dni, Nombre, Apellido, Email, Telefono);

            Legajo = Dni = Nombre = Apellido = Email = Telefono = string.Empty;

            // CA 1.1: el estudiante nuevo aparece enseguida en la lista
            if (Filtro == "Inactivos")
                Filtro = "Activos";
            else
                await CargarAsync();
        }
        catch (ReglaDeNegocioException ex)
        {
            // CA 1.2: advertencia por duplicado o por datos faltantes; no se guarda
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }

    private async Task DarDeBajaAsync(int id)
    {
        try
        {
            await _servicio.InactivarAsync(id);
            await CargarAsync();
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }

    private async Task ReactivarAsync(int id)
    {
        try
        {
            await _servicio.ReactivarAsync(id);
            await CargarAsync();
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }
}
