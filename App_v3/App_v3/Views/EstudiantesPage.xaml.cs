using App_v3.ViewModels;

namespace App_v3.Views;

public partial class EstudiantesPage : ContentPage
{
    private readonly EstudiantesViewModel _vm;

    public EstudiantesPage(EstudiantesViewModel vm)
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
