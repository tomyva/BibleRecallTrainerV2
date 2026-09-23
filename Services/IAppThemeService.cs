namespace BibleRecallTrainerV2.Services;

public interface IAppThemeService
{
    IReadOnlyList<string> ThemeNames { get; }
    string CurrentTheme { get; }
    void Initialize(ResourceDictionary resources);
    void Apply(string themeName);
}
