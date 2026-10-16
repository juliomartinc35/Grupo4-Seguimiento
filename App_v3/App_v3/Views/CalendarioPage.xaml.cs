using App_v3.ViewModels;

namespace App_v3.Views;

public partial class CalendarioPage : ContentPage
{
    private readonly CalendarioViewModel _vm;

    public CalendarioPage(CalendarioViewModel vm)
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
