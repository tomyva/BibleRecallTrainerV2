namespace BibleRecallTrainerV2.Services;

public sealed class ProgressDataException(string message, Exception? innerException = null) : Exception(message, innerException);
