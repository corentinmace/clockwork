namespace Clockwork.Core.Models;

/// <summary>
/// Shellos and Gastrodon regional form variants (Gen IV).
/// </summary>
public enum ShellosForm : uint
{
    WestSea = 0,  // Pink form (Western Sinnoh)
    EastSea = 1   // Blue form (Eastern Sinnoh)
}

/// <summary>
/// Unown form table values (Gen IV).
/// Controls which Unown letter forms can appear in encounters.
/// </summary>
public enum UnownFormTable : uint
{
    MostForms = 0,           // A-Z forms available
    OnlyFRIEND = 1,          // Only F, R, I, E, N, D forms
    ExclamationQuestion = 2  // Only ! and ? forms
}

/// <summary>
/// Helper class for Pokemon form display names.
/// </summary>
public static class PokemonFormsHelper
{
    public static string GetShellosFormName(uint formValue)
    {
        return formValue switch
        {
            0 => "West Sea (Pink)",
            1 => "East Sea (Blue)",
            _ => $"Unknown ({formValue})"
        };
    }

    public static string GetUnownTableName(uint tableValue)
    {
        return tableValue switch
        {
            0 => "Most Forms (A-Z)",
            1 => "Only FRIEND",
            2 => "Exclamation/Question (! ?)",
            _ => $"Unknown ({tableValue})"
        };
    }
}
