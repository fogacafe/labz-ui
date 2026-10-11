using System.ComponentModel;
using Labz.UI.WinForms.Controls;

namespace Labz.UI.Showcase;

/// <summary>Interactive acceptance demo for DataGrid v1 features.</summary>
public sealed class DataGridDemoForm : Form
{
    private readonly LabzDataGrid _grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, EditableGrid = true };
    private readonly BindingList<TradeRow> _trades = new()
    {
        new TradeRow { Id = "T-1001", Counterparty = "XP", Quantity = 100, Amount = 120000m },
        new TradeRow { Id = "T-1002", Counterparty = "XP", Quantity = 200, Amount = 450000m },
        new TradeRow { Id = "T-1003", Counterparty = "BTG", Quantity = 50, Amount = 99000m },
        new TradeRow { Id = "T-1004", Counterparty = "BTG", Quantity = 80, Amount = 78000m }
    };
    private readonly FlowLayoutPanel _groupHeaders = new()
    {
        Dock = DockStyle.Top, Height = 52, AutoScroll = true, WrapContents = false
    };
    private readonly TextBox _search = new() { Width = 170, PlaceholderText = "Find trade..." };
    private string? _savedLayout;

    public DataGridDemoForm()
    {
        Text = "Labz UI • DataGrid v1";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1080, 680);
        MinimumSize = new Size(760, 480);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 48, WrapContents = false,
            Padding = new Padding(8, 6, 8, 4)
        };
        Button Action(string label, Action action)
        {
            var button = new LabzButton { Text = label, Width = 122, Height = 32 };
            button.Click += (_, _) => action();
            toolbar.Controls.Add(button);
            return button;
        }

        toolbar.Controls.Add(_search);
        Action("Find next", () => _grid.TryFindNext(_search.Text));
        Action("Group", () => _grid.GroupRows(row => ((TradeRow)row.DataBoundItem!).Counterparty));
        Action("Clear groups", () => _grid.ClearGrouping());
        Action("Save layout", () => _savedLayout = _grid.ExportColumnLayout());
        Action("Load layout", () => { if (_savedLayout is not null) _grid.ImportColumnLayout(_savedLayout); });
        Action("Clear errors", () => _grid.ClearKeyedValidations());

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Ticket", DataPropertyName = "Id", ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Counterparty", HeaderText = "Counterparty", DataPropertyName = "Counterparty" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Quantity", DataPropertyName = "Quantity" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", DataPropertyName = "Amount" });
        _grid.DataSource = _trades;
        _grid.ValidationRowKeySelector = row => (row.DataBoundItem as TradeRow)?.Id;
        _grid.CellValueChangedHandler = e =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.RowCount) return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not TradeRow trade) return;
            _grid.SetKeyedRowValidation(trade.Id,
                LabzDataGrid.CellValidationSeverity.Warning,
                "Edited trade: revalidation required");
            _grid.RefreshGrouping();
        };
        _grid.CellValidating += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name != "Quantity") return;
            if (int.TryParse(e.FormattedValue?.ToString(), out var quantity) && quantity > 0)
            {
                if (_grid.Rows[e.RowIndex].DataBoundItem is TradeRow valid)
                    _grid.ClearKeyedCellValidation(valid.Id, "Quantity");
                return;
            }
            if (_grid.Rows[e.RowIndex].DataBoundItem is TradeRow invalid)
                _grid.SetKeyedCellValidation(invalid.Id, "Quantity",
                    LabzDataGrid.CellValidationSeverity.Error, "Quantity must be greater than zero");
            e.Cancel = true;
        };
        _grid.GroupsChanged += (_, _) => RebuildGroupHeaders();

        var note = new Label
        {
            Dock = DockStyle.Bottom, Height = 32,
            Text = "Edit a value to flag its trade • Right-click headers to choose columns • Drag headers to reorder",
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0)
        };
        Controls.Add(_grid);
        Controls.Add(_groupHeaders);
        Controls.Add(toolbar);
        Controls.Add(note);
    }

    private void RebuildGroupHeaders()
    {
        _groupHeaders.SuspendLayout();
        try
        {
            _groupHeaders.Controls.Clear();
            foreach (var group in _grid.GetGroups())
            {
                var key = group.Key;
                var total = _trades.Where(x => x.Counterparty == key).Sum(x => x.Amount);
                var header = new LabzDataGridGroupHeader
                {
                    Width = 270, Height = 40,
                    GroupTitle = key,
                    GroupSummary = $"{group.Count} trades · {total:N0}",
                    Expanded = group.Expanded
                };
                header.ExpandToggleRequested += (_, _) => _grid.SetGroupExpanded(key, !header.Expanded);
                _groupHeaders.Controls.Add(header);
            }
        }
        finally { _groupHeaders.ResumeLayout(); }
    }

    private sealed class TradeRow
    {
        public string Id { get; set; } = "";
        public string Counterparty { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
