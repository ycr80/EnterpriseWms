using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EnterpriseWms.WinForms;

internal sealed class LoginForm : Form
{
    private readonly ApiClient _client;
    private readonly TextBox _username = new() { Text = "admin", Width = 280 };
    private readonly TextBox _password = new() { Text = "Admin123!", Width = 280, UseSystemPasswordChar = true };
    private readonly Label _error = new() { ForeColor = UiTheme.Danger, AutoSize = true };
    private readonly Button _login = UiTheme.Button("登录系统", true);

    public LoginForm(ApiClient client)
    {
        _client = client;
        Text = "EnterpriseWms 登录";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 420);
        BackColor = UiTheme.Background;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        AcceptButton = _login;

        var card = new TableLayoutPanel { Width = 360, Height = 330, Left = 50, Top = 40, BackColor = Color.White, Padding = new Padding(35), ColumnCount = 1, RowCount = 9 };
        card.Controls.Add(new Label { Text = "企业仓储管理系统", Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true });
        card.Controls.Add(new Label { Text = "EnterpriseWms · REST API", ForeColor = Color.Gray, AutoSize = true });
        card.Controls.Add(new Label { Text = "用户名", AutoSize = true, Margin = new Padding(0, 18, 0, 4) });
        card.Controls.Add(_username);
        card.Controls.Add(new Label { Text = "密码", AutoSize = true, Margin = new Padding(0, 12, 0, 4) });
        card.Controls.Add(_password);
        _login.Width = 280;
        _login.Margin = new Padding(0, 18, 0, 0);
        _login.Click += async (_, __) => await LoginAsync();
        card.Controls.Add(_login);
        card.Controls.Add(_error);
        card.Controls.Add(new Label { Text = "演示账号：admin / Admin123!", ForeColor = Color.Gray, AutoSize = true });
        Controls.Add(card);
    }

    private async Task LoginAsync()
    {
        _login.Enabled = false;
        _error.Text = "正在连接 API…";
        try
        {
            await _client.LoginAsync(_username.Text, _password.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception) { _error.Text = exception.Message; }
        finally { _login.Enabled = true; }
    }
}
