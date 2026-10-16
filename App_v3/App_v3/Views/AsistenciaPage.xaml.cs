using App_v3.ViewModels;

namespace App_v3.Views;

public partial class AsistenciaPage : ContentPage
{
    private readonly AsistenciaViewModel _vm;

    public AsistenciaPage(AsistenciaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefrescarClasesAsync();
    }
}
