using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Windows.Input;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.ViewModels;

public class ItemSeleccion
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
}

public class FilaAsistenciaItem : ObservableBase
{
    public int EstudianteId { get; set; }
    public string Legajo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;

    private bool _presente;
    public bool Presente
    {
        get => _presente;
        set
        {
            if (Set(ref _presente, value))
            {
                Notificar(nameof(EstadoTexto));
                Notificar(nameof(ColorEstado));
            }
        }
    }

    private bool _editable = true;
    public bool Editable { get => _editable; set => Set(ref _editable, value); }

    // CA 3.2: la casilla desmarcada pasa a Ausente
    public string EstadoTexto => Presente ? "Presente" : "Ausente";
    public Color ColorEstado => Presente ? Apariencia.Verde : Apariencia.Rojo;

    public FilaAsistencia ToFila() => new()
    {
        EstudianteId = EstudianteId,
        Legajo = Legajo,
        NombreCompleto = NombreCompleto,
        Presente = Presente
    };
}

/// <summary>H3 y H4: toma de asistencia y corrección de una fecha ya registrada.</summary>
public class AsistenciaViewModel : ObservableBase
{
    private readonly AsistenciaService _asistencia;
    private readonly CalendarioService _calendario;
    private readonly IDialogService _dialog;

    private PlanillaAsistencia? _planilla;
    private bool _modoEdicion;

    public AsistenciaViewModel(AsistenciaService asistencia, CalendarioService calendario, IDialogService dialog)
    {
        _asistencia = asistencia;
        _calendario = calendario;
        _dialog = dialog;

        GuardarCommand = new Command(async () => await GuardarAsync());
        ModificarCommand = new Command(HabilitarEdicion);
        GuardarCambiosCommand = new Command(async () => await GuardarCambiosAsync());
    }

    public ObservableCollection<ItemSeleccion> Clases { get; } = new();
    public ObservableCollection<FilaAsistenciaItem> Filas { get; } = new();

    private ItemSeleccion? _claseSeleccionada;
    public ItemSeleccion? ClaseSeleccionada
    {
        get => _claseSeleccionada;
        set
        {
            if (Set(ref _claseSeleccionada, value))
                _ = CargarPlanillaAsync(notificar: true);
        }
    }

    public ICommand GuardarCommand { get; }
    public ICommand ModificarCommand { get; }
    public ICommand GuardarCambiosCommand { get; }

    // ---- Estado de la pantalla ----
    public bool HayPlanilla => _planilla is not null;
    public bool SinPlanilla => _planilla is null;
    public bool MostrarBanner => _planilla is { YaRegistrada: true };
    public bool MostrarGuardar => _planilla is { YaRegistrada: false };
    public bool MostrarModificar => _planilla is { YaRegistrada: true } && !_modoEdicion;
    public bool MostrarGuardarCambios => _planilla is { YaRegistrada: true } && _modoEdicion;

    public string Banner => _planilla is { YaRegistrada: true }
        ? (_modoEdicion
            ? "Editando una asistencia ya registrada. Presioná \"Guardar Cambios\" para confirmar."
            : "Ya existe un registro de asistencia para esta fecha. Presioná \"Modificar Asistencia\" para corregirlo.")
        : string.Empty;

    private void ActualizarEstado()
    {
        Notificar(nameof(HayPlanilla));
        Notificar(nameof(SinPlanilla));
        Notificar(nameof(MostrarBanner));
        Notificar(nameof(MostrarGuardar));
        Notificar(nameof(MostrarModificar));
        Notificar(nameof(MostrarGuardarCambios));
        Notificar(nameof(Banner));
    }

    /// <summary>Agrega al selector las clases nuevas del calendario sin tocar la selección actual.</summary>
    public async Task RefrescarClasesAsync()
    {
        try
        {
            var clases = await _calendario.ListarClasesAsync();
            foreach (var c in clases)
            {
                if (Clases.Any(x => x.Id == c.Id)) continue;

                var item = new ItemSeleccion
                {
                    Id = c.Id,
                    Fecha = c.Fecha,
                    Etiqueta = $"{c.Fecha:dd/MM/yyyy} — {c.Tema}"
                };
                int posicion = 0;
                while (posicion < Clases.Count && Clases[posicion].Fecha <= item.Fecha) posicion++;
                Clases.Insert(posicion, item);
            }
        }
        catch (Exception ex)
        {
            await _dialog.AlertAsync("No se pudo cargar el calendario", ex.Message);
        }
    }

    private async Task CargarPlanillaAsync(bool notificar)
    {
        Filas.Clear();
        _planilla = null;
        _modoEdicion = false;

        if (ClaseSeleccionada is null)
        {
            ActualizarEstado();
            return;
        }

        try
        {
            var planilla = await _asistencia.ObtenerPlanillaAsync(ClaseSeleccionada.Id);
            _planilla = planilla;

            foreach (var f in planilla.Filas)
            {
                Filas.Add(new FilaAsistenciaItem
                {
                    EstudianteId = f.EstudianteId,
                    Legajo = f.Legajo,
                    NombreCompleto = f.NombreCompleto,
                    Presente = f.Presente,
                    Editable = !planilla.YaRegistrada
                });
            }

            ActualizarEstado();

            // CA 3.4: el sistema avisa que ya existe un registro para ese día
            if (planilla.YaRegistrada && notificar)
            {
                await _dialog.AlertAsync(
                    "Asistencia ya registrada",
                    $"Ya existe un registro de asistencia para el {ClaseSeleccionada.Fecha:dd/MM/yyyy}. " +
                    "Podés revisarlo o corregirlo con \"Modificar Asistencia\".");
            }
        }
        catch (ReglaDeNegocioException ex)
        {
            ActualizarEstado();
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }

    // CA 3.3
    private async Task GuardarAsync()
    {
        if (_planilla is null || ClaseSeleccionada is null) return;

        try
        {
            var filas = Filas.Select(f => f.ToFila()).ToList();
            await _asistencia.GuardarAsync(ClaseSeleccionada.Id, filas);

            int presentes = filas.Count(f => f.Presente);
            await CargarPlanillaAsync(notificar: false);
            await _dialog.AlertAsync(
                "Asistencia guardada",
                $"Se registró la asistencia del {ClaseSeleccionada.Fecha:dd/MM/yyyy}. " +
                $"Presentes: {presentes}. Ausentes: {filas.Count - presentes}.");
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }

    // CA 4.2
    private void HabilitarEdicion()
    {
        if (_planilla is not { YaRegistrada: true }) return;

        _modoEdicion = true;
        foreach (var f in Filas) f.Editable = true;
        ActualizarEstado();
    }

    // CA 4.3 y CA 4.4
    private async Task GuardarCambiosAsync()
    {
        if (_planilla is null || ClaseSeleccionada is null) return;

        try
        {
            int cambios = await _asistencia.CorregirAsync(
                ClaseSeleccionada.Id, Filas.Select(f => f.ToFila()).ToList());

            await CargarPlanillaAsync(notificar: false);
            await _dialog.AlertAsync(
                "Asistencia actualizada",
                cambios == 0
                    ? "No se detectaron cambios en la asistencia."
                    : "La asistencia fue actualizada correctamente. El riesgo de los estudiantes se recalcula automáticamente.");
        }
        catch (ReglaDeNegocioException ex)
        {
            await _dialog.AlertAsync("Atención", ex.Message);
        }
    }
}
