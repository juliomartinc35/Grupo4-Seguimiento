using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Windows.Input;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.ViewModels;

public class ItemHistorial
{
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public Color ColorEstado { get; set; } = Apariencia.Gris;
}

/// <summary>H9, H10, H12 y H13: ficha del estudiante, historial y registro de contactos.</summary>
public class FichaViewModel : ObservableBase
{
    private readonly FichaService _fichas;
    private readonly ContactoService _contactos;
    private readonly IDialogService _dialog;

    private FichaDetalle? _ficha;

    public FichaViewModel(FichaService fichas, ContactoService contactos, IDialogService dialog)
    {
        _fichas = fichas;
        _contactos = contactos;
        _dialog = dialog;
        RegistrarContactoCommand = new Command(async () => await RegistrarContactoAsync());
    }

    // ---- CA 9.1: datos personales ----
    public string NombreCompleto => _ficha?.Estudiante.NombreCompleto ?? "Ficha del estudiante";
    public string Legajo => _ficha?.Estudiante.Legajo ?? string.Empty;
    public string Dni => _ficha?.Estudiante.Dni ?? string.Empty;
    public string Email => _ficha?.Estudiante.Email ?? string.Empty;
    public string Telefono =>
        string.IsNullOrWhiteSpace(_ficha?.Estudiante.Telefono) ? "—" : _ficha!.Estudiante.Telefono;

    // ---- CA 9.3: estado de cursada ----
    public bool EsActivo => _ficha?.Estudiante.Activo ?? true;
    public string EstadoTexto => EsActivo ? "ACTIVO" : "INACTIVO";
    public Color ColorEstado => Apariencia.Activo(EsActivo);

    // ---- CA 9.2: nivel y motivo ----
    public string NivelTexto => _ficha?.Nivel is { } n ? n.ToString().ToUpperInvariant() : "SIN NIVEL";
    public Color ColorNivel => _ficha?.Nivel is { } n ? Apariencia.Nivel(n) : Apariencia.Gris;
    public string Motivo => _ficha?.Motivo ?? string.Empty;

    // ---- CA 12.3: resumen de la cursada ----
    public string ResumenCursada => _ficha is null
        ? string.Empty
        : $"Inasistencias: {_ficha.TotalInasistencias}   ·   TPs vencidos sin entregar: {_ficha.TotalTpsNoEntregados}";

    public ObservableCollection<ItemHistorial> Asistencias { get; } = new();
    public ObservableCollection<ItemHistorial> Entregas { get; } = new();
    public ObservableCollection<ItemHistorial> Contactos { get; } = new();

    // ---- CA 10.1: formulario de contacto ----
    public IReadOnlyList<string> Medios => ContactoService.Medios;
    public IReadOnlyList<string> Resultados => ContactoService.Resultados;

    private DateTime? _fechaContacto = DateTime.Today;
    public DateTime? FechaContacto { get => _fechaContacto; set => Set(ref _fechaContacto, value); }

    private string? _medio;
    public string? Medio { get => _medio; set => Set(ref _medio, value); }

    private string? _resultado;
    public string? Resultado { get => _resultado; set => Set(ref _resultado, value); }

    private string _notas = string.Empty;
    public string Notas { get => _notas; set => Set(ref _notas, value); }

    public ICommand RegistrarContactoCommand { get; }

    public async Task CargarAsync(int estudianteId)
    {
        try
        {
            _ficha = await _fichas.ObtenerAsync(estudianteId, DateTime.Today);

            Asistencias.Clear();
            foreach (var a in _ficha.Asistencias)
            {
                Asistencias.Add(new ItemHistorial
                {
                    Titulo = $"{a.Fecha:dd/MM/yyyy}",
                    Detalle = a.Tema,
                    Estado = a.Estado,
                    ColorEstado = a.Presente ? Apariencia.Verde : Apariencia.Rojo
                });
            }

            Entregas.Clear();
            foreach (var e in _ficha.Entregas)
            {
                Entregas.Add(new ItemHistorial
                {
                    Titulo = e.Trabajo,
                    Detalle = $"Vence el {e.FechaLimite:dd/MM/yyyy}",
                    Estado = e.Estado,
                    ColorEstado = e.Entregado ? Apariencia.Verde : (e.Vencido ? Apariencia.Rojo : Apariencia.Gris)
                });
            }

            Contactos.Clear();
            foreach (var c in _ficha.Contactos)
            {
                Contactos.Add(new ItemHistorial
                {
                    Titulo = $"{c.Fecha:dd/MM/yyyy} — {c.Medio}",
                    Detalle = c.Notas,
                    Estado = c.Resultado,
                    ColorEstado = Apariencia.Gris
                });
            }

            NotificarTodo();
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }

    // CA 10.1 y CA 10.2
    private async Task RegistrarContactoAsync()
    {
        if (_ficha is null) return;

        try
        {
            await _contactos.RegistrarAsync(_ficha.Estudiante.Id, FechaContacto, Medio, Resultado, Notas);

            Medio = null;
            Resultado = null;
            Notas = string.Empty;
            await CargarAsync(_ficha.Estudiante.Id);

            await _dialog.AlertAsync("Contacto registrado", "La intervención quedó guardada en el historial del estudiante.");
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("No se registró el contacto", ex.Message);
        }
    }
}
