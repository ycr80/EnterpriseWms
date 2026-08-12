using System;
using System.Windows.Forms;

namespace EnterpriseWms.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ClientSettings.Load();
        using var client = new ApiClient(ClientSettings.Current.ApiBaseAddress);
        using var login = new LoginForm(client);
        if (login.ShowDialog() == DialogResult.OK && client.CurrentUser != null)
            Application.Run(new MainForm(client));
    }
}
