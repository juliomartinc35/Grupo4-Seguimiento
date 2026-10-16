using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Windows.Input;
using App_v3.Models;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.ViewModels;

public class FilaPanelItem
{
    public FilaPanelItem(FilaSeguimiento fila, ICommand abrirFicha)
    {
        Fila = fila;
        AbrirFichaCommand = abrirFicha;
    }

    public FilaSeguimiento Fila { get; }
    public ICommand AbrirFichaCommand { get; }

    public string NombreCompleto => Fila.NombreCompleto;
    public string Legajo => $"Legajo {Fila.Legajo}";
    public string EtiquetaNivel => Fila.Nivel.ToString().ToUpperInvariant();
    public Color ColorNivel => Apariencia.Nivel(Fila.Nivel);

    // CA 8.2: datos que justifican el nivel
    public string Detalle =>
        $"Inasistencias: {Fila.Inasistencias}   ·   TPs vencidos sin entregar: {Fila.TpsVencidosNoEntregados}";

    // CA 11.1: marca de contacto con la fecha de la última intervención
    public string EtiquetaContacto => Fila.UltimoContacto is { } fecha
        ? $"📞 Contactado el {fecha:dd/MM}"
        : "Sin contactar";

    public Color ColorContacto => Fila.Contactado ? Apariencia.Verde : Apariencia.Gris;
}

/// <summary>H6, H8, H10 y H11: panel de seguimiento ordenado por prioridad de riesgo.</summary>
public class PanelSeguimientoViewModel : ObservableBase
{
    private readonly RiesgoService _riesgo;
    private readonly IDialogService _dialog;
    private readonly IServiceProvider _servicios;

    public ObservableCollection<FilaPanelItem> Filas { get; } = new();

    private bool _soloNoContactados;
    public bool SoloNoContactados
    {
        get => _soloNoContactados;
        set
        {
            if (Set(ref _soloNoContactados, value))
                _ = CargarAsync();
        }
    }

    private string _resumen = string.Empty;
    public string Resumen
    {
        get => _resumen;
        private set => Set(ref _resumen, value);
    }

    public ICommand ActualizarCommand { get; }

    public PanelSeguimientoViewModel(RiesgoService riesgo, IDialogService dialog, IServiceProvider servicios)
    {
        _riesgo = riesgo;
        _dialog = dialog;
        _servicios = servicios;
        ActualizarCommand = new Command(async () => await CargarAsync());
    }

    // CA 8.4: se recalcula cada vez que se abre o se actualiza el panel
    public async Task CargarAsync()
    {
        try
        {
            var datos = await _riesgo.ObtenerPanelAsync(DateTime.Today, SoloNoContactados);

            Filas.Clear();
            foreach (var fila in datos)
            {
                var id = fila.EstudianteId;
                Filas.Add(new FilaPanelItem(fila,
                    new Command(async () => await Navegacion.AbrirFichaAsync(_servicios, id))));
            }

            int altos = datos.Count(f => f.Nivel == NivelRiesgo.Alto);
            int medios = datos.Count(f => f.Nivel == NivelRiesgo.Medio);
            int bajos = datos.Count(f => f.Nivel == NivelRiesgo.Bajo);
            Resumen = $"{datos.Count} estudiantes activos   ·   Alto: {altos}   ·   Medio: {medios}   ·   Bajo: {bajos}";
        }
        catch (Exception ex)
        {
            await _dialog.AlertAsync("No se pudo cargar el panel", ex.Message);
        }
    }
}
