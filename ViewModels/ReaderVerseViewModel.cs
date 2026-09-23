using System.ComponentModel;
using System.Runtime.CompilerServices;
using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class ReaderVerseViewModel(BibleVerse verse) : INotifyPropertyChanged
{
    private bool isActive;

    public int VerseNumber => verse.VerseNumber;
    public string Text => verse.Text;

    public bool IsActive
    {
        get => isActive;
        set
        {
            if (isActive == value)
                return;
            isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
