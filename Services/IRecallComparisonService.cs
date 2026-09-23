using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IRecallComparisonService
{
    RecallComparisonResult Compare(string canonicalText, string transcript);
}
