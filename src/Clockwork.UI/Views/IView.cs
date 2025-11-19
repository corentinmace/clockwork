namespace Clockwork.UI.Views;

/// <summary>
/// Interface de base pour toutes les vues ImGui.
/// </summary>
public interface IView
{
    /// <summary>
    /// Dessine la vue.
    /// </summary>
    void Draw();

    /// <summary>
    /// Indique si la vue est actuellement visible.
    /// </summary>
    bool IsVisible { get; set; }
}
