using System.Collections.ObjectModel;
using System.Windows.Input;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.ViewModels;

public class ItemCalendario
{
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
}

/// <summary>H2: calendario de clases y de trabajos del cuatrimestre.</summary>
public class CalendarioViewModel : ObservableBase
{
    private readonly CalendarioService _servicio;
    private readonly IDialogService _dialog;

    public CalendarioViewModel(CalendarioService servicio, IDialogService dialog)
    {
        _servicio = servicio;
        _dialog = dialog;
        AgregarClaseCommand = new Command(async () => await AgregarClaseAsync());
        AgregarTrabajoCommand = new Command(async () => await AgregarTrabajoAsync());
    }

    // ---- Formulario de clase (CA 2.1) ----
    private DateTime? _fechaClase = DateTime.Today;
    public DateTime? FechaClase { get => _fechaClase; set => Set(ref _fechaClase, value); }

    private string _temaClase = string.Empty;
    public string TemaClase { get => _temaClase; set => Set(ref _temaClase, value); }

    // ---- Formulario de trabajo (CA 2.2) ----
    private string _nombreTrabajo = string.Empty;
    public string NombreTrabajo { get => _nombreTrabajo; set => Set(ref _nombreTrabajo, value); }

    private DateTime? _fechaLimite = DateTime.Today.AddDays(7);
    public DateTime? FechaLimite { get => _fechaLimite; set => Set(ref _fechaLimite, value); }

    // ---- Listas (CA 2.3) ----
    public ObservableCollection<ItemCalendario> Clases { get; } = new();
    public ObservableCollection<ItemCalendario> Trabajos { get; } = new();

    public ICommand AgregarClaseCommand { get; }
    public ICommand AgregarTrabajoCommand { get; }

    public async Task CargarAsync()
    {
        try
        {
            var clases = await _servicio.ListarClasesAsync();
            Clases.Clear();
            foreach (var c in clases)
                Clases.Add(new ItemCalendario { Titulo = c.Tema, Detalle = $"{c.Fecha:dddd dd/MM/yyyy}" });

            var trabajos = await _servicio.ListarTrabajosAsync();
            Trabajos.Clear();
            foreach (var t in trabajos)
                Trabajos.Add(new ItemCalendario { Titulo = t.Nombre, Detalle = $"Entrega hasta el {t.FechaLimite:dd/MM/yyyy}" });
        }
        catch (Exception ex)
        {
            await _dialog.AlertAsync("No se pudo cargar el calendario", ex.Message);
        }
    }

    private async Task AgregarClaseAsync()
    {
        try
        {
            await _servicio.AgregarClaseAsync(FechaClase, TemaClase);
            TemaClase = string.Empty;
            await CargarAsync();
        }
        catch (ReglaDeNegocioException ex)
        {
            // CA 2.4: se pide completar los datos y no se guarda
            await _dialog.AlertAsync("Falta información", ex.Message);
        }
    }

    private async Task AgregarTrabajoAsync()
    {
        try
        {
            await _servicio.AgregarTrabajoAsync(NombreTrabajo, FechaLimite);
            NombreTrabajo = string.Empty;
            await CargarAsync();
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Falta información", ex.Message);
        }
    }
}
