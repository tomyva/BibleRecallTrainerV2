using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IQuizService
{
    IReadOnlyList<PracticeQuestion> CreateChapterQuiz(BibleBook book, BibleChapter chapter, int questionCount = 5);
    PracticeAnswerResult CheckAnswer(PracticeQuestion question, string answer);
}
