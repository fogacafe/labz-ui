using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;
using Labz.UI.WinForms.Layout;
using Labz.UI.WinForms.Dialogs;
using Labz.UI.WinForms.Notifications;
using Labz.UI.WinForms.Navigation;
using Labz.UI.Validation;

namespace Labz.UI.Showcase;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ShowcaseForm());
    }
}

/// <summary>Interactive catalog of Labz UI controls and layout containers.</summary>
public sealed class ShowcaseForm : Form
{
    private readonly Panel _page = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Label _subtitle = new() { AutoSize = true };
    private readonly LabzButton _theme = new() { Text = "Switch theme", Width = 160 };
    private readonly LabzProgressBar _progress = new() { Value = 65, ShowPercentage = true, Width = 260 };
    private readonly LabzLabel _status = new() { Text = "Ready", Variant = LabzLabelVariant.Muted };
    private readonly Bitmap _sampleImage = CreateSampleImage();
    private readonly LabzTooltip _tooltips = new();

    public ShowcaseForm()
    {
        Text = "Labz UI • Component Gallery";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(850, 650);
        ClientSize = new Size(1120, 800);
        Controls.Add(_page);

        var header = new Panel { Dock = DockStyle.Top, Height = 105, Padding = new Padding(24, 16, 24, 10) };
        var heading = new LabzLabel { Text = "Labz UI", Variant = LabzLabelVariant.Heading, Location = new Point(24, 12) };
        _subtitle.Text = "WinForms controls • interactive component gallery";
        _subtitle.Location = new Point(26, 52);
        _theme.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _theme.Location = new Point(Width - 220, 25);
        header.Resize += (_, _) => _theme.Left = Math.Max(350, header.ClientSize.Width - _theme.Width - 24);
        header.Controls.AddRange([heading, _subtitle, _theme]);
        Controls.Add(header);
        _theme.Click += (_, _) => UiThemeManager.Apply(
            ReferenceEquals(UiThemeManager.Current, UiTheme.Light) ? UiTheme.Dark : UiTheme.Light);

        var gallery = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2, Padding = new Padding(20), GrowStyle = TableLayoutPanelGrowStyle.AddRows
        };
        gallery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        gallery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _page.Controls.Add(gallery);

        AddCard(gallery, "Buttons", CreateButtons());
        AddCard(gallery, "Navigation", CreateNavigation());
        AddCard(gallery, "App shell", CreateAppShell());
        AddCard(gallery, "Tab workspace", CreateTabWorkspace());
        AddCard(gallery, "Advanced inputs", CreateAdvancedInputs());
        AddCard(gallery, "Visual components", CreateVisualComponents());
        AddCard(gallery, "Data grid", CreateDataGrid());
        AddCard(gallery, "Dialogs", CreateDialogs());
        AddCard(gallery, "Toast notifications", CreateToasts());
        AddCard(gallery, "Text & validation", CreateTextInputs());
        AddCard(gallery, "Selection", CreateSelection());
        AddCard(gallery, "Search & lookup", CreateLookup());
        AddCard(gallery, "Images", CreateImages());
        AddCard(gallery, "Feedback", CreateFeedback());
        AddCard(gallery, "Containers", CreateContainers());
        AddCard(gallery, "Typography & separators", CreateTypography());

        UiThemeManager.ThemeChanged += RefreshTheme;
        RefreshTheme(this, EventArgs.Empty);
    }

    private static void AddCard(TableLayoutPanel gallery, string title, Control content)
    {
        var row = gallery.Controls.Count / 2;
        var col = gallery.Controls.Count % 2;
        while (gallery.RowCount <= row)
        {
            gallery.RowCount++;
            gallery.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
        }
        var card = new LabzGroupBox
        {
            Text = title, Dock = DockStyle.Fill, Margin = new Padding(8),
            Padding = new Padding(14, 28, 14, 14)
        };
        content.Dock = DockStyle.Fill;
        card.Controls.Add(content);
        gallery.Controls.Add(card, col, row);
    }

    private static Control CreateTabWorkspace()
    {
        var workspace = new LabzTabWorkspace();
        workspace.Open("bonds/trades", "Trades", () => new LabzTextBox { Text = "Unsaved trade draft", Dock = DockStyle.Top });
        workspace.Open("funds/positions", "Positions", () => new LabzLabel { Text = "Fund positions", Dock = DockStyle.Top });
        workspace.PageClosing += (_, args) =>
        {
            if (args.PageId != "bonds/trades") return;
            args.Cancel = MessageBox.Show("Close the trade draft?", "Unsaved changes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes;
        };
        return workspace;
    }

    private static Control CreateAppShell()
    {
        var shell = new LabzAppShell();
        var bonds = new LabzModule("bonds", "Public Bonds", "B");
        bonds.Pages.Add(new LabzNavigationPage("trades", "Trades", "Operations"));
        bonds.Pages.Add(new LabzNavigationPage("import", "Import", "Operations"));
        var funds = new LabzModule("funds", "Funds", "F");
        funds.Pages.Add(new LabzNavigationPage("positions", "Positions", "Queries"));
        shell.SetModules([bonds, funds]);
        shell.StatusBar.UserName = "Alex";
        shell.StatusBar.TenantName = "Trading Desk";
        shell.StatusBar.EnvironmentName = "DEV";
        shell.StatusBar.TenantRequested += (_, _) => shell.StatusBar.TenantName = "Research";
        shell.StatusBar.UserRequested += (_, _) => MessageBox.Show("User profile requested");
        shell.ConnectWorkspace(new LabzTabWorkspace(), (moduleId, pageId) =>
            new LabzLabel { Text = moduleId + " / " + pageId, Dock = DockStyle.Top });
        return shell;
    }

    private static Control CreateDataGrid()
    {
        var grid = new LabzDataGrid { Dock = DockStyle.Fill, ReadOnlyGrid = true };
        grid.DataSource = new System.ComponentModel.BindingList<GridSample>([
            new GridSample(1001, "Draft", 1200.50m),
            new GridSample(1002, "Released", 850.75m),
            new GridSample(1003, "Pending", 2400m)
        ]);
        return grid;
    }

    private sealed record GridSample(int Ticket, string Status, decimal Amount);

    private static Control CreateVisualComponents()
    {
        var panel = Stack();
        var badges = Flow();
        badges.Controls.AddRange([
            new LabzBadge { Text = "Draft", Variant = LabzBadgeVariant.Neutral },
            new LabzBadge { Text = "Success", Variant = LabzBadgeVariant.Success },
            new LabzBadge { Text = "Warning", Variant = LabzBadgeVariant.Warning },
            new LabzBadge { Text = "Error", Variant = LabzBadgeVariant.Error }
        ]);
        var spinner = new LabzSpinner { Width = 36, Height = 36 };
        var card = new LabzCard { Title = "Summary", Width = 340, Height = 90 };
        card.Controls.Add(new LabzLabel { Text = "Reusable themed content surface", Location = new Point(18, 48) });
        var empty = new LabzEmptyState { Heading = "No results", Description = "Try changing the search criteria.", Width = 340, Height = 115 };
        panel.Controls.AddRange([empty, card, spinner, badges]);
        return panel;
    }

    private static Control CreateAdvancedInputs()
    {
        var panel = Stack();
        panel.Controls.AddRange([
            new LabzDatePicker { Width = 270 },
            new LabzDatePicker { Width = 270, ShowTime = true },
            new LabzNumericInput { Width = 270, NumericFormat = LabzNumericFormat.Currency, Value = 1234.56m },
            new LabzNumericInput { Width = 270, NumericFormat = LabzNumericFormat.Percent, Value = 12.5m }
        ]);
        return panel;
    }

    private Control CreateNavigation()
    {
        var panel = Stack();
        var tabs = new LabzTabs { Width = 400, Height = 175 };
        var overview = new TabPage("Overview");
        overview.Controls.Add(new LabzLabel { Text = "Overview content", Location = new Point(18, 20) });
        var details = new TabPage("Details");
        details.Controls.Add(new LabzLabel { Text = "Details content", Location = new Point(18, 20) });
        tabs.TabPages.AddRange([overview, details]);
        var menu = new LabzContextMenu();
        menu.Items.Add("Refresh", null, (_, _) => _status.Text = "Refresh selected");
        menu.Items.Add("Copy", null, (_, _) => _status.Text = "Copy selected");
        tabs.ContextMenuStrip = menu;
        _tooltips.SetLabzToolTip(tabs, "Ctrl+Tab changes pages; right-click for actions");
        panel.Controls.Add(tabs);
        return panel;
    }

    private Control CreateButtons()
    {
        var panel = Stack();
        var variants = Flow();
        variants.Controls.AddRange([
            new LabzButton { Text = "Solid" },
            new LabzButton { Text = "Outline", Variant = LabzButtonVariant.Outline },
            new LabzButton { Text = "Ghost", Variant = LabzButtonVariant.Ghost }
        ]);
        var sizes = Flow();
        sizes.Controls.AddRange([
            new LabzButton { Text = "Small", ButtonSize = LabzButtonSize.Small },
            new LabzButton { Text = "Large", ButtonSize = LabzButtonSize.Large }
        ]);
        var iconButton = new LabzButton
        {
            Text = "Image + text", Image = _sampleImage,
            ImagePlacement = LabzButtonImagePlacement.Left,
            ImageSize = new Size(22, 22), Width = 180
        };
        iconButton.Click += (_, _) => _status.Text = "Image button clicked";
        panel.Controls.AddRange([iconButton, sizes, variants]);
        return panel;
    }

    private Control CreateToasts()
    {
        var panel = Stack();
        var success = new LabzButton { Text = "Success toast", Width = 180 };
        success.Click += (_, _) => LabzToast.Success(this, "Changes saved successfully!");
        var warning = new LabzButton { Text = "Warning toast", Width = 180, Variant = LabzButtonVariant.Outline };
        warning.Click += (_, _) => LabzToast.Warning(this, "Please review your inputs.");
        var error = new LabzButton { Text = "Error toast", Width = 180, Variant = LabzButtonVariant.Ghost };
        error.Click += (_, _) => LabzToast.Error(this, "Something went wrong.");
        panel.Controls.AddRange([error, warning, success]);
        return panel;
    }

    private Control CreateDialogs()
    {
        var panel = Stack();
        var success = new LabzButton { Text = "Success message", Width = 180 };
        success.Click += (_, _) => LabzMessageBox.Show("Your changes have been saved.", "Saved", LabzDialogKind.Success, owner: this);
        var warning = new LabzButton { Text = "Warning message", Width = 180, Variant = LabzButtonVariant.Outline };
        warning.Click += (_, _) => LabzMessageBox.Show("Check your data before continuing.", "Warning", LabzDialogKind.Warning, owner: this);
        var confirm = new LabzButton { Text = "Confirm deletion", Width = 180, Variant = LabzButtonVariant.Ghost };
        confirm.Click += (_, _) => _status.Text = "Confirmation: " + LabzMessageBox.Confirm("Delete this item?", "Confirm", this);
        panel.Controls.AddRange([confirm, warning, success]);
        return panel;
    }

    private static Control CreateTextInputs()
    {
        var panel = Stack();
        panel.Controls.AddRange([
            new LabzTextBox { PlaceholderText = "Normal input", Width = 300, Height = 44 },
            new LabzTextBox { PlaceholderText = "Required field", ValidationState = ValidationSeverity.Error,
                ValidationMessage = "This field is required", Width = 300, Height = 70 },
            new LabzTextBox { PlaceholderText = "Warning", ValidationState = ValidationSeverity.Warning,
                ValidationMessage = "Please double-check", Width = 300, Height = 70 }
        ]);
        return panel;
    }

    private static Control CreateSelection()
    {
        var panel = Stack();
        var combo = new LabzComboBox { Width = 280 };
        combo.Items.AddRange(["Alpha", "Beta", "Gamma"]);
        combo.SelectedIndex = 0;
        var radios = Flow();
        radios.Controls.AddRange([
            new LabzRadioButton { Text = "Daily", Checked = true },
            new LabzRadioButton { Text = "Weekly" }
        ]);
        panel.Controls.AddRange([
            new LabzSwitch { Checked = true, Text = "Notifications", Width = 230 },
            new LabzCheckBox { Text = "Remember my choice", Checked = true },
            radios, combo
        ]);
        return panel;
    }

    private static Control CreateLookup()
    {
        var panel = Stack();
        var lookup = new LabzSearchLookup { Width = 330, PlaceholderText = "Search a city..." };
        var cities = new[] { "São Paulo", "Campinas", "Rio de Janeiro", "Curitiba", "Florianópolis", "Belo Horizonte" };
        lookup.SearchProvider = (query, limit, token) =>
        {
            token.ThrowIfCancellationRequested();
            IReadOnlyList<LabzLookupItem> results = cities
                .Where(city => city.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .Select(city => new LabzLookupItem(city, city, new Dictionary<string, object?>()))
                .ToArray();
            return Task.FromResult(results);
        };
        panel.Controls.AddRange([
            new LabzLabel { Text = "Try typing São, Rio or Curitiba", Variant = LabzLabelVariant.Caption },
            lookup
        ]);
        return panel;
    }

    private Control CreateImages()
    {
        var panel = Stack();
        var images = Flow();
        images.Controls.AddRange([
            new LabzImage { Image = _sampleImage, Fit = LabzImageFit.Contain,
                ShowBorder = true, Size = new Size(135, 100) },
            new LabzImage { Image = _sampleImage, Fit = LabzImageFit.Cover,
                CornerRadius = 20, Size = new Size(135, 100) },
            new LabzImage { Image = _sampleImage, Fit = LabzImageFit.Fill,
                Opacity = .55f, Size = new Size(135, 100) }
        ]);
        panel.Controls.AddRange([
            new LabzLabel { Text = "Contain • Cover • Fill with 55% opacity", Variant = LabzLabelVariant.Caption },
            images
        ]);
        return panel;
    }

    private Control CreateFeedback()
    {
        var panel = Stack();
        var update = new LabzButton { Text = "Advance progress" };
        update.Click += (_, _) => _progress.Value = _progress.Value >= 100 ? 0 : Math.Min(100, _progress.Value + 10);
        panel.Controls.AddRange([
            _status,
            update,
            new LabzProgressBar { Value = 35, Variant = LabzProgressVariant.Success, Width = 260 },
            _progress
        ]);
        return panel;
    }

    private static Control CreateContainers()
    {
        var panel = Stack();
        var accordion = new LabzAccordion { Title = "Advanced settings", ExpandedHeight = 150, Width = 360 };
        accordion.ContentPanel.Controls.Add(new LabzCheckBox { Text = "Enable advanced mode", Dock = DockStyle.Top });
        var inner = new LabzPanel { ShowBorder = true, Height = 55, Width = 340 };
        inner.Controls.Add(new LabzLabel { Text = "Nested LabzPanel", Dock = DockStyle.Top });
        panel.Controls.AddRange([accordion, inner]);
        return panel;
    }

    private static Control CreateTypography()
    {
        var panel = Stack();
        panel.Controls.AddRange([
            new LabzLabel { Text = "Muted helper text", Variant = LabzLabelVariant.Muted },
            new LabzSeparator { Width = 320 },
            new LabzLabel { Text = "Caption", Variant = LabzLabelVariant.Caption },
            new LabzLabel { Text = "Section heading", Variant = LabzLabelVariant.Heading },
            new LabzLabel { Text = "Body text", Variant = LabzLabelVariant.Body }
        ]);
        return panel;
    }

    private static LabzStackPanel Stack() => new()
    {
        Direction = LabzDirection.Vertical, Wrap = false, Spacing = 8,
        Padding = new Padding(6)
    };

    private static LabzFlowPanel Flow() => new()
    {
        Direction = LabzDirection.Horizontal, Wrap = true, Spacing = 8,
        Height = 60, Width = 410, Padding = new Padding(2)
    };

    private static Bitmap CreateSampleImage()
    {
        var bitmap = new Bitmap(320, 180);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.FromArgb(61, 106, 230));
        using var brush = new SolidBrush(Color.FromArgb(255, 194, 86));
        g.FillEllipse(brush, 110, 20, 100, 100);
        using var pen = new Pen(Color.White, 7);
        g.DrawLine(pen, 0, 160, 120, 90);
        g.DrawLine(pen, 120, 90, 220, 155);
        g.DrawLine(pen, 220, 155, 320, 70);
        return bitmap;
    }

    private void RefreshTheme(object? sender, EventArgs e)
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        ForeColor = theme.Foreground;
        _page.BackColor = theme.Background;
        _subtitle.ForeColor = theme.Foreground;
        Invalidate(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UiThemeManager.ThemeChanged -= RefreshTheme;
            _sampleImage.Dispose();
            _tooltips.Dispose();
        }
        base.Dispose(disposing);
    }
}
