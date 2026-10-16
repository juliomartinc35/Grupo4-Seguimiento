using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Windows.Input;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.ViewModels;

public class FilaEntregaItem : ObservableBase
{
    public int EstudianteId { get; set; }
    public string Legajo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;

    private bool _entregado;
    public bool Entregado
    {
        get => _entregado;
        set
        {
            if (Set(ref _entregado, value))
            {
                Notificar(nameof(EstadoTexto));
                Notificar(nameof(ColorEstado));
            }
        }
    }

    public string EstadoTexto => Entregado ? "Entregó" : "No entregó";
    public Color ColorEstado => Entregado ? Apariencia.Verde : Apariencia.Rojo;

    public FilaEntrega ToFila() => new()
    {
        EstudianteId = EstudianteId,
        Legajo = Legajo,
        NombreCompleto = NombreCompleto,
        Entregado = Entregado
    };
}

/// <summary>H5: registrar la entrega o no entrega de un trabajo práctico.</summary>
public class EntregasViewModel : ObservableBase
{
    private readonly EntregaService _entregas;
    private readonly CalendarioService _calendario;
    private readonly IDialogService _dialog;

    private PlanillaEntrega? _planilla;

    public EntregasViewModel(EntregaService entregas, CalendarioService calendario, IDialogService dialog)
    {
        _entregas = entregas;
        _calendario = calendario;
        _dialog = dialog;
        GuardarCommand = new Command(async () => await GuardarAsync());
    }

    public ObservableCollection<ItemSeleccion> Trabajos { get; } = new();
    public ObservableCollection<FilaEntregaItem> Filas { get; } = new();

    private ItemSeleccion? _trabajoSeleccionado;
    public ItemSeleccion? TrabajoSeleccionado
    {
        get => _trabajoSeleccionado;
        set
        {
            if (Set(ref _trabajoSeleccionado, value))
                _ = CargarPlanillaAsync();
        }
    }

    public ICommand GuardarCommand { get; }

    public bool HayPlanilla => _planilla is not null;
    public bool SinPlanilla => _planilla is null;
    public bool MostrarBanner => _planilla is { YaRegistrado: true };

    public string Banner =>
        "Este trabajo ya tiene información guardada. Se muestra el estado registrado de cada estudiante; podés corregirlo y volver a guardar.";

    private void ActualizarEstado()
    {
        Notificar(nameof(HayPlanilla));
        Notificar(nameof(SinPlanilla));
        Notificar(nameof(MostrarBanner));
    }

    public async Task RefrescarTrabajosAsync()
    {
        try
        {
            var trabajos = await _calendario.ListarTrabajosAsync();
            foreach (var t in trabajos)
            {
                if (Trabajos.Any(x => x.Id == t.Id)) continue;

                var item = new ItemSeleccion
                {
                    Id = t.Id,
                    Fecha = t.FechaLimite,
                    Etiqueta = $"{t.Nombre} (vence {t.FechaLimite:dd/MM/yyyy})"
                };
                int posicion = 0;
                while (posicion < Trabajos.Count && Trabajos[posicion].Fecha <= item.Fecha) posicion++;
                Trabajos.Insert(posicion, item);
            }
        }
        catch (Exception ex)
        {
            await _dialog.AlertAsync("No se pudo cargar el calendario", ex.Message);
        }
    }

    private async Task CargarPlanillaAsync()
    {
        Filas.Clear();
        _planilla = null;

        if (TrabajoSeleccionado is null)
        {
            ActualizarEstado();
            return;
        }

        try
        {
            var planilla = await _entregas.ObtenerPlanillaAsync(TrabajoSeleccionado.Id);
            _planilla = planilla;

            foreach (var f in planilla.Filas)
            {
                Filas.Add(new FilaEntregaItem
                {
                    EstudianteId = f.EstudianteId,
                    Legajo = f.Legajo,
                    NombreCompleto = f.NombreCompleto,
                    Entregado = f.Entregado
                });
            }
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Atención", ex.Message);
        }

        ActualizarEstado();
    }

    // CA 5.1 y CA 5.2
    private async Task GuardarAsync()
    {
        if (_planilla is null || TrabajoSeleccionado is null) return;

        try
        {
            var filas = Filas.Select(f => f.ToFila()).ToList();
            await _entregas.GuardarAsync(TrabajoSeleccionado.Id, filas);

            int entregaron = filas.Count(f => f.Entregado);
            _planilla.YaRegistrado = true;
            ActualizarEstado();

            await _dialog.AlertAsync(
                "Entregas guardadas",
                $"Se registró \"{TrabajoSeleccionado.Etiqueta}\". Entregaron: {entregaron}. No entregaron: {filas.Count - entregaron}.");
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }
}
