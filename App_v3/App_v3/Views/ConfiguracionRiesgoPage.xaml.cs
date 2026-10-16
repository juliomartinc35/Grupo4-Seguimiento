using App_v3.ViewModels;

namespace App_v3.Views;

public partial class ConfiguracionRiesgoPage : ContentPage
{
    private readonly ConfiguracionRiesgoViewModel _vm;

    public ConfiguracionRiesgoPage(ConfiguracionRiesgoViewModel vm)
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
