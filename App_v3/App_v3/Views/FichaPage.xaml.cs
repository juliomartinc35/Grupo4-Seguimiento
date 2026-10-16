using App_v3.ViewModels;

namespace App_v3.Views;

public partial class FichaPage : ContentPage
{
    private readonly FichaViewModel _vm;

    public FichaPage(FichaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}
