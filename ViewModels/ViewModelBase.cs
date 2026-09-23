using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BibleRecallTrainerV2.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private bool isBusy;
    private string errorMessage = string.Empty;

    public bool IsBusy
    {
        get => isBusy;
        protected set => SetProperty(ref isBusy, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        protected set
        {
            if (SetProperty(ref errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
