using App_v3.ViewModels;

namespace App_v3.Views;

public partial class PanelPage : ContentPage
{
    private readonly PanelSeguimientoViewModel _vm;

    public PanelPage(PanelSeguimientoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }
}
