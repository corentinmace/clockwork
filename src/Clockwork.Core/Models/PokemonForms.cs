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
    AllForms = 0,        // All forms (A-Z, !, ?)
    OnlyF = 1,           // Only F form
    OnlyR = 2,           // Only R form
    OnlyI = 3,           // Only I form
    OnlyE = 4,           // Only E form
    OnlyN = 5,           // Only N form
    OnlyD = 6,           // Only D form
    Exclamation = 7      // Only ! form
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
            0 => "All Forms (A-Z, !, ?)",
            1 => "Only F",
            2 => "Only R",
            3 => "Only I",
            4 => "Only E",
            5 => "Only N",
            6 => "Only D",
            7 => "Only !",
            _ => $"Unknown ({tableValue})"
        };
    }
}
