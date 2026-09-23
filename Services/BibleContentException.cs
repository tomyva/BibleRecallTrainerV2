namespace BibleRecallTrainerV2.Services;

public sealed class BibleContentException : Exception
{
    public BibleContentException(string message) : base(message) { }
    public BibleContentException(string message, Exception innerException) : base(message, innerException) { }
}
