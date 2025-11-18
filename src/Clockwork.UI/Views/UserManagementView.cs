using Clockwork.Core;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// User management view.
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

        bool isVisible = IsVisible;
        ImGui.Begin("User Management", ref isVisible);

        ImGui.Text("User List");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Add User"))
        {
            // TODO: Open a dialog to add a user
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_userService != null)
        {
            var users = _userService.GetUsers();

            // User table
            if (ImGui.BeginTable("users_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Email");
                ImGui.TableSetupColumn("Role");
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
        IsVisible = isVisible;
    }
}
