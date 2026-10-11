using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Lightweight animated loading indicator; stops its timer when hidden or disposed.</summary>
public class LabzSpinner : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 80 };
    private int _frame;
    private bool _active = true;

    public LabzSpinner()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(36, 36);
        AccessibleName = "Loading";
        _timer.Tick += (_, _) => { _frame = (_frame + 1) % 12; Invalidate(); };
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Active
    {
        get => _active;
        set { _active = value; UpdateTimer(); Invalidate(); }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        UpdateTimer();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateTimer();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _timer.Stop();
        base.OnHandleDestroyed(e);
    }

    private void UpdateTimer()
    {
        if (Active && Visible && IsHandleCreated && !DesignMode) _timer.Start();
        else _timer.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var cx = Width / 2f;
        var cy = Height / 2f;
        var radius = Math.Max(2, Math.Min(Width, Height) * .32f);
        var dot = Math.Max(2, Math.Min(Width, Height) * .075f);
        for (var i = 0; i < 12; i++)
        {
            var angle = (float)((i * 30 - 90) * Math.PI / 180);
            var x = cx + MathF.Cos(angle) * radius;
            var y = cy + MathF.Sin(angle) * radius;
            var alpha = 40 + ((i + _frame) % 12) * 18;
            using var brush = new SolidBrush(Color.FromArgb(Math.Min(255, alpha), theme.Primary));
            e.Graphics.FillEllipse(brush, x - dot, y - dot, dot * 2, dot * 2);
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke((Action)Invalidate);
        else Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
            UiThemeManager.ThemeChanged -= OnThemeChanged;
        }
        base.Dispose(disposing);
    }
}
