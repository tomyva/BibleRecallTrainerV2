using Microsoft.Maui.Storage;

namespace BibleRecallTrainerV2.Services;

public sealed class AppThemeService(IPreferences preferences) : IAppThemeService
{
    private const string ThemeKey = "appearance.colorTheme";
    private const string DefaultTheme = "Purple";
    private ResourceDictionary? resources;

    private static readonly IReadOnlyDictionary<string, ThemePalette> Palettes =
        new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["Purple"] = new("#512BD4", "#AC99EA", "#DFD8F7", "#9880E5", "#2B0B98", "#E7E0F4", "#3A3150", "#DED6F2"),
            ["Red"] = new("#B3261E", "#FFB4AB", "#FFDAD6", "#FFB4AB", "#8C1D18", "#FFE4E1", "#5C211D", "#F0C6C2"),
            ["Orange"] = new("#A94200", "#FFB77D", "#FFE0C2", "#FFB77D", "#713000", "#FFE8D2", "#543019", "#EFCDAE"),
            ["Greyscale"] = new("#3F4850", "#C4C7C9", "#E1E3E5", "#C4C7C9", "#252A2E", "#E8EAEC", "#373C40", "#CDD0D2"),
            ["Yellow"] = new("#745A00", "#E9C349", "#FFF0B3", "#E9C349", "#574300", "#FFF4C8", "#514522", "#E8D58A"),
            ["Blue"] = new("#2457A6", "#A9C7FF", "#D8E2FF", "#A9C7FF", "#173F7A", "#E1E9FF", "#263A5D", "#C7D4F2")
        };

    public IReadOnlyList<string> ThemeNames { get; } = ["Purple", "Red", "Orange", "Greyscale", "Yellow", "Blue"];

    public string CurrentTheme
    {
        get
        {
            var saved = preferences.Get(ThemeKey, DefaultTheme);
            return Palettes.ContainsKey(saved) ? saved : DefaultTheme;
        }
    }

    public void Initialize(ResourceDictionary applicationResources)
    {
        resources = applicationResources;
        ApplyPalette(CurrentTheme, save: false);
    }

    public void Apply(string themeName)
    {
        if (!Palettes.ContainsKey(themeName))
            themeName = DefaultTheme;
        ApplyPalette(themeName, save: true);
    }

    private void ApplyPalette(string themeName, bool save)
    {
        if (resources is null)
            return;

        var palette = Palettes[themeName];
        SetColor("Primary", palette.Primary);
        SetColor("PrimaryDark", palette.PrimaryDark);
        SetColor("Secondary", palette.Secondary);
        SetColor("SecondaryDarkText", palette.SecondaryDarkText);
        SetColor("Tertiary", palette.Tertiary);
        SetColor("ActiveVerseBackground", palette.ActiveVerseBackground);
        SetColor("ActiveVerseBackgroundDark", palette.ActiveVerseBackgroundDark);
        SetColor("ThemeBorder", palette.Border);

        resources["PrimaryBrush"] = new SolidColorBrush(Color.FromArgb(palette.Primary));
        resources["SecondaryBrush"] = new SolidColorBrush(Color.FromArgb(palette.Secondary));
        resources["TertiaryBrush"] = new SolidColorBrush(Color.FromArgb(palette.Tertiary));
        if (save)
            preferences.Set(ThemeKey, themeName);
    }

    private void SetColor(string key, string value) => resources![key] = Color.FromArgb(value);

    private sealed record ThemePalette(
        string Primary,
        string PrimaryDark,
        string Secondary,
        string SecondaryDarkText,
        string Tertiary,
        string ActiveVerseBackground,
        string ActiveVerseBackgroundDark,
        string Border);
}
