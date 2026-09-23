using System.Text.RegularExpressions;
using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed partial class QuizService : IQuizService
{
    public IReadOnlyList<PracticeQuestion> CreateChapterQuiz(BibleBook book, BibleChapter chapter, int questionCount = 5)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(chapter);
        if (questionCount <= 0 || chapter.Verses.Count == 0)
            return [];

        var count = Math.Min(questionCount, chapter.Verses.Count);
        var questions = new List<PracticeQuestion>(count);
        for (var index = 0; index < count; index++)
        {
            var verseIndex = index * chapter.Verses.Count / count;
            var verse = chapter.Verses[verseIndex];
            var matches = WordPattern().Matches(verse.Text).Cast<Match>().ToList();
            if (matches.Count == 0)
                continue;

            var answerMatch = matches
                .OrderByDescending(match => match.Length)
                .ThenBy(match => match.Index)
                .First();
            var prompt = string.Concat(
                verse.Text.AsSpan(0, answerMatch.Index),
                "____",
                verse.Text.AsSpan(answerMatch.Index + answerMatch.Length));
            questions.Add(new PracticeQuestion(
                book.Id,
                book.DisplayName,
                chapter.ChapterNumber,
                verse.VerseNumber,
                prompt,
                answerMatch.Value));
        }

        return questions;
    }

    public PracticeAnswerResult CheckAnswer(PracticeQuestion question, string answer)
    {
        ArgumentNullException.ThrowIfNull(question);
        var correct = Normalize(question.Answer) == Normalize(answer);
        return new PracticeAnswerResult(
            correct,
            question.Answer,
            correct ? "Correct." : $"The missing word is “{question.Answer}”.");
    }

    private static string Normalize(string value) => string.Concat(value
        .Replace('\u2018', '\'')
        .Replace('\u2019', '\'')
        .Where(character => char.IsLetterOrDigit(character) || character == '\''))
        .ToLowerInvariant();

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’][\p{L}\p{N}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
