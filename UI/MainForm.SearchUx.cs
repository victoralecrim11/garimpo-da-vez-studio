using GarimpoDaVezStudio.Services;
using System.Drawing.Drawing2D;

namespace GarimpoDaVezStudio.UI;

public sealed partial class MainForm
{
    private readonly CheckBox _sidebarIncludeHistorical = new();
    private readonly ToolTip _sidebarTooltip = new() { AutoPopDelay = 14000, InitialDelay = 300,
        ReshowDelay = 150, ShowAlways = true };
    private int _sidebarHoveredIndex = -1;

    private Control CreateSearchControl(TextBox input)
    {
        var frame = new Panel {
            Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(2),
            Margin = new Padding(0, 4, 6, 5)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3,
            BackColor = Card, Margin = Padding.Empty, Padding = new Padding(4, 2, 4, 2)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
        var searchIcon = new Label {
            Text = "⌕", Dock = DockStyle.Fill, ForeColor = Accent,
            TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 17f)
        };
        input.BorderStyle = BorderStyle.None;
        input.BackColor = Card;
        input.ForeColor = Foreground;
        input.Font = new Font("Segoe UI", 10.5f);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 11, 0, 0);
        var clear = new Button {
            Text = "×", FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill,
            Margin = Padding.Empty, Font = new Font("Segoe UI", 12f),
            BackColor = Card, ForeColor = Muted, TabStop = false,
            Cursor = Cursors.Hand, AccessibleName = "Limpar pesquisa"
        };
        clear.FlatAppearance.BorderSize = 0;
        clear.Click += (_, _) => { input.Clear(); input.Focus(); };
        clear.Visible = input.Text.Length > 0;
        input.TextChanged += (_, _) => clear.Visible = input.Text.Length > 0;
        input.GotFocus += (_, _) => frame.BackColor = Accent;
        input.LostFocus += (_, _) => frame.BackColor = Card;
        layout.Controls.Add(searchIcon, 0, 0);
        layout.Controls.Add(input, 1, 0);
        layout.Controls.Add(clear, 2, 0);
        frame.Controls.Add(layout);
        return frame;
    }

    private void DrawSidebarProduct(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _products.Items.Count) return;
        if (_products.Items[e.Index] is not SidebarSearchItem item) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        bool hovered = e.Index == _sidebarHoveredIndex;
        var back = selected ? Color.FromArgb(35, 87, 124) :
            hovered ? Color.FromArgb(37, 52, 73) : Card;
        using (var brush = new SolidBrush(back))
            e.Graphics.FillRectangle(brush, e.Bounds);
        if (selected)
        {
            using var stripe = new SolidBrush(Accent);
            e.Graphics.FillRectangle(stripe, e.Bounds.X, e.Bounds.Y + 4, 3, e.Bounds.Height - 8);
        }
        var titleArea = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 22,
            Math.Max(30, e.Bounds.Width - 24), e.Bounds.Height - 25);
        var originArea = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 5,
            Math.Max(30, e.Bounds.Width - 24), 17);
        var originColor = item.OriginLabel.Contains("HISTÓRICA", StringComparison.Ordinal)
            ? Color.FromArgb(246, 195, 116) : Accent;
        TextRenderer.DrawText(e.Graphics, item.OriginLabel, new Font("Segoe UI", 8f, FontStyle.Bold),
            originArea, originColor, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, item.ProductTitle, _products.Font, titleArea,
            Foreground, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl);
        using var line = new Pen(Color.FromArgb(47, 61, 82));
        e.Graphics.DrawLine(line, e.Bounds.Left + 10, e.Bounds.Bottom - 1,
            e.Bounds.Right - 10, e.Bounds.Bottom - 1);
    }

    private void SidebarProductHover(object? sender, MouseEventArgs e)
    {
        int index = _products.IndexFromPoint(e.Location);
        if (index == _sidebarHoveredIndex) return;
        _sidebarHoveredIndex = index;
        _products.Invalidate();
        _sidebarTooltip.Hide(_products);
        if (index >= 0 && _products.Items[index] is SidebarSearchItem item)
            _sidebarTooltip.Show(item.OriginLabel + "\n" + item.FullDetails,
                _products, Math.Min(e.X + 14, _products.Width - 20),
                e.Y + 15, 12000);
    }
}
