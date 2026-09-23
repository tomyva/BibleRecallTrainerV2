namespace BibleRecallTrainerV2.Models;

public sealed record PracticeQuestion(
    string BookId,
    string BookName,
    int ChapterNumber,
    int VerseNumber,
    string Prompt,
    string Answer);

public sealed record PracticeAnswerResult(bool IsCorrect, string ExpectedAnswer, string Feedback);
