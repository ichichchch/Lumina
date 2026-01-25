using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace Lumina.App.Localization;

public interface ILocalizationService : INotifyPropertyChanged
{
    CultureInfo CurrentCulture { get; }
    string this[string key] { get; }
    void SetCulture(string? cultureName);
}

public sealed class LocalizationService : ILocalizationService
{
    private static readonly Uri EnDictionaryUri = new("avares://Lumina.App/Resources/Strings/Strings.en.axaml");
    private static readonly Uri ZhDictionaryUri = new("avares://Lumina.App/Resources/Strings/Strings.zh-CN.axaml");

    private ResourceInclude? _currentStringsDictionary;

    public static LocalizationService Instance { get; private set; } = null!;

    public LocalizationService()
    {
        Instance = this;
        CurrentCulture = CultureInfo.CurrentUICulture;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo CurrentCulture { get; private set; }

    public string this[string key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (Application.Current is null)
                return key;

            return Application.Current.TryFindResource(key, out var value) ? value?.ToString() ?? key : key;
        }
    }

    public void SetCulture(string? cultureName)
    {
        var culture = string.IsNullOrWhiteSpace(cultureName)
            ? CultureInfo.CurrentUICulture
            : CultureInfo.GetCultureInfo(cultureName);

        CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;

        var dictionaryUri = culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? ZhDictionaryUri
            : EnDictionaryUri;

        if (Application.Current is not null)
        {
            var resources = Application.Current.Resources;

            for (var i = resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                if (resources.MergedDictionaries[i] is ResourceInclude include &&
                    (include.Source == EnDictionaryUri || include.Source == ZhDictionaryUri))
                {
                    resources.MergedDictionaries.RemoveAt(i);
                }
            }

            #pragma warning disable IL2026
            _currentStringsDictionary = new ResourceInclude(new Uri("avares://Lumina.App/")) { Source = dictionaryUri };
            #pragma warning restore IL2026
            resources.MergedDictionaries.Add(_currentStringsDictionary);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
