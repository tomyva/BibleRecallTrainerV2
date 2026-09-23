using System.Collections.ObjectModel;
using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class BooksViewModel : ViewModelBase
{
    private readonly IBibleContentService bibleContent;
    private bool isLoaded;

    public BooksViewModel(IBibleContentService bibleContent)
    {
        this.bibleContent = bibleContent;
        OpenBookCommand = new Command<BibleBook>(async book => await OpenBookAsync(book));
    }

    public ObservableCollection<BibleBook> Books { get; } = [];
    public ICommand OpenBookCommand { get; }

    public async Task LoadAsync()
    {
        if (isLoaded)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var books = await bibleContent.GetBooksAsync();
            Books.Clear();
            foreach (var book in books)
                Books.Add(book);
            isLoaded = true;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static Task OpenBookAsync(BibleBook? book) => book is null
        ? Task.CompletedTask
        : Shell.Current.GoToAsync($"{nameof(ChaptersPage)}?bookId={Uri.EscapeDataString(book.Id)}");
}
