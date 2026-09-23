using System.Text.RegularExpressions;
using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed partial class RecallComparisonService : IRecallComparisonService
{
    public RecallComparisonResult Compare(string canonicalText, string transcript)
    {
        ArgumentNullException.ThrowIfNull(canonicalText);
        ArgumentNullException.ThrowIfNull(transcript);

        var expected = Tokenize(canonicalText);
        var heard = Tokenize(transcript);
        var differences = Align(expected, heard);
        var matches = differences.Count(word => word.Kind is RecallWordKind.Match);
        var denominator = Math.Max(expected.Count, heard.Count);
        var percentage = denominator == 0 ? 100 : (int)Math.Round(matches * 100d / denominator);
        var exact = expected.SequenceEqual(heard, StringComparer.Ordinal);

        return new RecallComparisonResult
        {
            CanonicalText = canonicalText,
            Transcript = transcript,
            Words = differences,
            MatchPercentage = percentage,
            IsExactMatch = exact,
            Feedback = exact
                ? "Exact match"
                : percentage >= 80
                    ? "Nearly there — review the marked differences."
                    : "Keep practicing — review the marked differences and try again."
        };
    }

    private static IReadOnlyList<string> Tokenize(string text) => WordPattern()
        .Matches(text.Replace('\u2018', '\'').Replace('\u2019', '\''))
        .Select(match => match.Value.ToLowerInvariant())
        .ToList();

    private static IReadOnlyList<RecallWordDifference> Align(IReadOnlyList<string> expected, IReadOnlyList<string> heard)
    {
        var costs = new int[expected.Count + 1, heard.Count + 1];
        for (var i = 0; i <= expected.Count; i++) costs[i, 0] = i;
        for (var j = 0; j <= heard.Count; j++) costs[0, j] = j;

        for (var i = 1; i <= expected.Count; i++)
        {
            for (var j = 1; j <= heard.Count; j++)
            {
                var substitution = costs[i - 1, j - 1] + (expected[i - 1] == heard[j - 1] ? 0 : 1);
                costs[i, j] = Math.Min(substitution, Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1));
            }
        }

        var result = new List<RecallWordDifference>();
        var x = expected.Count;
        var y = heard.Count;
        while (x > 0 || y > 0)
        {
            if (x > 0 && y > 0)
            {
                var same = expected[x - 1] == heard[y - 1];
                var diagonalCost = costs[x - 1, y - 1] + (same ? 0 : 1);
                if (costs[x, y] == diagonalCost)
                {
                    result.Add(new RecallWordDifference(
                        same ? RecallWordKind.Match : RecallWordKind.Different,
                        expected[x - 1],
                        heard[y - 1]));
                    x--;
                    y--;
                    continue;
                }
            }

            if (x > 0 && costs[x, y] == costs[x - 1, y] + 1)
            {
                result.Add(new RecallWordDifference(RecallWordKind.Missing, expected[x - 1], string.Empty));
                x--;
            }
            else
            {
                result.Add(new RecallWordDifference(RecallWordKind.Extra, string.Empty, heard[y - 1]));
                y--;
            }
        }

        result.Reverse();
        return result;
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:'[\p{L}\p{N}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
