using System.Drawing;
using System.Windows.Forms;

namespace EnterpriseWms.WinForms;

internal static class UiTheme
{
    public static readonly Color Primary = Color.FromArgb(30, 64, 175);
    public static readonly Color Sidebar = Color.FromArgb(15, 23, 42);
    public static readonly Color Background = Color.FromArgb(241, 245, 249);
    public static readonly Color Success = Color.FromArgb(5, 150, 105);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);

    public static Button Button(string text, bool primary = false) => new()
    {
        Text = text,
        Height = 36,
        AutoSize = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Primary : Color.White,
        ForeColor = primary ? Color.White : Color.FromArgb(30, 41, 59),
        Font = new Font("Microsoft YaHei UI", 9F),
        Margin = new Padding(4)
    };

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(226, 232, 240);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
    }
}
