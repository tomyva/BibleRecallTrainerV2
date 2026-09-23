using System.Collections.ObjectModel;
using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class ChaptersViewModel : ViewModelBase
{
    private readonly IBibleContentService bibleContent;
    private string bookId = string.Empty;
    private string bookName = string.Empty;

    public ChaptersViewModel(IBibleContentService bibleContent)
    {
        this.bibleContent = bibleContent;
        OpenChapterCommand = new Command<BibleChapter>(async chapter => await OpenChapterAsync(chapter));
    }

    public string BookName
    {
        get => bookName;
        private set => SetProperty(ref bookName, value);
    }

    public ObservableCollection<BibleChapter> Chapters { get; } = [];
    public ICommand OpenChapterCommand { get; }

    public async Task LoadAsync(string id)
    {
        if (string.Equals(bookId, id, StringComparison.OrdinalIgnoreCase) && Chapters.Count > 0)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var books = await bibleContent.GetBooksAsync();
            var book = books.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                ?? throw new BibleContentException($"Book '{id}' is not included in the syllabus.");
            bookId = book.Id;
            BookName = book.DisplayName;
            Chapters.Clear();
            foreach (var chapter in book.Chapters)
                Chapters.Add(chapter);
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

    private Task OpenChapterAsync(BibleChapter? chapter) => chapter is null
        ? Task.CompletedTask
        : Shell.Current.GoToAsync($"{nameof(ChapterReaderPage)}?bookId={Uri.EscapeDataString(bookId)}&chapter={chapter.ChapterNumber}");
}
