using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>A lightweight, owner-drawn indeterminate spinner. Animation uses the WinForms message loop.</summary>
public sealed class ScreenSpinner : Control
{
    private readonly System.Windows.Forms.Timer _timer;
    private int _frame;
    private bool _animating;

    public ScreenSpinner()
    {
        Size = new Size(40, 40);
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _timer = new System.Windows.Forms.Timer { Interval = 80 };
        _timer.Tick += (_, _) => { _frame = (_frame + 1) % 12; Invalidate(); };
        ForeColor = Color.FromArgb(70, 110, 205);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Animating
    {
        get => _animating;
        set
        {
            if (_animating == value) return;
            _animating = value;
            UpdateTimer();
            Invalidate();
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        UpdateTimer();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _timer.Stop();
        base.OnHandleDestroyed(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (_animating && Visible && IsHandleCreated && !DesignMode) _timer.Start();
        else _timer.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var cx = Width / 2f;
        var cy = Height / 2f;
        var radius = Math.Min(Width, Height) * 0.29f;
        var dot = Math.Max(2f, Math.Min(Width, Height) * 0.07f);
        for (var i = 0; i < 12; i++)
        {
            var angle = (i * Math.PI / 6) - Math.PI / 2;
            var age = (i - _frame + 12) % 12;
            var alpha = 45 + (11 - age) * 19;
            using var brush = new SolidBrush(Color.FromArgb(Math.Min(255, alpha), ForeColor));
            var x = cx + (float)Math.Cos(angle) * radius;
            var y = cy + (float)Math.Sin(angle) * radius;
            e.Graphics.FillEllipse(brush, x - dot, y - dot, dot * 2, dot * 2);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
