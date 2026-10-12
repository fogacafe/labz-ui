using System.ComponentModel;
using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;

namespace Labz.UI.Showcase;

/// <summary>In-memory private credit trading showcase. No orders are submitted to a backend.</summary>
internal static class PrivateBondsDemo
{
    internal sealed record Trade(int Id, string Instrument, string Issuer, string Side, decimal Quantity,
        decimal UnitPrice, string Counterparty, string Status);

    internal static readonly BindingList<Trade> Trades = new([
        new(2401, "CDB-2028", "Banco Horizonte", "BUY", 150, 1024.50m, "Mesa Alpha", "DRAFT"),
        new(2402, "DEB-2030", "Energia Aurora", "SELL", 80, 985.20m, "Gestora Atlas", "RELEASED"),
        new(2403, "CRI-2029", "Vértice Imóveis", "BUY", 200, 1012.75m, "Fundo Boreal", "PENDING"),
        new(2404, "CRA-2031", "Agro Vale", "SELL", 65, 997.10m, "Mesa Delta", "DRAFT")
    ]);

    internal static Control CreateBlotter(Action<int> openTrade, Action createTrade)
    {
        var theme = UiThemeManager.Current;
        var root = new Panel { Dock = DockStyle.Fill, BackColor = theme.Background, Padding = new Padding(24) };
        var header = new Panel { Dock = DockStyle.Top, Height = 94 };
        var title = new Label { Text = "Private Credit  /  Trades", Dock = DockStyle.Top, Height = 44,
            ForeColor = theme.Foreground, Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 19f, FontStyle.Bold) };
        var subtitle = new Label { Text = "Sample trades · double-click a row to inspect or edit", Dock = DockStyle.Top, Height = 28,
            ForeColor = theme.MutedForeground };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = theme.Background };
        var newButton = new Button { Text = "+ New trade", Width = 128, Height = 34, Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat, BackColor = theme.Primary, ForeColor = Color.White, Cursor = Cursors.Hand };
        newButton.FlatAppearance.BorderSize = 0;
        newButton.Click += (_, _) => createTrade();
        toolbar.Controls.Add(newButton);

        var grid = new LabzDataGrid { Dock = DockStyle.Fill, ReadOnlyGrid = true, DataSource = Trades };
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count &&
                grid.Rows[e.RowIndex].DataBoundItem is Trade trade) openTrade(trade.Id);
        };
        root.Controls.Add(grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(header);
        return root;
    }

    internal static Control CreateEditor(int? id)
    {
        var theme = UiThemeManager.Current;
        var trade = id is null ? null : Trades.FirstOrDefault(t => t.Id == id);
        var root = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = theme.Background, Padding = new Padding(24) };
        var title = new Label { Text = trade is null ? "New private credit trade" : $"Trade #{trade.Id}",
            Dock = DockStyle.Top, Height = 64, ForeColor = theme.Foreground,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 19f, FontStyle.Bold) };
        var card = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 0,
            Padding = new Padding(20), BackColor = theme.Surface, Width = 740 };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

        var fields = new (string Label, string Value)[]
        {
            ("Side (BUY / SELL)", trade?.Side ?? "BUY"),
            ("Instrument", trade?.Instrument ?? ""),
            ("Issuer", trade?.Issuer ?? ""),
            ("Quantity", trade?.Quantity.ToString() ?? ""),
            ("Unit price", trade?.UnitPrice.ToString("0.00") ?? ""),
            ("Counterparty", trade?.Counterparty ?? ""),
        };
        var editors = new List<TextBox>();
        foreach (var (label, value) in fields)
        {
            var row = card.RowCount++;
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            card.Controls.Add(new Label { Text = label, ForeColor = theme.MutedForeground,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            var input = new TextBox { Text = value, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
                BackColor = theme.Background, ForeColor = theme.Foreground, Margin = new Padding(6, 10, 6, 10) };
            editors.Add(input);
            card.Controls.Add(input, 1, row);
        }
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, FlowDirection = FlowDirection.LeftToRight,
            BackColor = theme.Surface, Padding = new Padding(20, 6, 0, 0) };
        var save = new Button { Text = "Save demo trade", AutoSize = true, Height = 34,
            BackColor = theme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        save.FlatAppearance.BorderSize = 0;
        var message = new Label { AutoSize = true, ForeColor = theme.MutedForeground, Padding = new Padding(12, 8, 0, 0) };
        save.Click += (_, _) =>
        {
            if (!decimal.TryParse(editors[3].Text, out var quantity) || quantity <= 0 ||
                !decimal.TryParse(editors[4].Text, out var price) || price <= 0 ||
                string.IsNullOrWhiteSpace(editors[1].Text) ||
                (editors[0].Text.Trim().ToUpperInvariant() is not ("BUY" or "SELL")))
            {
                message.Text = "Check side, instrument, quantity and price.";
                return;
            }
            var next = new Trade(trade?.Id ?? (Trades.Max(t => t.Id) + 1),
                editors[1].Text.Trim(), editors[2].Text.Trim(), editors[0].Text.Trim().ToUpperInvariant(),
                quantity, price, editors[5].Text.Trim(), "DRAFT");
            if (trade is null) Trades.Add(next);
            else
            {
                var index = Trades.IndexOf(trade);
                if (index >= 0) Trades[index] = next;
            }
            trade = next;
            message.Text = $"Saved locally as DRAFT · #{next.Id}";
        };
        actions.Controls.Add(save);
        actions.Controls.Add(message);
        root.Controls.Add(actions);
        root.Controls.Add(card);
        root.Controls.Add(title);
        root.Resize += (_, _) => card.Width = Math.Min(760, Math.Max(360, root.ClientSize.Width - 48));
        return root;
    }
}
