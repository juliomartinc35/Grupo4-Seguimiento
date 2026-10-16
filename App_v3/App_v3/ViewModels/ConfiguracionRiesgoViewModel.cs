using System.Windows.Input;
using App_v3.Models;
using App_v3.Seguimiento;
using App_v3.Services;

namespace App_v3.ViewModels;

/// <summary>H7: configurar los umbrales de riesgo (reemplaza la versión anterior).</summary>
public class ConfiguracionRiesgoViewModel : ObservableBase
{
    private readonly IDatabaseService _db;
    private readonly IDialogService _dialog;
    private readonly RiesgoService _riesgo;
    private int _id;

    public ConfiguracionRiesgoViewModel(IDatabaseService db, IDialogService dialog, RiesgoService riesgo)
    {
        _db = db;
        _dialog = dialog;
        _riesgo = riesgo;
        GuardarCommand = new Command(async () => await GuardarAsync());
    }

    private string _inasistenciasMedio = string.Empty;
    public string InasistenciasMedio { get => _inasistenciasMedio; set => Set(ref _inasistenciasMedio, value); }

    private string _inasistenciasAlto = string.Empty;
    public string InasistenciasAlto { get => _inasistenciasAlto; set => Set(ref _inasistenciasAlto, value); }

    private string _tpsNoEntregadosMedio = string.Empty;
    public string TpsNoEntregadosMedio { get => _tpsNoEntregadosMedio; set => Set(ref _tpsNoEntregadosMedio, value); }

    private string _tpsNoEntregadosAlto = string.Empty;
    public string TpsNoEntregadosAlto { get => _tpsNoEntregadosAlto; set => Set(ref _tpsNoEntregadosAlto, value); }

    private string _resumenNiveles = string.Empty;
    public string ResumenNiveles { get => _resumenNiveles; private set => Set(ref _resumenNiveles, value); }

    public ICommand GuardarCommand { get; }

    public async Task CargarAsync()
    {
        try
        {
            var config = (await _db.GetAllAsync<ConfiguracionRiesgo>()).FirstOrDefault() ?? new ConfiguracionRiesgo();

            _id = config.Id;
            InasistenciasMedio = config.InasistenciasMedio.ToString();
            InasistenciasAlto = config.InasistenciasAlto.ToString();
            TpsNoEntregadosMedio = config.TpsNoEntregadosMedio.ToString();
            TpsNoEntregadosAlto = config.TpsNoEntregadosAlto.ToString();

            await ActualizarResumenAsync();
        }
        catch (Exception ex)
        {
            await _dialog.AlertAsync("No se pudo cargar la configuración", ex.Message);
        }
    }

    // CA 7.2: muestra cuántos estudiantes quedan en cada nivel con los umbrales vigentes
    private async Task ActualizarResumenAsync()
    {
        var filas = await _riesgo.CalcularAsync(DateTime.Today);
        int altos = filas.Count(f => f.Nivel == NivelRiesgo.Alto);
        int medios = filas.Count(f => f.Nivel == NivelRiesgo.Medio);
        int bajos = filas.Count(f => f.Nivel == NivelRiesgo.Bajo);

        ResumenNiveles =
            $"Con estos umbrales: {altos} en riesgo Alto, {medios} Medio y {bajos} Bajo ({filas.Count} estudiantes activos).";
    }

    private async Task GuardarAsync()
    {
        if (!int.TryParse(InasistenciasMedio?.Trim(), out var im) ||
            !int.TryParse(InasistenciasAlto?.Trim(), out var ia) ||
            !int.TryParse(TpsNoEntregadosMedio?.Trim(), out var tm) ||
            !int.TryParse(TpsNoEntregadosAlto?.Trim(), out var ta))
        {
            await _dialog.AlertAsync("Valores inválidos", "Ingresá números enteros en los cuatro umbrales.");
            return;
        }

        var config = new ConfiguracionRiesgo
        {
            Id = _id,
            InasistenciasMedio = im,
            InasistenciasAlto = ia,
            TpsNoEntregadosMedio = tm,
            TpsNoEntregadosAlto = ta
        };

        // CA 7.3: los valores contradictorios no se guardan
        if (!config.EsValida(out var mensaje))
        {
            await _dialog.AlertAsync("Umbrales inválidos", mensaje);
            return;
        }

        try
        {
            // CA 7.1: se registra la nueva configuración
            await _db.SaveAsync(config);
            _id = config.Id;

            await ActualizarResumenAsync();
            await _dialog.AlertAsync("Umbrales guardados", "La configuración se guardó. " + ResumenNiveles);
        }
        catch (Exception ex)
        {
            await _dialog.AlertAsync("No se pudo guardar", ex.Message);
        }
    }
}
