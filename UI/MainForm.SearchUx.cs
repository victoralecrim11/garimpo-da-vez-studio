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
        // Uso compartilhado pelas buscas lateral e do catálogo.
        // Cantos arredondados e foco são pintados pelo WinForms, sem CSS.
        var frame = new Panel {
            Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(2),
            Margin = new Padding(0, 4, 0, 4)
        };
        bool focused = false;
        void RoundEdges()
        {
            if (frame.Width <= 2 || frame.Height <= 2) return;
            using var path = RoundedOutline(new Rectangle(0, 0, frame.Width, frame.Height), 11);
            var oldRegion = frame.Region;
            frame.Region = new Region(path);
            oldRegion?.Dispose();
        }
        frame.Resize += (_, _) => RoundEdges();
        frame.HandleCreated += (_, _) => RoundEdges();
        frame.Paint += (_, e) =>
        {
            if (frame.Width < 8 || frame.Height < 8) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var border = new Pen(focused ? Accent : Color.FromArgb(67, 85, 108), focused ? 1.8f : 1f);
            using var path = RoundedOutline(new Rectangle(1, 1, frame.Width - 3, frame.Height - 3), 10);
            e.Graphics.DrawPath(border, path);
        };
        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = Card, Padding = new Padding(6, 1, 6, 1),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        var glass = new Panel { Dock = DockStyle.Fill, BackColor = Card, Cursor = Cursors.IBeam };
        glass.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            var cx = glass.Width / 2 - 3;
            var cy = glass.Height / 2 - 3;
            e.Graphics.DrawEllipse(pen, cx - 6, cy - 6, 12, 12);
            e.Graphics.DrawLine(pen, cx + 5, cy + 5, cx + 10, cy + 10);
        };
        glass.Click += (_, _) => input.Focus();
        input.BorderStyle = BorderStyle.None;
        input.BackColor = Card;
        input.ForeColor = Foreground;
        input.Font = new Font("Segoe UI", 10.4f);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(4, 9, 0, 0);
        input.AccessibleDescription = "Digite o nome de produto, categoria ou código de anúncio";
        input.GotFocus += (_, _) => { focused = true; frame.Invalidate(); };
        input.LostFocus += (_, _) => { focused = false; frame.Invalidate(); };
        var clear = new Button {
            Text = "×", FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill,
            Margin = Padding.Empty, Font = new Font("Segoe UI", 13f),
            BackColor = Card, ForeColor = Muted, TabStop = false,
            Cursor = Cursors.Hand, AccessibleName = "Limpar pesquisa"
        };
        clear.FlatAppearance.BorderSize = 0;
        clear.FlatAppearance.MouseOverBackColor = Color.FromArgb(47, 65, 90);
        clear.Click += (_, _) => { input.Clear(); input.Focus(); };
        clear.Visible = !string.IsNullOrEmpty(input.Text);
        input.TextChanged += (_, _) => clear.Visible = !string.IsNullOrEmpty(input.Text);
        input.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Escape) return;
            input.Clear();
            e.Handled = true;
            e.SuppressKeyPress = true;
        };
        layout.Controls.Add(glass, 0, 0);
        layout.Controls.Add(input, 1, 0);
        layout.Controls.Add(clear, 2, 0);
        frame.Controls.Add(layout);
        return frame;
    }

    private static GraphicsPath RoundedOutline(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Max(2, Math.Min(bounds.Width, bounds.Height)));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
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
