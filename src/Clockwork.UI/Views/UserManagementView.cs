using Clockwork.Core;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue de gestion des utilisateurs.
/// </summary>
public class UserManagementView : IView
{
    private readonly ApplicationContext _appContext;
    private UserService? _userService;

    public bool IsVisible { get; set; } = false;

    public UserManagementView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _userService = _appContext.GetService<UserService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.Begin("Gestion des utilisateurs", ref Unsafe.AsRef(in IsVisible));

        ImGui.Text("Liste des utilisateurs");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Ajouter utilisateur"))
        {
            // TODO: Ouvrir un dialogue pour ajouter un utilisateur
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_userService != null)
        {
            var users = _userService.GetUsers();

            // Table des utilisateurs
            if (ImGui.BeginTable("users_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Nom");
                ImGui.TableSetupColumn("Email");
                ImGui.TableSetupColumn("Rôle");
                ImGui.TableHeadersRow();

                foreach (var user in users)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(user.Name);
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(user.Email);
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(user.Role);
                }

                ImGui.EndTable();
            }
        }

        ImGui.End();
    }
}
