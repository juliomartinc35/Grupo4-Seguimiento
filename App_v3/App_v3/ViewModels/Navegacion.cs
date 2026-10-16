using Microsoft.Extensions.DependencyInjection;
using App_v3.Views;

namespace App_v3.ViewModels;

public static class Navegacion
{
    /// <summary>Abre la ficha de un estudiante (H9) encima de la pestaña actual.</summary>
    public static async Task AbrirFichaAsync(IServiceProvider servicios, int estudianteId)
    {
        var vm = servicios.GetRequiredService<FichaViewModel>();
        await vm.CargarAsync(estudianteId);
        await Shell.Current.Navigation.PushAsync(new FichaPage(vm));
    }
}
