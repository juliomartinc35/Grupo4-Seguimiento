using App_v3.ViewModels;

namespace App_v3.Views;

public partial class EntregasPage : ContentPage
{
    private readonly EntregasViewModel _vm;

    public EntregasPage(EntregasViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefrescarTrabajosAsync();
    }
}
