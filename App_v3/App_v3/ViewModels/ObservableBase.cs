using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App_v3.ViewModels;

/// <summary>Base mínima de notificación de cambios para los ViewModels del seguimiento.</summary>
public abstract class ObservableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T campo, T valor, [CallerMemberName] string? nombre = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor)) return false;
        campo = valor;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
        return true;
    }

    protected void Notificar([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));

    protected void NotificarTodo() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
