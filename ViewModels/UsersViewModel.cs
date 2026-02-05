using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Views.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRdpGuard.ViewModels
{
    public class UserInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool IsAdmin { get; set; }
    }

    public partial class UsersViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<UserInfo> _users = new();

        public UsersViewModel()
        {
            Refresh();
        }

        [RelayCommand]
        private async void Refresh()
        {
            try
            {
                var list = await Task.Run(() =>
                {
                    var collected = new List<UserInfo>();

                    try
                    {
                        using var context = new PrincipalContext(ContextType.Machine);
                        using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                        foreach (var result in searcher.FindAll())
                        {
                            if (result is UserPrincipal user)
                            {
                                collected.Add(new UserInfo
                                {
                                    Name = user.SamAccountName ?? user.Name ?? string.Empty,
                                    Description = user.Description ?? string.Empty,
                                    Enabled = user.Enabled ?? false
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Fallback to net user output when AccountManagement is blocked.
                        collected.AddRange(GetUsersFromNetUser());
                    }

                    return collected;
                });

                Users = new ObservableCollection<UserInfo>(list.OrderBy(u => u.Name));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"读取用户列表失败：{ex.Message}", "错误");
            }
        }

        [RelayCommand]
        private async Task ResetPassword(UserInfo user)
        {
            if (user == null) return;

            var dialog = new PasswordPromptWindow();
            dialog.Owner = Application.Current?.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var newPassword = dialog.Password;
                if (string.IsNullOrWhiteSpace(newPassword)) return;

                var success = await Task.Run(() =>
                {
                    try
                    {
                        using var context = new PrincipalContext(ContextType.Machine);
                        var principal = UserPrincipal.FindByIdentity(context, user.Name);
                        if (principal != null)
                        {
                            principal.SetPassword(newPassword);
                            principal.Save();
                            return true;
                        }
                    }
                    catch
                    {
                    }

                    return false;
                });

                if (!success)
                {
                    MessageBox.Show("密码修改失败，请确认以管理员身份运行。", "错误");
                }
                else
                {
                    MessageBox.Show("密码已更新。", "完成");
                }
            }
        }

        private static IEnumerable<UserInfo> GetUsersFromNetUser()
        {
            var results = new List<UserInfo>();
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c net user")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return results;

                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                var collecting = false;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("---"))
                    {
                        collecting = true;
                        continue;
                    }

                    if (!collecting) continue;

                    if (trimmed.Contains("命令成功完成") || trimmed.Contains("The command completed successfully"))
                    {
                        break;
                    }

                    var tokens = trimmed.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var token in tokens)
                    {
                        results.Add(new UserInfo
                        {
                            Name = token,
                            Description = string.Empty,
                            Enabled = true
                        });
                    }
                }
            }
            catch
            {
            }

            return results;
        }
    }
}
